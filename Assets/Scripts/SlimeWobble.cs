using UnityEngine;

/// <summary>
/// Animation "gelée" du slime — le modèle McSteeg n'a aucune animation
/// intégrée, donc on l'anime par code. DÉMANDE SALIM : PLUS D'EFFET,
/// et il faut que ça se VOIE (salim disait « y'a pas d'animation »).
///   • il SAUTILLE vraiment : hop généreux, on voit le slime quitter le sol,
///   • l'écrasement / étirement est BEAUCOUP plus fort (vrai rebond de gelée),
///   • il se PENCHE vers sa direction de déplacement,
///   • il se BALANCE gauche/droite en rythme (roulis de gelée),
///   • il GIGOTE quand il tourne brusquement,
///   • il se TOURNE vers sa direction (les yeux avancent),
///   • petite respiration quand il est à l'arrêt.
/// Tout s'applique sur l'enfant visuel (VisualSlime) : le gameplay
/// (physique, squash du corps par SlimeController) reste inchangé.
/// Robustesse : si le Rigidbody est absent, on déduit la vitesse de la
/// position (delta entre 2 frames) → l'animation marche dans tous les cas.
/// </summary>
[DefaultExecutionOrder(210)]
public class SlimeWobble : MonoBehaviour
{
    [Tooltip("Le visuel animé (l'enfant 'VisualSlime'). Vide = retrouvé tout seul.")]
    public Transform visual;

    // NOTE : champs renommés (bounceRun/bounceWalk/squashForce) — les préfabs
    // sérialisaient les ANCIENNES valeurs (3.4/1.7/0.16, trop discrètes) ;
    // un renommage force Unity à repartir sur les nouvelles valeurs par défaut.
    [Tooltip("Rebonds par seconde quand le slime court à fond")]
    public float bounceRun = 4.2f;
    [Tooltip("Rebonds par seconde quand il bouge doucement")]
    public float bounceWalk = 2.2f;
    [Tooltip("Intensité de l'écrasement (0 = raide, 0.45 = très mou)")]
    public float squashForce = 0.28f;

    [Header("Effets visibles (demande salim : plus d'animation !)")]
    [Tooltip("Hauteur du hop : le slime décolle du sol en courant (0.18 = bien visible)")]
    public float hopAmount = 0.18f;
    [Tooltip("Balancement gauche/droite en degrés (roulis de gelée)")]
    public float rollAmount = 16f;
    [Tooltip("Penche vers l'avant dans sa direction de déplacement (degrés)")]
    public float leanAmount = 14f;
    [Tooltip("Gigotement quand il tourne brusquement (force du ressort)")]
    public float jiggleStrength = 4.5f;

    Rigidbody rb;       // peut rester null → on mesure la position
    Vector3 lastPos;    // position précédente (fallback sans Rigidbody)
    Vector3 baseScale, basePos;
    float baseYaw;
    float phase;        // avancement du rebond
    float yaw;          // orientation horizontale courante
    float squash;       // écrasement lissé (relatif à 1)

    // Gigotement (ressort) déclenché quand il tourne brusquement
    float jiggle;        // angle actuel du gigotement
    float jiggleVel;     // vitesse du ressort
    Vector3 dirPrev = Vector3.zero;   // direction d'avant (pour détecter les virages)

    bool warnedOnce;

