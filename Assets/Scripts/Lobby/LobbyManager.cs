using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkObject))]
public class LobbyManager : NetworkBehaviour
{
    [Header("Lobby UI")]
    [SerializeField] private TMP_Text playersListText;
    [SerializeField] private Button startGameButton;

    [Header("Lobby Settings")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 4;

    // Liste synchronisée des joueurs.
    private NetworkList<ulong> players;

    private void Awake()
    {
        players = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        // Réagir aux modifications de la liste.
        players.OnListChanged += OnPlayersChanged;

        if (IsServer)
        {
            // Écouter les connexions et déconnexions.
            NetworkManager.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.OnClientDisconnectCallback +=
                OnClientDisconnected;

            // Ajouter les joueurs déjà connectés.
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                AddPlayer(clientId);
            }
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }

        RefreshUI();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        AddPlayer(clientId);
    }

    private void AddPlayer(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == clientId)
                return;
        }

        players.Add(clientId);

        Debug.Log($"Joueur {clientId} rejoint le Lobby.");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i] == clientId)
            {
                players.RemoveAt(i);
            }
        }
    }

    private void OnPlayersChanged(
        NetworkListEvent<ulong> changeEvent)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (playersListText != null)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine(
                $"Joueurs connectés : {players.Count}/{maxPlayers}"
            );

            text.AppendLine();

            for (int i = 0; i < players.Count; i++)
            {
                ulong clientId = players[i];

                text.Append($"Joueur {clientId + 1}");

                if (clientId == NetworkManager.ServerClientId)
                    text.Append(" (Host)");

                if (clientId == NetworkManager.LocalClientId)
                    text.Append(" (vous)");

                text.AppendLine();
            }

            playersListText.text = text.ToString();
        }

        if (startGameButton != null)
        {
            // Seul le Host voit le bouton.
            startGameButton.gameObject.SetActive(IsHost);

            // Il faut entre 2 et 4 joueurs.
            startGameButton.interactable =
                IsHost &&
                players.Count >= minPlayers &&
                players.Count <= maxPlayers;
        }
    }

    public void StartGame()
    {
        // Sécurité : seul le Host peut démarrer.
        if (!IsHost) return;

        if (players.Count < minPlayers ||
            players.Count > maxPlayers)
        {
            Debug.LogWarning(
                "Nombre de joueurs incorrect."
            );
            return;
        }

        // Charger GameScene chez tous les joueurs.
        SceneEventProgressStatus result =
            NetworkManager.SceneManager.LoadScene(
                "GameScene",
                LoadSceneMode.Single
            );

        if (result != SceneEventProgressStatus.Started)
        {
            Debug.LogError(
                $"Impossible de lancer la partie : {result}"
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        players.OnListChanged -= OnPlayersChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -=
                OnClientConnected;

            NetworkManager.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }
    }

    private void OnDestroy()
    {
        players?.Dispose();
    }
}