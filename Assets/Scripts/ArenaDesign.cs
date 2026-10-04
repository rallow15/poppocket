using UnityEngine;

/// <summary>
/// DESIGN DE L'ARENE (salim 03/10 : « le fond bleu c'est trop sec, les bulles
/// sont pas intégrées, on aurait dit un truc posé comme ça »).
///
/// Idée : l'arène devient une PISCINE À BULLES, pas un sol plat bleu uni.
///   1. LE SOL du bord (tour de cuve) : bleu océan PROFOND + très brillant,
///      comme une piscine vue d'avion.
///   2. LE FOND de la cuve : un seul grand carré clair posé au centre
///      (la partie creuse de la piscine) → deux tons au lieu d'un seul bleu sec.
///   3. LES MURS : mur de PIERRE (salim 04/10 : « l'arène surélevée à peine,
///      entourée d'un mur de pierre ») — blocs gris générés en code, murs
///      un PEU plus hauts → l'arène a l'air d'un bassin posé sur la pelouse.
///   4. Plus bas dans BubbleSpawner : chaque bulle gagne une OMBRE SOFT
///      dessous + s'enfonce un peu dans le sol → elle vit DANS le tapis,
///      elle n'est plus posée dessus.
///
/// 100 % en code, BUILT-IN pipeline, AUCUNE texture image au sol : que des
/// couleurs de matériaux fait maison. Jamais de .mat sur disque : uniquement
/// des clones EN MÉMOIRE des matériaux de la scène (règle du projet).
/// Lançé tout seul quand la scène charge — rien à brancher.
/// </summary>
public static class ArenaDesign
{
    /// <summary>Material de l'ombre SOFT sous les bulles (demandé par le spawner).</summary>
    private static Material matOmbre;

