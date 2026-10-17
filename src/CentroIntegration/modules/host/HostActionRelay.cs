using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using CentroFeatures.network;
using System.Collections;
using System.Linq;

namespace CentroFeatures.modules.host
{
	// Official servers only accept authoritative gameplay RPCs from the lobby host.
	// This relay lets a Centro host explicitly opt in to performing those actions for
	// other Centro clients without making the requesting client send a host-only RPC.
	internal class HostActionRelay : Module
	{
		private const byte RequestRpc = 252;
		private const byte ResponseRpc = 253;
		private const uint ProtocolMagic = 0x48594452; // "HYDR"
		private const byte ProtocolVersion = 2;

		private enum Action : byte
		{
			Murder = 1,
			SetRole = 2,
			ShapeshiftAll = 3
		}

		private enum Result : byte
		{
			Completed = 0,
			Disabled = 1,
			InvalidTarget = 2,
			GameNotStarted = 3
		}

		public HostActionRelay() : base("HostActionRelay") { }

		public static bool RequestMurder(PlayerControl target)
		{
			if(target == null || PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null)
			{
				return false;
			}

			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.NetId,
				RequestRpc,
				SendOption.Reliable,
				AmongUsClient.Instance.HostId
			);

			writer.Write(ProtocolMagic);
			writer.Write(ProtocolVersion);
			writer.Write((byte)Action.Murder);
			writer.Write(target.PlayerId);
			AmongUsClient.Instance.FinishRpcImmediately(writer);

			Centro.notifications.Send("Host Action", $"Requested the host to kill {target.Data.PlayerName}.", 5);
			return true;
		}

		public static bool RequestSetRole(RoleTypes role)
		{
			if(PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) return false;

			MessageWriter writer = StartRequest(Action.SetRole);
			writer.Write((ushort)role);
			AmongUsClient.Instance.FinishRpcImmediately(writer);

			Centro.notifications.Send("Host Action", $"Requested the host to set your role to {role}.", 5);
			return true;
		}

		public static bool RequestShapeshiftAll(PlayerControl target)
		{
			if(target == null || PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) return false;

			MessageWriter writer = StartRequest(Action.ShapeshiftAll);
			writer.Write(target.PlayerId);
			AmongUsClient.Instance.FinishRpcImmediately(writer);

			Centro.notifications.Send("Host Action", $"Requested the host to shapeshift everyone into {target.Data.PlayerName}.", 5);
			return true;
		}

		private static MessageWriter StartRequest(Action action)
		{
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.NetId,
				RequestRpc,
				SendOption.Reliable,
				AmongUsClient.Instance.HostId
			);

			writer.Write(ProtocolMagic);
			writer.Write(ProtocolVersion);
			writer.Write((byte)action);
			return writer;
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
		private class OnCustomRpc
		{
			static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
			{
				if(callId != RequestRpc && callId != ResponseRpc) return true;

				int oldPosition = reader.Position;
				uint magic = reader.ReadUInt32();
				byte version = reader.ReadByte();

				if(magic != ProtocolMagic || version != ProtocolVersion)
				{
					reader.Position = oldPosition;
					return true;
				}

				if(callId == RequestRpc)
				{
					HandleRequest(__instance, reader);
				}
				else
				{
					HandleResponse(reader);
				}

				return false;
			}
		}

