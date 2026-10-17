using UnityEngine;

namespace Centromenu;

public class ChatTab : ITab
{
    private const float SpamIntervalSeconds = 3.2f;
    private static string _automatedMessage = string.Empty;
    private static bool _sendRequested;
    private static bool _isEditingMessage;
    private static float _nextSpamTime;

    public string name => "Chat";

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawTextbox();

        GUILayout.Space(15);

        DrawAutomation();

        GUILayout.EndVertical();
    }

    private void DrawGeneral()
    {
        CheatToggles.enableChat = GUILayout.Toggle(CheatToggles.enableChat, " Enable Chat");

        CheatToggles.bypassUrlBlock = GUILayout.Toggle(CheatToggles.bypassUrlBlock, " Bypass URL Block");

        CheatToggles.lowerRateLimits = GUILayout.Toggle(CheatToggles.lowerRateLimits, " Lower Rate Limits");

        CheatToggles.noChatDelay = GUILayout.Toggle(CheatToggles.noChatDelay, " No chat delay");
    }

    private void DrawTextbox()
    {
        GUILayout.Label("Textbox", GUIStylePreset.TabSubtitle);

        CheatToggles.unlockCharacters = GUILayout.Toggle(CheatToggles.unlockCharacters, " Unlock Extra Characters");

        CheatToggles.longerMessages = GUILayout.Toggle(CheatToggles.longerMessages, " Allow Longer Messages");

        CheatToggles.unlockClipboard = GUILayout.Toggle(CheatToggles.unlockClipboard, " Unlock Clipboard");
    }

    private void DrawAutomation()
    {
        GUILayout.Label("Chat Automation", GUIStylePreset.TabSubtitle);

        var displayedMessage = string.IsNullOrEmpty(_automatedMessage)
            ? "Click here to type a message"
            : _automatedMessage;
        if (_isEditingMessage) displayedMessage += " |";

        if (GUILayout.Button(
            displayedMessage,
            GUIStylePreset.TextField,
            GUILayout.Height(34f),
            GUILayout.ExpandWidth(true)
        ))
        {
            _isEditingMessage = true;
        }

        HandleMessageInput();

        if (GUILayout.Button("Execute", GUIStylePreset.NormalButton, GUILayout.Height(32f)))
        {
            _sendRequested = true;
        }

        CheatToggles.chatSpam = GUILayout.Toggle(CheatToggles.chatSpam, " Spam");
    }

    private static void HandleMessageInput()
    {
        if (!_isEditingMessage) return;

        var currentEvent = Event.current;
        if (currentEvent == null || currentEvent.type != EventType.KeyDown) return;

        if (currentEvent.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape)
        {
            _isEditingMessage = false;
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Backspace)
        {
            if (_automatedMessage.Length > 0)
            {
                _automatedMessage = _automatedMessage[..^1];
            }

            currentEvent.Use();
            return;
        }

        if (currentEvent.control && currentEvent.keyCode == KeyCode.V && CheatToggles.unlockClipboard)
        {
            AppendMessageText(GUIUtility.systemCopyBuffer);
            currentEvent.Use();
            return;
        }

        if (!char.IsControl(currentEvent.character))
        {
            AppendMessageText(currentEvent.character.ToString());
            currentEvent.Use();
        }
    }

    private static void AppendMessageText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        text = text.Replace('\r', ' ').Replace('\n', ' ');
        var characterLimit = CheatToggles.longerMessages ? 120 : 100;
        var availableCharacters = characterLimit - _automatedMessage.Length;
        if (availableCharacters <= 0) return;

        _automatedMessage += text.Length > availableCharacters
            ? text[..availableCharacters]
            : text;
    }

    public static void Tick()
    {
        if (_sendRequested)
        {
            _sendRequested = false;
            if (SendAutomatedMessage())
            {
                _nextSpamTime = Time.unscaledTime + SpamIntervalSeconds;
            }
        }

        if (!CheatToggles.chatSpam || Time.unscaledTime < _nextSpamTime) return;

        if (SendAutomatedMessage())
        {
            _nextSpamTime = Time.unscaledTime + SpamIntervalSeconds;
        }
    }

    private static bool SendAutomatedMessage()
    {
        if (string.IsNullOrWhiteSpace(_automatedMessage) ||
            !PlayerControl.LocalPlayer ||
            !HudManager.Instance ||
            !HudManager.Instance.Chat)
        {
            return false;
        }

        var chat = HudManager.Instance.Chat;
        var characterLimit = CheatToggles.longerMessages ? 120 : 100;
        var message = _automatedMessage.Length > characterLimit
            ? _automatedMessage[..characterLimit]
            : _automatedMessage;



        chat.freeChatField.textArea.SetText(message);
        chat.SendFreeChat();
        return true;
    }
}
