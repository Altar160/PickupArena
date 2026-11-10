using UnityEngine;
using Unity.Netcode;
using TMPro;

[RequireComponent(typeof(GameManager))]
public class GameHUD : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    public override void OnNetworkSpawn()
    {
        // Ecouter les changements de phase.
        gameManager.State.OnValueChanged += OnPhaseChanged;

        // Ecouter les changements du chronomètre.
        gameManager.RemainingSeconds.OnValueChanged
            += OnTimerChanged;

        // Afficher les valeurs déjà synchronisées.
        RefreshPhase(gameManager.State.Value);
        RefreshTimer(gameManager.RemainingSeconds.Value);
    }

    private void OnPhaseChanged(
        GamePhase previous,
        GamePhase current)
    {
        RefreshPhase(current);
        RefreshTimer(gameManager.RemainingSeconds.Value);
    }

    private void OnTimerChanged(int previous, int current)
    {
        RefreshTimer(current);
    }

    private void RefreshPhase(GamePhase phase)
    {
        if (phaseText == null) return;

        switch (phase)
        {
            case GamePhase.Lobby:
                phaseText.text = "EN ATTENTE";
                break;

            case GamePhase.Countdown:
                phaseText.text = "PRÉPAREZ-VOUS !";
                break;

            case GamePhase.Playing:
                phaseText.text = "PARTIE EN COURS";
                break;

            case GamePhase.GameOver:
                phaseText.text = "FIN DE PARTIE !";
                break;
        }
    }

    private void RefreshTimer(int seconds)
    {
        if (timerText == null) return;

        GamePhase phase = gameManager.State.Value;

        if (phase == GamePhase.Countdown)
        {
            // Afficher 3, 2, 1.
            timerText.text = seconds.ToString();
        }
        else if (phase == GamePhase.Playing)
        {
            // Afficher le temps en minutes:secondes.
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;

            timerText.text =
                $"{minutes:00}:{remainingSeconds:00}";
        }
        else if (phase == GamePhase.GameOver)
        {
            timerText.text = "00:00";
        }
        else
        {
            timerText.text = "--:--";
        }
    }

    public override void OnNetworkDespawn()
    {
        gameManager.State.OnValueChanged -= OnPhaseChanged;

        gameManager.RemainingSeconds.OnValueChanged
            -= OnTimerChanged;
    }
}