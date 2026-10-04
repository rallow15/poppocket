using UnityEngine;

/// <summary>
/// PROGRESSION DES NIVEAUX (demande de salim, 03/10) :
/// « si on gagne le niveau 1 on passe à un niveau suivant, les bots
///    légèrement plus fort et le niveau change de design ».
///
/// • Le joueur GAGNE sa partie (3 rounds) → niveau +1, gardé dans
///   PlayerPrefs (survit à un redémarrage du jeu).
/// • Le niveau décide de DEUX choses :
///     1. le décor (niveau 1 = forêt kawaii, niveau 2 = plage tropicale)
///     2. la force des bots (GameManager / SlimeController.AppliquerNiveau)
/// • Le joueur PERD → il reste au même niveau et peut réessayer.
/// Classe statique simple : aucune case à glisser, accessible partout
/// (GameManager, GameUI, et les menus de test POPPOCKET 12/13).
/// </summary>
public static class ProgresseurJeu
{
    const string Cle = "PopPocket.Niveau";

    /// <summary>Niveau actuel (1 = forêt, 2 = plage, …). Toujours ≥ 1.</summary>
    public static int Niveau
    {
        get { return Mathf.Max(1, PlayerPrefs.GetInt(Cle, 1)); }
    }

    /// <summary>Le joueur gagne SA partie : on passe au niveau suivant.</summary>
    public static void PasserNiveauSuivant()
    {
        PlayerPrefs.SetInt(Cle, Niveau + 1);
        PlayerPrefs.Save();
        Debug.Log("[NIVEAU] Victoire du joueur ✔ → niveau " + Niveau);
    }

    /// <summary>Force un niveau précis (menus de test POPPOCKET/12 et /13).</summary>
    public static void ForcerNiveau(int n)
    {
        n = Mathf.Max(1, n);
        PlayerPrefs.SetInt(Cle, n);
        PlayerPrefs.Save();
        Debug.Log("[NIVEAU] Niveau réglé à " + Niveau);
    }
}