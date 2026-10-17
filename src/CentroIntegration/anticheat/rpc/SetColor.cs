using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class SetColor : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			uint netId = reader.ReadUInt32();
			byte color = reader.ReadByte();


			if(netId != player.Data.NetId)
			{
				Anticheat.Flag(player, $"SetColor RPC sent for {player.Data.PlayerName} contains invalid net id, expected {player.Data.NetId}, received {netId}", false);
				player.SetColor((byte)CrewmateColor.Red);
				return false;
			}

			if(color >= Palette.ColorNames.Length)
			{
				Anticheat.Flag(player, $"SetColor RPC sent for {player.Data.PlayerName} contains an invalid color: {color}", false);
				player.SetColor((byte)CrewmateColor.Red);
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetColor;
		}

		public override bool IsHostOnly()
		{
			return true;
		}
	}
}
