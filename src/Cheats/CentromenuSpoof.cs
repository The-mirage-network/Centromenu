using AmongUs.Data;

namespace Centromenu;
public static class CentromenuSpoof
{
    public static void SpoofLevel()
    {
        // Parse Spoofing.Level config entry and turn it into a uint
        if (!string.IsNullOrEmpty(Centromenu.spoofLevel.Value) &&
            uint.TryParse(Centromenu.spoofLevel.Value, out uint parsedLevel) &&
            parsedLevel != DataManager.Player.Stats.Level)
        {

            // Store the spoofed level using DataManager
            DataManager.Player.stats.level = parsedLevel - 1;
            DataManager.Player.Save();
        }
    }

    public static string SpoofFriendCode()
    {
        string friendCode = Centromenu.guestFriendCode.Value;
        if (string.IsNullOrWhiteSpace(friendCode))
        {
            friendCode = AccountManager.Instance.GetRandomName();
        }
        return friendCode;
    }
}
