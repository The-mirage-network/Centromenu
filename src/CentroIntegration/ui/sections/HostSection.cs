using BepInEx.Unity.IL2CPP.Utils.Collections;
using CentroFeatures.modules;
using CentroFeatures.network;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CentroFeatures.ui.sections
{
	internal class HostSection : Section
	{
		public HostSection() : base("Host") { }

		private byte selectedMap = 0;
		private Controls.PlayerColors selectedColor = 0;

		public static readonly Queue<InnerNetObject> lobbyList = new Queue<InnerNetObject>();
		public static readonly Queue<InnerNetObject> shipList = new Queue<InnerNetObject>();

		public override void Render()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				GUILayout.Label("You are not currently in a game, these options will not work.");
			}
			else if(!AmongUsClient.Instance.AmHost)
			{
				GUILayout.Label("You are not the host of the current lobby. Using these options will either do nothing or get you banned by the anticheat");
			}

			ModuleManager.banMidGame.Enabled = GUILayout.Toggle(ModuleManager.banMidGame.Enabled, "Be able to ban players mid-game");

			ModuleManager.flipSkeld.Enabled = GUILayout.Toggle(ModuleManager.flipSkeld.Enabled, "Use Flipped Skeld Map");

			ModuleManager.disableVentClean.Enabled = GUILayout.Toggle(ModuleManager.disableVentClean.Enabled, "Disable Vent Clean");

			ModuleManager.fakeShapeshiftBubble.Enabled = GUILayout.Toggle(ModuleManager.fakeShapeshiftBubble.Enabled, "Fake Shapeshift Bubble");
			ModuleManager.hostActionRelay.Enabled = GUILayout.Toggle(ModuleManager.hostActionRelay.Enabled, "Allow host-authorized actions from Centro clients");

			GUILayout.BeginHorizontal();
			ModuleManager.blockLowLevels.Enabled = GUILayout.Toggle(ModuleManager.blockLowLevels.Enabled, $"Kick players with less than {ModuleManager.blockLowLevels.MinLevel} levels");
			ModuleManager.blockLowLevels.MinLevel = (uint)GUILayout.HorizontalSlider(ModuleManager.blockLowLevels.MinLevel, 0, 100);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Force Crewmate Victory"))
			{
				// Just in case the user has this enabled
				ModuleManager.disableGameEnd.Enabled = false;

				GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
				Centro.notifications.Send("Game Finished", "You ended the game with a crewmate victory.", 5);
			}

			if(GUILayout.Button("Force Imposter Victory"))
			{
				// Just in case the user has this enabled
				ModuleManager.disableGameEnd.Enabled = false;

				GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
				Centro.notifications.Send("Game Finished", "You ended the game with an imposter victory.", 5);
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Temp Ban All Players"))
			{
				ModuleManager.tempBanAll.Enabled = true;
			}

			GUILayout.Space(5);
			GUILayout.Label("Map Spawner:");

			GUILayout.Label($"Selected map: {(MapNames)selectedMap}");
			selectedMap = (byte)GUILayout.HorizontalSlider(selectedMap, 0, 5);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Despawn Map"))
			{
				DespawnMap();
			}

			if(GUILayout.Button("Spawn Map"))
			{
				AmongUsClient.Instance.StartCoroutine(SpawnMap(selectedMap).WrapToIl2Cpp());
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Despawn Lobby"))
			{
				DespawnLobby();
			}

			if(GUILayout.Button("Spawn Lobby"))
			{
				SpawnLobby();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Assign roles for next round:");
			ModuleManager.assignRoles.Enabled = GUILayout.Toggle(ModuleManager.assignRoles.Enabled, "Enabled");
			GUILayout.Label($"Role to assign: {ModuleManager.assignRoles.AssignedRole}");
			ModuleManager.assignRoles.AssignedRole = Controls.HorizontalRoleSlider(ModuleManager.assignRoles.AssignedRole);

			GUILayout.Space(5);
			GUILayout.Label("Meeting Controls:");
			ModuleManager.disableMeetings.Enabled = GUILayout.Toggle(ModuleManager.disableMeetings.Enabled, "Disable Meetings");
			Centro.routines.reportBodySpam.Enabled = GUILayout.Toggle(Centro.routines.reportBodySpam.Enabled, "Spam Report Bodies");
			Centro.routines.voteSpammer.Enabled = Controls.GlobalPlayerSpecificToggle("Spam Votes", Centro.routines.voteSpammer.targets);

			GUILayout.Space(5);
			GUILayout.Label("Shapeshift Controls:");
			if(GUILayout.Button("Shapeshift Everyone Into Me"))
			{
				AmongUsClient.Instance.StartCoroutine(ShapeshiftAll(PlayerControl.LocalPlayer).WrapToIl2Cpp());
			}

			if(GUILayout.Button("Shapeshift Everyone Into Random"))
			{
				PlayerControl target = Utilities.GetRandomPlayer(false, false, false, false);
				AmongUsClient.Instance.StartCoroutine(ShapeshiftAll(target).WrapToIl2Cpp());
			}

			if(GUILayout.Button("Revert All Shapeshifts"))
			{
				AmongUsClient.Instance.StartCoroutine(RevertAllShapeshift().WrapToIl2Cpp());
			}

			GUILayout.Space(5);
			GUILayout.Label("Color Controls:");

			GUILayout.Label($"Change everyone's color to: {selectedColor}");
			selectedColor = Controls.HorizontalColorSlider(selectedColor);

			if(GUILayout.Button("Change Colors"))
			{
				BatchedMessage batch = new BatchedMessage();

				foreach(PlayerControl player in PlayerControl.AllPlayerControls)
				{
					batch.QueueSetColor(player, (byte)selectedColor);
				}

				batch.FinishBatch();
			}

			Centro.routines.discoHost.Enabled = Controls.GlobalPlayerSpecificToggle("Disco Party", Centro.routines.discoHost.targets);

			GUILayout.Label($"Color randomization delay: {Centro.routines.discoHost.RandomizationDelay:F2}s");
			Centro.routines.discoHost.RandomizationDelay = GUILayout.HorizontalSlider(Centro.routines.discoHost.RandomizationDelay, 0.1f, 2.0f);
		}

		private void ForceStartGame()
		{
			// Local lobbies are the only lobbies where we can start the game without host
			if(AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Start Game", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			// The vanilla anticheat prevents players from sending a ClientReady message more than once
			// If we attempt to start the game twice, then all players will send another ClientReady message, and the entire lobby will be kicked
			if(AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && Utilities.IsAnticheatPresent())
			{
				Centro.notifications.Send("Start Game", "The game has already been started.");
				return;
			}

			AmongUsClient.Instance.StartGame();

			// PlayerControl::RpcSetRole has checks against playing the intro cutscene in Freeplay
			// To avoid a blackscreen in Freeplay, we force the intro cutscene to start
			if(AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
			{
				HudManager.Instance.StartCoroutine(HudManager.Instance.CoShowIntro());
			}
		}

		private static void KillAllPlayers()
		{
			bool hasAnticheat = Utilities.IsAnticheatPresent();

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Centro.notifications.Send("Murder Player", "This feature can only be used once the game has started.");
				return;
			}

			if(hasAnticheat && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Murder Player", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			// On +25 modded protocol lobbies, we are able to send SetColor RPCs as non-host
			// however we are still affected by message packing limits
			int packingLimit = AmongUsClient.Instance.GetMaxMessagePackingLimit();
			BatchedMessage batch = new BatchedMessage();

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(batch.msgCount >= packingLimit)
				{
					batch.FinishBatch();
					batch = new BatchedMessage();
				}

				batch.QueueMurderPlayer(PlayerControl.LocalPlayer, player, MurderResultFlags.Succeeded);
			}

			batch.FinishBatch();
		}

		private static void SpawnLobby()
		{
			Centro.Log.LogInfo($"Attempting to spawn in lobby");

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Spawn Lobby", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			InnerNetObject lobbyPrefab = AmongUsClient.Instance.NonAddressableSpawnableObjects.First((obj) => obj.SpawnId == (uint)SpawnType.LobbyBehavior);
			if(lobbyPrefab == null)
			{
				Centro.Log.LogError($"Failed to find LobbyBehavior prefab in NonAddressableSpawnableObjects");
				return;
			}

			LobbyBehaviour lobby = UnityEngine.Object.Instantiate(lobbyPrefab).Cast<LobbyBehaviour>();

			BatchedMessage batch = new BatchedMessage();
			batch.QueueSpawn(lobby, -2, SpawnFlags.None);
			batch.FinishBatch();

			Centro.notifications.Send("Spawn Lobby", "A new instance of the lobby has been spawned.", 5);
		}

		private static void DespawnLobby()
		{
			Centro.Log.LogInfo($"Attempting to despawn lobby");

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Despawn Lobby", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			if(lobbyList.Count == 0)
			{
				Centro.notifications.Send("Despawn Lobby", "The lobby map has already been despawned.", 10);
				return;
			}

			InnerNetObject lobby = lobbyList.Dequeue();
			lobby.Despawn();

			Centro.notifications.Send("Despawn Lobby", "The lobby map has been despawned.", 5);
		}

		public static IEnumerator SpawnMap(byte mapId)
		{
			Centro.Log.LogInfo($"Attempting to spawn in map id {mapId}");

			// The Utilities::IsAnticheatPresent function does not work perfectly here
			// We are able to spawn in any maps we want without host in local, or even skeld.net lobbies
			// however +25 modded protocol lobbies, while having much of their anticheat checks disabled, still has checks against non-hosts sending spawn messages
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Spawn Map", "This feature can only be used if you are the host of the lobby.");
				yield break;
			}

			AsyncOperationHandle<GameObject> asyncHandle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync(null, false);
			yield return asyncHandle;

			ShipStatus ship = asyncHandle.Result.GetComponent<ShipStatus>();

			BatchedMessage batch = new BatchedMessage();
			batch.QueueSpawn(ship, -2, SpawnFlags.None);
			batch.FinishBatch();

			Centro.notifications.Send("Spawn Map", $"{(MapNames)mapId} has been spawned.", 5);
		}

		private static void DespawnMap()
		{
			Centro.Log.LogInfo($"Attempting to despawn map");

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Despawn Map", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			if(shipList.Count == 0)
			{
				Centro.notifications.Send("Despawn Map", "The current map has already been despawned.", 10);
				return;
			}

			InnerNetObject ship = shipList.Dequeue();
			ship.Despawn();

			Centro.notifications.Send("Despawn Map", "The current map has been despawned.", 5);
		}

		private void CloseMeeting()
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Close Meeting", "This feature can only be used if you are the host of the lobby.");
				return;
			}

			if(MeetingHud.Instance == null)
			{
				Centro.notifications.Send("Close Meeting", "This option can only be during a meeting.");
				return;
			}

			BatchedMessage batch = new BatchedMessage();
			batch.QueueVotingComplete(Array.Empty<MeetingHud.VoterState>(), null, false, false, 0);
			batch.QueueCloseMeeting();
			batch.FinishBatch();
		}

		private static IEnumerator ShapeshiftAll(PlayerControl target)
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				CentroFeatures.modules.host.HostActionRelay.RequestShapeshiftAll(target);
				yield break;
			}

			PlayerControl[] allPlayers = PlayerControl.AllPlayerControls.ToArray();
			foreach(PlayerControl player in allPlayers)
			{
				if(player == target || player.shapeshiftTargetPlayerId == target.PlayerId) continue;

				Utilities.ShapeshiftPlayer(player, target);

				// This function can send up to 42 reliable messages at once, so we need to implement a delay to avoid getting disconnected
				yield return Effects.Wait(0.05f);
			}
		}

		private static IEnumerator RevertAllShapeshift()
		{
			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Shapeshift Player", "You need to be the host of the lobby in order to use this feature.");
				yield break;
			}

			PlayerControl[] allPlayers = PlayerControl.AllPlayerControls.ToArray();
			foreach(PlayerControl player in allPlayers)
			{
				if(player.shapeshiftTargetPlayerId == -1) continue;

				Utilities.ShapeshiftPlayer(player, player);

				yield return Effects.Wait(0.05f);
			}
		}
	}
}
