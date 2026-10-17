using UnityEngine;
using Il2CppSystem.Collections.Generic;
using Sentry.Internal.Extensions;

namespace Centromenu;
public static class PlayerPickMenu
{
    public static ShapeshifterMinigame playerpickMenu;
    public static bool isActive;
    public static NetworkedPlayerInfo targetPlayerData;
    public static Il2CppSystem.Action customAction;
    public static List<NetworkedPlayerInfo> customPlayerList;



    public static ShapeshifterMinigame GetShapeshifterMenu()
    {
        var rolePrefab = Utils.GetBehaviourByRoleType(AmongUs.GameOptions.RoleTypes.Shapeshifter);
        return Object.Instantiate(rolePrefab?.Cast<ShapeshifterRole>(), GameData.Instance.transform).ShapeshifterMenu;
    }


    public static void OpenPlayerPickMenu(List<NetworkedPlayerInfo> playerList, Il2CppSystem.Action action)
    {
        isActive = true;
        customPlayerList = playerList;
        customAction = action;


        playerpickMenu = Object.Instantiate(GetShapeshifterMenu(), Camera.main.transform, false);

        playerpickMenu.transform.localPosition = new Vector3(0f, 0f, -50f);
		playerpickMenu.Begin(null);
    }


    public static NetworkedPlayerInfo CustomPPMChoice(string name, NetworkedPlayerInfo.PlayerOutfit outfit, RoleBehaviour role = null)
    {
        NetworkedPlayerInfo customChoice = Object.Instantiate<NetworkedPlayerInfo>(GameData.Instance.PlayerInfoPrefab);

        outfit.PlayerName = name;

        customChoice.Outfits[PlayerOutfitType.Default] = outfit;

        if (!role.IsNull())
        {
            customChoice.Role = role;
        }

        return customChoice;
    }
}
