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
        public string label;      // "⚡ VITESSE x4" etc.
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

        GameObject t = new GameObject("ActiveList",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(go.transform, false);
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(1000, 240);
        rt.anchoredPosition = new Vector2(0, -260);   // sous le chrono

        text = t.GetComponent<TextMeshProUGUI>();
        text.fontSize = 52;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private void Update()
    {
        if (text == null) return;
        entries.RemoveAll(e => Time.time >= e.until);
        if (entries.Count == 0) { text.text = ""; return; }

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

        string bonus = type == PowerUpType.Speed ? "⚡ VITESSE x4"
                     : type == PowerUpType.Aura ? "✨ AURA d'éclatement"
                                                : "🌩️ ZAP (a figé les autres)";
        string who = sc.isPlayer ? "TOI" : $"Bot {sc.slimeIndex}";

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
    }
}