#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

/// <summary>
/// ReStyle complet "façon Roblox" (demandé par l'utilisateur) :
///  - sol de l'arène bleu clair et brillant
///  - grande pelouse verte autour + petits arbres cartoon
///  - murs invisibles aux 4 bords (les slimes ne tombent plus)
///  - ciel bleu lumineux + soleil doux
///  - le slime du joueur grossit (2m), les bots un peu moins (1,5m)
///  - le BubbleSpawner (tapis de bulles) est calé sur la taille de l'arène
/// S'exécute tout seul au démarrage de l'éditeur. Idempotent.
/// </summary>
[InitializeOnLoad]
public static class RobloxStyleRebuild
{
    static RobloxStyleRebuild()
    {
        EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        var arena = GameObject.Find("Arena");
        if (arena == null) return; // pas dans la bonne scène

        bool already = GameObject.Find("GrassPlane") != null;
        if (already)
        {
            Debug.Log("[ROBLOX-STYLE] Déco Roblox déjà appliquée ✔");
            return;
        }

        // Taille réelle de l'arène (renderer bounds, en coordonnées monde)
        var arenaRenderer = arena.GetComponent<MeshRenderer>();
        if (arenaRenderer == null) return;
        Bounds bounds = arenaRenderer.bounds;

        // ── 1) SOL BLEU CLAIR BRILLANT ────────────────────────────
        arenaRenderer.sharedMaterial = MakeMat("MatFloorPlayland",
            new Color(0.70f, 0.86f, 0.99f), 0.75f);

        // ── 2) PELOUSE AUTOUR ────────────────────────────────────
        var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grass.name = "GrassPlane";
        grass.transform.position = new Vector3(0f, -0.06f, 0f);
        grass.transform.localScale = Vector3.one * 7f;   // 70 m de côté
        grass.GetComponent<MeshRenderer>().sharedMaterial =
            MakeMat("MatGrass", new Color(0.48f, 0.80f, 0.42f), 0.2f);

        // ── 3) ARBRES CARTOON AUTOUR ─────────────────────────────
        var deco = new GameObject("DecoRoot");
        Vector3[] treeSpots =
        {
            new Vector3( 20f, 0, -10f), new Vector3( 15f, 0,  16f),
            new Vector3(-18f, 0,  12f), new Vector3(-20f, 0, -14f),
            new Vector3( 10f, 0,  22f), new Vector3(-10f, 0,  24f),
            new Vector3( 24f, 0,   6f), new Vector3(-24f, 0,  -2f),
        };
        for (int i = 0; i < treeSpots.Length; i++)
            MakeTree(deco.transform, treeSpots[i], 2.2f + (i % 3) * 0.5f);

        // ── 4) MURS INVISIBLES AUX 4 BORDS ───────────────────────
        if (GameObject.Find("WallN") == null)
        {
            float hx = bounds.extents.x, hz = bounds.extents.z;
            MakeWall("WallN", new Vector3(0f, 2f,  hz + 0.5f), new Vector3(bounds.size.x + 2f, 4f, 1f));
            MakeWall("WallS", new Vector3(0f, 2f, -hz - 0.5f), new Vector3(bounds.size.x + 2f, 4f, 1f));
            MakeWall("WallE", new Vector3( hx + 0.5f, 2f, 0f), new Vector3(1f, 4f, bounds.size.z + 2f));
            MakeWall("WallW", new Vector3(-hx - 0.5f, 2f, 0f), new Vector3(1f, 4f, bounds.size.z + 2f));
        }

        // ── 5) CIEL BLEU + LUMIÈRE DOUCE ─────────────────────────
        SetupSkyAndSun();

        // ── 6) SLIMES BIEN GROS ──────────────────────────────────
        ScalePrefab("Assets/Prefabs/PlayerSlimePrefab.prefab", 2.0f);
        ScalePrefab("Assets/Prefabs/BotSlimePrefab.prefab", 1.5f);

        // ── 7) BUBBLE SPAWNER CALÉ SUR L'ARÈNE ───────────────────
        var spawners = Object.FindObjectsByType<BubbleSpawner>(FindObjectsSortMode.None);
        foreach (var sp in spawners)
        {
            sp.arenaSize = new Vector2(bounds.size.x, bounds.size.z);
            sp.bubblesPerSide = Mathf.Max(16, Mathf.RoundToInt(bounds.size.x / 1.4f));
            EditorUtility.SetDirty(sp);
        }

        // ── 8) CAMÉRA EN VUE PERSPECTIVE ─────────────────────────
        var mainCam = GameObject.Find("MainCamera");
        if (mainCam != null)
        {
            var cam = mainCam.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                EditorUtility.SetDirty(cam);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ROBLOX-STYLE] Jeu restylé façon Roblox ✔ — APPUIE SUR PLAY !");
    }

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>Crée (une fois) un matériau couleur unie, plus ou moins brillant.</summary>
    static Material MakeMat(string name, Color color, float smoothness)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || GraphicsSettings.currentRenderPipeline == null)
            shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");

        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(mat, path);
        var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (saved != null) AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(saved), name);
        return saved;
    }

    /// <summary>Mur invisible : BoxCollider seul, aucun rendu.</summary>
    static void MakeWall(string name, Vector3 pos, Vector3 size)
    {
        var wall = new GameObject(name);
        wall.transform.position = pos;
        wall.transform.localScale = size;
        wall.AddComponent<BoxCollider>();
    }

    /// <summary>Petit arbre cartoon : tronc cylindre + 3 boules vertes.</summary>
    static void MakeTree(Transform parent, Vector3 at, float trunkHeight)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = at;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localScale = new Vector3(0.38f, trunkHeight, 0.38f);
        trunk.transform.localPosition = new Vector3(0f, trunkHeight, 0f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial =
            MakeMat("MatTrunk", new Color(0.48f, 0.32f, 0.20f), 0.15f);

        Material leaf = MakeMat("MatLeaf", new Color(0.30f, 0.75f, 0.35f), 0.3f);
        Vector3[] blobs =
        {
            new Vector3( 0f, trunkHeight * 2f + 0.8f, 0f),
            new Vector3( 1.0f, trunkHeight * 2f + 0.3f, 0.3f),
            new Vector3(-0.8f, trunkHeight * 2f + 0.4f, -0.4f),
        };
        float[] sizes = { 2.4f, 2.0f, 1.8f };
        for (int i = 0; i < blobs.Length; i++)
        {
            var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blob.name = "Leaf" + i;
            blob.transform.SetParent(tree.transform, false);
            blob.transform.localPosition = blobs[i];
            blob.transform.localScale = Vector3.one * sizes[i];
            blob.GetComponent<MeshRenderer>().sharedMaterial = leaf;
        }
    }

    /// <summary>Ciel procédural bleu + soleil doux + ambient lumineux.</summary>
    static void SetupSkyAndSun()
    {
        // Matériau de ciel procédural (existe dans Assets depuis l'exécution 1)
        string skyPath = "Assets/Materials/MatSky.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                sky = new Material(skyShader);
                if (sky.HasProperty("_SkyTint"))
                    sky.SetColor("_SkyTint", new Color(0.55f, 0.75f, 1f));
                if (sky.HasProperty("_AtmosphereThickness"))
                    sky.SetFloat("_AtmosphereThickness", 0.8f);
                Directory.CreateDirectory("Assets/Materials");
                AssetDatabase.CreateAsset(sky, skyPath);
                sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            }
        }

        if (sky != null)
        {
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.05f;
        }

        // Caméra : affiche le ciel
        var mainCam = GameObject.Find("MainCamera");
        if (mainCam != null)
        {
            var cam = mainCam.GetComponent<Camera>();
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
        }

        // Un seul soleil doux
        Light sun = null;
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional) { sun = l; break; }
        }
        if (sun == null)
        {
            var sunGo = new GameObject("Sun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }
        sun.intensity = 1.15f;
        sun.color = new Color(1f, 0.97f, 0.92f);   // blanc tiède
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
    }

    /// <summary>Change l'échelle racine d'un préfab slime (le visuel enfant suit).</summary>
    static void ScalePrefab(string path, float scale)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
        var contents = PrefabUtility.LoadPrefabContents(path);
        contents.transform.localScale = Vector3.one * scale;
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}
#endif