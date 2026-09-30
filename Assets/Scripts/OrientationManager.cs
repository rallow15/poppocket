using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Gère l'orientation du jeu (Portrait / Landscape) et adapte l'UI.
/// En pratique : laisse l'appareil tourner librement, mais réagit à chaque
/// changement en repositionnant les éléments d'interface.
/// </summary>
public class OrientationManager : MonoBehaviour
{
    public static OrientationManager Instance { get; private set; }

    [Header("Réglages UI adaptative")]
    [Tooltip("Panneaux à repositionner quand l'orientation change")]
    public RectTransform[] adaptivePanels;

    [Tooltip("Offsets en mode PAYSAGE (l'UI est plus basse, plus large)")]
    public Vector2 landscapePadding = new Vector2(30f, 30f);
    [Tooltip("Offsets en mode PORTRAIT (l'UI est plus étroite)")]
    public Vector2 portraitPadding = new Vector2(30f, 60f);

    private bool currentlyPortrait;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentlyPortrait = Screen.height > Screen.width;
        ApplyOrientation();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Sur Android/iOS la rotation automatique est gérée par l'OS,
        // on l'autorise explicitement dans les deux sens.
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    private void Update()
    {
        // Surveille si l'utilisateur a tourné son téléphone
        bool nowPortrait = Screen.height > Screen.width;
        if (nowPortrait != currentlyPortrait)
        {
            currentlyPortrait = nowPortrait;
            ApplyOrientation();
        }
    }

    /// <summary>
    /// Force une orientation spécifique (bouton "Mode Paysage" dans les menus).
    /// </summary>
    public void SetOrientation(bool portrait)
    {
        Screen.orientation = portrait ? ScreenOrientation.Portrait
                                      : ScreenOrientation.LandscapeLeft;
        currentlyPortrait = Screen.height > Screen.width;
        ApplyOrientation();
    }

    /// <summary>Laisse l'OS décider de l'orientation (mode auto).</summary>
    public void SetAutoOrientation()
    {
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    /// <summary>Repositionne les panneaux UI selon l'orientation actuelle.</summary>
    private void ApplyOrientation()
    {
        Vector2 padding = currentlyPortrait ? portraitPadding : landscapePadding;

        foreach (RectTransform panel in adaptivePanels)
        {
            if (panel == null) continue;

            // Repositionne le panneau : même taille, mais décalé selon le padding
            // de l'orientation courante (bord supérieur du parent comme référence).
            panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,
                                                padding.y,
                                                panel.rect.height);
            panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left,
                                                padding.x,
                                                panel.rect.width);
        }

        Debug.Log($"[OrientationManager] Application padding " +
                  $"{(currentlyPortrait ? "PORTRAIT" : "PAYSAGE")} : {padding}");
    }

    /// <summary>UI pour tester : bascule Portrait <-> Paysage manuellement.</summary>
    public void ToggleOrientation()
    {
        bool nextPortrait = Screen.height <= Screen.width; // si actuel paysage -> portrait
        SetOrientation(nextPortrait);
    }
}