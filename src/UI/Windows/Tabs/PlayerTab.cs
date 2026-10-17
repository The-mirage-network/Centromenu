using System;
using System.Collections.Generic;
using System.Text;
using InnerNet;
using UnityEngine;
using CentroFeatures;

namespace Centromenu;






public sealed class PlayerTab : ITab
{
    private enum FollowMode
    {
        None,
        Stick
    }

    private const float FollowRpcInterval = 0.10f;

    private static byte? _selectedPlayerId;
    private static FollowMode _followMode;
    private static float _nextFollowRpc;
    private static bool _dataHooked;
    private static byte? _pendingHookPlayerId;
    private static List<(string label, string value)> _hookedRows = new();
    private static bool _drawErrorLogged;
    private static GUIStyle _dataLabelStyle;
    private static string _status = "Select a player to begin.";
    private static float _statusExpiresAt;

    private Vector2 _playerScroll;
    private Vector2 _detailScroll;

    public string name => "Players";

    public void Draw()
    {
        GUILayout.BeginHorizontal();
        try
        {
            DrawPlayerList();
            GUILayout.Space(12f);
            DrawPlayerPanel();
        }
        catch (Exception error)
        {
            if (!_drawErrorLogged)
            {
                Debug.LogError($"[Centromenu] Players panel failed to draw: {error}");
                _drawErrorLogged = true;
            }
        }
        finally
        {
            GUILayout.EndHorizontal();
        }
    }

    public static void Tick()
    {
        if (_pendingHookPlayerId.HasValue)
        {
            var requestedId = _pendingHookPlayerId.Value;
            _pendingHookPlayerId = null;
            var selected = FindPlayer(_selectedPlayerId);
            if (selected && selected.PlayerId == requestedId)
            {
                try
                {
                    _hookedRows = BuildDataRows(selected);
                    _dataHooked = true;
                    SetStatus($"Captured {_hookedRows.Count} fields for {selected.Data.PlayerName}.");
                }
                catch (Exception error)
                {
                    Debug.LogError($"[Centromenu] Could not capture player data: {error}");
                    SetStatus("Could not capture player data; see game log.");
                }
            }
        }

        if (_followMode == FollowMode.None) return;
        try
        {
            var local = PlayerControl.LocalPlayer;
            var target = FindPlayer(_selectedPlayerId);
            if (!IsUsable(local) || !IsUsable(target) || target.AmOwner)
            {
                StopFollowing("Follow stopped: player is no longer available.");
                return;
            }

            if (Time.unscaledTime < _nextFollowRpc) return;

            var destination = target.GetTruePosition();
            local.NetTransform.RpcSnapTo(destination);
            _nextFollowRpc = Time.unscaledTime + FollowRpcInterval;
        }
        catch
        {
            StopFollowing("Follow stopped while lobby data refreshed.");
        }
    }

