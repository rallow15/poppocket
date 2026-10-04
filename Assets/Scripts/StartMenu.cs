using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;   // recharge la scène quand on change la langue
using Symphonie.StoreAssets;   // SlimeVisual (gelée du pack) — aperçus 3D du shop

/// <summary>
/// Écran de départ : "POP POCKET" + gros bouton JOUER.
/// Le GameManager n'attaque le round 1 QUE quand ce bouton est appuyé
/// (il attend StartMenu.StartRequested). Après un "Rejouer", le menu
/// est sauté automatiquement : on repart directement dans le jeu.
/// </summary>
[DefaultExecutionOrder(-200)]
public class StartMenu : MonoBehaviour
{
    public static StartMenu Instance { get; private set; }

    /// <summary>Le joueur a appuyé sur JOUER → GameManager peut démarer.</summary>
    public static bool StartRequested { get; private set; }

    // Vrai dès le premier JOUER (garde sa valeur même après un Rejouer)
    private static bool playedOnce;

    private Canvas canvas;
    private Canvas shopCanvas;         // panneau BOUTIQUE (créé/détruit à la demande)
    private TMP_Text shopMessage;      // message d'info DANS la boutique (ex: pas assez de pièces)

    // Lien vers l'indication "PLUS DE SKINS" + la flèche : servent à les
    // cacher quand on est arrivé tout en bas de la boutique (salim 04/10).
    private TMP_Text plusSkinsLabel;
    private GameObject chevronScroll;
    private ScrollRect shopScroll;
    private GameObject panneauReglages;   // panneau REGLAGES (langue + son), créé à la demande

    // APERÇUS 3D (salim 03/10 : « voir les slimes, pas juste une couleur ») :
    // le préfab du joueur est instancié très loin de l'arène, teinté comme
    // chaque skin, et une caméra « studio » prend UNE photo de chacun. Les
    // cartes affichent ces photos (RawImage) au lieu d'une pastille plate.
    private RenderTexture[] previewTextures;              // une photo par skin
    private readonly List<RawImage> cardPreviews = new List<RawImage>();
    private GameObject previewStage;                      // le studio photo (dormant après usage)

    private void Awake()
    {
        Instance = this;
        StartRequested = playedOnce;   // Rejouer → on attaque direct
    }

    /// <summary>
    /// salim 03/10 : bouton MENU de l'écran final + bouton QUITTER en jeu.
    /// Appelé AVANT le rechargement de la scène : remet les statics à zéro
    /// pour que le MENU se ré-affiche au lieu de relancer direct le jeu.
    /// </summary>
    public static void RevenirAuMenu()
    {
        playedOnce = false;
        StartRequested = false;
    }

    private void Start()
    {
        if (StartRequested) { enabled = false; return; }
        BuildUI();
        Debug.Log("[MENU-START] Menu de départ affiché (attente du bouton JOUER)");
    }

