using Hazel;
using System;

namespace CentroFeatures.anticheat.rpc
{
	internal class EnterVent : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			if(ShipStatus.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to vent when there is no instance of ShipStatus.");
				return false;
			}




			if(!player.Data.IsDead && !player.Data.Role.CanVent)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to vent when their role ({player.Data.RoleType}) does not support venting.");
				return false;
			}

			if(GameManager.Instance.IsHideAndSeek() && RoleManager.IsImpostorRole(player.Data.RoleType))
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to vent while being the seeker.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.EnterVent;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(PlayerPhysics);
		}
	}
}
