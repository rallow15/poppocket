using UnityEngine;

/// <summary>
/// Animation "gelée" du slime — le modèle McSteeg n'a aucune animation
/// intégrée, donc on l'anime par code.
/// DÉMANDES SALIM (2026-10-02) : hop, balancement et penchement DÉSACTIVÉS
/// (rien de "mécanique") → par contre le CORPS se déforme comme un vrai gel :
///   • le mesh ondule (une onde parcourt le corps de la queue vers la tête),
///   • le corps s'ÉTIRE dans la direction du mouvement, l'arrière traîne,
///   • il se relâche AVEC DU RETARD quand on s'arrête (jello),
///   • le bas s'aplatit légèrement sous le poids quand il accélère.
/// Ce qui RESTE aussi : écrasement/étirement d'ensemble, respiration à
/// l'arrêt, il se TOURNE vers sa direction (les yeux avancent).
/// Tout s'applique sur l'enfant visuel (VisualSlime) : le gameplay
/// (physique, squash du corps par SlimeController) reste inchangé.
/// Robustesse : si le Rigidbody est absent, on déduit la vitesse de la
/// position (delta entre 2 frames) → l'animation marche dans tous les cas.
/// </summary>
[DefaultExecutionOrder(210)]
public class SlimeWobble : MonoBehaviour
{
    [Tooltip("DOIT rester FALSE sur ce projet : ce script se battait avec les animations du pack Symphonie (jitter au déplacement) et redéformait le mesh. Il est désactivé par défaut pour ne jamais pouvoir le refaire.")]
    public bool actif = false;

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

    [Header("Effets activés / désactivés (salim 2026-10-02)")]
    [Tooltip("Hauteur du hop : le slime décolle du sol en courant (0 = PAS de sautillement, demandé par salim)")]
    public float hopAmount = 0f;
    [Tooltip("BALANCEMENT gauche/droite en degrés (0 = désactivé, demandé par salim)")]
    public float rollAmount = 0f;
    [Tooltip("PENCHEMENT vers l'avant en degrés (0 = désactivé, demandé par salim)")]
    public float leanAmount = 0f;
    [Tooltip("Gigotement quand il tourne brusquement (force du ressort)")]
    public float jiggleStrength = 4.5f;

    [Header("Corps de gelée — déformation réelle du mesh (pas mécanique)")]
    [Tooltip("Amplitude de l'onde qui parcourt le corps (0 = pas d'onde)")]
    public float gelOnde = 0.030f;
    [Tooltip("De combien le corps s'étire dans sa direction quand il avance (0 = raide)")]
    public float gelEtirement = 0.25f;
    [Tooltip("Souplesse : plus GRAND = le gel suit vite (10 = ferme, 4 = très tout-mou)")]
    public float gelSouplesse = 9f;

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

    // ── Corps de gelée : état de la déformation du mesh ────────────────
    Mesh meshInstance;      // COPIE du mesh (l'asset FBX n'est jamais touché)
    Vector3[] restVerts;    // positions de repos, relatives au centre des bounds
    Vector3 center;         // centre des bounds du mesh d'origine
    Vector3 dirMesh = Vector3.forward;  // direction de déplacement, espace mesh (lissée)
    float stretch;          // étirement courant (suit avec du retard = jello)
    float waveT;            // phase de l'onde qui voyage le long du corps

    bool warnedOnce;

