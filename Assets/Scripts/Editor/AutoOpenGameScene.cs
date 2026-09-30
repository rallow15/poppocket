#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// À chaque démarrage de l'éditeur : si aucune vraie scène n'est chargée
/// (scène vide "Untitled"), ouvre automatiquement la scène de jeu
/// Assets/Scenes/MainScene.unity. Plus besoin de rien faire à la main.
/// </summary>
[InitializeOnLoad]
public static class AutoOpenGameScene
{
    static AutoOpenGameScene()
    {
        // delayCall = exécuté quand l'éditeur est prêt ET après chaque recompilation
        EditorApplication.delayCall += OpenIfEmpty;
    }

    static void OpenIfEmpty()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        bool emptyScene = !scene.IsValid()
                           || string.IsNullOrEmpty(scene.path);
        if (emptyScene)
        {
            Debug.Log("[AUTOOPEN] Scène vide détectée → ouverture de la scène de jeu...");
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            Debug.Log("[AUTOOPEN] Scène de jeu chargée ✔ — APPUIE SUR PLAY !");
        }
    }
}
#endif