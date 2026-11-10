using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class SessionConnectionApproval : MonoBehaviour
{
    [Header("Session Settings")]
    [SerializeField, Range(2, 4)]
    private int maxPlayers = 4;

    private NetworkManager networkManager;

    // Pseudos enregistrés côté serveur.
    private readonly Dictionary<ulong, string> playerNames = new();

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        networkManager.ConnectionApprovalCallback +=
            ApproveConnection;

        networkManager.OnClientDisconnectCallback +=
            OnClientDisconnected;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Vérifier si une partie est déjà en cours.
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != "MainMenu" &&
            currentScene != "Lobby")
        {
            RejectConnection(
                response,
                "La partie a déjà commencé."
            );
            return;
        }

        // Refuser les connexions si le Lobby est plein.
        if (networkManager.ConnectedClientsIds.Count >= maxPlayers)
        {
            RejectConnection(
                response,
                "Le Lobby est complet."
            );
            return;
        }

        // Limiter la taille des données reçues.
        if (request.Payload != null &&
            request.Payload.Length > 128)
        {
            RejectConnection(
                response,
                "Données de connexion invalides."
            );
            return;
        }

        ulong clientId = request.ClientNetworkId;

        // Récupérer le pseudo envoyé.
        string nickname = "";

        if (request.Payload != null &&
            request.Payload.Length > 0)
        {
            nickname = Encoding.UTF8.GetString(
                request.Payload
            );
        }

        nickname = CleanNickname(
            nickname,
            $"Joueur {clientId + 1}"
        );

        playerNames[clientId] = nickname;

        // Accepter sans créer de capsule dans le Lobby.
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        Debug.Log($"{nickname} a rejoint la session.");
    }

    private void RejectConnection(
        NetworkManager.ConnectionApprovalResponse response,
        string reason)
    {
        response.Approved = false;
        response.CreatePlayerObject = false;
        response.Reason = reason;
        response.Pending = false;

        Debug.LogWarning($"Connexion refusée : {reason}");
    }

    public void SetHostName(string nickname)
    {
        playerNames[NetworkManager.ServerClientId] =
            CleanNickname(nickname, "Host");
    }

    public string GetPlayerName(ulong clientId)
    {
        if (playerNames.TryGetValue(clientId, out string name))
            return name;

        return $"Joueur {clientId + 1}";
    }

    public static string CleanNickname(
        string nickname,
        string fallback = "Joueur")
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return fallback;

        nickname = nickname.Trim()
            .Replace("\n", "")
            .Replace("\r", "");

        if (nickname.Length > 16)
            nickname = nickname.Substring(0, 16);

        // Respecter la capacité du FixedString64Bytes.
        while (Encoding.UTF8.GetByteCount(nickname) > 60)
        {
            nickname = nickname.Substring(
                0,
                nickname.Length - 1
            );
        }

        return string.IsNullOrWhiteSpace(nickname)
            ? fallback
            : nickname;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!networkManager.IsServer)
            return;

        playerNames.Remove(clientId);

        Debug.Log($"Joueur {clientId} déconnecté.");
    }

    private void OnDestroy()
    {
        if (networkManager == null)
            return;

        networkManager.ConnectionApprovalCallback -=
            ApproveConnection;

        networkManager.OnClientDisconnectCallback -=
            OnClientDisconnected;
    }
}