    private void DrawPlayerList()
    {
        GUILayout.BeginVertical(GUIStylePreset.RoundedPanel, GUILayout.Width(180f));
        try
        {
            GUILayout.Label("PLAYER FRAME", GUIStylePreset.TabSubtitle);

            _playerScroll = GUILayout.BeginScrollView(
                _playerScroll,
                false,
                true,
                GUILayout.Height(440f));
            try
            {
                var any = false;
                foreach (var player in GetPlayerSnapshot())
                {
                    if (!IsUsable(player)) continue;
                    var originalColor = GUI.backgroundColor;
                    try
                    {
                        any = true;
                        if (_selectedPlayerId == player.PlayerId)
                            GUI.backgroundColor = GetPlayerColor(player);

                        var suffix = player.AmOwner ? " (You)" : "";
                        if (GUILayout.Button(
                                $"{player.Data.PlayerName}{suffix}",
                                GUIStylePreset.NormalButton,
                                GUILayout.Height(34f)))
                        {
                            SelectPlayer(player);
                        }
                    }
                    catch { }
                    finally
                    {
                        GUI.backgroundColor = originalColor;
                    }
                }

                if (!any) GUILayout.Label("Join a lobby to see players.");
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

    private void DrawPlayerPanel()
    {
        var target = FindPlayer(_selectedPlayerId);
        GUILayout.BeginVertical(GUIStylePreset.RoundedPanel, GUILayout.ExpandWidth(true));
        try
        {
            if (!IsUsable(target))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a player from the frame.", GUIStylePreset.TabSubtitle);
                GUILayout.FlexibleSpace();
                return;
            }

            _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(445f));
            try
            {
                GUILayout.Label(target.Data.PlayerName, GUIStylePreset.TabSubtitle);
                var playerColor = GetPlayerColor(target);
                GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(playerColor)}>■ Player color</color>");
                GUILayout.Label($"Player ID: {target.PlayerId}");
                GUILayout.Label(target.Data.IsDead ? "Status: Dead" : "Status: Alive");

                GUILayout.Space(8f);
                if (GUILayout.Button("Hook Data", GUIStylePreset.NormalButton, GUILayout.Height(38f)))
                {
                    _pendingHookPlayerId = target.PlayerId;
                }

                GUILayout.Space(8f);
                GUILayout.BeginVertical(GUIStylePreset.RoundedPanel);
                try { DrawHookData(); }
                finally { GUILayout.EndVertical(); }

                GUILayout.Space(8f);
                GUILayout.BeginVertical(GUIStylePreset.RoundedPanel);
                try { DrawActions(target); }
                finally { GUILayout.EndVertical(); }

                if (!string.IsNullOrEmpty(_status) && Time.unscaledTime <= _statusExpiresAt)
                    GUILayout.Label(_status);
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

    private static void DrawActions(PlayerControl target)
    {
        var canInteract = PlayerControl.LocalPlayer && !target.AmOwner;
        var previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && canInteract;

        GUILayout.BeginHorizontal();
        try
        {
            if (GUILayout.Button("Teleport", GUIStylePreset.NormalButton, GUILayout.Height(32f)))
            {
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target.GetTruePosition());
                SetStatus($"Teleported to {target.Data.PlayerName}.");
            }
        }
        finally
        {
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        try
        {
            var stickLabel = _followMode == FollowMode.Stick ? "Unstick" : "Stick";
            if (GUILayout.Button(stickLabel, GUIStylePreset.NormalButton, GUILayout.Height(32f)))
            {
                if (_followMode == FollowMode.Stick)
                {
                    StopFollowing("Unstuck.");
                }
                else
                {
                    _followMode = FollowMode.Stick;
                    _nextFollowRpc = 0f;
                    SetStatus($"Sticking to {target.Data.PlayerName}.");
                }
            }

            if (GUILayout.Button("Pet Player", GUIStylePreset.NormalButton, GUILayout.Height(32f)))
            {
                PetPlayer(target);
            }
        }
        finally
        {
            GUILayout.EndHorizontal();
        }

        GUI.enabled = previousEnabled;
        GUILayout.Space(8f);
        GUILayout.Label("Teleport Flooder", GUIStylePreset.TabSubtitle);
        var flooder = Centro.routines?.teleportSpammer;
        if (flooder != null)
        {
            bool selected = flooder.IsDirectTarget(target);
            bool next = GUILayout.Toggle(selected, target.AmOwner
                ? " Teleport Flood Myself"
                : $" Teleport Flood {target.Data.PlayerName}");
            if (next != selected)
                flooder.SetTarget(target, next);
        }
        else
        {
            GUILayout.Label("Teleport Flooder is not ready.");
        }

        GUILayout.Space(8f);
        GUILayout.Label("Jail", GUIStylePreset.TabSubtitle);
        var jail = Centro.routines?.jailPlayer;
        if (jail != null)
        {
            bool jailed = jail.IsTarget(target);
            bool nextJailed = GUILayout.Toggle(jailed, target.AmOwner
                ? " Jail Myself"
                : $" Jail {target.Data.PlayerName}");
            if (nextJailed != jailed)
                jail.SetTarget(target, nextJailed);
        }
        else
        {
            GUILayout.Label("Jail is not ready.");
        }

        GUI.enabled = previousEnabled;
    }

    private static void DrawHookData()
    {
        if (!_dataHooked)
        {
            GUILayout.Label("Click Hook Data to inspect and copy this player's lobby data.");
            return;
        }

        GUILayout.Label("HOOK DATA", GUIStylePreset.TabSubtitle);
        _dataLabelStyle ??= new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            richText = false,
            clipping = TextClipping.Overflow
        };
        foreach (var row in _hookedRows)
        {
            GUILayout.Label($"{row.label}: {DisplayValue(row.value)}", _dataLabelStyle, GUILayout.ExpandWidth(true));
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("Copy All", GUIStylePreset.NormalButton, GUILayout.Height(30f)))
        {
            var all = new StringBuilder();
            foreach (var row in _hookedRows) all.AppendLine($"{row.label}: {DisplayValue(row.value)}");
            GUIUtility.systemCopyBuffer = all.ToString().TrimEnd();
            SetStatus("Copied all player data.");
        }

        foreach (var row in _hookedRows)
        {
            if (GUILayout.Button("Copy " + row.label, GUIStylePreset.NormalButton, GUILayout.Height(26f)))
                CopyToClipboard(row.label, row.value);
        }
    }

    private static List<(string label, string value)> BuildDataRows(PlayerControl target)
    {
        ClientData client = null;
        try
        {
            client = AmongUsClient.Instance.GetClientFromPlayerInfo(target.Data);
        }
        catch { }
        if (client == null)
        {
            try { client = AmongUsClient.Instance.GetClientFromCharacter(target); }
            catch { }
        }
        if (client == null)
        {
            try { client = AmongUsClient.Instance.GetClient(target.Data.ClientId); }
            catch { }
        }

        return new List<(string, string)>
        {
            ("Username", FirstValue(
                SafeValue(() => target.Data.PlayerName),
                SafeValue(() => client.PlayerName),
                SafeValue(() => target.CurrentOutfit.PlayerName))),
            ("PUID", FirstValue(
                SafeValue(() => target.Data.Puid),
                SafeValue(() => target.Puid),
                SafeValue(() => client.ProductUserId))),
            ("Friend Code", FirstValue(
                SafeValue(() => target.Data.FriendCode),
                SafeValue(() => target.FriendCode),
                SafeValue(() => client.FriendCode))),
            ("Player ID", SafeValue(() => target.PlayerId.ToString())),
            ("Client ID", FirstValue(
                SafeValue(() => target.Data.ClientId.ToString()),
                SafeValue(() => client.Id.ToString()))),
            ("Network ID", SafeValue(() => target.NetId.ToString())),
            ("Platform", SafeValue(() => Utils.PlatformTypeToString(client.PlatformData.Platform))),
            ("Level", SafeValue(() => (target.Data.PlayerLevel + 1).ToString())),
            ("Color", SafeValue(() => "#" + ColorUtility.ToHtmlStringRGB(target.Data.Color)))
        };
    }

    private static void PetPlayer(PlayerControl target)
    {
        var local = PlayerControl.LocalPlayer;
        if (!IsUsable(local) || !IsUsable(target)) return;



        var targetPosition = target.GetTruePosition();
        local.MyPhysics.RpcPet(targetPosition, targetPosition);
        SetStatus($"Petting animation aimed at {target.Data.PlayerName}.");
    }

    private static void SelectPlayer(PlayerControl player)
    {
        if (_selectedPlayerId == player.PlayerId) return;
        _selectedPlayerId = player.PlayerId;
        _dataHooked = false;
        _pendingHookPlayerId = null;
        _hookedRows.Clear();
        _followMode = FollowMode.None;
        _status = $"Selected {player.Data.PlayerName}.";
        _statusExpiresAt = Time.unscaledTime + 3f;
    }

    private static void StopFollowing(string message)
    {
        _followMode = FollowMode.None;
        SetStatus(message);
    }

    private static PlayerControl FindPlayer(byte? playerId)
    {
        if (!playerId.HasValue) return null;
        foreach (var player in GetPlayerSnapshot())
            if (IsUsable(player) && player.PlayerId == playerId.Value) return player;
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
                    if (player) snapshot.Add(player);
                }
                catch { }
            }
        }
        catch { }

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

    private static Color GetPlayerColor(PlayerControl player)
    {
        try { return player.Data.Color; }
        catch { return Color.gray; }
    }

    private static string FirstValue(params string[] values)
    {
        foreach (var value in values)
            if (!string.IsNullOrWhiteSpace(value) && value != "Unavailable") return value;
        return "Unavailable";
    }

    private static string SafeValue(Func<string> getter)
    {
        try
        {
            return DisplayValue(getter());
        }
        catch
        {
            return "Unavailable";
        }
    }

    private static string DisplayValue(string value) => string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;

    private static void CopyToClipboard(string label, string value)
    {
        GUIUtility.systemCopyBuffer = DisplayValue(value);
        SetStatus($"Copied {label}.");
    }

    private static void SetStatus(string message)
    {
        _status = message;
        _statusExpiresAt = Time.unscaledTime + 3f;
    }
}
