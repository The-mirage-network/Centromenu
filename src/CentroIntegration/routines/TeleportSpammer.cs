using CentroFeatures.modules;
using System.Collections.Generic;
using UnityEngine;

namespace CentroFeatures.routines
{
	public class TeleportSpammer : Routine
	{
		public TeleportSpammer() : base("TeleportSpammer") { }

		public readonly HashSet<int> targets = new HashSet<int>();

		private readonly System.Random rnd = new System.Random();
		private readonly float TELEPORT_DELAY = 0.5f;
		private float timeElapsed = 0f;

		public override void Run()
		{
			if(ShipStatus.Instance == null || ShipStatus.Instance.AllVents.Count == 0) return;

			timeElapsed += Time.deltaTime;
			if(timeElapsed < TELEPORT_DELAY) return;
			timeElapsed = 0f;

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(!IsGlobal && !targets.Contains(player.GetHashCode())) continue;

				int ventId = rnd.Next(0, ShipStatus.Instance.AllVents.Count);

				Teleporter.TeleportToVent(player, ventId);
			}
		}

		public bool IsGlobal
		{
			get { return targets.Count == 1 && targets.Contains(int.MaxValue); }
		}

		public bool IsDirectTarget(PlayerControl player)
		{
			return player != null && targets.Contains(player.GetHashCode());
		}

		public void SetGlobal(bool enabled)
		{
			if(IsGlobal == enabled) return;

			targets.Clear();
			if(enabled) targets.Add(int.MaxValue);
			Enabled = enabled;

			// OnEnable validates that a match is active. Do not show a success
			// notification if that validation disabled the routine again.
			if(enabled)
			{
				if(Enabled && IsGlobal)
					Centro.notifications?.Send("Teleport Flooder", "Teleport flooding everyone!", 5f);
			}
			else
			{
				Centro.notifications?.Send("Teleport Flooder", "Stopped teleport flooding everyone.", 5f);
			}
		}

		public void SetTarget(PlayerControl player, bool enabled)
		{
			if(player == null) return;
			bool wasTargeted = IsDirectTarget(player);
			if(!IsGlobal && wasTargeted == enabled) return;

			// A direct player selection replaces global mode so the selected player,
			// including the local player, becomes the only flood target.
			if(IsGlobal) targets.Clear();

			if(enabled)
				targets.Add(player.GetHashCode());
			else
				targets.Remove(player.GetHashCode());

			Enabled = targets.Count > 0;

			if(enabled)
			{
				if(Enabled && IsDirectTarget(player))
					Centro.notifications?.Send("Teleport Flooder", $"Teleport flooding \"{player.Data.PlayerName}\"!", 5f);
			}
			else if(wasTargeted)
			{
				Centro.notifications?.Send("Teleport Flooder", $"Stopped teleport flooding \"{player.Data.PlayerName}\".", 5f);
			}
		}

		private void OnDisconnect()
		{
			Centro.notifications.Send("Teleport Spammer", "Teleport Spammer was disabled as you left the game.", 10);
			Enabled = false;
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer == null || ShipStatus.Instance == null)
			{
				Centro.notifications.Send("Teleport Spammer", "Teleport Spammer can only be used once the game has started.", 10);
				Enabled = false;
				return;
			}

			EventCoordinator.OnDisconnect += OnDisconnect;
		}

		protected override void OnDisable()
		{
			targets.Clear();

			EventCoordinator.OnDisconnect -= OnDisconnect;
		}
	}
}
