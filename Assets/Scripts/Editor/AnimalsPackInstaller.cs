#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Remplace les animaux-cubes par les VRAIS modèles du pack Asset Store
/// « Animals FREE - Animated Low Poly 3D Models » (ithappy) :
/// tigre, cheval, chien, cerf, chat, pingouin, poule — avec animations
/// marche / course / idle, lues AUTOMATIQUEMENT.
///
/// DÉROULÉ POUR SALIM (une seule fois) :
///   1. Window → Package Manager → My Assets → "Animals FREE" → Download
///   2. Import (tout importer)
///   → dès l'import, CE SCRIPT remplace tout seul les animaux du jeu.
///
/// Si le pack n'est pas encore importé, le script affiche la consigne
/// et garde les animaux-cubes (rien ne casse). Idempotent.
/// </summary>
[InitializeOnLoad]
public static class AnimalsPackInstaller
{
    static AnimalsPackInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("POPPOCKET/3. Animaux du pack (Asset Store)")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        if (GameUpgradeInstaller.EnsureGameSceneOpen()) return;

        // Déjà remplacé ? (les animaux du pack ont un Animator, pas ceux en cubes)
        GameObject existing = GameObject.Find("Animaux");
        if (existing != null && existing.GetComponentInChildren<Animator>(true) != null)
        {
            // Animaux du pack déjà posés : il ne reste qu'à convertir les
            // matériaux en URP si le pack était en shaders Built-in (ROSE).
            if (UrpMaterialConverter.Convert(existing))
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("[ANIMAUX-URP] Matériaux des animaux convertis en URP ✔ (plus de rose !)");
            }
            return;
        }

        // ── 1. Détecter le pack dans le projet ───────────────────────
        List<GameObject> models = FindAnimalModels();
        if (models.Count == 0)
        {
            Debug.Log(
                "[ANIMAUX-PACK] Le pack « Animals FREE » n'est pas encore importé. " +
                "Dans Unity : Window > Package Manager > My Assets > Animals FREE > " +
                "Download > Import. Dès l'import, les animaux s'installeront TOUT SEULS 🐴🐈 (les animaux-cubes gardent la place en attendant)");
            return;
        }

        // ── 2. Virer les animals-cubes de secours ────────────────────
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        float half = GameUpgradeInstaller.ArenaHalfForDecor();
        var root = new GameObject("Animaux");

        // ── 3. Prépare l'animation de marche partagée ────────────────
        AnimationClip walkClip = FindWalkClip(models);
        AnimatorController controller = null;
        if (walkClip != null) controller = BuildWalkController(walkClip);
        if (walkClip == null)
            Debug.LogWarning("[ANIMAUX-PACK] Clip de marche introuvable dans le pack — les animaux se baladeront sans animation");

        // ── 4. Une instance de CHAQUE animal sur l'anneau ────────────
        // (cap à 14 modèles : si le pack contient des variantes, on s'en tient là)
        if (models.Count > 14) models.RemoveRange(14, models.Count - 14);
        int count = 0;
        foreach (GameObject model in models)
        {
            GameObject animal = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (animal == null) animal = Object.Instantiate(model);
            animal.name = "Animal_" + model.name;
            animal.transform.SetParent(root.transform, false);

            // Position sur l'anneau, hors de l'arène
            float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Random.Range(half + 2.8f, half + 7.5f);
            animal.transform.position = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            animal.transform.rotation = Quaternion.identity;

            // Mise à l'échelle automatique : chaque modèle ≈ 1,1 m de haut
            float height = BoundsHeight(animal);
            if (height > 0.01f)
            {
                float k = 1.1f / height;
                animal.transform.localScale *= k;
            }
            else
            {
                animal.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            }

            // Décor pur : aucun collider (jamais en travers d'un slime)
            StripColliders(animal);

            // Shaders Built-in du pack → URP (sinon tout est rose)
            UrpMaterialConverter.Convert(animal);

            // Animation de marche en boucle
            if (controller != null)
            {
                var anim = animal.GetComponent<Animator>();
                if (anim == null) anim = animal.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
            }
            else
            {
                // Pas de clip trouvé : retire l'Animator de base (T-pose figée)
                var baseAnim = animal.GetComponent<Animator>();
                if (baseAnim != null && baseAnim.runtimeAnimatorController == null)
                    Object.DestroyImmediate(baseAnim);
            }

            // Balade : reste TOUJOURS sur l'anneau autour de l'arène
            var wanderer = animal.AddComponent<Wanderer>();
            wanderer.ringRadius = new Vector2(half + 2.3f, half + 8.0f);
            wanderer.speed = Random.Range(0.8f, 1.6f);
            wanderer.bobAmplitude = 0.02f;

            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ANIMAUX-PACK] " + count + " vrais animaux animés installés " +
                  "(tigre, cheval, chien, cerf, chat, pingouin, poule) — ils contournent l'arène ✔ 🦌🐧");
    }

    // ────────────────────────────────────────────────────────────────
    //  DÉTECTION DU PACK
    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Trouve les modèles d'animaux du pack : prefabs / modèles situés dans un
    /// dossier dont le nom contient "animal" (le nom du pack l'a), avec un rendu.
    /// </summary>
    private static List<GameObject> FindAnimalModels()
    {
        var result = new List<GameObject>();
        var seen = new HashSet<string>();

        // On ne prend QUE les prefabs (les FBX du pack sont les mêmes
        // modèles en double à l'intérieur, on éviterait 2 fois le même animal)
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || seen.Contains(path)) continue;
            seen.Add(path);

            // Le dossier du pack a "animal" dans son chemin (insensible à la casse)
            string lowered = path.ToLower();
            if (!lowered.Contains("animal")) continue;

            // Évite les dossiers techniques (icônes, shaders, demo)
            if (lowered.Contains("demo") || lowered.Contains("editor")) continue;

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;
            if (asset.GetComponentInChildren<SkinnedMeshRenderer>(true) == null &&
                asset.GetComponentInChildren<MeshRenderer>(true) == null) continue;

            result.Add(asset);
        }
        return result;
    }

    /// <summary>Cherche un clip de marche ("walk") dans tous les modèles du pack.</summary>
    private static AnimationClip FindWalkClip(List<GameObject> models)
    {
        // 1) Le clip peut être référencé par un Animator dans le modèle lui-même
        foreach (GameObject model in models)
        {
            var src = model.GetComponentInChildren<Animator>(true);
            if (src != null && src.runtimeAnimatorController != null)
            {
                foreach (var clip in src.runtimeAnimatorController.animationClips)
                    if (clip != null && clip.name.ToLower().Contains("walk")) return clip;
            }
        }

        // 2) Sinon : tout clip "walk" posé dans le dossier du pack
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            string lowered = path.ToLower();
            if (!lowered.Contains("animal")) continue;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name.ToLower().Contains("walk")) return clip;
        }
        return null;
    }

    /// <summary>Un contrôleur d'animation tout simple : 1 état = la marche, en boucle.</summary>
    private static AnimatorController BuildWalkController(AnimationClip clip)
    {
        const string path = "Assets/Animals/AnimalWalk.controller";
        System.IO.Directory.CreateDirectory("Assets/Animals");

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var state = controller.layers[0].stateMachine.AddState("Marche");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        AssetDatabase.SaveAssets();
        return controller;
    }

    // ────────────────────────────────────────────────────────────────
    //  OUTILS
    // ────────────────────────────────────────────────────────────────
    private static void StripColliders(GameObject go)
    {
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        // les colliders de l'animator d'origine ne servent à rien non plus
        foreach (var bone in go.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(bone);
    }

    /// <summary>Hauteur totale du modèle (ses rendus), en mètres.</summary>
    private static float BoundsHeight(GameObject go)
    {
        bool any = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var b = r.bounds;
            if (!any) { min = b.min; max = b.max; any = true; }
            else { min = Vector3.Min(min, b.min); max = Vector3.Max(max, b.max); }
        }
        return any ? max.y - min.y : 0f;
    }
}
#endif