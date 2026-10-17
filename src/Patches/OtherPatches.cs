using HarmonyLib;
using AmongUs.Data;
using AmongUs.Data.Player;
using UnityEngine;
using System;
using System.Security.Cryptography;
using InnerNet;
using System.Collections.Generic;

namespace Centromenu;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetPlatformData))]
public static class Constants_GetPlatformData
{

    public static void Postfix(ref PlatformSpecificData __result)
    {
        if (Utils.StringToPlatformType(Centromenu.spoofPlatform.Value, out Platforms? platformType))
        {
            __result = new PlatformSpecificData
            {
                Platform = (Platforms)platformType,
                PlatformName = Constants.GetPlatformName()
            };
        }
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.HandleDisconnect), new[] { typeof(PlayerControl), typeof(DisconnectReasons) })]
public static class GameData_HandleDisconnect
{
    public static HashSet<int> disconnectQueue = new();


    public static void Prefix(PlayerControl player)
    {
        if (!CheatToggles.runOverload) return;

        NetworkedPlayerInfo playerData = player?.Data;
        if (playerData == null) return;

        bool isTarget = OverloadUI.currentTargets.Contains(playerData);

        if (isTarget) disconnectQueue.Add(playerData.ClientId);
    }



    public static void Postfix(PlayerControl player)
    {
        if (!CheatToggles.runOverload) return;

        NetworkedPlayerInfo playerData = player?.Data;
        if (playerData == null) return;

        int clientId = player.Data.ClientId;

        if (disconnectQueue.Contains(clientId))
        {
            OverloadUI.numSuccesses++;

            if (CheatToggles.olLogDisconnect)
            {
                int total = OverloadUI.currentTargets.Count
                            + OverloadUI.numSuccesses
                            - disconnectQueue.Count;

                string colorStr = ColorUtility.ToHtmlStringRGB(Color.green);

                OverloadUI.LogConsole($"> <b><color=#{colorStr}>!! {playerData.DefaultOutfit.PlayerName} (ID : {playerData.ClientId}) Disconnected !! - [{OverloadUI.numSuccesses}/{total}]</color></b>");
            }

            disconnectQueue.Remove(clientId);
        }
    }
}

