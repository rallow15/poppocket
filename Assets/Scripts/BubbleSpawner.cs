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

    // Matériau blanc brillant partagé par toutes les bulles
    private Material domeMaterial;
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

                SpawnDome(new Vector3(x, domeScaleY * 0.5f, z), layerBubble);
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

        MeshRenderer mrenderer = dome.GetComponent<MeshRenderer>();
        mrenderer.sharedMaterial = domeMaterial;

        dome.transform.localScale = new Vector3(domeScaleXZ, domeScaleY, domeScaleXZ);
        dome.transform.position = pos;
        dome.transform.SetParent(bubbleContainer, true);

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
    }

    /// <summary>Matériau brillant blanc-lumineux des bulles (créé une fois).</summary>
    private void EnsureMaterial()
    {
        if (domeMaterial != null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");

        domeMaterial = new Material(shader);
        Color white = new Color(0.94f, 0.97f, 1.00f, 1f);
        if (domeMaterial.HasProperty("_BaseColor")) domeMaterial.SetColor("_BaseColor", white);
        domeMaterial.color = white;
        if (domeMaterial.HasProperty("_Smoothness")) domeMaterial.SetFloat("_Smoothness", 0.9f);
        if (domeMaterial.HasProperty("_SmoothnessSource"))
            domeMaterial.SetFloat("_SmoothnessSource", 1f); // brillant depuis l'albedo

        domeMaterial.name = "MatBubbleDomes";
    }

    /// <summary>Nombre de bulles vivantes (utilisé par GameManager si besoin).</summary>
    public int CountBubbles()
    {
        return bubbleContainer != null ? bubbleContainer.childCount : 0;
    }
}