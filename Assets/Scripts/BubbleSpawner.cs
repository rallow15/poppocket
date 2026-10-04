using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Construit le "tapis de bulles" façon Roblox : une grille dense de petites
/// bulles 3D en relief qui couvre tout le sol de l'arène.
/// On roule dessus → elles éclatent (script Bubble) → +1 point.
/// Le tapis entier est reconstruit à chaque round.
/// </summary>
public class BubbleSpawner : MonoBehaviour
{
    [Header("Tapis de bulles")]
    [Tooltip("Nombre de bulles par côté de l'arène (20 = 400 bulles)")]
    public int bubblesPerSide = 20;

    [Tooltip("Prefab de la bulle (sert UNIQUEMENT de source de sons/particules)")]
    public GameObject bubblePrefab;

    [Tooltip("Surface de l'arène en mètres (X = largeur, Z = profondeur)")]
    public Vector2 arenaSize = new Vector2(28f, 28f);

    [Tooltip("Largeur d'une bulle (m)")]
    public float domeScaleXZ = 0.95f;

    [Tooltip("Hauteur d'une bulle (m)")]
    public float domeScaleY = 0.55f;

    [Tooltip("Parent de tous les objets bulle (plus propre dans la hiérarchie)")]
    public Transform bubbleContainer;

    // Matériaux translucides partagés par toutes les bulles (eau)
    private Material[] domeMaterials;
    // Réglages de la bulle d'origine (sons, particules, pitch)
    private Bubble settingsSource;

    private void Awake()
    {
        // Le préfab d'origine ne sert qu'à copier sons + particules + pitch
        // sur chaque nouvelle mini-bulle.
        if (bubblePrefab != null)
            settingsSource = bubblePrefab.GetComponent<Bubble>();
    }

    /// <summary>GameManager appelle cette méthode à chaque round (nom conservé).</summary>
    public void SpawnBubblesForRound()
    {
        // FLUIDITÉ (salim 05/10) : on ne détruit PLUS le tapis puis on le
        // recrée (400 Destroy + 400 CreatePrimitive en une frame = pic de
        // mémoire et l'image accroche au début de chaque round, surtout sur
        // téléphone). Les bulles vivantes sont REMISES À NEUF et REPLACÉES
        // dans la grille (pool maison) : on ne crée que la différence.
        if (bubbleContainer == null)
        {
            GameObject go = new GameObject("Bubbles");
            bubbleContainer = go.transform;
        }

        List<Bubble> recyclables = new List<Bubble>();
        foreach (Bubble b in bubbleContainer.GetComponentsInChildren<Bubble>(true))
        {
            b.RemiseAZero();
            recyclables.Add(b);
        }

        EnsureMaterial();

        // Grille régulière façon film à bulles : rangées décalées (quinconce)
        float halfX = arenaSize.x * 0.5f;
        float halfZ = arenaSize.y * 0.5f;
        float cellX = arenaSize.x / bubblesPerSide;
        float cellZ = arenaSize.y / bubblesPerSide;
        int layerBubble = LayerMask.NameToLayer("Bubble");

        int spawned = 0;
        int suivante = 0;   // prochaine bulle du pool à reprendre
        for (int row = 0; row < bubblesPerSide; row++)
        {
            // Quinconce : une rangée sur deux est décalée d'une demi-cellule
            float rowOffset = (row % 2 == 0) ? 0f : cellX * 0.5f;

            for (int col = 0; col < bubblesPerSide; col++)
            {
                float x = -halfX + cellX * col + cellX * 0.5f + rowOffset;
                float z = -halfZ + cellZ * row + cellZ * 0.5f;

                // Déborde de l'arène sur les rangées décalées ? on recentre dans le mur
                if (x > halfX - cellX * 0.45f) x = halfX - cellX * 0.45f;

                // SINK (salim 03/10 : « les bulles sont pas intégrées, on
                // aurait dit un truc posé ») : la bulle est posée un peu
                // PLUS BAS que son centre exact → le bas rentre dans le
                // sol, elle vit DANS le tapis au lieu de flotter.
                Bubble aRecycler = suivante < recyclables.Count
                    ? recyclables[suivante++] : null;
                SpawnDome(new Vector3(x, domeScaleY * 0.40f, z), layerBubble, aRecycler);
                spawned++;
            }
        }

        // Vieilles bulles en trop (grille rétrécie) : on les retire d'un coup
        for (; suivante < recyclables.Count; suivante++)
            Destroy(recyclables[suivante].gameObject);

        Debug.Log($"[BubbleSpawner] Tapis de bulles : {spawned} mini-bulles posées au sol " +
                  $"({recyclables.Count} recyclées).");
    }