    void Start()
    {
        // DÉSACTIVÉ : le pack Symphonie anime le slime lui-même (Animator
        // ZSpeed/XSpeed piloté par SlimeAnimDriver). Si on laissait ce script
        // tourner, il réécrivait localScale/localPosition/localRotation et le
        // mesh CHAQUE frame → jitter au déplacement, surtout sur les bots.
        // On coupe le composant : même si Unity le garde dans un préfab,
        // il ne fait plus rien tant que « actif » n'est pas coché dans
        // l'Inspector.
        if (!actif)
        {
            enabled = false;
            return;
        }

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

        // ── Copie du mesh pour le déformer en gelée ─────────────────────
        // On prend le MeshFilter avec le PLUS de sommets = le corps (pas les yeux).
        // L'asset FBX est partagé et en lecture seule → on en fait une copie.
        MeshFilter best = null;
        foreach (MeshFilter mf in visual.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            if (best == null || mf.sharedMesh.vertexCount > best.sharedMesh.vertexCount)
                best = mf;
        }
        if (best != null && best.sharedMesh.vertexCount <= 12000) // garde perf mobile
        {
            Mesh srcMesh = best.sharedMesh;
            meshInstance = Instantiate(srcMesh);
            meshInstance.name = srcMesh.name + "_Gel";
            meshInstance.MarkDynamic();
            best.mesh = meshInstance;

            center = srcMesh.bounds.center;
            restVerts = srcMesh.vertices;
            for (int i = 0; i < restVerts.Length; i++)
                restVerts[i] -= center;
            Debug.Log("[SLIME-GEL] " + name + " : déformation du mesh " + srcMesh.name +
                      " (" + restVerts.Length + " sommets)");
        }
        else
        {
            Debug.LogWarning("[SLIME-WOBBLE] " + name + " : pas de mesh déformable" +
                             (best != null ? " (trop de sommets : " + best.sharedMesh.vertexCount + ")" : ""));
        }

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

        // ── Rotation : UNIQUEMENT la direction (salim : pas de penchement /
        //    balancement → rollAmount et leanAmount sont à 0, et le
        //    roll ne reçoit plus le gigotement des virages) ────────────
        float roll = Mathf.Sin(phase) * rollAmount * move01;
        float lean = leanAmount * move01;
        visual.localRotation = Quaternion.Euler(lean, yaw, roll);

        // ── CORPS DE GELÉE : déformation réelle du mesh ────────────────
        if (meshInstance != null && restVerts != null)
        {
            // 1) La direction est LISSÉE : un vrai gel ne change pas de
            //    forme instantanément, il suit avec du retard.
            if (speed > 0.4f)
            {
                Vector3 dirWanted = visual.InverseTransformDirection(
                    new Vector3(vel.x, 0f, vel.z).normalized).normalized;
                dirMesh = Vector3.Slerp(dirMesh, dirWanted,
                                        1f - Mathf.Exp(-6f * dt)).normalized;
            }

            // 2) L'étirement suit avec du retard → quand on s'arrête,
            //    le corps reste étiré un instant puis se relâche (jello).
            stretch = Mathf.Lerp(stretch, gelEtirement * move01,
                                 1f - Mathf.Exp(-gelSouplesse * dt));

            // 3) L'onde voyage le long du corps, plus vite quand il va vite
            waveT += dt * (5f + speed * 2.2f);

            float amp = gelOnde * move01;                        // l'onde n'existe qu'en mouvement
            Vector3 dir = dirMesh;
            Vector3 side = new Vector3(dir.z, 0f, -dir.x);       // perpendiculaire à plat du sol

            Vector3[] verts = meshInstance.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 p0 = restVerts[i];
                // distance le long de la direction de déplacement
                float along = p0.x * dir.x + p0.y * dir.y + p0.z * dir.z;

                float ph = along * 2.3f - waveT;
                // a) ÉTIREMENT : l'avant du corps est poussé vers l'avant,
                //    l'arrière traîne derrière → forme de goutte en mouvement
                Vector3 disp = dir * (along * stretch);
                // b) ONDE qui parcourt le corps de la queue vers la tête
                disp += dir * (Mathf.Sin(ph) * amp);
                // c) ONDULATION latérale douce (déphasée, asymétrique → organique)
                disp += side * (Mathf.Sin(ph * 0.6f + p0.y * 2.5f + along * 0.4f) * amp * 0.7f);
                // d) LE BAS s'aplatit légèrement sous le poids en accélération
                disp.y -= Mathf.Max(0f, -p0.y) * 0.22f * stretch;

                verts[i] = center + p0 + disp;
            }
            meshInstance.vertices = verts;
            meshInstance.RecalculateBounds();
        }
    }
}