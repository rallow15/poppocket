#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Remplace les slimes low-poly actuels (Studio Nik) par le modèle du pack
/// « Slime » de Games by McSteeg, déjà sur le PC de salim
/// (C:\Users\salim\Desktop\Slime) : Slime_fbx.fbx + textures de couleur.
/// Un seul modèle, 4 couleurs de corps → bleu = joueur,
/// vert / orange / blanc = les 3 bots. Yeux séparés (Slime_Eyes).
/// Tout le gameplay (SlimeController, Rigidbody, collider) est conservé :
/// on ne change QUE le visuel. Idempotent.
/// </summary>
[InitializeOnLoad]
public static class SlimeModelInstaller
{
    const string PackSource = @"C:\Users\salim\Desktop\Slime";
    const string PackFolder = "Assets/SlimePack";

    static SlimeModelInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("POPPOCKET/5. Slimes du pack (Bureau)")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        if (GameUpgradeInstaller.EnsureGameSceneOpen()) return;

        // ── 1. Copier le pack du Bureau dans le projet (une fois) ──
        CopyPackIntoProject();

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PackFolder + "/Slime_fbx.fbx");
        if (model == null)
        {
            Debug.LogWarning(
                "[SLIMES-NOUVEAUX] Dossier du pack introuvable sur le Bureau (" + PackSource +
                ") — les slimes actuels sont conservés, rien ne casse");
            return;
        }

        // ── 2. Matériaux : les 4 couleurs de corps + les yeux ──────
        Material eyes    = MakeSlimeMat("MatSlimeYeux",  "Slime_Eyes",  0.75f);
        Material blue    = MakeSlimeMat("MatSlime_Bleu",   "Slime_Blue",   0.5f);
        Material green   = MakeSlimeMat("MatSlime_Vert",   "Slime_Green",  0.5f);
        Material orange  = MakeSlimeMat("MatSlime_Orange", "Slime_Orange", 0.5f);
        Material white   = MakeSlimeMat("MatSlime_Blanc",  "Slime_White",  0.55f);
        if (eyes == null || blue == null) return;
        var colors = new[] { blue, green, orange, white };
        AssetDatabase.SaveAssets();

        // ── 3. Habiller les 2 préfabs (le joueur + les bots) ──────
        bool changed = false;
        changed |= DressSlime("Assets/Prefabs/PlayerSlimePrefab.prefab", model, colors, eyes);
        changed |= DressSlime("Assets/Prefabs/BotSlimePrefab.prefab", model, colors, eyes);

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SLIMES-NOUVEAUX] Slimes remplacés par le modèle McSteeg ✔" +
                      " (bleu = toi, vert/orange/blanc = bots) — APPUIE SUR PLAY ! 🟦🟩🟧⬜");
        }
        else
        {
            Debug.Log("[SLIMES-NOUVEAUX] Slimes déjà à jour ✔");
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  COPIE DU PACK (Bureau → projet)
    // ────────────────────────────────────────────────────────────────
    static void CopyPackIntoProject()
    {
        if (File.Exists(PackFolder + "/Slime_fbx.fbx")) return;   // déjà copié

        if (!Directory.Exists(PackSource))
        {
            Debug.LogWarning("[SLIMES-NOUVEAUX] Dossier introuvable : " + PackSource);
            return;
        }

        Directory.CreateDirectory(PackFolder);
        Directory.CreateDirectory(PackFolder + "/Textures");

        File.Copy(PackSource + "\\Slime_fbx.fbx", PackFolder + "/Slime_fbx.fbx");

        // Toutes les textures utiles (les .png ; pas les sources Blender/psd)
        foreach (string file in Directory.GetFiles(PackSource + "\\Textures", "*.png"))
        {
            string dest = PackFolder + "/Textures/" + Path.GetFileName(file);
            if (!File.Exists(dest)) File.Copy(file, dest);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[SLIMES-NOUVEAUX] Modèle + textures copiés dans " + PackFolder + " ✔");
    }

    // ────────────────────────────────────────────────────────────────
    //  HABILLAGE D'UN PRÉFAB
    // ────────────────────────────────────────────────────────────────
    static bool DressSlime(string prefabPath, GameObject model, Material[] colors, Material eyes)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (root == null || root.GetComponent<Collider>() == null) return false;

        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
        bool changed = false;

        try
        {
            // Ancien visuel low-poly (Studio Nik) : on le retire
            Transform old = contents.transform.Find("VisualLowPoly");
            if (old != null) { Object.DestroyImmediate(old.gameObject); changed = true; }

            // Le nouveau modèle du pack
            Transform vis = contents.transform.Find("VisualSlime");
            if (vis == null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                if (inst == null) inst = Object.Instantiate(model);
                inst.name = "VisualSlime";
                inst.transform.SetParent(contents.transform, false);
                vis = inst.transform;
                changed = true;
            }

            FitVisual(vis);                       // remplir le collider, posé au sol
            changed |= StripVisualPhysics(vis);   // la physique reste celle du slime
            changed |= ApplyMaterials(vis, colors, eyes);

            // La couleur change au spawn selon l'index (player / bots)
            var recolor = contents.GetComponent<SlimeRecolor>();
            if (recolor == null) { recolor = contents.AddComponent<SlimeRecolor>(); changed = true; }
            if (!SameMats(recolor.bodyByIndex, colors) || recolor.eyesMaterial != eyes)
            {
                recolor.bodyByIndex = colors;
                recolor.eyesMaterial = eyes;
                changed = true;
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
        return changed;
    }

    /// <summary>Rescale le modèle pour remplir le collider local (diamètre 1).</summary>
    static void FitVisual(Transform vis)
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool any = false;
        foreach (Renderer r in vis.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = GetMesh(r);
            if (mesh == null) continue;
            if (!any) { b = mesh.bounds; any = true; }
            else b.Encapsulate(mesh.bounds);
        }
        if (!any) return;

        float maxExtent = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float scale = 1f / Mathf.Max(0.001f, maxExtent);
        vis.transform.localScale = Vector3.one * scale;

        // Posé au sol : bas du modèle aligné avec le bas du collider (y = -0.5)
        float bottomY = (b.center.y - b.extents.y) * scale;
        vis.transform.localPosition = new Vector3(
            -(b.center.x * scale), -0.5f - bottomY, -(b.center.z * scale));
    }

    static bool StripVisualPhysics(Transform vis)
    {
        bool changed = false;
        foreach (var rb in vis.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rb);
            changed = true;
        }
        foreach (var col in vis.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
            changed = true;
        }
        return changed;
    }

    // ────────────────────────────────────────────────────────────────
    //  MATÉRIAUX SUR LE MODÈLE (corps + yeux)
    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sur chaque rendu : le slot des YEUX → matériau yeux, tout le reste →
    /// matériau du corps (la couleur de base du préfab, le jeu recolorera).
    /// Le slot des yeux est repéré par son nom, ou (2 slots) par le plus
    /// petit sous-maillage (les yeux ont toujours moins de triangles).
    /// </summary>
    static bool ApplyMaterials(Transform vis, Material[] colors, Material eyes)
    {
        Material body = colors[0];   // bleu par défaut, SlimeRecolor ajustera
        bool changed = false;

        foreach (Renderer r in vis.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = GetMesh(r);
            if (mesh == null) continue;

            var mats = r.sharedMaterials;
            var next = new Material[mats.Length];
            bool eyesNamed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                bool isEye = mats[i] != null &&
                             mats[i].name.ToLower().Contains("eye");
                if (isEye) eyesNamed = true;
                next[i] = isEye ? eyes : body;
            }

            // Aucun nom parlant : sur 2 slots, le plus petit = les yeux
            if (!eyesNamed && mats.Length == 2 && mesh.subMeshCount == 2)
            {
                long a = mesh.GetIndexCount(0);
                long b = mesh.GetIndexCount(1);
                int eyeSlot = a <= b ? 0 : 1;
                next[eyeSlot] = eyes;
            }

            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != next[i]) changed = true;
            if (changed) r.sharedMaterials = next;
        }
        return changed;
    }

    static Mesh GetMesh(Renderer r)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf != null) return mf.sharedMesh;
        var smr = r as SkinnedMeshRenderer;
        return smr != null ? smr.sharedMesh : null;
    }

    // ────────────────────────────────────────────────────────────────
    //  CRÉATION DES MATÉRIAUX (shader du projet : Standard, Built-in)
    // ────────────────────────────────────────────────────────────────
    static Material MakeSlimeMat(string name, string texName, float smoothness)
    {
        string dir = PackFolder + "/Materials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(PackFolder, "Materials");

        string path = dir + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Mobile/Diffuse");
        if (shader == null) return null;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PackFolder + "/Textures/" + texName + ".png");

        var mat = new Material(shader) { name = name };
        if (tex != null) mat.mainTexture = tex;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static bool SameMats(Material[] a, Material[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
#endif