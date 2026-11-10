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

        IPlayerEffectReceiver receiver =
            player.GetComponent<IPlayerEffectReceiver>();

        if (receiver == null)
            return;

        receiver.GivePoints(settings.coinPoints);
    }
}