// ══════════════════════════════════════════════════════════════════════
//  IMPORT DES SPRITES KENNEY UI (salim 03/10 : « j'ai un pack ui »)
//  Règle d'import UNIQUEMENT pour Assets/Resources/KenneyUI :
//    - mipmap OFF      : sprite d'interface, jamais vu en vue de loin
//    - NPOT None       : 192x64 reste 192x64 (sinon Unity l'étire en
//                        256x64 et les coins arrondis se déforment !)
//    - Clamp           : on ne répète pas une texture d'UI
//  Éditeur uniquement (jamais au runtime — règle du projet). Le pack
//  d'origine dans Downloads n'est jamais touché : on avait juste COPIÉ
//  le PNG utile dans le projet.
//
//  salim 04/10 : même soin pour Resources/Effets (les petits PNG du
//  pack NamuFX pour l'éclat de bulle) — SANS COMPRESSION, sinon les
//  dégradés doux de l'eau deviennent des BLOCS visibles (effet pixel).
// ══════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class KenneyUIPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath.Contains("Effets"))
        {
            TextureImporter effet = (TextureImporter)assetImporter;
            effet.mipmapEnabled = false;              // jamais vu de loin
            effet.alphaIsTransparency = true;         // contours sans halo blanc
            effet.npotScale = TextureImporterNPOTScale.None;
            effet.textureCompression = TextureImporterCompression.Uncompressed;
            effet.maxTextureSize = 4096;              // résolution max, zéro bloc
            effet.filterMode = FilterMode.Bilinear;
            effet.wrapMode = TextureWrapMode.Clamp;
            return;
        }

        if (!assetPath.Contains("KenneyUI")) return;   // tout le reste : normal

        TextureImporter imp = (TextureImporter)assetImporter;
        imp.mipmapEnabled = false;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.maxTextureSize = 512;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
    }

    // ── Un coup de main : si les PNG de l'éclat ont été importés AVANT
    //    cette règle (donc compressés → blocs visibles), on les fait
    //    ré-importer UNE fois. Guard « dejaReimporte » : sinon il se
    //    ré-importerait en boucle infinie. ──────────────────────────────
    private static bool dejaReimporte;
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
        string[] movedTo, string[] movedFrom)
    {
        if (dejaReimporte) return;

        string[] pngEclat =
        {
            "Assets/Resources/Effets/EclatBulle.png",
            "Assets/Resources/Effets/BouleEau.png"
        };

        bool aFaire = false;
        foreach (string p in pngEclat)
        {
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && imp.textureCompression != TextureImporterCompression.Uncompressed)
                aFaire = true;
        }
        if (!aFaire) return;

        dejaReimporte = true;               // une seule fois, pas de boucle
        foreach (string p in pngEclat)
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
    }
}
#endif