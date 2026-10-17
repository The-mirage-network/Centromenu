using System;
using HarmonyLib;

namespace Centromenu;


























[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.HasServerTimestamp), MethodType.Getter)]
public static class EOSManager_HasServerTimestamp_Getter
{

    public static void Postfix(ref bool __result)
    {
        if (!CheatToggles.spoofAprilFoolsDate) return;

        __result = true;
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.ApproximateServerTime), MethodType.Getter)]
public static class EOSManager_ApproximateServerTime_Getter
{

    public static void Postfix(ref Il2CppSystem.DateTime __result)
    {
        if (!CheatToggles.spoofAprilFoolsDate) return;

        var managedDate = new DateTime(DateTime.UtcNow.Year, 4, 1, 7, 1, 0, DateTimeKind.Utc);
        __result = new Il2CppSystem.DateTime(managedDate.Ticks);
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFreechatAllowed))]
public static class EOSManager_IsFreechatAllowed
{

    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.unlockFeatures)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFriendsListAllowed))]
public static class EOSManager_IsFriendsListAllowed
{

    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.unlockFeatures)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
public static class EOSManager_IsAllowedOnline
{

    public static void Prefix(ref bool canOnline)
    {
        if (CheatToggles.unlockFeatures)
        {
            canOnline = true;
        }
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsMinorOrWaiting))]
public static class EOSManager_IsMinorOrWaiting
{

    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.unlockFeatures)
        {
            __result = false;
        }
    }
}
