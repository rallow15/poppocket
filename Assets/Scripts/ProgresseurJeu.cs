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

    // salim 04/10 : « rajoute jusqu'à 10 NIVEAU » — le jeu s'arrête au 10 :
    // gagner la partie du niveau 10 ne débloque plus rien (c'est le sommet).
    public const int NiveauMax = 10;

    // Niveau CHOISI sur l'écran d'accueil (bouton 1 / 2 / 3…). Gardé en
    // mémoire de session seulement : on peut TOUJOURS rejouer un niveau
    // déjà débloqué, sans jamais dépasser le niveau max gagné.
    private static int choisi = 0;

    /// <summary>
    /// Le niveau que le joueur a choisi de jouer. Par défaut = le niveau
    /// max débloqué. Toujours ≤ Niveau (jamais un niveau verrouillé).
    /// </summary>
    public static int Choisi
    {
        get { return Mathf.Clamp(choisi >= 1 ? choisi : Niveau, 1, Niveau); }
        set { choisi = Mathf.Max(1, value); }
    }

    /// <summary>Niveau actuel (1 = forêt, 2 = plage, …). Toujours ≥ 1.</summary>
    public static int Niveau
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(Cle, 1), 1, NiveauMax); }
    }

    /// <summary>Le joueur gagne SA partie : on passe au niveau suivant (max 10).</summary>
    public static void PasserNiveauSuivant()
    {
        if (Niveau >= NiveauMax)
        {
            Debug.Log("[NIVEAU] Victoire du joueur ✔ → niveau " + NiveauMax +
                      " (DERNIER) déjà débloqué : rien de plus à gagner.");
            return;
        }
        PlayerPrefs.SetInt(Cle, Niveau + 1);
        PlayerPrefs.Save();
        choisi = Niveau;   // la sélection suit : le nouveau max est présélectionné
        Debug.Log("[NIVEAU] Victoire du joueur ✔ → niveau " + Niveau + " DEBLOQUE !");
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