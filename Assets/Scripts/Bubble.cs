using UnityEngine;

/// <summary>
/// Une bulle du tapis posée au sol (collider STATIQUE : le slime qui roule
/// dessus déclenche la collision, pas besoin de Rigidbody sur 400 bulles).
/// Éclate quand un Slime la percute assez vite (impact >= minImpactVelocity).
/// Sur éclatement : son "pop" aléatoire + particules + score + destruction.
/// </summary>
public class Bubble : MonoBehaviour
{
    [Header("Détection d'impact")]
    [Tooltip("Vélocité minimale de collision pour éclater (évite les frottements)")]
    public float minImpactVelocity = 2.0f;

    [Header("Effets - Sons (0-2 assignés dans l'éditeur)")]
    public AudioClip[] popSounds = new AudioClip[3];

    [Header("Effets - Particules")]
    [Tooltip("Prefab de particules spawné à l'éclatement (optionnel)")]
    public GameObject popParticlePrefab;
    [Tooltip("Pitch aléatoire du son pour varier les pops")]
    public float minPitch = 0.85f;
    public float maxPitch = 1.15f;

    private bool popped = false; // garde-fou : une bulle n'éclate qu'une fois

    private void OnCollisionEnter(Collision collision)
    {
        if (popped) return;

        // Seuls les objets taggés "Slime" peuvent éclater la bulle
        if (!collision.gameObject.CompareTag("Slime")) return;

        // Vérifie la force de l'impact : un frottement doux ne compte pas
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactVelocity) return;

        Pop(collision.gameObject);
    }

    /// <summary>
    /// Éclate la bulle : son + particules + score + destruction.
    /// Public : l'AURA des power-ups peut éclater des bulles à distance
    /// (le slime passé reçoit le point).
    /// </summary>
    public void Pop(GameObject slime)
    {
        popped = true;

        // 1) Son "pop" aléatoire parmi les 3 clips
        PlayPopSound();

        // 2) Petite explosion de particules
        if (popParticlePrefab != null)
        {
            Instantiate(popParticlePrefab, transform.position, Quaternion.identity);
        }

        // 3) +1 au score du slime responsable
        SlimeController sc = slime.GetComponentInParent<SlimeController>();
        if (sc != null && GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(sc, 1);
        }

        // 4) La bulle disparaît
        Destroy(gameObject);
    }

    /// <summary>Joue un son pop via un AudioManager central (sinon via une source locale).</summary>
    private void PlayPopSound()
    {
        if (popSounds == null || popSounds.Length == 0) return;

        AudioClip clip = popSounds[Random.Range(0, popSounds.Length)];
        if (clip == null) return;

        // Utilise l'AudioManager global s'il existe (meilleur pooling, pas coupé à la destruction)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPop(clip, Random.Range(minPitch, maxPitch));
        }
        else
        {
            // Fallback : crée un AudioSource temporaire dans la scène
            BubbleSoundExtensions.PlayClipAtPoint(clip, transform.position, 1f,
                                        Random.Range(minPitch, maxPitch));
        }
    }
}

/// <summary>
/// Extension : Unity ne propose pas de PlayClipAtPoint avec pitch, on l'ajoute.
/// </summary>
public static class BubbleSoundExtensions
{
    public static AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position,
                                              float volume, float pitch)
    {
        GameObject go = new GameObject("PopSound");
        go.transform.position = position;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 0f; // 2D : toujours audible, parfait pour l'ASMR
        src.Play();
        Object.Destroy(go, clip.length / pitch + 0.1f);
        return src;
    }
}