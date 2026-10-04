#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Installe TOUT ce que salim a demandé (idempotent, tout en code) :
///   1. (REtiré 03/10) plus de MAISONS ni de bâtiments autour de l'arène
///      — voir BuildingsPackInstaller qui nettoie la scène
///   2. ANIMAUX qui se promènent autour de l'arène (script Wanderer)
///   3. MENU DE DÉPART (objet StartMenu : écran POP POCKET + bouton JOUER)
///   4. HUD POWER-UP (objet PowerUpHud : secondes qui défilent + qui l'a pris)
///   5. SAFE AREA : le chrono descend sous la Dynamic Island
/// S'exécute tout seul au démarrage de l'éditeur (comme DragInputInstaller).
/// </summary>
[InitializeOnLoad]
public static class GameUpgradeInstaller
{
    static GameUpgradeInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    /// <summary>
    /// En mode batch (-executeMethod), Unity ouvre une scène VIDE "Untitled"
    /// (compte comme chargée !) : si elle n'a pas de chemin, on ouvre la vraie
    /// scène de jeu des Build Settings. Retourne true si on doit s'arrêter
    /// (aucune scène trouvable).
    /// </summary>
    internal static bool EnsureGameSceneOpen()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(scene.path)) return false;   // déjà OK

        if (EditorBuildSettings.scenes.Length == 0)
        {
            Debug.LogWarning("[MAISON-DECOR] Aucune scène en Build Settings — rien à faire");
            return true;
        }
        scene = EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path,
                                             OpenSceneMode.Single);
        Debug.Log("[MAISON-DECOR] Scène de jeu ouverte : " + scene.path);
        return false;
    }

    public static void Run()
    {
        if (EditorApplication.isPlaying) return;   // rien pendant le jeu
        if (EnsureGameSceneOpen()) return;

        BuildVillage();
        BuildAnimaux();
        InstallStartMenu();
        InstallPowerHud();
        FitTopUiToSafeArea();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    private static float ArenaHalf()
    {
        var spawner = Object.FindFirstObjectByType<BubbleSpawner>();
        return spawner != null ? spawner.arenaSize.x * 0.5f : 14f;
    }

    /// <summary>Demi-taille de l'arène, lisible par les autres installers.</summary>
    internal static float ArenaHalfForDecor() => ArenaHalf();

    // ────────────────────────────────────────────────────────────────
    //  1. LE VILLAGE : MAISONS AUTOUR DE L'ARÈNE
    // ────────────────────────────────────────────────────────────────
    private static void BuildVillage()
    {
        // salim (03/10) : « je veux plus les maison gas station » — plus
        // AUCUN décor de bâtiments : ni ce village de cubes, ni les bâtiments
        // du pack. Le nettoyage (suppression de ce qui reste dans la scène)
        // est fait par BuildingsPackInstaller.Run(), qui tourne aussi au
        // chargement de l'éditeur. On ne construit plus rien ici.
    }

    private static void BuildVillage_OLD()
    {
        // Les vrais bâtiments du pack sont là ("Batiments") : on garde,
        // on ne remet PAS les maisons-cubes par-dessus.
        if (GameObject.Find("MaisonsVillage") != null || GameObject.Find("Batiments") != null) return;

        var root = new GameObject("MaisonsVillage");
        root.transform.SetParent(null);

        // Couleurs de murs pastel (une maison = une couleur)
        Color[] wallColors =
        {
            new Color(1.00f, 0.85f, 0.60f),   // jaune paille
            new Color(0.65f, 0.85f, 0.62f),   // vert d'eau
            new Color(0.95f, 0.70f, 0.60f),   // pêche
            new Color(0.70f, 0.80f, 0.95f),   // bleu ciel
            new Color(0.92f, 0.65f, 0.75f),   // rose
            new Color(0.95f, 0.90f, 0.80f),   // crème
            new Color(0.80f, 0.72f, 0.95f),   // lilas
            new Color(0.88f, 0.88f, 0.86f)    // blanc cassé
        };
        Color roofColor = new Color(0.72f, 0.30f, 0.22f);   // terre cuite

        float half = ArenaHalf();
        int count = 10;   // 10 maisons autour de l'arène

        for (int i = 0; i < count; i++)
        {
            float angle = (i * (360f / count) + Random.Range(-9f, 9f)) * Mathf.Deg2Rad;
            float r = Random.Range(half + 4.5f, half + 8.5f);   // hors arène
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            var house = new GameObject("Maison_" + i);
            house.transform.SetParent(root.transform, false);
            house.transform.position = pos;
            // La façade (porte + fenêtres) regarde le centre de l'arène
            house.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 180f, 0f);

            // --- Corps de la maison ---
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Mur";
            body.transform.SetParent(house.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            body.transform.localScale = new Vector3(3.4f, 2.2f, 3.0f);
            body.GetComponent<MeshRenderer>().sharedMaterial =
                WallMat(wallColors[i % wallColors.Length]);   // les couleurs tournent
            DestroyColl(body);

            // --- Toit (cube tourné 45° = silhouette pointue) ---
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Toit";
            roof.transform.SetParent(house.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            roof.transform.localScale = new Vector3(2.55f, 1.45f, 2.55f);
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            roof.GetComponent<MeshRenderer>().sharedMaterial =
                GetOrCreateMat("MatToit_" + (i % 3),
                    Color.Lerp(roofColor, wallColors[i % wallColors.Length], 0.15f + (i % 3) * 0.03f));
            DestroyColl(roof);

            // --- Porte (sur la façade, côté arène = +z local) ---
            AddDeco(house, "Porte", new Vector3(0f, 0.65f, 1.53f),
                    new Vector3(0.8f, 1.3f, 0.1f), new Color(0.45f, 0.30f, 0.18f));

            // --- Deux fenêtres ---
            AddDeco(house, "Fenetre_G", new Vector3(-0.95f, 1.35f, 1.53f),
                    new Vector3(0.55f, 0.55f, 0.08f), new Color(0.75f, 0.90f, 1.00f));
            AddDeco(house, "Fenetre_D", new Vector3(0.95f, 1.35f, 1.53f),
                    new Vector3(0.55f, 0.55f, 0.08f), new Color(0.75f, 0.90f, 1.00f));

            // --- Cheminée ---
            AddDeco(house, "Cheminee", new Vector3(0.9f, 3.05f, -0.4f),
                    new Vector3(0.35f, 0.8f, 0.35f), new Color(0.55f, 0.35f, 0.30f));
        }

        Debug.Log("[MAISON-DECOR] Village installé : " + count + " maisons autour de l'arène ✔");
    }

    /// <summary>Petit décor plat fixé à la maison (porte, fenêtre, cheminée).</summary>
    private static void AddDeco(GameObject house, string name, Vector3 pos,
                                Vector3 scale, Color color)
    {
        GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cube);
        d.name = name;
        d.transform.SetParent(house.transform, false);
        d.transform.localPosition = pos;
        d.transform.localScale = scale;
        d.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMat("MatDeco_" + name, color);
        DestroyColl(d);
    }

    // ────────────────────────────────────────────────────────────────
    //  2. LES ANIMAUX (ils se promènent avec le script Wanderer)
    // ────────────────────────────────────────────────────────────────
    private static void BuildAnimaux()
    {
        if (GameObject.Find("Animaux") != null) return;

        var root = new GameObject("Animaux");
        root.transform.SetParent(null);

        // Couleurs d'animaux : vache, cochon, mouton, chien, poule...
        Color[] pelages =
        {
            new Color(0.95f, 0.94f, 0.90f),   // blanc (mouton)
            new Color(0.66f, 0.47f, 0.33f),   // marron (chien)
            new Color(0.95f, 0.65f, 0.72f),   // rose (cochon)
            new Color(0.25f, 0.23f, 0.22f),   // noir (chat)
            new Color(1.00f, 0.55f, 0.35f)    // roux (renard)
        };
        Color muselage = new Color(0.35f, 0.25f, 0.20f);   // museau commun

        int count = 12;
        float half = ArenaHalf();

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Random.Range(half + 2.5f, half + 7.5f);   // anneau hors arène
            Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            var animal = new GameObject("Animal_" + i);
            animal.transform.SetParent(root.transform, false);
            animal.transform.position = pos;
            animal.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Color pelage = pelages[Random.Range(0, pelages.Length)];

            // --- Corps + tête + oreilles (low-poly cubes) ---
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Corps";
            body.transform.SetParent(animal.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.9f, 0.55f, 1.3f);
            body.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMat("MatAniCorps_" + ColorUtility.ToHtmlStringRGB(pelage), pelage);
            DestroyColl(body);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Tete";
            head.transform.SetParent(animal.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.85f, 0.75f);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            head.GetComponent<MeshRenderer>().sharedMaterial = body.GetComponent<MeshRenderer>().sharedMaterial;
            DestroyColl(head);

            // Oreilles
            AddAnimalPart(animal, "Oreille_G", new Vector3(-0.15f, 1.08f, 0.75f),
                          new Vector3(0.08f, 0.14f, 0.08f), pelage);
            AddAnimalPart(animal, "Oreille_D", new Vector3(0.15f, 1.08f, 0.75f),
                          new Vector3(0.08f, 0.14f, 0.08f), pelage);

            // Museau
            AddAnimalPart(animal, "Museau", new Vector3(0f, 0.82f, 1.02f),
                          new Vector3(0.24f, 0.2f, 0.12f), muselage);

            // Queue
            AddAnimalPart(animal, "Queue", new Vector3(0f, 0.68f, -0.7f),
                          new Vector3(0.1f, 0.1f, 0.3f), pelage);

            // --- Comportement de balade (script de jeu, pas d'éditeur) ---
            var wanderer = animal.AddComponent<Wanderer>();
            wanderer.ringRadius = new Vector2(half + 2.0f, half + 8.0f);
            wanderer.speed = Random.Range(0.6f, 1.4f);
        }

        Debug.Log("[ANIMAUX-DECOR] " + count + " animaux lâchés autour de l'arène ✔");
    }

    private static void AddAnimalPart(GameObject parent, string name,
        Vector3 pos, Vector3 scale, Color color)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = name;
        p.transform.SetParent(parent.transform, false);
        p.transform.localPosition = pos;
        p.transform.localScale = scale;
        p.GetComponent<MeshRenderer>().sharedMaterial =
            GetOrCreateMat("MatAniCorps_" + ColorUtility.ToHtmlStringRGB(color), color);
        DestroyColl(p);
    }

    // ────────────────────────────────────────────────────────────────
    //  3. MENU DE DÉPART (POCKET + bouton JOUER)
    // ────────────────────────────────────────────────────────────────
    private static void InstallStartMenu()
    {
        if (Object.FindFirstObjectByType<StartMenu>() != null) return;

        GameObject go = new GameObject("StartMenuController");
        go.AddComponent<StartMenu>();
        Debug.Log("[MENU-START] Objet StartMenuController posé dans la scène ✔");
    }

    // ────────────────────────────────────────────────────────────────
    //  4. HUD DES POWER-UPS (secondes qui défilent + qui l'a activé)
    // ────────────────────────────────────────────────────────────────
    private static void InstallPowerHud()
    {
        if (Object.FindFirstObjectByType<PowerUpHud>() != null) return;

        GameObject go = new GameObject("PowerUpHudObject");
        go.AddComponent<PowerUpHud>();
        Debug.Log("[POWER-HUD] HUD des bonus installé ✔ (secondes + nom du joueur)");
    }

    // ────────────────────────────────────────────────────────────────
    //  5. SAFE AREA : chrono et scores passent sous la Dynamic Island
    // ────────────────────────────────────────────────────────────────
    private static void FitTopUiToSafeArea()
    {
        bool changed = false;

        // Chrono : on le descend déjà un peu (base), + adapt. auto (SafeAreaFitter)
        GameObject timer = GameObject.Find("TimerText");
        if (timer != null)
        {
            var rt = timer.GetComponent<RectTransform>();
            if (rt != null && !Mathf.Approximately(rt.anchoredPosition.y, -110f))
            {
                Undo.RecordObject(rt, "Chrono sous Dynamic Island");
                rt.anchoredPosition = new Vector2(0f, -110f);
                changed = true;
            }
            if (timer.GetComponent<SafeAreaFitter>() == null)
            {
                Undo.AddComponent<SafeAreaFitter>(timer);
                changed = true;
            }
        }

        // Scores (bloc en haut à droite aussi)
        GameObject scoreBlock = GameObject.Find("ScoreBlock");
        if (scoreBlock != null)
        {
            var rt = scoreBlock.GetComponent<RectTransform>();
            if (rt != null && !Mathf.Approximately(rt.anchoredPosition.y, -85f))
            {
                Undo.RecordObject(rt, "Scores sous Dynamic Island");
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -85f);
                changed = true;
            }
            if (scoreBlock.GetComponent<SafeAreaFitter>() == null)
            {
                Undo.AddComponent<SafeAreaFitter>(scoreBlock);
                changed = true;
            }
        }

        if (changed)
            Debug.Log("[SAFE-AREA] Chrono + scores descendus (Dynamic Island iPhone) ✔");
    }

    // ────────────────────────────────────────────────────────────────
    //  OUTILS
    // ────────────────────────────────────────────────────────────────
    private static void DestroyColl(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);   // décor = jamais collé
    }

    private static Material WallMat(Color color)
    {
        return GetOrCreateMat("MatMaison_" + ColorUtility.ToHtmlStringRGB(color), color);
    }

    /// <summary>Matériau URP/Lit à la couleur voulue (asset .mat, réutilisé si déjà créé).</summary>
    private static Material GetOrCreateMat(string name, Color color)
    {
        string dir = "Assets/Materials/Deco";
        if (!System.IO.Directory.Exists(dir))
            AssetDatabase.CreateFolder("Assets/Materials", "Deco");

        string path = dir + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = name };
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
#endif