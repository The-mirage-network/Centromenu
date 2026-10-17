using UnityEngine;

namespace Centromenu;





public sealed class CustomizationTab : ITab
{
    public string name => "Customization";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.36f));
        GUILayout.Label("Overlay", GUIStylePreset.TabSubtitle);
        CheatToggles.showFps = GUILayout.Toggle(CheatToggles.showFps, " Show FPS");
        CheatToggles.showTime = GUILayout.Toggle(CheatToggles.showTime, " Show Time");
        CheatToggles.hideWatermark = GUILayout.Toggle(CheatToggles.hideWatermark, " Hide Watermark");
        CheatToggles.hideModStamp = GUILayout.Toggle(CheatToggles.hideModStamp, " Hide Mod Stamp");

        GUILayout.Space(15f);
        GUILayout.Label("HUD", GUIStylePreset.TabSubtitle);
        CheatToggles.disableHud = GUILayout.Toggle(CheatToggles.disableHud, " Disable HUD");
        GUILayout.EndVertical();

        GUILayout.Space(20f);

        GUILayout.BeginVertical();
        GUILayout.Label("Controls", GUIStylePreset.TabSubtitle);
        CheatToggles.allowKeybindsWhileChatting = GUILayout.Toggle(
            CheatToggles.allowKeybindsWhileChatting,
            " Allow Keybinds While Chatting");
        CheatToggles.smoothZoom = GUILayout.Toggle(CheatToggles.smoothZoom, " Smooth Zoom");
        CheatToggles.lightTheme = GUILayout.Toggle(CheatToggles.lightTheme, " Light Theme");

        GUILayout.Space(15f);
        GUILayout.Label("Configured Hotkeys", GUIStylePreset.TabSubtitle);
        GUILayout.Label($"Menu: {Centromenu.menuKeybind.Value}");
        GUILayout.Label($"Panic: {Centromenu.panicKeybind.Value}");
        GUILayout.Label("Per-feature hotkeys remain available through CentromenuProfile.txt.");
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }
}
