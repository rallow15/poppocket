using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD des power-ups : quand un bonus est ramassé (par le joueur OU un bot),
/// une ligne s'affiche en haut de l'écran avec :
///   - le nom du bonus (VITESSE / AURA / ZAP)
///   - QUI l'a activé (Toi / Bot 2 ...)
///   - les secondes qui défilent
/// La ligne disparaît toute seule à la fin du bonus (3 s).
/// </summary>
public class PowerUpHud : MonoBehaviour
{
    public static PowerUpHud Instance { get; private set; }

    private class Entry
    {
        public string label;      // "ECLAIR : VITESSE x4" etc.
        public string who;        // "Toi" / "Bot 2"
        public Color slimeColor;  // couleur du slime qui a le bonus
        public float until;       // Time.time de fin
    }

    private const float DURATION = 3f;   // même durée que les effets (PowerUpManager)

    private static readonly Color[] SlimeColors =
    {
        new Color(0.2f, 0.6f, 1f),   // Joueur : bleu
        new Color(1f, 0.3f, 0.3f),   // Bot 1 : rouge
        new Color(0.3f, 1f, 0.4f),   // Bot 2 : vert
        new Color(1f, 0.8f, 0.2f)    // Bot 3 : jaune
    };

    private readonly List<Entry> entries = new();
    private TextMeshProUGUI text;

    private void Awake()
    {
        Instance = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildUI()
    {
        GameObject go = new GameObject("PowerUpHudCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;    // au-dessus du HUD de jeu, sous le menu

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // salim 03/10 v3 : plus de PANNEAU derrière l'info (salim test n°2 :
        // « un gros carré transparent apparaît » — on le retire, le texte
        // seul avec son gros contour navy est lisible sur le jeu).
        GameObject t = new GameObject("ActiveList",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(go.transform, false);
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(1040f, 300f);
        rt.anchoredPosition = new Vector2(0f, -250f);   // sous le chrono

        text = t.GetComponent<TextMeshProUGUI>();
        text.fontSize = 60;
        text.alignment = TextAlignmentOptions.Top;
        text.raycastTarget = false;

        // Style voodoo PLUS marqué : lettres dilatées + contour épais + ombre.
        // La couleur d'affichage vient du rich text (<color=…>) ligne par ligne.
        VoodooStyle.ApplyText(text, VoodooStyle.Blanc, 0.45f);
    }

    private void Update()
    {
        if (text == null) return;
        entries.RemoveAll(e => Time.time >= e.until);
        if (entries.Count == 0)
        {
            text.text = "";
            // salim 04/10 : plus d'info power → le chrono revient
            GameUI.SetChronoVisible(true);
            return;
        }

        string all = "";
        foreach (var e in entries)
        {
            string hex = ColorUtility.ToHtmlStringRGB(e.slimeColor);
            float left = Mathf.Max(0f, e.until - Time.time);
            all += $"<b><color=#{hex}>{e.who}</color></b> : {e.label}  <color=#ffffff>{left:0.0}s</color>\n";
        }
        text.text = all;
    }

    // ────────────────────────────────────────────────────────────────
    //  APPELÉ PAR PowerUp.OnTriggerEnter (joueur OU bot)
    // ────────────────────────────────────────────────────────────────
    public static void Show(PowerUpType type, SlimeController sc)
    {
        if (Instance == null || sc == null) return;

        string bonus = type == PowerUpType.Speed
            ? Trad.T("ECLAIR : VITESSE x4", "BOLT : SPEED x4")
            : type == PowerUpType.Aura
            ? Trad.T("CHAMPIGNON : GEANT x3", "MUSHROOM : GIANT x3")
            : Trad.T("FLOCON : les autres sont geles", "SNOWFLAKE : the others are frozen");
        string who = sc.isPlayer ? Trad.T("TOI", "YOU") : $"Bot {sc.slimeIndex}";

        var entry = new Entry
        {
            label = bonus,
            who = who,
            slimeColor = SlimeColors[Mathf.Clamp(sc.slimeIndex, 0, 3)],
            until = Time.time + DURATION
        };

        // Max 4 lignes (une par slime) : la plus ancienne saute
        Instance.entries.Add(entry);
        if (Instance.entries.Count > 4) Instance.entries.RemoveAt(0);

        // salim 04/10 : une info power s'affiche → on CACHE le chrono
        GameUI.SetChronoVisible(false);
    }
}