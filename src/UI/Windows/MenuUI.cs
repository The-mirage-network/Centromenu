using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Centromenu;

public class MenuUI : MonoBehaviour
{
    public static int windowHeight = 550;
    public static int windowWidth = 700;
    public static Rect windowRect;

    public static bool isGUIActive = false;
    private List<ITab> _tabs = new();
    private int _selectedTab;
    private VotekickTab _votekickTab;
    private Vector2 _tabScrollPosition;
    private Texture2D _backgroundTexture;
    public static float hue; // For RGB mode

    private void Start()
    {
        // Add all tabs on start
        _tabs.Add(new MovementTab());
        _tabs.Add(new ESPTab());
        _tabs.Add(new RolesTab());
        _tabs.Add(new ShipTab());
        _tabs.Add(new KillTab());
        _votekickTab = new VotekickTab();
        _tabs.Add(_votekickTab);
        _tabs.Add(new PlayerTab());
        _tabs.Add(new NameTab());
        _tabs.Add(new DeviceTab());
        _tabs.Add(new UnloaderTab());
        _tabs.Add(new KillLobbyTab());
        _tabs.Add(new AntiDatingTab());
        _tabs.Add(new DnoteTab());
        _tabs.Add(new ChatTab());
        _tabs.Add(new AnimationsTab());
        _tabs.Add(new ConsoleTab());
        _tabs.Add(new HostOnlyTab());
        _tabs.Add(new PassiveTab());
        _tabs.Add(new ModesTab());
        _tabs.Add(new CustomizationTab());
        _tabs.Add(new ConfigTab());
        _tabs.Add(new FunTab());
        // _tabs.Add(new OverloadTab());

        _backgroundTexture = Utils.LoadTextureFromResources("Centromenu.Assets.Background.png");

        // Instantiate 2D area of MenuUI
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );

