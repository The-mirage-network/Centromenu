using System.Collections.Generic;
using UnityEngine;

namespace CentroFeatures.ui.sections
{
	internal class SabotageSection : Section
	{
		public SabotageSection() : base("Sabotage") { }

		public override void Render()
		{
			if(ShipStatus.Instance == null)
			{
				GUILayout.Label("You are not currently in a game, or the game has not started yet. These options will not work.");
			}

			Sabotage.UpdateSystemsDirectly = GUILayout.Toggle(Sabotage.UpdateSystemsDirectly, "Update Sabotage Systems Directly");

			if(GUILayout.Button("Sabotage All"))
			{
				Sabotage.SabotageAll();
				Centro.notifications.Send("Sabotage", "All sabotages have been enabled.", 5);
			}

			if(GUILayout.Button("Fix All Sabotages"))
			{
				Sabotage.FixAllSabotages();
				Centro.notifications.Send("Sabotage", "All sabotages have been repaired.", 5);
			}
		}

		private void HandleSabotage(SystemTypes system)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Centro.notifications.Send("Sabotage", "This option can only be used inside of a game.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Centro.notifications.Send("Sabotage", "There must be an instance of ShipStatus for this feature to work.");
				return;
			}

			Event currentEvent = Event.current;

			if(currentEvent.button == 0)
			{
				Sabotage.SabotageSystem(system);
				Centro.notifications.Send("Sabotage", $"{system} has been sabotaged.", 5);
			}
			else if(currentEvent.button == 1)
			{
				Sabotage.FixSabotage(system);
				Centro.notifications.Send("Sabotage", $"{system} has been fixed.", 5);
			}
		}

		private void HandleCloseDoor(SystemTypes door)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Centro.notifications.Send("Sabotage", "This option can only be used inside of a game.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Centro.notifications.Send("Sabotage", "There must be an instance of ShipStatus for this feature to work.");
				return;
			}

			Event currentEvent = Event.current;

			if(currentEvent.button == 0)
			{
				Sabotage.LockDoor(door);
				return;
			}

			if(!Sabotage.CanUnlockDoors())
			{
				Centro.notifications.Send("Sabotage", "You can only unlock doors if you are the host or if the map is Polus, Airship, or Fungle.");
				return;
			}

			Sabotage.UnlockDoor(door);
		}
	}
}
