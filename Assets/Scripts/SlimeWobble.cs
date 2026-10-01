using UnityEngine;

/// <summary>
/// Animation "gelée" du slime — le modèle McSteeg n'a aucune animation
/// intégrée, donc on l'anime par code :
///   • rebond écrasé / étiré rythmé par la vitesse (plus il court, plus il
///     se compresse vite),
///   • il se TOURNE vers sa direction de déplacement (les yeux avancent),
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

    Rigidbody rb;
    Vector3 baseScale, basePos;
    float baseYaw;
    float phase;      // avancement du rebond
    float yaw;        // orientation horizontale courante
    float squash;     // écrasement lissé (relatif à 1)

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
            visual.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // ── Rebond : écrasé / étiré, plus vite quand il court ─────────
        if (move01 > 0.02f)
            phase += Mathf.Lerp(bounceRateWalk, bounceRateRun, move01) * dt * 2f * Mathf.PI;

        float wave = Mathf.Sin(phase);
        float target = 1f
            - squashAmount * (0.15f + 0.85f * move01) * wave              // rebond mou (comprime / étire)
            + 0.035f * Mathf.Sin(Time.time * 2.4f) * (1f - move01);       // respiration à l'arrêt
        squash = Mathf.Lerp(squash, target, 1f - Mathf.Exp(-14f * dt));

        // Volume conservé : quand il s'écrase, il grossit sur les côtés
        float xz = 1f / Mathf.Sqrt(Mathf.Max(0.5f, squash));
        visual.localScale = new Vector3(
            baseScale.x * xz,
            baseScale.y * squash,
            baseScale.z * xz);
    }
}