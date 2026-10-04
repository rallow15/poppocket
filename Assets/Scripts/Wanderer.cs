using UnityEngine;

/// <summary>
/// Petit animal décoratif qui se balade tranquillement autour de l'arène.
/// IL NE RENTRE JAMAIS DANS L'ARÈNE : il se déplace EN CONTORNANT le terrain
/// (mouvement "en arc" sur son anneau, jamais en ligne droite à travers).
/// Un Animator est optionnel : s'il en a un, on lance l'animation de marche.
/// Aucun collider : décor pur, ça peut ralentir personne.
/// </summary>
public class Wanderer : MonoBehaviour
{
    [Header("Balade (anneau autour de l'arène)")]
    [Tooltip("Rayon mini / maxi de l'anneau où l'animal se promène")]
    public Vector2 ringRadius = new Vector2(16f, 22f);

    [Header("Allure")]
    [Tooltip("Vitesse de marche (m/s)")]
    public float speed = 1.1f;

    [Tooltip("Amplitude du petit trottement (du au galop)")]
    public float bobAmplitude = 0.035f;

    [Tooltip("Vitesse du trottement")]
    public float bobSpeed = 7f;

    // Position de l'animal sur l'anneau en "polaire" :
    // angle (radians) + rayon. On bouge l'angle OU le rayon, jamais à travers.
    private float angle;
    private float radius;
    private float targetAngle;
    private float targetRadius;
    private float baseY;
    private Vector3 lastPos;
    private Animator animator;

    private void Start()
    {
        baseY = transform.position.y;

        // FIX ARÈNE (salim 04/10 : « les animaux rentrent dans l'arène ») :
        // l'arène est un CARRÉ — ses COINS sont plus loin du centre que son
        // demi-côté. L'anneau réglé juste au-delà du demi-côté (16 m) passait
        // par-DESSUS les coins (à 19,8 m) : près d'un coin, l'animal marchait
        // À L'INTÉRIEUR de l'arène. On repousse donc l'anneau au-delà de la
        // DIAGONALE du carré, à chaque lancement (la scène peut garder l'ancien
        // rayon sérialisé, on le corrige ici à l'exécution = règle du projet).
        var spawner = FindFirstObjectByType<BubbleSpawner>();
        float half = spawner != null ? spawner.arenaSize.x * 0.5f : 14f;
        float coin = half * 1.4142f;                       // demi-diagonale du carré
        float rMin = Mathf.Max(ringRadius.x, coin + 1.2f);
        float rMax = Mathf.Max(ringRadius.y, rMin + 2f);
        ringRadius = new Vector2(rMin, rMax);

        // Départ sur l'anneau (sécurité : on ne peut JAMAIS être dedans)
        angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        radius = Random.Range(ringRadius.x, ringRadius.y);
        PlaceOnRing(angle, radius);

        // S'il a un Animator avec une animation de marche : on la lance
        animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.Play(0, 0, Random.Range(0f, 1f));

        Pick();
    }

    private void Pick()
    {
        // Nouvelle destination : un autre point du MÊME anneau
        targetAngle = angle + Random.Range(30f, 210f) * Mathf.Deg2Rad * (Random.value < 0.5f ? -1f : 1f);
        targetRadius = Random.Range(ringRadius.x, ringRadius.y);
    }

    private void Update()
    {
        // ── Angle : on tourne le long de l'arc (jamais en ligne droite à travers)
        float angDiff = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float angSpeed = speed / Mathf.Max(radius, 1f);   // rad/s pour la vitesse donnée
        float angStep = Mathf.Clamp(angDiff, -angSpeed * Time.deltaTime, angSpeed * Time.deltaTime);
        angle += angStep;

        // ── Rayon : on glisse doucement vers le rayon voulu (toujours hors arène)
        float rDiff = targetRadius - radius;
        float maxRStep = speed * Time.deltaTime;
        radius += Mathf.Clamp(rDiff, -maxRStep, maxRStep);

        PlaceOnRing(angle, radius);

        // ── Regarde où il va (sens du déplacement réel)
        Vector3 dir = transform.position - lastPos;
        if (dir.sqrMagnitude > 0.0000001f)
        {
            dir.y = 0f;
            Quaternion wanted = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wanted, Time.deltaTime * 5f);
        }

        // ── Petit trottement (le corps monte et descend)
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * bobSpeed + p.x) * bobAmplitude;
        transform.position = p;

        // ── Destination atteinte (angle ET rayon) : on choisit la suivante
        if (Mathf.Abs(angDiff) < 0.03f && Mathf.Abs(rDiff) < 0.05f) Pick();

        lastPos = transform.position;
    }

    /// <summary>Place l'animal exactement sur son anneau, à l'angle/rayon donnés.</summary>
    private void PlaceOnRing(float ang, float r)
    {
        transform.position = new Vector3(Mathf.Cos(ang) * r, transform.position.y,
                                         Mathf.Sin(ang) * r);
    }
}