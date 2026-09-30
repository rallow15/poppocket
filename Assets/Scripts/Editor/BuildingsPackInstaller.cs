#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Pose les VRAIS bâtiments du pack Asset Store « Cartoon Buildings »
/// (Garage / Station-service / Station) tout autour de l'environnement,
/// JAMAIS sur l'arène. Remplace les maisons-cubes ("MaisonsVillage").
///
/// Les matériaux du pack peuvent porter un shader absent du projet
/// (URP ici absent) → ROSE. `UrpMaterialConverter` répare tout seul
/// chaque matériau en le rebranchant sur le shader du projet. Idempotent.
/// </summary>
[InitializeOnLoad]
public static class BuildingsPackInstaller
{
    static BuildingsPackInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("POPPOCKET/4. Bâtiments du pack (Asset Store)")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        if (GameUpgradeInstaller.EnsureGameSceneOpen()) return;

        // Déjà installé ? On vérifie juste que leurs matériaux ne sont pas roses.
        var existingBat = GameObject.Find("Batiments");
        if (existingBat != null)
        {
            if (UrpMaterialConverter.Convert(existingBat))
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("[BATIMENTS-URP] Matériaux des bâtiments réparés ✔ (plus de rose !)");
            }
            return;
        }

        List<GameObject> models = FindBuildingPrefabs();
        if (models.Count == 0)
        {
            Debug.Log(
                "[BATIMENTS-PACK] Le pack « Cartoon Buildings » n'est pas encore importé. " +
                "Dans Unity : Window > Package Manager > My Assets > Cartoon Buildings > " +
                "Download > Import. Dès l'import, les bâtiments se placeront TOUT SEULS 🏪 (les maisons-cubes gardent la place en attendant)");
            return;
        }

        // ── Les VRAIS bâtiments remplacent les maisons-cubes ─────────
        GameObject oldVillage = GameObject.Find("MaisonsVillage");
        if (oldVillage != null) Undo.DestroyObjectImmediate(oldVillage);

        float half = GameUpgradeInstaller.ArenaHalfForDecor();
        var root = new GameObject("Batiments");

        // ── 2 anneaux autour de l'arène (jamais dessus) ──────────────
        // anneau 1 : proche (au-delà du rayon des animaux)
        // anneau 2 : plus loin (l'environnement est habillé de partout)
        int count = 0;
        count += PlaceRing(root, models, half, 8, half + 6.5f, half + 9.5f);
        count += PlaceRing(root, models, half, 6, half + 10.5f, half + 14.0f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[BATIMENTS-PACK] " + count + " bâtiments du pack placés partout autour " +
                  "(Garage + Station-service + Station en plusieurs exemplaires) — " +
                  "RIEN sur l'arène ✔ 🏪⛽");
    }

    /// <summary>
    /// Place count bâtiments sur un anneau (rayon mini/maxi), espacés
    /// régulièrement + petit hasard, façade vers l'arène.
    /// </summary>
    private static int PlaceRing(GameObject root, List<GameObject> models, float half,
                                 int count, float rMin, float rMax)
    {
        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            GameObject model = models[placed % models.Count];   // tourne sur les 3 modèles
            float angle = (i * (360f / count) + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
            float r = Random.Range(rMin, rMax);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (go == null) go = Object.Instantiate(model);
            go.name = "Batiment_" + model.name + "_" + placed;
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            // Garde la taille du pack multipliée par une petite variation
            go.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 180f
                                                        + Random.Range(-25f, 25f), 0f);

            // Mise à l'échelle auto : empreinte maxi ≈ 5,5 m (comme une maison)
            float size = BoundsMaxSide(go);
            if (size > 0.01f)
            {
                float k = 5.5f / size;
                k = Mathf.Clamp(k, 0.4f, 3.0f);
                go.transform.localScale = go.transform.localScale * k;
            }

            // Décor pur : aucun collider
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            // Matériaux du pack : on répare tout shader rose
            UrpMaterialConverter.Convert(go);

            placed++;
        }
        return placed;
    }

    // ────────────────────────────────────────────────────────────────
    //  DÉTECTION DU PACK
    // ────────────────────────────────────────────────────────────────
    private static List<GameObject> FindBuildingPrefabs()
    {
        var result = new List<GameObject>();
        var seen = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || seen.Contains(path)) continue;
            seen.Add(path);

            string lowered = path.ToLower();
            if (!lowered.Contains("building")) continue;          // dossier du pack
            if (lowered.Contains("demo") || lowered.Contains("editor")) continue;

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;
            if (asset.GetComponentInChildren<MeshRenderer>(true) == null) continue;
            result.Add(asset);
        }
        return result;
    }

    // ────────────────────────────────────────────────────────────────
    //  OUTILS
    // ────────────────────────────────────────────────────────────────
    /// <summary>Plus grand côté horizontal (X ou Z) du modèle, en mètres.</summary>
    private static float BoundsMaxSide(GameObject go)
    {
        bool any = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var b = r.bounds;
            if (!any) { min = b.min; max = b.max; any = true; }
            else { min = Vector3.Min(min, b.min); max = Vector3.Max(max, b.max); }
        }
        if (!any) return 0f;
        float dx = max.x - min.x, dz = max.z - min.z;
        return Mathf.Max(dx, dz);
    }
}
#endif