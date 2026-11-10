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
    [Header("Game Configuration")]
    [SerializeField]
    private PickupArenaSettings settings;

    public static GameManager Instance { get; private set; }

    // État du jeu synchronisé.
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
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (State.Value != GamePhase.Countdown &&
            State.Value != GamePhase.Playing)
            return;

        float remaining = phaseEndTime - Time.time;

        int seconds = Mathf.Max(
            0,
            Mathf.CeilToInt(remaining)
        );

        if (RemainingSeconds.Value != seconds)
            RemainingSeconds.Value = seconds;

        if (remaining > 0f)
            return;

        if (State.Value == GamePhase.Countdown)
            StartPlaying();
        else if (State.Value == GamePhase.Playing)
            EndGame();
    }

    [ContextMenu("Start Match (Host Only)")]
    public void StartMatch()
    {
        if (!Application.isPlaying ||
            !IsSpawned ||
            !IsServer ||
            State.Value != GamePhase.Lobby)
            return;

        if (settings == null)
        {
            Debug.LogError("GameSettings non assigné !");
            return;
        }

        int duration = Mathf.Max(
            1, settings.countdownDuration
        );

        phaseEndTime = Time.time + duration;
        RemainingSeconds.Value = duration;
        State.Value = GamePhase.Countdown;

        Debug.Log("Début du Countdown !");
    }

    private void StartPlaying()
    {
        int duration = Mathf.Max(
            1, settings.matchDuration
        );

        phaseEndTime = Time.time + duration;
        RemainingSeconds.Value = duration;
        State.Value = GamePhase.Playing;

        Debug.Log("Début de la partie !");
    }

    private void EndGame()
    {
        RemainingSeconds.Value = 0;
        State.Value = GamePhase.GameOver;

        Debug.Log("Fin de la partie !");
    }

    private void OnStateChanged(
        GamePhase previous,
        GamePhase current)
    {
        Debug.Log($"État : {previous} -> {current}");
    }

    private void OnTimerChanged(int previous, int current)
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