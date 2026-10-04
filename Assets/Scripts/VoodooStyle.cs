using UnityEngine;
using UnityEngine.EventSystems;   // PuffyButton (appui du doigt sur le bouton)
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Style « VOODOO » (jeux hyper-casuels : grosse typo ronde TRÈS grasse,
/// contour épais, ombre portée, couleurs vives, boutons puffy).
/// salim (03/10) : « ameliorer la police du menu et des marqueur de point
/// du chrono des info quand power pris style voodoo ».
///
/// 100 % en code, BUILT-IN render pipeline :
///  • Texte   : matériel TMP instancié PAR TEXTE (fontMaterial — jamais de
///             fichier .mat sur disque) avec OUTLINE_ON + UNDERLAY_ON du
///             vrai shader TMP_SDF → contour épais + ombre en dessous.
///  • Panneau : sprite arrondi (rounded box) GÉNÉRÉ en code, Image.type=Sliced
///             → s'étire sans déformer les angles.
///  • Bouton  : image arrondie + couche d'ombre sombre décalée dessous
///             (effet « puffy » qu'on peut appuyer).
/// Aucune dépendance UnityEditor : marche en runtime / build.
/// </summary>
public static class VoodooStyle
{
    // ── Palette (vives, type hyper-casuel) ───────────────────────────
    public static readonly Color BleuNuit = new Color(0.13f, 0.17f, 0.42f);
    public static readonly Color Contour  = new Color(0.06f, 0.08f, 0.20f);  // contour sombre navy
    public static readonly Color Jaune    = new Color(1.00f, 0.80f, 0.15f);
    public static readonly Color Orange   = new Color(1.00f, 0.55f, 0.15f);
    public static readonly Color Blanc    = new Color(1f, 1f, 1f);

    // ── POLICE DU JEU : HOLLY BERRY POP (salim 04/10 : « change la police »,
    //    zip de sa libéralité, ttf copié dans Assets/Resources/Fonts) ─────
    // TMP ne sait pas créer son asset tout seul : on le construit À LA
    // VOLÉE une seule fois avec TMP_FontAsset.CreateFontAsset — tout est
    // en mémoire, rien n'est écrit sur disque (règle du projet).
    private static TMP_FontAsset bebas;

    public static TMP_FontAsset BebasNeue()
    {
        if (bebas != null) return bebas;

        var ttf = Resources.Load<Font>("Fonts/HollyBerryPop");
        if (ttf == null)      // Holly absent : retour arrière sur Bebas (jamais casser)
            ttf = Resources.Load<Font>("Fonts/BebasNeue-Regular");
        if (ttf == null) return null;      // aucune police : on ne casse rien

        bebas = TMP_FontAsset.CreateFontAsset(ttf);
        if (bebas != null) bebas.name = "PoliceJeuRuntime";
        return bebas;
    }

    // ── TEXTE : contour épais + ombre au niveau du SHADER (les composants
    //    UGUI Shadow/Outline ne marchent PAS sur TMP) ──────────────────
    /// <summary>Gros style gras : contour épais + ombre portée (look Voodoo).</summary>
    public static void ApplyText(TMP_Text t, Color fill, float outlineWidth = 0.4f)
    {
        ApplyText(t, fill, Contour, outlineWidth);
    }

    public static void ApplyText(TMP_Text t, Color fill, Color outline, float outlineWidth)
    {
        if (t == null) return;
        t.fontStyle = FontStyles.Bold;

        // SALIM 03/10 : tout le texte passe en BEBAS NEUE. Si le ttf n'est
        // pas trouvé (police absente), on garde l'ancien comportement :
        // reparer les textes orphelins avec la police TMP par défaut, et
        // jamais lever d'exception (le bug qui tuait tout GameUI.Start).
        var bebasAsset = BebasNeue();
        if (bebasAsset != null)
        {
            t.font = bebasAsset;    // matériau valide recréé avec la police
        }
        else if (t.fontSharedMaterial == null)
        {
            var defaut = TMP_Settings.defaultFontAsset;
            if (defaut == null || defaut.material == null) return;   // rien à faire : pas d'exception
            t.fontSharedMaterial = defaut.material;                  // texte orphelin réparé
        }

        // fontMaterial crée une instance du matériel pour CE texte seulement
        // (pas d'asset .mat sur disque — règle du projet).
        var mat = t.fontMaterial;
        mat.EnableKeyword("OUTLINE_ON");
        mat.EnableKeyword("UNDERLAY_ON");
        mat.EnableKeyword("DILATE");                       // active le _FaceDilate
        mat.SetFloat("_FaceDilate", 0.12f);                // glyphes PLUS ÉPAISSES
        mat.SetColor("_OutlineColor", outline);            // navy très sombre
        mat.SetFloat("_OutlineWidth", outlineWidth);       // 0.4 = contour "chunky"
        mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.45f));
        mat.SetFloat("_UnderlayOffsetX", 0f);
        mat.SetFloat("_UnderlayOffsetY", -0.25f);          // normalisé (-1..1) ≈ 2 texels
        mat.SetFloat("_UnderlayDilate", 0f);
        mat.SetFloat("_UnderlaySoftness", 0.05f);
        t.color = fill;
    }

    /// <summary>Variante « titre géant » : contour encore plus épais.</summary>
    public static void ApplyTitle(TMP_Text t, Color fill)
    {
        ApplyText(t, fill, Contour, 0.45f);
    }

    // ── SPRITE ARRONDI (généré une fois, réutilisé partout) ──────────
    private static Sprite roundedSprite;

    public static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite != null) return roundedSprite;
            const int S = 64;
            const float R = 22f;                        // rayon des coins (px)
            var tex = new Texture2D(S, S, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[S * S];
            float half = S * 0.5f;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    // distance signée au rectangle aux coins arrondis (SDF)
                    float qx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - half + R, 0f);
                    float qy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - half + R, 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy)
                            + Mathf.Min(Mathf.Max(qx, qy), 0f) - R;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                    px[y * S + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            roundedSprite = Sprite.Create(tex, new Rect(0, 0, S, S),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(16, 16, 16, 16));           // bordures Sliced
            roundedSprite.name = "VoodooRounded";
            return roundedSprite;
        }
    }

    /// <summary>Transforme une Image en panneau arrondi doux.</summary>
    public static void ApplyRounded(Image img, Color color)
    {
        if (img == null) return;
        img.type = Image.Type.Sliced;
        img.sprite = RoundedSprite;
        img.color = color;
    }

    // ── SPRITE KENNEY (salim 03/10 : « j'ai un pack ui ») ────────────
    /// <summary>
    /// Le bouton du Kenney UI Pack (button_rectangle_depth_gloss, CC0) :
    /// la PROFONDEUR + le GLOSS sont déjà DESSINÉS dans le sprite. Sa face
    /// est BLANCHE → Image.color la teinte avec n'importe quelle couleur
    /// et le relief cuit reste bon. Bordure 9-slice 20 px : les coins
    /// restent ronds même quand le bouton s'étire en grand.
    /// Chargé en Resources → marche aussi en build. Cache une seule fois.
    /// </summary>
    private static Sprite kenneyBouton;

    public static Sprite SpriteBoutonKenney()
    {
        if (kenneyBouton != null) return kenneyBouton;
        var tex = Resources.Load<Texture2D>("KenneyUI/BoutonGris");
        if (tex == null) return null;   // pack pas installé : fallback code
        kenneyBouton = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(20f, 20f, 20f, 20f));
        kenneyBouton.name = "BoutonKenney";
        return kenneyBouton;
    }

    // ── BOUTON PUFFY 3D (salim 03/10 : « bouton menu plus effet 3d ») ─
    /// <summary>
    /// BOUTON À BLOC de jouet. Avec le sprite Kenney en Resources, ON NE
    /// CONSTRUIT PLUS RIEN EN CODE : la profondeur et le gloss vivent dans
    /// le sprite, il ne reste que l'appui (PuffyButton). Sans le sprite,
    /// les 3 couches code ci-dessous prennent le relais (fallback) :
    ///   1. PuffyShadow : couche sombre GROSSE décalée en dessous (l'épaisseur
    ///      du bouton, comme un bonbon qu'on pourrait saisir sur le côté),
    ///   2. PuffyTop    : reflet clair DOUX en haut de la face (light gloss),
    ///   3. la face colorée elle-même, remontée AU-DESSUS du bloc.
    /// Et PuffyButton (ajouté à la fin) fait SINKER la face quand on appuie :
    /// le bouton descend sur son bloc, on dirait qu'on le pousse dedans.
    /// </summary>
    public static void MakePuffy(GameObject btn, Color color)
    {
        if (btn == null) return;

        RectTransform rt = btn.transform as RectTransform;

        // ---- 0) LE BOUTON KENNEY (si présent dans Resources) ----
        // Une seule Image : sprite 9-slice teinté par Image.color (face
        // blanche du pack). Plus de couches PuffyShadow/PuffyTop : la
        // profondeur est CUITE dans le sprite. Pas de remontée +14 non
        // plus — et l'appui descend moins (7 px, le relief dessiné
        // fait déjà ~10 px visuels).
        Sprite kenney = SpriteBoutonKenney();
        if (kenney != null)
        {
            var img0 = btn.GetComponent<Image>();
            if (img0 != null)
            {
                img0.sprite = kenney;
                img0.type = Image.Type.Sliced;
                img0.color = color;
            }

            // sécurité : si du code-ancien avait déjà posé les couches
            // (pas le cas d'un bouton tout neuf, mais on nettoie quand même)
            var vieux = btn.transform.Find("PuffyShadow");
            if (vieux != null) Object.Destroy(vieux.gameObject);
            var vieuxTop = btn.transform.Find("PuffyTop");
            if (vieuxTop != null) Object.Destroy(vieuxTop.gameObject);

            if (btn.GetComponent<PuffyButton>() == null) btn.AddComponent<PuffyButton>();
            btn.GetComponent<PuffyButton>().profondeurAppui = 7f;
            return;
        }

        // ---- 1) LE BLOC DESSOUS (l'épaisseur du bonbon) : GROS décalage ----
        if (btn.transform.Find("PuffyShadow") == null)
        {
            GameObject shadow = new GameObject("PuffyShadow", typeof(Image));
            shadow.transform.SetParent(btn.transform, false);
            var srt = shadow.transform as RectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            // épaisseur GRANDE (20 px de plus de chaque côté en bas) → relief net
            srt.offsetMin = new Vector2(-6f, -20f);
            srt.offsetMax = new Vector2(6f, -4f);
            var simg = shadow.GetComponent<Image>();
            ApplyRounded(simg, color * 0.50f);          // même teinte, bien sombre
            simg.raycastTarget = false;
            srt.SetSiblingIndex(0);                     // toujours dessous
        }

        // ---- 2) LE REFLET EN HAUT (light gloss sur le dessus de la face) ----
        if (btn.transform.Find("PuffyTop") == null)
        {
            GameObject top = new GameObject("PuffyTop", typeof(Image));
            top.transform.SetParent(btn.transform, false);
            var trt = top.transform as RectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            // une petite bande arrondie collée en haut de la face
            trt.offsetMin = new Vector2(14f, -46f);
            trt.offsetMax = new Vector2(-14f, -6f);
            var timg = top.GetComponent<Image>();
            ApplyRounded(timg, new Color(1f, 1f, 1f, 0.30f));
            timg.raycastTarget = false;
            trt.SetSiblingIndex(1);                     // sous le label, sur la face
        }

        // ---- 3) LA FACE (la couleur du bouton, posée SUR le bloc) ----
        var img = btn.GetComponent<Image>();
        if (img != null) ApplyRounded(img, color);
        if (rt != null)
        {
            Vector2 p = rt.anchoredPosition;
            p.y += 14f;                                 // face soulevée sur le bloc
            rt.anchoredPosition = p;
        }

        // ---- 4) L'APPUI : le bouton descend quand on pousse ----
        if (btn.GetComponent<PuffyButton>() == null) btn.AddComponent<PuffyButton>();
    }
}

