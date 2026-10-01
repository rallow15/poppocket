#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Installe automatiquement le contrôle AU DOIGT (drag partout sur l'écran) :
///  - crée l'objet "TouchInput" (script ScreenDragInput) dans la scène
///  - branche GameManager.playerJoystick dessus (le slime joueur s'en sert)
///  - supprime l'ancien joystick-bouton "PlayerJoystick" à l'écran
/// S'exécute tout seul au démarrage de l'éditeur. Idempotent.
/// </summary>
[InitializeOnLoad]
public static class DragInputInstaller
{
    static DragInputInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        if (EditorApplication.isPlaying) return; // ne rien faire pendant le jeu
        if (GameUpgradeInstaller.EnsureGameSceneOpen()) return; // batch : ouvrir la scène D'ABORD
        FixCamera();    // toujours (corrige la vieille hauteur 22 enregistrée)
        FixMovement();  // réactivité : pas de friction, accélération forte
        FixSound();     // son d'éclatement = PopUser.wav
        SlimeArtApplier.Run(); // habillage slimes low-poly (aussi appelé par son propre InitializeOnLoad)
        FixJump();      // saut plus haut (demande du joueur)
        InstallPowerUps(); // système de bonus : VITESSE / AURA / ZAP
        DenseDecorInstaller.Run(); // décor plus dense autour de l'arène
        GameUpgradeInstaller.Run(); // village + animaux + menu + HUD bonus + safe area
        AnimalsPackInstaller.Run(); // vrais modèles animés du pack si importé (sinon consigne)
        BuildingsPackInstaller.Run(); // bâtiments du pack Cartoon Buildings si importé (sinon consigne)
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null) return; // pas dans la scène de jeu

        var cur = gm.playerJoystick;

        // Déjà installé via ce script ? Ne rien refaire.
        if (cur != null && cur is ScreenDragInput)
        {
            return;
        }

        // 1) Crée / réutilise l'objet TouchInput
        GameObject touchGO = GameObject.Find("TouchInput");
        if (touchGO == null)
        {
            touchGO = new GameObject("TouchInput");
            touchGO.AddComponent<ScreenDragInput>();
        }
        if (touchGO.GetComponent<ScreenDragInput>() == null)
            touchGO.AddComponent<ScreenDragInput>();

        // 2) Branche le champ playerJoystick du GameManager
        Undo.RecordObject(gm, "Branche déplacement au doigt");
        gm.playerJoystick = touchGO.GetComponent<ScreenDragInput>();
        EditorUtility.SetDirty(gm);

        // 3) Supprime l'ancien joystick-bouton (et sa poignée "Handle")
        GameObject oldJoy = GameObject.Find("PlayerJoystick");
        if (oldJoy != null)
        {
            Undo.DestroyObjectImmediate(oldJoy);
            Debug.Log("[DRAG-INPUT] Ancien joystick-bouton supprimé");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[DRAG-INPUT] Déplacement au doigt installé ✔ (glisse partout sur l'écran)");
    }

    /// <summary>
    /// Force la caméra en VRAIE 3e personne : derrière le slime, Basse.
    /// (la scène avait gardé height=22 → vue d'en haut = problème signalé)
    /// </summary>
    private static void FixCamera()
    {
        GameObject mainCamGO = GameObject.Find("MainCamera");
        if (mainCamGO == null) return;

        var cf = mainCamGO.GetComponent<CameraFollow>();
        var cam = mainCamGO.GetComponent<Camera>();
        if (cf == null || cam == null) return;

        bool change = cf.height != 5.0f || cf.backDistance != 10.5f ||
                      cf.fovLandscape != 66f || cam.orthographic;

        // DEZOOM (demande salim) : l'iPhone montrait trop près, et le
        // chrono était caché par la Dynamic Island. On recule la caméra
        // et on ouvre le champ de vision — MÊME VUE VERTICAL ET HORIZONTAL :
        cf.height = 5.0f;          // caméra plus haute : on voit les alentours
        cf.backDistance = 10.5f;   // reculée : village + animaux visibles
        cf.lookAtHeight = 1.6f;
        cf.smoothSpeed = 5f;
        cf.fovLandscape = 66f;     // champ de vision (les 2 sens : même vue)
        cam.orthographic = false;

        if (change)
        {
            EditorUtility.SetDirty(cf);
            EditorUtility.SetDirty(cam);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[CAMERA-DRAG] Caméra dézoomée ✔ (même vue vertical et horizontal)");
        }
    }

    /// <summary>
    /// Réactivité du déplacement : les slimes glissent comme du savon
    /// (friction quasi nulle) avec une accélération forte ; le freinage est
    /// assuré par la traînée du Rigidbody → réponse immédiate quand on
    /// relâche le doigt. Appliqué aux PROJÉFABS (les slimes naissent de là).
    /// </summary>
    private static void FixMovement()
    {
        bool changed = false;
        changed |= TuneSlime("Assets/Prefabs/PlayerSlimePrefab.prefab", 60f);
        changed |= TuneSlime("Assets/Prefabs/BotSlimePrefab.prefab", 30f);
        if (changed)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[MOVE-FIX] Slimes boostés : glisse rapide et réactive ✔");
        }
    }

    /// <summary>Force accélération / traînée / matériau glissant sur un prefab slime.</summary>
    private static bool TuneSlime(string path, float accel)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) return false;

        var contents = PrefabUtility.LoadPrefabContents(path);
        bool changed = false;

        var sc = contents.GetComponent<SlimeController>();
        if (sc != null)
        {
            if (!Mathf.Approximately(sc.playerForce, accel) || !Mathf.Approximately(sc.botForce, accel * 0.5f))
            {
                sc.playerForce = accel;
                sc.botForce = accel * 0.5f;
                changed = true;
            }
            // Saut plus haut (demande du joueur) — les valeurs enregistrées
            // du prefab gagnent sur les valeurs par défaut du script.
            const float wantedJump = 12f;
            if (!Mathf.Approximately(sc.jumpForce, wantedJump))
            {
                sc.jumpForce = wantedJump;
                changed = true;
            }
        }

        var rb = contents.GetComponent<Rigidbody>();
        if (rb != null && !Mathf.Approximately(rb.linearDamping, 8f))
        {
            rb.linearDamping = 8f; // top speed ≈ accel/8 → ~7.5 m/s joueur, réactif
            changed = true;
        }

        var col = contents.GetComponent<SphereCollider>();
        if (col != null)
        {
            var mat = LoadOrCreateGlideMaterial();
            if (col.sharedMaterial != mat)
            {
                col.sharedMaterial = mat;
                changed = true;
            }
        }

        if (changed) PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        return changed;
    }

    /// <summary>PhysicMaterial "savon" : friction quasi nulle combinée au minimum.</summary>
    private static PhysicsMaterial LoadOrCreateGlideMaterial()
    {
        const string path = "Assets/Materials/MatSlimeGlide.physicMaterial";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat != null) return mat;

        Directory.CreateDirectory("Assets/Materials");
        mat = new PhysicsMaterial("SlimeGlide")
        {
            dynamicFriction = 0.02f,
            staticFriction = 0.02f,
            bounciness = 0.05f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>
    /// Installe le système de power-ups : crée l'objet "PowerUpManager" dans la
    /// scène (il fait apparaître UN bonus toutes les 30 s, position au hasard).
    /// Idempotent : ne fait rien s'il existe déjà. Idempotent.
    /// </summary>
    private static void InstallPowerUps()
    {
        var existing = Object.FindFirstObjectByType<PowerUpManager>();
        if (existing != null) return; // déjà installé

        GameObject go = new GameObject("PowerUpManager");
        go.AddComponent<PowerUpManager>();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[POWER-UP] Système de bonus installé ✔ (VITESSE / AURA / ZAP — 1 toutes les 30 s)");
    }

    /// <summary>Le saut du joueur demande plus de punch : jumpForce du prefab → 12.</summary>
    private static void FixJump()
    {
        bool changed = false;
        changed |= TuneSlimeJump("Assets/Prefabs/PlayerSlimePrefab.prefab");
        changed |= TuneSlimeJump("Assets/Prefabs/BotSlimePrefab.prefab");
        if (changed) Debug.Log("[JUMP-FIX] Saut plus haut ✔ (jumpForce = 12)");
    }

    private static bool TuneSlimeJump(string path)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) return false;

        var contents = PrefabUtility.LoadPrefabContents(path);
        bool changed = false;
        var sc = contents.GetComponent<SlimeController>();
        if (sc != null && !Mathf.Approximately(sc.jumpForce, 12f))
        {
            sc.jumpForce = 12f;
            changed = true;
        }
        if (changed) PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        return changed;
    }

    /// <summary>
    /// Remplace le son d'éclatement par le son bubble de l'utilisateur
    /// (copié dans Assets/Sounds/PopUser.wav). Appliqué au prefab des bulles
    /// ET au modèle de bulle dans la scène (le spawner copie ses réglages).
    /// </summary>
    private static void FixSound()
    {
        var userClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/PopUser.wav");
        if (userClip == null) return; // le fichier n'est pas (encore) importé

        var wanted = new AudioClip[] { userClip };
        bool changed = false;

        // 1) Le prefab des bulles
        const string prefabPath = "Assets/Prefabs/BubblePrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<Bubble>(prefabPath) != null)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var b = contents.GetComponent<Bubble>();
            if (!SameSounds(b.popSounds, userClip))
            {
                b.popSounds = wanted;
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                changed = true;
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // 2) Le modèle de bulle posé dans la scène (source des réglages du spawner)
        foreach (var bub in Object.FindObjectsByType<Bubble>(FindObjectsSortMode.None))
        {
            if (SameSounds(bub.popSounds, userClip)) continue;
            Undo.RecordObject(bub, "Son d'éclatement utilisateur");
            bub.popSounds = wanted;
            EditorUtility.SetDirty(bub);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[POP-SOUND] Son d'éclatement remplacé par PopUser.wav ✔");
        }
    }

    private static bool SameSounds(AudioClip[] arr, AudioClip clip)
    {
        return arr != null && arr.Length == 1 && arr[0] == clip;
    }
}
#endif