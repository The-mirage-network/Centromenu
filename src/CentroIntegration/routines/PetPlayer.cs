using Hazel;
using CentroFeatures.modules;
using InnerNet;
using UnityEngine;

namespace CentroFeatures.routines
{

	public class PetPlayerRoutine : Routine
	{
		public PetPlayerRoutine() : base("PetPlayer") { }


		public readonly float PET_DELAY = 0.60f;

		public PlayerControl target;
		private float timeElapsed = 0.0f;

		public override void Run()
		{
			if(PlayerControl.LocalPlayer == null || target == null) return;

			timeElapsed += Time.deltaTime;
			if(timeElapsed < PET_DELAY) return;
			timeElapsed = 0.0f;

			Vector2 petPosition = target.transform.position;
			petPosition.y -= PlayerControl.LocalPlayer.cosmetics.currentPet.yOffset * 2;




			PlayerControl.LocalPlayer.cosmetics.CurrentPet.SetGettingPet(true, petPosition);
			PlayerControl.LocalPlayer.cosmetics.PettingHand.StartPet(PlayerControl.LocalPlayer.cosmetics.currentPet);

			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.MyPhysics.NetId,
				(byte)RpcCalls.Pet,
				SendOption.Reliable,
				-1
			);

			NetHelpers.WriteVector2(PlayerControl.LocalPlayer.GetTruePosition(), writer);
			NetHelpers.WriteVector2(petPosition, writer);

			AmongUsClient.Instance.FinishRpcImmediately(writer);
		}

		private void OnDisconnect()
		{
			Centro.notifications.Send("Pet Player", "Pet Player was disabled as you left the game.", 10);
			Enabled = false;
		}

		private void OnPlayerDisconnect(ClientData client, DisconnectReasons reason)
		{
			if(client.Character != target) return;

			Centro.notifications.Send("Pet Player", "Pet Player was disabled as the player you were petting left the game.");
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
				PlayerControl.LocalPlayer.MyPhysics.RpcCancelPet();
			}

			EventCoordinator.OnDisconnect -= OnDisconnect;
			EventCoordinator.OnPlayerDisconnect -= OnPlayerDisconnect;
		}
	}
}
