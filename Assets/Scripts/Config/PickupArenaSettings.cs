using UnityEngine;

[CreateAssetMenu(
    fileName = "PickupArenaSettings",
    menuName = "Pickup Arena/Game Settings"
)]
public class PickupArenaSettings : ScriptableObject
{
    [Header("Coin Settings")]
    [Min(1)]
    public int coinPoints = 1;

    [Header("Spawn Settings")]
    [Min(0.2f)]
    public float coinSpawnInterval = 3f;

    [Min(1)]
    public int maxActiveCoins = 3;
}
