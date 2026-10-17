using HarmonyLib;
using System;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Centromenu;

[HarmonyPatch(typeof(BlockedWords), nameof(BlockedWords.CensorWords), typeof(string), typeof(bool))]
public static class BlockedWords_CensorWords
{
    public static bool Prefix(string chatText, ref string __result)
    {
        if (AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat) return true;

        __result = chatText;
        return false;
    }
}

[HarmonyPatch(typeof(BlockedWords), nameof(BlockedWords.CensorWordsAsync), typeof(string), typeof(Il2CppSystem.Action<string>))]
public static class BlockedWords_CensorWordsAsync
{
    public static bool Prefix(string chatText, Il2CppSystem.Action<string> onWordsCensoredCallback)
    {
        if (AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat) return true;

        onWordsCensoredCallback?.Invoke(chatText);
        return false;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class ChatController_AddChat
{


	public static bool Prefix(PlayerControl sourcePlayer, string chatText, bool censor, ChatController __instance)
    {

        if (!CheatToggles.seeGhosts || PlayerControl.LocalPlayer.Data.IsDead) return true;

        if (!sourcePlayer || !PlayerControl.LocalPlayer) return true;

		NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
		NetworkedPlayerInfo data2 = sourcePlayer.Data;

		if (data2 == null || data == null) return true;

		ChatBubble pooledBubble = __instance.GetPooledBubble();

		try
		{
			pooledBubble.transform.SetParent(__instance.scroller.Inner);
			pooledBubble.transform.localScale = Vector3.one;
			bool flag = sourcePlayer == PlayerControl.LocalPlayer;
			if (flag)
			{
				pooledBubble.SetRight();
			}
			else
			{
				pooledBubble.SetLeft();
			}
			bool didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
			pooledBubble.SetCosmetics(data2);
			__instance.SetChatBubbleName(pooledBubble, data2, data2.IsDead, didVote, PlayerNameColor.Get(data2), null);
			if (censor && AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat)
			{
				chatText = BlockedWords.CensorWords(chatText, false);
			}
			pooledBubble.SetText(chatText);
			pooledBubble.AlignChildren();
			__instance.AlignAllBubbles();
			if (!__instance.IsOpenOrOpening && __instance.notificationRoutine == null)
			{
				__instance.notificationRoutine = __instance.StartCoroutine(__instance.BounceDot());
			}
			if (!flag && !__instance.IsOpenOrOpening)
			{
				SoundManager.Instance.PlaySound(__instance.messageSound, false).pitch = 0.5f + sourcePlayer.PlayerId / 15f;
				__instance.chatNotification.SetUp(sourcePlayer, chatText);
			}
		}
		catch (Exception message)
		{
			ChatController.Logger.Error(message.ToString(), null);
			__instance.chatBubblePool.Reclaim(pooledBubble);
		}

        return false;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatController_Update
{

    public static void Postfix(ChatController __instance)
    {
        if (CheatToggles.noChatDelay)
        {



            AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat = false;


            if (__instance.timeSinceLastMessage < 3f)
            {
                __instance.timeSinceLastMessage = 3f;
            }
        }






        if (CheatToggles.longerMessages)
		{

            __instance.freeChatField.textArea.characterLimit = 120;
        }
		else
		{
            __instance.freeChatField.textArea.characterLimit = 100;
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class ChatController_SendChat
{

    public static void Postfix(ChatController __instance)
    {
        if (!CheatToggles.lowerRateLimits) return;

		if (__instance.timeSinceLastMessage == 0f)
		{

			__instance.timeSinceLastMessage += 1f;
		}
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
public static class ChatController_SendFreeChat
{

    public static void Prefix(ChatController __instance)
    {

        if (!CheatToggles.bypassUrlBlock) return;

		string text = __instance.freeChatField.Text;


        string modifiedText = CensorUrlsAndEmails(text);




        if (modifiedText != text)
        {
            __instance.freeChatField.textArea.SetText(modifiedText);
        }

    }

    private static string CensorUrlsAndEmails(string text)
    {

        string pattern = @"(http[s]?://)?([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}(/[\w-./?%&=]*)?|([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)";
        Regex regex = new Regex(pattern);


        return regex.Replace(text, match =>
        {
            var censored = match.Value;
            censored = censored.Replace('.', ',');
            return censored;
        });
    }
}
