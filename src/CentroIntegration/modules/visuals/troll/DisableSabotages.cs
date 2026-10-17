using HarmonyLib;

namespace CentroFeatures.modules.troll
{
	internal class DisableSabotages : Module
	{
		public DisableSabotages() : base("DisableSabotages") { }

		public readonly float MINIMUM_TIMER_DURATION = 0.1f;

		public readonly byte INVALID_SYSTEM_TYPE = 255;

		private static DisableSabotages Instance
		{
			get { return ModuleManager.disableSabotages; }
		}

		[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
		class BlockSabotagesHost
		{
			static bool Prefix()
			{
				return !Instance.Enabled;
			}
		}







		[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.Deserialize))]
		class BlockSabotagesNonHost
		{
			static void Postfix(SabotageSystemType __instance)
			{
				if(!Instance.Enabled || AmongUsClient.Instance.AmHost || __instance.Timer > Instance.MINIMUM_TIMER_DURATION || MeetingHud.Instance) return;

				Centro.Log.LogMessage($"Sabotage cooldown has depleted to {__instance.Timer}, sending Sabotage system update");
				ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, Instance.INVALID_SYSTEM_TYPE);
			}
		}

		private void OnMeetingEnd()
		{
			Centro.Log.LogMessage($"Meeting has ended, sending Sabotage system update");
			ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, Instance.INVALID_SYSTEM_TYPE);
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnMeetingEnd += OnMeetingEnd;

			if(ShipStatus.Instance != null)
			{
				ISystemType system = ShipStatus.Instance.Systems[SystemTypes.Sabotage];
				SabotageSystemType sabotageSystem = system.Cast<SabotageSystemType>();

				if(sabotageSystem.Timer > MINIMUM_TIMER_DURATION)
				{
					ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, INVALID_SYSTEM_TYPE);
				}
			}
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnMeetingEnd -= OnMeetingEnd;
		}
	}
}
