using Hazel;
using CentroFeatures.network;

namespace CentroFeatures.modules.troll
{
	internal class DisableCameras : Module
	{


		public DisableCameras() : base("DisableCameras") { }

		private void OnPlayerEnterCameras(PlayerControl player)
		{

			if(player.OwnerId == AmongUsClient.Instance.HostId || player == PlayerControl.LocalPlayer) return;

			Centro.Log.LogMessage($"{player.Data.PlayerName} started to watch cameras, sending Comms system update");

			EnableCommsFor(player);
		}

		private void OnPlayerExitCameras(PlayerControl player)
		{

			if(player.OwnerId == AmongUsClient.Instance.HostId || player == PlayerControl.LocalPlayer) return;


			if(Sabotage.IsSabotageActive(SystemTypes.Comms))
			{
				Centro.Log.LogMessage($"{player.Data.name} updated security cameras, we do not need to do anything as the Comms sabotage is already active");
				return;
			}

			Centro.Log.LogMessage($"{player.Data.PlayerName} stopped watching cameras, sending Comms system update");

			DisableCommsFor(player);
		}




		private void OnCommsRepair()
		{
			Centro.Log.LogMessage($"Comms sabotage was fixed, re-applying comms effect to all players watching security cameras");





			if(AmongUsClient.Instance.AmHost)
			{
				AmongUsClient.Instance.SendAllStreamedObjects();
			}

			ReapplyCommsEffect();
		}

		private void EnableCommsFor(PlayerControl player)
		{
			if(!AmongUsClient.Instance.AmHost)
			{
				Sabotage.SabotageSystem(SystemTypes.Comms, player.OwnerId);
				return;
			}

			BatchedMessage batch = new BatchedMessage(player.OwnerId);

			MessageWriter systemUpdate = MessageWriter.Get(SendOption.Reliable);
			systemUpdate.StartMessage((byte)SystemTypes.Comms);

			systemUpdate.Write(1);
			systemUpdate.EndMessage();

			batch.QueueDataFlag(ShipStatus.Instance.NetId, systemUpdate);

			batch.FinishBatch();
		}

		private void DisableCommsFor(PlayerControl player)
		{
			if(!AmongUsClient.Instance.AmHost)
			{
				Sabotage.FixSabotage(SystemTypes.Comms, player.OwnerId);
				return;
			}

			BatchedMessage batch = new BatchedMessage(player.OwnerId);

			MessageWriter systemUpdate = MessageWriter.Get(SendOption.Reliable);
			systemUpdate.StartMessage((byte)SystemTypes.Comms);

			systemUpdate.Write(0);
			systemUpdate.EndMessage();

			batch.QueueDataFlag(ShipStatus.Instance.NetId, systemUpdate);

			batch.FinishBatch();
		}

		private void ReapplyCommsEffect()
		{
			if(ShipStatus.Instance == null) return;

			ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Security, out ISystemType system);
			if(system == null) return;

			SecurityCameraSystemType securitySystem = system.Cast<SecurityCameraSystemType>();

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player.OwnerId == AmongUsClient.Instance.HostId || player == PlayerControl.LocalPlayer || !securitySystem.PlayersUsing.Contains(player.PlayerId)) continue;

				EnableCommsFor(player);
			}
		}

		protected override void OnEnable()
		{
			ReapplyCommsEffect();

			if(Utilities.GetCurrentMap() == MapNames.Fungle)
			{
				Centro.notifications.Send("Disable Security Cameras", "This feature does not work on The Fungle.", 10);
			}

			EventCoordinator.OnPlayerEnterCameras += OnPlayerEnterCameras;
			EventCoordinator.OnPlayerExitCameras += OnPlayerExitCameras;
			EventCoordinator.OnHudOverrideRepair += OnCommsRepair;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnPlayerEnterCameras -= OnPlayerEnterCameras;
			EventCoordinator.OnPlayerExitCameras -= OnPlayerExitCameras;
			EventCoordinator.OnHudOverrideRepair -= OnCommsRepair;
		}
	}
}
