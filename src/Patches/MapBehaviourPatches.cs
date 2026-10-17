using HarmonyLib;
using System.Collections.Generic;

namespace Centromenu;

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
public static class MapBehaviour_ShowNormalMap
{

    public static void Postfix(MapBehaviour __instance)
    {
        MinimapHandler.minimapActive = MinimapHandler.IsCheatEnabled();

        if (!MinimapHandler.minimapActive)
        {
            return;
        }

        __instance.ColorControl.SetColor(Palette.Purple);

        __instance.DisableTrackerOverlays();


        try
        {
            MinimapHandler.herePoints.ForEach(x => UnityEngine.Object.Destroy(x.sprite.gameObject));
            MinimapHandler.herePoints.Clear();
        }
        catch { }


        var temp = new List<HerePoint>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!player.AmOwner)
            {
                var herePoint = UnityEngine.Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent);

                temp.Add(new HerePoint(player, herePoint));
            }
        }
        MinimapHandler.herePoints = temp;

    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
public static class MapBehaviour_FixedUpdate
{

    public static void Postfix(MapBehaviour __instance)
    {

        if (MinimapHandler.IsCheatEnabled() != MinimapHandler.minimapActive)
        {
            if (!__instance.infectedOverlay.gameObject.active)
            {
                __instance.Close();
                __instance.ShowNormalMap();
            }
        }


        var temp = MinimapHandler.herePoints;
        foreach (var herePoint in temp)
        {
            MinimapHandler.HandleHerePoint(herePoint);
        }

        foreach (var herePoint in MinimapHandler.herePointsToRemove)
        {
            MinimapHandler.herePoints.Remove(herePoint);
        }

    }
}

[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close))]
public static class MapBehaviour_Close
{

    public static void Postfix(MapBehaviour __instance)
    {
        try
        {
            MinimapHandler.herePoints.ForEach(x => UnityEngine.Object.Destroy(x.sprite.gameObject));
            MinimapHandler.herePoints.Clear();
        }
        catch { }
    }
}
