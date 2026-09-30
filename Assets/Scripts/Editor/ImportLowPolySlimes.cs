#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Importe automatiquement le pack "Low-Poly-Slimes" téléchargé par l'utilisateur,
/// au premier démarrage de l'éditeur où ce script existe. Idempotent :
/// si des modèles Slime sont déjà dans le projet, ne réimporte pas.
/// </summary>
[InitializeOnLoad]
public static class ImportLowPolySlimes
{
    static ImportLowPolySlimes()
    {
        EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        // Déjà importé ? (un modèle FBX/OBJ contenant "slime" dans son chemin)
        var models = AssetDatabase.FindAssets("slime t:Model");
        if (models != null && models.Length > 0)
        {
            Debug.Log("[SLIMES] Pack Low-Poly Slimes déjà présent ✔");
            return;
        }

        string pkg = @"C:\Users\salim\Downloads\Low-Poly-Slimes.unitypackage";
        if (File.Exists(pkg))
        {
            Debug.Log("[SLIMES] Import du pack Low-Poly-Slimes (155 Ko)...");
            AssetDatabase.ImportPackage(pkg, false); // false : sans fenêtre de confirmation
            EditorUtility.RequestScriptReload();
            Debug.Log("[SLIMES] Import terminé ✔");
        }
        else
        {
            Debug.LogWarning("[SLIMES] unitypackage introuvable : " + pkg);
        }
    }
}
#endif