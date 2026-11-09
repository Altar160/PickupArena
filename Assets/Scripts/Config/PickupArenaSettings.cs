using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "PickupArenaSettings",
    menuName = "Pickup Arena/Game Settings"
)]
public class PickupArenaSettings : ScriptableObject
{
    [Header("Coin Settings")]
    [Min(1)]
    public int coinPoints = 1;

    [Header("Speed Boost Settings")]
    [Min(1f)]
    public float speedBoostMultiplier = 1.5f;

    [Min(0.1f)]
    public float speedBoostDuration = 5f;

    [Header("Freeze Bomb Settings")]
    [Min(0.1f)]
    public float freezeDuration = 2f;

    [Header("Pickup Spawn Settings")]
    [FormerlySerializedAs("coinSpawnInterval")]
    [Min(0.2f)]
    public float pickupSpawnInterval = 3f;

    [FormerlySerializedAs("maxActiveCoins")]
    [Min(1)]
    public int maxActivePickups = 3;
}