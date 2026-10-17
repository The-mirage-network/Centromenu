using AmongUs.InnerNet.GameDataMessages;
using HarmonyLib;
using Hazel;
using CentroFeatures.anticheat.gamedata;
using CentroFeatures.anticheat.rpc;
using System;
using System.Collections.Generic;

namespace CentroFeatures.anticheat
{
	internal class Anticheat
	{
		public static bool Enabled { get; set; } = true;

		public static readonly Dictionary<GameDataTypes, GameDataCheck> GameDataHandlers = new Dictionary<GameDataTypes, GameDataCheck>()
		{
			{ GameDataTypes.SceneChangeFlag, new SceneChange() },
			{ GameDataTypes.ReadyFlag, new ClientReady() }
		};

		public static readonly Dictionary<RpcCalls, RpcCheck> RpcHandlers = new Dictionary<RpcCalls, RpcCheck>()
		{

			{ RpcCalls.PlayAnimation, new PlayAnimation() },
			{ RpcCalls.CompleteTask, new CompleteTask() },
			{ RpcCalls.Exiled, new Exiled() },
			{ RpcCalls.CheckName, new CheckName() },
			{ RpcCalls.SetName, new SetName() },
			{ RpcCalls.SetColor, new SetColor() },
			{ RpcCalls.ReportDeadBody, new ReportDeadBody() },
			{ RpcCalls.SetScanner, new SetScanner() },
			{ RpcCalls.SetStartCounter, new SetStartCounter() },
			{ RpcCalls.EnterVent, new EnterVent() },
			{ RpcCalls.ExitVent, new ExitVent() },
			{ RpcCalls.SnapTo, new SnapTo() },
			{ RpcCalls.AddVote, new AddVote() },
			{ RpcCalls.CloseDoorsOfType, new CloseDoorsOfType() },
			{ RpcCalls.ClimbLadder, new ClimbLadder() },
			{ RpcCalls.UsePlatform, new UsePlatform() },
			{ RpcCalls.UpdateSystem, new UpdateSystem() },
			{ RpcCalls.SetLevel, new SetLevel() }
		};

		public static bool CheckSpoofedPlatforms { get; set; } = true;

		public enum Punishments
		{
			None,
			Kick,
			ErrorKick,
			Ban
		}

		public static float NotificationDuration = 10.0f;

		public static Punishments punishment = Punishments.None;
		public static bool sendNotification = true;
		public static bool discardRpc = true;

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
		class OnPlayerControlRPC
		{
			static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(PlayerControl), __instance, (RpcCalls)callId, reader);
			}
		}

		[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
		class OnPlayerPhysicsRPC
		{
			static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(PlayerPhysics), __instance.myPlayer, (RpcCalls)callId, reader);
			}
		}

		[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
		class OnNetTransformRPC
		{
			static bool Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(CustomNetworkTransform), __instance.myPlayer, (RpcCalls)callId, reader);
			}
		}

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
		class OnShipStatusRPC
		{
			static bool Prefix(byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(ShipStatus), null, (RpcCalls)callId, reader);
			}
		}

		private static bool HandleRpc(Type sourceNetObj, PlayerControl player, RpcCalls rpc, MessageReader reader)
		{
			RpcHandlers.TryGetValue(rpc, out RpcCheck rpcCheck);
			if(!Enabled || rpcCheck == null || !rpcCheck.Enabled) return true;

			if(sourceNetObj != rpcCheck.GetExpectedNetObject())
			{

				return false;
			}


			if(player != null && AmongUsClient.Instance.AmHost && rpcCheck.IsHostOnly())
			{
				Flag(player, $"{player.Data.PlayerName} sent the {rpc} RPC while non-host.");
				return false;
			}

			int oldReadPosition = reader.Position;

			bool isValid = rpcCheck.Validate(player, reader);


			reader.Position = oldReadPosition;

			return isValid || !discardRpc;
		}

		public static bool HandleGameData(GameDataTypes type, MessageReader reader)
		{
			GameDataHandlers.TryGetValue(type, out GameDataCheck gameDataCheck);
			if(!Enabled || gameDataCheck == null || !gameDataCheck.Enabled) return true;

			int oldReadPosition = reader.Position;

			bool isValid = gameDataCheck.Validate(reader);


			reader.Position = oldReadPosition;

			return isValid || !discardRpc;
		}

		public static void Flag(PlayerControl player, string reason, bool shouldPunish = true)
		{



			if(player == PlayerControl.LocalPlayer) return;

			if(sendNotification)
			{
				Centro.notifications.Send("Anticheat", reason, NotificationDuration);
			}

			if(AmongUsClient.Instance.AmHost && shouldPunish)
			{
				Punish(player);
			}
		}


		public static void Flag(string reason)
		{
			if(sendNotification)
			{
				Centro.notifications.Send("Anticheat", reason, NotificationDuration);
			}
		}

		private static void Punish(PlayerControl player)
		{
			switch(punishment)
			{
				case Punishments.None:
					break;

				case Punishments.Kick:
				case Punishments.ErrorKick:
					Centro.Log.LogMessage($"{player.Data.PlayerName} was kicked by Centro Anticheat for hacking");


					if(punishment == Punishments.Kick || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
					{
						AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
					}
					else
					{





						AmongUsClient.Instance.SendLateRejection(player.OwnerId, DisconnectReasons.ClientTimeout);
					}
					break;

				case Punishments.Ban:
					Centro.Log.LogMessage($"{player.Data.PlayerName} was automatically banned by Centro Anticheat for hacking");
					AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
					break;
			}
		}

		public class AnticheatConfigData
		{
			public bool AcEnabled { get; set; }
			public bool SendNotification { get; set; }
			public bool DiscardRpc { get; set; }
			public Punishments Punishment { get; set; }
		}

		public static AnticheatConfigData GetConfigData()
		{
			return new AnticheatConfigData
			{
				AcEnabled = Enabled,
				SendNotification = sendNotification,
				DiscardRpc = discardRpc,
				Punishment = punishment,
			};
		}

		public static void LoadConfigData(AnticheatConfigData configData)
		{
			if(configData == null) return;

			Enabled = configData.AcEnabled;
			sendNotification = configData.SendNotification;
			discardRpc = configData.DiscardRpc;
			punishment = configData.Punishment;
		}
	}
}
