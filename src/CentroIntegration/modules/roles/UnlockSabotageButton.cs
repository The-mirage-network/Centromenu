using HarmonyLib;

namespace CentroFeatures.modules.roles
{
	internal class UnlockSabotageButton : Module
	{
		public UnlockSabotageButton() : base("UnlockSabotageButton")
		{
			base.Enabled = true;
		}

		private static UnlockSabotageButton Instance
		{
			get { return ModuleManager.unlockSabotageButton; }
		}

		public bool SabotageAsCrewmate { get; set; } = false;
		public bool SabotageInVents { get; set; } = false;



		[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
		class SkipSabotageChecks
		{
			static bool Prefix()
			{
				PlayerControl player = PlayerControl.LocalPlayer;


				if(!Instance.SabotageInVents && player.inVent && !RoleManager.IsImpostorRole(player.Data.RoleType)) return true;

				HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.Sabotage });
				return false;
			}
		}
	}
}
