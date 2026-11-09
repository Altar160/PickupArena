
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkManager))]
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;

    // Mémorise le SpawnPoint attribué à chaque joueur.
    private Dictionary<ulong, int> assignedSpawns = new();

    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();

        networkManager.OnClientConnectedCallback +=
            OnClientConnected;

        networkManager.OnClientDisconnectCallback +=
            OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (networkManager == null) return;

        networkManager.OnClientConnectedCallback -=
            OnClientConnected;

        networkManager.OnClientDisconnectCallback -=
            OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Seul le serveur choisit les positions.
        if (!networkManager.IsServer) return;

        // Chercher les SpawnPoints disponibles.
        List<int> availableSpawns = new();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            if (!assignedSpawns.ContainsValue(i))
            {
                availableSpawns.Add(i);
            }
        }

        // Vérifier qu'il reste une place.
        if (availableSpawns.Count == 0)
        {
            Debug.LogWarning("Aucun SpawnPoint disponible !");
            return;
        }

        // Choisir une position aléatoire.
        int randomIndex = Random.Range(
            0, availableSpawns.Count
        );

        int spawnIndex = availableSpawns[randomIndex];

        Transform spawnPoint = spawnPoints[spawnIndex];

        // Récupérer le personnage du joueur.
        NetworkObject player =
            networkManager.SpawnManager
                .GetPlayerNetworkObject(clientId);

        if (player == null) return;

        // Désactiver temporairement les collisions.
        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        // Positionner le personnage.
        player.transform.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (controller != null)
            controller.enabled = true;

        // Réserver ce SpawnPoint pour ce joueur.
        assignedSpawns[clientId] = spawnIndex;

        Debug.Log(
            $"Joueur {clientId} apparu au SpawnPoint {spawnIndex + 1}"
        );
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!networkManager.IsServer) return;

        // Libérer le SpawnPoint du joueur déconnecté.
        assignedSpawns.Remove(clientId);
    }
}
