using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void Awake()
    {
        Instance = this;
        StartRequested = playedOnce;   // Rejouer → on attaque direct
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

        // Fond coloré doux (bleu nuit du jeu)
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.10f, 0.22f, 0.97f);
        bg.raycastTarget = true;       // le fond bloque les clics pendant le menu

        // ---- TITRE ----
        TMP_Text title = MakeText(go.transform, "TitleText", "POP POCKET", 150,
            new Color(0.4f, 0.9f, 1f), new Vector2(0, 460));

        // ---- SOUS-TITRE ----
        MakeText(go.transform, "SubtitleText", "Slime & Fidgets", 70,
            new Color(1f, 0.85f, 0.3f), new Vector2(0, 320));

        // ---- décor d'emojis slimes ----
        MakeText(go.transform, "EmojiRow", "🟢  🔴  🟢  🟡", 90,
            Color.white, new Vector2(0, 120));

        // ---- BOUTON JOUER ----
        GameObject btnGO = new GameObject("PlayButton",
            typeof(Image), typeof(Button));
        btnGO.transform.SetParent(go.transform, false);
        RectTransform brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(500, 170);
        brt.anchoredPosition = new Vector2(0, -170);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.95f, 0.6f, 0.15f);   // orange vif

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(OnPlayClicked);

        // Label du bouton
        TMP_Text label = MakeText(btnGO.transform, "Label", "JOUER  ▶", 90, Color.white, Vector2.zero);
        label.fontStyle = FontStyles.Bold;

        // ---- AIDE en bas ----
        MakeText(go.transform, "HelpText",
            "Glisse le doigt pour te déplacer\nTAP l'écran pour sauter\nEclate le plus de bulles !",
            44, new Color(1f, 1f, 1f, 0.75f), new Vector2(0, -640));

        Debug.Log("[MENU-START] Menu construit ✔");
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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}