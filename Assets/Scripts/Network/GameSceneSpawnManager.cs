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

        // Récupérer les points d'apparition valides.
        List<Transform> availableSpawns = new();

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                availableSpawns.Add(point);
        }

        // Trier les identifiants pour attribuer les couleurs
        // dans un ordre constant, en commençant par le Host.
        List<ulong> clientIds =
            new List<ulong>(NetworkManager.ConnectedClientsIds);

        clientIds.Sort();

        if (availableSpawns.Count < clientIds.Count)
        {
            Debug.LogError("Pas assez de SpawnPoints !");
            return;
        }

        // Vérifier que tous les joueurs ont chargé la scène.
        foreach (ulong clientId in clientIds)
        {
            if (!clientsCompleted.Contains(clientId))
            {
                Debug.LogWarning(
                    $"Le joueur {clientId} n'est pas prêt."
                );
                return;
            }
        }

        gameStarted = true;

        // Créer et configurer chaque personnage.
        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];

            if (NetworkManager.SpawnManager
                .GetPlayerNetworkObject(clientId) != null)
                continue;

            int randomIndex =
                Random.Range(0, availableSpawns.Count);

            Transform spawnPoint =
                availableSpawns[randomIndex];

            availableSpawns.RemoveAt(randomIndex);

            NetworkObject player = Instantiate(
                playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            player.SpawnAsPlayerObject(clientId, true);

            // NOUVEAU : attribuer la couleur du joueur.
            PlayerColor playerColor =
                player.GetComponent<PlayerColor>();

            if (playerColor != null)
            {
                playerColor.SetColorIndex(i);
            }
            else
            {
                Debug.LogWarning(
                    "Le prefab Player n'a pas de PlayerColor !"
                );
            }

            Debug.Log(
                $"Joueur {clientId} apparu au SpawnPoint " +
                $"{spawnPoint.name}, couleur {i + 1}"
            );
        }

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