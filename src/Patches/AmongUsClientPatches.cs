using HarmonyLib;

namespace Centromenu;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
public static class AmongUsClient_Update
{
    public static void Postfix()
    {
        CentromenuSpoof.SpoofLevel();

        // GuestMode cheats are commented out as they are broken in latest updates

        // Code to treat temp accounts the same as full accounts, including access to friend codes
        // if (!EOSManager.Instance.loginFlowFinished || !Centromenu.guestMode.Value) return;
        // DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.LoggedIn;

        // if (!string.IsNullOrWhiteSpace(EOSManager.Instance.FriendCode)) return;
        // var friendCode = CentromenuSpoof.spoofFriendCode();
        // var editUsername = EOSManager.Instance.editAccountUsername;
        // editUsername.UsernameText.SetText(friendCode);
        // editUsername.SaveUsername();
        // EOSManager.Instance.FriendCode = friendCode;
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class AmongUsClient_OnGameJoined
{
    // Postfix patch of AmongUsClient.OnGameJoined to store the last joined game ID string
    public static string lastGameIdString = "";

    public static void Postfix(string gameIdString)
    {
        NameSpoofManager.ClearAll();
        lastGameIdString = gameIdString;
        LobbyHistory.Record(gameIdString);
        CentroAPI.OnGameJoined(gameIdString);
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
public static class AmongUsClient_CoStartGame
{
    public static void Postfix()
    {
        CentroAPI.OnGameStart();

        if (CheatToggles.logGameState)
            ConsoleUI.Log("Game started");
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class AmongUsClient_OnGameEnd
{
    public static void Postfix(EndGameResult endGameResult)
    {
        CentroAPI.OnGameEnd(endGameResult);

        if (CheatToggles.logGameState)
            ConsoleUI.Log($"Game ended with reason {endGameResult.GameOverReason}");
    }
}
