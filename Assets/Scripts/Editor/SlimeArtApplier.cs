#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

/// <summary>
/// Branche les slimes low-poly du pack "Studio Nik" importé par l'utilisateur
/// (Assets/Assets/Studio Nik/Low Poly Slime) sur les préfabs + instances du jeu :
/// player = Slime_1, bots = Slime_2/3/4. Remplace la sphère blanche temporaire.
/// Crée aussi un matériau "papier" PBR depuis les textures ambientCG extraites.
/// Tout s'applique automatiquement au démarrage de l'éditeur.
/// </summary>
[InitializeOnLoad]
public static class SlimeArtApplier
{
    const string VisualPath = "Assets/Assets/Studio Nik/Low Poly Slime/Assets/Prefabs/Slime_{0}.prefab";

    static SlimeArtApplier()
    {
        EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        if (EditorApplication.isPlaying) return; // ne rien faire pendant le jeu
        if (!AssetDatabase.IsValidFolder("Assets/Assets/Studio Nik"))
            return; // pack pas encore importé

        MakePaperMaterial();

        bool changed = false;
        changed |= DecoratePrefab("Assets/Prefabs/PlayerSlimePrefab.prefab", 1);
        changed |= DecoratePrefab("Assets/Prefabs/BotSlimePrefab.prefab", 2);

        // Instances déjà posées dans la scène ouverte
        var slimes = Object.FindObjectsByType<SlimeController>(FindObjectsSortMode.None);
        foreach (var sc in slimes)
        {
            if (sc.transform.Find("VisualLowPoly") != null) continue; // déjà habillé
            int idx = sc.isPlayer ? 1 : Mathf.Clamp(sc.slimeIndex + 1, 2, 4);
            if (sc.isPlayer && sc.slimeIndex >= 0) idx = 1;
            changed |= DecorateInstance(sc.transform, idx);
            changed |= StripBadPhysics(sc.transform); // masse fantôme du pack
        }
        if (changed) EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SLIMES-ART] Slimes low-poly branchés ✔ — APPUIE SUR PLAY !");
    }

    // ───────────────────────── PRÉFABS ─────────────────────────
    static bool DecoratePrefab(string path, int visualIndex)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) return false;

        var contents = PrefabUtility.LoadPrefabContents(path);
        bool changed = StripBadPhysics(contents.transform);

        bool decorated = contents.transform.childCount > 0; // déjà habillé
        if (!decorated)
        {
            DisablePlaceholderRenderer(contents.transform);
            AddVisual(contents.transform, visualIndex);
            changed = true;
        }
        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        PrefabUtility.UnloadPrefabContents(contents);
        return changed;
    }

    /// <summary>
    /// Le pack Low-Poly embarque son PROPRE Rigidbody (masse 10, traînée 5!) et
    /// des MeshColliders sur chaque enfant. Parenté au slime, cette masse fantôme
    /// écrase le vrai Rigidbody → le joueur bouge à peine. On retire tout :
    /// le physics, c'est uniquement le SphereCollider du slime parent.
    /// </summary>
    static bool StripBadPhysics(Transform root)
    {
        bool changed = false;
        foreach (Transform child in root)
        {
            if (child.name != "VisualLowPoly") continue;
            foreach (var rb in child.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(rb);
                changed = true;
            }
            foreach (var col in child.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                changed = true;
            }
        }
        return changed;
    }

    // ─────────────────── INSTANCES DE SCÈNE ───────────────────
    static bool DecorateInstance(Transform root, int visualIndex)
    {
        DisablePlaceholderRenderer(root);
        AddVisual(root, visualIndex);
        StripBadPhysics(root);
        return true;
    }

    /// <summary>Éteint la sphère blanche de remplacement, garde le collider.</summary>
    static void DisablePlaceholderRenderer(Transform root)
    {
        var mr = root.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
    }

    /// <summary>
    /// Ajoute le slime du pack comme enfant "VisualLowPoly", rescalé pour remplir
    /// le collider local (rayon 0.5 → diamètre 1) et posé sur le sol.
    /// </summary>
    static void AddVisual(Transform root, int visualIndex)
    {
        string path = string.Format(VisualPath, visualIndex);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(
                         AssetDatabase.LoadAssetAtPath<GameObject>(path));
        if (visual == null)
        {
            Debug.LogWarning("[SLIMES-ART] Modèle introuvable : " + path);
            return;
        }
        visual.name = "VisualLowPoly";
        visual.transform.SetParent(root, false);

        // Mesure du mesh (unités locales du modèle)
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        var mf = visual.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            b = mf.sharedMesh.bounds;
        else
        {
            // Fallback : combine tous les mesh filters enfants
            var mfs = visual.GetComponentsInChildren<MeshFilter>();
            if (mfs.Length > 0 && mfs[0].sharedMesh != null) b = mfs[0].sharedMesh.bounds;
        }

        float maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float scale = 1f / Mathf.Max(0.001f, maxExtent); // diamètre voulu : 1 unité locale
        visual.transform.localScale = Vector3.one * scale;

        // Pose au sol : bas du modèle aligné avec le bas du collider (y = -0.5 local)
        float bottomY = (b.center.y - b.extents.y) * scale;
        visual.transform.localPosition = new Vector3(
            -(b.center.x * scale), -0.5f - bottomY, -(b.center.z * scale));
    }

    // ──────────────── MATÉRIAU PAPIER (textures PBR) ────────────────
    static void MakePaperMaterial()
    {
        string path = "Assets/Materials/MatPaper0011.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

        var color = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Paper0011/paper_0011_color_1k.jpg");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Paper0011/paper_0011_normal_opengl_1k.png");
        var ao = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Paper0011/paper_0011_ao_1k.jpg");
        if (color == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || GraphicsSettings.currentRenderPipeline == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.mainTexture = color;
        if (mat.HasProperty("_BumpMap") && normal != null) mat.SetTexture("_BumpMap", normal);
        if (mat.HasProperty("_NormalMap") && normal != null) mat.SetTexture("_NormalMap", normal);
        if (mat.HasProperty("_OcclusionMap") && ao != null) mat.SetTexture("_OcclusionMap", ao);
        if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", 0.6f);
        mat.EnableKeyword(normal != null ? "_NORMALMAP" : "");

        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(mat, path);
        var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (saved != null) AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(saved), "MatPaper0011.mat");
        Debug.Log("[PAPIER] Matériau 'MatPaper0011' créé (prêt à remplacer le sol si tu veux) ✔");
    }
}
#endif