using UnityEngine;

/// <summary>
/// Gère la SAFE AREA sur les 4 CÔTÉS de l'écran (encoches, rond, Dynamic Island).
/// Fix « en mode paysage je voit pas le score » (salim, 03/10) : en paysage,
/// la Dynamic Island est SUR LE CÔTÉ — l'ancienne version ne compensait que
/// le haut, donc les scores ancrés à droite passaient sous l'île.
/// Règle : le décalage est appliqué SELON L'ANCRAGE de l'élément —
///   • ancré à droite  → on pousse de l'inset droit
///   • ancré à gauche  → on pousse de l'inset gauche
///   • ancré en haut   → on descend de l'inset haut
///   • ancré en bas    → on monte de l'inset bas
///   • ancré au centre → on ne touche pas ce côté
/// </summary>
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;
    private Vector2 basePos;                      // position posée à l'installation
    private Vector2 appliedInsets = new Vector2(float.MinValue, float.MinValue);
    private bool recorded;

    private void OnEnable() { rt = transform as RectTransform; Apply(); }
    private void Update() { Apply(); }            // réagit à la rotation du téléphone

    private void Apply()
    {
        if (rt == null) return;

        if (!recorded)
        {
            basePos = rt.anchoredPosition;
            recorded = true;
        }

        Rect sa = Screen.safeArea;
        float left = sa.x;
        float right = Screen.width - sa.xMax;
        float top = Screen.height - sa.yMax;
        float bottom = sa.y;

        var insets = new Vector2(left + right, top + bottom);
        if (Mathf.Approximately(insets.x, appliedInsets.x) &&
            Mathf.Approximately(insets.y, appliedInsets.y)) return;
        appliedInsets = insets;

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;

        // Choix des côtés à décaler selon notre ancrage (l'ancrage dit
        // « je suis collé à ce bord »).
        float dx = 0f;
        if (rt.anchorMin.x > 0.99f && rt.anchorMax.x > 0.99f) dx = -(left + right) / scale;
        else if (rt.anchorMin.x < 0.01f && rt.anchorMax.x < 0.01f) dx = (left + right) / scale;

        float dy = 0f;
        if (rt.anchorMin.y > 0.99f && rt.anchorMax.y > 0.99f) dy = -(top + bottom) / scale;
        else if (rt.anchorMin.y < 0.01f && rt.anchorMax.y < 0.01f) dy = (top + bottom) / scale;

        rt.anchoredPosition = basePos + new Vector2(dx, dy);
    }

    /// <summary>
    /// Re-mémorise la position actuelle comme "base" (après qu'un script
    /// a repositionné l'élément) — sinon la prochaine rotation le
    /// ramènerait à l'ancienne position.
    /// </summary>
    public void ResetBase()
    {
        if (rt != null) { basePos = rt.anchoredPosition; }
    }
}