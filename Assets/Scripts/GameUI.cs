using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interface utilisateur du jeu : chrono, scores des 4 slimes, panneau de
/// fin de round, écran final avec gagnant + bouton Rejouer.
/// Toutes les références UI sont glissées depuis l'éditeur (voir guide).
/// TMP_Text marche pour TextMeshPro ET Text Legacy (fallback via TMPro).
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("Chrono")]
    [Tooltip("Texte affichant le temps restant du round")]
    public TMP_Text timerText;

    [Header("Scores live (4 textes, un par slime)")]
    public TMP_Text[] scoreTexts = new TMP_Text[4];

    [Header("Panneau fin de round (rappels des scores)")]
    public GameObject roundEndPanel;
    public TMP_Text roundEndTitleText;
    public TMP_Text roundEndScoresText;

    [Header("Panneau final : grand gagnant")]
    public GameObject winnerPanel;
    public TMP_Text winnerTitleText;
    public TMP_Text winnerScoreText;
    public Button replayButton;   // bouton "Rejouer"

    [Header("Couleurs des slimes (pour colorier les scores)")]
    public Color[] slimeColors = new Color[4]
    {
        new Color(0.2f, 0.6f, 1f),   // Joueur : bleu
        new Color(1f, 0.3f, 0.3f),   // Bot 1 : rouge
        new Color(0.3f, 1f, 0.4f),   // Bot 2 : vert
        new Color(1f, 0.8f, 0.2f)    // Bot 3 : jaune
    };

    private void Start()
    {
        // Cache les panneaux au démarrage
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(false);

        // Bouton Rejouer -> GameManager.OnReplayButton (voir guide : drag & drop)
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() =>
                GameManager.Instance?.OnReplayButton());
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  CALLBACKS REÇUS DE GAME MANAGER
    // ────────────────────────────────────────────────────────────────
    public void OnRoundStart(int round, int duration)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(false);

        if (roundEndTitleText != null)
            roundEndTitleText.text = $"ROUND {round}";
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText == null) return;

        int seconds = Mathf.Max(0, (int)Mathf.Ceil(timeLeft));
        timerText.text = seconds.ToString();

        // Urgence : les 10 dernières secondes en rouge
        timerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    public void UpdateScores(int[] scores, int currentRound)
    {
        for (int i = 0; i < scoreTexts.Length; i++)
        {
            if (scoreTexts[i] == null) continue;

            scoreTexts[i].text = scores[i].ToString();
            scoreTexts[i].color = slimeColors[i]; // couleur identitaire du slime
        }
    }

    /// <summary>Panneau affiché pendant la pause de 5s entre les rounds.</summary>
    public void ShowRoundEndPanel(int finishedRound, int[] scores)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(true);

        if (roundEndTitleText != null)
            roundEndTitleText.text = $"FIN DU ROUND {finishedRound}";

        if (roundEndScoresText != null)
        {
            // Construit le mini classement
            string lines = "";
            for (int i = 0; i < scores.Length; i++)
            {
                string name = i == 0 ? "Toi" : $"Bot {i}";
                lines += $"{name} : {scores[i]} pts\n";
            }
            roundEndScoresText.text = lines;
        }
    }

    /// <summary>Écran final : gagnant + score + bouton Rejouer visible.</summary>
    public void ShowWinner(string winnerName, int winnerScore)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (winnerPanel != null) winnerPanel.SetActive(true);

        if (winnerTitleText != null)
            winnerTitleText.text = $"{winnerName} GAGNE !";

        if (winnerScoreText != null)
            winnerScoreText.text = $"{winnerScore} points";
    }
}