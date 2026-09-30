#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// REND LE DECOR PLUS DENSE : ajoute plein d'arbres, rochers, buissons,
/// fleurs et nuages AUTOUR de l'arène (sur le plan d'herbe de 70 m, en dehors
/// des murs invisibles → ne gêne jamais le jeu). Style cartoon identique aux
/// arbres existants (troncs cylindres + boules de feuillage).
/// Idempotent : ne fait rien si "DecoDense" existe déjà.
/// </summary>
[InitializeOnLoad]
public static class DenseDecorInstaller
{
    static DenseDecorInstaller()
    {
        EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        if (EditorApplication.isPlaying) return; // ne rien faire pendant le jeu
        if (GameObject.Find("GrassPlane") == null) return; // le sol Roblox n'existe pas encore

        // Déjà installé → on ne re-remplit pas le champ
        if (GameObject.Find("DecoDense") != null) return;

        var grass = GameObject.Find("GrassPlane").transform;

        // Racine de tout le nouveau décor
        GameObject root = new GameObject("DecoDense");
        Undo.RegisterCreatedObjectUndo(root, "Decor dense");

        int trees = 0, rocks = 0, bushes = 0, flowers = 0;

        // ── 1) Plein d'arbres tout autour de l'arène ──
        for (int i = 0; i < 28; i++)
        {
            Vector3 pos = RandomOutsideArena(17f);
            float s = Random.Range(0.7f, 1.6f);            // tailles variées
            float trunkH = Random.Range(1.6f, 2.8f);
            Transform t = MakeTree(root.transform, pos, trunkH);
            t.localScale = Vector3.one * s;
            t.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            trees++;
        }

        // ── 2) Rochers gris arrondis ──
        Material rock = MakeMat("MatRock", new Color(0.62f, 0.62f, 0.66f), 0.25f);
        for (int i = 0; i < 16; i++)
        {
            Vector3 pos = RandomOutsideArena(16f);
            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(r);
            r.name = "Rock";
            r.transform.SetParent(root.transform, false);
            float s = Random.Range(0.5f, 1.4f);
            r.transform.localScale = new Vector3(s, s * 0.65f, s); // pierre aplatie
            r.transform.position = pos + Vector3.down * (s * 0.15f);
            r.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            r.GetComponent<MeshRenderer>().sharedMaterial = rock;
            rocks++;
        }

        // ── 3) Petits buissons (boules vertes bien rapprochées) ──
        Material leaf = MakeMat("MatLeaf", new Color(0.30f, 0.75f, 0.35f), 0.5f);
        for (int i = 0; i < 16; i++)
        {
            Vector3 pos = RandomOutsideArena(16f);
            GameObject bush = new GameObject("Bush");
            bush.transform.SetParent(root.transform, false);
            bush.transform.position = pos;
            float s = Random.Range(0.7f, 1.3f);
            bush.transform.localScale = Vector3.one * s;
            for (int b = 0; b < 3; b++)
            {
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                StripCollider(ball);
                ball.name = "Leaf" + b;
                ball.transform.SetParent(bush.transform, false);
                ball.transform.localPosition = new Vector3(
                    Random.Range(-0.35f, 0.35f), Random.Range(0.15f, 0.45f), Random.Range(-0.35f, 0.35f));
                ball.transform.localScale = Vector3.one * Random.Range(0.55f, 0.85f);
                ball.GetComponent<MeshRenderer>().sharedMaterial = leaf;
            }
            bushes++;
        }

        // ── 4) Petites fleurs colorées (tige + tête) ──
        Color[] flowerColors =
        {
            new Color(1f, 0.55f, 0.75f),  // rose
            new Color(1f, 0.85f, 0.30f),  // jaune
            new Color(1f, 1f, 1f),        // blanche
            new Color(0.60f, 0.45f, 1f),  // violette
        };
        Material stem = MakeMat("MatStem", new Color(0.25f, 0.62f, 0.30f), 0.4f);
        Material[] petals =
        {
            MakeMat("MatFlower0", flowerColors[0], 0.6f),
            MakeMat("MatFlower1", flowerColors[1], 0.6f),
            MakeMat("MatFlower2", flowerColors[2], 0.6f),
            MakeMat("MatFlower3", flowerColors[3], 0.6f),
        };
        for (int i = 0; i < 30; i++)
        {
            Vector3 pos = RandomOutsideArena(16f);
            GameObject fl = new GameObject("Flower");
            fl.transform.SetParent(root.transform, false);
            fl.transform.position = pos;

            GameObject st = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(st);
            st.name = "Stem";
            st.transform.SetParent(fl.transform, false);
            st.transform.localScale = new Vector3(0.09f, 0.22f, 0.09f);
            st.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            st.GetComponent<MeshRenderer>().sharedMaterial = stem;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(head);
            head.name = "Head";
            head.transform.SetParent(fl.transform, false);
            head.transform.localScale = Vector3.one * 0.30f;
            head.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            head.GetComponent<MeshRenderer>().sharedMaterial = petals[i % petals.Length];
            flowers++;
        }

        // ── 5) Nuages ronds dans le ciel (comme dans les dessins animés) ──
        Material cloud = MakeMat("MatCloud", new Color(1f, 1f, 1f), 0.8f);
        for (int i = 0; i < 8; i++)
        {
            GameObject cl = new GameObject("Cloud");
            cl.transform.SetParent(root.transform, false);
            float ang = i * 45f + Random.Range(-20f, 20f);
            float dist = Random.Range(8f, 26f);
            cl.transform.position = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * dist,
                Random.Range(13f, 20f), Mathf.Sin(ang * Mathf.Deg2Rad) * dist);
            float big = Random.Range(3f, 5.5f);
            for (int b = 0; b < 4; b++)
            {
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                StripCollider(ball);
                ball.name = "Puff" + b;
                ball.transform.SetParent(cl.transform, false);
                ball.transform.localPosition = new Vector3(
                    (b - 1.5f) * big * 0.42f, Random.Range(-0.1f, 0.25f) * big, Random.Range(-0.2f, 0.2f) * big);
                ball.transform.localScale = Vector3.one * big * Random.Range(0.55f, 0.8f);
                ball.GetComponent<MeshRenderer>().sharedMaterial = cloud;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[DECOR-DENSE] Decor plus dense ✔ : " + trees + " arbres, " +
                  rocks + " rochers, " + bushes + " buissons, " + flowers +
                  " fleurs, 8 nuages — autour de l'arene");
    }

