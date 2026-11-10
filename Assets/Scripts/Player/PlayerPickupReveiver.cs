using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerScore))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerPickupReceiver :
    NetworkBehaviour, IPlayerEffectReceiver
{
    private PlayerScore playerScore;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerScore = GetComponent<PlayerScore>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void GivePoints(int points)
    {
        if (!IsSpawned || !IsServer)
            return;

        playerScore.AddPoints(points);
    }

    public void ApplySpeedBoost(
        float multiplier,
        float duration)
    {
        if (!IsSpawned || !IsServer)
            return;

        playerMovement.ApplySpeedBoost(
            multiplier,
            duration
        );
    }

    public void ApplyFreeze(float duration)
    {
        if (!IsSpawned || !IsServer)
            return;

        playerMovement.ApplyFreeze(duration);
    }
}