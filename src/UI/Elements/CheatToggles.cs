using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AmongUs.GameOptions;
using UnityEngine;

namespace Centromenu;

public struct CheatToggles
{

    public static bool noClip;
    public static bool teleportPlayer;
    public static bool teleportCursor;
    public static bool invertControls;
    public static bool anchor;


    public static bool setFakeRole;
    public static bool setFakeAlive;
    public static bool noKillCd;
    public static bool showTasksMenu;
    public static bool completeMyTasks;
    public static bool impostorTasks;
    public static bool killReach;
    public static bool killAnyone;
    public static bool endlessSsDuration;
    public static bool endlessBattery;
    public static bool endlessTracking;
    public static bool noTrackingCooldown;
    public static bool noTrackingDelay;
    public static bool trackReach;
    public static bool interrogateReach;
    public static bool noVitalsCooldown;
    public static bool noVentCooldown;
    public static bool endlessVentTime;
    public static bool endlessVanish;
    public static bool killVanished;
    public static bool noVanishAnim;
    public static bool noShapeshiftAnim;
    public static bool noShapeshiftCooldown;
    public static bool noProtectCooldown;
    public static bool noPhantomCooldown;
    public static bool noRoleCooldowns;
    public static bool customKillDistance;
    public static float killDistanceMultiplier = 1f;


    public static bool noShadows;
    public static bool seeGhosts;
    public static bool seeRoles;
    public static bool seePlayerInfo;
    public static bool seeDisguises;
    public static bool taskArrows;
    public static bool revealVotes;
    public static bool seeLobbyInfo;


    public static bool spectate;
    public static bool zoomOut;
    public static bool freecam;
    public static bool smoothZoom = true;
    public static bool noCameraShake;


    public static bool allowKeybindsWhileChatting;
    public static bool showFps;
    public static bool showTime;
    public static bool hideWatermark;
    public static bool hideModStamp;
    public static bool disableHud;
    public static bool lightTheme;


    public static bool mapCrew;
    public static bool mapImps;
    public static bool mapGhosts;
    public static bool colorBasedMap;


    public static bool tracersImps;
    public static bool tracersCrew;
    public static bool tracersGhosts;
    public static bool tracersBodies;
    public static bool colorBasedTracers;
    public static bool distanceBasedTracers;


    public static bool enableChat;
    public static bool unlockCharacters;
    public static bool bypassUrlBlock;
    public static bool longerMessages;
    public static bool unlockClipboard;
    public static bool lowerRateLimits;
    public static bool noChatDelay;
    public static bool chatSpam;


    public static bool closeMeeting;
    public static bool autoOpenDoorsOnUse;
    public static bool unfixableLights;
    public static bool callMeeting;
    public static bool reportBody;
    public static bool autoReportBodies;


    public static bool kira;


    public static bool commsSab;
    public static bool elecSab;
    public static bool reactorSab;
    public static bool oxygenSab;
    public static bool mushSab;
    public static bool mushSpore;
    public static bool showDoorsMenu;
    public static bool openAllDoors;
    public static bool closeAllDoors;
    public static bool spamOpenAllDoors;
    public static bool spamCloseAllDoors;
    public static bool sabotageMap;
    public static bool autoFixSabotages;


    public static bool unlockVents;
    public static bool walkInVents;
    public static bool kickVents;


    public static bool animShields;
    public static bool animAsteroids;
    public static bool autoWeapons;
    public static bool animEmptyGarbage;
    public static bool animMedScan;
    public static bool animCamsInUse;
    public static bool animPet;
    public static bool moonWalk;


    public static bool showOverload;
    public static bool showOverloadSettings;
    public static bool olAutoStart;
    public static bool olAutoAdapt;
    public static bool olShowRpcTotal;
    public static bool olAutoStop;
    public static bool olLockTargets;
    public static bool olKillSwitch;
    public static bool olPlayerCooldown;
    public static bool olAutoClear;
    public static bool olLogStartStop;
    public static bool olLogAddRemove;
    public static bool olLogDisconnect;
    public static bool olLogAttack;
    public static bool olVerboseLogs;
    public static bool runOverload;
    public static bool overloadAll;
    public static bool overloadHost;
    public static bool overloadCrew;
    public static bool overloadImps;
    public static bool overloadReset;


    public static bool showConsole;
    public static bool logDeaths;
    public static bool logShapeshifts;
    public static bool logVents;
    public static bool logTasks;
    public static bool logGameState;


    public static bool voteImmune;
    public static bool forceRole;
    public static RoleTypes? forcedRole;
    public static bool showRolesMenu;
    public static bool skipMeeting;
    public static bool forceStartGame;
    public static bool noGameEnd;
    public static bool showProtectMenu;
    public static bool noOptionsLimits;
    public static bool ejectPlayer;
    public static bool killPlayer;
    public static bool telekillPlayer;
    public static bool killAll;
    public static bool killAllCrew;
    public static bool killAllImps;