    // ────────────────────────────────────────────────────────────────
    //  Aides
    // ────────────────────────────────────────────────────────────────

    /// <summary>Point au hasard SUR l'herbe mais HORS de l'arène (+marge).</summary>
    private static Vector3 RandomOutsideArena(float inner)
    {
        for (int tries = 0; tries < 40; tries++)
        {
            float x = Random.Range(-32f, 32f);
            float z = Random.Range(-32f, 32f);
            // hors de l'arène (on garde une marge au-delà des murs invisibles)
            if (Mathf.Abs(x) < inner && Mathf.Abs(z) < inner) continue;
            // reste sur le plan d'herbe (70 m de côté)
            if (Mathf.Abs(x) > 33f || Mathf.Abs(z) > 33f) continue;
            return new Vector3(x, 0f, z);
        }
        return new Vector3(30f, 0f, 30f);
    }

    /// <summary>Arbre cartoon identique à ceux déjà en scène (tronc + 3 boules).</summary>
    private static Transform MakeTree(Transform parent, Vector3 at, float trunkHeight)
    {
        GameObject tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = at;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        StripCollider(trunk);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localScale = new Vector3(0.38f, trunkHeight, 0.38f);
        trunk.transform.localPosition = new Vector3(0f, trunkHeight, 0f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial =
            MakeMat("MatTrunk", new Color(0.48f, 0.32f, 0.20f), 0.3f);

        Vector3[] leafPos =
        {
            new Vector3(0f, trunkHeight * 2f + 0.8f, 0f),
            new Vector3(1.0f, trunkHeight * 2f + 0.3f, 0.3f),
            new Vector3(-0.8f, trunkHeight * 2f + 0.4f, -0.4f),
        };
        float[] leafSize = { 2.4f, 2.0f, 1.8f };
        Material leafMat = MakeMat("MatLeaf", new Color(0.30f, 0.75f, 0.35f), 0.5f);
        for (int i = 0; i < 3; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(leaf);
            leaf.name = "Leaf" + i;
            leaf.transform.SetParent(tree.transform, false);
            leaf.transform.localPosition = leafPos[i];
            leaf.transform.localScale = Vector3.one * leafSize[i];
            leaf.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
        }
        return tree.transform;
    }

    /// <summary>Décor = visuel seulement : on retire les colliders fantômes.</summary>
    private static void StripCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
    }

    /// <summary>Crée (une fois) un matériau URP dans Assets/Materials.</summary>
    private static Material MakeMat(string name, Color color, float smoothness)
    {
        const string folder = "Assets/Materials";
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        System.IO.Directory.CreateDirectory(folder);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
#endif