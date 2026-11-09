using UnityEngine;
using Unity.Netcode;

public class PlayerScore : NetworkBehaviour
{
    public readonly NetworkVariable<int> Score =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        Score.OnValueChanged += OnScoreChanged;

        Debug.Log(
            $"Joueur {OwnerClientId} : score = {Score.Value}"
        );
    }

    public override void OnNetworkDespawn()
    {
        Score.OnValueChanged -= OnScoreChanged;
    }

    public void AddPoints(int points)
    {
        if (!IsServer || points <= 0)
            return;

        Score.Value += points;
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        Debug.Log(
            $"Joueur {OwnerClientId} : {oldScore} -> {newScore} points"
        );
    }
}
