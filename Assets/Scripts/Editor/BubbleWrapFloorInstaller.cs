#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Applique la texture papier à bulles (TextureCan, déjà téléchargée par salim)
/// sur le sol de l'arène ("Arena") : albedo + relief (normal map),
/// smoothness 0.9 → le sol brillant façon film à bulles sous les sphères 3D.
/// Les bulles POP 3D (BubbleSpawner) restent AU-DESSUS, elles donnent le score.
/// Idempotent : si l'arène porte déjà le bon matériau, rien ne se passe.
/// </summary>
[InitializeOnLoad]
public static class BubbleWrapFloorInstaller
{
    const string ColorTex  = "Assets/TexturesBulles/paper_0011_color_1k.jpg";
    const string NormalTex = "Assets/TexturesBulles/paper_0011_normal_opengl_1k.png";
    const string MaterialPath = "Assets/Materials/MatSolPapierBulles.mat";
    const float TilingXZ = 14f;   // 28 m d'arène / ~2 m par motif
    const float Smoothness = 0.9f;

    static BubbleWrapFloorInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("POPPOCKET/6. Sol papier à bulles")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        if (GameUpgradeInstaller.EnsureGameSceneOpen()) return;

        var arena = GameObject.Find("Arena");
        if (arena == null)
        {
            Debug.LogWarning("[BULLES-SOL] Arène introuvable (scène du jeu pas ouverte), rien cassé");
            return;
        }
        var ren = arena.GetComponent<MeshRenderer>();
        if (ren == null) return;

        // Déjà fait ? on s'arrête là (idempotent)
        if (ren.sharedMaterial != null && ren.sharedMaterial.name == "MatSolPapierBulles")
        {
            Debug.Log("[BULLES-SOL] Sol papier à bulles déjà appliqué ✔");
            return;
        }

        var mat = MakeMaterial();
        if (mat == null) return;

        ren.sharedMaterial = mat;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[BULLES-SOL] Sol de l'arène = papier à bulles brillant ✔ (les 400 bulles 3D restent par-dessus, POP !)");
    }

    /// <summary>Crée une fois le matériau Standard : couleur + relief + brillance.</summary>
    static Material MakeMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null) return existing;

        Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(ColorTex);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTex);
        if (color == null)
        {
            Debug.LogWarning("[BULLES-SOL] Texture couleur introuvable : " + ColorTex);
            return null;
        }

        // La normal map doit être marquée comme RELIEF, sinon Unity l'affiche à plat
        var imp = AssetImporter.GetAtPath(NormalTex) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogWarning("[BULLES-SOL] Shader Standard introuvable (projet BUILT-IN)");
            return null;
        }

        var mat = new Material(shader);
        mat.mainTexture = color;
        mat.mainTextureScale = new Vector2(TilingXZ, TilingXZ);
        if (normal != null) mat.SetTexture("_BumpMap", normal);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", Smoothness);   // 0.9 : très brillant
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
#endif