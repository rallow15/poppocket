#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Génère le PROJET XCODE du jeu (étape 1 du voyage vers l'iPhone).
/// Prévu pour être lancé en ligne de commande :
///   Unity.exe -projectPath ... -executeMethod IosProjectBuilder.BuildXcodeProject -quit -logFile Builds/ios-build.log
/// RÉALITÉ IMPORTANTE : sur Windows on génère le dossier Xcode ; le .ipa
/// final doit ensuite être compilé sur un Mac avec Xcode et un compte
/// Apple Developer.
/// </summary>
public static class IosProjectBuilder
{
    public const string OutputDir = "Builds/iOS/XcodeProject";

    public static void BuildXcodeProject()
    {
        // S'assure que la scène de jeu est dans les Build Settings
        const string scenePath = "Assets/Scenes/MainScene.unity";
        if (File.Exists(scenePath) &&
            System.Array.Find(EditorBuildSettings.scenes, s => s.path == scenePath) == null)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
        }

        // iOS n'accepte QUE le backend IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.poppocket.slimefidgets");

        Debug.Log("[IOS-BUILD] Compilation du projet Xcode (peut prendre 10-20 min)...");

        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { scenePath },
            OutputDir,
            BuildTarget.iOS,
            BuildOptions.None);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[IOS-BUILD] PROJET XCODE GÉNÉRÉ ✔ → " +
                      Path.GetFullPath(OutputDir) +
                      " (à ouvrir sur un Mac avec Xcode pour créer le .ipa)");
        }
        else
        {
            Debug.LogError("[IOS-BUILD] ÉCHEC : " + report.summary.result +
                           " — erreurs : " + report.summary.totalErrors);
        }
    }
}
#endif