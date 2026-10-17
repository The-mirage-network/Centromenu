using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using CentroFeatures.assets;
using CentroFeatures.modules;
using CentroFeatures.network;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CentroFeatures.ui.sections
{
	internal class PlayersSection : Section
	{
		public PlayersSection() : base("Players") { }

		public static Vector2 PlayerPaneSize
		{
			get { return new Vector2(100 * MainUI.scale, MainUI.WindowSize.y - MainUI.HeaderSize.y); }
		}

		public static Vector2 PlayerPanePosition
		{
			get { return new Vector2(MainUI.SectionListPosition.x + MainUI.SectionListSize.x, MainUI.HeaderSize.y + MainUI.HeaderPosition.y); }
		}

		public static Vector2 PlayerButtonSize
		{
			get { return new Vector2(PlayerPaneSize.x, 30 * MainUI.scale); }
		}

		public static Vector2 PlayerOptionsSize
		{
			get { return new Vector2(MainUI.WindowSize.x - MainUI.SectionListSize.x - PlayerPaneSize.x, MainUI.WindowSize.y - MainUI.HeaderSize.y); }
		}

		public static Vector2 PlayerOptionsPosition
		{
			get { return new Vector2(PlayerPanePosition.x + PlayerPaneSize.x, MainUI.HeaderPosition.y + MainUI.HeaderSize.y); }
		}

		public static Vector2 PlayerColorBoxSize
		{
			get { return new Vector2(5 * MainUI.scale, PlayerButtonSize.y); }
		}

		public static PlayerControl selectedPlayer;
		private Vector2 playerScrollVector;
		private Vector2 subsectionScrollVector;

		private Controls.PlayerColors selectedColor = Controls.PlayerColors.Red;
		private int selectedVent = 0;

		public override void HandleSubsectionMove(int offset)
		{
			if(PlayerControl.AllPlayerControls.Count == 0) return;

			int currentPlayer = PlayerControl.AllPlayerControls.IndexOf(selectedPlayer);
			int newPosition = Math.Clamp(currentPlayer + offset, 0, PlayerControl.AllPlayerControls.Count - 1);

			selectedPlayer = PlayerControl.AllPlayerControls[newPosition];
		}

		public override void Render()
		{
			if(PlayerControl.AllPlayerControls.Count == 0)
			{
				GUILayout.Label("There are currently no online players.");
				return;
			}

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.Width(175f));
			GUILayout.Label("Players");
			playerScrollVector = GUILayout.BeginScrollView(playerScrollVector, false, true, GUILayout.Height(410f));
			for(byte i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
			{
				PlayerControl player = PlayerControl.AllPlayerControls[i];
				if(player.Data == null) continue;

				string suffix = player.AmOwner ? " (You)" : "";
				Color oldBackground = GUI.backgroundColor;
				if(player == selectedPlayer) GUI.backgroundColor = player.Data.Color;
				if(GUILayout.Button(player.Data.PlayerName + suffix, GUILayout.Height(34f)))
					selectedPlayer = player;
				GUI.backgroundColor = oldBackground;
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();

			GUILayout.Space(10f);
			GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
			subsectionScrollVector = GUILayout.BeginScrollView(subsectionScrollVector, false, true, GUILayout.Height(410f));
			if(selectedPlayer == null || selectedPlayer.Data == null)
				GUILayout.Label("Select a player to view the extra actions.");
			else
				RenderPlayerControls(selectedPlayer);
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private void RenderPlayerSelection(byte position, PlayerControl player)
		{
			Rect playerInfo = new Rect(0, position * PlayerButtonSize.y, PlayerButtonSize.x, PlayerButtonSize.y);

			string playerName = player.Data.PlayerName;
			playerName += $"\n<color=\"{GetRoleColor(player.Data.RoleType)}\">{player.Data.RoleType}</color>";

			GUIStyle style = player == selectedPlayer ? Styles.PlayerBoxActive : Styles.PlayerBox;

			if(player.OwnerId == AmongUsClient.Instance.HostId)
			{
				style.normal.textColor = new Color(1.0f, 0.84f, 0.0f);
			}

			if(GUI.Button(playerInfo, playerName, style))
			{
				selectedPlayer = player;
			}

			Rect playerColor = new Rect(0, position * PlayerButtonSize.y, PlayerColorBoxSize.x, PlayerColorBoxSize.y);
			Controls.DrawCrewmateColorBox(playerColor, player.Data);
		}

		private string GetRoleColor(RoleTypes role)
		{
			return RoleManager.IsImpostorRole(role) ? "red" : "#8afcfc";
		}

		internal void RenderPlayerControls(PlayerControl target)
		{
			if(target == null || target.Data == null)
			{
				GUILayout.Label("Specified target is not valid.");
				return;
			}

			bool hasAnticheat = Utilities.IsAnticheatPresent();



			string playerInfo =
				$"Name: {target.Data.PlayerName} ({Utilities.GetPlayerColor(target.Data)})" +
				$"\nRole: {target.Data.RoleType}" +
				$"\nState: " + (target.Data.IsDead ? "Dead" : "Alive");

			ClientData clientData = AmongUsClient.Instance.GetClientFromCharacter(target);
			if(clientData != null)
			{
				PlatformSpecificData platform = clientData.PlatformData;

				bool streamerMode = DataManager.Settings.Gameplay.StreamerMode;

				playerInfo +=
					$"\nFriendcode: " + (streamerMode ? "REDACTED" : target.Data.FriendCode) +
					$"\nPUID: " + (streamerMode ? "REDACTED" : target.Data.Puid) +
					$"\nLevel: {target.Data.PlayerLevel + 1}" +
					$"\nDevice: {platform.Platform}" +
					(target.OwnerId == AmongUsClient.Instance.HostId ? "\nHost: true" : "");
			}

			GUILayout.Label(playerInfo);

			Centro.routines.jailPlayer.Enabled = Controls.PlayerSpecificToggle("Place in Jail", target, Centro.routines.jailPlayer.targets);
			Centro.routines.teleportSpammer.Enabled = Controls.PlayerSpecificToggle("Spam Teleports", target, Centro.routines.teleportSpammer.targets);
			Centro.routines.ziplineSpammer.Enabled = Controls.PlayerSpecificToggle("Spam Zipline", target, Centro.routines.ziplineSpammer.targets);
			ModuleManager.whisper.Enabled = Controls.PlayerSpecificToggle("Whisper", target, ref ModuleManager.whisper.target);

			GUILayout.BeginHorizontal();
			if(!hasAnticheat && GUILayout.Button("Teleport to Me"))
			{
				Teleporter.TeleportPlayerTo(target, PlayerControl.LocalPlayer.transform.position);
			}
			GUILayout.EndHorizontal();

			if(!hasAnticheat && GUILayout.Button("Teleport All To"))
			{
				Teleporter.TeleportAllTo(target.transform.position);
			}

			if(GUILayout.Button("Copy Avatar"))
			{
				Utilities.CopyPlayer(target);
			}

			SortedDictionary<int, string> vents = MapAssets.GetVents();

			GUILayout.Label($"Teleport player to vent: {vents.GetValueOrDefault(selectedVent, "N/A")}");
			selectedVent = Controls.HorizontalVentSlider(vents, selectedVent);
			if(GUILayout.Button("Teleport"))
			{
				Teleporter.TeleportToVent(target, selectedVent);
			}

			GUILayout.Space(5);
			GUILayout.Label("Host Only Features:" + (AmongUsClient.Instance.AmHost ? "" : "\n(Using these will get you kicked!)"));

			ModuleManager.autoReportBodies.Enabled = Controls.PlayerSpecificToggle("Auto Report Bodies As", target, ref ModuleManager.autoReportBodies.target);
			Centro.routines.discoHost.Enabled = Controls.PlayerSpecificToggle("Disco Mode", target, Centro.routines.discoHost.targets);
			Centro.routines.voteSpammer.Enabled = Controls.PlayerSpecificToggle("Spam Votes As", target, Centro.routines.voteSpammer.targets);

			if(GUILayout.Button("Force Meeting As"))
			{
				Utilities.AttemptStartMeeting(target, null);
			}

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Force All Votes To"))
			{
				ForceAllVotesTo(target);
			}

			GUILayout.EndHorizontal();

			if(GUILayout.Button("Frame Shapeshift"))
			{
				PlayerControl randomPl = Utilities.GetRandomPlayer(false, false, false, false);
				Utilities.ShapeshiftPlayer(target, randomPl);
			}

			if(GUILayout.Button("Frame for Killing All"))
			{
				target.StartCoroutine(AttemptFrameForKillingAll(target).WrapToIl2Cpp());
			}

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Flood Player with Tasks"))
			{
				byte[] taskIds = new byte[255];

				for(byte i = 0; i < 255; i++)
				{
					taskIds[i] = i;
				}

				target.Data.RpcSetTasks(taskIds);
			}

			if(GUILayout.Button("Clear Tasks"))
			{
				target.Data.RpcSetTasks(Array.Empty<byte>());
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Game Options Modifier:");

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Blind"))
			{
				IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
				gameOptions.SetFloat(FloatOptionNames.CrewLightMod, -1.0f);
				gameOptions.SetFloat(FloatOptionNames.ImpostorLightMod, -1.0f);

				GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
			}

			if(GUILayout.Button("Fullbright"))
			{
				IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
				gameOptions.SetFloat(FloatOptionNames.CrewLightMod, 1000f);
				gameOptions.SetFloat(FloatOptionNames.ImpostorLightMod, 1000f);

				GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Slow Speed"))
			{
				IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
				gameOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.1f);

				GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
			}

			if(GUILayout.Button("Super Speed"))
			{

				float maxSpeed = Utilities.IsAnticheatPresent() ? 3.0f : 5.0f;

				IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
				gameOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, maxSpeed);

				GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
			}
			GUILayout.EndHorizontal();






















			if(GUILayout.Button("Reset to Defaults"))
			{
				IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
				GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
			}

			GUILayout.Space(5);
			GUILayout.Label($"Change color to: {selectedColor}");
			selectedColor = Controls.HorizontalColorSlider(selectedColor);

			if(GUILayout.Button("Set Color"))
			{
				target.RpcSetColor((byte)selectedColor);
			}
		}

		private static void AttemptMurder(PlayerControl target)
		{
			bool hasAnticheat = Utilities.IsAnticheatPresent();

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Centro.notifications.Send("Murder Player", $"You can only kill players once the game has started.");
				return;
			}

			if(AmongUsClient.Instance.AmHost)
			{
				Centro.Log.LogInfo($"Attempting to murder {target.Data.PlayerName}, we are the host so we can use the MurderPlayer RPC");
				PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
				Centro.notifications.Send("Murder Player", $"Killed {target.Data.PlayerName}.", 5);
				return;
			}

			if(!hasAnticheat)
			{
				Centro.Log.LogInfo($"Attempting to murder {target.Data.PlayerName}, we are are in a host-authoritative lobby so we can use the MurderPlayer RPC");
				PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
				Centro.notifications.Send("Murder Player", $"Killed {target.Data.PlayerName}.", 5);
				return;
			}

			if(CentroFeatures.modules.host.HostActionRelay.RequestMurder(target)) return;

			Centro.Log.LogInfo($"Attempting to kill {target.Data.PlayerName}, we are not the host so we have to use the CheckMurder RPC");



			if(!RoleManager.IsImpostorRole(PlayerControl.LocalPlayer.Data.RoleType))
			{
				Centro.notifications.Send("Murder Player", "You can only murder players when you are an Impostor, unless you are the host of the lobby.");
				return;
			}

			if(MeetingHud.Instance != null)
			{
				Centro.notifications.Send("Murder Player", "You can only murder players outside of meetings, unless you are the host of the lobby.");
				return;
			}

			Centro.notifications.Send("Murder Player", $"Attempted to kill {target.Data.PlayerName}.", 5);
			PlayerControl.LocalPlayer.CmdCheckMurder(target);
		}

		private void ForceAllVotesTo(PlayerControl player)
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Force Votes", "You must be the host of the lobby in order to use this feature.");
				return;
			}

			if(MeetingHud.Instance == null)
			{
				Centro.notifications.Send("Force Votes", "This option can only be used when there is an active meeting.");
				return;
			}

			MeetingHud.VoterState[] array = new MeetingHud.VoterState[PlayerControl.AllPlayerControls.Count];

			for(int i = 0; i < array.Length; i++)
			{
				MeetingHud.VoterState state = array[i];

				state.VoterId = (byte)i;
				state.VotedForId = player.PlayerId;

				array[i] = state;
			}

			BatchedMessage batch = new BatchedMessage();
			batch.QueueVotingComplete(array, player.Data, false, false, 0);
			batch.FinishBatch();
		}

		private void EjectPlayer(PlayerControl player)
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Eject Player", "You must be the host of the lobby in order to use this feature.");
				return;
			}

			BatchedMessage batch = new BatchedMessage();

			if(MeetingHud.Instance == null)
			{
				MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(HudManager.Instance.MeetingPrefab);
				batch.QueueSpawn(MeetingHud.Instance, -2, SpawnFlags.None);
			}

			MeetingHud.VoterState[] votes = Array.Empty<MeetingHud.VoterState>();

			batch.QueueVotingComplete(votes, player.Data, false, false, 0);

			batch.QueueCloseMeeting();
			batch.FinishBatch();
		}

		private static IEnumerator AttemptFrameForKillingAll(PlayerControl target)
		{
			Centro.Log.LogInfo($"Attempting to frame {target.Data.PlayerName} for killing all players...");

			bool hasAnticheat = Utilities.IsAnticheatPresent();
			if(hasAnticheat && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Framer", "You must be the host of the lobby in order to use this option.");
				yield break;
			}

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Centro.notifications.Send("Framer", "The game must have started for this option for this option to work.");
				yield break;
			}

			ModuleManager.disableGameEnd.Enabled = true;

			if(target != PlayerControl.LocalPlayer)
			{


				Utilities.ShapeshiftPlayer(PlayerControl.LocalPlayer, target, false);
			}

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == target) continue;

				PlayerControl.LocalPlayer.RpcMurderPlayer(player, true);
			}


			yield return Effects.Wait(3.0f);

			ModuleManager.disableGameEnd.Enabled = false;
			Centro.notifications.Send("Framer", $"Framed {target.Data.PlayerName} for killing all players!");
		}
	}
}
