
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private NetworkObject playerPrefab;

    private NetworkManager networkManager;

    private readonly Dictionary<ulong, int> assignedSpawns = new();

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        networkManager.ConnectionApprovalCallback += ApproveConnection;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        List<int> availableSpawns = new();

        if (playerPrefab != null && spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                    continue;

                if (!assignedSpawns.ContainsValue(i))
                    availableSpawns.Add(i);
            }
        }

        if (availableSpawns.Count == 0)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "Partie complète ou configuration manquante.";
            response.Pending = false;
            return;
        }

        int randomIndex = Random.Range(0, availableSpawns.Count);
        int spawnIndex = availableSpawns[randomIndex];

        assignedSpawns[request.ClientNetworkId] = spawnIndex;

        response.Approved = true;

        // On empêche la création automatique du joueur.
        response.CreatePlayerObject = false;

        response.Pending = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!networkManager.IsServer)
            return;

        if (!assignedSpawns.TryGetValue(clientId, out int spawnIndex))
            return;

        Transform spawnPoint = spawnPoints[spawnIndex];

        // Créer la capsule directement au SpawnPoint.
        NetworkObject player = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Associer cette capsule au joueur connecté.
        player.SpawnAsPlayerObject(clientId, true);

        Debug.Log(
            $"Joueur {clientId} créé au SpawnPoint {spawnIndex + 1} " +
            $"à la position {player.transform.position}"
        );
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!networkManager.IsServer)
            return;

        assignedSpawns.Remove(clientId);
    }

    private void OnDestroy()
    {
        if (networkManager == null)
            return;

        networkManager.ConnectionApprovalCallback -= ApproveConnection;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}
