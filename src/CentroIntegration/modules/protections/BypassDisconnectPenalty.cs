using AmongUs.Data.Player;
using HarmonyLib;

namespace CentroFeatures.modules.protections
{
	internal class BypassDisconnectPenalty : Module
	{
		public BypassDisconnectPenalty() : base("BypassDisconnectPenalty")
		{
			base.Enabled = true;
		}

		private static BypassDisconnectPenalty Instance
		{
			get { return ModuleManager.bypassDisconnectPenalty; }
		}




		[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanMinutesLeft), MethodType.Getter)]
		class GetBanMinutes
		{
			static bool Prefix(PlayerBanData __instance, ref int __result)
			{
				if(!Instance.Enabled) return true;

				__instance.banPoints = 0.0f;
				__result = 0;
				return false;
			}
		}
	}
}
