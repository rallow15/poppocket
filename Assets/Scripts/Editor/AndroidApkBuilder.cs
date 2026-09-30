#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Compile le jeu en .apk Android (installable tout de suite sur un tel).
/// Lancement : Unity.exe -projectPath ... -executeMethod AndroidApkBuilder.BuildBoth
///             -batchmode -quit -logFile Builds/mobile-build.log
/// BuildBoth = d'abord le .apk Android, PUIS le projet Xcode (iOS).
/// </summary>
public static class AndroidApkBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string ApkPath = "Builds/Android/PopPocket.apk";

    /// <summary>Tout enchaîné : APK Android puis projet Xcode iOS.</summary>
    public static void BuildBoth()
    {
        EnsureSceneInBuild();
        BuildAndroid();
        IosProjectBuilder.BuildXcodeProject();
    }

    /// <summary>SEUL le .apk Android.</summary>
    public static void BuildAndroidApk()
    {
        EnsureSceneInBuild();
        BuildAndroid();
    }

    private static void BuildAndroid()
    {
        // IL2CPP + ARM64 : marche sur TOUS les téléphones modernes
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.poppocket.slimefidgets");

        Debug.Log("[APK-BUILD] Compilation Android (peut prendre 10-25 min)...");

        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { ScenePath }, ApkPath, BuildTarget.Android, BuildOptions.None);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[APK-BUILD] APK PRÊT ✔ → " + System.IO.Path.GetFullPath(ApkPath));
        else
            Debug.LogError("[APK-BUILD] ÉCHEC : " + report.summary.result +
                           " — erreurs : " + report.summary.totalErrors);
    }

    private static void EnsureSceneInBuild()
    {
        if (System.Array.Find(EditorBuildSettings.scenes, s => s.path == ScenePath) == null)
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
#endif