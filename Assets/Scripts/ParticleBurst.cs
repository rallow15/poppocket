using UnityEngine;

/// <summary>
/// Effet visuel de "pop" : petit burst de particules colorées.
/// Se détruit automatiquement à la fin du burst (pas de fuite mémoire).
/// </summary>
public class ParticleBurst : MonoBehaviour
{
    [Tooltip("Durée avant auto-destruction du burst (en secondes)")]
    public float lifetime = 2f;

    private void Start()
    {
        // L'index -1 n'est pas utilisé : on laisse ParticleSystem jouer
        // son animation puis on détruit l'objet.
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // S'assure que le burst se joue une fois puis s'arrête
            ps.Play();

            // Destruction après la durée de vie des particules + marge
            float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(gameObject, Mathf.Max(lifetime, totalDuration));
        }
        else
        {
            // Sans ParticleSystem, auto-destruction simple
            Destroy(gameObject, lifetime);
        }
    }
}