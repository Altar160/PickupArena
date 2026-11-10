using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text scoresText;

    // Joueurs actuellement présents.
    private readonly Dictionary<ulong, PlayerScore> players = new();

    private void OnEnable()
    {
        // Écouter les apparitions et disparitions.
        PlayerScore.PlayerSpawned += RegisterPlayer;
        PlayerScore.PlayerDespawned += UnregisterPlayer;

        // Retrouver les joueurs déjà apparus.
        PlayerScore[] existingPlayers =
            FindObjectsByType<PlayerScore>(
                FindObjectsSortMode.None
            );

        foreach (PlayerScore player in existingPlayers)
        {
            RegisterPlayer(player);
        }

        RefreshUI();
    }

    private void RegisterPlayer(PlayerScore player)
    {
        if (player == null || !player.IsSpawned)
            return;

        ulong clientId = player.OwnerClientId;

        // Éviter les inscriptions en double.
        if (players.TryGetValue(clientId, out PlayerScore oldPlayer))
        {
            if (oldPlayer == player)
                return;

            if (oldPlayer != null)
                oldPlayer.ScoreChanged -= OnScoreChanged;
        }

        players[clientId] = player;

        // Écouter les changements de score.
        player.ScoreChanged += OnScoreChanged;

        RefreshUI();
    }

    private void UnregisterPlayer(PlayerScore player)
    {
        if (player == null)
            return;

        player.ScoreChanged -= OnScoreChanged;

        ulong clientId = player.OwnerClientId;

        if (players.TryGetValue(clientId, out PlayerScore current)
            && current == player)
        {
            players.Remove(clientId);
        }

        RefreshUI();
    }

    private void OnScoreChanged(PlayerScore player)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (scoresText == null)
            return;

        List<PlayerScore> ranking =
            new List<PlayerScore>(players.Values);

        ranking.RemoveAll(
            player => player == null || !player.IsSpawned
        );

        // Classer les joueurs par score décroissant.
        // En cas d'égalité, départager par identifiant.
        ranking.Sort((a, b) =>
        {
            int result = b.Score.Value.CompareTo(a.Score.Value);

            if (result != 0)
                return result;

            return a.OwnerClientId.CompareTo(b.OwnerClientId);
        });

        StringBuilder display = new StringBuilder();

        display.AppendLine("CLASSEMENT");
        display.AppendLine();

        NetworkManager manager = NetworkManager.Singleton;

        for (int i = 0; i < ranking.Count; i++)
        {
            PlayerScore player = ranking[i];

            string name = $"Joueur {player.OwnerClientId + 1}";

            if (player.OwnerClientId == NetworkManager.ServerClientId)
                name += " (Host)";

            if (manager != null &&
                player.OwnerClientId == manager.LocalClientId)
                name += " (vous)";

            display.AppendLine(
                $"{i + 1}. {name} : {player.Score.Value} pts"
            );
        }

        scoresText.text = display.ToString();
    }

    private void OnDisable()
    {
        PlayerScore.PlayerSpawned -= RegisterPlayer;
        PlayerScore.PlayerDespawned -= UnregisterPlayer;

        foreach (PlayerScore player in players.Values)
        {
            if (player != null)
                player.ScoreChanged -= OnScoreChanged;
        }

        players.Clear();
    }
}