using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class SessionConnectionApproval : MonoBehaviour
{
    private NetworkManager networkManager;

    // Pseudos conservés par le serveur pendant la session.
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
        ulong clientId = request.ClientNetworkId;

        string nickname = "";

        // Récupérer le pseudo envoyé par le client.
        if (request.Payload != null &&
            request.Payload.Length > 0)
        {
            nickname = Encoding.UTF8.GetString(
                request.Payload
            );
        }

        // Nettoyer et sauvegarder le pseudo.
        nickname = CleanNickname(
            nickname,
            $"Joueur {clientId + 1}"
        );

        playerNames[clientId] = nickname;

        // Accepter la connexion sans créer de capsule.
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        Debug.Log(
            $"{nickname} connecté (ID : {clientId})"
        );
    }

    // Enregistrer explicitement le pseudo du Host.
    public void SetHostName(string nickname)
    {
        playerNames[NetworkManager.ServerClientId] =
            CleanNickname(nickname, "Host");
    }

    // Permettre aux autres scripts de récupérer un pseudo.
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

        return string.IsNullOrWhiteSpace(nickname)
            ? fallback
            : nickname;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager.IsServer)
            playerNames.Remove(clientId);
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