    // ────────────────────────────────────────────────────────────────
    //  CRÉATION D'UNE MINI-BULLE 3D
    // ────────────────────────────────────────────────────────────────
    private void SpawnDome(Vector3 pos, int layerBubble, Bubble aRecycler = null)
    {
        // PetiteVariation : chaque bulle a une taille un peu différente
        // (tapis de film à bulles RÉEL : pas un millier de clones identiques)
        float vari = Random.Range(0.85f, 1.05f);

        GameObject dome;
        MeshRenderer mrenderer;
        Bubble bub;

        if (aRecycler != null)
        {
            // POOL (salim 05/10) : l'objet existe déjà — collider, script
            // Bubble, ombre-enfant… tout est conservé, on ne déplace que lui.
            dome = aRecycler.gameObject;
            mrenderer = dome.GetComponent<MeshRenderer>();
            bub = aRecycler;

            // CLEAN FPS (salim 04/10) : les anciennes bulles créées avec une
            // ombre-enfant (le "Quad") la perdent définitivement au recyclage.
            for (int e = dome.transform.childCount - 1; e >= 0; e--)
            {
                Transform enfant = dome.transform.GetChild(e);
                if (enfant.name == "Quad")
                    Destroy(enfant.gameObject);
            }
        }
        else
        {
            // Sphère primitive = MeshRenderer + MeshFilter déjà prêts
            dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            // On remplace le collider par un rayon réglé
            Destroy(dome.GetComponent<SphereCollider>());
            SphereCollider sc = dome.AddComponent<SphereCollider>();
            sc.radius = 0.5f;
            sc.center = Vector3.zero;
            // BULLE D'EAU FLUIDE : trigger = le slime ne se cogne JAMAIS dedans.
            // Il roule à travers, la bulle éclate au contact (détecté par OnTriggerEnter).
            sc.isTrigger = true;

            mrenderer = dome.GetComponent<MeshRenderer>();

            // FLUIDITÉ FPS, façon Voodoo (salim 04/10) : PLUS d'ombre par
            // bulle. 400 quads d'ombre = 400 draw calls en plus À CHAQUE
            // image ; sur téléphone c'est l'ennemi n°1 du framerate. La
            // bulle garde son relief 3D, c'est le sol qui fait le travail.
            // (les ombres restantes des vieilles bulles sont purgées au
            // recyclage, voir plus haut).

            // Le script Bubble fait le pop : son + particules + score
            bub = dome.AddComponent<Bubble>();
            if (settingsSource != null)
            {
                bub.minImpactVelocity = settingsSource.minImpactVelocity;
                bub.popSounds = settingsSource.popSounds;
                bub.popParticlePrefab = settingsSource.popParticlePrefab;
                bub.minPitch = settingsSource.minPitch;
                bub.maxPitch = settingsSource.maxPitch;
            }

            if (layerBubble >= 0) dome.layer = layerBubble;
        }

        int choisi = domeMaterials != null && domeMaterials.Length > 0
            ? Random.Range(0, domeMaterials.Length) : -1;
        mrenderer.sharedMaterial = choisi >= 0 ? domeMaterials[choisi] : null;

        // FLUIDITÉ FPS : la bulle ne projette PLUS d'ombre temps réel (le
        // shader transparent projette une ombre pleine = 400 objets re-
        // dessinés pour le sol d'ombre chaque image). Elle REÇOIT toujours
        // l'ombre de la lumière : le rendu reste joli, le GPU respire.
        mrenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        dome.transform.localScale = new Vector3(domeScaleXZ * vari, domeScaleY * 0.92f, domeScaleXZ * vari);
        dome.transform.position = pos;
        dome.transform.SetParent(bubbleContainer, true);
        bub.PoseEchelleDeBase();   // le squish/regonfle repart de CETTE taille

        // salim 04/10 : les gouttes de l'éclat prennent la couleur de
        // CETTE bulle (chacune des 3 teintes d'eau passe ses gouttes)
        if (mrenderer.sharedMaterial != null)
            bub.teinteEau = mrenderer.sharedMaterial.color;
    }

    /// <summary>
    /// Matériaux « BULLES D'EAU » des bulles (créés une fois) :
    /// gouttes transparentes et très brillantes, 3 teintes d'eau
    /// (eau claire / eau turquoise / eau bleue). Mode TRANSPARENT du
    /// shader Standard → on voit un peu à travers, comme une vraie bulle.
    /// Ce projet est en BUILT-IN : shader Standard, jamais URP/Lit (rose).
    /// </summary>
    private void EnsureMaterial()
    {
        if (domeMaterials != null && domeMaterials.Length > 0) return;

        Shader shader = Shader.Find("Standard");
        if (shader == null) return;   // pas de fallback opaque : on veut du translucide

        // salim 03/10 : « le sol et les bulles font qu'un » → les 3 teintes
        // sont resserrées dans la MÊME famille azzurro que le fond de cuve
        // (ArenaDesign.MatCuve) : plus de bulle "différente", les bulles se
        // fondent dans le sol et le tout vit ensemble.
        Color[] teintes = new Color[]
        {
            new Color(0.62f, 0.90f, 0.98f, 0.45f),   // eau claire du centre
            new Color(0.58f, 0.86f, 0.96f, 0.42f),   // eau du milieu
            new Color(0.55f, 0.84f, 0.98f, 0.47f),   // eau du bord
        };

        domeMaterials = new Material[teintes.Length];
        for (int i = 0; i < teintes.Length; i++)
        {
            var mat = new Material(shader);
            mat.color = teintes[i];

            // Passage en mode TRANSPARENT (on voit à travers la bulle)
            mat.SetFloat("_Mode", 3f);                  // 3 = Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            // Surface mouillée : très lisse, gros reflets
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.96f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);
            if (mat.HasProperty("_SmoothnessSource"))
                mat.SetFloat("_SmoothnessSource", 0f);
            mat.name = "MatBulleEau" + i;
            domeMaterials[i] = mat;
        }
    }

    /// <summary>Nombre de bulles vivantes (utilisé par GameManager si besoin).</summary>
    public int CountBubbles()
    {
        return bubbleContainer != null ? bubbleContainer.childCount : 0;
    }
}