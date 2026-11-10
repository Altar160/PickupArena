using System;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

// Informations synchronisées d'un joueur du Lobby.
public struct LobbyPlayerData :
    INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes Nickname;

    public void NetworkSerialize<T>(
        BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Nickname);
    }

    public bool Equals(LobbyPlayerData other)
    {
        return ClientId == other.ClientId &&
               Nickname.Equals(other.Nickname);
    }
}

[RequireComponent(typeof(NetworkObject))]
public class LobbyManager : NetworkBehaviour
{
    [Header("Lobby UI")]
    [SerializeField] private TMP_Text playersListText;
    [SerializeField] private Button startGameButton;

    [Header("Lobby Settings")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 4;

    private NetworkList<LobbyPlayerData> players;

    private void Awake()
    {
        players = new NetworkList<LobbyPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        players.OnListChanged += OnPlayersChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.OnClientDisconnectCallback +=
                OnClientDisconnected;

            // Récupérer les joueurs déjà connectés.
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                AddPlayer(clientId);
            }
        }

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        RefreshUI();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
            AddPlayer(clientId);
    }

    private void AddPlayer(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
                return;
        }

        SessionConnectionApproval approval =
            NetworkManager.GetComponent<SessionConnectionApproval>();

        string nickname = approval != null
            ? approval.GetPlayerName(clientId)
            : $"Joueur {clientId + 1}";

        players.Add(new LobbyPlayerData
        {
            ClientId = clientId,
            Nickname = new FixedString64Bytes(nickname)
        });

        Debug.Log($"{nickname} rejoint le Lobby.");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].ClientId == clientId)
                players.RemoveAt(i);
        }
    }

    private void OnPlayersChanged(
        NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (playersListText != null)
        {
            StringBuilder display = new StringBuilder();

            display.AppendLine(
                $"Joueurs connectés : {players.Count}/{maxPlayers}"
            );

            display.AppendLine();

            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayerData player = players[i];

                display.Append(player.Nickname.ToString());

                if (player.ClientId == NetworkManager.ServerClientId)
                    display.Append(" (Host)");

                if (player.ClientId == NetworkManager.LocalClientId)
                    display.Append(" (vous)");

                display.AppendLine();
            }

            playersListText.text = display.ToString();
        }

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(IsHost);

            startGameButton.interactable =
                IsHost &&
                players.Count >= minPlayers &&
                players.Count <= maxPlayers;
        }
    }

    public void StartGame()
    {
        if (!IsHost)
            return;

        if (players.Count < minPlayers ||
            players.Count > maxPlayers)
            return;

        SceneEventProgressStatus result =
            NetworkManager.SceneManager.LoadScene(
                "GameScene",
                LoadSceneMode.Single
            );

        if (result != SceneEventProgressStatus.Started)
        {
            Debug.LogError(
                $"Impossible de charger GameScene : {result}"
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
            startGameButton.onClick.RemoveListener(StartGame);
    }

    private void OnDestroy()
    {
        players?.Dispose();
    }
}