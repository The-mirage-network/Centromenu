using HarmonyLib;

namespace CentroFeatures.modules.protections
{
	internal class AntiCrash : Module
	{






		public AntiCrash() : base("AntiCrash")
		{
			base.Enabled = true;
		}

		private static AntiCrash Instance
		{
			get { return ModuleManager.antiCrash; }
		}

		private bool gameFullyLoaded = false;

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
		class OnCallMeeting
		{
			static bool Prefix(PlayerControl __instance)
			{
				if(!Instance.Enabled || Instance.gameFullyLoaded) return true;

				Centro.notifications.Send("Protections Alert", $"{__instance.Data.PlayerName} attempted to use a lobby crash exploit!");
				return false;
			}
		}

		private void OnGameStart()
		{
			gameFullyLoaded = false;
		}

		private void OnGameLoad()
		{
			gameFullyLoaded = true;
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnGameStart += OnGameStart;
			EventCoordinator.OnGameLoad += OnGameLoad;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnGameStart -= OnGameStart;
			EventCoordinator.OnGameLoad -= OnGameLoad;
		}
	}
}
