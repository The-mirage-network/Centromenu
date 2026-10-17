using System.Runtime.CompilerServices;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Centromenu;
public static class CentromenuCheats
{
    private const float KiraDelaySeconds = 1.4f;
    private static float _nextKiraPulse;
    private static bool _kiraStarted;
    private static bool _kiraSabotagesExecuted;
    private static bool _isScanAnimActive;
    private static bool _isCamsAnimActive;
    private static bool _anchorActive;
    private static bool _anchorSnapPending;
    private static float _nextAutoWeaponsPulse;
    private static byte _anchoredPlayerId;
    private static Vector2 _anchorPosition;
    private static float _anchorSnapAt;

    private const float AnchorSnapDelay = 0.1f;

    public static void SuppressCameraShake()
    {
        if (!CheatToggles.noCameraShake || Camera.main == null) return;

        var followerCamera = Camera.main.GetComponent<FollowerCamera>();
        if (!followerCamera) return;

        followerCamera.shakeAmount = 0f;
        followerCamera.shakePeriod = 0f;
    }

    public static void AutoWeaponsCheat()
    {
        if (!CheatToggles.autoWeapons)
        {
            _nextAutoWeaponsPulse = 0f;
            return;
        }

        if (!Utils.isShip || Time.unscaledTime < _nextAutoWeaponsPulse) return;

        var map = (MapNames)Utils.GetCurrentMapID();
        if (map is not (MapNames.Skeld or MapNames.Dleks or MapNames.Polus)) return;

        Utils.ForcePlayAnimation((byte)TaskTypes.ClearAsteroids);
        _nextAutoWeaponsPulse = Time.unscaledTime + 0.6f;
    }

