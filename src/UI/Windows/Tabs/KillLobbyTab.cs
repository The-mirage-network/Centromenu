using HarmonyLib;
using InnerNet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Centromenu
{





    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
    class CanBanPatch
    {
        static bool Prefix(InnerNetClient __instance, ref bool __result)
        {

            __result = true;
            return false;
        }
    }

    public class KillLobbyTab : ITab
    {
        private const int NoPlayerSelected = int.MinValue;

        private int _selectedOwnerId = NoPlayerSelected;
        private Vector2 _playerScrollPosition;
        private bool _kickAllArmed;
        private string _status = "Select a connected player to remove.";

        public string name => "Kick";

        public void Draw()
        {
            if (PersistentVotekickController.TryConsumeTerminalStatus(out string terminalStatus))
                _status = terminalStatus;


            bool canControl = Utils.isPlayer;

            GUILayout.Label("Kick Controls", GUIStylePreset.TabSubtitle);
            GUILayout.Label("In the lobby, it rejoins and vote-kicks until the target is gone. During a game, it uses Centro's kick method.");

            DrawPlayerPicker(canControl);

            GUILayout.Space(12f);


            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && canControl && _selectedOwnerId != NoPlayerSelected;

            if (GUILayout.Button("Kick Selected Player", GUIStylePreset.NormalButton))
            {
                KickSelectedPlayer();
            }

            GUI.enabled = previousEnabled;

            GUILayout.Space(18f);


            GUILayout.Label("Kick Everyone", GUIStylePreset.TabSubtitle);

            GUI.enabled = previousEnabled && canControl;

            _kickAllArmed = GUILayout.Toggle(_kickAllArmed, " Enable Kick All Players");

            GUI.enabled = previousEnabled && canControl && _kickAllArmed;
            if (GUILayout.Button("Kick All Players", GUIStylePreset.NormalButton))
            {
                KickAllPlayers();
            }

            GUI.enabled = previousEnabled;

            if (PersistentVotekickController.IsActive)
            {
                GUILayout.Space(10f);
                GUI.enabled = previousEnabled;
                if (GUILayout.Button("Cancel Rejoin Vote-Kick", GUIStylePreset.NormalButton))
                {
                    PersistentVotekickController.Cancel("Rejoin vote-kick cancelled.");
                }
            }

            if (!canControl)
            {
                _selectedOwnerId = NoPlayerSelected;
                _kickAllArmed = false;
                if (!PersistentVotekickController.IsActive)
                    _status = "You must be in a game to use these controls.";
            }

            GUILayout.Space(12f);
            GUILayout.Label(PersistentVotekickController.IsActive
                ? PersistentVotekickController.Status
                : _status);
        }

        private void DrawPlayerPicker(bool canControl)
        {
            bool foundTarget = false;
            bool selectedTargetStillAvailable = false;

            _playerScrollPosition = GUILayout.BeginScrollView(
                _playerScrollPosition,
                GUILayout.Height(210f)
            );

            if (canControl)
            {

                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (!IsKickTarget(player)) continue;

                        foundTarget = true;
                        bool isSelected = player.OwnerId == _selectedOwnerId;
                        selectedTargetStillAvailable |= isSelected;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(player.Data.PlayerName, GUILayout.ExpandWidth(true));

                        if (GUILayout.Button(isSelected ? "Selected" : "Select", GUILayout.Width(100f)))
                        {
                            _selectedOwnerId = player.OwnerId;



                            selectedTargetStillAvailable = true;
                            _status = $"Selected {player.Data.PlayerName}.";
                        }

                        GUILayout.EndHorizontal();
                    }
                }
            }

            if (!foundTarget)
            {
                GUILayout.Label(canControl
                    ? "No other connected players were found."
                    : "Game access is required to list kick targets.");
            }

            GUILayout.EndScrollView();


            if (_selectedOwnerId != NoPlayerSelected && !selectedTargetStillAvailable)
            {
                _selectedOwnerId = NoPlayerSelected;
            }
        }

        private void KickSelectedPlayer()
        {
            if (!CanControlNow()) return;

            if (PlayerControl.AllPlayerControls == null) return;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (!IsKickTarget(player) || player.OwnerId != _selectedOwnerId) continue;

                string playerName = player.Data.PlayerName;

                if (!Utils.isLobby)
                {
                    CentroFeatures.Utilities.KickPlayer(player);
                    _status = $"Sent Centro kick request for {playerName}.";
                }
                else
                {
                    PersistentVotekickController.Start(new[] { player });
                    _status = PersistentVotekickController.Status;
                }

                _selectedOwnerId = NoPlayerSelected;
                return;
            }

            _selectedOwnerId = NoPlayerSelected;
            _status = "The selected player is no longer connected.";
        }

        private void KickAllPlayers()
        {
            if (!CanControlNow()) return;

            if (PlayerControl.AllPlayerControls == null) return;

            var targets = new List<PlayerControl>();

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (!IsKickTarget(player)) continue;

                targets.Add(player);
                if (!Utils.isLobby)
                    CentroFeatures.Utilities.KickPlayer(player);
            }

            _selectedOwnerId = NoPlayerSelected;
            _kickAllArmed = false;
            if (Utils.isLobby && targets.Count > 0)
                PersistentVotekickController.Start(targets);

            _status = targets.Count == 0
                ? "No other connected players were found."
                : Utils.isLobby
                    ? PersistentVotekickController.Status
                    : $"Sent Centro kick requests for {targets.Count} player{(targets.Count == 1 ? string.Empty : "s")}.";
        }

        private bool CanControlNow()
        {
            if (Utils.isPlayer) return true;

            _selectedOwnerId = NoPlayerSelected;
            _kickAllArmed = false;
            _status = "Cancelled: you must be in a game.";
            return false;
        }

        private static bool IsKickTarget(PlayerControl player)
        {

            return player != null && !player.AmOwner && player.Data != null && !player.Data.Disconnected;
        }
    }







    internal static class PersistentVotekickController
    {
        private const float JoinTimeout = 15f;

        private enum RejoinPhase
        {
            Idle,
            WaitingToJoin,
            Joining,
            Voting,
            WaitingToCycle
        }

        private sealed class TargetIdentity
        {
            public int OwnerId;
            public string PlayerName = string.Empty;
            public string FriendCode = string.Empty;
        }

        private static readonly List<TargetIdentity> Targets = new();
        private static readonly Queue<int> PendingVotes = new();
        private static RejoinPhase _phase;
        private static string _roomCode = string.Empty;
        private static float _joinDeadline;
        private static int _cycle;
        private static int _votesThisCycle;
        private static bool _hasTerminalStatus;

        public static bool IsActive => _phase != RejoinPhase.Idle;
        public static string Status { get; private set; } = "Rejoin vote-kick is idle.";

        public static bool Start(IEnumerable<PlayerControl> players)
        {
            if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmConnected)
            {
                Status = "Could not start: no connected server was found.";
                return false;
            }

            string roomCode = AmongUsClient_OnGameJoined.lastGameIdString;
            if (string.IsNullOrWhiteSpace(roomCode))
                roomCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                Status = "Could not start: the current room code is unavailable.";
                return false;
            }

            Targets.Clear();
            PendingVotes.Clear();
            foreach (PlayerControl player in players)
            {
                if (!IsAvailableTarget(player)) continue;

                var identity = CaptureIdentity(player);
                bool duplicate = Targets.Exists(existing => SameIdentity(existing, identity));
                if (!duplicate) Targets.Add(identity);
            }

            if (Targets.Count == 0)
            {
                Status = "No connected vote-kick targets were found.";
                return false;
            }

            _roomCode = roomCode.Trim().ToUpperInvariant();
            _cycle = 0;
            _votesThisCycle = 0;
            _hasTerminalStatus = false;
            BeginReconnect();
            return true;
        }

        public static void Tick()
        {
            if (!IsActive || !AmongUsClient.Instance) return;

            switch (_phase)
            {
                case RejoinPhase.WaitingToJoin:
                    if (AmongUsClient.Instance.AmConnected)
                    {
                        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                        return;
                    }

                    JoinRoom();
                    break;

                case RejoinPhase.Joining:
                    if (AmongUsClient.Instance.AmConnected && Utils.isPlayer)
                    {
                        PrepareVotes();
                    }
                    else if (Time.unscaledTime >= _joinDeadline)
                    {
                        _phase = RejoinPhase.WaitingToJoin;
                        Status = $"Rejoin timed out; retrying {_roomCode}...";
                    }
                    break;

                case RejoinPhase.Voting:
                    SubmitNextVote();
                    break;

                case RejoinPhase.WaitingToCycle:
                    if (!AnyTargetStillPresent())
                    {
                        Complete();
                        return;
                    }
                    BeginReconnect();
                    break;
            }
        }

        public static void Cancel(string status)
        {
            Targets.Clear();
            PendingVotes.Clear();
            _phase = RejoinPhase.Idle;
            Status = status;
            _hasTerminalStatus = true;
        }

        public static bool TryConsumeTerminalStatus(out string status)
        {
            status = Status;
            if (!_hasTerminalStatus) return false;

            _hasTerminalStatus = false;
            return true;
        }

        private static void BeginReconnect()
        {
            _cycle++;
            _votesThisCycle = 0;
            PendingVotes.Clear();
            Status = $"Rejoin vote-kick cycle {_cycle}: leaving {_roomCode}...";

            if (AmongUsClient.Instance && AmongUsClient.Instance.AmConnected)
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);

            _phase = RejoinPhase.WaitingToJoin;
        }

        private static void JoinRoom()
        {
            int gameId;
            try
            {
                gameId = GameCode.GameNameToInt(_roomCode);
            }
            catch (Exception exception)
            {
                Cancel($"Could not rejoin {_roomCode}: {exception.Message}");
                return;
            }

            Status = $"Rejoin vote-kick cycle {_cycle}: joining {_roomCode}...";
            _phase = RejoinPhase.Joining;
            _joinDeadline = Time.unscaledTime + JoinTimeout;
            AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoFindGameInfoFromCodeAndJoin(gameId));
        }

        private static void PrepareVotes()
        {
            if (!VoteBanSystem.Instance)
            {
                Status = $"Rejoined {_roomCode}; waiting for the vote-kick system...";
                return;
            }

            PendingVotes.Clear();
            foreach (TargetIdentity target in Targets)
            {
                PlayerControl player = FindTarget(target);
                if (player != null) PendingVotes.Enqueue(player.OwnerId);
            }

            if (PendingVotes.Count == 0)
            {
                Complete();
                return;
            }

            _phase = RejoinPhase.Voting;
            Status = $"Cycle {_cycle}: queued {PendingVotes.Count} vote-kick{(PendingVotes.Count == 1 ? string.Empty : "s")}.";
        }

        private static void SubmitNextVote()
        {
            if (!AmongUsClient.Instance.AmConnected)
            {
                RetryAfterDisconnect();
                return;
            }
            if (PendingVotes.Count == 0)
            {
                _phase = RejoinPhase.WaitingToCycle;
                Status = $"Cycle {_cycle}: submitted {_votesThisCycle} vote-kick{(_votesThisCycle == 1 ? string.Empty : "s")}; checking targets...";
                return;
            }

            int ownerId = PendingVotes.Dequeue();
            if (VoteBanSystem.Instance && IsOwnerStillTargeted(ownerId) && !VoteBanSystem.Instance.HasMyVote(ownerId))
            {
                VoteBanSystem.Instance.CmdAddVote(ownerId);
                _votesThisCycle++;
            }
        }

        private static void RetryAfterDisconnect()
        {
            _phase = RejoinPhase.WaitingToJoin;
            Status = $"Disconnected during cycle {_cycle}; rejoining {_roomCode}...";
        }

        private static void Complete()
        {
            int cycles = _cycle;
            Cancel($"Target{(Targets.Count == 1 ? string.Empty : "s")} gone after {cycles} rejoin cycle{(cycles == 1 ? string.Empty : "s")}.");
        }

        private static bool AnyTargetStillPresent()
        {
            foreach (TargetIdentity target in Targets)
            {
                if (FindTarget(target) != null) return true;
            }
            return false;
        }

        private static bool IsOwnerStillTargeted(int ownerId)
        {
            foreach (TargetIdentity target in Targets)
            {
                PlayerControl player = FindTarget(target);
                if (player != null && player.OwnerId == ownerId) return true;
            }
            return false;
        }

        private static TargetIdentity CaptureIdentity(PlayerControl player)
        {
            string friendCode = string.Empty;
            try
            {
                friendCode = AmongUsClient.Instance.GetClient(player.OwnerId)?.FriendCode ?? string.Empty;
            }
            catch { }

            return new TargetIdentity
            {
                OwnerId = player.OwnerId,
                PlayerName = player.Data.PlayerName ?? string.Empty,
                FriendCode = friendCode
            };
        }

        private static PlayerControl FindTarget(TargetIdentity target)
        {
            if (PlayerControl.AllPlayerControls == null) return null;

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!IsAvailableTarget(player)) continue;

                string friendCode = string.Empty;
                try
                {
                    friendCode = AmongUsClient.Instance.GetClient(player.OwnerId)?.FriendCode ?? string.Empty;
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(target.FriendCode))
                {
                    if (string.Equals(target.FriendCode, friendCode, StringComparison.Ordinal)) return player;
                    continue;
                }

                if (player.OwnerId == target.OwnerId &&
                    string.Equals(player.Data.PlayerName, target.PlayerName, StringComparison.Ordinal))
                    return player;
            }

            return null;
        }

        private static bool SameIdentity(TargetIdentity left, TargetIdentity right)
        {
            if (!string.IsNullOrWhiteSpace(left.FriendCode) && !string.IsNullOrWhiteSpace(right.FriendCode))
                return string.Equals(left.FriendCode, right.FriendCode, StringComparison.Ordinal);

            return left.OwnerId == right.OwnerId &&
                   string.Equals(left.PlayerName, right.PlayerName, StringComparison.Ordinal);
        }

        private static bool IsAvailableTarget(PlayerControl player)
        {
            return player && !player.AmOwner && player.Data != null && !player.Data.Disconnected;
        }
    }
}
