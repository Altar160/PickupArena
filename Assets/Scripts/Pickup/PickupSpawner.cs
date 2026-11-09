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

    private float nextSpawnTime;
    private bool wasPlaying;

    private void Update()
    {
        // Seul le serveur gère les pickups.
        if (!IsSpawned || !IsServer)
            return;

        bool isPlaying =
            GameManager.Instance != null &&
            GameManager.Instance.IsSpawned &&
            GameManager.Instance.State.Value == GamePhase.Playing;

        // En dehors d'une manche, supprimer les pickups.
        if (!isPlaying)
        {
            if (wasPlaying)
                ClearPickups();

            wasPlaying = false;
            return;
        }

        // Début de la manche.
        if (!wasPlaying)
        {
            wasPlaying = true;
            nextSpawnTime = Time.time;
        }

        if (settings == null)
            return;

        if (Time.time < nextSpawnTime)
            return;

        nextSpawnTime = Time.time +
            Mathf.Max(0.2f, settings.pickupSpawnInterval);

        // Nettoyer les références des pickups ramassés.
        activePickups.RemoveAll(
            pickup => pickup == null || !pickup.IsSpawned
        );

        if (activePickups.Count < settings.maxActivePickups)
            SpawnRandomPickup();
    }

    private void SpawnRandomPickup()
    {
        if (pickupPrefabs == null ||
            pickupPrefabs.Length == 0 ||
            pickupSpawnPoints == null ||
            pickupSpawnPoints.Length == 0)
            return;

        // Rechercher les positions disponibles.
        List<Transform> availablePoints = new();

        foreach (Transform point in pickupSpawnPoints)
        {
            if (point == null)
                continue;

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

        // Choisir une position.
        Transform spawnPoint = availablePoints[
            Random.Range(0, availablePoints.Count)
        ];

        // Chercher les prefabs valides.
        List<NetworkObject> validPrefabs = new();

        foreach (NetworkObject prefab in pickupPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
            return;

        // Choisir un pickup aléatoire.
        NetworkObject selectedPrefab = validPrefabs[
            Random.Range(0, validPrefabs.Count)
        ];

        NetworkObject pickupInstance = Instantiate(
            selectedPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Créer le pickup chez tous les joueurs.
        pickupInstance.Spawn();

        activePickups.Add(pickupInstance);
    }

    private void ClearPickups()
    {
        foreach (NetworkObject pickup in activePickups)
        {
            if (pickup != null && pickup.IsSpawned)
                pickup.Despawn(true);
        }

        activePickups.Clear();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            ClearPickups();

        wasPlaying = false;
    }
}