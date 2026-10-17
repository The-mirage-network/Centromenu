using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class Exiled : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{

			Anticheat.Flag(player, $"{player.Data.PlayerName} sent an invalid Exiled RPC.");
			return false;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.Exiled;
		}
	}
}
