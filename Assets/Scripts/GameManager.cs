using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cerveau du jeu : chrono 60s, 3 rounds, scores des 4 slimes, pause entre
/// les rounds (Time.timeScale = 0), écran final avec gagnant + bouton Rejouer.
/// Instance est publique car SlimeController, Bubble et GameUI la lisent.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { WaitingToStart, Playing, RoundEndPause, GameOver }

    [Header("Références scènes / préfabs")]
    [Tooltip("Préfab du slime du JOUEUR (tag Slime)")]
    public GameObject playerSlimePrefab;
    [Tooltip("Préfab du slime BOT (les 3 IA, peut être le même préfab)")]
    public GameObject botSlimePrefab;
    [Tooltip("Caméra de la scène (script CameraFollow dessus)")]
    public CameraFollow gameCamera;
    [Tooltip("Script BubbleSpawner de la scène")]
    public BubbleSpawner spawner;
    [Tooltip("UI du jeu (script GameUI dessus)")]
    public GameUI gameUI;
    [Tooltip("Joystick FixedJoystick du Joystick Pack (typé MonoBehaviour pour compiler sans le pack)")]
    public MonoBehaviour playerJoystick;

    [Header("Règles du jeu")]
    public int roundDuration = 60;   // secondes par round
    public int totalRounds = 3;      // nombre de rounds
    [Tooltip("Positions de départ des 4 slimes (assignées dans l'éditeur)")]
    public Transform[] startPositions = new Transform[4];

    // --- État du jeu ---
    public GameState CurrentState { get; private set; } = GameState.WaitingToStart;
    public int CurrentRound { get; private set; } = 0;   // 1 à 3 pendant le jeu
    public float TimeLeft { get; private set; } = 0f;    // chrono affiché dans l'UI

    // Scores indexés par slimeIndex (0 = joueur, 1..3 = bots)
    private readonly int[] scores = new int[4];

    private SlimeController playerController;   // référence du slime joueur
    private readonly SlimeController[] allSlimes = new SlimeController[4];

    public MonoBehaviour PlayerJoystick => playerJoystick;

    private void Awake()
    {
        // Singleton standard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Sécurité : le framerate n'a pas besoin de dépasser les 60 fps
        // en jeu mobile, économie de batterie.
        Application.targetFrameRate = 60;

        // Le jeu n'attaque le round 1 QUE quand le joueur a appuyé sur
        // JOUER dans le menu de départ (StartMenu.StartRequested).
        // S'il n'y a aucun menu dans la scène, on démarre direct.
        StartCoroutine(WaitMenuThenStart());
    }

    private System.Collections.IEnumerator WaitMenuThenStart()
    {
        yield return new WaitUntil(() => StartMenu.StartRequested || StartMenu.Instance == null);

        SpawnSlimes();

        // Menu passé (ou absent) : on démarre le 1er round
        StartCoroutine(StartRoundFlow());
    }

    // ────────────────────────────────────────────────────────────────
    //  SPAWN DES 4 SLIMES
    // ────────────────────────────────────────────────────────────────
    private void SpawnSlimes()
    {
        for (int i = 0; i < 4; i++)
        {
            bool isPlayer = (i == 0);         // slime 0 = joueur
            GameObject prefab = isPlayer ? playerSlimePrefab : botSlimePrefab;
            Vector3 pos = startPositions[i] != null
                ? startPositions[i].position
                : Vector3.zero;

            GameObject slime = Instantiate(prefab, pos, Quaternion.identity);
            SlimeController sc = slime.GetComponent<SlimeController>();
            sc.isPlayer = isPlayer;
            sc.slimeIndex = i;

            // Le préfab IA arrive déjà correctement, le joueur aussi,
            // mais on s'assure du booléen par sécurité.
            allSlimes[i] = sc;
            if (isPlayer)
            {
                playerController = sc;
            }
        }

        // La caméra suit le joueur
        if (gameCamera != null && playerController != null)
        {
            gameCamera.SetTarget(playerController.transform);
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  FLOW POUR UN ROUND COMPLET
    // ────────────────────────────────────────────────────────────────
    private IEnumerator StartRoundFlow()
    {
        CurrentRound++;
        CurrentState = GameState.Playing;
        TimeLeft = roundDuration;

        // Reset des positions et de la vélocité des slimes
        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = startPositions[i] != null
                ? startPositions[i].position
                : Vector3.zero;
            allSlimes[i].ResetToSpawn(pos);
        }

        // Spawn des 50 nouvelles bulles pour ce round
        if (spawner != null) spawner.SpawnBubblesForRound();

        // Mise à jour de l'UI
        if (gameUI != null) gameUI.OnRoundStart(CurrentRound, roundDuration);

        // Reprise du temps si on revenait d'une pause
        Time.timeScale = 1f;
        yield break;
    }

    // ────────────────────────────────────────────────────────────────
    //  CHRONO
    // ────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        TimeLeft -= Time.deltaTime;
        if (gameUI != null) gameUI.UpdateTimer(TimeLeft);

        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            EndRound();
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  FINS DE ROUND / DE JEU
    // ────────────────────────────────────────────────────────────────
    private void EndRound()
    {
        CurrentState = GameState.RoundEndPause;
        Time.timeScale = 0f;              // PAUSE : tout se fige

        bool isLastRound = (CurrentRound >= totalRounds);

        if (gameUI != null)
        {
            if (isLastRound)
            {
                // Fin du round 3 : affichage du grand gagnant
                CurrentState = GameState.GameOver;
                int winnerIndex = GetWinnerIndex();
                int winnerScore = scores[winnerIndex];
                string winnerName = winnerIndex == 0
                    ? "TOI"
                    : $"Bot {winnerIndex}";

                gameUI.ShowWinner(winnerName, winnerScore);
            }
            else
            {
                // Fin du round 1 ou 2 : panneau de scores intermédiaire
                gameUI.ShowRoundEndPanel(CurrentRound, scores);
            }
        }

        if (!isLastRound)
        {
            // Attend 5 secondes (temps réel, PAS affecté par timeScale=0 !)
            StartCoroutine(NextRoundCountdown());
        }
    }

    /// <summary>Attente "temps réel" : fonctionne même avec Time.timeScale = 0.</summary>
    private IEnumerator NextRoundCountdown()
    {
        // 5 secondes réelles, hors chrono du jeu
        yield return new WaitForSecondsRealtime(5f);

        // StartRoundFlow s'occupe du reset des positions, du spawn des
        // nouvelles bulles et de Time.timeScale = 1f.
        StartCoroutine(StartRoundFlow()); // passe au round suivant
    }

    // ────────────────────────────────────────────────────────────────
    //  SCORES
    // ────────────────────────────────────────────────────────────────
    /// <summary>Appelé par Bubble.Pop : +1 point au slime qui a éclaté la bulle.</summary>
    public void AddScore(SlimeController slime, int points)
    {
        if (CurrentState != GameState.Playing) return; // pas de score hors round

        int idx = Mathf.Clamp(slime.slimeIndex, 0, scores.Length - 1);
        scores[idx] += points;

        if (gameUI != null) gameUI.UpdateScores(scores, CurrentRound);
    }

    /// <summary>Renvoie l'index du slime avec le plus de points (0..3).</summary>
    public int GetWinnerIndex()
    {
        int best = 0;
        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > scores[best]) best = i;
        }
        return best;
    }

    public int GetScore(int slimeIndex)
    {
        return scores[Mathf.Clamp(slimeIndex, 0, scores.Length - 1)];
    }

    /// <summary>Score courant de tous les slimes (utile pour l'UI live).</summary>
    public int[] GetScoresSnapshot()
    {
        return (int[])scores.Clone();
    }

    // ────────────────────────────────────────────────────────────────
    //  BOUTON "REJOUER" (connecté au bouton UI du panneau GameOver)
    // ────────────────────────────────────────────────────────────────
    public void OnReplayButton()
    {
        // Reload simple de la scène en cours : remet tout à zéro proprement.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}