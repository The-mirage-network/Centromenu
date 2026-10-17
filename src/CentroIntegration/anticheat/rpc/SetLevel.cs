using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class SetLevel : RpcCheck
	{
		public readonly uint MAX_PLAYER_LEVEL = 10000;


		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			uint level = reader.ReadPackedUInt32();



			if(level > MAX_PLAYER_LEVEL)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent SetLevel RPC with a level that is too high ({level}).");
				player.SetLevel(MAX_PLAYER_LEVEL);
				return false;
			}


			if(ShipStatus.Instance)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent SetLevel RPC when the game has already started.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetLevel;
		}
	}
}