[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
public static class FreeChatInputField_UpdateCharCount
{

    public static void Postfix(FreeChatInputField __instance)
    {

        if (!CheatToggles.longerMessages) return;


        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText($"{length}/{__instance.textArea.characterLimit}");

        if (length < 90)
        {
            __instance.charCountText.color = Color.black;
        }
        else if (length < 120)
        {
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        }
        else
        {
            __instance.charCountText.color = Color.red;
        }
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
public static class ChatBubble_SetName
{
    public static void Postfix(ChatBubble __instance)
	{
        CentromenuESP.ChatNametags(__instance);
    }
}

[HarmonyPatch(typeof(SystemInfo), nameof(SystemInfo.deviceUniqueIdentifier), MethodType.Getter)]
public static class SystemInfo_deviceUniqueIdentifier_Getter
{


    public static void Postfix(ref string __result)
    {
        if (!Centromenu.spoofDeviceId.Value) return;

        var bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        __result = BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShower_Start
{
    private static VersionShower _instance;
    private static string _originalText;


    public static void Postfix(VersionShower __instance)
    {
        _instance = __instance;
        _originalText ??= __instance.text.text;
        UpdateText();
    }

    public static void UpdateText()
    {
        if (_instance == null || Centromenu.inStealthMode || Centromenu.isPanicked) return;

        if (CheatToggles.hideWatermark)
        {
            _instance.text.text = _originalText ?? $"v{Application.version}";
            return;
        }

        int gradientOffset = Mathf.FloorToInt(Time.unscaledTime * 10f);
        string brandText = Utils.GetAnimatedCentromenuVersion(gradientOffset);
        string prominentBrandText = $"<size=160%><b>{brandText}</b></size>";
        string gameVersion = Centromenu.supportedAU.Contains(Application.version)
            ? $"v{Application.version}"
            : $"<color=red>v{Application.version}</color>";

        _instance.text.text = $"{prominentBrandText}  <size=90%>({gameVersion})</size>";
    }
}

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
public static class PingTracker_Update
{

    public static void Postfix(PingTracker __instance)
    {
        VersionShower_Start.UpdateText();

        if (Centromenu.inStealthMode)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopLeft;

            return;
        }

        __instance.text.alignment = TMPro.TextAlignmentOptions.Center;

        int ping = Utils.GetPing();
        string pingText = Utils.GetColoredPingText($"PING: {ping} ms", ping);

        float footerGradientOffset = Time.unscaledTime * 2.5f;


        string footerText = CheatToggles.hideWatermark
            ? string.Empty
            : $"<size=115%><b>{Utils.GetGradientText(
                "Centromenu",
                new Color32(88, 6, 20, 255),
                new Color32(205, 28, 55, 255),
                new Color32(150, 10, 35, 255),
                footerGradientOffset
            )}</b></size>  <size=82%><color=#B55A68>by</color></size>  <size=105%><b>{Utils.GetGradientText(
                "Centroscape",
                new Color32(150, 10, 35, 255),
                new Color32(205, 28, 55, 255),
                new Color32(88, 6, 20, 255),
                -footerGradientOffset
            )}</b></size>";
        string fpsText = CheatToggles.showFps ? $"FPS: {Utils.GetFps()}" : string.Empty;
        string timeText = CheatToggles.showTime ? DateTime.Now.ToString("HH:mm:ss") : string.Empty;
        var statusParts = new List<string>();
        if (!string.IsNullOrEmpty(footerText)) statusParts.Add(footerText);
        statusParts.Add(pingText);
        if (!string.IsNullOrEmpty(fpsText)) statusParts.Add(fpsText);
        if (!string.IsNullOrEmpty(timeText)) statusParts.Add(timeText);
        string statusText = string.Join(" ~ ", statusParts);

        if (AmongUsClient.Instance.IsGameStarted)
        {
            __instance.aspectPosition.DistanceFromEdge = new Vector3(-0.21f, 0.50f, 0f);

            __instance.text.text = statusText;

            return;
        }

        __instance.text.text = string.IsNullOrEmpty(footerText)
            ? string.Join(" ~ ", statusParts)
            : $"{footerText}\n{string.Join(" ~ ", statusParts.GetRange(1, statusParts.Count - 1))}";

    }
}

[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
public static class DisconnectPopup_DoShow
{

    public static void Postfix(DisconnectPopup __instance)
    {
        if (!CheatToggles.copyLobbyCodeOnDisconnect) return;

        GUIUtility.systemCopyBuffer = AmongUsClient_OnGameJoined.lastGameIdString;

        __instance.SetText(__instance._textArea.text + "\n\n<size=60%>Lobby code has been copied to the clipboard</size>");
    }
}

[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanMinutesLeft), MethodType.Getter)]
public static class PlayerBanData_BanMinutesLeft_Getter
{

    public static void Postfix(PlayerBanData __instance, ref int __result)
    {
        if (!CheatToggles.avoidPenalties) return;

        __instance.BanPoints = 0f;
        __result = 0;
    }
}

[HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
public static class FullAccount_CanSetCustomName
{

    public static void Prefix(ref bool canSetName)
    {
        if (CheatToggles.unlockFeatures)
        {
            canSetName = true;
        }
    }
}

[HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
public static class AccountManager_CanPlayOnline
{

    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.unlockFeatures)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.JoinGame))]
public static class InnerNetClient_JoinGame
{

