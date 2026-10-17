using HarmonyLib;

namespace CentroFeatures.modules.protections
{
	internal class BlockUnauthorizedUpdates : Module
	{



		public BlockUnauthorizedUpdates() : base("BlockUnauthorizedUpdates")
		{
			base.Enabled = true;
		}

		private static BlockUnauthorizedUpdates Instance
		{
			get { return ModuleManager.blockUnauthorizedUpdates; }
		}

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
		class OnShipStatusRPC
		{
			static bool Prefix(byte callId)
			{
				if(!Instance.Enabled) return true;

				if(!AmongUsClient.Instance.AmHost && (callId == (byte)RpcCalls.UpdateSystem || callId == (byte)RpcCalls.CloseDoorsOfType))
				{
					return false;
				}

				return true;
			}
		}
	}
}
