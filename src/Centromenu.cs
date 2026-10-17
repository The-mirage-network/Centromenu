using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine.SceneManagement;
using System;
using UnityEngine;
using UnityEngine.Analytics;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using CentroFeatures;

namespace Centromenu;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class Centromenu : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static Centromenu Plugin;
    public new static ManualLogSource Log;
    public static readonly string ProfilePath = Path.Combine(Paths.ConfigPath, "CentromenuProfile.txt");

    public static MenuUI menuUI;
    public static ConsoleUI consoleUI;
    public static RolesUI rolesUI;
    public static OverloadUI overloadUI;
    public static DoorsUI doorsUI;
    public static TasksUI tasksUI;
    public static ProtectUI protectUI;
    public static ModDetectorUI modDetectorUI;
    public static KeybindListener keybindListener;
    public static CentroAPI centroAPI;
    public static MeetingMusicPlayer meetingMusicPlayer;

    public static string centromenuVersion = "3.3.1";
    public static List<string> supportedAU = new List<string> { "2026.9.29" };
    public static bool isPanicked = false;
    public static bool inStealthMode = false;

    public static ConfigEntry<string> menuKeybind;
    public static ConfigEntry<string> menuHtmlColor;
    public static ConfigEntry<bool> menuOpenOnMouse;
    public static ConfigEntry<bool> menuKeepSubwindowsOpen;
    public static ConfigEntry<bool> menuAllowClickThrough;
    public static ConfigEntry<bool> menuShowOnStartup;
    public static ConfigEntry<string> panicKeybind;
    public static ConfigEntry<string> spoofLevel;
    public static ConfigEntry<string> spoofPlatform;
    public static ConfigEntry<bool> spoofDeviceId;
    public static ConfigEntry<bool> noTelemetry;
    public static ConfigEntry<string> guestFriendCode;
    public static ConfigEntry<bool> guestMode;
    public static ConfigEntry<bool> autoLoadProfile;
    public static ConfigEntry<string> configEditor;
    public static ConfigEntry<int> adaptMaxStrength;
    public static ConfigEntry<float> adaptMaxCooldown;
    public static ConfigEntry<float> attackLogDelay;
    public static ConfigEntry<int> defaultStrength;
    public static ConfigEntry<float> defaultCooldown;
    public static ConfigEntry<int> killSwitchLvl;

    private static Assembly _embeddedNLayer;

    public override void Load()
    {
        Log = base.Log;
        Plugin = this;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedDependency;


        menuKeybind = Config.Bind("Centromenu.GUI",
                                "Keybind",
                                "Delete",
                                "The keyboard key used to toggle the GUI on and off. List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");

        menuHtmlColor = Config.Bind("Centromenu.GUI",
                                "Color",
                                "",
                                "A custom color for your Centromenu GUI. Supports html color codes");

        menuOpenOnMouse = Config.Bind("Centromenu.GUI",
                                "OpenOnMouse",
                                false,
                                "When enabled, the Centromenu GUI will always be opened at the current mouse position");

        menuKeepSubwindowsOpen = Config.Bind("Centromenu.GUI",
                                "KeepSubwindowsOpen",
                                false,
                                "When enabled, closing the Centromenu GUI will not automatically close its subwindows");

        menuAllowClickThrough = Config.Bind("Centromenu.GUI",
                                "AllowClicksThrough",
                                false,
                                "Legacy setting retained for config compatibility; clicks over the main Centromenu panel are always blocked");

        menuShowOnStartup = Config.Bind("Centromenu.GUI",
                                "ShowOnStartup",
                                false,
                                "When enabled, the Centromenu menu is visible as soon as the plugin loads");

        panicKeybind = Config.Bind("Centromenu.GUI",
                                "PanicKeybind",
                                "Pause",
                                "Immediately disables Centromenu and removes its Harmony patches for the rest of the session");

        autoLoadProfile = Config.Bind("Centromenu.Profile",
                                "AutoLoadProfile",
                                false,
                                "When enabled, your saved keybind and toggle profile will be automatically loaded at game startup");

        configEditor = Config.Bind("Centromenu.Config",
                                "ConfigEditor",
                                "notepad.exe",
                                "The program used to open the config file when using the Open Config toggle. Can be any executable, but using a text editor is recommended");













        spoofLevel = Config.Bind("Centromenu.Spoofing",
                                "Level",
                                "",
                                "A custom player level to display to others in online games to hide your actual platform. IMPORTANT: Custom levels can only be within 1 and 100001. Decimal numbers will not work");

        spoofPlatform = Config.Bind("Centromenu.Spoofing",
                                "Platform",
                                "",
                                "A gaming platform label to display in lobbies. Leave empty to use the native platform; it can also be changed from the Device tab.");

        spoofDeviceId = Config.Bind("Centromenu.Privacy",
                                "HideDeviceId",
                                true,
                                "When enabled, it will hide your unique deviceId from Among Us, which could potentially help bypass hardware bans in the future");

        noTelemetry = Config.Bind("Centromenu.Privacy",
                                "NoTelemetry",
                                true,
                                "When enabled, it will stop Among Us from collecting analytics of your games and sending them to Innersloth using Unity Analytics");















































        CheatToggles.unlockFeatures = true;
        CheatToggles.freeCosmetics = true;
        CheatToggles.avoidPenalties = true;


        CheatToggles.olAutoAdapt = true;
        CheatToggles.olKillSwitch = true;
        CheatToggles.olAutoStop = true;
        CheatToggles.olAutoClear = true;
        CheatToggles.olLogStartStop = true;
        CheatToggles.olLogAttack = true;
        CheatToggles.olLogAddRemove = true;
        CheatToggles.olLogDisconnect = true;

        Harmony.PatchAll();


        menuUI = AddComponent<MenuUI>();
        consoleUI = AddComponent<ConsoleUI>();
        doorsUI = AddComponent<DoorsUI>();
        tasksUI = AddComponent<TasksUI>();
        protectUI = AddComponent<ProtectUI>();
        modDetectorUI = AddComponent<ModDetectorUI>();




        keybindListener = AddComponent<KeybindListener>();
        centroAPI = AddComponent<CentroAPI>();
        meetingMusicPlayer = AddComponent<MeetingMusicPlayer>();


        Centro.Initialize(this);


        if (noTelemetry.Value)
        {
            Analytics.enabled = false;
            Analytics.deviceStatsEnabled = false;
            PerformanceReporting.enabled = false;
        }


        if (!File.Exists(ProfilePath))
        {
            CheatToggles.SaveTogglesToProfile();
        }


        if (autoLoadProfile.Value)
        {
            CheatToggles.LoadTogglesFromProfile();
        }

        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((scene, _) =>
        {
            if (scene.name == "MainMenu" && !(inStealthMode || isPanicked))
            {

                if (!supportedAU.Contains(Application.version))
                {
                    Utils.ShowPopup("\nThis version of Centromenu and this version of Among Us are incompatible\n\nInstall the right version to avoid problems");
                }
            }
        }));
    }

    private static Assembly ResolveEmbeddedDependency(object sender, ResolveEventArgs args)
    {
        if (new AssemblyName(args.Name).Name != "NLayer") return null;
        if (_embeddedNLayer != null) return _embeddedNLayer;

        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Centromenu.Dependencies.NLayer.dll");
        if (stream == null) return null;

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        _embeddedNLayer = Assembly.Load(buffer.ToArray());
        return _embeddedNLayer;
    }
}
