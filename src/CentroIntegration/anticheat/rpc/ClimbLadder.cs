using Hazel;
using System;

namespace CentroFeatures.anticheat.rpc
{
	internal class ClimbLadder : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			if(ShipStatus.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to climb a ladder when there is no instance of ShipStatus.");
				return false;
			}

			MapNames map = Utilities.GetCurrentMap();
			if(map != MapNames.Airship && map != MapNames.Fungle)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to climb a ladder outside of the proper map.");
				return false;
			}




			if(player.Data.IsDead)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried to climb a ladder while dead.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.ClimbLadder;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(PlayerPhysics);
		}
	}
}
