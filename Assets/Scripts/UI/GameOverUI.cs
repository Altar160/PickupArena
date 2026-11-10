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

    private GameManager gameManager;
    private bool actionInProgress;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    public override void OnNetworkSpawn()
    {
        gameManager.State.OnValueChanged += OnStateChanged;

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

    private void OnStateChanged(
        GamePhase previous,
        GamePhase current)
    {
        RefreshUI(current);
    }

    private void RefreshUI(GamePhase phase)
    {
        bool isGameOver = phase == GamePhase.GameOver;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(isGameOver);

        // Boutons réservés au Host.
        bool showHostButtons = isGameOver && IsHost;

        if (replayButton != null)
            replayButton.gameObject.SetActive(showHostButtons);

        if (returnLobbyButton != null)
            returnLobbyButton.gameObject.SetActive(showHostButtons);

        if (returnMenuButton != null)
            returnMenuButton.gameObject.SetActive(showHostButtons);

        // Bouton réservé aux Clients.
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

    // REJOUER : recharger l'arène.
    public void Replay()
    {
        if (!CanUseHostButtons())
            return;

        LoadNetworkScene("GameScene");
    }

    // RETOUR LOBBY : conserver la session.
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
            Debug.LogError(
                $"Erreur de chargement : {result}"
            );
        }
    }

    // Le Host ferme la session pour tout le monde.
    public void ReturnToMenu()
    {
        if (!CanUseHostButtons())
            return;

        actionInProgress = true;

        Debug.Log("Le Host ferme la session.");

        NetworkManager.Shutdown();
    }

    // NOUVEAU : un Client quitte seul.
    public void LeaveGame()
    {
        if (!IsSpawned ||
            !IsClient ||
            IsHost ||
            actionInProgress ||
            gameManager.State.Value != GamePhase.GameOver)
        {
            return;
        }

        actionInProgress = true;

        Debug.Log("Le Client quitte la partie.");

        // Déconnecter uniquement ce Client.
        NetworkManager.Shutdown();
    }

    public override void OnNetworkDespawn()
    {
        gameManager.State.OnValueChanged -= OnStateChanged;

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