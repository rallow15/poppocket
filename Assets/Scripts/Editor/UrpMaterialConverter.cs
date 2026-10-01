#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Répare les matériaux ROSES (shader magenta) des packs Asset Store.
///
/// On détecte le VRAI pipeline du projet (URP ou Built-in) et on rebranche
/// chaque matériau cassé sur le shader du projet ("Standard" ici).
///
/// RUSE IMPORTANTE : quand le shader est cassé, l'API Unity n'expose AUCUNE
/// propriété (ni couleur, ni texture) → on relit directement le fichier .mat
/// (YAML) pour retrouver la vraie couleur + texture (ex. atlas des animaux).
///
/// On répare le matériau D'ORIGINE du pack (pas une copie) : guid stable,
/// la scène reste branchée. Idempotent (matériau réparé = shader non cassé).
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
        if (IsUrp())
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null) return urp;
        }
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

                // Ancienne copie "..._PopPocket" créée pendant la première
                // réparation : MÊME si son shader est correct (Standard sans
                // texture = blanc), on la rebranche sur l'original du pack.
                bool isOldCopy = mats[m].name.EndsWith("_PopPocket");
                if (!isOldCopy && !IsBroken(mats[m])) continue;

                Material target = ResolveOriginal(mats[m]);
                Heal(target);                      // rebranche sur le bon shader + textures
                if (target != mats[m]) instanceChanged = true;
                mats[m] = target;
            }
            if (instanceChanged)
            {
                renderer.sharedMaterials = mats;   // override sur l'instance seulement
                changed = true;
            }
        }
        AssetDatabase.SaveAssets();
        return changed;
    }

    // ────────────────────────────────────────────────────────────────
    //  RÉPARATION DU MATÉRIAU (SUR PLACE)
    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Rebranche un matériau cassé sur le shader du projet, sans perdre
    /// sa couleur ni sa texture (relues dans le fichier .mat si besoin).
    /// </summary>
    private static void Heal(Material broken)
    {
        Shader target = TargetShader();
        if (target == null) return;

        // On lit la couleur + la texture AVANT de changer le shader
        // (tant que le shader est cassé, l'API expose rien → lecture YAML).
        Texture tex;
        Color color;
        ExtractLookFromYaml(broken, out tex, out color);

        if (broken.shader != target) broken.shader = target;

        // Couleur (Standard lit "_Color", URP lit "_BaseColor")
        if (broken.HasProperty("_Color")) broken.SetColor("_Color", color);
        if (broken.HasProperty("_BaseColor")) broken.SetColor("_BaseColor", color);
        if (broken.HasProperty("_Smoothness")) broken.SetFloat("_Smoothness", 0.3f);
        if (broken.HasProperty("_Glossiness")) broken.SetFloat("_Glossiness", 0.3f);

        // Texture principale (atlas des animaux, façades des bâtiments...)
        if (tex != null)
        {
            if (broken.HasProperty("_MainTex") && broken.GetTexture("_MainTex") == null)
                broken.SetTexture("_MainTex", tex);
            if (broken.HasProperty("_BaseMap") && broken.GetTexture("_BaseMap") == null)
                broken.SetTexture("_BaseMap", tex);
        }

        EditorUtility.SetDirty(broken);
    }

    // ────────────────────────────────────────────────────────────────
    //  ANCIENNES COPIES "..._PopPocket" → ON RETROUVE L'ORIGINAL
    // ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Si le matériau est une de nos anciennes copies "_PopPocket",
    /// retrouve le matériau d'origine du pack (même nom, sans le suffixe)
    /// pour le réparer LUI et rebrancher le renderer dessus.
    /// </summary>
    private static Material ResolveOriginal(Material mat)
    {
        const string suffix = "_PopPocket";
        if (!mat.name.EndsWith(suffix)) return mat;
        string baseName = mat.name.Substring(0, mat.name.Length - suffix.Length);

        foreach (string guid in AssetDatabase.FindAssets(baseName + " t:Material"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            var candidate = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (candidate != null && candidate.name == baseName) return candidate;
        }
        return mat;   // rien trouvé : on réparera la copie elle-même
    }

    // ────────────────────────────────────────────────────────────────
    //  LECTURE YAML DU .mat (quand le shader est cassé, l'API ne
    //  expose aucune propriété : on lit le fichier texte)
    // ────────────────────────────────────────────────────────────────
    private static void ExtractLookFromYaml(Material mat, out Texture tex, out Color color)
    {
        tex = null;
        color = Color.white;

        // Déjà réparable via l'API (shader lisible) ? Alors on lit le matériau.
        Shader s = mat.shader;
        bool apiReadable = s != null && !s.name.StartsWith("Hidden/");
        if (apiReadable)
        {
            if (mat.HasProperty("_Color")) color = mat.color;
            if (mat.HasProperty("_BaseColor")) color = mat.GetColor("_BaseColor");
            if (mat.HasProperty("_MainTex")) tex = mat.mainTexture;
            if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
            if (tex != null) return;   // texture OK : rien de plus à faire
        }

        // Lecture texte du fichier .mat (couleur + guid de texture)
        string path = AssetDatabase.GetAssetPath(mat);
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return;

        string[] lines = System.IO.File.ReadAllLines(path);
        bool inTexEnvs = false, inColors = false;
        string current = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();

            // ── entrées "- _Nom:" (texture ou couleur) ──────────────
            if (trimmed.StartsWith("- _"))
            {
                int colon = trimmed.IndexOf(':');
                current = colon > 0 ? trimmed.Substring(2, colon - 2) : null;

                // La valeur peut être sur la même ligne (m_Colors)
                string rest = colon >= 0 ? trimmed.Substring(colon + 1) : "";
                if (current != null && inColors &&
                    (current == "_BaseColor" || current == "_Color") &&
                    rest.Contains("{"))
                {
                    color = ParseColor(rest);
                    current = null;
                }
                // "- _BaseColor:" tout seul = l'objet vient à la ligne → on garde current
                continue;
            }

            // ── sections ───────────────────────────────────────────
            if (trimmed == "m_TexEnvs:") { inTexEnvs = true; inColors = false; current = null; continue; }
            if (trimmed == "m_Colors:") { inColors = true; inTexEnvs = false; current = null; continue; }
            if (trimmed.StartsWith("m_Ints") || trimmed.StartsWith("m_BuildTextureStacks") ||
                trimmed.StartsWith("m_SavedProperties"))
            { inTexEnvs = false; inColors = false; current = null; continue; }

            if (current == null) continue;

            // ── ligne m_Texture: dans une entrée de texture ────────
            if (inTexEnvs && (current == "_BaseMap" || current == "_MainTex" || current == "_BaseColorMap") &&
                trimmed.StartsWith("m_Texture:"))
            {
                var guidMatch = Regex.Match(trimmed, @"guid:\s*([0-9a-fA-F]+)");
                if (guidMatch.Success && guidMatch.Groups[1].Value != "00000000000000000000000000000000")
                {
                    string texPath = AssetDatabase.GUIDToAssetPath(guidMatch.Groups[1].Value);
                    if (!string.IsNullOrEmpty(texPath))
                        tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                }
                current = null;   // on ne prend que la première texture utile
            }
        }
    }

    /// <summary>{r: 1, g: 1, b: 1, a: 1} → Color</summary>
    private static Color ParseColor(string yaml)
    {
        var nums = Regex.Match(yaml, @"r:\s*(-?[\d.]+),\s*g:\s*(-?[\d.]+),\s*b:\s*(-?[\d.]+)(?:,\s*a:\s*(-?[\d.]+))?");
        if (!nums.Success) return Color.white;
        float r = float.Parse(nums.Groups[1].Value, CultureInfo.InvariantCulture);
        float g = float.Parse(nums.Groups[2].Value, CultureInfo.InvariantCulture);
        float b = float.Parse(nums.Groups[3].Value, CultureInfo.InvariantCulture);
        float a = nums.Groups[4].Success
            ? float.Parse(nums.Groups[4].Value, CultureInfo.InvariantCulture) : 1f;
        return new Color(r, g, b, a);
    }
}
#endif