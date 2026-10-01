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
        EditorApplication.delayCall += () =>
        {
            Run();
            // Photo manquante → on la refait (utile quand Unity reste ouvert
            // pendant qu'on modifie les scripts sur disque)
            if (!File.Exists("Builds/SlimePreview_Front.png")) CapturePreview();
        };
    }

    [MenuItem("POPPOCKET/5. Slimes du pack (Bureau)")]
    public static void RunFromMenu() => Run();

    /// <summary>Comme Run(), mais prend en plus une photo des slimes habillés
    /// → Builds/SlimePreview.png (pour vérifier le pose au sol et les yeux).</summary>
    [MenuItem("POPPOCKET/Debug. Photo des slimes")]
    public static void RunAndCapture()
    {
        if (EditorApplication.isPlaying) return;
        Run();
        CapturePreview();
    }

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
            CapturePreview();   // photo de contrôle (Builds/SlimePreview.png)
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

            changed |= FitVisual(vis);            // remplir le collider, posé au sol
            changed |= StripVisualPhysics(vis);   // la physique reste celle du slime
            changed |= ApplyMaterials(vis, colors, eyes);

            // L'animation "gelée" par code (le FBX du pack n'a aucun clip)
            var wobble = contents.GetComponent<SlimeWobble>();
            if (wobble == null) { wobble = contents.AddComponent<SlimeWobble>(); changed = true; }
            wobble.visual = vis;   // toujours re-ligné (le transform peut changer)

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

    /// <summary>
    /// Rescale le modèle pour remplir le collider local (diamètre 1), posé au sol.
    /// Mesure les bounds COIN PAR COIN dans l'espace LOCAL DU VISUEL :
    /// le modèle a 2 rendus (corps + yeux enfants à des offsets différents) —
    /// mélanger des bounds locaux d'espaces différents donnait un mauvais
    /// cadrage (bug « les slimes flottent ») ; et mesurer dans le MONDE
    /// mélangeait l'échelle du root (joueur 2, bots 1.5) → le slime bleu
    /// devenait 2× plus petit que son collider.
    /// Idempotent : on repart toujours du modèle brut (échelle/position/rotation 1/0/0).
    /// </summary>
    static bool FitVisual(Transform vis)
    {
        vis.localPosition = Vector3.zero;
        vis.localRotation = Quaternion.identity;
        vis.localScale = Vector3.one;        // état brut, mesure exacte

        Bounds b = new Bounds(); bool any = false;
        foreach (Renderer r in vis.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = GetMesh(r);
            if (mesh == null) continue;
            foreach (Vector3 corner in MeshCorners(mesh))
                EncapsulateWorld(ref b, ref any,
                    vis.InverseTransformPoint(r.transform.TransformPoint(corner)));
        }
        if (!any) return false;

        Debug.Log("[SLIMES-FIT] " + vis.name + " : modèle complet mesuré " +
                  b.size.ToString("F3") + " (bas du modèle y=" + b.min.y.ToString("F3") + ")");

        float scale = 1f / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.y, b.size.z));
        var newScale = Vector3.one * scale;
        var newPos = new Vector3(
            -b.center.x * scale,
            -0.5f - b.min.y * scale,   // bas du modèle = bas du collider (y = -0.5)
            -b.center.z * scale);
        Debug.Log("[SLIMES-FIT] " + vis.name + " → échelle " + scale.ToString("F4") +
                  ", pos " + newPos.ToString("F4"));
        // Ne re-sauvegarde le préfab QUE si les valeurs ont vraiment changé
        bool changed = vis.localScale != newScale || vis.localPosition != newPos;
        vis.localScale = newScale;
        vis.localPosition = newPos;
        return changed;
    }

    /// <summary>Les 8 coins d'une bounds de mesh (espace local du mesh).</summary>
    static Vector3[] MeshCorners(Mesh m)
    {
        var ext = m.bounds.extents;
        var ctr = m.bounds.center;
        var pts = new Vector3[8];
        int i = 0;
        for (int dx = 0; dx <= 1; dx++)
            for (int dy = 0; dy <= 1; dy++)
                for (int dz = 0; dz <= 1; dz++)
                    pts[i++] = ctr + new Vector3(
                        (dx == 0 ? -1f : 1f) * ext.x,
                        (dy == 0 ? -1f : 1f) * ext.y,
                        (dz == 0 ? -1f : 1f) * ext.z);
        return pts;
    }

    static void EncapsulateWorld(ref Bounds b, ref bool any, Vector3 p)
    {
        if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
        else b.Encapsulate(p);
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

    // ────────────────────────────────────────────────────────────────
    //  PHOTO DE VÉRIFICATION (Builds/SlimePreview_Front.png + _Side.png)
    //  Scène vide additive : plan + lumière + les 2 slimes → rendu PNG.
    //  TOUT est posé très loin de l'arène (~x=400) pour qu'aucun décor du
    //  du jeu ne puisse apparaître sur la photo. 2 vues :
    //    • FRONT  : on voit les yeux (orientation du modèle),
    //    • PROFIL : au niveau des yeux, preuve que le slime TOUCHE le sol.
    // ────────────────────────────────────────────────────────────────
    static void CapturePreview()
    {
        Directory.CreateDirectory("Builds");
        string old = "Builds/SlimePreview.png";
        if (File.Exists(old)) File.Delete(old);   // ancienne photo unique

        var added = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        try
        {
            // Studio photo posé très loin (~x = 400) : l'arène est à l'origine,
            // rien d'autre que ce studio ne peut rentrer dans le cadre.
            var root = new GameObject("StudioPhoto");
            root.transform.position = new Vector3(400f, 0f, 400f);
            var rig = new GameObject("Rig");
            rig.transform.SetParent(root.transform, false);

            // Sol + lumière
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(rig.transform, false);
            ground.transform.localScale = Vector3.one * 6f;
            var gm = new Material(Shader.Find("Standard")) { color = new Color(0.72f, 0.75f, 0.7f) };
            ground.GetComponent<Renderer>().sharedMaterial = gm;

            RenderSettings.fog = false;               // plus de voile blanc
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.65f, 0.75f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.42f, 0.38f);

            var sunGo = new GameObject("Lumiere");
            sunGo.transform.SetParent(rig.transform, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Les 2 slimes habillés, posés au sol comme en jeu (centre collider à 0.5)
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerSlimePrefab.prefab");
            var bot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BotSlimePrefab.prefab");
            if (player == null || bot == null)
            {
                Debug.LogWarning("[SLIMES-PHOTO] Préfabs introuvables, pas de photo");
                return;
            }
            var p1 = (GameObject)PrefabUtility.InstantiatePrefab(player);
            p1.transform.SetParent(rig.transform, false);
            p1.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var p2 = (GameObject)PrefabUtility.InstantiatePrefab(bot);
            p2.transform.SetParent(rig.transform, false);
            p2.transform.localPosition = new Vector3(1.15f, 0.5f, 0.35f);
            // Les yeux des 2 slimes tournés face à la caméra (comme à l'arrêt en jeu)
            foreach (var slime in new[] { p1, p2 })
            {
                var vis = slime.transform.Find("VisualSlime");
                if (vis != null) vis.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Caméra
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.45f, 0.58f);

            // ── Vue 1 : FACE (les yeux visibles) ──
            camGo.transform.localPosition = new Vector3(0.55f, 1.35f, -2.2f);
            // LookAt vise en coordonnées MONDE : le studio est à x=400,
            // donc on convertit la cible locale vers le monde
            camGo.transform.LookAt(root.transform.TransformPoint(new Vector3(0.5f, 0.42f, 0f)));
            cam.fieldOfView = 42f;
            Shoot(cam, "Builds/SlimePreview_Front.png");

            // ── Vue 2 : PROFIL (au niveau des yeux — prouve le contact au sol) ──
            camGo.transform.localPosition = new Vector3(1.05f, 0.62f, 5.0f);
            camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // regarde −Z
            cam.fieldOfView = 30f;
            Shoot(cam, "Builds/SlimePreview_Side.png");
        }
        finally
        {
            EditorSceneManager.CloseScene(added, true);
        }
    }

    static void Shoot(Camera cam, string outPath)
    {
        var rt = new RenderTexture(1000, 750, 24);
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(1000, 750, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1000, 750), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(tex);
        Debug.Log("[SLIMES-PHOTO] Vue sauvée : " + outPath + " ✔");
    }
}
#endif