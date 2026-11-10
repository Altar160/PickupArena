using UnityEngine;
using Unity.Netcode;

public class FreezeBombPickup : Pickup
{
    [SerializeField]
    private PickupArenaSettings settings;

    protected override void ApplyEffect(NetworkObject player)
    {
        if (settings == null)
            return;

        ulong collectorId = player.OwnerClientId;

        foreach (NetworkClient client
            in NetworkManager.ConnectedClientsList)
        {
            NetworkObject target = client.PlayerObject;

            if (target == null)
                continue;

            // Ne pas geler celui qui ramasse.
            if (target.OwnerClientId == collectorId)
                continue;

            IPlayerEffectReceiver receiver =
                target.GetComponent<IPlayerEffectReceiver>();

            if (receiver == null)
                continue;

            receiver.ApplyFreeze(settings.freezeDuration);
        }
    }
}