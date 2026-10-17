using System;
using System.Collections.Generic;
using InnerNet;
using UnityEngine;

namespace Centromenu;

public sealed class VotekickTab : ITab
{
    private const int NoPlayerSelected = int.MinValue;
    // A standard 15-player lobby has at most 14 remote targets. Sending one
    // vote every 150 ms keeps the full batch under two seconds while ensuring
    // every command is handled on a separate update/network-send opportunity.
    private const float VoteSubmitInterval = 0.15f;

    // MenuUI can be recreated when leaving a lobby, so keep this state static
    // until the player has had a chance to rejoin from the main menu.
    private static string _rejoinCode = string.Empty;
    private static bool _rejoinAvailable;

    private int _selectedOwnerId = NoPlayerSelected;
    private Vector2 _tabScrollPosition;
    private Vector2 _historyScrollPosition;
    private Vector2 _playerScrollPosition;
    private bool _hasRefreshedHistory;
    private readonly Queue<int> _pendingVotes = new();
    private int _queuedVoteCount;
    private int _submittedVoteCount;
    private int _skippedVoteCount;
    private float _nextVoteSubmitAt;
    private string _status = "Select a player to vote-kick.";

    public string name => "Votekick";

    public void Draw()
    {
        bool canVote = Utils.isPlayer && AmongUsClient.Instance;

        _tabScrollPosition = GUILayout.BeginScrollView(_tabScrollPosition, GUILayout.ExpandHeight(true));
        DrawServerHistory();

        GUILayout.Space(12f);
        GUILayout.Label("Vote-kick players", GUIStylePreset.TabSubtitle);
        GUILayout.Label("Select one connected player, or submit votes for everyone across two seconds.");

        DrawPlayerPicker(canVote);

        GUILayout.Space(10f);

        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && canVote && _selectedOwnerId != NoPlayerSelected;
        if (GUILayout.Button("Vote Kick Selected", GUIStylePreset.NormalButton, GUILayout.Height(38f)))
        {
            VoteKickSelected();
        }

        GUILayout.BeginHorizontal();
        GUI.enabled = previousEnabled && canVote;
        if (GUILayout.Button("Votekick All Players", GUIStylePreset.NormalButton, GUILayout.Height(38f)))
        {
            VoteKickAll();
        }

        bool isConnected = AmongUsClient.Instance && AmongUsClient.Instance.AmConnected;
        bool canLeaveOrRejoin = isConnected ||
                                (_rejoinAvailable && !string.IsNullOrWhiteSpace(_rejoinCode));
        GUI.enabled = previousEnabled && canLeaveOrRejoin;
        if (GUILayout.Button(isConnected ? "Leave" : "Rejoin", GUIStylePreset.NormalButton, GUILayout.Height(38f)))
        {
            if (isConnected)
                LeaveCurrentServer();
            else
                RejoinLastServer();
        }
        GUILayout.EndHorizontal();

        bool canVoteKickHost = canVote && TryGetHostTarget(out _);
        GUI.enabled = previousEnabled && canVoteKickHost;
        if (GUILayout.Button("Votekick Host", GUIStylePreset.NormalButton, GUILayout.Height(38f)))
        {
            VoteKickHost();
        }

        GUI.enabled = previousEnabled;

        if (!canVote)
        {
            _selectedOwnerId = NoPlayerSelected;
            _status = "Join a lobby or game to use vote-kick.";
        }

        GUILayout.Space(10f);
        GUILayout.Label(_status);
        GUILayout.EndScrollView();
    }

