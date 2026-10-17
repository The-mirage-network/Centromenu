using UnityEngine;

namespace Centromenu;

public sealed class DeviceTab : ITab
{
    private static readonly (string Label, Platforms Platform)[] Devices =
    {
        ("Steam", Platforms.StandaloneSteamPC),
        ("Epic Games", Platforms.StandaloneEpicPC),
        ("Microsoft Store", Platforms.StandaloneWin10),
        ("Xbox", Platforms.Xbox),
        ("PS5 / PS4", Platforms.Playstation),
        ("Nintendo Switch", Platforms.Switch),
        ("Android", Platforms.Android),
        ("iPhone / iPad", Platforms.IPhone),
        ("Mac", Platforms.StandaloneMac),
        ("Itch.io", Platforms.StandaloneItch)
    };

    public string name => "Device";

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        GUILayout.Label("Platform shown in lobbies", GUIStylePreset.TabSubtitle);

        bool hasSpoof = Utils.StringToPlatformType(
            Centromenu.spoofPlatform.Value,
            out Platforms? selectedPlatform);

        string current = hasSpoof
            ? Utils.PlatformTypeToString(selectedPlatform.Value)
            : $"Native ({Utils.PlatformTypeToString(Constants.GetPlatformType())})";

        GUILayout.Label($"Current: {current}");
        GUILayout.Label("PS5 and PS4 share the PlayStation platform value. Rejoin a lobby after changing this setting.");
        GUILayout.Space(8f);

        if (GUILayout.Button(
                hasSpoof ? "Use Native Device" : "Native Device (Selected)",
                GUIStylePreset.NormalButton,
                GUILayout.Height(34f)))
        {
            SetPlatform(null);
        }

        foreach ((string label, Platforms platform) in Devices)
        {
            bool isSelected = hasSpoof && selectedPlatform.Value == platform;
            string buttonLabel = isSelected ? $"{label} (Selected)" : label;

            if (GUILayout.Button(buttonLabel, GUIStylePreset.NormalButton, GUILayout.Height(32f)))
            {
                SetPlatform(platform);
            }
        }

        GUILayout.EndVertical();
    }

    private static void SetPlatform(Platforms? platform)
    {
        Centromenu.spoofPlatform.Value = platform?.ToString() ?? string.Empty;
        Centromenu.Plugin.Config.Save();
    }
}
