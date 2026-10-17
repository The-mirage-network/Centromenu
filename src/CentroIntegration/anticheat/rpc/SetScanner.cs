using Hazel;

namespace CentroFeatures.anticheat.rpc
{
	internal class SetScanner : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			bool scanning = reader.ReadBoolean();





			if(ShipStatus.Instance == null && scanning)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC while the map has not spawned in yet.");
				return false;
			}



			if(RoleManager.IsImpostorRole(player.Data.RoleType) && scanning)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC when they are an imposter.");
				return false;
			}

			if(!GameManager.Instance.LogicOptions.GetVisualTasks())
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC while visual tasks were disabled.");
				return false;
			}

			bool hasMedbayScanTask = false;
			foreach(NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
			{
				if(task.Id != (byte)TaskTypes.SubmitScan) continue;

				hasMedbayScanTask = true;
				break;
			}


			if(!hasMedbayScanTask && scanning)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent the SetScanner RPC without being assigned the medbay scan task.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetScanner;
		}
	}
}
