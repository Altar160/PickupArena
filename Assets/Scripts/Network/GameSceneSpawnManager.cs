using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class GameSceneSpawnManager : NetworkBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private bool gameStarted = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Attendre que tous les joueurs aient chargé la scène.
        NetworkManager.SceneManager.OnLoadEventCompleted
            += OnSceneLoaded;
    }

    private void OnSceneLoaded(
        string sceneName,
        LoadSceneMode loadMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!IsServer || gameStarted)
            return;

        // On ne s'intéresse qu'à GameScene.
        if (sceneName != "GameScene")
            return;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogError(
                "Certains joueurs n'ont pas chargé GameScene."
            );
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab manquant !");
            return;
        }

        // Récupérer les positions valides.
        List<Transform> availableSpawns = new();

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                availableSpawns.Add(point);
        }

        int playerCount =
            NetworkManager.ConnectedClientsIds.Count;

        if (availableSpawns.Count < playerCount)
        {
            Debug.LogError(
                "Pas assez de SpawnPoints pour les joueurs !"
            );
            return;
        }

        // Vérifier que tous les joueurs sont prêts.
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!clientsCompleted.Contains(clientId))
            {
                Debug.LogWarning(
                    $"Joueur {clientId} pas encore prêt."
                );
                return;
            }
        }

        gameStarted = true;

        // Créer les personnages.
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            // Éviter de créer deux capsules au même joueur.
            if (NetworkManager.SpawnManager
                .GetPlayerNetworkObject(clientId) != null)
                continue;

            int randomIndex =
                Random.Range(0, availableSpawns.Count);

            Transform spawnPoint =
                availableSpawns[randomIndex];

            // Retirer ce point pour éviter les doublons.
            availableSpawns.RemoveAt(randomIndex);

            NetworkObject player = Instantiate(
                playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            // Attribuer cette capsule au joueur.
            player.SpawnAsPlayerObject(clientId, true);

            Debug.Log(
                $"Joueur {clientId} apparu à " +
                $"{spawnPoint.position}"
            );
        }

        // Tous les personnages sont créés.
        // Le serveur peut commencer le compte à rebours.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMatch();
        }
        else
        {
            Debug.LogError("GameManager introuvable !");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted
                -= OnSceneLoaded;
        }
    }
}