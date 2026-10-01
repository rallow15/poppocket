using UnityEngine;

/// <summary>
/// Animation "gelée" du slime — le modèle McSteeg n'a aucune animation
/// intégrée, donc on l'anime par code. DÉMANDE SALIM : PLUS D'EFFET !
///   • rebond écrasé / étiré rythmé par la vitesse (plus il court, plus il
///     se compresse vite),
///   • il SAUTILLE : le visuel décolle du sol puis retombe (petit hop),
///   • il se PENCHE vers sa direction de déplacement,
///   • il se BALANCE gauche/droite en rythme (roulis de gelée),
///   • il GIGOTE quand il change brusquement de direction,
///   • il se TOURNE vers sa direction (les yeux avancent),
///   • petite respiration quand il est à l'arrêt.
/// Tout s'applique sur l'enfant visuel (VisualSlime) : le gameplay
/// (physique, squash du corps par SlimeController) reste inchangé.
/// </summary>
[DefaultExecutionOrder(210)]
public class SlimeWobble : MonoBehaviour
{
    [Tooltip("Le visuel animé (l'enfant 'VisualSlime'). Vide = retrouvé tout seul.")]
    public Transform visual;

    [Tooltip("Rebonds par seconde quand le slime court à fond")]
    public float bounceRateRun = 3.4f;
    [Tooltip("Rebonds par seconde quand il bouge doucement")]
    public float bounceRateWalk = 1.7f;
    [Tooltip("Intensité de l'écrasement (0 = raide, 0.4 = très mou)")]
    public float squashAmount = 0.16f;

    [Header("Effets en plus (demande salim : plus d'animation !)")]
    [Tooltip("Hauteur du hop : le slime décolle du sol en courant (0.02 = subtil)")]
    public float hopAmount = 0.06f;
    [Tooltip("Balancement gauche/droite en degrés (roulis de gelée)")]
    public float rollAmount = 9f;
    [Tooltip("Penche vers l'avant dans sa direction de déplacement (degrés)")]
    public float leanAmount = 7f;
    [Tooltip("Gigotement quand il Tourne brusquement (force du ressort)")]
    public float jiggleStrength = 3.2f;

    Rigidbody rb;
    Vector3 baseScale, basePos;
    float baseYaw;
    float phase;        // avancement du rebond
    float yaw;          // orientation horizontale courante
    float squash;       // écrasement lissé (relatif à 1)

    // Gigotement (ressort) déclenché quand il tourne brusquement
    float jiggle;        // angle actuel du gigotement
    float jiggleVel;     // vitesse du ressort
    Vector3 dirPrev = Vector3.zero;   // direction d'avant (pour détecter les virages)

    void Start()
    {
        if (visual == null)
        {
            Transform t = transform.Find("VisualSlime");
            if (t == null) t = transform.Find("VisualLowPoly");
            visual = t;
        }
        if (visual == null) return;

        baseScale = visual.localScale;
        basePos = visual.localPosition;
        baseYaw = visual.localEulerAngles.y;
        yaw = baseYaw;
        squash = 1f;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (visual == null) return;
        float dt = Time.deltaTime;

        Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero;
        float speed = new Vector2(vel.x, vel.z).magnitude;
        float move01 = Mathf.Clamp01(speed / 3f);

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
            phase += Mathf.Lerp(bounceRateWalk, bounceRateRun, move01) * dt * 2f * Mathf.PI;

        float wave = Mathf.Sin(phase);
        float target = 1f
            - squashAmount * (0.15f + 0.85f * move01) * wave              // rebond mou (comprime / étire)
            + 0.035f * Mathf.Sin(Time.time * 2.4f) * (1f - move01);       // respiration à l'arrêt

        // Le gigotement ajoute un petit pli d'écrasement en plus
        target += 0.08f * jiggle;
        squash = Mathf.Lerp(squash, target, 1f - Mathf.Exp(-14f * dt));

        // Volume conservé : quand il s'écrase, il grossit sur les côtés
        float xz = 1f / Mathf.Sqrt(Mathf.Max(0.5f, squash));
        visual.localScale = new Vector3(
            baseScale.x * xz,
            baseScale.y * squash,
            baseScale.z * xz);

        // ── HOP : il décolle du sol au sommet du rebond ───────────────
        // (sin positif = il est en l'air, sin négatif = appuyé au sol)
        float hop = Mathf.Max(0f, Mathf.Sin(phase + 0.9f)) * hopAmount * move01;
        visual.localPosition = basePos + Vector3.up * hop * (visual.parent != null ? visual.parent.lossyScale.x : 1f);

        // ── PENCHANT + ROULIS de gelée (en degrés) ───────────────────
        // Le gigotement s'ajoute au roulis → vraie sensation de gelée.
        float roll = Mathf.Sin(phase) * rollAmount * move01 + jiggle * 22f;
        float lean = leanAmount * move01;   // penche vers l'avant (il "sprinte")
        visual.localRotation = Quaternion.Euler(lean, yaw, roll);
    }
}