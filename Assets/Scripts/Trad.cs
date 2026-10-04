using UnityEngine;

/// <summary>
/// Langue du jeu FR / EN (salim 04/10 : « rajoute langue anglais »).
///   - AU PREMIER LANCEMENT : on suit la langue du téléphone
///     (salim : « si telephone langue francais le jeux en francais et si
///     anglais beh langue anglaise dans le jeux »).
///   - ENSUITE : le choix fait dans REGLAGES est enregistré sur l'appareil
///     (PlayerPrefs) et gagne TOUJOURS sur la langue du téléphone.
/// Utilisation : Trad.T("JOUER", "PLAY") partout où un texte s'écrit.
/// </summary>
public static class Trad
{
    private const string CLE = "LangueJeu";   // PlayerPrefs : 0 = FR, 1 = EN, -1 = jamais changé

    /// <summary>Vrai = interface en ANGLAIS.</summary>
    public static bool Anglais { get; private set; }

    static Trad()
    {
        int sauve = PlayerPrefs.GetInt(CLE, -1);
        if (sauve == 1)      Anglais = true;    // choix du réglage : anglais
        else if (sauve == 0) Anglais = false;   // choix du réglage : français
        else Anglais = Application.systemLanguage != SystemLanguage.French;
        Debug.Log("[TRAD] Langue jeu : " + (Anglais ? "EN" : "FR"));
    }

    /// <summary>Change la langue pour de bon (panneau réglages) + sauvegarde.</summary>
    public static void Choisir(bool anglais)
    {
        Anglais = anglais;
        PlayerPrefs.SetInt(CLE, anglais ? 1 : 0);
    }

    /// <summary>Renvoie le texte dans la langue du jeu.</summary>
    public static string T(string fr, string en)
    {
        return Anglais ? en : fr;
    }
}