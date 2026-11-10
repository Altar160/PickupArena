using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "PickupArenaSettings",
    menuName = "Pickup Arena/Game Settings"
)]
public class PickupArenaSettings : ScriptableObject
{
    [Header("Match Settings")]
    [Min(1)]
    public int countdownDuration = 3;

    [Min(1)]
    public int matchDuration = 90;

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
    public float pickupSpawnInterval = 1.5f;

    [FormerlySerializedAs("maxActiveCoins")]
    [Min(1)]
    public int maxActivePickups = 10;
    [Header("Rainbow Pickup Settings")]
    [Min(1)]
    public int rainbowPoints = 5;

    [Min(1f)]
    public float rainbowSpeedBoostMultiplier = 2f;

    [Min(0.1f)]
    public float rainbowSpeedBoostDuration = 5f;

    [Min(0.1f)]
    public float rainbowFreezeDuration = 2f;

    [Range(0f, 1f)]
    public float rainbowPickupChance = 0.1f;
}