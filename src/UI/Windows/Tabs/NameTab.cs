using System;
using System.Collections.Generic;
using UnityEngine;

namespace Centromenu;

public sealed class NameTab : ITab
{
    private const int MaxSpoofedNameLength = 64;

    private byte? _selectedPlayerId;
    private string _spoofedName = string.Empty;
    private string _hexColor = "#FFFFFF";
    private NameSpoofManager.ColorMode _colorMode = NameSpoofManager.ColorMode.Solid;
    private Vector2 _playerScroll;
    private string _status = "Select a player to spoof locally.";

    public string name => "Name";

    public void Draw()
    {
        GUILayout.BeginHorizontal();
        try
        {
            GUILayout.BeginVertical(GUIStylePreset.RoundedPanel, GUILayout.Width(220f));
            try
            {
                DrawPlayerPicker();
            }
            finally
            {
                GUILayout.EndVertical();
            }

            GUILayout.Space(10f);

            GUILayout.BeginVertical(GUIStylePreset.RoundedPanel, GUILayout.ExpandWidth(true));
            try
            {
                GUILayout.Label("Local username spoof", GUIStylePreset.TabSubtitle);
                GUILayout.Label("Visible on your screen only.");

                GUILayout.Space(8f);
                GUILayout.Label("Spoofed username", GUIStylePreset.TabSubtitle);
                GUI.SetNextControlName("SpoofedUsernameInput");
                _spoofedName = GUILayout.TextField(
                    _spoofedName,
                    MaxSpoofedNameLength,
                    GUIStylePreset.TextField,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(36f));

                GUILayout.Space(8f);
                GUILayout.Label("Color mode", GUIStylePreset.TabSubtitle);
                DrawModeButtons();

                if (_colorMode != NameSpoofManager.ColorMode.Rainbow)
                {
                    GUILayout.Label("Hex color");
                    _hexColor = GUILayout.TextField(
                        _hexColor,
                        9,
                        GUIStylePreset.TextField,
                        GUILayout.ExpandWidth(true),
                        GUILayout.Height(34f));
                }

                GUILayout.Space(10f);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && _selectedPlayerId.HasValue;
                if (GUILayout.Button("Execute", GUIStylePreset.NormalButton, GUILayout.Height(38f)))
                    ExecuteSpoof();

                GUI.enabled = previousEnabled && _selectedPlayerId.HasValue &&
                              NameSpoofManager.HasSpoof(_selectedPlayerId.Value);
                if (GUILayout.Button("Reset Selected Name", GUIStylePreset.NormalButton, GUILayout.Height(32f)))
                    ResetSelected();

                GUI.enabled = previousEnabled;
                GUILayout.Space(8f);
                GUILayout.Label(_status);
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }
        catch (Exception error)
        {
            _status = "The name panel could not refresh; see the game log.";
            Debug.LogError($"[Centromenu] Name panel failed: {error}");
        }
        finally
        {
            GUILayout.EndHorizontal();
        }
    }

    private void DrawPlayerPicker()
    {
        GUILayout.Label("Player", GUIStylePreset.TabSubtitle);
        _playerScroll = GUILayout.BeginScrollView(_playerScroll, GUILayout.Height(405f));
        try
        {
            bool any = false;
            bool selectedStillAvailable = false;
            foreach (var player in GetPlayers())
            {
                if (!IsUsable(player)) continue;
                any = true;
                bool selected = _selectedPlayerId == player.PlayerId;
                selectedStillAvailable |= selected;

                if (GUILayout.Button(
                        selected ? $"> {player.Data.PlayerName}" : player.Data.PlayerName,
                        GUIStylePreset.NormalButton,
                        GUILayout.Height(32f)))
                {
                    _selectedPlayerId = player.PlayerId;
                    _status = $"Selected {player.Data.PlayerName}.";
                }
            }

            if (!any) GUILayout.Label("Join a lobby or game to list players.");
            if (_selectedPlayerId.HasValue && !selectedStillAvailable)
                _selectedPlayerId = null;
        }
        finally
        {
            GUILayout.EndScrollView();
        }
    }

    private void DrawModeButtons()
    {
        GUILayout.BeginHorizontal();
        DrawModeButton("Single", NameSpoofManager.ColorMode.Solid);
        DrawModeButton("Rainbow", NameSpoofManager.ColorMode.Rainbow);
        DrawModeButton("Animate", NameSpoofManager.ColorMode.Animate);
        GUILayout.EndHorizontal();

        string description = _colorMode switch
        {
            NameSpoofManager.ColorMode.Rainbow => "Slowly cycles the whole name through rainbow colors.",
            NameSpoofManager.ColorMode.Animate => "Moves a color wave through the name, based on your hex color.",
            _ => "Uses one fixed hex color."
        };
        GUILayout.Label(description);
    }

    private void DrawModeButton(string label, NameSpoofManager.ColorMode mode)
    {
        Color previous = GUI.backgroundColor;
        if (_colorMode == mode) GUI.backgroundColor = new Color(0.25f, 0.7f, 1f);
        if (GUILayout.Button(label, GUIStylePreset.NormalButton, GUILayout.Height(32f)))
            _colorMode = mode;
        GUI.backgroundColor = previous;
    }

    private void ExecuteSpoof()
    {
        PlayerControl player = FindPlayer(_selectedPlayerId);
        if (!IsUsable(player))
        {
            _selectedPlayerId = null;
            _status = "The selected player is no longer available.";
            return;
        }

        string spoofedName = (_spoofedName ?? string.Empty).Trim();
        if (spoofedName.Length == 0)
        {
            _status = "Enter a spoofed username first.";
            return;
        }

        Color chosenColor = Color.white;
        if (_colorMode != NameSpoofManager.ColorMode.Rainbow &&
            !TryParseHexColor(_hexColor, out chosenColor))
        {
            _status = "Enter a valid hex color such as #4FC3F7.";
            return;
        }

        string originalName = player.Data.PlayerName;
        NameSpoofManager.Set(player.PlayerId, spoofedName, _colorMode, chosenColor);
        _status = $"{originalName} is now displayed as {spoofedName}.";
        CentroAPI.Notification($"{originalName} user has been spoofed into {spoofedName}");
    }

    private void ResetSelected()
    {
        if (!_selectedPlayerId.HasValue) return;
        PlayerControl player = FindPlayer(_selectedPlayerId);
        string playerName = IsUsable(player) ? player.Data.PlayerName : "Selected player";
        NameSpoofManager.Remove(_selectedPlayerId.Value);
        _status = $"Restored {playerName}'s original display name.";
    }

    private static bool TryParseHexColor(string value, out Color color)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!normalized.StartsWith("#")) normalized = "#" + normalized;
        return ColorUtility.TryParseHtmlString(normalized, out color);
    }

    private static PlayerControl FindPlayer(byte? playerId)
    {
        if (!playerId.HasValue) return null;
        foreach (var player in GetPlayers())
        {
            if (IsUsable(player) && player.PlayerId == playerId.Value) return player;
        }
        return null;
    }

    private static IEnumerable<PlayerControl> GetPlayers()
    {
        if (PlayerControl.AllPlayerControls == null) yield break;
        foreach (var player in PlayerControl.AllPlayerControls) yield return player;
    }

    private static bool IsUsable(PlayerControl player)
    {
        return player && player.Data != null && !player.Data.Disconnected;
    }
}
