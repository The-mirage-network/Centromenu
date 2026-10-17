using HarmonyLib;
using System;

namespace Centromenu;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
public static class HudManager_Start
{
	// Postfix patch of HudManager.Start to give minimap access to impostors too
	public static void Postfix(HudManager __instance)
	{
		__instance.MapButton.OnClick.RemoveAllListeners(); // Remove previous OnClick action

		// Always open normal map when map button is clicked
		// To access sabotage map, sabotage button can be used
		__instance.MapButton.OnClick.AddListener((Action) (() =>
        {
			__instance.ToggleMapVisible(new MapOptions
			{
				Mode = MapOptions.Modes.Normal
			});

		}));

		BodyFinderButton.Create(__instance);
	}
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudManager_Update
{
	private static bool _hudWasDisabled;

	public static void Postfix(HudManager __instance)
    {
		if (CheatToggles.disableHud != _hudWasDisabled)
		{
			__instance.SetHudActive(!CheatToggles.disableHud);
			_hudWasDisabled = CheatToggles.disableHud;
		}

		__instance.ShadowQuad.gameObject.SetActive(!CentromenuESP.IsFullbrightActive()); // Fullbright

		if (Utils.IsChatUiActive()) // AlwaysChat
		{
			__instance.Chat.gameObject.SetActive(true);
		}
		else
		{
			Utils.CloseChat();
			__instance.Chat.gameObject.SetActive(false);
		}

		CentromenuCheats.UseVentCheat(__instance);
		CentromenuCheats.UnlockReportButtonCheat(__instance);
		BodyFinderButton.Update(__instance);
		CentromenuESP.ZoomOut(__instance);
		CentromenuESP.FreecamCheat();

		// These actions must keep running while the meeting UI is active.
		CentromenuCheats.KiraCheat();
		CentromenuCheats.CloseMeetingCheat();
		CentromenuCheats.SkipMeetingCheat();
		CentromenuCheats.CallMeetingCheat();

		// Close PlayerPickMenu if there is no PPM cheat enabled
		if (PlayerPickMenu.playerpickMenu != null && CheatToggles.ShouldPPMClose())
		{
            PlayerPickMenu.playerpickMenu.Close();
        }
    }
}

[HarmonyPatch(typeof(ReportButton), nameof(ReportButton.SetActive))]
public static class ReportButton_SetActive
{
	public static void Prefix(ref bool isActive)
	{
		if (CheatToggles.unlockReportButton)
		{
			isActive = true;
		}
	}
}
