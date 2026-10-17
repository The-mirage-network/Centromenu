using System;
using HarmonyLib;
using UnityEngine;

namespace Centromenu;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class PlayerPhysics_LateUpdate
{
    public static void Postfix(PlayerPhysics __instance)
    {
        CentromenuESP.PlayerNametags(__instance);
        CentromenuESP.SeeGhostsCheat(__instance);

        CentromenuCheats.NoClipCheat();
        if (__instance.AmOwner) CentromenuCheats.AnchorCheat();
        CentromenuCheats.ProtectCheat();
        CentromenuCheats.KillAllCheat();
        CentromenuCheats.KillAllCrewCheat();
        CentromenuCheats.KillAllImpsCheat();
        CentromenuCheats.ForceStartGameCheat();
        CentromenuCheats.TeleportCursorCheat();
        CentromenuCheats.CompleteMyTasksCheat();
        CentromenuCheats.PlayAnimationCheat();
        CentromenuCheats.PlayScannerCheat();


        if (Utils.isLobby || Utils.isFreePlay)
        {

            CentromenuPPMCheats.ClearFakeRoleCache();
        }

        CentromenuPPMCheats.EjectPlayerPPM();
        CentromenuPPMCheats.SpectatePPM();
        CentromenuPPMCheats.KillPlayerPPM();
        CentromenuPPMCheats.TelekillPlayerPPM();
        CentromenuPPMCheats.TeleportPlayerPPM();
        CentromenuPPMCheats.SetFakeRolePPM();
        CentromenuPPMCheats.SetFakeAlivePPM();









        TracersHandler.DrawPlayerTracer(__instance);

        GameObject[] bodyObjects = GameObject.FindGameObjectsWithTag("DeadBody");
        foreach(GameObject bodyObject in bodyObjects)
        {
            DeadBody deadBody = bodyObject.GetComponent<DeadBody>();
            if (!deadBody) continue;

            TracersHandler.DrawBodyTracer(deadBody);

            if (CheatToggles.autoReportBodies)
            {
                if (deadBody.Reported) continue;

                deadBody.Reported = true;

                PlayerControl.LocalPlayer.CmdReportDeadBody(GameData.Instance.GetPlayerById(deadBody.ParentId));
            }
        }

        try
        {
            if (CheatToggles.invertControls)
            {
                PlayerControl.LocalPlayer.MyPhysics.Speed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
            }
            else
            {
                PlayerControl.LocalPlayer.MyPhysics.Speed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
            }
        } catch (NullReferenceException) { }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
public static class PlayerPhysics_HandleAnimation
{

    public static bool Prefix(PlayerPhysics __instance)
    {
        if (CheatToggles.moonWalk && __instance.AmOwner)
        {
            __instance.ResetAnimState();

            return false;
        }

        return true;
    }
}
