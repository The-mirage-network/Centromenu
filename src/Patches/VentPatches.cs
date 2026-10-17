using HarmonyLib;
using UnityEngine;

namespace Centromenu;

[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
public static class Vent_CanUse
{

    public static void Postfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (!PlayerControl.LocalPlayer || !PlayerControl.LocalPlayer.Data) return;
        if (PlayerControl.LocalPlayer.Data.Role.CanVent || PlayerControl.LocalPlayer.Data.IsDead) return;
        if (!CheatToggles.unlockVents) return;

        var @object = pc.Object;

        var center = @object.Collider.bounds.center;
        var position = __instance.transform.position;
        var num = Vector2.Distance(center, position);


        canUse = num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(@object.Collider, center, position, Constants.ShipOnlyMask, false);
        couldUse = true;
        __result = num;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
public static class Vent_EnterVent
{


    public static void Postfix(Vent __instance, PlayerControl pc)
    {
        if (!CheatToggles.logVents || !Utils.isShip) return;

        var (realPlayerName, displayPlayerName, isDisguised) = Utils.GetPlayerIdentity(pc);
        var room = Utils.GetRoomFromPosition(__instance.transform.position);
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        ConsoleUI.Log(isDisguised
            ? $"{realPlayerName} (as {displayPlayerName}) entered a vent in {roomName}"
            : $"{realPlayerName} entered a vent in {roomName}");
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
public static class Vent_ExitVent
{


    public static void Postfix(Vent __instance, PlayerControl pc)
    {
        if (!CheatToggles.logVents || !Utils.isShip) return;

        var (realPlayerName, displayPlayerName, isDisguised) = Utils.GetPlayerIdentity(pc);

        var room = Utils.GetRoomFromPosition(__instance.transform.position);
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        ConsoleUI.Log(isDisguised
            ? $"{realPlayerName} (as {displayPlayerName}) exited a vent in {roomName}"
            : $"{realPlayerName} exited a vent in {roomName}");
    }
}
