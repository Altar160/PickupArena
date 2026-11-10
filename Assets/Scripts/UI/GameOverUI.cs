
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(GameManager))]
public class GameOverUI : NetworkBehaviour
{
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Host Buttons")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button returnLobbyButton;
    [SerializeField] private Button returnMenuButton;

    [Header("Client Buttons")]
    [SerializeField] private Button clientLeaveButton;

    [Header("Match Settings")]
    [SerializeField, Min(2)] private int minPlayers = 2;

    private GameManager gameManager;
    private bool actionInProgress;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    public override void OnNetworkSpawn()
    {
        gameManager.State.OnValueChanged += OnStateChanged;

        NetworkManager.OnClientDisconnectCallback +=
            OnClientDisconnected;

        if (replayButton != null)
            replayButton.onClick.AddListener(Replay);

        if (returnLobbyButton != null)
            returnLobbyButton.onClick.AddListener(ReturnToLobby);

        if (returnMenuButton != null)
            returnMenuButton.onClick.AddListener(ReturnToMenu);

        if (clientLeaveButton != null)
            clientLeaveButton.onClick.AddListener(LeaveGame);

        actionInProgress = false;

        RefreshUI(gameManager.State.Value);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsSpawned || !IsHost)
            return;

        // Actualiser le bouton lorsqu'un joueur quitte.
        RefreshUI(gameManager.State.Value);
    }

    private void OnStateChanged(
        GamePhase previous,
        GamePhase current)
    {
        RefreshUI(current);
    }

    private bool HasEnoughPlayers()
    {
        return NetworkManager != null &&
               NetworkManager.IsServer &&
               NetworkManager.ConnectedClientsIds.Count >= minPlayers;
    }

    private void RefreshUI(GamePhase phase)
    {
        bool isGameOver = phase == GamePhase.GameOver;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(isGameOver);

        bool showHostButtons = isGameOver && IsHost;

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(showHostButtons);

            // Désactiver REJOUER si le Host est seul.
            replayButton.interactable =
                showHostButtons &&
                HasEnoughPlayers() &&
                !actionInProgress;
        }

        if (returnLobbyButton != null)
            returnLobbyButton.gameObject.SetActive(showHostButtons);

        if (returnMenuButton != null)
            returnMenuButton.gameObject.SetActive(showHostButtons);

        bool showClientButton =
            isGameOver && IsClient && !IsHost;

        if (clientLeaveButton != null)
            clientLeaveButton.gameObject.SetActive(showClientButton);
    }

    private bool CanUseHostButtons()
    {
        return IsSpawned &&
               IsHost &&
               !actionInProgress &&
               gameManager.State.Value == GamePhase.GameOver;
    }

    public void Replay()
    {
        if (!CanUseHostButtons())
            return;

        // Vérifier aussi côté logique, pas seulement sur l'UI.
        if (!HasEnoughPlayers())
        {
            Debug.LogWarning(
                "Impossible de rejouer : minimum 2 joueurs."
            );

            RefreshUI(gameManager.State.Value);
            return;
        }

        LoadNetworkScene("GameScene");
    }

    public void ReturnToLobby()
    {
        if (!CanUseHostButtons())
            return;

        LoadNetworkScene("Lobby");
    }

    private void LoadNetworkScene(string sceneName)
    {
        SceneEventProgressStatus result =
            NetworkManager.SceneManager.LoadScene(
                sceneName,
                LoadSceneMode.Single
            );

        if (result == SceneEventProgressStatus.Started)
        {
            actionInProgress = true;
            Debug.Log($"Chargement de {sceneName}");
        }
        else
        {
            Debug.LogError($"Erreur de chargement : {result}");
        }
    }

    public void ReturnToMenu()
    {
        if (!CanUseHostButtons())
            return;

        actionInProgress = true;
        NetworkManager.Shutdown();
    }

    public void LeaveGame()
    {
        if (!IsSpawned ||
            !IsClient ||
            IsHost ||
            actionInProgress ||
            gameManager.State.Value != GamePhase.GameOver)
            return;

        actionInProgress = true;
        NetworkManager.Shutdown();
    }

    public override void OnNetworkDespawn()
    {
        gameManager.State.OnValueChanged -= OnStateChanged;

        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        if (replayButton != null)
            replayButton.onClick.RemoveListener(Replay);

        if (returnLobbyButton != null)
            returnLobbyButton.onClick.RemoveListener(ReturnToLobby);

        if (returnMenuButton != null)
            returnMenuButton.onClick.RemoveListener(ReturnToMenu);

        if (clientLeaveButton != null)
            clientLeaveButton.onClick.RemoveListener(LeaveGame);
    }
}
