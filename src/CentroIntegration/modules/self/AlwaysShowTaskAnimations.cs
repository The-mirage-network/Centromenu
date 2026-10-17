using HarmonyLib;
using CentroFeatures.network;

namespace CentroFeatures.modules.self
{
	internal class AlwaysShowTaskAnimations : Module
	{




		public AlwaysShowTaskAnimations() : base("AlwaysShowTaskAnimations") { }

		private static AlwaysShowTaskAnimations Instance
		{
			get { return ModuleManager.alwaysShowTaskAnimations; }
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetScanner))]
		class SetScanner
		{
			static bool Prefix(PlayerControl __instance, bool value)
			{
				if(!Instance.Enabled || __instance != PlayerControl.LocalPlayer) return true;

				BatchedMessage batch = new BatchedMessage();
				batch.QueueSetScanner(__instance, value);
				batch.FinishBatch();
				return false;
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcPlayAnimation))]
		class PlayAnimation
		{
			static bool Prefix(PlayerControl __instance, byte animType)
			{
				if(!Instance.Enabled || __instance != PlayerControl.LocalPlayer) return true;

				RPCEmitter.SendPlayAnimation(animType);
				return false;
			}
		}
	}
}
