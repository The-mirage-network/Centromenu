using AmongUs.GameOptions;
using CentroFeatures.modules;
using UnityEngine;

namespace CentroFeatures.ui.sections
{
	internal class RolesSection : Section
	{
		public RolesSection() : base("Roles") { }

		public override void Render()
		{
			ModuleManager.unlockSabotageButton.SabotageAsCrewmate = GUILayout.Toggle(ModuleManager.unlockSabotageButton.SabotageAsCrewmate, "Sabotage As Crewmate");
			ModuleManager.unlockSabotageButton.SabotageInVents = GUILayout.Toggle(ModuleManager.unlockSabotageButton.SabotageInVents, "Allow Sabotaging In Vents As Imposter");
			ModuleManager.noSabotageCooldown.Enabled = GUILayout.Toggle(ModuleManager.noSabotageCooldown.Enabled, "No Sabotage Cooldown");

		}

		public static void UpdateRole(RoleTypes role)
		{
			Centro.Log.LogInfo($"Updating role to {role}");

			bool isGhost = RoleManager.IsGhostRole(role);

			// When a player turns into the ghost, the PlayerControl::CoSetRole function hides the report button. This function then calls the RoleManager::SetRole function we call here
			// This means when we are changing between normal or ghost roles, the report button will not properly be added/removed, so we have to reimplement it here
			// We also cannot use PlayerControl::CoSetRole directly as it prevents in-game roles being overriden by non-ghosts ones (we could just patch it and disable overriding, however a blackout occurs when the game starts)
			HudManager.Instance.ReportButton.gameObject.SetActive(!isGhost);

			RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, role);

			if(AmongUsClient.Instance.AmHost)
			{
				Centro.Log.LogInfo("Since we are host, we can send the SetRole RPC to sync the new role to the server");
				PlayerControl.LocalPlayer.RpcSetRole(role, true);
			}
			else if(Utilities.IsAnticheatPresent() && CentroFeatures.modules.host.HostActionRelay.RequestSetRole(role))
			{
				return;
			}

			Centro.notifications.Send("Update Role", $"Your role has been updated to {role}.");
		}
	}
}
