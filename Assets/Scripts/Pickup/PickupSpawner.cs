using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PickupSpawner : NetworkBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PickupArenaSettings settings;

    [Header("Normal Pickup Prefabs")]
    [SerializeField] private NetworkObject[] pickupPrefabs;

    [Header("Rare Rainbow Pickup")]
    [SerializeField] private NetworkObject rainbowPickupPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Transform[] pickupSpawnPoints;

    private readonly List<NetworkObject> activePickups = new();

    private float nextSpawnTime;
    private bool wasPlaying;

    private void Update()
    {
        // Seul le serveur fait apparaître les pickups.
        if (!IsSpawned || !IsServer)
            return;

        bool isPlaying =
            GameManager.Instance != null &&
            GameManager.Instance.IsSpawned &&
            GameManager.Instance.State.Value == GamePhase.Playing;

        // Supprimer les pickups quand la manche est terminée.
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
        if (pickupSpawnPoints == null ||
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

        // Choisir une position libre.
        Transform spawnPoint = availablePoints[
            Random.Range(0, availablePoints.Count)
        ];

        // Choisir le type de pickup.
        NetworkObject selectedPrefab = GetRandomPickupPrefab();

        if (selectedPrefab == null)
            return;

        // Créer le pickup.
        NetworkObject pickupInstance = Instantiate(
            selectedPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Le pickup est synchronisé et détruit
        // lors du déchargement de la scène.
        pickupInstance.Spawn(true);

        activePickups.Add(pickupInstance);
    }

    // NOUVEAU : choisir entre un pickup normal
    // et le RainbowPickup rare.
    private NetworkObject GetRandomPickupPrefab()
    {
        // Vérifier si un RainbowPickup est déjà présent.
        bool rainbowAlreadyActive = false;

        foreach (NetworkObject pickup in activePickups)
        {
            if (pickup != null &&
                pickup.IsSpawned &&
                pickup.GetComponent<RainbowPickup>() != null)
            {
                rainbowAlreadyActive = true;
                break;
            }
        }

        // Tenter de faire apparaître le RainbowPickup.
        // Exemple : 0.1 = 10 % de chance.
        if (!rainbowAlreadyActive &&
            rainbowPickupPrefab != null &&
            settings != null &&
            Random.value < Mathf.Clamp01(
                settings.rainbowPickupChance))
        {
            Debug.Log("RainbowPickup rare sélectionné !");

            return rainbowPickupPrefab;
        }

        // Sinon, choisir un pickup normal.
        if (pickupPrefabs == null ||
            pickupPrefabs.Length == 0)
            return null;

        List<NetworkObject> validPrefabs = new();

        foreach (NetworkObject prefab in pickupPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
            return null;

        return validPrefabs[
            Random.Range(0, validPrefabs.Count)
        ];
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