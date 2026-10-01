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
            // Animaux du pack déjà posés : on répare les matériaux roses
            // ET l'animation de marche (chaque animal doit jouer SA marche,
            // partagée entre modèles = glissade sans bouger les pattes).
            bool fixedMats = UrpMaterialConverter.Convert(existing);
            bool fixedWalk = FixWalkAnimations(existing);
            if (fixedMats || fixedWalk)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("[ANIMAUX-PACK] Animaux vérifiés ✔ (matériaux + animation de marche de chaque animal réparés)");
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

        // ── 3. Trouve LE clip de marche de CHAQUE animal ─────────────
        // Un clip de marche anime seulement les os du modèle dont il vient
        // → réutiliser LE même clip pour tous = les animaux glissent sans
        // bouger les pattes. On cherche donc le clip de chacun.
        var walkClips = new Dictionary<GameObject, AnimationClip>();
        foreach (GameObject model in models)
        {
            AnimationClip clip = FindWalkClipForModel(model);
            if (clip != null) walkClips[model] = clip;
        }
        if (walkClips.Count == 0)
            Debug.LogWarning("[ANIMAUX-PACK] Aucun clip de marche trouvé dans le pack — les animaux se baladeront sans animation");

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

            // On répare tout shader rose du pack (shader absent du projet)
            UrpMaterialConverter.Convert(animal);

            // Animation de marche : LE clip de CET animal (via son contrôleur)
            AnimationClip ownClip;
            walkClips.TryGetValue(model, out ownClip);
            var anim = animal.GetComponent<Animator>();
            if (anim == null) anim = animal.AddComponent<Animator>();
            if (ownClip != null)
            {
                string safe = SanitizeName(model.name);
                anim.runtimeAnimatorController =
                    BuildWalkController(ownClip, "Assets/Animals/Walk_" + safe + ".controller");
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

    /// <summary>
    /// Cherche le clip de marche du modèle DONNÉ (et pas n'importe lequel) :
    /// 1. dans son propre contrôleur d'animation,
    /// 2. dans son Animation (legacy),
    /// 3. sinon le 1er clip de son contrôleur,
    /// 4. sinon un clip du pack dont le nom contient le nom du modèle.
    /// </summary>
    private static AnimationClip FindWalkClipForModel(GameObject model)
    {
        var src = model.GetComponentInChildren<Animator>(true);
        var legacy = model.GetComponentInChildren<Animation>(true);

        // 1) Clip "walk" du contrôleur du modèle
        if (src != null && src.runtimeAnimatorController != null)
        {
            var best = FindAnyClipInController(src.runtimeAnimatorController);
            if (best != null) return best;
        }

        // 2) Animation legacy (anciens packs)
        if (legacy != null)
        {
            foreach (AnimationState state in legacy)
                if (state.clip != null && state.clip.name.ToLower().Contains("walk"))
                    return state.clip;
        }

        // 3) Clip du pack dont le nom contient le nom du modèle
        string modelName = model.name.ToLower();
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.ToLower().Contains("animal")) continue;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            string cn = clip.name.ToLower();
            if (cn.Contains(modelName) && (cn.Contains("walk") || cn.Contains("run"))) return clip;
        }
        return FindAnyClipInController(src != null ? src.runtimeAnimatorController : null);
    }

    /// <summary>Tout clip de marche (ou à défaut le 1er clip) d'un contrôleur.</summary>
    private static AnimationClip FindAnyClipInController(RuntimeAnimatorController controller)
    {
        if (controller == null) return null;
        foreach (var clip in controller.animationClips)
        {
            if (clip == null) continue;
            if (clip.name.ToLower().Contains("walk")) return clip;
        }
        foreach (var clip in controller.animationClips)
            if (clip != null) return clip;
        return null;
    }

    /// <summary>
    /// Répare déjà posés : chaque animal reçoit le contrôleur avec SA
    /// marche (les clips d'un autre modèle n'animent pas ses os).
    /// Retourne true si un contrôleur a changé.
    /// </summary>
    private static bool FixWalkAnimations(GameObject root)
    {
        bool changed = false;
        foreach (Transform child in root.transform)
        {
            var anim = child.GetComponent<Animator>();
            if (anim == null) continue;

            // "Animal_Tigre" → "Tigre" : on retrouve le modèle du pack
            string name = child.name.StartsWith("Animal_")
                ? child.name.Substring(7) : child.name;
            GameObject model = FindModelByName(name);
            AnimationClip clip = model != null ? FindWalkClipForModel(model) : null;
            if (clip == null)
            {
                Debug.LogWarning("[ANIMAUX-MARCHE] Pas de clip de marche pour " + child.name);
                continue;
            }
            var controller = BuildWalkController(clip,
                "Assets/Animals/Walk_" + SanitizeName(name) + ".controller");
            if (anim.runtimeAnimatorController != controller)
            {
                anim.runtimeAnimatorController = controller;
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>Retrouve le prefab du pack qui porte exactement ce nom.</summary>
    private static GameObject FindModelByName(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.ToLower().Contains("animal")) continue;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null && asset.name == name) return asset;
        }
        return null;
    }

    /// <summary>Nom de fichier sûr : lettres/chiffres/tirets seulement.</summary>
    private static string SanitizeName(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "Animal";
    }

    /// <summary>
    /// Un contrôleur d'animation tout simple : 1 état = la marche, en boucle.
    /// Un contrôleur PAR animal (le clip doit venir du bon modèle).
    /// </summary>
    private static AnimatorController BuildWalkController(AnimationClip clip, string path)
    {
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