using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class SetStartCounter : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			reader.ReadPackedInt32();
			sbyte counter = reader.ReadSByte();



			if(player.OwnerId != AmongUsClient.Instance.HostId && counter != -1)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent a SetStartCounter RPC with an invalid value: {counter}.");


				if(AmongUsClient.Instance.AmHost)
				{
					PlayerControl.LocalPlayer.RpcSetStartCounter(-1);
				}

				return false;
			}










			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetStartCounter;
		}
	}
}
