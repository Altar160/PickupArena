using UnityEngine;
using Unity.Netcode;

public enum GamePhase
{
    Lobby,
    Countdown,
    Playing,
    GameOver
}

[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int countdownDuration = 3;
    [SerializeField] private int matchDuration = 90;

    public static GameManager Instance { get; private set; }

    // Etat du jeu synchronisé.
    public readonly NetworkVariable<GamePhase> State =
        new NetworkVariable<GamePhase>(
            GamePhase.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // Chronomètre synchronisé.
    public readonly NetworkVariable<int> RemainingSeconds =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private float phaseEndTime;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += OnStateChanged;
        RemainingSeconds.OnValueChanged += OnTimerChanged;

        Debug.Log($"Etat actuel : {State.Value}");
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (State.Value != GamePhase.Countdown &&
            State.Value != GamePhase.Playing)
            return;

        // Calculer le temps restant.
        float remaining = phaseEndTime - Time.time;

        int seconds = Mathf.Max(
            0,
            Mathf.CeilToInt(remaining)
        );

        if (RemainingSeconds.Value != seconds)
            RemainingSeconds.Value = seconds;

        if (remaining > 0f)
            return;

        // Passer automatiquement à l'état suivant.
        if (State.Value == GamePhase.Countdown)
        {
            StartPlaying();
        }
        else if (State.Value == GamePhase.Playing)
        {
            EndGame();
        }
    }

    // Pour lancer temporairement depuis l'Inspector.
    [ContextMenu("Start Match (Host Only)")]
    public void StartMatch()
    {
        if (!Application.isPlaying || !IsSpawned || !IsServer)
            return;

        if (State.Value != GamePhase.Lobby)
            return;

        State.Value = GamePhase.Countdown;
        RemainingSeconds.Value = countdownDuration;

        phaseEndTime = Time.time + countdownDuration;

        Debug.Log("Début du compte à rebours !");
    }

    private void StartPlaying()
    {
        State.Value = GamePhase.Playing;
        RemainingSeconds.Value = matchDuration;

        phaseEndTime = Time.time + matchDuration;

        Debug.Log("LA PARTIE COMMENCE !");
    }

    private void EndGame()
    {
        State.Value = GamePhase.GameOver;
        RemainingSeconds.Value = 0;

        Debug.Log("FIN DE LA PARTIE !");
    }

    private void OnStateChanged(
        GamePhase previous,
        GamePhase current)
    {
        Debug.Log($"Etat : {previous} -> {current}");
    }

    private void OnTimerChanged(
        int previous,
        int current)
    {
        Debug.Log($"Temps restant : {current}");
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= OnStateChanged;
        RemainingSeconds.OnValueChanged -= OnTimerChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}