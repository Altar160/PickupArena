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
        if (!IsSpawned || !IsServer)
            return;

        bool isPlaying =
            GameManager.Instance != null &&
            GameManager.Instance.IsSpawned &&
            GameManager.Instance.State.Value == GamePhase.Playing;

        if (!isPlaying)
        {
            if (wasPlaying)
                ClearPickups();

            wasPlaying = false;
            return;
        }

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

        // Retirer les références des pickups ramassés.
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

        List<Transform> availablePoints = new();

        // Trouver les positions libres.
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

        Transform spawnPoint = availablePoints[
            Random.Range(0, availablePoints.Count)
        ];

        NetworkObject selectedPrefab = GetRandomPickupPrefab();

        if (selectedPrefab == null)
            return;

        NetworkObject pickupInstance = Instantiate(
            selectedPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        pickupInstance.Spawn(true);

        activePickups.Add(pickupInstance);
    }

    private NetworkObject GetRandomPickupPrefab()
    {
        // Vérifier si un RainbowPickup est déjà actif.
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

        // Tenter le RainbowPickup rare.
        if (!rainbowAlreadyActive &&
            rainbowPickupPrefab != null &&
            Random.value < Mathf.Clamp01(
                settings.rainbowPickupChance))
        {
            return rainbowPickupPrefab;
        }

        // Sinon, choisir un pickup normal
        // selon les poids définis dans GameSettings.
        return GetWeightedNormalPickup();
    }

    private NetworkObject GetWeightedNormalPickup()
    {
        if (pickupPrefabs == null ||
            pickupPrefabs.Length == 0)
            return null;

        List<NetworkObject> validPrefabs = new();
        List<int> weights = new();

        int totalWeight = 0;

        foreach (NetworkObject prefab in pickupPrefabs)
        {
            if (prefab == null)
                continue;

            int weight = GetPickupWeight(prefab);

            // Un poids de 0 empêche son apparition.
            if (weight <= 0)
                continue;

            validPrefabs.Add(prefab);
            weights.Add(weight);

            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning(
                "Aucun pickup normal avec un poids positif !"
            );
            return null;
        }

        // Tirage aléatoire pondéré.
        int randomValue = Random.Range(0, totalWeight);

        for (int i = 0; i < validPrefabs.Count; i++)
        {
            if (randomValue < weights[i])
                return validPrefabs[i];

            randomValue -= weights[i];
        }

        return validPrefabs[validPrefabs.Count - 1];
    }

    private int GetPickupWeight(NetworkObject prefab)
    {
        if (prefab.GetComponent<CoinPickup>() != null)
            return Mathf.Max(0, settings.coinPickupWeight);

        if (prefab.GetComponent<SpeedBoostPickup>() != null)
            return Mathf.Max(0, settings.speedBoostPickupWeight);

        if (prefab.GetComponent<FreezeBombPickup>() != null)
            return Mathf.Max(0, settings.freezeBombPickupWeight);

        return 0;
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