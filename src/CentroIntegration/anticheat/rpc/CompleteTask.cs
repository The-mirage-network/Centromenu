using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class CompleteTask : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			uint taskIndex = reader.ReadPackedUInt32();



			if(ShipStatus.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried completing task {taskIndex} when there was no valid instance of ShipStatus.");
				return false;
			}

			if(RoleManager.IsImpostorRole(player.Data.RoleType))
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried completing task {taskIndex} while being an imposter.");
				return false;
			}


			if(taskIndex + 1 > player.Data.Tasks.Count)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} tried completing task {taskIndex} when they only have {player.Data.Tasks.Count} tasks.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.CompleteTask;
		}
	}
}
