using HarmonyLib;

namespace CentroFeatures.modules.spoofer
{
	internal class SpoofDevice : Module
	{
		public SpoofDevice() : base("SpoofDevice")
		{

			base.Enabled = true;
		}

		private static SpoofDevice Instance
		{
			get { return ModuleManager.spoofDevice; }
		}

		public Platforms SpoofedPlatform { get; set; } = Constants.GetPlatformType();

		private static Platforms GetSelectedPlatform()
		{
			return global::Centromenu.Utils.StringToPlatformType(
				global::Centromenu.Centromenu.spoofPlatform.Value,
				out Platforms? configuredPlatform)
				? configuredPlatform.Value
				: Constants.GetPlatformType();
		}

		[HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
		class SerializeDevice
		{
			static void Prefix(PlatformSpecificData __instance)
			{
				Platforms selectedPlatform = GetSelectedPlatform();
				Instance.SpoofedPlatform = selectedPlatform;
				__instance.Platform = selectedPlatform;


				__instance.PlatformName = "TESTNAME";
				__instance.XboxPlatformId = 0;
				__instance.PsnPlatformId = 0;

				switch (selectedPlatform)
				{
					case Platforms.StandaloneWin10:
						__instance.XboxPlatformId = 2584878536129841;
						break;

					case Platforms.Xbox:

						__instance.PlatformName = "Major Nelson";
						__instance.XboxPlatformId = 2584878536129841;
						break;

					case Platforms.Playstation:
						__instance.PlatformName = "Player";
						__instance.PsnPlatformId = 2584878536129841;
						break;

					case Platforms.Switch:
						__instance.PlatformName = "Sus";
						break;

					default:

						break;
				}
			}
		}
	}
}
