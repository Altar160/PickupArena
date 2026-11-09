using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PickupSpawner : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PickupArenaSettings settings;
    [SerializeField] private NetworkObject coinPrefab;
    [SerializeField] private Transform[] pickupSpawnPoints;

    private readonly List<NetworkObject> activeCoins = new();
    private Coroutine spawnRoutine;

    public override void OnNetworkSpawn()
    {
        // Seul le serveur génère les pièces.
        if (!IsServer) return;

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (IsSpawned && IsServer)
        {
            if (settings != null && coinPrefab != null)
            {
                // Retirer les pièces déjà ramassées.
                activeCoins.RemoveAll(
                    coin => coin == null || !coin.IsSpawned
                );

                if (activeCoins.Count < settings.maxActiveCoins)
                {
                    SpawnCoin();
                }
            }

            float interval = settings != null
                ? Mathf.Max(0.2f, settings.coinSpawnInterval)
                : 3f;

            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnCoin()
    {
        if (pickupSpawnPoints == null) return;

        // Trouver les emplacements libres.
        List<Transform> availablePoints = new();

        foreach (Transform point in pickupSpawnPoints)
        {
            if (point == null) continue;

            bool occupied = false;

            foreach (NetworkObject coin in activeCoins)
            {
                if (coin != null &&
                    Vector3.Distance(
                        coin.transform.position,
                        point.position
                    ) < 0.75f)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
                availablePoints.Add(point);
        }

        if (availablePoints.Count == 0) return;

        // Choisir un emplacement aléatoire.
        int index = Random.Range(0, availablePoints.Count);
        Transform spawnPoint = availablePoints[index];

        // Créer la pièce sur le serveur.
        NetworkObject coinInstance = Instantiate(
            coinPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Synchroniser son apparition.
        coinInstance.Spawn();

        activeCoins.Add(coinInstance);
    }

    public override void OnNetworkDespawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        activeCoins.Clear();
    }
}