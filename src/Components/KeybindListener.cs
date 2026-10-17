using UnityEngine;

namespace Centromenu;

public class KeybindListener : MonoBehaviour
{
    public void Update()
    {
        if (Centromenu.isPanicked) return;

        if (Input.GetKeyDown(Utils.StringToKeycode(Centromenu.panicKeybind.Value)))
        {
            Utils.Panic();
            return;
        }


        if (!CheatToggles.allowKeybindsWhileChatting &&
            HudManager.InstanceExists && HudManager.Instance.Chat && HudManager.Instance.Chat.IsOpenOrOpening) return;


        foreach (var (name, key) in CheatToggles.Keybinds)
        {
            if (key == KeyCode.None) continue;
            if (!Input.GetKeyDown(key)) continue;

            if (!CheatToggles.ToggleFields.TryGetValue(name, out var field)) continue;

            var current = (bool)field.GetValue(null);
            field.SetValue(null, !current);
        }
    }
}
