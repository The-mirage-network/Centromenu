using HarmonyLib;

namespace Centromenu;

[HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.Initialize))]
public static class NormalPlayerTask_Initialize
{

    public static void Postfix(NormalPlayerTask __instance)
    {

        if (__instance.TaskType == TaskTypes.UploadData && (MapNames)Utils.GetCurrentMapID() == MapNames.Airship)
        {
            if (__instance.taskStep == 0)
            {
                var airshipTask = __instance.GetComponent<AirshipUploadTask>();
                var consolePositions = airshipTask.FindValidConsolesPositions();


                for (var i = 0; i < consolePositions.Count && i < airshipTask.Arrows.Length; i++)
                {

                    airshipTask.Arrows[i].target = consolePositions[i];
                }

                airshipTask.LocationDirty = true;

                return;
            }
        }

        ArrowHandler.EnsureArrowExists(__instance);

        if (!ArrowHandler.NeedsSpecialTarget(__instance))
        {
            __instance.UpdateArrowAndLocation();
        }
        else if (ArrowHandler.IsOwnedAndIncomplete(__instance))
        {
            ArrowHandler.SetArrowTargetForSpecialTasks(__instance);
        }
    }
}

[HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.FixedUpdate))]
public static class NormalPlayerTask_FixedUpdate
{

    public static void Postfix(NormalPlayerTask __instance)
    {
        if (__instance.Arrow == null) return;

        if (!CheatToggles.taskArrows)
        {

            if (__instance.taskStep == 0)
            {
                __instance.Arrow.gameObject.SetActive(false);
            }

            return;
        }

        if (ArrowHandler.IsOwnedAndIncomplete(__instance))
        {
            if (ArrowHandler.NeedsSpecialTarget(__instance))
            {
                ArrowHandler.SetArrowTargetForSpecialTasks(__instance);
            }

            __instance.Arrow.gameObject.SetActive(true);
        }
    }
}

[HarmonyPatch(typeof(AirshipUploadTask), nameof(AirshipUploadTask.FixedUpdate))]
public static class AirshipUploadTask_FixedUpdate_Patch
{

    public static void Postfix(AirshipUploadTask __instance)
    {

        if (__instance.Arrows == null) return;

        if (!CheatToggles.taskArrows)
        {

            if (__instance.taskStep != 0) return;

            foreach (var arrow in __instance.Arrows)
            {
                arrow.gameObject.SetActive(false);
            }

            return;
        }

        var consolePositions = __instance.FindValidConsolesPositions();

        for (var i = 0; i < __instance.Arrows.Length; i++)
        {

            __instance.Arrows[i].gameObject.SetActive(i < consolePositions.Count && __instance.Owner != null && __instance.Owner.AmOwner);
        }
    }
}