    public static void CompleteAllTasksAsHost()
    {
        if ((!Utils.isHost && !Utils.isFreePlay) || !Utils.isShip || GameData.Instance == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!player || player.Data == null || player.Data.Disconnected ||
                player.Data.Role == null || player.Data.Role.IsImpostor || player.Data.Tasks == null)
            {
                continue;
            }

            foreach (var task in player.Data.Tasks)
            {
                if (task == null || task.Complete) continue;

                if (player.AmOwner)
                {
                    player.CompleteTask(task.Id);
                }
                else
                {
                    GameData.Instance.CompleteTask(player, task.Id);
                }
            }
        }
    }

    public static void AnchorCheat()
    {
        if (!CheatToggles.anchor)
        {
            _anchorActive = false;
            _anchorSnapPending = false;
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (!localPlayer || localPlayer.Data == null || localPlayer.NetTransform == null)
        {
            _anchorActive = false;
            _anchorSnapPending = false;
            return;
        }

        if (!_anchorActive || _anchoredPlayerId != localPlayer.PlayerId)
        {
            _anchorPosition = localPlayer.GetTruePosition();
            _anchoredPlayerId = localPlayer.PlayerId;
            _anchorActive = true;
            _anchorSnapPending = false;
            return;
        }

        if (Vector2.SqrMagnitude(localPlayer.GetTruePosition() - _anchorPosition) <= 0.0001f)
        {
            _anchorSnapPending = false;
            return;
        }

        if (!_anchorSnapPending)
        {
            _anchorSnapPending = true;
            _anchorSnapAt = Time.unscaledTime + AnchorSnapDelay;
            return;
        }

        if (Time.unscaledTime < _anchorSnapAt) return;

        localPlayer.NetTransform.RpcSnapTo(_anchorPosition);
        _anchorSnapPending = false;
    }

    public static void UnlockReportButtonCheat(HudManager hudManager)
    {
        if (!CheatToggles.unlockReportButton || !hudManager.ReportButton) return;

        // Some modes disable the entire object rather than only its ReportButton state.
        // Force both layers on so it also remains visible in lobbies and normal games.
        hudManager.ReportButton.gameObject.SetActive(true);
        hudManager.ReportButton.SetActive(true);
    }

    public static bool CanReportNearbyBody()
    {
        return TryGetNearbyReportableBody(out _);
    }

    public static void ReportNearbyBody()
    {
        if (!TryGetNearbyReportableBody(out var body)) return;

        var bodyInfo = GameData.Instance?.GetPlayerById(body.ParentId);
        if (bodyInfo == null) return;

        PlayerControl.LocalPlayer.CmdReportDeadBody(bodyInfo);
    }

    private static bool TryGetNearbyReportableBody(out DeadBody nearbyBody)
    {
        nearbyBody = null;

        var localPlayer = PlayerControl.LocalPlayer;
        if (!localPlayer ||
            localPlayer.Data == null ||
            localPlayer.Data.IsDead ||
            !Utils.isInGame ||
            !Utils.isHideNSeek ||
            Utils.isMeeting ||
            ExileController.Instance)
        {
            return false;
        }

        var playerPosition = localPlayer.GetTruePosition();
        var reportRadiusSquared = localPlayer.MaxReportDistance * localPlayer.MaxReportDistance;
        var nearestDistanceSquared = float.MaxValue;

        foreach (var bodyObject in GameObject.FindGameObjectsWithTag("DeadBody"))
        {
            var body = bodyObject.GetComponent<DeadBody>();
            if (!body || body.Reported) continue;

            var distanceSquared = Vector2.SqrMagnitude(body.TruePosition - playerPosition);
            if (distanceSquared > reportRadiusSquared || distanceSquared >= nearestDistanceSquared) continue;

            nearbyBody = body;
            nearestDistanceSquared = distanceSquared;
        }

        return nearbyBody;
    }

    public static void KiraCheat()
    {
        if (!CheatToggles.kira)
        {
            _nextKiraPulse = 0f;
            _kiraStarted = false;
            _kiraSabotagesExecuted = false;
            return;
        }

        var currentTime = Time.realtimeSinceStartup;

        // Schedule the first meeting attempt on the same 1.4-second cadence as every
        // later attempt.
        if (!_kiraStarted)
        {
            _kiraStarted = true;
            _nextKiraPulse = currentTime + KiraDelaySeconds;
            return;
        }

        if (currentTime < _nextKiraPulse) return;

        _nextKiraPulse = currentTime + KiraDelaySeconds;

        KiraMeetingPulse();

        // The first meeting request must be sent before the sabotages. Do this only
        // once per Kira activation; later pulses only request another meeting.
        if (!_kiraSabotagesExecuted)
        {
            ActivateKiraSabotages();
            _kiraSabotagesExecuted = true;
        }
    }

    private static void ActivateKiraSabotages()
    {
        CheatToggles.reactorSab = true;
        CheatToggles.commsSab = true;

        // Oxygen and lights do not exist on every map.
        var mapId = Utils.GetCurrentMapID();
        CheatToggles.oxygenSab = mapId != 2 && mapId != 4 && mapId != 5;
        CheatToggles.elecSab = mapId != 5;
    }

    private static void KiraMeetingPulse()
    {
        // Meetings cannot overlap. Keep the pulse schedule running, but only issue a
        // new request after the current meeting or exile sequence has cleared.
        if (MeetingHud.Instance || ExileController.Instance) return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (!localPlayer || localPlayer.Data == null) return;

        // The host is authorized to start a meeting directly, including during a crisis.
        if (Utils.isHost)
        {
            CallMeetingNow();
            return;
        }

        // A non-host dead player cannot report, and spoofing another reporter causes an
        // unauthorized-RPC kick. While alive, use a real unreported body during crises.
        if (localPlayer.Data.IsDead) return;

        foreach (var bodyObject in GameObject.FindGameObjectsWithTag("DeadBody"))
        {
            var deadBody = bodyObject.GetComponent<DeadBody>();
            if (!deadBody || deadBody.Reported) continue;

            var bodyInfo = GameData.Instance.GetPlayerById(deadBody.ParentId);
            if (bodyInfo == null) continue;

            localPlayer.CmdReportDeadBody(bodyInfo);
            return;
        }

        // Without a body, use the normal emergency request only when no crisis is active.
        if (!Utils.isAnySabotageActive)
        {
            localPlayer.CmdReportDeadBody(null);
        }
    }

    public static void CloseMeetingCheat()
    {
        if (!CheatToggles.closeMeeting) return;

        if (MeetingHud.Instance) // Closes the meeting locally; host privileges are not required.
        {
            var meetingHud = MeetingHud.Instance;

            // Destroy MeetingHud window gameobject
            meetingHud.DespawnOnDestroy = false;
            Object.Destroy(meetingHud.gameObject);

            // Gameplay must be reenabled
            HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false));
            PlayerControl.LocalPlayer.SetKillTimer(GameManager.Instance.LogicOptions.GetKillCooldown());
            if (ShipStatus.Instance)
            {
                ShipStatus.Instance.EmergencyCooldown = GameManager.Instance.LogicOptions.GetEmergencyCooldown();
            }
            if (Camera.main && Camera.main.TryGetComponent<FollowerCamera>(out var followerCamera))
            {
                followerCamera.Locked = false;
            }
            HudManager.Instance.SetMapAndInfoButtonsEnabled(true);
            HudManager.Instance.SetHudActive(true);
            ControllerManager.Instance.CloseAndResetAll();

        }
        else if (ExileController.Instance) // Ends exile cutscene if it's playing
        {
            ExileController.Instance.ReEnableGameplay();
            ExileController.Instance.WrapUp();
        }

        CheatToggles.closeMeeting = false;
    }

    public static void SkipMeetingCheat()
    {
        if (!CheatToggles.skipMeeting) return;

        if (Utils.isMeeting)
        {
            if (Utils.isHost)
            {
                MeetingHud.Instance.RpcVotingComplete(new Il2CppStructArray<MeetingHud.VoterState>(0L), null, true, false, ushort.MinValue);
            }
            else if (PlayerControl.LocalPlayer)
            {
                // Non-hosts use the normal vote command to avoid an unauthorized
                // VotingComplete RPC and the automatic kick that can follow it.
                MeetingHud.Instance.CmdCastVote(PlayerControl.LocalPlayer.PlayerId, PlayerVoteArea.SkippedVote);
            }
        }

        CheatToggles.skipMeeting = false;
    }

    public static void CallMeetingCheat()
    {
        if (!CheatToggles.callMeeting) return;

        CallMeetingNow();
        CheatToggles.callMeeting = false;
    }

    private static void CallMeetingNow()
    {
        if (!PlayerControl.LocalPlayer) return;

        if (Utils.isHost)
        {
            // Same as PlayerControl.ReportDeadBody but without additional checks
            if (MeetingRoomManager.Instance != null)
            {
                MeetingRoomManager.Instance.AssignSelf(PlayerControl.LocalPlayer, null);
            }
            if (HudManager.Instance)
            {
                HudManager.Instance.OpenMeetingRoom(PlayerControl.LocalPlayer);
            }
            PlayerControl.LocalPlayer.RpcStartMeeting(null);
        }
        else
        {
            PlayerControl.LocalPlayer.CmdReportDeadBody(null);
        }
    }

    public static void ForceStartGameCheat()
    {
        if (!CheatToggles.forceStartGame) return;

        if (Utils.isHost && Utils.isLobby)
        {
            AmongUsClient.Instance.SendStartGame();
        }

        CheatToggles.forceStartGame = false;
    }

    public static void CompleteMyTasksCheat()
    {
        if (CheatToggles.completeMyTasks)
        {
            foreach (var task in PlayerControl.LocalPlayer.myTasks)
            {
                PlayerControl.LocalPlayer.RpcCompleteTask(task.Id);
            }

            CheatToggles.completeMyTasks = false;
        }
    }

    public static void OpenSabotageMapCheat()
    {
        if (!CheatToggles.sabotageMap) return;

        HudManager.Instance.ToggleMapVisible(new MapOptions
        {
            Mode = MapOptions.Modes.Sabotage
        });

        CheatToggles.sabotageMap = false;
    }

    public static void HandleEngineerCheats(EngineerRole engineerRole)
    {
        if (CheatToggles.endlessVentTime)
        {
            // Makes vent time incredibly long (float.MaxValue) so that it never ends
            engineerRole.inVentTimeRemaining = float.MaxValue;
        }
        else if (engineerRole.inVentTimeRemaining > engineerRole.GetCooldown())
        {
            // Vent time is reset to normal value after the cheat is disabled
            engineerRole.inVentTimeRemaining = engineerRole.GetCooldown();
        }

        if (CheatToggles.noVentCooldown)
        {
            if (engineerRole.cooldownSecondsRemaining > 0f)
            {
                engineerRole.cooldownSecondsRemaining = 0f;

                HudManager.Instance.AbilityButton.ResetCoolDown();
                HudManager.Instance.AbilityButton.SetCooldownFill(0f);
            }
        }
    }

    public static void HandleShapeshifterCheats(ShapeshifterRole shapeshifterRole)
    {
        if (CheatToggles.endlessSsDuration)
        {
            // Makes shapeshift duration so incredibly long (float.MaxValue) so that it never ends
            shapeshifterRole.durationSecondsRemaining = float.MaxValue;
        }
        else if (shapeshifterRole.durationSecondsRemaining > GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.ShapeshifterDuration))
        {
            // Shapeshift duration is reset to normal value after the cheat is disabled
            shapeshifterRole.durationSecondsRemaining = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.ShapeshifterDuration);

        }
    }

    public static void HandleScientistCheats(ScientistRole scientistRole)
    {
        if (CheatToggles.noVitalsCooldown)
        {
            scientistRole.currentCooldown = 0f;
        }

        if (CheatToggles.endlessBattery)
        {
            // Makes vitals battery so incredibly long (float.MaxValue) so that it never ends
            scientistRole.currentCharge = float.MaxValue;
        }
        else if (scientistRole.currentCharge > scientistRole.RoleCooldownValue)
        {
            // Battery charge is reset to normal value after the cheat is disabled
            scientistRole.currentCharge = scientistRole.RoleCooldownValue;
        }
    }

    public static void HandleTrackerCheats(TrackerRole trackerRole)
    {
        if (CheatToggles.noTrackingCooldown)
        {
            trackerRole.cooldownSecondsRemaining = 0f;
            trackerRole.delaySecondsRemaining = 0f;

            HudManager.Instance.AbilityButton.ResetCoolDown();
            HudManager.Instance.AbilityButton.SetCooldownFill(0f);
        }

        if (CheatToggles.noTrackingDelay && MapBehaviour.Instance != null)
        {
            MapBehaviour.Instance.trackedPointDelayTime = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDelay);
        }

        if (CheatToggles.endlessTracking)
        {
            // Makes vitals battery so incredibly long (float.MaxValue) so that it never ends
            trackerRole.durationSecondsRemaining = float.MaxValue;
        }
        else if (trackerRole.durationSecondsRemaining > GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDuration))
        {
            // Battery charge is reset to normal value after the cheat is disabled
            trackerRole.durationSecondsRemaining = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDuration);
        }
    }

    public static void UseVentCheat(HudManager hudManager)
    {
        // try-catch to prevent errors when role is null
        try
        {

			// Engineers & Impostors don't need this cheat so it is disabled for them
			// Ghost venting causes issues so it is also disabled

			if (!PlayerControl.LocalPlayer.Data.Role.CanVent && !PlayerControl.LocalPlayer.Data.IsDead)
            {
				hudManager.ImpostorVentButton.gameObject.SetActive(CheatToggles.unlockVents);
			}

        } catch { }
    }

    public static void WalkInVentCheat()
    {
        try
        {
            if (!CheatToggles.walkInVents) return;

            PlayerControl.LocalPlayer.inVent = false;
            PlayerControl.LocalPlayer.moveable = true;

        } catch { }
    }

    public static void KickVentsCheat()
    {
        if (!CheatToggles.kickVents) return;

        foreach(var vent in ShipStatus.Instance.AllVents)
        {
            VentilationSystem.Update(VentilationSystem.Operation.BootImpostors, vent.Id);
        }

        CheatToggles.kickVents = false;
    }

    public static void KillAllCheat()
    {
        if (!CheatToggles.killAll) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {
            // Kill all players by sending a successful MurderPlayer RPC call
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
            }
        }

        CheatToggles.killAll = false;
    }

    public static void KillAllCrewCheat()
    {
        if (!CheatToggles.killAllCrew) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {
            // Kill all players by sending a successful MurderPlayer RPC call
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.Role.TeamType == RoleTeamTypes.Crewmate)
                {
                    Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
                }
            }
        }

        CheatToggles.killAllCrew = false;
    }

    public static void KillAllImpsCheat()
    {
        if (!CheatToggles.killAllImps) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {
            // Kill all players by sending a successful MurderPlayer RPC call
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.Role.TeamType == RoleTeamTypes.Impostor)
                {
                    Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
                }
            }
        }

        CheatToggles.killAllImps = false;
    }

    public static void ProtectCheat()
    {
        if (!Utils.isHost || Utils.isLobby) return;

        foreach (var player in ProtectUI.playersToProtect)
        {
            if (player.protectedByGuardianId == -1) // -1 means no protection is currently active
            {
                //PlayerControl.LocalPlayer.TurnOnProtection(true, PlayerControl.LocalPlayer.cosmetics.ColorId, PlayerControl.LocalPlayer.PlayerId);
                PlayerControl.LocalPlayer.RpcProtectPlayer(player, PlayerControl.LocalPlayer.cosmetics.ColorId);
            }
        }
    }

    public static void TeleportCursorCheat()
    {
        if (PlayerControl.LocalPlayer?.NetTransform == null || Camera.main == null) return;
        if (!CheatToggles.teleportCursor) return;

        // Teleport player to cursor's in-world position on right-click
        if (Input.GetMouseButtonDown(1))
        {
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    public static void NoClipCheat()
    {
        try
        {

            PlayerControl.LocalPlayer.Collider.enabled = !(CheatToggles.noClip || PlayerControl.LocalPlayer.onLadder);

        } catch { }
    }

    public static void PlayScannerCheat()
    {
        if (CheatToggles.animMedScan && !_isScanAnimActive)
        {
            Utils.ForceSetScanner(PlayerControl.LocalPlayer, true);
            _isScanAnimActive = true;
        }
        else if (!CheatToggles.animMedScan && _isScanAnimActive)
        {
            Utils.ForceSetScanner(PlayerControl.LocalPlayer, false);
            _isScanAnimActive = false;
        }
    }

    public static void PlayAnimationCheat()
    {
        if (CheatToggles.animPet && Utils.isPlayer && PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.CurrentPet != null)
        {
            // Don't move LocalPlayer, just send the RPC so others see the petting animation
            RpcPetMessage rpcMessage = new(PlayerControl.LocalPlayer.MyPhysics.NetId,
                PlayerControl.LocalPlayer.cosmetics.CurrentPet.PettingPlayerPosition,
                PlayerControl.LocalPlayer.cosmetics.CurrentPet.transform.position);
            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
        }

        byte mapId = Utils.GetCurrentMapID();

        if (mapId == byte.MaxValue) return;

        var map = (MapNames)mapId;

        if (CheatToggles.animShields)
        {
            if (map is MapNames.Skeld or MapNames.Dleks)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.PrimeShields);
            }
            CheatToggles.animShields = false;
        }

        if (CheatToggles.animAsteroids)
        {
            if (map is MapNames.Skeld or MapNames.Dleks or MapNames.Polus)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.ClearAsteroids);
            }
            else
            {
                CheatToggles.animAsteroids = false;
            }
        }

        if (CheatToggles.animEmptyGarbage)
        {
            if (map is MapNames.Skeld or MapNames.Dleks)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.EmptyGarbage);
            }

            CheatToggles.animEmptyGarbage = false;
        }

        if (map is not (MapNames.MiraHQ or MapNames.Fungle))
        {
            if (CheatToggles.animCamsInUse && !_isCamsAnimActive)
            {
                // ShipStatus.Instance.UpdateSystem(SystemTypes.Security, PlayerControl.LocalPlayer, (byte)(CheatToggles.animCamsInUse ? 1 : 0));
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 1);
                _isCamsAnimActive = true;
            }
            else if (!CheatToggles.animCamsInUse && _isCamsAnimActive)
            {
                // Turn off cams if the cheat was used before and is now disabled
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 0);
                _isCamsAnimActive = false;
            }
        }
        else
        {
            CheatToggles.animCamsInUse = false;
        }
    }

    public static void StopShipAnimCheats()
    {
        CheatToggles.animShields = false;
        CheatToggles.animAsteroids = false;
        CheatToggles.animEmptyGarbage = false;
        CheatToggles.animMedScan = false;
        CheatToggles.animCamsInUse = false;

        // This ensures cams and scan animations don't remain marked as active if the player
        // disconnects while the toggles are on (as this may cause unusual RPCs in lobbies)

        _isCamsAnimActive = false;
        _isScanAnimActive = false;
    }
}
