using UnityEngine;

/// <summary>
/// Petit animal décoratif qui se balade tranquillement autour de l'arène
/// (il ne rentre JAMAIS dedans : il reste sur un anneau autour du terrain).
/// Il marche, tourne doucement vers sa destination et fait un petit "trotte".
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

    private Vector3 targetPos;
    private float baseY;

    private void Start()
    {
        baseY = transform.position.y;
        Pick();
    }

    private void Pick()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float r = Random.Range(ringRadius.x, ringRadius.y);
        targetPos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
    }

    private void Update()
    {
        Vector3 to = targetPos - transform.position;
        to.y = 0f;

        if (to.sqrMagnitude < 0.4f)
        {
            Pick();   // arrivé : nouvelle destination
            return;
        }

        Vector3 dir = to.normalized;
        transform.position += dir * (speed * Time.deltaTime);

        // Tourne doucement vers la direction de marche
        Quaternion wanted = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, wanted,
                                              Time.deltaTime * 4f);

        // Petit trottement (le corps monte et descend)
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * bobSpeed + p.x) * bobAmplitude;
        transform.position = p;
    }
}