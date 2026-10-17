using CentroFeatures.modules;
using InnerNet;
using UnityEngine;

namespace CentroFeatures.routines
{
	public class PlayerFollowerRoutine : Routine
	{
		public PlayerFollowerRoutine() : base("PlayerFollower") { }

		public PlayerControl target;

		public override void Run()
		{
			if(PlayerControl.LocalPlayer == null) return;











			PlayerControl.LocalPlayer.transform.position = target.transform.position;
		}

		private void OnDisconnect()
		{
			Centro.notifications.Send("Player Follower", "Player Follower was disabled as you left the game.", 10);
			Enabled = false;
		}

		private void OnPlayerDisconnect(ClientData client, DisconnectReasons reason)
		{
			if(client.Character != target) return;

			Centro.notifications.Send("Follow Player", "Follow Player was disabled as the player you were following left the game.");
			Enabled = false;
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				_enabled = false;
				return;
			}

			PlayerControl.LocalPlayer.moveable = false;
			PlayerControl.LocalPlayer.NetTransform.body.velocity = Vector2.zero;

			EventCoordinator.OnDisconnect += OnDisconnect;
			EventCoordinator.OnPlayerDisconnect += OnPlayerDisconnect;
		}

		protected override void OnDisable()
		{
			target = null;

			if(PlayerControl.LocalPlayer != null)
			{
				PlayerControl.LocalPlayer.moveable = true;
			}

			EventCoordinator.OnDisconnect -= OnDisconnect;
			EventCoordinator.OnPlayerDisconnect -= OnPlayerDisconnect;
		}
	}
}
