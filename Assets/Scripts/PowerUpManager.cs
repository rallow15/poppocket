using System.Collections;
using UnityEngine;

/// <summary>
/// Système de power-ups : UN SEUL bonus à la fois, qui apparaît toutes les
/// 30 secondes à un endroit au hasard de l'arène. Si personne ne le ramasse,
/// il disparaît quand le suivant arrive. Joueur ET bots peuvent les prendre.
///
/// Effets (chaque PowerUpManager applique l'effet sur le SlimeController) :
///  - Speed : le slime va 4x plus vite pendant 3 s
///  - Aura  : une zone autour du slime éclate les bulles toutes seules (3 s)
///  - Zap   : TOUS les autres slimes sont gelés 3 s (grand flash blanc)
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    /// <summary>Position du bonus actuellement posé (null = aucun). Les bots la lisent.</summary>
    public static Transform CurrentPickup { get; private set; }

    [Header("Règles (tout se règle ici)")]
    [Tooltip("Secondes entre deux power-ups")]
    public float spawnInterval = 30f;
    [Tooltip("Secondes avant le TOUT PREMIER power-up du round")]
    public float firstSpawnDelay = 10f;
    [Tooltip("Hauteur de flottement du bonus au-dessus du sol")]
    public float pickupHeight = 0.9f;

    private GameObject pickup;                  // le bonus posé en ce moment
    private float nextSpawnTime = float.MaxValue;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CurrentPickup = null;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        // Hors round : aucun bonus, compteur remis à zéro
        if (gm == null || gm.CurrentState != GameManager.GameState.Playing)
        {
            ClearPickup();
            nextSpawnTime = float.MaxValue;
            return;
        }

        // Début de round : programme le premier bonus
        if (nextSpawnTime == float.MaxValue)
            nextSpawnTime = Time.time + firstSpawnDelay;

        if (Time.time >= nextSpawnTime)
        {
            SpawnRandom();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  APPARITION
    // ────────────────────────────────────────────────────────────────
    private void SpawnRandom()
    {
        ClearPickup(); // le nouveau remplace l'ancien non-ramassé

        // Taille de l'arène (28 x 28 par défaut), on reste un peu dedans
        Vector2 arena = new Vector2(28f, 28f);
        var spawner = GameManager.Instance != null ? GameManager.Instance.spawner : null;
        if (spawner != null) arena = spawner.arenaSize;
        float halfX = arena.x * 0.5f * 0.85f;
        float halfZ = arena.y * 0.5f * 0.85f;

        Vector3 pos = new Vector3(Random.Range(-halfX, halfX), pickupHeight,
                                  Random.Range(-halfZ, halfZ));

        PowerUpType kind = (PowerUpType)Random.Range(0, 3); // hasard total
        pickup = BuildPickup(kind, pos);
        CurrentPickup = pickup.transform;

        string nom = kind == PowerUpType.Speed ? "VITESSE x4" :
                     kind == PowerUpType.Aura  ? "AURA d'eclatement" : "ZAP d'etourdissement";
        Debug.Log("[POWER-UP] Apparu : " + nom);
    }

    private void ClearPickup()
    {
        if (pickup != null) Destroy(pickup);
        pickup = null;
        CurrentPickup = null;
    }

    /// <summary>
    /// Construit le bonus : sphère colorée + halo plat qui tourne.
    /// Collider en TRIGGER généreux : facile à attraper même en roulant vite.
    /// </summary>
    private GameObject BuildPickup(PowerUpType kind, Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "PowerUp_" + kind;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.55f;

        SphereCollider col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;  // traverse : on le touche, il donne le bonus
        col.radius = 2.2f;     // zone d'attrapage large (x l'épaisseur du slime)

        var pu = go.AddComponent<PowerUp>();
        pu.type = kind;

        // Halo plat incliné : en tournant, on voit bien que c'est un bonus
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(halo.GetComponent<CapsuleCollider>()); // jamais de collision fantôme
        halo.name = "Halo";
        halo.transform.SetParent(go.transform, false);
        halo.transform.localScale = new Vector3(2.6f, 0.03f, 2.6f);
        halo.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        halo.GetComponent<MeshRenderer>().material = MakeColorMat(Color.white * 1.15f);

        go.GetComponent<MeshRenderer>().material = MakeColorMat(ColorFor(kind));
        return go;
    }

    private static Color ColorFor(PowerUpType kind)
    {
        switch (kind)
        {
            case PowerUpType.Speed: return new Color(1f, 0.82f, 0.15f); // jaune
            case PowerUpType.Aura:  return new Color(1f, 0.35f, 0.75f); // rose
            default:                return new Color(0.15f, 0.85f, 1f); // cyan
        }
    }

    private static Material MakeColorMat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        Color c = Color.Lerp(color * 1.4f, color, 0.7f); // couleur vive
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        mat.color = c;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.2f); // brille un peu = visible
        }
        return mat;
    }

    // ────────────────────────────────────────────────────────────────
    //  EFFETS (appelé quand un slime touche le bonus)
    // ────────────────────────────────────────────────────────────────
    public static void Apply(PowerUpType type, SlimeController slime)
    {
        if (slime == null) return;
        switch (type)
        {
            case PowerUpType.Speed:
                slime.ApplySpeedBoost(3f); // = PowerUpManager durée unique : 3 s
                break;

            case PowerUpType.Aura:
                slime.ApplyAura(3f);
                break;

            case PowerUpType.Zap:
                // Tous les AUTRES slimes sont gelés 3 s
                foreach (var other in Object.FindObjectsByType<SlimeController>(FindObjectsSortMode.None))
                    if (other != slime) other.ApplyStun(3f);
                ZapFeedback();
                break;
        }
    }

    /// <summary>Petit son de ramassage (réutilise le son des bulles qui éclatent).</summary>
    public static void PlayBonusSound()
    {
        var spawner = GameManager.Instance != null ? GameManager.Instance.spawner : null;
        Bubble src = (spawner != null && spawner.bubblePrefab != null)
                     ? spawner.bubblePrefab.GetComponent<Bubble>() : null;
        if (src == null || src.popSounds == null || src.popSounds.Length == 0) return;

        AudioClip clip = src.popSounds[Random.Range(0, src.popSounds.Length)];
        if (clip == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPop(clip, 1f + Random.Range(-0.05f, 0.05f));
        else
            BubbleSoundExtensions.PlayClipAtPoint(clip, Vector3.zero, 1f, 1f);
    }

    /// <summary>Feedback du ZAP : grand flash blanc à l'écran.</summary>
    private static void ZapFeedback()
    {
        PlayBonusSound();
        if (Instance != null) Instance.StartCoroutine(FlashRoutine());
    }

    private static IEnumerator FlashRoutine()
    {
        GameObject canvasGO = new GameObject("ZapFlash");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        GameObject child = new GameObject("Flash");
        child.transform.SetParent(canvasGO.transform, false);
        UnityEngine.UI.Image img = child.AddComponent<UnityEngine.UI.Image>();
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0.85f);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, t / 0.4f));
            yield return null;
        }
        Destroy(canvasGO); // tout disparaît proprement
    }
}