using HarmonyLib;

namespace CentroFeatures.modules.host
{
	internal class DisableMeetings : Module
	{
		public DisableMeetings() : base("DisableMeetings") { }

		private static DisableMeetings Instance
		{
			get { return ModuleManager.disableMeetings; }
		}



		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
		class OnReportBody
		{
			static bool Prefix()
			{
				return !Instance.Enabled;
			}
		}
	}
}
