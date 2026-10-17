using HarmonyLib;

namespace Centromenu;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
public static class AmongUsClient_Update
{
    public static void Postfix()
    {
        CentromenuSpoof.SpoofLevel();













    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class AmongUsClient_OnGameJoined
{

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