    public static bool unlockFeatures;
    public static bool freeCosmetics;
    public static bool avoidPenalties;
    public static bool copyLobbyCodeOnDisconnect;
    public static bool spoofAprilFoolsDate;
    public static bool unlockReportButton;


    public static bool antiDatingEnabled;
    public static bool antiDatingAutoReport;
    public static bool antiDatingAutoVoteKick;


    public static bool rgbMode;
    public static bool stealthMode;
    public static bool panicMode;


    public static bool reloadConfig;
    public static bool openConfig;
    public static bool loadProfile;
    public static bool saveProfile;


    public static readonly Dictionary<string, KeyCode> Keybinds = new();


    public static readonly Dictionary<string, FieldInfo> ToggleFields = new();


    static CheatToggles()
    {
        var fields = typeof(CheatToggles).GetFields(BindingFlags.Static | BindingFlags.Public);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(bool)) continue;

            ToggleFields[field.Name] = field;
            Keybinds[field.Name] = KeyCode.None;
        }
    }

    public static void DisablePPMCheats(string variableToKeep)
    {
        ejectPlayer = variableToKeep == "ejectPlayer" && ejectPlayer;
        reportBody = variableToKeep == "reportBody" && reportBody;
        killPlayer = variableToKeep == "killPlayer" && killPlayer;
        telekillPlayer = variableToKeep == "telekillPlayer" && telekillPlayer;
        spectate = variableToKeep == "spectate" && spectate;
        setFakeRole = variableToKeep == "setFakeRole" && setFakeRole;
        setFakeAlive = variableToKeep == "setFakeAlive" && setFakeAlive;
        forceRole = variableToKeep == "forceRole" && forceRole;
        teleportPlayer = variableToKeep == "teleportPlayer" && teleportPlayer;
    }

    public static bool ShouldPPMClose()
    {
        return !setFakeRole && !setFakeAlive && !forceRole && !ejectPlayer && !reportBody && !telekillPlayer && !killPlayer && !spectate && !teleportPlayer;
    }


    public static void DisableAll()
    {
        foreach (var field in ToggleFields.Values)
        {
            field.SetValue(null, false);
        }
    }



    public static void SaveTogglesToProfile()
    {
        using var writer = new StreamWriter(Centromenu.ProfilePath);

        writer.WriteLine("# CentromenuProfile");
        writer.WriteLine("# Format: ToggleName = True/False = KeyCode.KEY");
        writer.WriteLine("# - List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");
        writer.WriteLine("# - Setting a keybind is optional. Use KeyCode.None to not set a keybind");
        writer.WriteLine("# - Multiple toggles may have the same key, but multiple keys per toggle are NOT supported");
        writer.WriteLine("# - Keybinds are only applied after loading this profile by pressing 'Load from Profile' in the Config menu");
        writer.WriteLine();

        foreach (var field in ToggleFields.Values)
        {
            Keybinds.TryGetValue(field.Name, out var key);
            writer.WriteLine($"{field.Name} = {field.GetValue(null)} = KeyCode.{key}");
        }
        writer.WriteLine($"killDistanceMultiplier = {killDistanceMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }



    public static void LoadTogglesFromProfile()
    {
        if (!File.Exists(Centromenu.ProfilePath)) return;

        using var reader = new StreamReader(Centromenu.ProfilePath);

        while (reader.ReadLine() is { } line)
        {

            if (string.IsNullOrWhiteSpace(line)) continue;


            line = line.Trim();
            if (line.StartsWith("#")) continue;


            var parts = line.Split('=', 3);
            if (parts.Length < 2) continue;


            var name = parts[0].Trim();
            if (name == nameof(killDistanceMultiplier))
            {
                if (float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var multiplier))
                {
                    killDistanceMultiplier = Mathf.Clamp(multiplier, 1f, 10f);
                }
                continue;
            }
            if (!ToggleFields.TryGetValue(name, out var field)) continue;


            if (bool.TryParse(parts[1].Trim(), out var boolVal))
            {
                field.SetValue(null, boolVal);
            }


            KeyCode key = KeyCode.None;
            if (parts.Length >= 3)
            {
                var keyPart = parts[2].Trim();
                if (keyPart.StartsWith("KeyCode."))
                {
                    keyPart = keyPart["KeyCode.".Length..];
                }

                if (!string.IsNullOrEmpty(keyPart) && System.Enum.TryParse<KeyCode>(keyPart, true, out var parsed))
                {
                    key = parsed;
                }
            }

            Keybinds[name] = key;
        }
    }

}
