using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class CheckName : RpcCheck
	{
		public readonly int MAX_NAME_LENGTH = 10;

		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			string requestedName = reader.ReadString();











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

		public override RpcCalls GetId()
		{
			return RpcCalls.CheckName;
		}
	}
}
