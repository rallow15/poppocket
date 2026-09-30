using UnityEngine;

/// <summary>
/// Pousse un élément d'UI (ancré en HAUT) en dessous de l'encoche /
/// de la Dynamic Island. iPhone masquait le chrono → ce script ajoute
/// automatiquement la hauteur de la zone non-sûre (safe area) comme
/// marge, en plus de la position de base enregistrée à l'installation.
/// S'adapte tout seul si l'utilisateur tourne le téléphone.
/// </summary>
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;
    private Vector2 basePos;                 // position enregistrée (sans encoche)
    private float appliedInset = float.MinValue;
    private bool recorded;

    private void OnEnable() { rt = transform as RectTransform; Apply(); }
    private void Update() { Apply(); }   // réagit au changement d'orientation

    private void Apply()
    {
        if (rt == null) return;

        // Mémorise la position de base UNE fois (celle posée par l'installer)
        if (!recorded)
        {
            basePos = rt.anchoredPosition;
            recorded = true;
        }

        // Distance entre le haut de l'écran et le haut de la zone sûre
        // (= encoche / Dynamic Island). 0 sur les écrans sans encoche.
        float insetPixels = Screen.height - Screen.safeArea.yMax;
        if (Mathf.Approximately(insetPixels, appliedInset)) return;
        appliedInset = insetPixels;

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;

        // Convertit les pixels d'encoche en unités du canvas, puis descend
        rt.anchoredPosition = basePos + new Vector2(0f, -insetPixels / scale);
    }
}