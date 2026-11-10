using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class SessionConnectionApproval : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        networkManager.ConnectionApprovalCallback +=
            ApproveConnection;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // Autoriser la connexion.
        response.Approved = true;

        // Ne pas créer de capsule dans le Lobby.
        response.CreatePlayerObject = false;

        response.Pending = false;

        Debug.Log(
            $"Connexion autorisée : joueur {request.ClientNetworkId}"
        );
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ConnectionApprovalCallback -=
                ApproveConnection;
        }
    }
}