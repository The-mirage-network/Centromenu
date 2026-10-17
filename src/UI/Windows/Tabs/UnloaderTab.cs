using System;
using System.Collections.Generic;
using UnityEngine;

namespace Centromenu;




public sealed class UnloaderTab : ITab
{
    private byte? _selectedPlayerId;
    private Vector2 _playerScrollPosition;
    private string _status = "Select a player to overwrite SpecifiedPlayer.";




    public static PlayerControl SpecifiedPlayer;

    public string name => "Unloader";

    public void Draw()
    {
        GUILayout.BeginHorizontal();
        try
        {
            DrawPlayerList();
            GUILayout.Space(15f);
            DrawOverwriteControls();
        }
        finally
        {
            GUILayout.EndHorizontal();
        }
    }

    private void DrawPlayerList()
    {
        GUILayout.BeginVertical(GUIStylePreset.RoundedPanel, GUILayout.Width(250f));
        try
        {
            GUILayout.Label("Players", GUIStylePreset.TabSubtitle);

            _playerScrollPosition = GUILayout.BeginScrollView(
                _playerScrollPosition,
                false,
                true,
                GUILayout.Height(425f));
            try
            {
                var players = GetPlayerSnapshot();
                if (players.Count == 0)
                {
                    GUILayout.Label("Join a lobby to see players.");
                    return;
                }

                foreach (var player in players)
                {
                    var oldColor = GUI.backgroundColor;
                    try
                    {
                        if (player.Data.IsDead)
                            GUI.backgroundColor = new Color(0.8f, 0.12f, 0.12f);
                        else if (_selectedPlayerId == player.PlayerId)
                            GUI.backgroundColor = player.Data.Color;

                        var suffix = player.AmOwner ? " (You)" : "";
                        if (GUILayout.Button(
                                player.Data.PlayerName + suffix,
                                GUIStylePreset.NormalButton,
                                GUILayout.Height(34f)))
                        {
                            _selectedPlayerId = player.PlayerId;
                            _status = $"Selected {player.Data.PlayerName}.";
                        }
                    }
                    finally
                    {
                        GUI.backgroundColor = oldColor;
                    }
                }
            }
            finally
            {
                GUILayout.EndScrollView();
            }
        }
        finally
        {
            GUILayout.EndVertical();
        }
    }

    private void DrawOverwriteControls()
    {
        GUILayout.BeginVertical(GUIStylePreset.RoundedPanel, GUILayout.ExpandWidth(true));
        try
        {
            var selectedPlayer = FindPlayer(_selectedPlayerId);
            var specifiedPlayer = IsUsable(SpecifiedPlayer) ? SpecifiedPlayer : null;

            GUILayout.Label(
                selectedPlayer ? $"Selected: {selectedPlayer.Data.PlayerName}" : "Select a player",
                GUIStylePreset.TabSubtitle);

            GUILayout.Space(10f);
            GUILayout.Label(
                specifiedPlayer
                    ? $"SpecifiedPlayer: {specifiedPlayer.Data.PlayerName}"
                    : "SpecifiedPlayer: Not set");

            GUILayout.Space(16f);
            var wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && selectedPlayer;
            if (GUILayout.Button("Overwrite", GUIStylePreset.NormalButton, GUILayout.Height(40f)))
                Overwrite(selectedPlayer);
            GUI.enabled = wasEnabled;

            GUILayout.Space(12f);
            GUILayout.Label(_status);
        }
        finally
        {
            GUILayout.EndVertical();
        }
    }

    private void Overwrite(PlayerControl selectedPlayer)
    {
        if (!IsUsable(selectedPlayer)) return;

        SpecifiedPlayer = selectedPlayer;
        var playerName = selectedPlayer.Data.PlayerName;
        _status = $"SpecifiedPlayer overwritten with {playerName}.";

        OnSpecifiedPlayerOverwritten(SpecifiedPlayer);

        if (CheatToggles.showConsole)
            ConsoleUI.Log($"SpecifiedPlayer: {playerName}");
    }

    private void OnSpecifiedPlayerOverwritten(PlayerControl specifiedPlayer)
    {
        if (!specifiedPlayer.Data.IsDead)
        {
            CentroAPI.Notification(
                "BODY NOT REPORTED",
                $"{specifiedPlayer.Data.PlayerName}'s body was not reported because they are not dead.");
            return;
        }



        PlayerControl.LocalPlayer.CmdReportDeadBody(specifiedPlayer.Data);
        CentroAPI.Notification("DEAD BODY REPORTED");
    }

    private static PlayerControl FindPlayer(byte? playerId)
    {
        if (!playerId.HasValue) return null;

        foreach (var player in GetPlayerSnapshot())
            if (player.PlayerId == playerId.Value) return player;

        return null;
    }

    private static List<PlayerControl> GetPlayerSnapshot()
    {
        var snapshot = new List<PlayerControl>();
        try
        {
            var players = PlayerControl.AllPlayerControls;
            if (players == null) return snapshot;

            var count = players.Count;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    var player = players[i];
                    if (IsUsable(player)) snapshot.Add(player);
                }
                catch { }
            }
        }
        catch (Exception) { }

        return snapshot;
    }

    private static bool IsUsable(PlayerControl player)
    {
        try
        {
            return player && player.Data != null && !player.Data.Disconnected;
        }
        catch
        {
            return false;
        }
    }
}
