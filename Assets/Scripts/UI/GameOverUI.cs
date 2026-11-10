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

        // Seul le Host voit les boutons.
        bool showButtons = IsHost && isGameOver;

        if (replayButton != null)
            replayButton.gameObject.SetActive(showButtons);

        if (returnLobbyButton != null)
            returnLobbyButton.gameObject.SetActive(showButtons);

        if (returnMenuButton != null)
            returnMenuButton.gameObject.SetActive(showButtons);
    }

    private bool CanUseButtons()
    {
        return IsSpawned &&
               IsHost &&
               !actionInProgress &&
               gameManager.State.Value == GamePhase.GameOver;
    }

    // REJOUER : nouvelle manche dans la même arène.
    public void Replay()
    {
        if (!CanUseButtons())
            return;

        LoadNetworkScene("GameScene");
    }

    // RETOUR LOBBY : garder les joueurs connectés.
    public void ReturnToLobby()
    {
        if (!CanUseButtons())
            return;

        LoadNetworkScene("Lobby");
    }

    // Charger une scène chez tous les joueurs.
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
                $"Erreur de chargement de {sceneName} : {result}"
            );
        }
    }

    // RETOUR MENU : fermer la session.
    public void ReturnToMenu()
    {
        if (!CanUseButtons())
            return;

        actionInProgress = true;

        // Déconnecter tous les joueurs.
        // Un autre script gérera le retour au menu local.
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
    }
}