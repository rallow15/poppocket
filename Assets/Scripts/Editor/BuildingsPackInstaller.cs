#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// salim (03/10) : « je veux plus les maison gas station » — plus AUCUN
/// décor de bâtiments autour de l'arène : ni le village de cubes
/// ("MaisonsVillage"), ni les Garage/Station-service du pack
/// "Cartoon Buildings" ("Batiments").
/// L'ancien installateur qui les plaçait a été retiré : ce script
/// SUPPRIME ce qui traîne encore dans la scène, une fois, et ne pose
/// plus jamais rien.
/// </summary>
[InitializeOnLoad]
public static class BuildingsPackInstaller
{
    static BuildingsPackInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("POPPOCKET/4. Retirer les maisons & bâtiments")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        if (GameUpgradeInstaller.EnsureGameSceneOpen()) return;

        bool removed = false;

        // Les stations/garages posés par l'ancien installateur
        var bat = GameObject.Find("Batiments");
        if (bat != null)
        {
            Object.DestroyImmediate(bat);
            removed = true;
        }

        // Le village de maisons-cubes (s'il revient jamais)
        var village = GameObject.Find("MaisonsVillage");
        if (village != null)
        {
            Object.DestroyImmediate(village);
            removed = true;
        }

        if (removed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[BATIMENTS-PACK] Maisons et gas stations retirées de la scène ✔");
        }
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