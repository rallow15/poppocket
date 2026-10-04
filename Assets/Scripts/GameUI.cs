using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interface utilisateur du jeu : chrono, scores des 4 slimes, panneau de
/// fin de round, écran final avec gagnant + bouton Rejouer.
/// Toutes les références UI sont glissées depuis l'éditeur (voir guide).
/// TMP_Text marche pour TextMeshPro ET Text Legacy (fallback via TMPro).
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("Chrono")]
    [Tooltip("Texte affichant le temps restant du round")]
    public TMP_Text timerText;

    [Header("Scores live (4 textes, un par slime)")]
    public TMP_Text[] scoreTexts = new TMP_Text[4];

    [Header("Panneau fin de round (rappels des scores)")]
    public GameObject roundEndPanel;
    public TMP_Text roundEndTitleText;
    public TMP_Text roundEndScoresText;

    [Header("Panneau final : grand gagnant")]
    public GameObject winnerPanel;
    public TMP_Text winnerTitleText;
    public TMP_Text winnerScoreText;
    public Button replayButton;   // bouton "Rejouer"

    // salim 03/10 : bouton discret "X" en haut à gauche pour quitter la partie
    private Button quitterBouton;
    private TMP_Text quitterLabel;
    private Image quitterImage;
    // salim 03/10 v2 (test n°2) : la confirmation "QUITTER ?" sur le bouton
    // est remplacée par une VRAIE MODALE au milieu de l'écran :
    //   voile sombre + panneau arrondi + titre + 2 boutons puffy
    //   (QUITTER = retour menu, ANNULER = on continue la partie).
    private GameObject modaleQuitter;   // le panneau central (créé 1 fois)
    private Canvas quitCanvas;          // canvas du bouton X (la modale vit dedans)
    private float timeScaleAvantQuitter = 1f;   // remis si on ANNULE

    // salim 04/10 : « quand info power apparait cache le chrono ». Le chrono
    // (timerText) est caché via SetActive pendant qu'une info power-up est
    // affichée, et revient dès qu'il n'y en a plus. Static + copié dans
    // chronoCache : PowerUpHud appelle GameUI.SetChronoVisible sans référence.
    private static TMP_Text chronoCache;

    /// <summary>Cache/remontre le chrono (appelé par PowerUpHud).</summary>
    public static void SetChronoVisible(bool visible)
    {
        // Unity "obj == null" marche même si l'objet a été détruit
        if (chronoCache != null && chronoCache.gameObject.activeSelf != visible)
            chronoCache.gameObject.SetActive(visible);
    }

    [Header("Couleurs des slimes (pour colorier les scores)")]
    public Color[] slimeColors = new Color[4]
    {
        new Color(0.2f, 0.6f, 1f),   // Joueur : bleu
        new Color(1f, 0.3f, 0.3f),   // Bot 1 : rouge
        new Color(0.3f, 1f, 0.4f),   // Bot 2 : vert
        new Color(1f, 0.8f, 0.2f)    // Bot 3 : jaune
    };

    private void Start()
    {
        chronoCache = timerText;   // salim 04/10 : passerelle chrono ↔ info power

        // Cache les panneaux au démarrage
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(false);

        // Bouton Rejouer -> GameManager.OnReplayButton (voir guide : drag & drop)
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() =>
                GameManager.Instance?.OnReplayButton());
        }

        LandscapeFix();   // sécurité : score visible aussi en PAYSAGE
        ApplyVoodoo();    // style voodoo sur chrono, scores, panneaux

        // salim 03/10 : chaque bouton est créé TO indépendamment — si un
        // plante, l'autre existe quand même (mieux que pas de bouton du tout).
        CreerBoutonQuitter();  // salim 03/10 : quitter la partie à tout moment
        CreerBoutonMenu();     // salim 03/10 : « je veux bouton menu » à la fin
    }

    // ────────────────────────────────────────────────────────────────
    //  FIX PAYSAGE (salim 03/10 : « je voit pas le score »)
    //  On reforce le coin haut-droit du bloc de scores + chrono, et on
    //  garantit un SafeAreaFitter v2 (compense la Dynamic Island latérale).
    // ────────────────────────────────────────────────────────────────
    private void LandscapeFix()
    {
        GameObject block = GameObject.Find("ScoreBlock");
        if (block == null && scoreTexts.Length > 0 && scoreTexts[0] != null)
            block = scoreTexts[0].transform.parent != null
                ? scoreTexts[0].transform.parent.gameObject : null;

        if (block != null && block.GetComponent<RectTransform>() is RectTransform brt)
        {
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-25f, -95f);
            EnsureFitter(brt);
        }

        if (timerText != null)
        {
            RectTransform trt = timerText.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            // Dynamic Island (salim 03/10) : on forcer le chrono bien PLUS BAS,
            // l'île déborde sur les 95-110 pixels du haut (portrait ET paysage).
            if (trt.anchoredPosition.y > -150f)
                trt.anchoredPosition = new Vector2(0f, -175f);
            EnsureFitter(trt);
        }
    }

    private static void EnsureFitter(RectTransform rt)
    {
        var fitter = rt.GetComponent<SafeAreaFitter>();
        if (fitter == null) fitter = rt.gameObject.AddComponent<SafeAreaFitter>();
        else fitter.ResetBase();   // la base = la position qu'on vient de forcer
    }

    // ────────────────────────────────────────────────────────────────
    //  STYLE VOODOO : grosses lettres grasses + contour épais + ombre
    // ────────────────────────────────────────────────────────────────
    private void ApplyVoodoo()
    {
        // Chrono : TRÈS lisible
        if (timerText != null)
        {
            VoodooStyle.ApplyText(timerText, VoodooStyle.Blanc, 0.35f);
            timerText.fontSize = Mathf.Max(timerText.fontSize, 96f);
        }

        // Scores : couleur de chaque slime + contour sombre
        for (int i = 0; i < scoreTexts.Length; i++)
        {
            if (scoreTexts[i] == null) continue;
            VoodooStyle.ApplyText(scoreTexts[i], slimeColors[i], 0.4f);
            scoreTexts[i].fontSize = Mathf.Max(scoreTexts[i].fontSize, 42f);
        }

        // Panneaux fin de round / gagnant
        if (roundEndTitleText != null)
        {
            VoodooStyle.ApplyTitle(roundEndTitleText, VoodooStyle.Blanc);
            roundEndTitleText.fontSize = Mathf.Max(roundEndTitleText.fontSize, 80f);
        }
        if (roundEndScoresText != null)
        {
            VoodooStyle.ApplyText(roundEndScoresText, VoodooStyle.Blanc, 0.3f);
            roundEndScoresText.fontSize = Mathf.Max(roundEndScoresText.fontSize, 48f);
        }
        if (winnerTitleText != null)
        {
            VoodooStyle.ApplyTitle(winnerTitleText, VoodooStyle.Jaune);
            winnerTitleText.fontSize = Mathf.Max(winnerTitleText.fontSize, 88f);
        }
        if (winnerScoreText != null)
        {
            VoodooStyle.ApplyText(winnerScoreText, VoodooStyle.Blanc, 0.3f);
            winnerScoreText.fontSize = Mathf.Max(winnerScoreText.fontSize, 56f);
        }

        // Bouton Rejouer : puffy jaune + label blanc stylé
        if (replayButton != null)
        {
            foreach (TMP_Text t in replayButton.GetComponentsInChildren<TMP_Text>(true))
                VoodooStyle.ApplyText(t, VoodooStyle.Blanc);
            VoodooStyle.MakePuffy(replayButton.gameObject, VoodooStyle.Jaune);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  CALLBACKS REÇUS DE GAME MANAGER
    // ────────────────────────────────────────────────────────────────
    public void OnRoundStart(int round, int duration)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(false);

        // Nouveau round → le chrono repart forcément VISIBLE
        SetChronoVisible(true);

        if (roundEndTitleText != null)
            roundEndTitleText.text = Trad.T($"ROUND {round}", $"ROUND {round}");
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText == null) return;

        int seconds = Mathf.Max(0, (int)Mathf.Ceil(timeLeft));
        timerText.text = seconds.ToString();

        // Urgence : les 10 dernières secondes en rouge
        timerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    public void UpdateScores(int[] scores, int currentRound)
    {
        for (int i = 0; i < scoreTexts.Length; i++)
        {
            if (scoreTexts[i] == null) continue;

            scoreTexts[i].text = scores[i].ToString();
            scoreTexts[i].color = slimeColors[i]; // couleur identitaire du slime
        }
    }

    /// <summary>Panneau affiché pendant la pause de 5s entre les rounds.</summary>
    public void ShowRoundEndPanel(int finishedRound, int[] scores)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(true);

        if (roundEndTitleText != null)
            roundEndTitleText.text = Trad.T($"FIN DU ROUND {finishedRound}",
                                            $"ROUND {finishedRound} OVER");

        if (roundEndScoresText != null)
        {
            // Construit le mini classement
            string lines = "";
            for (int i = 0; i < scores.Length; i++)
            {
                string name = i == 0 ? Trad.T("Toi", "You") : $"Bot {i}";
                lines += $"{name} : {scores[i]} pts\n";
            }
            roundEndScoresText.text = lines;
        }
    }

    /// <summary>Écran final : gagnant + score + bouton Rejouer visible.</summary>
    public void ShowWinner(string winnerName, int winnerScore)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(true);

        if (winnerTitleText != null)
            winnerTitleText.text = $"{winnerName} {Trad.T("GAGNE !", "WINS !")}";

        if (winnerScoreText != null)
            winnerScoreText.text = $"{winnerScore} {Trad.T("points", "points")}";
    }

    // ────────────────────────────────────────────────────────────────
    //  salim 03/10 : « à la fin de partie, Rejouer effet 3D + bouton MENU »
    //  Rejouer a DÉJÀ son effet 3D (MakePuffy) → on ajoute juste le bouton
    //  MENU, créé en code juste SOUS Rejouer, même style puffy (orange).
    //  Un clic recharge la scène en ré-affichant le MENU (RevenirAuMenu
    //  annule le « menu sauté » de la 1re partie).
    // ────────────────────────────────────────────────────────────────
    private void CreerBoutonMenu()
    {
        if (replayButton == null) return;

        GameObject go = new GameObject("BoutonMenu", typeof(Image), typeof(Button));
        RectTransform prt = replayButton.transform as RectTransform;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(prt.parent, false);
        // même ancrage que Rejouer, taille un peu plus petite, posé en dessous
        rt.anchorMin = prt.anchorMin;
        rt.anchorMax = prt.anchorMax;
        rt.pivot = prt.pivot;
        rt.sizeDelta = new Vector2(prt.sizeDelta.x * 0.85f, prt.sizeDelta.y * 0.72f);
        rt.anchoredPosition = prt.anchoredPosition + new Vector2(0f, -(prt.sizeDelta.y * 0.72f + 24f));

        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            StartMenu.RevenirAuMenu();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        GameObject lab = new GameObject("Label",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        lab.transform.SetParent(go.transform, false);
        var lt = lab.GetComponent<TextMeshProUGUI>();
        lt.text = "MENU";
        lt.fontSize = 58;
        lt.alignment = TextAlignmentOptions.Center;
        lt.rectTransform.anchorMin = Vector2.zero;
        lt.rectTransform.anchorMax = Vector2.one;
        lt.rectTransform.offsetMin = Vector2.zero;
        lt.rectTransform.offsetMax = Vector2.zero;
        VoodooStyle.ApplyText(lt, VoodooStyle.Blanc, 0.35f);

        VoodooStyle.MakePuffy(go, VoodooStyle.Orange);   // même relief 3D que Rejouer
    }

    // ────────────────────────────────────────────────────────────────
    //  salim 03/10 : « bouton discret en haut à gauche pour quitter ».
    //  Petit "X" arrondi translucide dans le SafeArea (Dynamic Island).
    //  Un appui → MODALE de confirmation au centre (voir AfficherModaleQuitter).
    // ────────────────────────────────────────────────────────────────
    private void CreerBoutonQuitter()
    {
        GameObject cv = new GameObject("QuitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = cv.GetComponent<Canvas>();
        quitCanvas = canvas;   // la modale de confirmation vivra dans ce canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;   // au-dessus du jeu, sous le HUD power-up

        var scaler = cv.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject btnGO = new GameObject("BoutonQuitter", typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cv.transform, false);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(130f, 100f);
        rt.anchoredPosition = new Vector2(26f, -28f);
        btnGO.AddComponent<SafeAreaFitter>();   // compense Dynamic Island / coins

        quitterImage = btnGO.GetComponent<Image>();
        VoodooStyle.ApplyRounded(quitterImage, new Color(1f, 1f, 1f, 0.22f));   // discret

        GameObject lab = new GameObject("Label",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        lab.transform.SetParent(btnGO.transform, false);
        quitterLabel = lab.GetComponent<TextMeshProUGUI>();
        quitterLabel.text = "X";
        quitterLabel.fontSize = 52;
        quitterLabel.alignment = TextAlignmentOptions.Center;
        quitterLabel.rectTransform.anchorMin = Vector2.zero;
        quitterLabel.rectTransform.anchorMax = Vector2.one;
        quitterLabel.rectTransform.offsetMin = Vector2.zero;
        quitterLabel.rectTransform.offsetMax = Vector2.zero;
        VoodooStyle.ApplyText(quitterLabel, VoodooStyle.Blanc, 0.35f);

        quitterBouton = btnGO.GetComponent<Button>();
        quitterBouton.onClick.AddListener(OnQuitter);
    }

    private void OnQuitter()
    {
        // salim 03/10 v2 : un appui sur le "X" ouvre la MODALE de confirmation
        // au milieu de l'écran (plus de "QUITTER ?" éphémère sur le bouton).
        AfficherModaleQuitter();
    }

    // ────────────────────────────────────────────────────────────────
    //  MODALE "QUITTER ?" (salim : « un modale s'ouvre au milieu de l'ecran
    //  avec chois quitter ou anuler pour continuer la partie »)
    //  Structure :
    //    Voile       : Image NOIRE 55 % plein écran — assombrit le jeu ET
    //                  bloque les clics sur le jeu pendant le choix.
    //    ModaleQuitter : panneau arrondi navy, centré, 760 x 460.
    //       Titre    : "QUITTER ?" (jaune, gros, style voodoo).
    //       Bouton QUITTER (orange puffy), à gauche  → retour au MENU.
    //       Bouton ANNULER (bleu puffy), à droite    → ferme, on continue.
    //  Pendant la modale le jeu est MIS EN PAUSE (timeScale = 0) — le
    //  timeScale d'avant est remis tel quel si on ANNULE (marche aussi
    //  pendant la pause entre deux rounds, déjà à 0).
    // ────────────────────────────────────────────────────────────────
    private void AfficherModaleQuitter()
    {
        if (quitCanvas == null) return;

        // Déjà créée une fois ? on la ré-affiche telle quelle
        if (modaleQuitter != null)
        {
            modaleQuitter.transform.parent.gameObject.SetActive(true);
            timeScaleAvantQuitter = Time.timeScale;
            Time.timeScale = 0f;
            return;
        }

        timeScaleAvantQuitter = Time.timeScale;
        Time.timeScale = 0f;   // le jeu attend le choix

        // 1) LE VOILE : assombrit TOUT l'écran et mange les clics du jeu
        GameObject voile = new GameObject("VoileModale", typeof(Image));
        voile.transform.SetParent(quitCanvas.transform, false);
        RectTransform vrt = voile.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.pivot = new Vector2(0.5f, 0.5f);
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        // Image SANS sprite : plein écran noir ; raycastTarget true = bloque
        // tout clic du jeu derrière (propre : impossible de jouer en même temps).
        voile.GetComponent<Image>().color = new Color(0f, 0f, 0.08f, 0.55f);

        // 2) LE PANNEAU CENTRAL arrondi (look voodoo)
        GameObject panneau = new GameObject("ModaleQuitter", typeof(Image));
        panneau.transform.SetParent(voile.transform, false);
        RectTransform prt = panneau.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(760f, 470f);
        prt.anchoredPosition = Vector2.zero;
        VoodooStyle.ApplyRounded(panneau.GetComponent<Image>(),
            new Color(0.10f, 0.14f, 0.36f, 0.97f));   // navy bien opaque, lisible

        // 3) LE TITRE "QUITTER ?"
        GameObject titreGO = new GameObject("Titre",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        titreGO.transform.SetParent(panneau.transform, false);
        var titre = titreGO.GetComponent<TextMeshProUGUI>();
        titre.text = Trad.T("QUITTER ?", "QUIT ?");
        titre.fontSize = 68;
        titre.alignment = TextAlignmentOptions.Center;
        RectTransform trt = titreGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(30f, -195f);   // bande haute du panneau
        trt.offsetMax = new Vector2(-30f, -45f);
        VoodooStyle.ApplyText(titre, VoodooStyle.Jaune, 0.45f);

        // 4) LES 2 BOUTONS puffy côte à côte en bas du panneau
        CreerBoutonModale(panneau.transform, "BoutonQuitterOui",
            Trad.T("QUITTER", "QUIT"),
            VoodooStyle.Orange, new Vector2(-205f, -325f), true);
        CreerBoutonModale(panneau.transform, "BoutonQuitterAnnuler",
            Trad.T("ANNULER", "CANCEL"),
            new Color(0.45f, 0.55f, 0.85f), new Vector2(205f, -325f), false);

        modaleQuitter = panneau;
    }

    /// <summary>
    /// Bouton puffy de la modale (taille / position en repère 1080 px).
    /// confirm = true → QUITTER la partie ; false → ANNULER (continuer).
    /// </summary>
    private void CreerBoutonModale(Transform parent, string nom, string texte,
        Color couleur, Vector2 pos, bool confirm)
    {
        GameObject go = new GameObject(nom, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(330f, 150f);
        rt.anchoredPosition = pos;

        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (confirm)
            {
                // QUITTER → retour au MENU (timeScale remis à 1 avant reload)
                Time.timeScale = 1f;
                StartMenu.RevenirAuMenu();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                // ANNULER → on ferme la modale et on REPREND la partie
                // exactement comme elle était (0 si pause de fin de round).
                Time.timeScale = timeScaleAvantQuitter;
                if (modaleQuitter != null)
                    modaleQuitter.transform.parent.gameObject.SetActive(false);
            }
        });

        GameObject lab = new GameObject("Label",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        lab.transform.SetParent(go.transform, false);
        var lt = lab.GetComponent<TextMeshProUGUI>();
        lt.text = texte;
        lt.fontSize = 54;
        lt.alignment = TextAlignmentOptions.Center;
        lt.rectTransform.anchorMin = Vector2.zero;
        lt.rectTransform.anchorMax = Vector2.one;
        lt.rectTransform.offsetMin = Vector2.zero;
        lt.rectTransform.offsetMax = Vector2.zero;
        VoodooStyle.ApplyText(lt, VoodooStyle.Blanc, 0.35f);

        VoodooStyle.MakePuffy(go, couleur);
    }
}