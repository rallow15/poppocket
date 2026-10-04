using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MONNAIE + SKINS du shop (demande de salim, 03/10 : « un shop dans le menu
/// où tu pourras acheter des skins pour changer » tout en « gardant le slime
/// identique »).
///
/// • Chaque bulle éclatée par le JOUEUR = 1 pièce (salim : choix « mes bulles
///   éclatées »). On la crédite AVEC le score, dans GameManager.AddScore.
/// • Un skin = une TEINTE de la gelée du pack : le slime garde son look
///   gelée officiel (reflets, réfraction, cœur lumineux) — seule la couleur
///   change. Le look est posé par SlimeAnimDriver via SlimeVisual.
/// • Tout est sauvegardé dans PlayerPrefs : rien n'est perdu en quittant le jeu.
/// • Prix choisis par salim : skin CHER (600 à 1200 pièces = plusieurs parties).
/// </summary>
public static class ShopManager
{
    /// <summary>Un skin du shop : juste un nom + une couleur + un prix.</summary>
    public class Skin
    {
        public string id;        // clé technique (PlayerPrefs)
        public string nom;       // affiché dans le shop
        public Color couleur;    // teinte appliquée à la gelée
        public int prix;         // en pièces (0 = skin de départ)
        public bool defaut;      // true = skin de départ (gratuit, déjà possédé)
    }

    /// <summary>Pièces données pour CHAQUE bulle éclatée par le joueur.
    /// (salim : 1 pièce / bulle. Si un jour c'est trop long à farmer,
    /// il suffit de monter ce chiffre à 2 ou 5.)</summary>
    public const int PiecesParBulle = 1;

    // ── La LISTE DES SKINS (salim a choisi des prix « chers ») ────────
    // Les 4 premières couleurs sont celles du jeu (pack), le reste est NEW.
    public static readonly Skin[] Skins =
    {
        Skn("defaut",  "Bleu",        0.14509805f, 0.18133720f, 0.74509805f,    0, true),
        Skn("vert",    "Vert pomme",  0.14509805f, 0.74509805f, 0.22963527f,  600, false),
        Skn("jaune",   "Soleil",      0.90964930f, 0.91509430f, 0.08201314f,  650, false),
        Skn("rouge",   "Cerise",      0.74509805f, 0.14509805f, 0.15476716f,  700, false),
        Skn("rose",    "Rose bonbon", 1.00f,       0.40f,       0.75f,        750, false),
        Skn("violet",  "Violet",      0.60f,       0.30f,       1.00f,        800, false),
        Skn("orange",  "Orange",      1.00f,       0.50f,       0.12f,        900, false),
        Skn("glacier", "Glacier",     0.35f,       0.90f,       0.98f,       1000, false),
        Skn("dore",    "Dore",        1.00f,       0.72f,       0.15f,       1200, false),
        Skn("neige",   "Neige",       0.93f,       0.96f,       1.00f,        950, false),
        Skn("minuit",  "Minuit",      0.08f,       0.09f,       0.14f,       1100, false),
        Skn("turquoise","Turquoise",  0.00f,       0.75f,       0.80f,       1050, false),
    };

    /// <summary>Petite usine pour garder la liste lisible ci-dessus.</summary>
    private static Skin Skn(string id, string nom,
                            float r, float g, float b, int prix, bool defaut)
    {
        return new Skin { id = id, nom = nom, couleur = new Color(r, g, b),
                          prix = prix, defaut = defaut };
    }

    // ── État sauvegardé ──────────────────────────────────────────────
    private static bool charge;
    private static int pieces;
    private static string equipe = "defaut";
    private static readonly List<string> possedes = new List<string> { "defaut" };

    // Clés PlayerPrefs (préfixe "PP_Shop_")
    private const string CLE_PIECES  = "PP_Shop_Pieces";
    private const string CLE_EQUIPE  = "PP_Shop_Equipe";
    private const string CLE_POSSEDE = "PP_Shop_Possedes";

    /// <summary>Preload à la première utilisation (menu, score, spawn…).</summary>
    static ShopManager()
    {
        Charger();
    }

    private static void Charger()
    {
        if (charge) return;
        charge = true;

        pieces = PlayerPrefs.GetInt(CLE_PIECES, 0);
        equipe = PlayerPrefs.GetString(CLE_EQUIPE, "defaut");
        possedes.Clear();
        possedes.Add("defaut");      // le skin de départ est TOUJOURS possédé

        string liste = PlayerPrefs.GetString(CLE_POSSEDE, "");
        if (!string.IsNullOrEmpty(liste))
        {
            foreach (string id in liste.Split(';'))
            {
                if (!string.IsNullOrEmpty(id) && EstUnSkinValide(id)) possedes.Add(id);
            }
        }

        // DEBLOCAGE COMPLET (salim 03/10 : « debloque les tous que je puisse
        // les utiliser ») : tous les skins sont possédés d'office, gratuit.
        // POUR REMETTRE LE SHOP Avec LES PRIX plus tard : supprimer ce foreach.
        foreach (Skin s in Skins)
        {
            if (!possedes.Contains(s.id)) possedes.Add(s.id);
        }
        Debug.Log("[SHOP] Chargé : " + pieces + " pièces, " + possedes.Count +
                  " skin(s) possédé(s), équipé : " + equipe);
    }

    private static void Sauver()
    {
        PlayerPrefs.SetInt(CLE_PIECES, pieces);
        PlayerPrefs.SetString(CLE_EQUIPE, equipe);
        PlayerPrefs.SetString(CLE_POSSEDE, string.Join(";", possedes.ToArray()));
        PlayerPrefs.Save();
    }

    public static bool EstUnSkinValide(string id)
    {
        return Get(id) != null;
    }

    // ── Questions / actions ──────────────────────────────────────────
    public static int Pieces
    {
        get { Charger(); return pieces; }
    }

    /// <summary>Cherche un skin par son id (null si inconnu).</summary>
    public static Skin Get(string id)
    {
        foreach (Skin s in Skins)
        {
            if (s.id == id) return s;
        }
        return null;
    }

    /// <summary>Le skin PORTÉ par le joueur (jamais null : retombe sur Bleu).</summary>
    public static Skin SkinEquipee()
    {
        Charger();
        Skin s = Get(equipe);
        return s != null ? s : Skins[0];
    }

    public static bool EstPossede(string id)
    {
        Charger();
        // Le skin de départ est gratuit et possédé d'office.
        if (id == "defaut") return true;
        return possedes.Contains(id);
    }

    /// <summary>Un skin possédé mais non équipé → un clic l'équipe.</summary>
    public static bool PeutEquiper(string id)
    {
        return EstPossede(id) && !EstEquipe(id);
    }

    public static bool EstEquipe(string id)
    {
        Charger();
        return equipe == id;
    }

    /// <summary>Credite les pièces gagnées en éclatant des bulles (joueur).</summary>
    public static void AjouterPieces(int n)
    {
        if (n <= 0) return;
        Charger();  // s'assure que le solde en mémoire est à jour
        pieces += n;
        // Sauvegarde immédiate : même si le téléphone coupe,rien n'est perdu.
        PlayerPrefs.SetInt(CLE_PIECES, pieces);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Achète un skin. Renvoie TRUE si l'achat a marché (la pièce est débitée
    /// ET le skin devient équipé automatiquement — le joueur veut le voir !).
    /// FALSE = déjà possédé (rien à faire) ou pas assez de pièces.
    /// </summary>
    public static bool Acheter(string id)
    {
        Charger();
        Skin s = Get(id);
        if (s == null) return false;
        if (EstPossede(id)) return false;          // déjà acheté
        if (pieces < s.prix) return false;         // pas assez de pièces

        pieces -= s.prix;
        possedes.Add(id);
        equipe = id;                               // équipé direct = satisfaction
        Sauver();
        Debug.Log("[SHOP] Skin acheté : " + s.nom + " (" + s.prix + " pièces), reste " + pieces);
        return true;
    }

    /// <summary>Change le skin porté (seulement si on le possède).</summary>
    public static void Equiper(string id)
    {
        if (EstPossede(id) == false) return;
        Charger();
        equipe = id;
        Sauver();
    }
}