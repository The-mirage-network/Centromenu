using HarmonyLib;

namespace CentroFeatures.modules.self
{
	internal class Immortality : Module
	{
















		public Immortality() : base("Immortality") { }

		private static readonly int CUSTOM_VENT_ID = 50;

		private static Immortality Instance
		{
			get { return ModuleManager.immortality; }
		}

		[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
		class BlockSendingUpdates
		{
			static bool Prefix(VentilationSystem.Operation op, int ventId)
			{
				if(Instance.Enabled && ventId != CUSTOM_VENT_ID && (op == VentilationSystem.Operation.Enter || op == VentilationSystem.Operation.Exit || op == VentilationSystem.Operation.Move))
				{



					Centro.Log.LogInfo($"Our client sent VentilationSystem operation {op} for vent {ventId}, cancelling..");
					return false;
				}

				return true;
			}
		}

		private void OnGameLoad()
		{
			Centro.Log.LogMessage($"A new instance of ShipStatus has spawned, sending the immortality RPC");
			VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
		}

		private void OnPlayerMurder(PlayerControl murderer, PlayerControl victim, MurderResultFlags flags)
		{
			if(victim != PlayerControl.LocalPlayer) return;

			Centro.notifications.Send("Immortality", $"{murderer.Data.PlayerName} attempted to kill you!", 5);
		}

		private void OnMeetingEnd()
		{
			if(PlayerControl.LocalPlayer.Data.IsDead) return;

			Centro.Log.LogInfo("Meeting has ended, resending Immortality RPC to retain immortal status");
			VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnGameLoad += OnGameLoad;
			EventCoordinator.OnPlayerMurder += OnPlayerMurder;
			EventCoordinator.OnMeetingEnd += OnMeetingEnd;

			if(PlayerControl.LocalPlayer != null)
			{
				Centro.Log.LogInfo("Immortality was enabled, sending a VentilationSystem update with operation Enter");
				VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
			}
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnGameLoad -= OnGameLoad;
			EventCoordinator.OnPlayerMurder -= OnPlayerMurder;
			EventCoordinator.OnMeetingEnd -= OnMeetingEnd;

			if(PlayerControl.LocalPlayer != null)
			{
				Centro.Log.LogInfo("Immortality was disabled, sending a VentilationSystem update with operation Exit");
				VentilationSystem.Update(VentilationSystem.Operation.Exit, CUSTOM_VENT_ID);
			}
		}
	}
}
