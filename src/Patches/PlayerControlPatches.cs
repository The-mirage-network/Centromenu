using HarmonyLib;
using Il2CppSystem;
using UnityEngine;

namespace Centromenu;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
public static class PlayerControl_SetKillTimer
{

    public static void Prefix(PlayerControl __instance, ref float time)
    {
        if (!__instance.AmOwner || !Utils.isHost || !CheatToggles.noKillCd) return;

        time = 0f;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
public static class PlayerControl_CmdCheckMurder
{

    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
























        if (!Utils.isHost) return true;


        PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class PlayerControl_MurderPlayer
{



    public static void Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (!CheatToggles.logDeaths || target == null) return;

        var (realKillerName, displayKillerName, isDisguised) = Utils.GetPlayerIdentity(__instance);
        var targetName = $"<color=#{ColorUtility.ToHtmlStringRGB(target.Data.Color)}>{target.CurrentOutfit.PlayerName}</color>";

        var room = Utils.GetRoomFromPosition(target.GetTruePosition());
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        if (target.protectedByGuardianId != -1)
        {
            ConsoleUI.Log(isDisguised ? $"{realKillerName} (as {displayKillerName}) tried to kill {targetName} in {roomName} (Protected)"
                : $"{realKillerName} tried to kill {targetName} in {roomName} (Protected)");
        }
        else
        {
            ConsoleUI.Log(isDisguised ? $"{realKillerName} (as {displayKillerName}) killed {targetName} in {roomName}"
                : $"{realKillerName} killed {targetName} in {roomName}");
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
public static class PlayerControl_TurnOnProtection
{

    public static void Prefix(ref bool visible)
    {
		if (CheatToggles.seeGhosts)
        {
            visible = true;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckShapeshift))]
public static class PlayerControl_CmdCheckShapeshift
{

    public static void Prefix(ref bool shouldAnimate)
    {
        if (shouldAnimate && CheatToggles.noShapeshiftAnim)
        {
            shouldAnimate = false;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckRevertShapeshift))]
public static class PlayerControl_CmdCheckRevertShapeshift
{

    public static void Prefix(ref bool shouldAnimate){

        if (shouldAnimate && CheatToggles.noShapeshiftAnim)
        {
            shouldAnimate = false;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public static class PlayerControl_Shapeshift
{


    public static void Postfix(PlayerControl __instance, PlayerControl targetPlayer, bool animate)
    {
        if (!CheatToggles.logShapeshifts) return;

        if (__instance.CurrentOutfitType == PlayerOutfitType.MushroomMixup) return;

        var targetPlayerInfo = targetPlayer.Data;

        var room = Utils.GetRoomFromPosition(__instance.GetTruePosition());
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        if (targetPlayerInfo.PlayerId == __instance.Data.PlayerId)
        {
            ConsoleUI.Log($"<color=#{ColorUtility.ToHtmlStringRGB(GameData.Instance.GetPlayerById(__instance.PlayerId).Color)}>" +
                          $"{GameData.Instance.GetPlayerById(__instance.PlayerId)._object.Data.PlayerName}</color> unshapeshifted in {roomName}");
        }
        else
        {
            ConsoleUI.Log($"<color=#{ColorUtility.ToHtmlStringRGB(GameData.Instance.GetPlayerById(__instance.PlayerId).Color)}>" +
                          $"{GameData.Instance.GetPlayerById(__instance.PlayerId)._object.Data.PlayerName}</color> shapeshifted into " +
                          $"<color=#{ColorUtility.ToHtmlStringRGB(GameData.Instance.GetPlayerById(targetPlayerInfo.PlayerId).Color)}>" +
                          $"{GameData.Instance.GetPlayerById(targetPlayerInfo.PlayerId)._object.Data.PlayerName}</color> in {roomName}");
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
public static class PlayerControl_CompleteTask
{
    public static void Postfix(PlayerControl __instance, uint idx)
    {
        if (!CheatToggles.logTasks) return;

        var task = __instance.myTasks.Find((Predicate<PlayerTask>)(p => (int)p.Id == (int)idx));
        var room = Utils.GetRoomFromPosition(__instance.GetTruePosition());
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        if (task)
        {
            ConsoleUI.Log(
                $"<color=#{ColorUtility.ToHtmlStringRGB(__instance.Data.Color)}>{__instance.Data.PlayerName}</color> completed task {task.TaskType} in {roomName}");
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public static class PlayerControl_RpcSyncSettings
{


    public static bool Prefix(PlayerControl __instance, byte[] optionsByteArray)
    {
        return !CheatToggles.noOptionsLimits;
    }
}
