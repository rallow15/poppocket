using UnityEngine;

/// <summary>
/// Singleton audio central : joue les sons "pop" quel que soit l'objet
/// qui les déclenche. Utilise un pool de sources pour éviter les coupures
/// quand beaucoup de bulles éclatent en même temps.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Réglages pool")]
    [Tooltip("Nombre de sources audio simultanées max (pops rapides)")]
    public int poolSize = 10;

    private AudioSource[] pool;
    private int poolIndex;

    private void Awake()
    {
        // Singleton standard : détecte les doublons
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Crée le pool d'AudioSources 2D
        pool = new AudioSource[poolSize];
        GameObject holder = new GameObject("AudioPool");
        holder.transform.SetParent(transform, false);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = holder.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D : volume constant, idéal pour l'ASMR
            src.volume = 1f;
            pool[i] = src;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Joue un clip via le pool. Si toutes les sources sont occupées,
    /// vole la plus ancienne (pool circulaire).
    /// </summary>
    public void PlayPop(AudioClip clip, float pitch = 1f, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource src = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Length; // tour de pool circulaire

        src.clip = clip;
        src.pitch = pitch;
        src.volume = volume;
        src.Play(); // Une source en cours est réutilisée : jamais plus de poolSize sons
    }
}