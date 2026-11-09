
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
}
