using System;
using UnityEngine;
using InnerNet;
using System.Linq;
using Il2CppSystem.Collections.Generic;
using System.IO;
using Hazel;
using System.Reflection;
using System.Text;
using AmongUs.GameOptions;
using BepInEx;
using HarmonyLib;
using UnityEngine.SceneManagement;
using Sentry.Internal.Extensions;
using System.Runtime.CompilerServices;
using AmongUs.InnerNet.GameDataMessages;
using Il2CppInterop.Runtime.Injection;

namespace Centromenu;

public static class Utils
{
    public static bool isPastingInput;
    public static ReferenceDataManager ReferenceDataManager = ReferenceDataManager.Instance;
    public static SabotageSystemType SabotageSystem => ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
    public static bool isShip => ShipStatus.Instance;
    public static bool isClient => AmongUsClient.Instance;
    public static bool isLobby => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined && !isFreePlay;
    public static bool isOnlineGame => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
    public static bool isLocalGame => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
    public static bool isFreePlay => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool isPlayer => PlayerControl.LocalPlayer;
    public static bool isHost => AmongUsClient.Instance && AmongUsClient.Instance.AmHost;
    public static bool isInGame => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && isPlayer;
    public static bool isMeeting => MeetingHud.Instance;
    public static bool isMeetingVoting => isMeeting && MeetingHud.Instance.state is MeetingHud.MeetingStates.Voted or MeetingHud.MeetingStates.NotVoted;
    public static bool isMeetingProceeding => isMeeting && MeetingHud.Instance.state is MeetingHud.MeetingStates.Proceeding;
    public static bool isExiling => ExileController.Instance && !(isAirshipMap && SpawnInMinigame.Instance.isActiveAndEnabled);
    public static bool isAnySabotageActive => ShipStatus.Instance && SabotageSystem.AnyActive;
    public static bool isNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;
    public static bool isHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;
    public static bool isSkeldMap => (MapNames)GetCurrentMapID() == MapNames.Skeld;
    public static bool isMiraHQMap => (MapNames)GetCurrentMapID() == MapNames.MiraHQ;
    public static bool isPolusMap => (MapNames)GetCurrentMapID() == MapNames.Polus;
    public static bool isDleksMap => (MapNames)GetCurrentMapID() == MapNames.Dleks;
    public static bool isAirshipMap => (MapNames)GetCurrentMapID() == MapNames.Airship;
    public static bool isFungleMap => (MapNames)GetCurrentMapID() == MapNames.Fungle;
    public const float DefaultSpeed = 2.5f;
    public const float DefaultGhostSpeed = 3f;


    public static bool IsSpeedDefault(bool forGhost = false)
    {
        return forGhost ? Mathf.Approximately(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed, DefaultGhostSpeed) :
            Mathf.Approximately(PlayerControl.LocalPlayer.MyPhysics.Speed, DefaultSpeed);
    }