    // ────────────────────────────────────────────────────────────────
    // FIX IPHONE (salim 04/10) : RuntimeInitializeOnLoadMethod(AfterSceneLoad)
    // ne tourne qu'UNE fois par lancement de l'app. Au Rejouer (LoadScene),
    // le reload détruisait notre décor (horizon, brume, arbres reculés)
    // et rien ne le refaisait. On se branche maintenant sur le chargement
    // de CHAQUE scène → l'arène est redessinée à chaque partie.
    // ────────────────────────────────────────────────────────────────
    static ArenaDesign()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Appliquer();
    }

    // Ce hook oblige Unity à toucher la classe AVANT le chargement de la
    // première scène → l'abonnement au-dessus est posé à temps.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ForcerInit() { }   // le static ctor fait le travail

    private static void Appliquer()
    {
        GameObject sol = GameObject.Find("Arena");
        if (sol == null) return;   // pas cette scène : on sort discrètement

        Renderer rendSol = sol.GetComponent<Renderer>();
        if (rendSol == null) rendSol = sol.GetComponentInChildren<Renderer>();
        if (rendSol == null || rendSol.sharedMaterial == null) return;

        Shader shaderEau = Shader.Find("PopPocket/EauStylisee");
        Material matSol;

        if (shaderEau != null)
        {
            // ── 1) LE SOL : VRAIE EAU ANIMÉE (shader maison, voir
            //     PopPocketEauStylisee.shader) — salim 03/10 : remplacer
            //     le fond de l'arène par un water stylisé, comme le pack
            //     « One Click Add Water » qui est URP-only et mort ici.
            matSol = new Material(shaderEau);          // instance mémoire
            matSol.SetColor("_CouleurCentre", new Color(0.30f, 0.76f, 0.93f));
            matSol.SetColor("_CouleurBord",   new Color(0.19f, 0.62f, 0.86f));
            matSol.SetFloat("_Echelle",      2.2f);    // vagues bien visibles
            matSol.SetFloat("_Vitesse",      0.28f);
            matSol.SetFloat("_ForceReflets", 0.40f);
            matSol.name = "ArenaEauSol";
        }
        else
        {
            // Fallback (shader pas encore compilé/importé) : l'ancien
            // azzurro Standard, comme avant. Rien ne casse jamais.
            matSol = new Material(rendSol.sharedMaterial);   // clone mémoire
            matSol.color = new Color(0.24f, 0.70f, 0.92f);
            if (matSol.HasProperty("_Smoothness")) matSol.SetFloat("_Smoothness", 0.85f);
            if (matSol.HasProperty("_Metallic")) matSol.SetFloat("_Metallic", 0f);
            matSol.name = "ArenaAqua";
            Debug.LogWarning("[ARENA] Shader eau introuvable : fallback azzurro Standard.");
        }
        rendSol.material = matSol;           // instance mémoire jamais sur disque
        Bounds b = rendSol.bounds;

        // ── 2) LA CUVE : GRADIENT RADIAL (centre clair → bord turquoise) ─
        // salim 03/10 : « le sol et les bulles font qu'un ». Le fond de cuve
        // n'est plus une couleur plate : texture 256² générée EN CODE
        // (pas une image importée — même règle que l'ombre des bulles).
        GameObject centre = GameObject.CreatePrimitive(PrimitiveType.Quad);
        centre.name = "ArenaCuveClaire";
        Object.Destroy(centre.GetComponent<Collider>());   // ne gêne pas le mouvement

        UnityEngine.MeshFilter mf = centre.GetComponent<MeshFilter>();
        Vector3 tailleMesh = mf != null && mf.sharedMesh != null
            ? mf.sharedMesh.bounds.size : Vector3.one;

        Material cuveClaire;
        if (shaderEau != null)
        {
            // ── 2) LA CUVE : la MÊME eau animée, ton plus clair ─────────
            // Les vagues sont calculées sur la position MONDE → elles
            // continuent tout droit entre le sol et la cuve : une seule
            // nappe d'eau, pas deux nappes qui ne se raccordent pas.
            cuveClaire = new Material(shaderEau);
            cuveClaire.SetColor("_CouleurCentre", new Color(0.64f, 0.94f, 0.99f));
            cuveClaire.SetColor("_CouleurBord",   new Color(0.42f, 0.83f, 0.95f));
            cuveClaire.SetFloat("_Echelle",      2.2f);   // mêmes vagues que le sol
            cuveClaire.SetFloat("_Vitesse",      0.28f);
            cuveClaire.SetFloat("_ForceReflets", 0.45f);
            cuveClaire.name = "ArenaCuveEau";
        }
        else
        {
            // Fallback : l'ancienne texture gradient générée en code.
            cuveClaire = new Material(Shader.Find("Standard"));
            cuveClaire.mainTexture = MatCuve();
            cuveClaire.color = Color.white;          // la teinte vient de la texture
            if (cuveClaire.HasProperty("_Smoothness")) cuveClaire.SetFloat("_Smoothness", 0.90f);
            if (cuveClaire.HasProperty("_Metallic")) cuveClaire.SetFloat("_Metallic", 0f);
        }
        cuveClaire.name = "ArenaCuveClaire";
        centre.GetComponent<Renderer>().sharedMaterial = cuveClaire;
        centre.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);    // face vers le haut
        float sx = Mathf.Max(b.size.x - 5f, 4f);   // 2,5 m de marge de chaque côté
        float sz = Mathf.Max(b.size.z - 5f, 4f);
        centre.transform.localScale = new Vector3(sx / tailleMesh.x, sz / tailleMesh.y, 1f);
        centre.transform.position = new Vector3(b.center.x, b.max.y + 0.01f, b.center.z);

        // ── 3) LES MURS deviennent le MUR DE PIERRE (salim 04/10 : «
        // l'arène surélevée à peine, entourée d'un mur de pierre ») ─────
        // Texture de pierre GÉNÉRÉE EN CODE (blocs gris + joints de mortier),
        // tuilée à la bonne taille sur chaque longueur de mur. On monte
        // aussi les murs UN PEU (×1.3) pour qu'on voie le bord qui dépasse
        // au-dessus de l'eau : ça donne l'impression que l'arène est un
        // grand bassin posé sur la pelouse. RIEN d'autre ne bouge : les
        // positions, les colliders, les bulles, les slimes — inchangés.
        FaireMurPierre();

        ReculerArbres();

        FaireHorizon();

        Debug.Log(shaderEau != null
            ? "[ARENA] Eau animee posee + mur de pierre autour (arene sur elevee)."
            : "[ARENA] Look azzurro pose (fallback) + mur de pierre autour.");
    }

    // ────────────────────────────────────────────────────────────────
    //  3-QUART) L'HORIZON (salim 05/10 : « rajoute horizon pendant game »)
    //  Pendant le jeu, après les 70 m de pelouse il n'y avait RIEN : on
    //  voyait le vide. Maintenant :
    //    1) une GRANDE PLAINE qui continue loin derrière la pelouse,
    //    2) des COLLINES lointaines tout autour (le trait d'horizon),
    //    3) de la BRUME linéaire : de 40 m à 105 m le vert se fond doucement
    //       dans le bleu du ciel → vraie ligne d'horizon.
    //  L'arène, les murs, les bulles, les slimes et la CAMÉRA : rien ne bouge
    //  (la brume commence à 40 m, l'arène fait moins de 25 m de rayon →
    //  elle reste exactement comme avant).
    // ────────────────────────────────────────────────────────────────
    private static void FaireHorizon()
    {
        if (GameObject.Find("GrassPlane") == null) return;   // pas cette scène

        // ── la BRUME (le plus important : elle fait la ligne d'horizon) ─
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40f;    // avant 40 m : rien ne change
        RenderSettings.fogEndDistance = 105f;     // à 105 m : tout est dans le ciel
        RenderSettings.fogColor = new Color(0.62f, 0.80f, 0.95f);   // bleu ciel

        // ── la GRANDE PLAINE qui continue après la pelouse ─────────────
        GameObject plaine = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plaine.name = "HorizonPlaine";
        Object.Destroy(plaine.GetComponent<Collider>());   // juste du décor, pas de collisions
        plaine.transform.localScale = new Vector3(36f, 1f, 36f);   // 360 m × 360 m
        plaine.transform.position = new Vector3(0f, -0.12f, 0f);   // juste SOUS la pelouse
        Material matPlaine = new Material(Shader.Find("Standard"));
        matPlaine.color = new Color(0.34f, 0.55f, 0.27f);          // vert un peu plus sombre
        if (matPlaine.HasProperty("_Smoothness")) matPlaine.SetFloat("_Smoothness", 0.05f);
        plaine.GetComponent<Renderer>().material = matPlaine;

        // ── les COLLINES lointaines (18, bien réparties, hauteur variée) ─
        // Positions DÉTERMINISTES (hash comme la texture de pierre) : pareil
        // à chaque partie, jamais de hasard qui change le décor.
        const int N = 18;
        Color vertProche = new Color(0.40f, 0.58f, 0.33f);   // colline la plus proche
        Color bleuLoin  = new Color(0.55f, 0.72f, 0.88f);    // colline la plus loin (dans la brume)
        for (int i = 0; i < N; i++)
        {
            // autour du centre, à chaque colline son angle + petit décalage
            float h = Mathf.Repeat(Mathf.Sin((i + 1) * 12.9898f) * 43758.5453f, 1f);
            float angle = i * Mathf.PI * 2f / N + h * 0.6f;
            float profondeur = 46f + Mathf.Repeat(h * 3.7f, 1f) * 12f;   // 46 à 58 m
            float hauteur = 8f + Mathf.Repeat(Mathf.Sin((i + 3) * 0.79f) * 31.7f, 1f) * 9f;  // 8 à 17 m
            float largeur = 24f + Mathf.Repeat(Mathf.Sin((i + 5) * 0.31f) * 71.3f, 1f) * 14f; // 24 à 38 m

            GameObject colline = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            colline.name = "HorizonColline" + i;
            Object.Destroy(colline.GetComponent<Collider>());          // juste du décor
            colline.transform.localScale = new Vector3(largeur, hauteur, largeur);
            colline.transform.position = new Vector3(
                Mathf.Sin(angle) * profondeur,
                hauteur * 0.5f - 3f,           // enfoncée un peu dans la plaine
                Mathf.Cos(angle) * profondeur);
            colline.GetComponent<Renderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;   // rien d'ombre là-bas

            Material collMat = new Material(Shader.Find("Standard"));
            float dansLaBrume = (profondeur - 46f) / 12f;   // 0 proche → 1 loin
            collMat.color = Color.Lerp(vertProche, bleuLoin, dansLaBrume);
            if (collMat.HasProperty("_Smoothness")) collMat.SetFloat("_Smoothness", 0f);
            if (collMat.HasProperty("_Metallic"))   collMat.SetFloat("_Metallic", 0f);
            colline.GetComponent<Renderer>().material = collMat;
        }

        Debug.Log("[ARENA] Horizon pose : plaine 360 m + 18 collines + brume 40->105 m.");
    }

    // ────────────────────────────────────────────────────────────────
    //  TROIS-DEMI) MUR DE PIERRE — les 4 murs WallN/WallS/WallE/WallW
    //  existants sont restylés en pierre (l'escalier de blocs gris
    //  comme dans les châteaux) et gagnent un peu de hauteur. On touche
    //  JAMAIS à leur position ni à leurs colliders : seul le look bouge,
    //  le jeu reste exactement celui qu'il était.
    // ────────────────────────────────────────────────────────────────
    private static void FaireMurPierre()
    {
        Texture2D pierreTex = TexPierre();        // générée en code, une fois

        foreach (string nom in new[] { "WallN", "WallS", "WallE", "WallW" })
        {
            GameObject mur = GameObject.Find(nom);
            if (mur == null) continue;
            Renderer rendMur = mur.GetComponent<Renderer>();
            if (rendMur == null || rendMur.sharedMaterial == null) continue;

            Material pierre = new Material(rendMur.sharedMaterial);   // clone mémoire
            pierre.mainTexture = pierreTex;
            pierre.color = Color.white;          // la teinte vient de la texture

            // tuiler la pierre à échelle humaine : une répétition de la
            // texture ≈ 2,2 m, donc les blocs font ~0,5 m de haut sur le mur
            Bounds bm = rendMur.bounds;
            pierre.mainTextureScale = new Vector2(
                Mathf.Max(bm.size.x, bm.size.z) / 2.2f,
                bm.size.y / 2.2f);

            if (pierre.HasProperty("_Smoothness")) pierre.SetFloat("_Smoothness", 0.35f);
            if (pierre.HasProperty("_Metallic"))   pierre.SetFloat("_Metallic", 0f);
            pierre.name = "ArenaPierre_" + nom;
            rendMur.material = pierre;

            // « surélevée à peine » : le mur devient ~30 % plus haut, donc
            // son haut dépasse davantage au-dessus de l'eau. Le bas du cube
            // passe sous la pelouse (invisible), rien ne descend jamais
            // au-dessus du sol du jeu.
            Vector3 ech = mur.transform.localScale;
            mur.transform.localScale = new Vector3(ech.x, ech.y * 1.3f, ech.z);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  4) ARBRES REPOUSSÉS (salim 03/10 : « les arbres de derrière me
    //  gênaient quand je passais, il y en avait beaucoup qui bloquaient la
    //  vision »). En vue perspective, un arbre juste derrière le mur passe
    //  ENTRE la caméra et l'arène : il cache les slimes. On repousse TOUS
    //  les arbres au bord de la pelouse (28 à 32 m du centre, la pelouse
    //  fait 70 m) et on les rétrécit un peu : décor lointain joli, jamais
    //  objet qui coupe la vue.
    // ────────────────────────────────────────────────────────────────
    private static void ReculerArbres()
    {
        var arbres = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        int pousses = 0;
        foreach (var t in arbres)
        {
            if (t.name != "Tree") continue;
            Vector3 pos = t.position;

            // distance plate au centre de l'arène
            float d = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
            if (d > 27.5f) continue;   // déjà loin : on n'y touche pas
            if (d < 0.01f) { pos.x = 29f; pos.z = 0f; d = 1f; }

            // à quelle distance on le pose : 28 à 32 m, varié selon position
            float vari = Mathf.Abs(pos.x * 0.37f + pos.z * 0.11f) % 4f;
            float cible = 28f + vari;
            pos.x *= cible / d;
            pos.z *= cible / d;
            t.position = pos;

            // un peu plus petits : moins hauts, moins bloquants
            t.localScale = t.localScale * 0.85f;
            pousses++;
        }
        if (pousses > 0)
            Debug.Log($"[ARENA] {pousses} arbres repousses au bord de la pelouse (28-32 m).");
    }

    /// <summary>
    /// TEXTURE DE CUVE (générée en code, jamais d'asset sur disque) :
    /// dégradé ROND bleu pâle au centre → bleu d'eau vers le bord. Le sol
    /// a exactement la palette des bulles translucides → tout fait qu'un,
    /// comme un vrai fond de piscine à bulles.
    /// </summary>
    private static Texture2D texCuve;

    private static Texture2D MatCuve()
    {
        if (texCuve != null) return texCuve;

        const int S = 256;
        var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[S * S];
        float c = (S - 1) * 0.5f;
        Color centreClaire = new Color(0.63f, 0.93f, 0.98f);   // eau claire (centre)
        Color bordTurquoise = new Color(0.33f, 0.78f, 0.94f);  // eau turquoise (bord)
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = x - c;
                float dy = y - c;
                // max(dxr,dyr) au lieu de la vraie diagonale : coins pas trop sombres
                float r = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) / c;
                uint idx = (uint)(y * S + x);
                Color col = Color.Lerp(centreClaire, bordTurquoise, r);
                px[idx] = col;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        texCuve = tex;
        return texCuve;
    }

    // ────────────────────────────────────────────────────────────────
    //  TEXTURE DE PIERRE (générée en code, jamais d'asset sur disque) :
    //  un mur de blocs posés en RANGÉES, comme les remparts de château.
    //  Chaque rangée est décalée d'un demi-bloc sur la précédente, les
    //  blocs sont gris variés, les joints de mortier plus sombres, et un
    //  petit grain partout pour que ça brille pas.
    // ────────────────────────────────────────────────────────────────
    private static Texture2D texPierre;

    private static Texture2D TexPierre()
    {
        if (texPierre != null) return texPierre;

        const int S = 256;
        const int RANGÉES = 8;                 // 8 rangées de ~32 px de haut
        const int PIERRES_PAR_RANGÉE = 4;      // blocs de 64 px → bloc 2:1
        const int JOINT = 3;                   // mortier : 3 px de chaque côté

        var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        tex.wrapMode = TextureWrapMode.Repeat; // se répète sur toute la longueur
        var px = new Color32[S * S];

        for (int y = 0; y < S; y++)
        {
            int rangée = Mathf.Min(y * RANGÉES / S, RANGÉES - 1);
            bool impaire = (rangée % 2) == 1;         // rangées décalées d'un demi-bloc
            int demiBloc = (S / (PIERRES_PAR_RANGÉE * 2));   // 32 px

            for (int x = 0; x < S; x++)
            {
                // position x dans la rangée (décalée si rangée impaire)
                int xDécalé = (x + (impaire ? demiBloc : 0)) % S;
                int col = xDécalé / (S / PIERRES_PAR_RANGÉE);  // 0..3 : quelle pierre
                int dansBloc = xDécalé % (S / PIERRES_PAR_RANGÉE);   // 0..63 : où dedans
                int dansRangée = y % (S / RANGÉES);                // 0..31 : où dedans

                // hash déterministe : chaque (rangée, colonne) a son gris
                uint graine = (uint)(rangée * 97 + col * 31 + 7);
                float h = Mathf.Repeat(Mathf.Sin(graine * 12.9898f) * 43758.5453f, 1f);
                float grisBase = 0.52f + h * 0.20f;   // 0.52 → 0.72 selon le bloc

                // petit grain par pixel (pierre pas toute lisse)
                uint g2 = (uint)(x * 19 + y * 53);
                float bruit = Mathf.Repeat(Mathf.Sin(g2 * 0.7919f) * 31337f, 1f) - 0.5f;

                float gris = grisBase + bruit * 0.05f;

                // face du bloc légèrement en pente : plus clair en haut
                gris += (1f - (float)dansRangée / 32f) * 0.035f;

                // JOINTS : près des bords du bloc → mortier sombre
                bool bordX = dansBloc < JOINT || dansBloc >= (S / PIERRES_PAR_RANGÉE) - JOINT;
                bool bordY = dansRangée < JOINT || dansRangée >= (S / RANGÉES) - JOINT;

                Color col2;
                if (bordX || bordY)
                    col2 = new Color(0.36f, 0.36f, 0.37f);         // mortier
                else
                {
                    // gris de pierre, un chouïa chaud (pas un gris robot)
                    col2 = new Color(gris, gris * 0.99f, gris * 0.95f);
                }
                px[y * S + x] = col2;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        texPierre = tex;
        return texPierre;
    }

    /// <summary>
    /// OMBRE SOFT sous chaque bulle (BubbleSpawner l'appelle) : un petit
    /// dégradé rond généré en code (pas une image importée) — la bulle
    /// « contacte » le sol au lieu de flotter dessus.
    /// </summary>
    public static Material MatOmbreBulle()
    {
        if (matOmbre != null) return matOmbre;

        const int S = 64;                       // 64 x 64 px, généré en code
        var tex = new Texture2D(S, S, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[S * S];
        float c = (S - 1) * 0.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / c;   // 0 centre → 1 bord
                float a = 0.30f * (1f - r);                    // dégradé doux
                a *= a;                                        // fade net au bord
                byte alpha = (byte)(Mathf.Clamp01(a) * 255f);
                px[y * S + x] = new Color32(6, 26, 50, alpha); // ombre d'eau sombre
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        // Sprites/Default : shader UGUI standard, respect l'alpha (translucide)
        matOmbre = new Material(Shader.Find("Sprites/Default"));
        matOmbre.mainTexture = tex;
        matOmbre.renderQueue = 2999;   // après le sol, avant les autres bulles
        matOmbre.name = "OmbreBulle";
        return matOmbre;
    }
}