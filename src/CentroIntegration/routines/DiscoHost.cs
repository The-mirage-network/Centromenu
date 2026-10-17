using CentroFeatures.modules;
using CentroFeatures.network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CentroFeatures.routines
{
	public class DiscoHostRoutine : Routine
	{
		public DiscoHostRoutine() : base("DiscoHost") { }
		public readonly HashSet<int> targets = new HashSet<int>();

		public float RandomizationDelay { get; set; } = 0.5f;
		private float timeElapsed = 0f;

		private readonly System.Random rnd = new System.Random();

		public override void Run()
		{
			timeElapsed += Time.deltaTime;
			if(timeElapsed < RandomizationDelay) return;
			timeElapsed = 0f;

			List<int> colors = Enumerable.Range(0, 18).ToList();



			int packingLimit = AmongUsClient.Instance.GetMaxMessagePackingLimit();
			BatchedMessage batch = new BatchedMessage();

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(!IsGlobal && !targets.Contains(player.GetHashCode())) continue;


				int color;
				if(colors.Count != 0)
				{
					color = colors[rnd.Next(0, colors.Count)];
					colors.Remove(color);
				}
				else
				{

					color = rnd.Next(0, 18);
				}

				if(batch.msgCount >= packingLimit)
				{
					batch.FinishBatch();
					batch = new BatchedMessage();
				}

				batch.QueueSetColor(player, (byte)color);
			}

			batch.FinishBatch();
		}

		private bool IsGlobal
		{
			get { return targets.Count == 1 && targets.Contains(int.MaxValue); }
		}

		private void OnDisconnect()
		{
			Centro.notifications.Send("Disco Party", "Disco Party was disabled as you left the game.", 10);
			Enabled = false;
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Centro.notifications.Send("Disco Party", "Disco Party can only be used inside of a game.", 10);
				Enabled = false;
				return;
			}

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Disco Party", "Disco Party can only be used if you are the host of the lobby.", 10);
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