    public static void SnapSpeedToDefault(float snapRange, bool forGhost = false)
    {
        if (forGhost)
        {
            PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed - DefaultGhostSpeed)
                                                             < snapRange ? DefaultGhostSpeed : PlayerControl.LocalPlayer.MyPhysics.GhostSpeed;
        }
        else
        {
            PlayerControl.LocalPlayer.MyPhysics.Speed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed - DefaultSpeed)
                                                        < snapRange ? DefaultSpeed : PlayerControl.LocalPlayer.MyPhysics.Speed;
        }
    }


    public static (string realName, string displayName, bool isDisguised) GetPlayerIdentity(PlayerControl player)
    {
        if (player == null || player.Data == null) return ("", "", false);

        var realName = $"<color=#{ColorUtility.ToHtmlStringRGB(player.Data.Color)}>{player.Data.PlayerName}</color>";
        var displayName = $"<color=#{ColorUtility.ToHtmlStringRGB(Palette.PlayerColors[player.CurrentOutfit.ColorId])}>{player.CurrentOutfit.PlayerName}</color>";
        var isDisguised = player.CurrentOutfit.PlayerName != player.Data.PlayerName;

        return (realName, displayName, isDisguised);
    }


    public static bool IsVanished(NetworkedPlayerInfo playerInfo)
    {
        PhantomRole phantomRole = playerInfo.Role as PhantomRole;

        if (phantomRole != null)
        {
            return phantomRole.fading || phantomRole.isInvisible;
        }

        return false;
    }


    public static bool IsValidTarget(NetworkedPlayerInfo target)
    {
        var killAnyoneRequirements = target && !target.Disconnected && target.Object.Visible && target.PlayerId != PlayerControl.LocalPlayer.PlayerId && target.Role && target.Object;

        var fullRequirements = killAnyoneRequirements && !target.IsDead && !target.Object.inVent && !target.Object.inMovingPlat && target.Role.CanBeKilled;

        return CheatToggles.killAnyone ? killAnyoneRequirements : fullRequirements;
    }

    public static List<NetworkedPlayerInfo> GetAllPlayerData()
    {
        var playerDataList = new List<NetworkedPlayerInfo>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null)
            {
                playerDataList.Add(player.Data);
            }
        }

        return playerDataList;
    }



    public static void AdjustResolution()
    {
        ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
    }


    public static RoleBehaviour GetBehaviourByRoleType(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.ToArray().First(r => r.Role == roleType);
    }


    public static RoleBehaviour GetBehaviourByTeamType(RoleTeamTypes roleTeamType)
    {
        RoleTypes roleType = (RoleTypes)Enum.Parse(typeof(RoleTypes), roleTeamType.ToString(), true);
        RoleBehaviour role = GetBehaviourByRoleType(roleType);

        return role;
    }

    public static void ForceSetScanner(PlayerControl player, bool toggle)
    {
        var count = ++player.scannerCount;
        player.SetScanner(toggle, count);
        RpcSetScannerMessage rpcMessage = new(player.NetId, toggle, count);
        AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
    }

    public static void ForcePlayAnimation(byte animationType)
    {



        PlayerControl.LocalPlayer.PlayAnimation(animationType);
        RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, animationType);
        AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
    }


    public static System.Collections.IEnumerator DelayedSnapTo(Vector2 position, float delay = 0.25f)
    {
        yield return new WaitForSeconds(delay);
        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
    }


    public static void MurderPlayer(PlayerControl target, MurderResultFlags result)
    {
        if (isFreePlay)
        {
            PlayerControl.LocalPlayer.MurderPlayer(target, MurderResultFlags.Succeeded);
            return;
        }

        foreach (var item in PlayerControl.AllPlayerControls)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, AmongUsClient.Instance.GetClientIdFromCharacter(item));
            writer.WriteNetObject(target);
            writer.Write((int)result);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }


    public static void OpenChat()
    {
        if (!HudManager.Instance.Chat.IsOpenOrOpening)
        {
            HudManager.Instance.Chat.chatScreen.SetActive(true);
            PlayerControl.LocalPlayer.NetTransform.Halt();
            HudManager.Instance.Chat.StartCoroutine(HudManager.Instance.Chat.CoOpen());
            if (FriendsListManager.InstanceExists)
            {
                FriendsListManager.Instance.SetFriendButtonColor(true);
            }
            if (HudManager.Instance.Chat.chatNotification.gameObject.activeSelf)
			{
				HudManager.Instance.Chat.chatNotification.Close();
			}
        }

    }

    private const float EspLineWidth = 0.006f;


    public static void DrawTracer(GameObject sourceObject, GameObject targetObject, Color color)
    {
        var lineRenderer = sourceObject.GetComponent<LineRenderer>();

        if (!lineRenderer)
        {
            lineRenderer = sourceObject.AddComponent<LineRenderer>();
        }

        lineRenderer.SetVertexCount(2);
        lineRenderer.SetWidth(EspLineWidth, EspLineWidth);
        lineRenderer.useWorldSpace = true;


        Material material = HatManager.Instance.PlayerMaterial;

        lineRenderer.material = material;
        lineRenderer.SetColors(color, color);

        lineRenderer.SetPosition(0, sourceObject.transform.position);
        lineRenderer.SetPosition(1, targetObject.transform.position);
    }


    public static void DrawPlayerBox(GameObject playerObject, Color color)
    {
        const string playerBoxName = "Centromenu_ESPPlayerBox";

        var playerBoxTransform = playerObject.transform.Find(playerBoxName);
        LineRenderer lineRenderer;

        if (!playerBoxTransform)
        {
            var playerBoxObject = new GameObject(playerBoxName);
            playerBoxObject.transform.SetParent(playerObject.transform, false);
            lineRenderer = playerBoxObject.AddComponent<LineRenderer>();
        }
        else
        {
            lineRenderer = playerBoxTransform.gameObject.GetComponent<LineRenderer>();
        }

        lineRenderer.SetVertexCount(5);
        lineRenderer.SetWidth(EspLineWidth, EspLineWidth);
        lineRenderer.useWorldSpace = false;
        lineRenderer.material = HatManager.Instance.PlayerMaterial;
        lineRenderer.SetColors(color, color);


        lineRenderer.SetPosition(0, new Vector3(-0.45f, -0.50f, -0.01f));
        lineRenderer.SetPosition(1, new Vector3(-0.45f, 0.85f, -0.01f));
        lineRenderer.SetPosition(2, new Vector3(0.45f, 0.85f, -0.01f));
        lineRenderer.SetPosition(3, new Vector3(0.45f, -0.50f, -0.01f));
        lineRenderer.SetPosition(4, new Vector3(-0.45f, -0.50f, -0.01f));
    }


    public static bool IsChatUiActive()
    {
        try
        {
            return CheatToggles.enableChat || MeetingHud.Instance || !ShipStatus.Instance || PlayerControl.LocalPlayer.Data.IsDead;
        }
        catch
        {
            return false;
        }
    }



    public static int GetMaxRpcPackingLimit()
    {
        int num = 0;

        if (isClient && AmongUsClient.Instance.AmHost)
        {
            num = GameManager.Instance.LogicOptions.MaxPlayers * 2;
        }

        return 10 + num;
    }




    public static void Overload(int targetId, int strength)
    {
        if (strength < 1) return;

        int maxRpc = GetMaxRpcPackingLimit();

        uint netId = PlayerControl.LocalPlayer.MyPhysics.NetId;
        byte rpcCall = (byte)RpcCalls.Pet;

        if (strength <= maxRpc)
        {


            var messageWriter = MessageWriter.Get(SendOption.None);

            if (targetId < 0)
            {
                messageWriter.StartMessage(Tags.GameData);
                messageWriter.Write(AmongUsClient.Instance.GameId);
            }
            else
            {
                messageWriter.StartMessage(Tags.GameDataTo);
                messageWriter.Write(AmongUsClient.Instance.GameId);
                messageWriter.WritePacked(targetId);
            }

            for (var msg = 0; msg < strength; msg++)
            {
                messageWriter.StartMessage((byte)GameDataTypes.RpcFlag);

                messageWriter.WritePacked(netId);

                messageWriter.Write(rpcCall);




                NetHelpers.WriteVector2(PlayerControl.LocalPlayer.GetTruePosition(), messageWriter);




                messageWriter.Write((ushort)0);

                messageWriter.Write((ushort)0);

                messageWriter.EndMessage();
            }

            messageWriter.EndMessage();

            AmongUsClient.Instance.connection.Send(messageWriter);

            messageWriter.Recycle();
        }
        else
        {
            int strengthGroups = strength / maxRpc;
            int remainder = strength % maxRpc;

            for (int group = 0; group < strengthGroups; group++)
            {
                Overload(targetId, maxRpc);
            }

            Overload(targetId, remainder);
        }
    }


    public static void CloseChat()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening)
        {
            HudManager.Instance.Chat.ForceClosed();
        }
    }


    public static float GetDistanceBetween(PlayerControl source, PlayerControl target)
    {

        Vector2 vector = target.GetTruePosition() - source.GetTruePosition();
		float magnitude = vector.magnitude;

        return magnitude;

    }


    public static System.Collections.Generic.List<PlayerControl> GetPlayersSortedByDistance(PlayerControl source = null)
    {

        if (source.IsNull())
        {
            source = PlayerControl.LocalPlayer;
        }

        System.Collections.Generic.List<PlayerControl> outputList = new System.Collections.Generic.List<PlayerControl>();

        outputList.Clear();

        var allPlayers = GameData.Instance.AllPlayers;
        foreach (var playerInfo in allPlayers)
        {
            var player = playerInfo.Object;
            if (player)
            {
                outputList.Add(player);
            }
        }

        outputList = outputList.OrderBy(target => GetDistanceBetween(source, target)).ToList();

        return outputList.Count <= 0 ? null : outputList;
    }


    public static byte GetCurrentMapID()
    {

        if (isFreePlay)
        {
            return (byte)AmongUsClient.Instance.TutorialMapId;
        }


        if (GameOptionsManager.Instance?.currentGameOptions != null)
        {
            return GameOptionsManager.Instance.currentGameOptions.MapId;
        }


        return byte.MaxValue;
    }


    public static SystemTypes GetCurrentRoom()
    {
        return HudManager.Instance.roomTracker.LastRoom.RoomId;
    }


    public static PlainShipRoom GetRoomFromPosition(Vector2 position)
    {
        return ShipStatus.Instance == null ? null : ShipStatus.Instance.AllRooms.FirstOrDefault(
            room => room != null && room.roomArea != null && room.roomArea.OverlapPoint(position));
    }


    public static string GetColoredPingText(string pingText, int ping)
    {
        return ping switch
        {
            < 1 => $"<color=#b8b8b8>{pingText}</color>",
            < 100 => $"<color=#00ff00ff>{pingText}</color>",
            < 400 => $"<color=#ffff00ff>{pingText}</color>",
            _ => $"<color=#ff0000ff>{pingText}</color>"
        };
    }


    public static string GetGradientText(string text, Color firstColor, Color secondColor, int offset)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        if (text.Length == 1)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.Lerp(firstColor, secondColor, 0.5f))}>{text}</color>";
        }

        var output = new StringBuilder(text.Length * 26);
        var cycleLength = 2 * (text.Length - 1);

        for (var i = 0; i < text.Length; i++)
        {
            var cycle = (i + offset) % cycleLength;
            var amount = cycle <= text.Length - 1
                ? cycle / (float)(text.Length - 1)
                : (cycleLength - cycle) / (float)(text.Length - 1);

            var color = Color.Lerp(firstColor, secondColor, amount);
            output.Append("<color=#");
            output.Append(ColorUtility.ToHtmlStringRGBA(color));
            output.Append('>');
            output.Append(text[i]);
            output.Append("</color>");
        }

        return output.ToString();
    }



    public static string GetGradientText(
        string text,
        Color firstColor,
        Color secondColor,
        Color thirdColor,
        float offset)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var output = new StringBuilder(text.Length * 26);
        var cycleLength = Mathf.Max(text.Length, 3);

        for (var i = 0; i < text.Length; i++)
        {
            var phase = Mathf.Repeat((i + offset) / (float)cycleLength, 1f) * 3f;
            Color color;

            if (phase < 1f)
                color = Color.Lerp(firstColor, secondColor, phase);
            else if (phase < 2f)
                color = Color.Lerp(secondColor, thirdColor, phase - 1f);
            else
                color = Color.Lerp(thirdColor, firstColor, phase - 2f);

            output.Append("<color=#");
            output.Append(ColorUtility.ToHtmlStringRGBA(color));
            output.Append('>');
            output.Append(text[i]);
            output.Append("</color>");
        }

        return output.ToString();
    }


    public static string GetAnimatedCentromenuVersion(int offset)
    {
        string menuText = GetGradientText(
            "Centromenu",
            new Color32(150, 10, 35, 255),
            new Color32(205, 28, 55, 255),
            offset
        );
        string versionText = GetGradientText(
            $"v{Centromenu.centromenuVersion}",
            new Color32(88, 6, 20, 255),
            new Color32(150, 16, 38, 255),
            offset
        );

        return $"{menuText}  {versionText}";
    }


    public static int GetFps()
    {
        return (int)(1f / Time.unscaledDeltaTime);
    }


    public static KeyCode StringToKeycode(string keyCodeStr)
    {

        if(!string.IsNullOrEmpty(keyCodeStr))
        {
            try
            {

                KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), keyCodeStr, true);

                return keyCode;

            }

            catch { }
        }

        return KeyCode.Delete;
    }


    public static bool StringToPlatformType(string platformStr, out Platforms? platform)
    {
        if (!string.IsNullOrEmpty(platformStr))
        {
            try
            {

                platform = (Platforms)Enum.Parse(typeof(Platforms), platformStr, true);

                return true;
            }catch{}
        }

        platform = null;
        return false;
    }

    public static string PlatformTypeToString(Platforms platform)
    {
        return platform switch
        {
            Platforms.StandaloneEpicPC => "Epic Games",
            Platforms.StandaloneSteamPC => "Steam",
            Platforms.StandaloneMac => "Mac",
            Platforms.StandaloneWin10 => "Microsoft Store",
            Platforms.StandaloneItch => "Itch.io",
            Platforms.IPhone => "iPhone / iPad",
            Platforms.Android => "Android",
            Platforms.Switch => "Nintendo Switch",
            Platforms.Xbox => "Xbox",
            Platforms.Playstation => "PlayStation",
            (Platforms)112 => "Starlight",
            _ => "Unknown"
        };
    }

    public static string GetPlayerPlatform(NetworkedPlayerInfo playerInfo)
    {
        Platforms platform = Platforms.Unknown;

        try
        {
            var client = AmongUsClient.Instance?.GetClientFromPlayerInfo(playerInfo);
            if (client != null) platform = client.PlatformData.Platform;
        }
        catch { }

        bool isLocalPlayer = false;
        try
        {
            isLocalPlayer = playerInfo?.Object && playerInfo.Object.AmOwner;
            if (isLocalPlayer && platform == Platforms.Unknown)
                platform = Constants.GetPlatformData().Platform;
        }
        catch { }

        string label = PlatformTypeToString(platform);
        if (label != "Unknown" || !isLocalPlayer) return label;



        return Application.platform switch
        {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "Windows PC",
            RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => "Mac",
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor => "Linux PC",
            RuntimePlatform.IPhonePlayer => "iPhone / iPad",
            RuntimePlatform.Android => "Android",
            _ => "Unknown"
        };
    }



    public static string GetRoleName(NetworkedPlayerInfo playerData)
    {
        var translatedRole = TranslationController.Instance.GetString(playerData.Role.StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
        if (translatedRole != "STRMISS") return translatedRole;

        translatedRole = TranslationController.Instance.GetString(GetBehaviourByTeamType(playerData.Role.TeamType).StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
        return translatedRole;
    }


    public static string GetNameTag(NetworkedPlayerInfo playerInfo, string playerName, bool isChat = false, bool isMatchInfo = false)
    {
        if (playerInfo.IsNull()) return playerName;

        var nameTag = NameSpoofManager.GetDisplayName(playerInfo, playerName, isChat);

        if (playerInfo.Role.IsNull() || playerInfo.Disconnected ||
            playerInfo.Object.CurrentOutfit.IsNull()) return nameTag;

        var player = AmongUsClient.Instance.GetClientFromPlayerInfo(playerInfo);
        var host = AmongUsClient.Instance.GetHost();
        var level = playerInfo.PlayerLevel + 1;

        var platform = GetPlayerPlatform(playerInfo);




        var roleColor = ColorUtility.ToHtmlStringRGB(playerInfo.Role.TeamColor);

        var hostString = player == host ? "Host - " : "";

        if (CheatToggles.seeRoles)
        {

            if (CheatToggles.seePlayerInfo)
            {
                if (isChat)
                {
                    nameTag = $"<color=#{roleColor}>{nameTag} <size=70%>{GetRoleName(playerInfo)}</size></color> <size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>";
                    return nameTag;
                }

                if (isMatchInfo)
                {
                    nameTag = $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n<color=#{roleColor}>{nameTag} <size=70%>{GetRoleName(playerInfo)}</size></color>";
                    return nameTag;
                }

                nameTag = $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n<color=#{roleColor}><size=70%>{GetRoleName(playerInfo)}</size>\r\n{nameTag}</color>";
            }
            else
            {
                if (isChat || isMatchInfo)
                {
                    nameTag = $"<color=#{roleColor}>{nameTag} <size=70%>{GetRoleName(playerInfo)}</size></color>";
                    return nameTag;
                }

                nameTag = $"<color=#{roleColor}><size=70%>{GetRoleName(playerInfo)}</size>\r\n{nameTag}</color>";
            }
        }
        else
        {
            if (CheatToggles.seePlayerInfo)
            {
                if (PlayerControl.LocalPlayer.Data.Role.NameColor == playerInfo.Role.NameColor)
                {
                    if (isChat)
                    {
                        nameTag = $"<color=#{ColorUtility.ToHtmlStringRGB(playerInfo.Role.NameColor)}>{nameTag}</color> <size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>";
                        return nameTag;
                    }

                    if (isMatchInfo)
                    {
                        nameTag = $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n{nameTag}";
                        return nameTag;
                    }

                    nameTag = $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n<color=#{ColorUtility.ToHtmlStringRGB(playerInfo.Role.NameColor)}>{nameTag}";
                }
                else
                {
                    if (isChat)
                    {
                        nameTag = $"{nameTag} <size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>";
                        return nameTag;
                    }

                    nameTag = $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n{nameTag}";
                }
            }
            else
            {
                if (PlayerControl.LocalPlayer.Data.Role.NameColor != playerInfo.Role.NameColor || isMatchInfo) return nameTag;

                nameTag = $"<color=#{ColorUtility.ToHtmlStringRGB(playerInfo.Role.NameColor)}>{nameTag}</color>";
            }
        }

        return nameTag;
    }


    public static NetworkedPlayerInfo GetPlayerDataFromClientId(int clientId)
    {
        var players = PlayerControl.AllPlayerControls.ToArray();

        for (int i = 0; i < players.Count; i++)
		{   NetworkedPlayerInfo playerData = players[i].Data;

			if (playerData.ClientId == clientId)
			{
				return playerData;
			}
		}

        return null;
    }


    public static string GetRandomName()
    {
        var length = UnityEngine.Random.Range(1, 13);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
    }


    public static int GetPing()
    {
        if (isClient && AmongUsClient.Instance.AmClient)
        {
            return AmongUsClient.Instance.Ping;
        }
        else
        {
            return 0;
        }
    }



    public static void ShowPopup(string text)
    {
        var popup = UnityEngine.Object.Instantiate(DiscordManager.Instance.discordPopup, Camera.main!.transform);

        var background = popup.transform.Find("Background").GetComponent<SpriteRenderer>();
        var size = background.size;
        size.x *= 2.5f;
        background.size = size;

        popup.TextAreaTMP.fontSizeMin = 2;
        popup.Show(text);
    }

    public static void ShowNewPopup(string text)
    {
        DisconnectPopup.Instance.ShowCustom(text);
    }



    public static Dictionary<string, Sprite> CachedSprites = new();
    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;

            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            Centromenu.Log.LogError($"Failed to read Texture: {path}");
        }
        return null;
    }



    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();

            stream.CopyTo(ms);
            ImageConversion.LoadImage(texture, ms.ToArray(), false);
            return texture;
        }
        catch
        {
            Centromenu.Log.LogError($"Failed to read Texture: {path}");
        }
        return null;
    }


    public static void OpenConfigFile()
    {
        var configFilePath = Centromenu.Plugin.Config.ConfigFilePath;
        var configEditor = Centromenu.configEditor.Value;

        if (!string.IsNullOrWhiteSpace(configEditor))
        {
            if (File.Exists(configFilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = configEditor,
                        Arguments = configFilePath,
                        UseShellExecute = true

                    });
                }
                catch (Exception ex)
                {
                    Centromenu.Log.LogError(ex.Message);
                }
            }
            else
            {
                Centromenu.Log.LogError("Configuration file does not exist");
            }
        }
        else
        {
            Centromenu.Log.LogError("Configuration editor not specified");
        }
    }

    public class PanicCleaner : MonoBehaviour
    {

        public static void Create()
        {
            ClassInjector.RegisterTypeInIl2Cpp<PanicCleaner>();
            var go = new GameObject("Centromenu_PanicCleaner");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<PanicCleaner>();
        }



        private void LateUpdate()
        {
            try { Harmony.UnpatchID(Centromenu.Id); } catch { }
            Destroy(gameObject);
        }
    }

    public static void Panic()
    {
        Centromenu.isPanicked = true;




        if (HudManager.InstanceExists && HudManager.Instance)
        {
            HudManager.Instance.SetHudActive(true);
        }

        CheatToggles.DisableAll();

        var stamp = ModManager.Instance.ModStamp;
        if (stamp) stamp.enabled = false;

        Scene scene = SceneManager.GetActiveScene();

        if (scene.name == "MainMenu" || scene.name == "MatchMaking")
        {
            SceneManager.LoadScene(scene.name);
        }

        UnityEngine.Object.Destroy(Centromenu.menuUI);

        UnityEngine.Object.Destroy(Centromenu.consoleUI);
        UnityEngine.Object.Destroy(Centromenu.doorsUI);
        UnityEngine.Object.Destroy(Centromenu.tasksUI);
        UnityEngine.Object.Destroy(Centromenu.protectUI);
        UnityEngine.Object.Destroy(Centromenu.modDetectorUI);



        UnityEngine.Object.Destroy(Centromenu.keybindListener);

        PanicCleaner.Create();
    }
}
