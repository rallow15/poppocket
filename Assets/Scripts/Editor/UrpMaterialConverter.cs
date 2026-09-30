#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Convertit les matériaux d'un objet en URP (les packs Asset Store
/// sont souvent en shaders "Built-in" → ROSES en URP sans ça).
/// Garde couleur, texture, émission et transparence. Idempotent :
/// un matériau déjà URP n'est jamais touché.
/// </summary>
internal static class UrpMaterialConverter
{
    /// <summary>
    /// Convertit tous les matériaux du GameObject (et de ses enfants).
    /// Retourne true si quelque chose a changé (au moins 1 matériau converti).
    /// </summary>
    internal static bool Convert(GameObject go)
    {
        bool changed = false;
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            bool instanceChanged = false;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;
                if (mats[m].shader != null &&
                    mats[m].shader.name.StartsWith("Universal")) continue; // déjà URP
                var urpMat = UrpCopy(mats[m]);
                if (urpMat != null && urpMat != mats[m])
                {
                    mats[m] = urpMat;
                    instanceChanged = true;
                }
            }
            if (instanceChanged)
            {
                renderer.sharedMaterials = mats;   // override sur l'instance seulement
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>Copie un matériau Built-in en URP/Lit (réutilisé si déjà créé).</summary>
    internal static Material UrpCopy(Material original)
    {
        if (original.shader == null) return original;
        if (original.shader.name.StartsWith("Universal")) return original;

        string dir = "Assets/Materials/Packs";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Materials", "Packs");
        string path = dir + "/" + original.name + "_URP.mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        // Transparent ? (shader "Transparent"/"Alpha" ou mot-clé _ALPHABLEND_ON)
        string shaderName = original.shader.name;
        bool transparent = shaderName.Contains("Transparent") || shaderName.Contains("Alpha") ||
                           System.Array.IndexOf(original.shaderKeywords, "_ALPHABLEND_ON") >= 0;

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) return original;   // pas d'URP dans le projet : on garde

        var mat = new Material(urpShader) { name = original.name + "_URP" };

        // Couleur
        Color color = Color.white;
        if (original.HasProperty("_Color")) color = original.color;
        if (original.HasProperty("_BaseColor")) color = original.GetColor("_BaseColor");
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);

        // Texture principale
        if (original.HasProperty("_MainTex") && original.mainTexture != null &&
            mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", original.mainTexture);

        // Émission (panneaux lumineux, etc.)
        if (System.Array.IndexOf(original.shaderKeywords, "_EMISSION") >= 0 &&
            original.HasProperty("_EmissionColor") && mat.HasProperty("_EmissionColor"))
        {
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", original.GetColor("_EmissionColor"));
        }

        // Transparence (style URP Lit transparent)
        if (transparent)
        {
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_Surface", 1f);          // 1 = transparent
            mat.SetFloat("_Blend", 0f);            // alpha
            mat.SetFloat("_SrcBlend", 5f);         // SrcAlpha
            mat.SetFloat("_DstBlend", 10f);        // OneMinusSrcAlpha
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
#endif