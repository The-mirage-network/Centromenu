using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;

namespace CentroFeatures.modules.host
{
	internal class AssignRoles : Module
	{
		public AssignRoles() : base("AssignRoles") { }

		private static AssignRoles Instance
		{
			get { return ModuleManager.assignRoles; }
		}

		public RoleTypes AssignedRole { get; set; } = RoleTypes.Viper;

		[HarmonyPatch(typeof(LogicRoleSelectionNormal), nameof(LogicRoleSelectionNormal.AssignRolesFromList))]
		class AlwaysImposter
		{

			static void Prefix(ref List<NetworkedPlayerInfo> players, ref List<RoleTypes> roleList, ref int rolesAssigned)
			{
				if(!Instance.Enabled || !AmongUsClient.Instance.AmHost) return;

				RoleTypes assignedRole = Instance.AssignedRole;
				Centro.Log.LogInfo($"Attempting to assign ourselves the {assignedRole} role");


				Il2CppSystem.Predicate<NetworkedPlayerInfo> predicate = (Il2CppSystem.Predicate<NetworkedPlayerInfo>)(player => player == PlayerControl.LocalPlayer.Data);
				int playerIndex = players.FindIndex(predicate);



				if(playerIndex == -1)
				{
					Centro.Log.LogInfo("Our NetworkedPlayerInfo does not exist in this list, skipping");
					return;
				}

				Centro.Log.LogInfo($"Found our NetworkedPlayerInfo in the players list at index {playerIndex}, removing from the list");
				players.RemoveAt(playerIndex);

				Il2CppSystem.Predicate<RoleTypes> predicate2 = (Il2CppSystem.Predicate<RoleTypes>)(roleType => roleType == assignedRole);
				int roleIndex = roleList.FindIndex(predicate2);

				Centro.Log.LogMessage($"Player index is {roleIndex}");



				if(roleIndex != -1)
				{
					Centro.Log.LogInfo($"Found an instance of our role in the roles list at index {roleIndex}, removing from the list");
					roleList.RemoveAt(roleIndex);
				}







				if(RoleManager.IsGhostRole(assignedRole) && players.Count == 0)
				{
					PlayerControl.LocalPlayer.RpcSetRole(RoleManager.IsImpostorRole(assignedRole) ? RoleTypes.Impostor : RoleTypes.Crewmate);
				}

				PlayerControl.LocalPlayer.RpcSetRole(assignedRole);
				rolesAssigned++;

				Centro.Log.LogInfo($"Assigned ourself the {assignedRole} role!");
			}
		}

		private void OnGameStart()
		{
			if(AmongUsClient.Instance.AmHost || Utilities.IsAnticheatPresent()) return;
			Centro.Log.LogMessage($"We are in a host-authoritative lobby, we can hijack the assigned roles");


			if(GameManager.Instance.IsHideAndSeek() && RoleManager.IsImpostorRole(AssignedRole))
			{
				foreach(PlayerControl player in PlayerControl.AllPlayerControls)
				{
					if(player == PlayerControl.LocalPlayer) continue;

					player.RpcSetRole(RoleTypes.Engineer, false);
				}
			}

			PlayerControl.LocalPlayer.RpcSetRole(AssignedRole, false);
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnGameStart += OnGameStart;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnGameStart -= OnGameStart;
		}
	}
}
