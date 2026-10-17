using HarmonyLib;
using UnityEngine;

namespace CentroFeatures.modules.roles
{
	internal class VentAsCrewmate : Module
	{
		public VentAsCrewmate() : base("VentAsCrewmate")
		{
			base.Enabled = true;
		}

		private static VentAsCrewmate Instance
		{
			get { return ModuleManager.ventAsCrewmate; }
		}



		[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
		class SkipVentChecks
		{
			static bool Prefix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
			{
				if(!Instance.Enabled) return true;

				PlayerControl player = pc.Object;
				if(pc.IsDead) return true;

				couldUse = true;
				__result = Vector2.Distance(player.Collider.bounds.center, __instance.transform.position);

				bool isObstructed = PhysicsHelpers.AnythingBetween(player.Collider, player.Collider.bounds.center, __instance.transform.position, Constants.ShipOnlyMask, false);
				if(__result <= __instance.UsableDistance && !isObstructed) canUse = true;

				return false;
			}
		}

		protected override void OnDisable()
		{
			if(PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || RoleManager.IsImpostorRole(PlayerControl.LocalPlayer.Data.RoleType)) return;

			HudManager.Instance.ImpostorVentButton.gameObject.SetActive(false);
			if(Vent.currentVent != null)
			{
				Vent.currentVent.SetButtons(false);
				PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
			}
		}
	}
}