        isGUIActive = Centromenu.menuShowOnStartup.Value;
    }

    public void InitStyles()
    {
        GUIStylePreset.ApplyMenuStyle(_backgroundTexture);
        GUI.skin.toggle.fontSize = GUI.skin.button.fontSize = GUI.skin.label.fontSize = 15;
    }

    private void Update()
    {
        AntiDatingNameGuard.Tick();
        PlayerTab.Tick();
        ChatTab.Tick();
        _votekickTab?.Tick();
        PersistentVotekickController.Tick();
        LobbyLookupQueue.Tick();

        if (Input.GetKeyDown(Utils.StringToKeycode(Centromenu.menuKeybind.Value)))
        {
            // Enable or disable GUI with DELETE key
            isGUIActive = !isGUIActive;

            if (Centromenu.menuOpenOnMouse.Value)
            {
                // Teleport the window to the mouse for immediate use
                Vector2 mousePosition = Input.mousePosition;
                windowRect.position = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            }
        }

        if (CheatToggles.rgbMode)
        {
            hue += Time.deltaTime * 0.3f; // Adjust speed of color change, higher multiplier = faster
            if (hue > 1f) hue -= 1f; // Loop hue back to 0 when it exceeds 1
        }

        if (CheatToggles.stealthMode != Centromenu.inStealthMode)
        {
            Centromenu.inStealthMode = CheatToggles.stealthMode;

            Scene scene = SceneManager.GetActiveScene();

            if (scene.name == "MainMenu" || scene.name == "MatchMaking")
            {
                SceneManager.LoadScene(scene.name);
            }
        }

        if (CheatToggles.panicMode) Utils.Panic();

        var stamp = ModManager.Instance.ModStamp;
        if (stamp) stamp.enabled = !(Centromenu.inStealthMode || Centromenu.isPanicked || CheatToggles.hideModStamp);
        HandleModStampClick(stamp);

        if (CheatToggles.openConfig)
        {
            Utils.OpenConfigFile();
            CheatToggles.openConfig = false;
        }

        if (CheatToggles.reloadConfig)
        {
            Centromenu.Plugin.Config.Reload();
            CheatToggles.reloadConfig = false;
        }

        if (CheatToggles.saveProfile)
        {
            CheatToggles.saveProfile = false; // Disable first to avoid saving it to profile
            CheatToggles.SaveTogglesToProfile();
        }

        if (CheatToggles.loadProfile)
        {
            CheatToggles.LoadTogglesFromProfile();
            CheatToggles.loadProfile = false;
        }

        // Some cheats only work if the LocalPlayer exists, so they are turned off if it does not
        if(!Utils.isPlayer)
        {
            CheatToggles.setFakeRole = false;
            CheatToggles.setFakeAlive = false;
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.teleportPlayer = false;
            CheatToggles.spectate = false;
            CheatToggles.freecam = false;
            CheatToggles.killPlayer = false;
            CheatToggles.callMeeting = false;

            if (CheatToggles.runOverload)
            {
                OverloadUI.StopOverload();
            }
        }

        // Some cheats only work if the ship exists, so they are turned off if it does not
        if(!Utils.isShip)
        {
            CheatToggles.kira = false;
            CheatToggles.sabotageMap = false;
            CheatToggles.unfixableLights = false;
            CheatToggles.completeMyTasks = false;
            CheatToggles.kickVents = false;
            CheatToggles.reportBody = false;
            CheatToggles.closeMeeting = false;
            CheatToggles.reactorSab = false;
            CheatToggles.oxygenSab = false;
            CheatToggles.commsSab = false;
            CheatToggles.elecSab = false;
            CheatToggles.mushSab = false;
            CheatToggles.closeAllDoors = false;
            CheatToggles.openAllDoors = false;
            CheatToggles.spamCloseAllDoors = false;
            CheatToggles.spamOpenAllDoors = false;
            CheatToggles.mushSpore = false;

            CentromenuCheats.StopShipAnimCheats();
        }

        if(!Utils.isHost && !Utils.isFreePlay)
        {
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.killPlayer = false;
            CheatToggles.ejectPlayer = false;
            CheatToggles.noKillCd = false;
            CheatToggles.killAnyone = false;
            CheatToggles.killVanished = false;
            CheatToggles.forceStartGame = false;
            CheatToggles.voteImmune = false;
            CheatToggles.noGameEnd = false;
            CheatToggles.showProtectMenu = false;
            CheatToggles.showRolesMenu = false;
            CheatToggles.noOptionsLimits = false;
        }

        // Some cheats only work if in a meeting, so they are turned off if it does not
        if (!Utils.isMeeting)
        {
            CheatToggles.skipMeeting = false;
            CheatToggles.ejectPlayer = false;
        }
    }

    private static void HandleModStampClick(SpriteRenderer stamp)
    {
        if (!stamp || !stamp.enabled || !Input.GetMouseButtonDown(0)) return;

        Camera camera = Camera.main;
        if (!camera) return;

        Bounds bounds = stamp.bounds;
        Vector3 firstCorner = camera.WorldToScreenPoint(bounds.min);
        Vector3 secondCorner = camera.WorldToScreenPoint(bounds.max);
        Rect clickableArea = Rect.MinMaxRect(
            Mathf.Min(firstCorner.x, secondCorner.x) - 10f,
            Mathf.Min(firstCorner.y, secondCorner.y) - 10f,
            Mathf.Max(firstCorner.x, secondCorner.x) + 10f,
            Mathf.Max(firstCorner.y, secondCorner.y) + 10f);

        if (clickableArea.Contains(Input.mousePosition))
        {
            Centromenu.modDetectorUI?.Open();
        }
    }

    private void LateUpdate()
    {
        CentromenuCheats.SuppressCameraShake();
        CentromenuCheats.AutoWeaponsCheat();
    }

    public void OnGUI()
    {
        if (!isGUIActive || Centromenu.isPanicked) return;

        InitStyles();

        UIHelpers.ApplyUIColor();

        UIHelpers.BeginPanelWindow();
        int gradientOffset = Mathf.FloorToInt(Time.unscaledTime * 10f);
        string windowTitle = Utils.GetAnimatedCentromenuVersion(gradientOffset);
        windowRect = GUI.Window((int)WindowId.MenuUI, windowRect, (GUI.WindowFunction)WindowFunction, windowTitle);
        UIHelpers.RestoreContentColor();

    }

    public void WindowFunction(int windowID)
    {
        UIHelpers.RestoreContentColor();

        GUILayout.BeginHorizontal();

        // Leave enough room for the vertical scrollbar so it cannot cover the
        // labels or clip the rounded right edge of the navigation buttons.
        const float tabScrollbarSpace = 22f;
        var tabRailWidth = windowWidth * 0.19f;
        _tabScrollPosition = GUILayout.BeginScrollView(
            _tabScrollPosition,
            false,
            true,
            GUILayout.Width(tabRailWidth),
            GUILayout.ExpandHeight(true)
        );
        for (var i = 0; i < _tabs.Count; i++)
        {
            var tabStyle = _selectedTab == i
                ? GUIStylePreset.SelectedTabButton
                : GUIStylePreset.TabButton;

            if (GUILayout.Button(
                    _tabs[i].name,
                    tabStyle,
                    GUILayout.Width(tabRailWidth - tabScrollbarSpace),
                    GUILayout.Height(35f)))
                _selectedTab = i;
        }
        GUILayout.EndScrollView();

        // Vertical separator line + invisible space to create gap between the tab selector and the content
        GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
        GUILayout.Space(10f);

        // Right tab content and controls
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // Tab-specific content
        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
            _tabs[_selectedTab].Draw();
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Make the window draggable
        GUI.DragWindow();
    }

}
