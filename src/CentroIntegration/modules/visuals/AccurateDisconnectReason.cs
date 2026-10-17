using HarmonyLib;

namespace CentroFeatures.modules.visuals
{
	internal class AccurateDisconnectReason : Module
	{
		public AccurateDisconnectReason() : base("AccurateDisconnectReason")
		{
			base.Enabled = true;
		}

		private static AccurateDisconnectReason Instance
		{
			get { return ModuleManager.accurateDisconnectReason; }
		}



		[HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
		class ShowDisconnectNotification
		{
			static bool Prefix(string playerName, DisconnectReasons reason)
			{
				if(!Instance.Enabled) return true;

				switch(reason) {

					case DisconnectReasons.ExitGame:
					case DisconnectReasons.Kicked:
					case DisconnectReasons.Banned:
					case DisconnectReasons.Error:
						return true;

					case DisconnectReasons.Hacking:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was banned by the Among Us anticheat for hacking.");
						return false;

					case DisconnectReasons.DuplicateConnectionDetected:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to duplicate login.");
						return false;


					case DisconnectReasons.ClientTimeout:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to timeout.");
						return false;

					default:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected due to {reason}.");
						return false;
				}
			}
		}
	}
}
