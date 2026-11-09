using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PickupSpawner : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PickupArenaSettings settings;

    [Header("Pickup Prefabs")]
    [SerializeField] private NetworkObject[] pickupPrefabs;

    [Header("Spawn Positions")]
    [SerializeField] private Transform[] pickupSpawnPoints;

    private readonly List<NetworkObject> activePickups = new();
    private Coroutine spawnRoutine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (IsSpawned && IsServer)
        {
            if (settings != null)
            {
                // Retirer les pickups qui ont été ramassés.
                activePickups.RemoveAll(
                    pickup => pickup == null || !pickup.IsSpawned
                );

                if (activePickups.Count < settings.maxActivePickups)
                {
                    SpawnRandomPickup();
                }
            }

            float interval = settings != null
                ? Mathf.Max(0.2f, settings.pickupSpawnInterval)
                : 3f;

            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnRandomPickup()
    {
        if (pickupPrefabs == null || pickupPrefabs.Length == 0)
            return;

        if (pickupSpawnPoints == null || pickupSpawnPoints.Length == 0)
            return;

        // Chercher les positions libres.
        List<Transform> availablePoints = new();

        foreach (Transform point in pickupSpawnPoints)
        {
            if (point == null) continue;

            bool occupied = false;

            foreach (NetworkObject pickup in activePickups)
            {
                if (pickup != null &&
                    Vector3.Distance(
                        pickup.transform.position,
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

        if (availablePoints.Count == 0)
            return;

        // Choisir un emplacement aléatoire.
        Transform spawnPoint = availablePoints[
            Random.Range(0, availablePoints.Count)
        ];

        // Choisir un type de pickup aléatoire.
        NetworkObject selectedPrefab = pickupPrefabs[
            Random.Range(0, pickupPrefabs.Length)
        ];

        if (selectedPrefab == null)
            return;

        // Créer le pickup côté serveur.
        NetworkObject pickupInstance = Instantiate(
            selectedPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Le faire apparaître chez tous les joueurs.
        pickupInstance.Spawn();

        activePickups.Add(pickupInstance);
    }

    public override void OnNetworkDespawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        activePickups.Clear();
    }
}
