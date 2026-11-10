using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text scoresText;

    private readonly Dictionary<ulong, PlayerScore> players = new();

    private void OnEnable()
    {
        PlayerScore.PlayerSpawned += RegisterPlayer;
        PlayerScore.PlayerDespawned += UnregisterPlayer;

        if (scoresText != null)
            scoresText.richText = false;

        // Retrouver les joueurs déjà présents.
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

        if (players.TryGetValue(
            clientId, out PlayerScore oldPlayer))
        {
            if (oldPlayer == player)
                return;

            if (oldPlayer != null)
            {
                oldPlayer.ScoreChanged -= OnScoreChanged;
                oldPlayer.NicknameChanged -= OnNicknameChanged;
            }
        }

        players[clientId] = player;

        player.ScoreChanged += OnScoreChanged;
        player.NicknameChanged += OnNicknameChanged;

        RefreshUI();
    }

    private void UnregisterPlayer(PlayerScore player)
    {
        if (player == null)
            return;

        player.ScoreChanged -= OnScoreChanged;
        player.NicknameChanged -= OnNicknameChanged;

        ulong clientId = player.OwnerClientId;

        if (players.TryGetValue(
            clientId, out PlayerScore current) &&
            current == player)
        {
            players.Remove(clientId);
        }

        RefreshUI();
    }

    private void OnScoreChanged(PlayerScore player)
    {
        RefreshUI();
    }

    private void OnNicknameChanged(PlayerScore player)
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

        // Trier par score décroissant.
        ranking.Sort((a, b) =>
        {
            int result =
                b.Score.Value.CompareTo(a.Score.Value);

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

            // Utiliser le vrai pseudo.
            string nickname = player.Nickname.Value.ToString();

            // Nom de secours si le pseudo n'est pas reçu.
            if (string.IsNullOrWhiteSpace(nickname))
            {
                nickname =
                    $"Joueur {player.OwnerClientId + 1}";
            }

            if (player.OwnerClientId ==
                NetworkManager.ServerClientId)
            {
                nickname += " (Host)";
            }

            if (manager != null &&
                player.OwnerClientId == manager.LocalClientId)
            {
                nickname += " (vous)";
            }

            display.AppendLine(
                $"{i + 1}. {nickname} : {player.Score.Value} pts"
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
            {
                player.ScoreChanged -= OnScoreChanged;
                player.NicknameChanged -= OnNicknameChanged;
            }
        }

        players.Clear();
    }
}