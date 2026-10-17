using HarmonyLib;

namespace Centromenu;



[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
public static class NumberOption_Increase
{

    public static bool Prefix(NumberOption __instance)
    {
        if (!CheatToggles.noOptionsLimits) return true;



        if (!Utils.isHideNSeek && __instance.Title is StringNames.GameNumImpostors or StringNames.GamePlayerSpeed) return true;

        __instance.Value += __instance.Increment;
        __instance.UpdateValue();
        __instance.OnValueChanged.Invoke(__instance);
        __instance.AdjustButtonsActiveState();

        return false;
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
public static class NumberOption_Decrease
{

    public static bool Prefix(NumberOption __instance)
    {
        if (!CheatToggles.noOptionsLimits) return true;



        if (!Utils.isHideNSeek && __instance.Title is StringNames.GameNumImpostors or StringNames.GamePlayerSpeed) return true;

        __instance.Value -= __instance.Increment;
        __instance.UpdateValue();
        __instance.OnValueChanged.Invoke(__instance);
        __instance.AdjustButtonsActiveState();

        return false;
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Initialize))]
public static class NumberOption_Initialize
{

    public static void Postfix(NumberOption __instance)
    {
        if (!CheatToggles.noOptionsLimits) return;



        if (!Utils.isHideNSeek && __instance.Title is StringNames.GameNumImpostors or StringNames.GamePlayerSpeed) return;

        __instance.ValidRange = new FloatRange(-999f, 999f);
    }
}