    private void DrawServerHistory()
    {
        IReadOnlyList<LobbyHistoryEntry> entries = LobbyHistory.Entries;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Active server history", GUIStylePreset.TabSubtitle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUIStylePreset.NormalButton, GUILayout.Width(85f)))
            RefreshHistory();

        bool headerEnabled = GUI.enabled;
        GUI.enabled = headerEnabled && entries.Count > 0;
        if (GUILayout.Button("CLEAR HISTORY", GUIStylePreset.NormalButton, GUILayout.Width(135f)))
        {
            LobbyHistory.Clear();
            _historyScrollPosition = Vector2.zero;
            _hasRefreshedHistory = false;
            _status = "Server history cleared.";
        }
        GUI.enabled = headerEnabled;
        GUILayout.EndHorizontal();
        GUILayout.Label("Servers you previously joined that are still active.");

        if (!_hasRefreshedHistory)
        {
            _hasRefreshedHistory = true;
            RefreshHistory();
        }

        int activeCount = 0;
        bool isChecking = false;
        foreach (LobbyHistoryEntry entry in entries)
        {
            activeCount += IsActive(entry) ? 1 : 0;
            isChecking |= entry.Availability is LobbyAvailability.Unknown or LobbyAvailability.Checking;
        }

        if (activeCount == 0)
        {
            GUILayout.Label(isChecking
                ? "Checking your recent servers..."
                : "None of your previously joined servers are active.");
            return;
        }

        _historyScrollPosition = GUILayout.BeginScrollView(
            _historyScrollPosition,
            false,
            true,
            GUILayout.Height(Mathf.Min(190f, activeCount * 88f))
        );

        foreach (LobbyHistoryEntry entry in entries)
        {
            if (!IsActive(entry)) continue;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(entry.Code, GUIStylePreset.TabSubtitle);
            GUILayout.Label($"Owner: {GetOwnerName(entry)}");
            GUILayout.Label($"Age: {FormatServerAge(entry)}   Players: {entry.PlayerCount}/{entry.MaxPlayers}");
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            bool isCurrentLobby = IsCurrentlyJoined(entry.Code);
            bool canJoin = !isCurrentLobby && AmongUsClient.Instance && !AmongUsClient.Instance.AmConnected;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && canJoin;
            if (GUILayout.Button(
                    isCurrentLobby ? "Joined" : "Join",
                    GUIStylePreset.NormalButton,
                    GUILayout.Width(72f),
                    GUILayout.Height(32f)))
            {
                Join(entry);
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        GUILayout.EndScrollView();
    }

    private void DrawPlayerPicker(bool canVote)
    {
        bool foundTarget = false;
        bool selectedTargetStillAvailable = false;

        _playerScrollPosition = GUILayout.BeginScrollView(
            _playerScrollPosition,
            false,
            true,
            GUILayout.Height(190f)
        );

        if (canVote && PlayerControl.AllPlayerControls != null)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (!IsVoteKickTarget(player)) continue;

                foundTarget = true;
                int ownerId = player.OwnerId;
                bool isSelected = ownerId == _selectedOwnerId;
                selectedTargetStillAvailable |= isSelected;

                GUILayout.BeginHorizontal();
                string hostSuffix = player.OwnerId == AmongUsClient.Instance.HostId ? " (Host)" : string.Empty;
                GUILayout.Label(player.Data.PlayerName + hostSuffix, GUILayout.ExpandWidth(true));

                if (GUILayout.Button(
                        isSelected ? "Selected" : "Select",
                        GUIStylePreset.NormalButton,
                        GUILayout.Width(105f),
                        GUILayout.Height(30f)))
                {
                    _selectedOwnerId = ownerId;
                    _status = $"Selected {player.Data.PlayerName}.";
                }

                GUILayout.EndHorizontal();
            }
        }

        if (!foundTarget)
        {
            GUILayout.Label(canVote
                ? "No other connected players were found."
                : "Game access is required to list players.");
        }

        GUILayout.EndScrollView();

        if (_selectedOwnerId != NoPlayerSelected && !selectedTargetStillAvailable)
        {
            _selectedOwnerId = NoPlayerSelected;
        }
    }

    private void VoteKickSelected()
    {
        if (!CanVoteNow()) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!IsVoteKickTarget(player) || player.OwnerId != _selectedOwnerId) continue;

