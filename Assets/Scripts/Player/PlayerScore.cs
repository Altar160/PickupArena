
using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerScore : NetworkBehaviour
{
    // Événements permettant de suivre les joueurs.
    public static event Action<PlayerScore> PlayerSpawned;
    public static event Action<PlayerScore> PlayerDespawned;

    // Événement déclenché lorsque le score change.
    public event Action<PlayerScore> ScoreChanged;

    // Score synchronisé, modifiable uniquement
    // par le serveur.
    public readonly NetworkVariable<int> Score =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        Score.OnValueChanged += OnScoreChanged;

        // Prévenir les systèmes intéressés.
        PlayerSpawned?.Invoke(this);
    }

    public void AddPoints(int points)
    {
        // Seul le serveur attribue les points.
        if (!IsSpawned || !IsServer || points <= 0)
            return;

        Score.Value += points;
    }

    public void ResetScore()
    {
        // Utile pour relancer une manche plus tard.
        if (!IsSpawned || !IsServer)
            return;

        Score.Value = 0;
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        Debug.Log(
            $"Joueur {OwnerClientId} : " +
            $"{oldScore} -> {newScore} points"
        );

        // Prévenir l'interface du changement.
        ScoreChanged?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        PlayerDespawned?.Invoke(this);

        Score.OnValueChanged -= OnScoreChanged;
    }
}
