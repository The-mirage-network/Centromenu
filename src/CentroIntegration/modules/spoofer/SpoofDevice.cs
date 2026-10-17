using HarmonyLib;

namespace CentroFeatures.modules.spoofer
{
	internal class SpoofDevice : Module
	{
		public SpoofDevice() : base("SpoofDevice")
		{
			// This module can be enabled at all times
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

				// Clear metadata from the real platform before populating the selected one.
				__instance.PlatformName = "TESTNAME";
				__instance.XboxPlatformId = 0;
				__instance.PsnPlatformId = 0;

				switch (selectedPlatform)
				{
					case Platforms.StandaloneWin10:
						__instance.XboxPlatformId = 2584878536129841;
						break;

					case Platforms.Xbox:
						// You can find the proper XUID for an Xbox gamertag at https://www.cxkes.me/xbox/xuid
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
						// Other platforms do not send additional platform specific data
						break;
				}
			}
		}
	}
}