            string playerName = player.Data.PlayerName;
            QueueVotes(new[] { player.OwnerId });
            _status = $"Queued a vote-kick for {playerName}.";
            return;
        }

        _selectedOwnerId = NoPlayerSelected;
        _status = "The selected player is no longer connected.";
    }

    private void VoteKickAll()
    {
        if (!CanVoteNow()) return;

        var targets = new List<int>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (IsVoteKickTarget(player))
            {
                targets.Add(player.OwnerId);
            }
        }

        _selectedOwnerId = NoPlayerSelected;
        QueueVotes(targets);
    }

    private void VoteKickHost()
    {
        if (!CanVoteNow()) return;

        if (!TryGetHostTarget(out var host))
        {
            _status = AmongUsClient.Instance.AmHost
                ? "You are the host, so you cannot vote-kick yourself."
                : "The host is not available as a vote-kick target.";
            return;
        }

        QueueVotes(new[] { host.OwnerId });
        _status = $"Queued a vote-kick for host {host.Data.PlayerName}.";
    }

    public void Tick()
    {
        if (_pendingVotes.Count == 0) return;

        if (!Utils.isPlayer || !AmongUsClient.Instance || !VoteBanSystem.Instance)
        {
            CancelPendingVotes("Vote-kick queue cancelled because the lobby is no longer available.");
            return;
        }

        if (Time.unscaledTime < _nextVoteSubmitAt) return;

        int ownerId = _pendingVotes.Dequeue();
        if (IsConnectedTarget(ownerId) && !VoteBanSystem.Instance.HasMyVote(ownerId))
        {
            SubmitVoteKick(ownerId);
            _submittedVoteCount++;
        }
        else
        {
            _skippedVoteCount++;
        }

        _nextVoteSubmitAt = Time.unscaledTime + VoteSubmitInterval;
        UpdateQueueStatus();
    }

    private bool CanVoteNow()
    {
        if (!Utils.isPlayer || !AmongUsClient.Instance || PlayerControl.AllPlayerControls == null)
        {
            _selectedOwnerId = NoPlayerSelected;
            _status = "Cancelled: join a lobby or game first.";
            return false;
        }

        if (!VoteBanSystem.Instance)
        {
            _status = "The vote-kick system is not available right now.";
            return false;
        }

        return true;
    }

    private void QueueVotes(IEnumerable<int> ownerIds)
    {
        _pendingVotes.Clear();
        var uniqueOwnerIds = new HashSet<int>();
        foreach (int ownerId in ownerIds)
        {
            if (uniqueOwnerIds.Add(ownerId)) _pendingVotes.Enqueue(ownerId);
        }

        _queuedVoteCount = _pendingVotes.Count;
        _submittedVoteCount = 0;
        _skippedVoteCount = 0;
        _nextVoteSubmitAt = 0f;

        if (_queuedVoteCount == 0)
        {
            _status = "No other connected players were found.";
            return;
        }

        _status = _queuedVoteCount == 1
            ? "Queued a vote-kick for 1 player."
            : $"Queued one vote-kick each for {_queuedVoteCount} players.";
    }

    private void UpdateQueueStatus()
    {
        int processed = _submittedVoteCount + _skippedVoteCount;
        if (_pendingVotes.Count > 0)
        {
            _status = $"Submitting vote-kicks: {processed}/{_queuedVoteCount}.";
            return;
        }

        _status = $"Submitted {_submittedVoteCount} vote-kick{(_submittedVoteCount == 1 ? string.Empty : "s")}.";
        if (_skippedVoteCount > 0)
        {
            _status += $" Skipped {_skippedVoteCount} disconnected or already-voted target{(_skippedVoteCount == 1 ? string.Empty : "s")}.";
        }
    }

    private void CancelPendingVotes(string status)
    {
        _pendingVotes.Clear();
        _queuedVoteCount = 0;
        _submittedVoteCount = 0;
        _skippedVoteCount = 0;
        _status = status;
    }

    private static void SubmitVoteKick(int ownerId)
    {
        VoteBanSystem.Instance.CmdAddVote(ownerId);
    }

    private static bool TryGetHostTarget(out PlayerControl host)
    {
        host = null;
        if (!AmongUsClient.Instance || PlayerControl.AllPlayerControls == null) return false;

        int hostId = AmongUsClient.Instance.HostId;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (IsVoteKickTarget(player) && player.OwnerId == hostId)
            {
                host = player;
                return true;
            }
        }

        return false;
    }

    private static bool IsConnectedTarget(int ownerId)
    {
        if (PlayerControl.AllPlayerControls == null) return false;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (IsVoteKickTarget(player) && player.OwnerId == ownerId) return true;
        }

        return false;
    }

    private static bool IsVoteKickTarget(PlayerControl player)
    {
        return player && !player.AmOwner && player.Data != null &&
               !player.Data.Disconnected && !string.IsNullOrWhiteSpace(player.Data.PlayerName);
    }

    private static bool IsActive(LobbyHistoryEntry entry)
    {
        return IsCurrentlyJoined(entry.Code) || entry.Availability == LobbyAvailability.Available;
    }

    private static bool IsCurrentlyJoined(string code)
    {
        return AmongUsClient.Instance && AmongUsClient.Instance.AmConnected &&
               string.Equals(AmongUsClient_OnGameJoined.lastGameIdString, code, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOwnerName(LobbyHistoryEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.OwnerName)) return entry.OwnerName;

        if (IsCurrentlyJoined(entry.Code) && PlayerControl.AllPlayerControls != null)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player && player.Data != null && player.OwnerId == AmongUsClient.Instance.HostId)
                    return player.Data.PlayerName;
            }
        }

        return "Unknown";
    }

    private static string FormatServerAge(LobbyHistoryEntry entry)
    {
        int ageSeconds = Math.Max(0, entry.ServerAgeSeconds);
        if (entry.LastChecked != default)
            ageSeconds += Math.Max(0, (int)(DateTime.Now - entry.LastChecked).TotalSeconds);

        TimeSpan age = TimeSpan.FromSeconds(ageSeconds);
        return age.TotalHours >= 1d
            ? $"{(int)age.TotalHours}h {age.Minutes}m"
            : $"{age.Minutes}m {age.Seconds}s";
    }

    private static void Join(LobbyHistoryEntry entry)
    {
        if (!AmongUsClient.Instance || entry.Availability != LobbyAvailability.Available) return;

        int gameId = GameCode.GameNameToInt(entry.Code);
        AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoFindGameInfoFromCodeAndJoin(gameId));
    }

    private void LeaveCurrentServer()
    {
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmConnected) return;

        string currentCode = AmongUsClient_OnGameJoined.lastGameIdString;
        if (string.IsNullOrWhiteSpace(currentCode))
            currentCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);

        if (string.IsNullOrWhiteSpace(currentCode))
        {
            _status = "Could not save this server for rejoining.";
            return;
        }

        _rejoinCode = currentCode.Trim().ToUpperInvariant();
        _rejoinAvailable = true;
        CancelPendingVotes("Left the server. Click Rejoin to return to it.");
        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
    }

    private void RejoinLastServer()
    {
        if (!AmongUsClient.Instance || AmongUsClient.Instance.AmConnected ||
            !_rejoinAvailable || string.IsNullOrWhiteSpace(_rejoinCode)) return;

        int gameId = GameCode.GameNameToInt(_rejoinCode);
        _status = $"Rejoining {_rejoinCode}...";
        AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoFindGameInfoFromCodeAndJoin(gameId));
    }

    private void RefreshHistory()
    {
        if (!AmongUsClient.Instance) return;
        LobbyLookupQueue.EnqueueAll();
    }
}
