using HarmonyLib;

namespace Centromenu;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class ShipStatus_FixedUpdate
{
    public static void Postfix(ShipStatus __instance)
    {
        CentromenuSabotageCheats.Process(__instance);
        CentromenuCheats.OpenSabotageMapCheat();

        CentromenuCheats.WalkInVentCheat();
        CentromenuCheats.KickVentsCheat();

        CentromenuPPMCheats.ReportBodyPPM();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class FungleShipStatus_FixedUpdate
{
    public static void Postfix(FungleShipStatus __instance)
    {
        CentromenuSabotageCheats.ProcessFungle(__instance);
    }
}