    void Start()
    {
        if (visual == null)
        {
            // Recherche directe, puis dans TOUT l'arbre (si le modèle a été renesté)
            Transform t = transform.Find("VisualSlime");
            if (t == null) t = transform.Find("VisualLowPoly");
            if (t == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child != transform && child.name.Contains("Visual"))
                        { t = child; break; }
                }
            }
            visual = t;
        }
        if (visual == null)
        {
            // Pas de visuel trouvé : on prévient UNE fois (lisible dans Editor.log)
            if (!warnedOnce)
            {
                warnedOnce = true;
                Debug.LogWarning("[SLIME-WOBBLE] Aucun visuel trouvé sur " + name +
                                 " — pas d'animation possible ! (préfab cassé ?)");
            }
            return;
        }

        baseScale = visual.localScale;
        basePos = visual.localPosition;
        baseYaw = visual.localEulerAngles.y;
        yaw = baseYaw;
        squash = 1f;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
        lastPos = transform.position;

        // Diagnostic une fois : le log dit clairement si tout est branché
        Debug.Log("[SLIME-WOBBLE] " + name +
                  " | visuel = " + visual.name +
                  " | Rigidbody = " + (rb != null ? "OK" : "non (fallback position)") +
                  " | échelle visuel = " + baseScale.ToString("F2"));
    }

    void Update()
    {
        if (visual == null)
        {
            // Une seule alerte : le slime bouge sans animation = problème
            if (!warnedOnce)
            {
                warnedOnce = true;
                Debug.LogWarning("[SLIME-WOBBLE] " + name + " : pas de visuel → pas d'animation.");
            }
            return;
        }
        float dt = Time.deltaTime;

        // ── Vitesse : via Rigidbody, sinon en mesurant la position ─────
        Vector3 vel;
        if (rb != null)
            vel = rb.linearVelocity;
        else
        {
            vel = (transform.position - lastPos) / Mathf.Max(0.0001f, dt);
            lastPos = transform.position;
        }
        float speed = new Vector2(vel.x, vel.z).magnitude;
        float move01 = Mathf.Clamp01(speed / 4f);   // vitesse "pleine" à 4 m/s

        // ── Tourner les yeux vers la direction de déplacement ────────
        if (speed > 0.4f)
        {
            float targetYaw = baseYaw + Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-9f * dt));
        }

        // ── Virage brusque → GIGOTEMENT (ressort de gelée) ───────────
        if (speed > 1f)
        {
            Vector3 dirNow = new Vector3(vel.x, 0f, vel.z) / speed;
            if (dirPrev != Vector3.zero)
            {
                float turn = 1f - Mathf.Clamp01(Vector3.Dot(dirPrev, dirNow));
                jiggleVel += turn * jiggleStrength;   // on pousse le ressort
            }
            dirPrev = dirNow;
        }
        else
        {
            dirPrev = Vector3.zero;
        }

        // Le ressort du gigotement oscille et s'amortit tout seul
        jiggleVel += -jiggle * 40f * dt;   // rappeleur (raideur du gel)
        jiggleVel *= 1f - Mathf.Min(1f, 6f * dt);   // amortissement
        jiggle += jiggleVel * dt;

        // ── Rebond : écrasé / étiré, plus vite quand il court ─────────
        if (move01 > 0.02f)
            phase += Mathf.Lerp(bounceWalk, bounceRun, move01) * dt * 2f * Mathf.PI;

        float wave = Mathf.Sin(phase);
        float target = 1f
            - squashForce * (0.15f + 0.85f * move01) * wave              // rebond mou (comprime / étire)
            + 0.05f * Mathf.Sin(Time.time * 2.4f) * (1f - move01);        // respiration à l'arrêt

        // Le gigotement ajoute un petit pli d'écrasement en plus
        target += 0.10f * jiggle;
        squash = Mathf.Lerp(squash, target, 1f - Mathf.Exp(-14f * dt));

        // Volume conservé : quand il s'écrase, il grossit sur les côtés
        float xz = 1f / Mathf.Sqrt(Mathf.Max(0.5f, squash));
        visual.localScale = new Vector3(
            baseScale.x * xz,
            baseScale.y * squash,
            baseScale.z * xz);

        // ── HOP : il décolle du sol au sommet du rebond (bien visible) ─
        float hop = Mathf.Max(0f, Mathf.Sin(phase + 0.9f)) * hopAmount * move01;
        visual.localPosition = basePos + Vector3.up * hop *
            (visual.parent != null ? visual.parent.lossyScale.x : 1f);

        // ── PENCHANT + ROULIS de gelée (en degrés, beaucoup plus forts) ─
        float roll = Mathf.Sin(phase) * rollAmount * move01 + jiggle * 22f;
        float lean = leanAmount * move01;   // penche vers l'avant (il "sprinte")
        visual.localRotation = Quaternion.Euler(lean, yaw, roll);
    }
}