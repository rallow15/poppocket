using UnityEngine;
using System.Collections;

/// <summary>
/// Une bulle du tapis posée au sol (collider STATIQUE : le slime qui roule
/// dessus déclenche la collision, pas besoin de Rigidbody sur 400 bulles).
///
/// Le côté ASMR "satisfaisant" :
///   • le slime la touche → la bulle S'ÉCRASE un instant (squish),
///   • s'il arrive assez vite (impact >= minImpactVelocity) → POP !
///     (son + particules + score + destruction),
///   • s'il roulait doucement → elle reprend doucement sa forme,
///     comme une vraie bulle d'eau qu'on n'a pas percée.
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

    [Header("Feel ASMR - écrasement avant l'éclatement")]
    [Tooltip("Durée du squish (la bulle s'aplatit) avant le POP")]
    public float squashTime = 0.08f;
    [Tooltip("Hauteur gardée pendant le squish (0.5 = moitié)")]
    public float squashHeight = 0.5f;
    [Tooltip("Largeur gagnée pendant le squish (1.15 = +15%)")]
    public float squashWidth = 1.15f;

    private bool popped = false;   // garde-fou : une bulle n'éclate qu'une fois
    private bool squashing = false;
    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;   // l'échelle posée par le spawner
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (popped || squashing) return;

        // Seuls les objets taggés "Slime" peuvent écraser / éclater la bulle
        if (!collision.gameObject.CompareTag("Slime")) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactVelocity * 0.35f) return;   // contact négligeable

        squashing = true;
        StartCoroutine(SquashThenPop(collision.gameObject, impactSpeed));
    }

    /// <summary>
    /// Le squish AVANT le pop : la bulle s'aplatit sous le slime, puis
    ///   • impact fort          → POP (son + particules + score + destruction),
    ///   • contact doux         → elle se regonfle, aucun point.
    /// C'est ce petit temps d'écrasement qui rend l'éclatement satisfaisant.
    /// </summary>
    private IEnumerator SquashThenPop(GameObject slime, float impactSpeed)
    {
        // ── 1. On écrase la bulle (squish) ─────────────────────────
        Vector3 squashed = new Vector3(
            baseScale.x * squashWidth,
            baseScale.y * squashHeight,
            baseScale.z * squashWidth);

        float t = 0f;
        while (t < squashTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(baseScale, squashed, t / squashTime);
            yield return null;
        }

        // ── 2. Assez forte → elle éclate ───────────────────────────
        if (impactSpeed >= minImpactVelocity)
        {
            Pop(slime);
            yield break;   // l'objet est détruit, la coroutine s'arrête ici
        }

        // ── 3. Trop douce → elle reprend sa forme doucement ────────
        t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squashed, baseScale, t / 0.28f);
            yield return null;
        }
        transform.localScale = baseScale;
        squashing = false;
    }

    /// <summary>
    /// Éclate la bulle : son + particules + score + destruction.
    /// Public : l'AURA des power-ups peut éclater des bulles à distance
    /// (le slime passé reçoit le point) — et l'aura éclate SANS squish,
    /// c'est voulu : ça fait une rafale, pas un toucher.
    /// </summary>
    public void Pop(GameObject slime)
    {
        popped = true;

        // 1) Son "pop" aléatoire parmi les 3 clips
        PlayPopSound();

        // 2) Éclaboussure d'eau (participe teinté bleu eau)
        if (popParticlePrefab != null)
        {
            var burst = Instantiate(popParticlePrefab, transform.position, Quaternion.identity);
            MakeSplashWater(burst);
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

    /// <summary>
    /// Teinte les particules de l'explosion en COULEUR D'EAU (bleu clair),
    /// pour une vraie éclaboussure de goutte qui éclate.
    /// </summary>
    private static void MakeSplashWater(GameObject burst)
    {
        Color eau = new Color(0.70f, 0.92f, 1f, 0.85f);   // goutte d'eau claire

        var ps = burst.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(eau);
        }
        foreach (var childPs in burst.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (childPs == ps) continue;
            var m = childPs.main;
            m.startColor = new ParticleSystem.MinMaxGradient(eau);
        }
        foreach (var pr in burst.GetComponentsInChildren<ParticleSystemRenderer>(true))
            pr.material.color = eau;
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