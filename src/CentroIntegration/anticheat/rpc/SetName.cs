using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class SetName : RpcCheck
	{

		public readonly int MAX_NAME_LENGTH = 12;

		public override bool Validate(PlayerControl player, MessageReader reader)
		{

			if(!Utilities.IsAnticheatPresent()) return true;

			uint netId = reader.ReadUInt32();
			string requestedName = reader.ReadString();

			if(netId != GetExpectedNetId(player))
			{
				Anticheat.Flag(player, $"SetName RPC sent for {requestedName} includes an invalid net id, received {netId}, expected {GetExpectedNetId(player)}.");
				return false;
			}

			if(requestedName.Length > MAX_NAME_LENGTH)
			{
				Anticheat.Flag(player, $"{requestedName} tried setting their name to something too long ({requestedName.Length}).");
				return false;
			}

			if(requestedName.Contains('<'))
			{
				Anticheat.Flag(player, $"{requestedName} requested a name with invalid characters.");
				return false;
			}

			return true;
		}

		private uint GetExpectedNetId(PlayerControl player)
		{



			return Utilities.IsAnticheatPresent() ? player.NetId : player.Data.NetId;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetName;
		}



		public override bool IsHostOnly()
		{
			return !Utilities.IsAnticheatPresent();
		}
	}
}
