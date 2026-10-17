using UnityEngine;

namespace CentroFeatures.modules.troll
{
	internal class AutoExposeImpostors : Module
	{
		public AutoExposeImpostors() : base("AutoExposeImpostors") { }

		public readonly float MIN_KILL_DISTANCE = 1.0f;
		public readonly float MAX_DISTANCE = 5.0f;

		public bool ExposeOnMurder { get; set; } = true;
		public bool ExposeOnShapeshift { get; set; } = true;


		public bool ExposeOnPhantom { get; set; } = true;

		private void OnPlayerMurder(PlayerControl murderer, PlayerControl target, MurderResultFlags flags)
		{
			if(!ExposeOnMurder || ShipStatus.Instance == null || !flags.HasFlag(MurderResultFlags.Succeeded) || Sabotage.IsSabotageActive(SystemTypes.Electrical) || murderer.shapeshiftTargetPlayerId != -1) return;

			Vent selectedVent = FindClosestVent(murderer, MIN_KILL_DISTANCE, MAX_DISTANCE);
			if(selectedVent == null)
			{
				Centro.Log.LogMessage("Found no applicable vents to teleport player to");
				return;
			}

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == murderer || player == target || player.Data.IsDead || RoleManager.IsImpostorRole(player.Data.RoleType)) continue;

				Teleporter.TeleportToVent(player, selectedVent.Id);
			}
		}

		private void OnPlayerShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shouldAnimate)
		{
			if(!ExposeOnShapeshift || ShipStatus.Instance == null || Sabotage.IsSabotageActive(SystemTypes.Electrical)) return;

			Vent selectedVent = FindClosestVent(shapeshifter, MIN_KILL_DISTANCE, MAX_DISTANCE);
			if(selectedVent == null)
			{
				Centro.Log.LogMessage("Found no applicable vents to teleport player to");
				return;
			}

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == shapeshifter || player.Data.IsDead || RoleManager.IsImpostorRole(player.Data.RoleType)) continue;

				Teleporter.TeleportToVent(player, selectedVent.Id);
			}
		}

		private void OnPlayerPhantom(PlayerControl phantom)
		{
			if(!ExposeOnPhantom || ShipStatus.Instance == null || Sabotage.IsSabotageActive(SystemTypes.Electrical) || phantom.shapeshiftTargetPlayerId != -1) return;

			Vent selectedVent = FindClosestVent(phantom, 0.0f, MAX_DISTANCE);
			if(selectedVent == null)
			{
				Centro.Log.LogMessage("Found no applicable vents to teleport player to");
				return;
			}

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == phantom || player.Data.IsDead || RoleManager.IsImpostorRole(player.Data.RoleType)) continue;

				Teleporter.TeleportToVent(player, selectedVent.Id);
			}
		}

		private Vent FindClosestVent(PlayerControl player, float minDistance, float maxDistance)
		{
			foreach(Vent vent in ShipStatus.Instance.AllVents) {
				float distance = Vector2.Distance(player.transform.position, vent.transform.position);
				Centro.Log.LogMessage($"Vent ID {vent.Id} has a distance of {distance}");



				if(distance < minDistance || distance > maxDistance) continue;



				if(PhysicsHelpers.AnythingBetween(player.Collider, player.Collider.bounds.center, vent.transform.position, Constants.ShipOnlyMask, false)) continue;

				return vent;
			}

			return null;
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnPlayerMurder += OnPlayerMurder;
			EventCoordinator.OnPlayerShapeshift += OnPlayerShapeshift;
			EventCoordinator.OnPlayerPhantom += OnPlayerPhantom;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnPlayerMurder -= OnPlayerMurder;
			EventCoordinator.OnPlayerShapeshift -= OnPlayerShapeshift;
			EventCoordinator.OnPlayerPhantom -= OnPlayerPhantom;
		}
	}
}
