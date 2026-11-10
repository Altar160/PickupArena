using UnityEngine;
using Unity.Netcode;

public class SpeedBoostPickup : Pickup
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

        receiver.ApplySpeedBoost(
            settings.speedBoostMultiplier,
            settings.speedBoostDuration
        );
    }
}