// ────────────────────────────────────────────────────────────────────
//  APPUI DU BOUTON : toute la face S'ENFONCE vers son bloc quand le
//  doigt presse (et remonte quand on relâche). Sensation « pousser
//  un bouton de jouet ». 100 % runtime, aucune asset.
// ────────────────────────────────────────────────────────────────────
public class PuffyButton : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler,
                                          UnityEngine.EventSystems.IPointerUpHandler,
                                          UnityEngine.EventSystems.IPointerExitHandler
{
    [Tooltip("De combien de pixels la face descend au clic (le relief est de 14 px)")]
    public float profondeurAppui = 12f;

    private RectTransform rt;
    private Vector2 positionRepos;
    private bool appele;

    private void Awake()
    {
        rt = transform as RectTransform;
        if (rt != null) positionRepos = rt.anchoredPosition;
    }

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (rt == null || appele) return;
        appele = true;
        // la face descend SUR le bloc + se tasse un tout petit peu (clic de jouet)
        rt.anchoredPosition = positionRepos + new Vector2(0f, -profondeurAppui);
        transform.localScale = new Vector3(0.97f, 0.97f, 1f);
    }

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Relacher();
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Relacher();
    }

    private void Relacher()
    {
        if (rt == null || !appele) return;
        appele = false;
        rt.anchoredPosition = positionRepos;
        transform.localScale = new Vector3(1f, 1f, 1f);
    }
}