#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Répare les matériaux ROSES (shader magenta) des packs Asset Store.
///
/// Astuce : on détecte le VRAI pipeline du projet (URP ou Built-in) et on
/// convertit TOUT MATÉRIAU CASSÉ vers le shader du projet :
///  - shader introuvable / shader d'erreur (Hidden/InternalErrorShader) → ROSE
///  - dans un projet URP : un shader "Standard" (Built-in) serait rose aussi
///
/// Garde couleur, texture, émission et transparence. Idempotent :
/// le .mat converti est réutilisé (et réparé) au lieu d'être recréé.
/// </summary>
internal static class UrpMaterialConverter
{
    /// <summary>Le projet tourne-t-il avec la Render Pipeline Universelle ?</summary>
    internal static bool IsUrp()
    {
        try { return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null; }
        catch { return false; }
    }

    private static Shader TargetShader()
    {
        // ICI : Built-in (pas de URP dans ce projet) → "Standard".
#if UNITY_6000_0_OR_NEWER
        if (IsUrp())
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null) return urp;
        }
#else
        if (IsUrp())
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null) return urp;
        }
#endif
        Shader s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Mobile/Diffuse");   // secours ultime
        return s;
    }

    /// <summary>Est-ce que ce matériau est cassé / magenta dans ce pipeline ?</summary>
    private static bool IsBroken(Material mat)
    {
        Shader s = mat.shader;
        if (s == null) return true;                       // shader introuvable
        string n = s.name;
        if (string.IsNullOrEmpty(n)) return true;
        if (n.StartsWith("Hidden/")) return true;         // shader d'erreur → ROSE
        if (IsUrp() && !n.Contains("Universal")) return true; // URP : shader Built-in → ROSE
        return false;
    }

    /// <summary>
    /// Convertit tous les matériaux cassés du GameObject (et de ses enfants).
    /// Retourne true si quelque chose a changé.
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
                if (!IsBroken(mats[m])) continue;
                var fixedMat = FixedCopy(mats[m]);
                if (fixedMat != null && fixedMat != mats[m])
                {
                    mats[m] = fixedMat;
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

    /// <summary>
    /// Recopie un matériau cassé avec le shader du projet.
    /// Réutilise le .mat déjà créé (même guid → la scène reste branchée).
    /// </summary>
    internal static Material FixedCopy(Material original)
    {
        string dir = "Assets/Materials/Packs";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Materials", "Packs");
        string path = dir + "/" + original.name + "_PopPocket.mat";

        Shader target = TargetShader();
        if (target == null) return original;

        // Réutilise le .mat déjà créé (on le répare s'il porte encore un shader rose)
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(target) { name = original.name + "_PopPocket" };
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != target)
        {
            mat.shader = target;
        }

        CopyProperties(original, mat, target);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CopyProperties(Material source, Material mat, Shader target)
    {
        // Couleur
        Color color = Color.white;
        if (source.HasProperty("_Color")) color = source.color;
        if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.3f);

        // Texture principale
        Texture tex = null;
        if (source.HasProperty("_MainTex")) tex = source.mainTexture;
        if (source.HasProperty("_BaseMap")) tex = source.GetTexture("_BaseMap");
        if (tex != null)
        {
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        }

        // Émission (panneaux lumineux, etc.)
        if (source.HasProperty("_EmissionColor"))
        {
            Color emis = source.GetColor("_EmissionColor");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emis);
                if (emis.maxColorComponent > 0.01f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else mat.DisableKeyword("_EMISSION");
            }
        }

        // Transparence
        string shaderName = source.shader != null ? source.shader.name : "";
        bool transparent = shaderName.Contains("Transparent") || shaderName.Contains("Alpha") ||
                           System.Array.IndexOf(source.shaderKeywords, "_ALPHABLEND_ON") >= 0;
        if (transparent)
        {
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 2f);   // Fade (Standard)
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", 5f);         // SrcAlpha
            mat.SetFloat("_DstBlend", 10f);        // OneMinusSrcAlpha
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_ALPHABLEND_ON");
        }
        else
        {
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 0f);   // opaque (Standard)
            mat.SetOverrideTag("RenderType", "");
            mat.SetFloat("_SrcBlend", 1f);         // One
            mat.SetFloat("_DstBlend", 0f);         // Zero
            mat.SetInt("_ZWrite", 1);
            mat.renderQueue = -1;
            mat.DisableKeyword("_ALPHABLEND_ON");
        }
    }
}
#endif