    // ────────────────────────────────────────────────────────────────
    //  CONSTRUCTION DU MENU (tout en code, rien à brancher à la main)
    // ────────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        GameObject go = new GameObject("MenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;   // au-dessus de tout

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Fond coloré doux (bleu nuit, un cran plus vif — style voodoo)
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.14f, 0.34f, 0.97f);
        bg.raycastTarget = true;       // le fond bloque les clics pendant le menu

        // FOND PHOTO (salim 03/10 : « utilise cette image pour le menu ») :
        // la capture des slimes gelée sert de fond, recouverte d'un voile
        // sombre pour que le titre se lise bien par-dessus.
        PoseFondImage(go, 0.25f);

        // Paysage (salim, 03/10) : l'écran est plus bas en horizontal →
        // on resserre les hauteurs sinon titre/bouton débordent.
        float kY = Screen.width > Screen.height ? 0.6f : 1f;
        Vector2 Pos(float y) => new Vector2(0f, kY * y);

        // ---- TITRE (voodoo : blanc TRÈS gras, contour épais + ombre) ----
        TMP_Text title = MakeText(go.transform, "TitleText", "POP POCKET", 150,
            VoodooStyle.Blanc, Pos(460));
        VoodooStyle.ApplyTitle(title, VoodooStyle.Blanc);

        // ---- SOUS-TITRE ----
        // salim 04/10 : « dans le menu slime & fidgets baisse le un peux
        // car il touche pop pockets » → Pos(320) descendu à 240.
        TMP_Text sub = MakeText(go.transform, "SubtitleText", "Slime & Fidgets", 70,
            VoodooStyle.Jaune, Pos(240));
        VoodooStyle.ApplyText(sub, VoodooStyle.Jaune, 0.35f);

        // salim 04/10 : « sur le menu retire les quatre cube de couleur »
        // → le bloc PASTILLES SLIMES a été retiré entièrement.

        // ---- BOUTON JOUER (puffy jaune avec relief dessous) ----
        GameObject btnGO = new GameObject("PlayButton",
            typeof(Image), typeof(Button));
        btnGO.transform.SetParent(go.transform, false);
        RectTransform brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        // NIVEAUX (salim 04/10) : bouton PLUS PETIT (520×180 → 420×130),
        // MÊME position (Pos -390) — la place libérée accueille la rangée
        // des niveaux juste en dessous.
        brt.sizeDelta = new Vector2(420f * kY, 130f * kY);
        brt.anchoredPosition = Pos(-390);   // salim 04/10 : JOUER + BOUTIQUE + règles descendus ENSEMBLE (-100) pour ne pas se chevaucher

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(OnPlayClicked);

        // Label du bouton (voodoo : blanc gras + contour sombre)
        // salim 04/10 : « il y a un carré à côté de jouer » — le ▶
        // n'existe pas dans Bebas Neue, TMP le dessinait en CARRÉ. Fini :
        // juste le mot, et le bouton Kenney parle tout seul.
        TMP_Text label = MakeText(btnGO.transform, "Label", Trad.T("JOUER", "PLAY"), (int)(66 * kY),
            VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(label, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(btnGO, VoodooStyle.Jaune);

        // salim 04/10 : « le nombre de piece tu la metra que dans le magasin »
        // → le compteur de pièces du MENU a été retiré (il ne reste que
        // celui de la BOUTIQUE).

        // ---- BOUTON REGLAGES (engrenages, salim 04/10 : « en haut a droite ») ----
        GameObject gearGO = new GameObject("SettingsButton",
            typeof(Image), typeof(Button));
        gearGO.transform.SetParent(go.transform, false);
        RectTransform grt = gearGO.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = new Vector2(1f, 1f);   // coin haut-droite
        grt.pivot = new Vector2(1f, 1f);
        grt.sizeDelta = new Vector2(110, 110);
        grt.anchoredPosition = new Vector2(-26, -28);
        gearGO.GetComponent<Button>().onClick.AddListener(OuvrirReglages);
        Image gearImg = gearGO.GetComponent<Image>();
        gearImg.sprite = Sprite.Create(TexEngrenage(),
            new Rect(0f, 0f, 128, 128), new Vector2(0.5f, 0.5f), 128f);
        gearImg.color = VoodooStyle.Blanc;   // engrenage blanc, lisible sur la photo

        // Le SafeArea passe APRÈS : ne pas laisser l'engrenage sous la caméra.
        gearGO.AddComponent<SafeAreaFitter>();

        // ---- BOUTON BOUTIQUE (shop de skins) ----
        GameObject shopGO = new GameObject("ShopButton",
            typeof(Image), typeof(Button));
        shopGO.transform.SetParent(go.transform, false);
        RectTransform srt = shopGO.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        // NIVEAUX (salim 04/10) : boutique PLUS PETITE (460×140 → 360×105),
        // MÊME position (Pos -590).
        srt.sizeDelta = new Vector2(360f * kY, 105f * kY);
        srt.anchoredPosition = Pos(-590);   // salim 04/10 : descendu avec JOUER (-100)

        shopGO.GetComponent<Button>().onClick.AddListener(OnShopClicked);

        TMP_Text shopLabel = MakeText(shopGO.transform, "Label", Trad.T("BOUTIQUE", "SHOP"), 46,
            VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(shopLabel, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(shopGO, VoodooStyle.Orange);

        // ---- BOUTON NIVEAUX (salim 04/10 : « je préfère une modale ») ----
        // Un 3e bouton ENTRE JOUER et BOUTIQUE (Pos -497) qui OUVRE la
        // modale de sélection des niveaux.
        GameObject lvlGO = new GameObject("LevelsButton",
            typeof(Image), typeof(Button));
        lvlGO.transform.SetParent(go.transform, false);
        RectTransform lrt = lvlGO.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(300f * kY, 90f * kY);
        lrt.anchoredPosition = Pos(-497);            // entre JOUER (-390) et BOUTIQUE (-590)

        lvlGO.GetComponent<Button>().onClick.AddListener(OuvrirModaleNiveaux);

        TMP_Text lvlLabel = MakeText(lvlGO.transform, "Label", Trad.T("NIVEAUX", "LEVELS"),
            (int)(40 * kY), VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(lvlLabel, VoodooStyle.Blanc);
        // KENNEY (salim 04/10) : vrai rect BLEU du pack (relief cuit),
        // pas le gris teinté — le bleu garde toute sa profondeur.
        VoodooStyle.MakePuffy(lvlGO, VoodooStyle.BleuNuit);
        Sprite rectBleu = VoodooStyle.SpriteKenney("RectBleu", new Vector4(20f, 20f, 20f, 20f));
        if (rectBleu != null)
        {
            Image li = lvlGO.GetComponent<Image>();
            li.sprite = rectBleu;
            li.type = Image.Type.Sliced;
            li.color = Color.white;
        }

        // ---- AIDE en bas ----
        TMP_Text help = MakeText(go.transform, "HelpText",
            Trad.T(
                "Glisse le doigt pour te déplacer\nTAP l'écran pour sauter\nEclate le plus de bulles !",
                "Drag your finger to move\nTAP the screen to jump\nPop as many bubbles as you can!"),
            44, VoodooStyle.Blanc, Pos(-790));   // salim 04/10 : descendu avec JOUER (-100)
        VoodooStyle.ApplyText(help, VoodooStyle.Blanc, 0.28f);
        help.color = new Color(1f, 1f, 1f, 0.9f);

        Debug.Log("[MENU-START] Menu construit ✔");
    }

    // ────────────────────────────────────────────────────────────────
    //  NIVEAUX (salim 04/10 : « je préfère une modale qui s'ouvre ») :
    //  le 3e bouton du menu (NIVEAUX, entre JOUER et BOUTIQUE) ouvre
    //  une fenêtre par-dessus TOUT, fond assombri, panneau central avec
    //  des boutons ronds de niveau. Débloqué = numéro (jaune si choisi,
    //  bleu sinon) ; verrouillé = "?" gris. On ferme avec le X ou en
    //  tapant le fond sombre.
    // ────────────────────────────────────────────────────────────────
    private void OuvrirModaleNiveaux()
    {
        // kT recalculé ICI (kY/Pos sont LOCALS à BuildUI ; la modale est
        // reconstruite à chaque ouverture, donc suit l'orientation)
        float kT = Screen.width > Screen.height ? 0.6f : 1f;

        // Une seule modale à la fois
        GameObject ancienne = GameObject.Find("ModaleNiveaux");
        if (ancienne != null) Destroy(ancienne);

        // ---- VOILE plein écran : assombrit + CAPTURE les clics (bloque le menu derrière) ----
        // Pas besoin de trier les canvas : la BOUTIQUE (sort 10100) ne peut
        // pas s'ouvrir pendant la modale (son bouton est bloqué par le voile).
        GameObject voile = new GameObject("ModaleNiveaux",
            typeof(RectTransform), typeof(Image), typeof(Button));
        if (canvas != null) voile.transform.SetParent(canvas.transform, false);
        RectTransform vt = voile.GetComponent<RectTransform>();
        vt.anchorMin = Vector2.zero;
        vt.anchorMax = Vector2.one;
        vt.offsetMin = Vector2.zero;
        vt.offsetMax = Vector2.zero;
        Image vi = voile.GetComponent<Image>();
        vi.color = new Color(0f, 0f, 0f, 0.65f);
        voile.GetComponent<Button>().onClick.AddListener(FermerModaleNiveaux);   // tap sur le fond = fermer

        // ---- PANNEAU central (puffy : même style que les boutons) ----
        GameObject panneau = new GameObject("Panneau", typeof(Image), typeof(Button));
        panneau.transform.SetParent(voile.transform, false);
        RectTransform prt = panneau.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(720f * kT, 760f * kT);   // 10 niveaux : panneau plus haut
        // Bouton sur le panneau pour "manger" le clic (sinon le voile ferme)
        Button pb = panneau.GetComponent<Button>();
        pb.onClick.AddListener(() => { });   // clic absorbé, rien ne se passe

        // KENNEY (salim 04/10) : vrai fond de panneau du pack, 9-slice.
        Sprite spPanneau = VoodooStyle.SpriteKenney("Panneau", new Vector4(24f, 24f, 24f, 26f));
        if (spPanneau != null)
        {
            Image pi = panneau.GetComponent<Image>();
            pi.sprite = spPanneau;
            pi.type = Image.Type.Sliced;
            pi.color = Color.white;
        }

        // ---- TITRE ----
        TMP_Text titre = MakeText(panneau.transform, "Titre", Trad.T("CHOISIS TON NIVEAU", "CHOOSE YOUR LEVEL"),
            (int)(56 * kT), VoodooStyle.Blanc, new Vector2(0f, 300f * kT));
        VoodooStyle.ApplyText(titre, VoodooStyle.Jaune);

        // ---- GRILLE DES NIVEAUX : 2 colonnes × 5 rangées = 10 (salim 04/10) ----
        int maxNiveau = ProgresseurJeu.Niveau;
        int choisi = ProgresseurJeu.Choisi;
        int debut = 1;                                    // TOUS les niveaux, du 1 au 10
        float taille = 88f * kT, pasY = 98f * kT, pasX = 140f * kT;

        for (int i = 0; i < ProgresseurJeu.NiveauMax; i++)
        {
            int niveau = debut + i;
            bool debloque = niveau <= maxNiveau;
            int col = i % 2, row = i / 2;                 // 0..4 en haut vers le bas

            GameObject b = new GameObject("Niveau" + niveau, typeof(Image), typeof(Button));
            b.transform.SetParent(panneau.transform, false);
            RectTransform bt = b.GetComponent<RectTransform>();
            bt.anchorMin = bt.anchorMax = new Vector2(0.5f, 0.5f);
            bt.pivot = new Vector2(0.5f, 0.5f);
            bt.sizeDelta = new Vector2(taille, taille);
            // 5 rangées serrées mais SÉPARÉES : pas 98 > taille 88, jamais
            // de chevauchement (même leçon que les 2 rangées d'avant).
            bt.anchoredPosition = new Vector2((col == 0 ? -1f : 1f) * pasX, ((2 - row) * pasY));

            TMP_Text lbl = MakeText(b.transform, "Label",
                debloque ? niveau.ToString() : "?", (int)(52 * kT),
                VoodooStyle.Blanc, Vector2.zero);
            VoodooStyle.ApplyText(lbl, VoodooStyle.Blanc);

            Button btn = b.GetComponent<Button>();
            if (debloque)
            {
                VoodooStyle.MakePuffy(b, niveau == choisi
                    ? VoodooStyle.Jaune : VoodooStyle.BleuNuit);
                // KENNEY (salim 04/10) : boutons RONDS du pack avec le
                // relief cuit dedans (jaune = choisi, bleu = les autres).
                Sprite rond = VoodooStyle.SpriteKenney(
                    niveau == choisi ? "RondJaune" : "RondBleu",
                    new Vector4(14f, 14f, 18f, 14f));
                if (rond != null)
                {
                    Image bi = b.GetComponent<Image>();
                    bi.sprite = rond;
                    bi.type = Image.Type.Sliced;
                    bi.color = Color.white;
                }
                int copie = niveau;                       // capture pour la lambda
                btn.onClick.AddListener(() =>
                {
                    ProgresseurJeu.Choisi = copie;
                    Debug.Log("[NIVEAU] Joueur choisit le niveau " + copie);
                    FermerModaleNiveaux();                // choix fait : la modale part
                });
            }
            else
            {
                VoodooStyle.MakePuffy(b, new Color(0.25f, 0.25f, 0.28f));
                // KENNEY : rond GRIS du pack pour le "?" verrouillé.
                Sprite rondGris = VoodooStyle.SpriteKenney("RondGris",
                    new Vector4(14f, 14f, 18f, 14f));
                if (rondGris != null)
                {
                    Image bi = b.GetComponent<Image>();
                    bi.sprite = rondGris;
                    bi.type = Image.Type.Sliced;
                    bi.color = Color.white;
                }
                btn.interactable = false;                 // "?" = verrouillé
            }
        }

        // ---- BOUTON X (fermer) ----
        GameObject xGO = new GameObject("FermerButton", typeof(Image), typeof(Button));
        xGO.transform.SetParent(panneau.transform, false);
        RectTransform xt = xGO.GetComponent<RectTransform>();
        xt.anchorMin = xt.anchorMax = new Vector2(1f, 1f);   // coin haut-droit du panneau
        xt.pivot = new Vector2(1f, 1f);
        xt.sizeDelta = new Vector2(70f * kT, 70f * kT);
        xt.anchoredPosition = new Vector2(10f * kT, -10f * kT);
        TMP_Text xl = MakeText(xGO.transform, "Label", "X", (int)(44 * kT),
            VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(xl, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(xGO, VoodooStyle.Orange);
        // KENNEY (salim 04/10) : le X devient un rond ROUGE du pack.
        Sprite rondRouge = VoodooStyle.SpriteKenney("RondRouge", new Vector4(14f, 14f, 18f, 14f));
        if (rondRouge != null)
        {
            Image xi = xGO.GetComponent<Image>();
            xi.sprite = rondRouge;
            xi.type = Image.Type.Sliced;
            xi.color = Color.white;
        }
        xGO.GetComponent<Button>().onClick.AddListener(FermerModaleNiveaux);

        // ---- BOUTON JOUER au bas de la modale ----
        GameObject jouerGO = new GameObject("JouerDepuisModale", typeof(Image), typeof(Button));
        jouerGO.transform.SetParent(panneau.transform, false);
        RectTransform jt = jouerGO.GetComponent<RectTransform>();
        jt.anchorMin = jt.anchorMax = new Vector2(0.5f, 0.5f);
        jt.pivot = new Vector2(0.5f, 0.5f);
        jt.sizeDelta = new Vector2(360f * kT, 95f * kT);
        jt.anchoredPosition = new Vector2(0f, -320f * kT);   // sous la grille de 10
        TMP_Text jl = MakeText(jouerGO.transform, "Label", Trad.T("JOUER", "PLAY"),
            (int)(46 * kT), VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(jl, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(jouerGO, VoodooStyle.Jaune);
        jouerGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            FermerModaleNiveaux();
            OnPlayClicked();
        });

        Debug.Log("[MENU-START] Modale NIVEAUX ouverte");
    }

    private void FermerModaleNiveaux()
    {
        GameObject m = GameObject.Find("ModaleNiveaux");
        if (m != null) Destroy(m);
    }

    /// <summary>
    /// Pose l'image de fond (Assets/Resources/FondMenu.png) sur un canvas,
    /// en mode "couvrir" : elle remplit TOUT l'écran sans jamais se
    /// déformer (on rogne ce qui dépasse, comme un zoom de photo). Un voile
    /// sombre par-dessus pour que le texte se lise.
    /// </summary>
    private void PoseFondImage(GameObject canvasGO, float voileAlpha)
    {
        // PIÈGE CORRIGÉ (salim 03/10 : « je vois pas l'image ») : dans un
        // projet 3D, le PNG importé reste en type « Default » (pas « Sprite »)
        // → Resources.Load<Sprite> renvoie NULL et le fond restait bleu.
        // On charge la TEXTURE (ça marche quel que soit le type d'import)
        // et on fabrique le sprite nous-mêmes juste après.
        // DEUX IMAGES (salim 03/10 : une par orientation) :
        //  - écran PORTRAIT (téléphone tenu droit) → FondMenuPortrait.png
        //  - écran PAYSAGE (téléphone couché / PC) → FondMenu.png
        // Si une des deux manque, on retombe sur l'autre (jamais de fond vide).
        bool portrait = Screen.height > Screen.width;
        Texture2D texFond = Resources.Load<Texture2D>(portrait ? "FondMenuPortrait" : "FondMenu");
        if (texFond == null) texFond = Resources.Load<Texture2D>("FondMenuPortrait");
        if (texFond == null) texFond = Resources.Load<Texture2D>("FondMenu");
        if (texFond == null)
        {
            Debug.LogWarning("[MENU-START] FondMenu.png introuvable dans Assets/Resources — fond couleur de secours.");
            return;
        }
        Sprite spriteFond = Sprite.Create(texFond,
            new Rect(0f, 0f, texFond.width, texFond.height),
            new Vector2(0.5f, 0.5f),   // pivot au centre
            100f);                     // 100 pixels = 1 unité (standard)

        // La taille RÉELLE du canvas en unités Unity (l'écran peut être
        // portrait 1080x1920 ou paysage) : on la recalcule comme le
        // CanvasScaler (match 0,5 = racine des deux ratios).
        float echelle = Mathf.Sqrt((Screen.width / 1080f) * (Screen.height / 1920f));
        Vector2 tailleCanvas = new Vector2(Screen.width / echelle, Screen.height / echelle);

        // FIX (salim 03/10 : « image pas bien integrée, coupée en haut et bas ») :
        // le mode "couvrir" ROGNAIT la photo (elle dépassait de l'écran).
        // Maintenant la photo reste ENTIÈRE (jamais coupée, jamais déformée),
        // et la MÊME image étirée en coulisse bouche les vides restants
        // (légèrement assombrie → ça se voit à peine, l'effet est continu).
        // TAILLE 1 — l'image ENTIÈRE (le plus grand rectangle qui tient dans
        // l'écran sans toucher à ses proportions) : ni rognage, ni déformation.
        float echelleComplete = Mathf.Min(tailleCanvas.x / (float)texFond.width,
                                          tailleCanvas.y / (float)texFond.height);
        Vector2 tailleComplete = new Vector2(texFond.width * echelleComplete,
                                             texFond.height * echelleComplete);

        // TAILLE 2 — la même image étendue jusqu'à DÉBORDER DE L'ÉCRAN
        // (sert derrière, pour boucher les vides laissés par la photo entière).
        float echelleMaxi = Mathf.Max(tailleCanvas.x / (float)texFond.width,
                                      tailleCanvas.y / (float)texFond.height);
        Vector2 tailleRemplissage = new Vector2(texFond.width * echelleMaxi,
                                                texFond.height * echelleMaxi);

        // 1) REMPLISSAGE : la même image étirée jusqu'à déborder, assombrie.
        GameObject remplissage = new GameObject("FondRemplissage", typeof(Image));
        remplissage.transform.SetParent(canvasGO.transform, false);
        RectTransform ret = remplissage.GetComponent<RectTransform>();
        ret.anchorMin = ret.anchorMax = new Vector2(0.5f, 0.5f);
        ret.pivot = new Vector2(0.5f, 0.5f);
        ret.sizeDelta = tailleRemplissage;
        ret.anchoredPosition = Vector2.zero;
        Image rimg = remplissage.GetComponent<Image>();
        rimg.sprite = spriteFond;
        rimg.color = new Color(0.55f, 0.55f, 0.55f, 1f);   // assombrie, juste pour boucher les vides
        rimg.raycastTarget = false;

        // 2) L'IMAGE ENTIÈRE posée par-dessus : jamais coupée, jamais déformée.
        GameObject fond = new GameObject("FondImage", typeof(Image));
        fond.transform.SetParent(canvasGO.transform, false);
        RectTransform frt = fond.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = tailleComplete;
        frt.anchoredPosition = Vector2.zero;
        Image fimg = fond.GetComponent<Image>();
        fimg.sprite = spriteFond;
        fimg.raycastTarget = false;    // la photo ne bloque pas les boutons

        // Voile sombre par-dessus la photo, sous les textes.
        GameObject voile = new GameObject("VoileFond", typeof(Image));
        voile.transform.SetParent(canvasGO.transform, false);
        RectTransform vrt = voile.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        Image vimg = voile.GetComponent<Image>();
        vimg.color = new Color(0.05f, 0.07f, 0.18f, voileAlpha);
        vimg.raycastTarget = false;
    }

    // ────────────────────────────────────────────────────────────────
    //  PANNEAU BOUTIQUE (shop de skins, salim 03/10)
    //  Grille de 9 cartes : chaque carte montre la COULEUR du slime +
    //  un nom + un prix. Clic une fois = ACHAT (si assez de pièces, le
    //  skin est équipé direct). Clic suivant = CHANGER DE SKIN.
    //  Reconstruit à chaque action → toujours à jour, zéro état à suivre.
    // ────────────────────────────────────────────────────────────────
    private void OnShopClicked()
    {
        if (shopCanvas != null) return;   // déjà ouvert
        BuildShopPanel();
        Debug.Log("[MENU-START] Boutique ouverte — " + ShopManager.Pieces + " pièces");
    }

    private void BuildShopPanel()
    {
        GameObject go = new GameObject("ShopCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        shopCanvas = go.GetComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 10100;   // au-dessus du menu (10000)

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.14f, 0.34f, 0.99f);
        bg.raycastTarget = true;

        // Paysage : mêmes hauteurs resserrées que le menu
        float kY = Screen.width > Screen.height ? 0.6f : 1f;
        Vector2 Pos(float y) => new Vector2(0f, kY * y);

        // ---- TITRE de la boutique ----
        TMP_Text title = MakeText(go.transform, "ShopTitle", Trad.T("BOUTIQUE", "SHOP"), 130,
            VoodooStyle.Jaune, Pos(620));
        VoodooStyle.ApplyTitle(title, VoodooStyle.Jaune);

        // ---- Solde de pièces ----
        TMP_Text pices = MakeText(go.transform, "ShopPieces",
            Trad.T("PIECES : ", "COINS: ") + ShopManager.Pieces, 70, VoodooStyle.Blanc,
            new Vector2(-170f, kY * 450f));
        VoodooStyle.ApplyText(pices, VoodooStyle.Blanc, 0.3f);

        // ---- Indicateur : COMBIEN de skins existent (salim 04/10 :
        // « comment savoir dans boutique qu'il y a plusieurs skin »).
        // À DROITE du solde de pièces : sinon les cartes du haut le
        // recouvraient (salim : « le 12 est caché »).
        TMP_Text nbSkins = MakeText(go.transform, "NbSkins",
            ShopManager.Skins.Length + Trad.T(" SKINS", " SKINS"), 46,
            VoodooStyle.Orange, new Vector2(220f, kY * 450f));
        VoodooStyle.ApplyText(nbSkins, VoodooStyle.Orange, 0.3f);

        // ---- Petites flèches + « PLUS DE SKINS » : montre qu'on peut
        // GLISSER pour voir les suivants ----
        TMP_Text plusTxt = MakeText(go.transform, "PlusDeSkins",
            Trad.T("PLUS DE SKINS", "MORE SKINS"), 40,
            VoodooStyle.Orange, new Vector2(0f, kY * -530f));
        VoodooStyle.ApplyText(plusTxt, VoodooStyle.Orange, 0.3f);
        GameObject chevron = new GameObject("ChevronScroll", typeof(Image));
        chevron.transform.SetParent(go.transform, false);
        RectTransform crt = chevron.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(64, 36);
        crt.anchoredPosition = new Vector2(0f, kY * -485f);
        Image cimg = chevron.GetComponent<Image>();
        cimg.sprite = Sprite.Create(TexTriangleBas(),
            new Rect(0f, 0f, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        cimg.color = VoodooStyle.Orange;
        cimg.raycastTarget = false;

        // salim 04/10 : « quand je descends le MORE SKIN reste alors que je
        // suis à la FIN » → on mémorise ces 2 éléments et dès que la grille
        // est tout en bas (ou ne déroule pas du tout), on les cache.
        plusSkinsLabel = plusTxt;
        chevronScroll = chevron;

        // ---- Grille des skins : 3 colonnes, dans une ZONE QUI DÉROULE ----
        // 12 skins = 4 lignes : ça ne tient plus d'un coup à l'écran, donc la
        // grille vit dans un panneau qu'on glisse vers le bas (comme un feed).
        cardPreviews.Clear();

        GameObject scrollGO = new GameObject("GrilleScroll",
            typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(go.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRT.pivot = new Vector2(0.5f, 0.5f);
        scrollRT.sizeDelta = new Vector2(1020, 860);
        scrollRT.anchoredPosition = new Vector2(0f, kY * -40f);
        scrollGO.GetComponent<Image>().color = Color.clear;   // aucun fond visible

        ScrollRect scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 40f;

        // Viewport : la "coulisse" qui découpe ce qui dépasse
        GameObject vpGO = new GameObject("Viewport",
            typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vrt = vpGO.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        vpGO.GetComponent<Image>().color = Color.white;    // sert AU MASK, pas dessiné
        vpGO.GetComponent<Mask>().showMaskGraphic = false;

        // Content : la grille elle-même, plus haute que l'écran → on scrolle
        GameObject gridGO = new GameObject("Grille", typeof(RectTransform));
        gridGO.transform.SetParent(vpGO.transform, false);
        RectTransform grt = gridGO.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 1f);
        grt.pivot = new Vector2(0.5f, 1f);
        int lignes = (ShopManager.Skins.Length + 2) / 3;
        float hauteurGrille = kY * (lignes * 320f - 20f);
        grt.sizeDelta = new Vector2(1020, hauteurGrille);
        grt.anchoredPosition = Vector2.zero;   // collée en haut du viewport

        scroll.viewport = vrt;
        scroll.content = grt;

        // À chaque glissement : on regarde si on est arrivé en bas
        shopScroll = scroll;
        scroll.onValueChanged.AddListener(_ => VerifierHintScroll());
        VerifierHintScroll();   // état initial (tout en haut → montré)

        for (int i = 0; i < ShopManager.Skins.Length; i++)
        {
            ShopManager.Skin s = ShopManager.Skins[i];
            int col = i % 3;
            int ligne = i / 3;
            Vector2 cotePos = new Vector2((col - 1) * 340f,
                hauteurGrille * 0.5f - 150f - ligne * 320f);
            BuildSkinCard(gridGO.transform, s, cotePos, i);
        }

        // ---- Message d'info (pas assez de pièces, etc.) ----
        shopMessage = MakeText(go.transform, "ShopMessage", "", 54,
            VoodooStyle.Orange, Pos(-660));
        VoodooStyle.ApplyText(shopMessage, VoodooStyle.Orange, 0.3f);

        // ---- BOUTON RETOUR ----
        GameObject backGO = new GameObject("BackButton",
            typeof(Image), typeof(Button));
        backGO.transform.SetParent(go.transform, false);
        RectTransform brt = backGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(380, 130);
        brt.anchoredPosition = Pos(-800);

        backGO.GetComponent<Button>().onClick.AddListener(OnShopBack);

        TMP_Text backLabel = MakeText(backGO.transform, "Label", Trad.T("RETOUR", "BACK"), 56,
            VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(backLabel, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(backGO, VoodooStyle.Orange);

        Debug.Log("[MENU-START] Boutique construite ✔");

        // Les VRAIS slimes 3D sur les cartes : photos prises une fois,
        // puis recyclées à chaque réouverture de la boutique.
        StartCoroutine(ConstruirePreviewsCoro());
    }

    /// <summary>
    /// Cache la flèche + "PLUS DE SKINS" quand : la grille est arrivée tout
    /// en bas du défilement, OU quand elle ne déroule pas du tout (paysage,
    /// tout tient à l'écran — rien de plus à voir → rien à montrer).
    /// </summary>
    private void VerifierHintScroll()
    {
        if (shopScroll == null) return;

        // Les rects dépendent du canvas : on le recalcule avant de tester
        Canvas.ForceUpdateCanvases();
        bool peutScroller = shopScroll.content != null && shopScroll.viewport != null
            && shopScroll.content.rect.height > shopScroll.viewport.rect.height + 1f;
        bool auBas = peutScroller && shopScroll.verticalNormalizedPosition <= 0.05f;
        bool montrer = peutScroller && !auBas;

        if (plusSkinsLabel != null) plusSkinsLabel.gameObject.SetActive(montrer);
        if (chevronScroll != null) chevronScroll.SetActive(montrer);
    }

    /// <summary>
    /// Une carte de skin : APERÇU DU VRAI SLIME (gelée rendue en 3D, salim
    /// 03/10) + nom + prix / état. Fond de carte identique partout : la
    /// photo 3D a exactement ce fond, donc elle se fond dans la carte.
    /// </summary>
    private void BuildSkinCard(Transform parent, ShopManager.Skin s, Vector2 pos, int index)
    {
        GameObject card = new GameObject("Skin_" + s.id,
            typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(310, 300);
        rt.anchoredPosition = pos;

        ShopManager.Skin copie = s;
        card.GetComponent<Button>().onClick.AddListener(() => OnCardClicked(copie));

        Color fond = new Color(0.18f, 0.23f, 0.50f);   // même fond partout (les photos du studio ont ce fond)
        VoodooStyle.ApplyRounded(card.GetComponent<Image>(), fond);

        // ---- APERÇU du slime (photo 3D du studio) ----
        GameObject apercu = new GameObject("ApercuSlime", typeof(RawImage));
        apercu.transform.SetParent(card.transform, false);
        RectTransform prt = apercu.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.sizeDelta = new Vector2(170, 170);
        prt.anchoredPosition = new Vector2(0f, -18f);
        RawImage rimg = apercu.GetComponent<RawImage>();
        rimg.raycastTarget = false;
        // La photo arrive quelques frames après l'ouverture de la boutique
        // (le studio a besoin de 3 frames pour caler le blob) : en attendant
        // on affiche la pastille couleur, comme avant.
        if (previewTextures != null && index < previewTextures.Length &&
            previewTextures[index] != null)
        {
            rimg.texture = previewTextures[index];
        }
        else
        {
            rimg.color = s.couleur;   // secours : pastille couleur en attendant la photo
        }
        cardPreviews.Add(rimg);

        // ---- Nom du skin ----
        TMP_Text nom = MakeText(card.transform, "Nom", s.nom, 38,
            VoodooStyle.Blanc, new Vector2(0f, -95f));
        VoodooStyle.ApplyText(nom, VoodooStyle.Blanc, 0.25f);

        // ---- Prix / état (EQUIPE / POSSÉDÉ) ----
        string etat;
        Color coul;
        if (ShopManager.EstEquipe(s.id))      { etat = Trad.T("EQUIPE !", "ON !");            coul = new Color(0.35f, 0.85f, 0.35f); }
        else if (ShopManager.EstPossede(s.id)){ etat = Trad.T("POSEDE", "OWNED");             coul = VoodooStyle.Blanc;               }
        else                                   { etat = s.prix + Trad.T(" PIECES", " COINS"); coul = VoodooStyle.Jaune;            }

        TMP_Text etatTxt = MakeText(card.transform, "Etat", etat, 34,
            coul, new Vector2(0f, -130f));
        VoodooStyle.ApplyText(etatTxt, coul, 0.25f);
    }

    /// <summary>Clic sur une carte : ACHAT (équipé direct) puis ENFILEMENT.</summary>
    private void OnCardClicked(ShopManager.Skin s)
    {
        if (ShopManager.EstEquipe(s.id) == false && ShopManager.EstPossede(s.id))
        {
            ShopManager.Equiper(s.id);          // possédé → on l'enfile
            RebuildShop();
        }
        else if (ShopManager.Acheter(s.id))
        {
            RebuildShop();                      // acheté ET équipé direct
        }
        else if (ShopManager.EstEquipe(s.id))
        {
            return;                             // déjà équipé : rien à dire
        }
        else
        {
            // Pas assez de pièces (ou déjà possédé imprévu) : prévenir 2 s.
            StartCoroutine(FlashMessage(Trad.T("PAS ASSEZ DE PIECES !", "NOT ENOUGH COINS !")));
        }
    }

    /// <summary>Petit message rouge au-dessus du bouton RETOUR, qui s'efface seul.</summary>
    private System.Collections.IEnumerator FlashMessage(string message)
    {
        if (shopMessage != null) shopMessage.text = message;
        yield return new WaitForSeconds(1.2f);
        if (shopMessage != null && shopMessage.text == message)
            shopMessage.text = "";
    }

    // ────────────────────────────────────────────────────────────────
    //  STUDIO PHOTO DES SKINS (salim 03/10 : « voir les slimes, pas juste
    //  une couleur »). Le préfab du joueur est instancié 9 fois TRÈS LOIN
    //  de l'arène (y=300), teinté comme chaque skin, et une caméra prend
    //  une photo de chacun → ces photos remplissent les cartes.
    //  Les slimes du studio sont des STATUES : physique gelée, pas d'IA.
    // ────────────────────────────────────────────────────────────────
    private System.Collections.IEnumerator ConstruirePreviewsCoro()
    {
        // Photos déjà prises (réouverture / re-construction) → recycler.
        if (previewTextures != null) { AssignerTexturesAuxCartes(); yield break; }

        GameObject prefabJoueur = GameManager.Instance != null
            ? GameManager.Instance.playerSlimePrefab : null;
        if (prefabJoueur == null)
        {
            Debug.LogWarning("[MENU-START] Pas de préfab joueur : aperçus 3D du shop ignorés.");
            yield break;
        }

        // Le studio photo, très loin du terrain : ne gêne JAMAIS le jeu.
        previewStage = new GameObject("ShopPreviewStage");
        previewStage.transform.position = new Vector3(300f, 300f, 300f);

        // Lumière dédiée (celle du soleil de la scène n'est pas garantie)
        GameObject lum = new GameObject("Lumiere");
        lum.transform.SetParent(previewStage.transform, false);
        lum.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light l = lum.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;

        int n = ShopManager.Skins.Length;
        GameObject[] slimes = new GameObject[n];

        for (int i = 0; i < n; i++)
        {
            GameObject sl = Instantiate(prefabJoueur,
                new Vector3(300f + i * 3f, 300f, 300f), Quaternion.identity);
            slimes[i] = sl;

            // Statue : pas de physique, il fait juste son animation idle.
            Rigidbody rb = sl.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Teinte du skin sur la gelée (la même qu'en jeu, via SlimeVisual).
            SlimeVisual sv = sl.GetComponentInChildren<SlimeVisual>(true);
            if (sv != null)
            {
                sv.OverrideColor = true;
                sv.SlimeColor = ShopManager.Skins[i].couleur;
            }
        }

        // SlimeAnimDriver a une coroutine AutoFit (2 frames) qui cale le blob
        // à la bonne taille, et le driver ré-applique le skin à son Start :
        // on attend 3 frames PUIS re-teinte à la main avant la photo (le
        // Start du driver a pu remettre le look « équipé » sur les clones).
        for (int f = 0; f < 3; f++) yield return null;

        // Caméra de studio : elle ne sert QUE pour prendre les 9 photos.
        // DISABLED : on ne veut PAS qu'elle s'affiche sur l'écran du joueur.
        GameObject camGO = new GameObject("StudioCam");
        camGO.transform.SetParent(previewStage.transform, false);
        Camera cam = camGO.AddComponent<Camera>();
        cam.enabled = false;                  // aucune image sur l'écran, juste Render()
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.23f, 0.50f, 1f);   // = fond des cartes
        cam.farClipPlane = 20f;             // ne voit QUE le studio (l'arène est à 500 m)
        cam.cullingMask = -1;
        cam.fieldOfView = 50f;

        previewTextures = new RenderTexture[n];
        for (int i = 0; i < n; i++)
        {
            // Re-teinte APRÈS le Start de SlimeAnimDriver (qui a réécrit le
            // look des clones), PUIS on attend UNE frame : SlimeVisual pose
            // sa teinte dans un bloc de matériaux au LateUpdate, la photo DOIT
            // attendre qu'elle soit appliquée, sinon la couleur ne se verrait pas.
            SlimeVisual sv = slimes[i] != null
                ? slimes[i].GetComponentInChildren<SlimeVisual>(true) : null;
            if (sv != null)
            {
                sv.OverrideColor = true;
                sv.SlimeColor = ShopManager.Skins[i].couleur;
            }
            yield return null;

            previewTextures[i] = new RenderTexture(256, 256, 16);

            // CADRAGE (salim : « on voit juste le haut ») : on mesure le
            // VRAI corps du slime (bounds) et on cale la caméra pour qu'il
            // tienne ENTIER dans la photo — plus de recette à l'aveugle.
            Renderer rend = slimes[i] != null
                ? slimes[i].GetComponentInChildren<Renderer>() : null;
            Vector3 centre = rend != null
                ? rend.bounds.center
                : slimes[i] != null
                    ? slimes[i].transform.position + new Vector3(0f, 0.8f, 0f)
                    : new Vector3(300f + i * 3f, 300f, 300f);
            float hauteur = rend != null ? rend.bounds.size.y : 2f;
            // distance pour que tout le corps rentre (FOV 50, image carrée) + marge
            float dist = Mathf.Max(0.9f, hauteur * 1.6f);
            cam.transform.position = centre + new Vector3(0f, dist * 0.15f, -dist);
            cam.transform.rotation = Quaternion.LookRotation(
                centre - cam.transform.position, Vector3.up);
            cam.aspect = 1f;
            cam.targetTexture = previewTextures[i];
            cam.Render();    // CLICK : la photo est prise
        }
        cam.targetTexture = null;
        Destroy(camGO);
        previewStage.SetActive(false);   // les slimes dorment, on garde les photos

        AssignerTexturesAuxCartes();
    }

    /// <summary>Colle les photos du studio sur les cartes (une fois prêtes).</summary>
    private void AssignerTexturesAuxCartes()
    {
        if (previewTextures == null) return;
        for (int i = 0; i < cardPreviews.Count; i++)
        {
            if (cardPreviews[i] == null) continue;
            if (i >= previewTextures.Length || previewTextures[i] == null) continue;
            cardPreviews[i].texture = previewTextures[i];
            cardPreviews[i].color = Color.white;   // la pastille de secours s'efface
        }
    }

    /// <summary>Libère le studio photo + les 9 photos (à la fin de la scène).</summary>
    private void NettoyerPreviews()
    {
        if (previewStage != null) Destroy(previewStage);
        previewStage = null;
        if (previewTextures != null)
        {
            for (int i = 0; i < previewTextures.Length; i++)
            {
                if (previewTextures[i] != null) previewTextures[i].Release();
            }
            previewTextures = null;
        }
    }

    private void OnDestroy()
    {
        NettoyerPreviews();          // sinon les photos fuient au rechargement de la scène (Rejouer)
        if (Instance == this) Instance = null;
    }

    private void RebuildShop()
    {
        if (shopCanvas != null) Destroy(shopCanvas.gameObject);
        shopCanvas = null;
        BuildShopPanel();
    }

    private void OnShopBack()
    {
        if (shopCanvas != null)
        {
            Destroy(shopCanvas.gameObject);
            shopCanvas = null;
        }
        shopMessage = null;
        plusSkinsLabel = null;      // détruits avec la boutique
        chevronScroll = null;
        shopScroll = null;
        cardPreviews.Clear();       // les cartes sont détruites, on efface leurs liens
    }

    // ────────────────────────────────────────────────────────────────
    //  PANNEAU REGLAGES (salim 04/10) :
    //   - LANGUE : FRANÇAIS / ENGLISH (choisi ici, mémorisé, et le menu
    //     entier se reconstruit dans la nouvelle langue)
    //   - SON : OUI / NON (et si le téléphone est muet, le jeu se tait aussi)
    //   - FERMER
    // ────────────────────────────────────────────────────────────────
    private void OuvrirReglages()
    {
        if (panneauReglages != null) return;   // déjà ouvert

        GameObject go = new GameObject("ReglagesCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        panneauReglages = go;
        var rc = go.GetComponent<Canvas>();
        rc.renderMode = RenderMode.ScreenSpaceOverlay;
        rc.sortingOrder = 10200;   // au-dessus de la boutique (10100)

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Voile plein écran : il bloque les clics sur le menu derrière.
        GameObject voile = new GameObject("Voile", typeof(Image));
        voile.transform.SetParent(go.transform, false);
        RectTransform vrt = voile.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        voile.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        voile.GetComponent<Image>().raycastTarget = true;

        // Panneau centré (navy, comme la modale QUITTER du jeu)
        GameObject panel = new GameObject("Panneau", typeof(Image));
        panel.transform.SetParent(go.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(860, 900);
        prt.anchoredPosition = Vector2.zero;
        VoodooStyle.ApplyRounded(panel.GetComponent<Image>(),
            new Color(0.13f, 0.17f, 0.40f, 1f));

        float kY = Screen.width > Screen.height ? 0.75f : 1f;
        Vector2 P(float y) => new Vector2(0f, kY * y);

        // Titre
        TMP_Text titre = MakeText(panel.transform, "Titre",
            Trad.T("REGLAGES", "SETTINGS"), 90, VoodooStyle.Jaune, P(330));
        VoodooStyle.ApplyTitle(titre, VoodooStyle.Jaune);

        // BOUTON LANGUE : affiche la langue ACTUELLE, clic = basculer
        GameObject langueGO = new GameObject("BtnLangue", typeof(Image), typeof(Button));
        langueGO.transform.SetParent(panel.transform, false);
        RectTransform lrt = langueGO.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(700, 150);
        lrt.anchoredPosition = P(120);
        TMP_Text txtLangue = MakeText(langueGO.transform, "Label",
            Trad.T("LANGUE : ", "LANGUAGE: ") +
            (Trad.Anglais ? "ENGLISH" : "FRANÇAIS"), 54, VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(txtLangue, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(langueGO, VoodooStyle.BleuNuit);
        langueGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            Trad.Choisir(!Trad.Anglais);   // sauve + applique
            // Le menu entier se reconstruit dans la nouvelle langue.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        // BOUTON SON : OUI / NON (mémorisé dans PlayerPrefs "Son")
        GameObject sonGO = new GameObject("BtnSon", typeof(Image), typeof(Button));
        sonGO.transform.SetParent(panel.transform, false);
        RectTransform srt = sonGO.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(700, 150);
        srt.anchoredPosition = P(-60);
        TMP_Text txtSon = MakeText(sonGO.transform, "Label",
            Trad.T("SON : ", "SOUND: ") +
            (PlayerPrefs.GetInt("Son", 1) == 1
                ? Trad.T("OUI", "ON") : Trad.T("NON", "OFF")),
            54, VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(txtSon, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(sonGO, new Color(0.30f, 0.70f, 0.35f));   // vert
        sonGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            int nouveau = PlayerPrefs.GetInt("Son", 1) == 1 ? 0 : 1;
            PlayerPrefs.SetInt("Son", nouveau);
            PlayerPrefs.Save();
            txtSon.text = Trad.T("SON : ", "SOUND: ") +
                (nouveau == 1 ? Trad.T("OUI", "ON") : Trad.T("NON", "OFF"));
        });

        // BOUTON FERMER
        GameObject fermerGO = new GameObject("BtnFermer", typeof(Image), typeof(Button));
        fermerGO.transform.SetParent(panel.transform, false);
        RectTransform frt = fermerGO.GetComponent<RectTransform>();
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = new Vector2(400, 130);
        frt.anchoredPosition = P(-260);
        TMP_Text txtFermer = MakeText(fermerGO.transform, "Label",
            Trad.T("FERMER", "CLOSE"), 50, VoodooStyle.Blanc, Vector2.zero);
        VoodooStyle.ApplyText(txtFermer, VoodooStyle.Blanc);
        VoodooStyle.MakePuffy(fermerGO, new Color(0.88f, 0.32f, 0.28f));   // rouge doux
        fermerGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            Destroy(go);
            panneauReglages = null;
        });
    }

    /// <summary>
    /// Dessine une ICÔNE ENGRENAVAGE en code (Texture2D 128×128, cercle
    /// central + 8 dents). JAMAIS de caractère TMP : la font n'a pas les
    /// symboles → carrés.
    /// </summary>
    private static Texture2D TexEngrenage()
    {
        int taille = 128;
        Texture2D tex = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float centre = (taille - 1) / 2f;
        for (int y = 0; y < taille; y++)
        {
            for (int x = 0; x < taille; x++)
            {
                float dx = x - centre;
                float dy = y - centre;
                float r = Mathf.Sqrt(dx * dx + dy * dy);

                // angle pour les dents (8 dents)
                float angle = Mathf.Atan2(dy, dx);
                float dent = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 8f + 0.5f, 1f) - 0.5f) * 2f;

                // Dents : entre rayon 0,32 et 0,46, mais seulement là où dent < 0,35
                bool corps = r < 0.34f * centre;                    // rond plein
                bool dents = r < 0.48f * centre && dent < 0.4f;     // dents pointues
                // Trou central (creux du gear)
                bool trou = r < 0.14f * centre;

                float alpha = 0f;
                if (corps) alpha = 1f;
                else if (dents) alpha = 1f - Mathf.SmoothStep(0.36f, 0.48f, r / centre);
                if (trou) alpha = 0f;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>Petit triangle vers le BAS (indique qu'on peut glisser).</summary>
    private static Texture2D TexTriangleBas()
    {
        int taille = 64;
        Texture2D tex = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < taille; y++)
        {
            for (int x = 0; x < taille; x++)
            {
                // Triangle : large au en haut, pointe en bas
                float progres = 1f - y / (float)(taille - 1);      // 0 en haut → 1 en bas
                float demiLargeur = Mathf.Lerp(taille * 0.48f, 1f, progres);
                float distDuCentre = Mathf.Abs((taille - 1) / 2f - x);
                float alpha = distDuCentre < demiLargeur ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private TMP_Text MakeText(Transform parent, string name, string content,
        float size, Color color, Vector2 pos)
    {
        GameObject t = new GameObject(name,
            typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(parent, false);
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 180);
        rt.anchoredPosition = pos;

        var txt = t.GetComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = size;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        return txt;
    }

    // ────────────────────────────────────────────────────────────────
    //  LE BOUTON
    // ────────────────────────────────────────────────────────────────
    public void OnPlayClicked()
    {
        playedOnce = true;
        StartRequested = true;
        if (canvas != null) Destroy(canvas.gameObject);
        canvas = null;
        enabled = false;
        Debug.Log("[MENU-START] JOUER appuyé — c'est parti ! 🎮");
    }
}