using HarmonyLib;

namespace CentroFeatures.modules.visuals
{
	internal class ShowGhosts : Module
	{
		public ShowGhosts() : base("ShowGhosts")
		{
			base.Enabled = true;
		}

		private static ShowGhosts Instance
		{
			get { return ModuleManager.showGhosts; }
		}




		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Visible), MethodType.Setter)]
		class SetVisible
		{
			static bool Prefix(PlayerControl __instance)
			{
				if(!Instance.Enabled || !__instance.Data.IsDead) return true;

				__instance.cosmetics.Visible = true;
				return false;
			}
		}
	}
}
