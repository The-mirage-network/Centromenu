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
	// Prefix patch of ChatController.AddChat to receive ghost messages if CheatSettings.seeGhosts is enabled even if LocalPlayer is alive
	// Basically does what the original method did with the required modifications
	public static bool Prefix(PlayerControl sourcePlayer, string chatText, bool censor, ChatController __instance)
    {
		// Simply run original method if seeGhosts is disabled or LocalPlayer already dead
        if (!CheatToggles.seeGhosts || PlayerControl.LocalPlayer.Data.IsDead) return true;

        if (!sourcePlayer || !PlayerControl.LocalPlayer) return true;

		NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
		NetworkedPlayerInfo data2 = sourcePlayer.Data;

		if (data2 == null || data == null) return true; // Remove isDead check for LocalPlayer

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

        return false; // Skips the original method completly
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatController_Update
{
    // Postfix patch of ChatController.Update to unlock longer message length
    public static void Postfix(ChatController __instance)
    {
        if (CheatToggles.noChatDelay)
        {
            // Keep censorship disabled while no-chat-delay is active. The game can
            // restore this preference when settings are reloaded, so enforce it
            // from the chat update instead of only changing it once in the menu.
            AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat = false;

            // Keep chat ready between sends without modifying state inside SendChat.
            if (__instance.timeSinceLastMessage < 3f)
            {
                __instance.timeSinceLastMessage = 3f;
            }
        }

        //__instance.freeChatField.textArea.allowAllCharacters = CheatToggles.chatJailbreak; // Not really used by the game's code, but I include it anyway
        //__instance.freeChatField.textArea.AllowSymbols = true; // Allow sending certain symbols
        //__instance.freeChatField.textArea.AllowEmail = CheatToggles.chatJailbreak; // Allow sending email addresses when chatJailbreak is enabled
        //__instance.freeChatField.textArea.AllowPaste = CheatToggles.chatJailbreak; // Allow pasting from clipboard in chat when chatJailbreak is enabled

        if (CheatToggles.longerMessages)
		{
			// Increasing the maximum length by 20 characters still avoids anticheat kicks
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
    // Postfix patch of ChatController.SendChat to unlock lower chat rate limits
    public static void Postfix(ChatController __instance)
    {
        if (!CheatToggles.lowerRateLimits) return;

		if (__instance.timeSinceLastMessage == 0f)
		{
			// Decreasing rate limit by 1 sec max still avoids anticheat kicks
			__instance.timeSinceLastMessage += 1f;
		}
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
public static class ChatController_SendFreeChat
{
    // Prefix patch of ChatController.SendFreeChat to allow sending URLs without being censored
    public static void Prefix(ChatController __instance)
    {
		// Only works if CheatSettings.bypassUrlBlock is enabled
        if (!CheatToggles.bypassUrlBlock) return;

		string text = __instance.freeChatField.Text;

        // Replace periods in URLs and email addresses with commas to avoid censorship
        string modifiedText = CensorUrlsAndEmails(text);

        // Keep the game's normal send path intact. Calling RpcSendChat here and
        // skipping SendFreeChat bypassed its input/cooldown cleanup, which could
        // make one submit send the same message more than once.
        if (modifiedText != text)
        {
            __instance.freeChatField.textArea.SetText(modifiedText);
        }

    }

    private static string CensorUrlsAndEmails(string text)
    {
        // Regular expression pattern to match URLs and email addresses
        string pattern = @"(http[s]?://)?([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}(/[\w-./?%&=]*)?|([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)";
        Regex regex = new Regex(pattern);

        // Censor periods in each match
        return regex.Replace(text, match =>
        {
            var censored = match.Value;
            censored = censored.Replace('.', ',');
            return censored;
        });
    }
}