		private static void HandleRequest(PlayerControl requester, MessageReader reader)
		{
			if(!AmongUsClient.Instance.AmHost || requester == null || requester == PlayerControl.LocalPlayer) return;

			Action action = (Action)reader.ReadByte();

			if(!ModuleManager.hostActionRelay.Enabled)
			{
				SendResult(requester, action, Result.Disabled);
				return;
			}

			if(AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
			{
				SendResult(requester, action, Result.GameNotStarted);
				return;
			}

			switch(action)
			{
				case Action.Murder:
					HandleMurderRequest(requester, reader.ReadByte());
					break;
				case Action.SetRole:
					HandleSetRoleRequest(requester, (RoleTypes)reader.ReadUInt16());
					break;
				case Action.ShapeshiftAll:
					HandleShapeshiftAllRequest(requester, reader.ReadByte());
					break;
			}
		}

		private static PlayerControl FindPlayer(byte playerId)
		{
			return PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(player => player.PlayerId == playerId);
		}

		private static void HandleMurderRequest(PlayerControl requester, byte targetId)
		{
			PlayerControl target = FindPlayer(targetId);
			if(target == null || target.Data == null || target.Data.Disconnected)
			{
				SendResult(requester, Action.Murder, Result.InvalidTarget, targetId);
				return;
			}

			Centro.Log.LogInfo($"Host approved murder request from {requester.Data.PlayerName} for {target.Data.PlayerName}");
			requester.RpcMurderPlayer(target, true);
			SendResult(requester, Action.Murder, Result.Completed, targetId);
		}

		private static void HandleSetRoleRequest(PlayerControl requester, RoleTypes role)
		{
			Centro.Log.LogInfo($"Host approved role request from {requester.Data.PlayerName}: {role}");
			requester.RpcSetRole(role, true);
			SendResult(requester, Action.SetRole, Result.Completed, requester.PlayerId);
		}

		private static void HandleShapeshiftAllRequest(PlayerControl requester, byte targetId)
		{
			PlayerControl target = FindPlayer(targetId);
			if(target == null || target.Data == null || target.Data.Disconnected)
			{
				SendResult(requester, Action.ShapeshiftAll, Result.InvalidTarget, targetId);
				return;
			}

			Centro.Log.LogInfo($"Host approved shapeshift-all request from {requester.Data.PlayerName} for {target.Data.PlayerName}");
			AmongUsClient.Instance.StartCoroutine(ShapeshiftAll(requester, target).WrapToIl2Cpp());
		}

		private static IEnumerator ShapeshiftAll(PlayerControl requester, PlayerControl target)
		{
			foreach(PlayerControl player in PlayerControl.AllPlayerControls.ToArray())
			{
				if(player == target || player.shapeshiftTargetPlayerId == target.PlayerId) continue;

				Utilities.ShapeshiftPlayer(player, target);
				yield return Effects.Wait(0.05f);
			}

			SendResult(requester, Action.ShapeshiftAll, Result.Completed, target.PlayerId);
		}

		private static void SendResult(PlayerControl requester, Action action, Result result, byte targetId = byte.MaxValue)
		{
			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.NetId,
				ResponseRpc,
				SendOption.Reliable,
				requester.OwnerId
			);

			writer.Write(ProtocolMagic);
			writer.Write(ProtocolVersion);
			writer.Write((byte)action);
			writer.Write((byte)result);
			writer.Write(targetId);
			AmongUsClient.Instance.FinishRpcImmediately(writer);
		}

		private static void HandleResponse(MessageReader reader)
		{
			Action action = (Action)reader.ReadByte();
			Result result = (Result)reader.ReadByte();
			byte targetId = reader.ReadByte();
			PlayerControl target = FindPlayer(targetId);
			string targetName = target?.Data?.PlayerName ?? $"player {targetId}";

			switch(result)
			{
				case Result.Completed:
					string message = action switch
					{
						Action.Murder => $"The host killed {targetName} for you.",
						Action.SetRole => "The host synchronized your requested role.",
						Action.ShapeshiftAll => $"The host shapeshifted everyone into {targetName}.",
						_ => "The host completed your request."
					};
					Centro.notifications.Send("Host Action", message, 5);
					break;
				case Result.Disabled:
					Centro.notifications.Send("Host Action", "The host has not enabled Centro client requests.", 8);
					break;
				case Result.InvalidTarget:
					Centro.notifications.Send("Host Action", $"The host could not find {targetName}.", 8);
					break;
				case Result.GameNotStarted:
					Centro.notifications.Send("Host Action", "The host rejected the request because the game has not started.", 8);
					break;
			}
		}
	}
}
