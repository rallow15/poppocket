using UnityEngine;

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
        // 1) Nettoyage des vieilles bulles
        if (bubbleContainer != null)
        {
            foreach (Transform child in bubbleContainer)
                Destroy(child.gameObject);
        }

        // Crée le container au besoin
        if (bubbleContainer == null)
        {
            GameObject go = new GameObject("Bubbles");
            bubbleContainer = go.transform;
        }

        EnsureMaterial();

        // 2) Grille régulière façon film à bulles : rangées décalées (quinconce)
        float halfX = arenaSize.x * 0.5f;
        float halfZ = arenaSize.y * 0.5f;
        float cellX = arenaSize.x / bubblesPerSide;
        float cellZ = arenaSize.y / bubblesPerSide;
        int layerBubble = LayerMask.NameToLayer("Bubble");

        int spawned = 0;
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
                SpawnDome(new Vector3(x, domeScaleY * 0.40f, z), layerBubble);
                spawned++;
            }
        }

        Debug.Log($"[BubbleSpawner] Tapis de bulles : {spawned} mini-bulles posées au sol.");
    }

    // ────────────────────────────────────────────────────────────────
    //  CRÉATION D'UNE MINI-BULLE 3D
    // ────────────────────────────────────────────────────────────────
    private void SpawnDome(Vector3 pos, int layerBubble)
    {
        // Sphère primitive = MeshRenderer + MeshFilter déjà prêts
        GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // On remplace le collider par un rayon réglé
        Destroy(dome.GetComponent<SphereCollider>());
        SphereCollider sc = dome.AddComponent<SphereCollider>();
        sc.radius = 0.5f;
        sc.center = Vector3.zero;
        // BULLE D'EAU FLUIDE : trigger = le slime ne se cogne JAMAIS dedans.
        // Il roule à travers, la bulle éclate au contact (détecté par OnTriggerEnter).
        sc.isTrigger = true;

        MeshRenderer mrenderer = dome.GetComponent<MeshRenderer>();
        int choisi = domeMaterials != null && domeMaterials.Length > 0
            ? Random.Range(0, domeMaterials.Length) : -1;
        mrenderer.sharedMaterial = choisi >= 0 ? domeMaterials[choisi] : null;

        // PetiteVariation : chaque bulle a une taille un peu différente
        // (tapis de film à bulles RÉEL : pas un millier de clones identiques)
        float vari = Random.Range(0.85f, 1.05f);
        dome.transform.localScale = new Vector3(domeScaleXZ * vari, domeScaleY * 0.92f, domeScaleXZ * vari);
        dome.transform.position = pos;
        dome.transform.SetParent(bubbleContainer, true);

        // OMBRE DE CONTACT : un rond sombre doux posé sur le sol DERRIÈRE la
        // bulle. La bulle « touche » le sol au lieu de flotter au-dessus.
        GameObject ombre = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(ombre.GetComponent<Collider>());
        UnityEngine.MeshFilter mf = ombre.GetComponent<MeshFilter>();
        Vector3 tailleMesh = mf != null && mf.sharedMesh != null
            ? mf.sharedMesh.bounds.size : Vector3.one;
        ombre.GetComponent<MeshRenderer>().sharedMaterial =
            ArenaDesign.MatOmbreBulle();
        ombre.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // face vers le haut
        float tailleOmbre = domeScaleXZ * vari * 1.6f;
        ombre.transform.localScale = new Vector3(tailleOmbre / tailleMesh.x,
                                                 tailleOmbre / tailleMesh.y, 1f);
        ombre.transform.position = new Vector3(pos.x, 0.012f, pos.z);
        // ENFANT de la bulle (pas du container) : au pop (.Destroy(gameObject)
        // dans Bubble.cs), l'ombre part AVEC elle — jamais d'ombre orpheline.
        // En plus, elle squishe pendant l'écrasement : encore plus réaliste.
        ombre.transform.SetParent(dome.transform, true);

        if (layerBubble >= 0) dome.layer = layerBubble;

        // Le script Bubble fait le pop : son + particules + score
        Bubble bub = dome.AddComponent<Bubble>();
        if (settingsSource != null)
        {
            bub.minImpactVelocity = settingsSource.minImpactVelocity;
            bub.popSounds = settingsSource.popSounds;
            bub.popParticlePrefab = settingsSource.popParticlePrefab;
            bub.minPitch = settingsSource.minPitch;
            bub.maxPitch = settingsSource.maxPitch;
        }
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