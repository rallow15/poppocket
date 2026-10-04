using System.Collections;
using UnityEngine;

/// <summary>
/// Système de power-ups : UN SEUL bonus à la fois, qui apparaît toutes les
/// 15 secondes (salim, 03/10) à un endroit au hasard de l'arène. Si personne
/// ne le ramasse, il disparaît quand le suivant arrive. Joueur ET bots
/// peuvent les prendre.
///
/// Designs (salim : « designe de voodoo net », tout en formes 3D posées à plat) :
///  - ÉCLAIR jaune   → le slime va 4x plus vite pendant 3 s
///  - CHAMPIGNON rouge style Mario → le slime devient GEANT x3 pendant 3 s
///    (l'ancienne AURA qui éclatait les bulles a disparu)
///  - FLOCON de neige → TOUS les autres slimes sont gelés 3 s (flash blanc)
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    /// <summary>Position du bonus actuellement posé (null = aucun). Les bots la lisent.</summary>
    public static Transform CurrentPickup { get; private set; }

    [Header("Règles (tout se règle ici)")]
    [Tooltip("Secondes entre deux power-ups")]
    public float spawnInterval = 15f;
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

        // PIÈGE DE SÉRIALISATION : la scène MainScene enregistre l'ANCIENNE
        // valeur (30) → elle écraserait le default du script. On remet 15 ICI,
        // à l'exécution (même schéma que le botForce des bots / roundDuration).
        if (spawnInterval != 15f) spawnInterval = 15f;   // salim : 1 power-up / 15 s
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

        string nom = kind == PowerUpType.Speed ? "ECLAIR de vitesse" :
                     kind == PowerUpType.Aura  ? "CHAMPIGNON geant x3" : "FLOCON de gel";
        Debug.Log("[POWER-UP] Apparu : " + nom);
    }

    private void ClearPickup()
    {
        if (pickup != null) Destroy(pickup);
        pickup = null;
        CurrentPickup = null;
    }

    /// <summary>
    /// Construit le bonus avec SON DESIGN (salim 03/10) :
    ///   Speed → éclair jaune, Aura → champignon rouge style Mario, Zap → flocon.
    /// Tout est construit en primitives de code (aucun asset du pack modifié).
    /// Collider en TRIGGER généreux : facile à attraper même en roulant vite.
    /// </summary>
    private GameObject BuildPickup(PowerUpType kind, Vector3 pos)
    {
        GameObject go = new GameObject("PowerUp_" + kind);
        go.transform.position = pos;

        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;  // traverse : on le touche, il donne le bonus
        col.radius = 1.5f;     // zone d'attrapage large (x l'épaisseur du slime)

        var pu = go.AddComponent<PowerUp>();
        pu.type = kind;

        switch (kind)
        {
            case PowerUpType.Speed: BuildLightning(go); break;
            case PowerUpType.Aura:  BuildMushroom(go);  break;
            default:                BuildSnowflake(go); break;
        }

        // Halo plat incliné : en tournant, on voit bien que c'est un bonus
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(halo.GetComponent<CapsuleCollider>()); // jamais de collision fantôme
        halo.name = "Halo";
        halo.transform.SetParent(go.transform, false);
        halo.transform.localPosition = new Vector3(0f, -0.30f, 0f);
        halo.transform.localScale = new Vector3(2.2f, 0.03f, 2.2f);
        halo.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        halo.GetComponent<MeshRenderer>().material = MakeColorMat(ColorFor(kind));
        return go;
    }

    // ────────────────────────────────────────────────────────────────
    //  DESIGNS 3D (shapes posées à PIANI comme des jetons, "voodoo net")
    // ────────────────────────────────────────────────────────────────

    /// <summary>ÉCLAIR jaune : 2 pavés de biseau assemblés à plat en zig-zag.</summary>
    private static void BuildLightning(GameObject go)
    {
        Material mat = MakeColorMat(new Color(1f, 0.82f, 0.15f)); // jaune
        BoltSlab(go.transform, mat, new Vector2(0.30f, 0.92f), new Vector2(-0.30f, 0.12f), 0.32f);
        BoltSlab(go.transform, mat, new Vector2(0.10f, 0.15f), new Vector2(-0.30f, -0.90f), 0.32f);
    }

    /// <summary>Un morceau de l'éclair : cube allongé orienté le long du segment.</summary>
    private static void BoltSlab(Transform parent, Material mat,
                                 Vector2 a, Vector2 b, float thickness)
    {
        GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(slab.GetComponent<BoxCollider>()); // jamais de collision fantôme
        slab.name = "BoltSlab";
        Transform t = slab.transform;
        t.SetParent(parent, false);

        Vector2 dir = b - a;
        Vector2 dirN = dir.normalized;
        t.localPosition = new Vector3((a.x + b.x) * 0.5f, 0.20f, (a.y + b.y) * 0.5f);
        // aligne l'axe Z local du cube sur le segment (tout à plat, lisible du dessus)
        t.localRotation = Quaternion.FromToRotation(Vector3.forward, new Vector3(dirN.x, 0f, dirN.y));
        t.localScale = new Vector3(thickness, 0.16f, dir.magnitude);
        slab.GetComponent<MeshRenderer>().material = mat;
    }

    /// <summary>FLOCON de neige : 3 branches (6 pointes) + cœur blanc brillant.</summary>
    private static void BuildSnowflake(GameObject go)
    {
        Material mat = MakeColorMat(new Color(0.55f, 0.86f, 1f)); // bleu glace
        for (int i = 0; i < 3; i++)
        {
            GameObject branche = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(branche.GetComponent<BoxCollider>()); // jamais de collision fantôme
            branche.name = "Flocon";
            branche.transform.SetParent(go.transform, false);
            branche.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            branche.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
            branche.transform.localScale = new Vector3(0.14f, 0.16f, 1.5f);
            branche.GetComponent<MeshRenderer>().material = mat;
        }

        // Cœur blanc au centre du flocon
        GameObject coeur = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(coeur.GetComponent<SphereCollider>()); // jamais de collision fantôme
        coeur.name = "Coeur";
        coeur.transform.SetParent(go.transform, false);
        coeur.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        coeur.transform.localScale = Vector3.one * 0.34f;
        coeur.GetComponent<MeshRenderer>().material = MakeColorMat(Color.white);
    }

    /// <summary>CHAMPIGNON style Mario : chapeau rouge bombé + pois blancs + pied crème.</summary>
    private static void BuildMushroom(GameObject go)
    {
        // Pied crème
        GameObject pied = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(pied.GetComponent<CapsuleCollider>()); // jamais de collision fantôme
        pied.name = "Pied";
        pied.transform.SetParent(go.transform, false);
        pied.transform.localPosition = new Vector3(0f, -0.10f, 0f);
        pied.transform.localScale = new Vector3(0.42f, 0.28f, 0.42f);
        pied.GetComponent<MeshRenderer>().material = MakeColorMat(new Color(1f, 0.93f, 0.80f));

        // Chapeau rouge bombé (demi-sphère écrasée)
        GameObject chapeau = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(chapeau.GetComponent<SphereCollider>()); // jamais de collision fantôme
        chapeau.name = "Chapeau";
        chapeau.transform.SetParent(go.transform, false);
        chapeau.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        chapeau.transform.localScale = new Vector3(1.05f, 0.62f, 1.05f);
        chapeau.GetComponent<MeshRenderer>().material = MakeColorMat(new Color(0.92f, 0.22f, 0.22f));

        // Pois blancs posés sur le chapeau
        Vector3[] pois =
        {
            new Vector3(-0.28f, 0.34f,  0.18f),
            new Vector3( 0.26f, 0.34f, -0.16f),
            new Vector3( 0.02f, 0.44f, -0.06f)
        };
        foreach (Vector3 p in pois)
        {
            GameObject poi = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(poi.GetComponent<SphereCollider>()); // jamais de collision fantôme
            poi.name = "Pois";
            poi.transform.SetParent(go.transform, false);
            poi.transform.localPosition = p;
            poi.transform.localScale = Vector3.one * 0.22f;
            poi.GetComponent<MeshRenderer>().material = MakeColorMat(Color.white);
        }
    }

    /// <summary>Couleur d'identité : éclair = jaune, champignon = rouge, flocon = bleu glace.</summary>
    private static Color ColorFor(PowerUpType kind)
    {
        switch (kind)
        {
            case PowerUpType.Speed: return new Color(1f, 0.82f, 0.15f); // jaune
            case PowerUpType.Aura:  return new Color(0.92f, 0.22f, 0.22f); // rouge champignon
            default:                return new Color(0.55f, 0.86f, 1f); // bleu glace
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
                slime.ApplyGiant(3f); // champignon : GEANT x3 (salim 03/10)
                break;

            case PowerUpType.Zap:
                // Tous les AUTRES slimes sont gelés 3 s
                foreach (var other in Object.FindObjectsByType<SlimeController>(FindObjectsSortMode.None))
                {
                    if (other == slime) continue;
                    other.ApplyStun(3f);
                }
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