    public static void Prefix()
    {
        if (CheatToggles.unlockFeatures)
        {
            DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.LoggedIn;
        }
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
public static class GameManager_CheckTaskCompletion
{

    public static bool Prefix(ref bool __result)
    {
        if (!CheatToggles.noGameEnd) return true;

        __result = false;

        return false;
    }
}

[HarmonyPatch(typeof(Mushroom), nameof(Mushroom.FixedUpdate))]
public static class Mushroom_FixedUpdate
{
    public static void Postfix(Mushroom __instance)
    {
        CentromenuESP.SporeCloudVision(__instance);
    }
}


[HarmonyPatch(typeof(DoorBreakerGame), nameof(DoorBreakerGame.Start))]
public static class DoorBreakerGame_Start
{

    public static bool Prefix(DoorBreakerGame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        DoorsHandler.OpenDoor(__instance.MyDoor);
        __instance.MyDoor.SetDoorway(true);
        __instance.Close();

        return false;
    }
}


[HarmonyPatch(typeof(DoorCardSwipeGame), nameof(DoorCardSwipeGame.Begin))]
public static class DoorCardSwipeGame_Begin
{

    public static bool Prefix(DoorCardSwipeGame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        DoorsHandler.OpenDoor(__instance.MyDoor);
        __instance.MyDoor.SetDoorway(true);
        __instance.Close();

        return false;
    }
}


[HarmonyPatch(typeof(MushroomDoorSabotageMinigame), nameof(MushroomDoorSabotageMinigame.Begin))]
public static class MushroomDoorSabotageMinigame_Begin
{

    public static bool Prefix(MushroomDoorSabotageMinigame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        __instance.FixDoorAndCloseMinigame();

        return false;
    }
}














[HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
public static class IntroCutscene_CoBegin
{

    public static void Prefix()
    {
        if (!Utils.isHost || !CheatToggles.forcedRole.HasValue) return;

        var forcedRole = CheatToggles.forcedRole.Value;


        if (PlayerControl.LocalPlayer.Data.RoleType == forcedRole)
        {
            return;
        }


        PlayerControl roleSwapTarget = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.RoleType != forcedRole) continue;
            roleSwapTarget = player;
            break;
        }

        RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, forcedRole);

        if (roleSwapTarget != null)
        {
            RoleManager.Instance.SetRole(roleSwapTarget, PlayerControl.LocalPlayer.Data.RoleType);
        }
    }
}


[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class GameContainer_SetupGameInfo
{


    public static void Postfix(GameContainer __instance)
    {
        if (!CheatToggles.seeLobbyInfo) return;

        var trueHostName = __instance.gameListing.TrueHostName;

        var age = __instance.gameListing.Age;
        var lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : "")}{age % 60}";

        var platform = Utils.PlatformTypeToString(__instance.gameListing.Platform);
        var capacity = $"{__instance.gameListing.PlayerCount}/{__instance.gameListing.MaxPlayers}";
        var gameCode = GameCode.IntToGameName(__instance.gameListing.GameId);




        __instance.capacity.text = $"{capacity}<size=45%>\n{trueHostName}  <#fb0>{gameCode}</color>\n" +
                                   $"<#b0f>{platform}</color>  {lobbyTime}</size>";
    }
}

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
public static class BanMenu_SetVisible
{

    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!Utils.isHost) return true;

        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;

        __instance.BanButton.gameObject.SetActive(true);
        __instance.KickButton.gameObject.SetActive(true);
        __instance.MenuButton.gameObject.SetActive(show);

        return false;
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
public static class IGameOptionsExtensions_GetAdjustedNumImpostors
{

    public static bool Prefix(ref int __result)
    {
        if (!CheatToggles.noOptionsLimits) return true;

        __result = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;

        return false;
    }
}

[HarmonyPatch(typeof(MatchInfoHudButton), nameof(MatchInfoHudButton.Update))]
public static class MatchInfoHudButton_Update
{

    public static bool Prefix(MatchInfoHudButton __instance)
    {
        if (CheatToggles.enableChat)
        {
            __instance.aspectPosition.DistanceFromEdge = MatchInfoHudButton.adjustedDistanceFromEdge;

            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class PlayerPurchasesData_GetPurchase
{

    public static void Postfix(ref bool __result)
    {
        if (!CheatToggles.freeCosmetics) return;

        __result = true;
    }
}

[HarmonyPatch]
public static class PassiveUiElement_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown))]
    [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp))]
    [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveMouseOver))]
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.ReceiveClickDown))]
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.ReceiveClickUp))]
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.ReceiveMouseOver))]
    [HarmonyPatch(typeof(SlideBar), nameof(SlideBar.ReceiveClickDrag))]
    [HarmonyPatch(typeof(Scrollbar), nameof(Scrollbar.ReceiveClickDrag))]
    [HarmonyPatch(typeof(Scroller), nameof(Scroller.UpdateScrollBars))]


    public static bool Prefix()
    {


        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);


        bool overMainMenu = MenuUI.isGUIActive && MenuUI.windowRect.Contains(mousePosition);
        bool overModDetector = ModDetectorUI.IsVisible && ModDetectorUI.windowRect.Contains(mousePosition);
        return !(overMainMenu || overModDetector);
    }
}
