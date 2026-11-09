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

        // Identifier le joueur qui a ramassé la bombe.
        ulong collectorId = player.OwnerClientId;

        // Parcourir les joueurs connectés.
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            NetworkObject targetPlayer = client.PlayerObject;

            if (targetPlayer == null)
                continue;

            // Ne pas geler celui qui ramasse la bombe.
            if (targetPlayer.OwnerClientId == collectorId)
                continue;

            PlayerMovement movement =
                targetPlayer.GetComponent<PlayerMovement>();

            if (movement == null)
                continue;

            // Appliquer le gel aux autres joueurs.
            movement.ApplyFreeze(settings.freezeDuration);
        }
    }
}