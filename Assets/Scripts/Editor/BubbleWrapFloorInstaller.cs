#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// ANNULE la texture papier à bulles sur le sol de l'arène (salim a changé
/// d'idée : il veut des BULLES D'EAU qui éclatent, cf. BubbleSpawner).
/// Si l'arène porte encore MatSolPapierBulles, on remet le sol bleu
/// brillant d'origine (MatFloorPlayland). Idempotent.
/// </summary>
[InitializeOnLoad]
public static class BubbleWrapFloorInstaller
{
    const string WrapMaterialName = "MatSolPapierBulles";
    const string BlueMaterialName = "MatFloorPlayland";
    const string BlueMaterialPath = "Assets/Materials/MatFloorPlayland.mat";

    static BubbleWrapFloorInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("POPPOCKET/6. Retour au sol bleu (annule papier à bulles)")]
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

        if (ren.sharedMaterial == null || ren.sharedMaterial.name != WrapMaterialName)
        {
            Debug.Log("[BULLES-SOL] Le sol n'a jamais eu de papier à bulles ✔");
            return;
        }

        var blue = AssetDatabase.LoadAssetAtPath<Material>(BlueMaterialPath);
        if (blue == null)
        {
            // Recrée le sol bleu brillant d'origine si l'asset a disparu
            Shader shader = Shader.Find("Standard");
            blue = new Material(shader) { name = BlueMaterialName,
                color = new Color(0.70f, 0.86f, 0.99f) };
            if (blue.HasProperty("_Glossiness")) blue.SetFloat("_Glossiness", 0.75f);
            Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.CreateAsset(blue, BlueMaterialPath);
            AssetDatabase.SaveAssets();
        }

        ren.sharedMaterial = blue;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.DeleteAsset("Assets/Materials/MatSolPapierBulles.mat");
        Debug.Log("[BULLES-SOL] Papier à bulles DU SOL retiré — retour du sol bleu brillant ✔" +
                  " (au-dessus, les bulles sont maintenant des BULLES D'EAU !)");
    }
}
#endif