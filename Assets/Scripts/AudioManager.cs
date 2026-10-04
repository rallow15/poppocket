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

    // ────────────────────────────────────────────────────────────────
    //  SON DU TELEPHONE (salim 04/10 : « prend en compte le son du
    //  telephone si desactiver son jeux aussi ») : si le téléphone est
    //  muet (volume média à zéro), le jeu ne fait plus AUCUN son.
    //  Volume média vérifié toutes les 0,5 s (pas chaque son, trop cher).
    //  + réglage manuel SON OUI/NON dans les réglages (PlayerPrefs "Son").
    // ────────────────────────────────────────────────────────────────
    private static float prochaineVerification;   // Time.unscaledTime du prochain check
    private static bool telephoneMuet;            // résultat du dernier check

    /// <summary>Vrai si on a le DROIT de jouer un son maintenant.</summary>
    public static bool SonAutorise()
    {
        if (PlayerPrefs.GetInt("Son", 1) == 0) return false;   // réglage NON
        if (Time.unscaledTime >= prochaineVerification)
        {
            telephoneMuet = LireVolumeTelephone() <= 0;
            prochaineVerification = Time.unscaledTime + 0.5f;
        }
        return !telephoneMuet;
    }

    /// <summary>
    /// Volume MÉDIA du téléphone (STREAM_MUSIC sur Android). 0 = muet.
    /// Éditeur/iOS : toujours 100 (pas de lecture possible → on laisse actif).
    /// </summary>
    private static int LireVolumeTelephone()
    {
#if UNITY_EDITOR || UNITY_IOS
        return 100;
#elif UNITY_ANDROID
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var audio = context.Call<AndroidJavaObject>("getSystemService", "audio"))
            {
                return audio.Call<int>("getStreamVolume", 3);   // 3 = STREAM_MUSIC
            }
        }
        catch (System.Exception)
        {
            return 100;   // en cas de souci : mieux vaut faire du son que rien
        }
#else
        return 100;
#endif
    }

    /// <summary>
    /// Joue un clip via le pool. Si toutes les sources sont occupées,
    /// vole la plus ancienne (pool circulaire).
    /// </summary>
    public void PlayPop(AudioClip clip, float pitch = 1f, float volume = 1f)
    {
        if (clip == null) return;
        if (!SonAutorise()) return;   // téléphone muet ou réglage NON → silence

        AudioSource src = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Length; // tour de pool circulaire

        src.clip = clip;
        src.pitch = pitch;
        src.volume = volume;
        src.Play(); // Une source en cours est réutilisée : jamais plus de poolSize sons
    }
}