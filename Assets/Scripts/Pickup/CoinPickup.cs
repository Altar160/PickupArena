using UnityEngine;
using Unity.Netcode;

public class CoinPickup : Pickup
{
    [SerializeField]
    private PickupArenaSettings settings;

    protected override void ApplyEffect(NetworkObject player)
    {
        if (settings == null)
            return;

        PlayerScore playerScore =
            player.GetComponent<PlayerScore>();

        if (playerScore == null)
            return;

        playerScore.AddPoints(settings.coinPoints);
    }
}
