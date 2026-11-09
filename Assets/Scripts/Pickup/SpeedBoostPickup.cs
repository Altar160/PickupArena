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

        // Récupérer le système de déplacement du joueur.
        PlayerMovement movement =
            player.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        // Appliquer le bonus de vitesse.
        movement.ApplySpeedBoost(
            settings.speedBoostMultiplier,
            settings.speedBoostDuration
        );
    }
}