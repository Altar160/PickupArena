using UnityEngine;
using Unity.Netcode;

public class RainbowPickup : Pickup
{
    [SerializeField] private PickupArenaSettings settings;

    protected override void ApplyEffect(NetworkObject player)
    {
        if (settings == null || player == null)
            return;

        IPlayerEffectReceiver receiver =
            player.GetComponent<IPlayerEffectReceiver>();

        if (receiver == null)
            return;

        // 1. Donner les points
        receiver.GivePoints(settings.rainbowPoints);

        // 2. Donner le speed boost au joueur qui ramasse
        receiver.ApplySpeedBoost(
            settings.rainbowSpeedBoostMultiplier,
            settings.rainbowSpeedBoostDuration
        );

        // 3. Freeze tous les autres joueurs
        ulong collectorId = player.OwnerClientId;

        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            NetworkObject target = client.PlayerObject;

            if (target == null)
                continue;

            // Ne pas geler le joueur qui a ramassé le pickup
            if (target.OwnerClientId == collectorId)
                continue;

            IPlayerEffectReceiver targetReceiver =
                target.GetComponent<IPlayerEffectReceiver>();

            if (targetReceiver == null)
                continue;

            targetReceiver.ApplyFreeze(settings.rainbowFreezeDuration);
        }
    }
}