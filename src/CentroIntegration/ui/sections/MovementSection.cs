using CentroFeatures.modules;
using System.Collections.Generic;
using UnityEngine;

namespace CentroFeatures.ui.sections
{
	internal class MovementSection : Section
	{
		public MovementSection() : base("Movement") { }

		public override void Render()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				GUILayout.Label("You are not currently in a game, these options will not work.");
			}
			else
			{

				Vector2 position = PlayerControl.LocalPlayer.transform.position;

				GUILayout.Label($"Current Map: {Utilities.GetCurrentMap()}\nCurrent Position:\nX: {position.x:F2}\nY: {position.y:F2}");
			}

			Teleporter.UseSnapToRPC = GUILayout.Toggle(Teleporter.UseSnapToRPC, "Use SnapTo RPC For Teleports");
			GUILayout.Label("Teleport To Location:");

			Dictionary<string, Vector2> teleportLocations = Teleporter.GetTeleportLocations();
			Controls.DrawButtonCell(teleportLocations, HandleTeleport, 2);
		}

		private void HandleTeleport(Vector2 location)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Centro.notifications.Send("Teleport", "This option can only be used inside of a game.");
				return;
			}

			Teleporter.TeleportTo(location);
		}
	}
}
