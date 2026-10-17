using Hazel;
using System;

namespace CentroFeatures.anticheat.rpc
{
	internal class SnapTo : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{



			if(LobbyBehaviour.Instance != null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SnapTo RPC while inside the lobby.");


				if(AmongUsClient.Instance.AmHost && !Utilities.IsAnticheatPresent())
				{
					player.NetTransform.RpcSnapTo(player.transform.position);
				}

				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SnapTo;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(CustomNetworkTransform);
		}
	}
}
