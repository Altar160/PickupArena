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
        {
            // IMPORTANT : autoriser les balises de couleur TMP.
            scoresText.richText = true;
        }

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
                UnsubscribePlayer(oldPlayer);
        }

        players[clientId] = player;

        player.ScoreChanged += OnScoreChanged;
        player.NicknameChanged += OnNicknameChanged;

        PlayerColor color = player.GetComponent<PlayerColor>();

        if (color != null)
            color.ColorChanged += OnColorChanged;

        RefreshUI();
    }

    private void UnregisterPlayer(PlayerScore player)
    {
        if (player == null)
            return;

        UnsubscribePlayer(player);

        ulong clientId = player.OwnerClientId;

        if (players.TryGetValue(
            clientId, out PlayerScore current) &&
            current == player)
        {
            players.Remove(clientId);
        }

        RefreshUI();
    }

    private void UnsubscribePlayer(PlayerScore player)
    {
        player.ScoreChanged -= OnScoreChanged;
        player.NicknameChanged -= OnNicknameChanged;

        PlayerColor color = player.GetComponent<PlayerColor>();

        if (color != null)
            color.ColorChanged -= OnColorChanged;
    }

    private void OnScoreChanged(PlayerScore player)
    {
        RefreshUI();
    }

    private void OnNicknameChanged(PlayerScore player)
    {
        RefreshUI();
    }

    private void OnColorChanged()
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

        // Meilleur score en premier.
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

            string nickname = player.Nickname.Value.ToString();

            if (string.IsNullOrWhiteSpace(nickname))
                nickname = $"Joueur {player.OwnerClientId + 1}";

            // Éviter que le pseudo soit interprété
            // comme une balise TextMeshPro.
            nickname = nickname
                .Replace("<", "‹")
                .Replace(">", "›");

            PlayerColor playerColor =
                player.GetComponent<PlayerColor>();

            string coloredNickname = nickname;

            if (playerColor != null)
            {
                string htmlColor = PlayerColor.GetHtmlColor(
                    playerColor.ColorIndex.Value
                );

                coloredNickname =
                    $"<color={htmlColor}>{nickname}</color>";
            }

            display.Append($"{i + 1}. ");
            display.Append(coloredNickname);

            if (player.OwnerClientId ==
                NetworkManager.ServerClientId)
            {
                display.Append(" (Host)");
            }

            if (manager != null &&
                player.OwnerClientId == manager.LocalClientId)
            {
                display.Append(" (vous)");
            }

            display.AppendLine(
                $" : {player.Score.Value} pts"
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
                UnsubscribePlayer(player);
        }

        players.Clear();
    }
}