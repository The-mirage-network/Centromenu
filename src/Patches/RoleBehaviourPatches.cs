using HarmonyLib;
using System.Linq;
using Sentry.Internal.Extensions;
using UnityEngine;
using AmongUs.GameOptions;

namespace Centromenu;

[HarmonyPatch(typeof(EngineerRole), nameof(EngineerRole.FixedUpdate))]
public static class EngineerRole_FixedUpdate
{
    public static void Postfix(EngineerRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            CentromenuCheats.HandleEngineerCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(ShapeshifterRole), nameof(ShapeshifterRole.FixedUpdate))]
public static class ShapeshifterRole_FixedUpdate
{
    public static void Postfix(ShapeshifterRole __instance)
    {
        try
        {
            if(__instance.Player.AmOwner)
            {
                CentromenuCheats.HandleShapeshifterCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(ScientistRole), nameof(ScientistRole.Update))]
public static class ScientistRole_Update
{
    public static void Postfix(ScientistRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            CentromenuCheats.HandleScientistCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FixedUpdate))]
public static class TrackerRole_FixedUpdate
{
    public static void Postfix(TrackerRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            CentromenuCheats.HandleTrackerCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(ActionButton), nameof(ActionButton.SetCoolDown))]
public static class ActionButton_SetCoolDown
{
    public static void Prefix(ActionButton __instance, ref float timer)
    {
        if (!__instance || !PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.Data?.Role == null) return;
        if (CheatToggles.noRoleCooldowns)
        {
            timer = 0f;
            return;
        }

        if (HudManager.Instance == null || __instance != HudManager.Instance.AbilityButton) return;

        var role = PlayerControl.LocalPlayer.Data.Role.Role;
        if ((CheatToggles.noVentCooldown && role == RoleTypes.Engineer) ||
            (CheatToggles.noVitalsCooldown && role == RoleTypes.Scientist) ||
            (CheatToggles.noTrackingCooldown && role == RoleTypes.Tracker) ||
            (CheatToggles.noShapeshiftCooldown && role == RoleTypes.Shapeshifter) ||
            (CheatToggles.noProtectCooldown && role == RoleTypes.GuardianAngel) ||
            (CheatToggles.noPhantomCooldown && role == RoleTypes.Phantom))
        {
            timer = 0f;
        }
    }
}

[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.IsValidTarget))]
public static class PhantomRole_IsValidTarget
{

    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killVanished)
        {
            __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.IsValidTarget))]
public static class ImpostorRole_IsValidTarget
{


    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killAnyone)
        {
           __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.FindClosestTarget))]
public static class ImpostorRole_FindClosestTarget
{

    public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.killReach && !CheatToggles.customKillDistance) return true;

        var playerList = Utils.GetPlayersSortedByDistance().Where(player => !player.IsNull() && __instance.IsValidTarget(player.Data) && player.Collider.enabled).ToList();

        if (CheatToggles.killReach)
        {
            __result = playerList.Count > 0 ? playerList[0] : null;
            return false;
        }

        var maxDistance = 2.5f * Mathf.Clamp(CheatToggles.killDistanceMultiplier, 1f, 10f);
        __result = playerList.FirstOrDefault(player =>
            Vector2.Distance(__instance.Player.GetTruePosition(), player.GetTruePosition()) <= maxDistance);

        return false;
    }
}

[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.FindClosestTarget))]
public static class DetectiveRole_FindClosestTarget
{

    public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.interrogateReach) return true;

        var playerList = Utils.GetPlayersSortedByDistance().Where(player => !player.IsNull() && __instance.IsValidTarget(player.Data) && player.Collider.enabled).ToList();

        __result = playerList[0];

        return false;
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FindClosestTarget))]
public static class TrackerRole_FindClosestTarget
{

    public static bool Prefix(TrackerRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.trackReach) return true;

        var playerList = Utils.GetPlayersSortedByDistance().Where(player => !player.IsNull() && __instance.IsValidTarget(player.Data) && player.Collider.enabled).ToList();

        __result = playerList[0];

        return false;
    }
}
