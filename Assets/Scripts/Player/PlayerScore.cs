using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerScore : NetworkBehaviour
{
    // Événements pour l'interface.
    public static event Action<PlayerScore> PlayerSpawned;
    public static event Action<PlayerScore> PlayerDespawned;

    public event Action<PlayerScore> ScoreChanged;
    public event Action<PlayerScore> NicknameChanged;

    // Score synchronisé entre tous les joueurs.
    public readonly NetworkVariable<int> Score =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // Pseudo synchronisé entre tous les joueurs.
    public readonly NetworkVariable<FixedString64Bytes> Nickname =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        Score.OnValueChanged += OnScoreChanged;
        Nickname.OnValueChanged += OnNicknameChanged;

        // Seul le serveur attribue les pseudos.
        if (IsServer)
        {
            SessionConnectionApproval approval =
                NetworkManager.GetComponent<
                    SessionConnectionApproval>();

            string playerName = approval != null
                ? approval.GetPlayerName(OwnerClientId)
                : $"Joueur {OwnerClientId + 1}";

            Nickname.Value =
                new FixedString64Bytes(playerName);
        }

        // Informer les interfaces de l'apparition.
        PlayerSpawned?.Invoke(this);
    }

    public void AddPoints(int points)
    {
        if (!IsSpawned || !IsServer || points <= 0)
            return;

        Score.Value += points;
    }

    public void ResetScore()
    {
        if (!IsSpawned || !IsServer)
            return;

        Score.Value = 0;
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        Debug.Log(
            $"{Nickname.Value} : {oldScore} -> {newScore} points"
        );

        ScoreChanged?.Invoke(this);
    }

    private void OnNicknameChanged(
        FixedString64Bytes oldName,
        FixedString64Bytes newName)
    {
        // Informer l'interface du changement de pseudo.
        NicknameChanged?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        PlayerDespawned?.Invoke(this);

        Score.OnValueChanged -= OnScoreChanged;
        Nickname.OnValueChanged -= OnNicknameChanged;
    }
}