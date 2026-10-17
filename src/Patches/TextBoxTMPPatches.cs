using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace Centromenu;

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
public static class TextBoxTMP_Update
{

    public static void Postfix(TextBoxTMP __instance)
    {
        if (!CheatToggles.unlockClipboard || !__instance.hasFocus) return;

        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            GUIUtility.systemCopyBuffer = __instance.text;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            Utils.isPastingInput = true;

            __instance.SetText(__instance.text + GUIUtility.systemCopyBuffer);

            Utils.isPastingInput = false;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            GUIUtility.systemCopyBuffer = __instance.text;
            __instance.SetText("");
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
public static class TextBoxTMP_IsCharAllowed
{

    public static bool Prefix(TextBoxTMP __instance, char i, ref bool __result)
    {



        string compositionString = Input.compositionString;
        if (compositionString.Length > 0)
        {
            __result = true;
            return false;
        }

        if (CheatToggles.unlockCharacters)
        {

            HashSet<char> blockedSymbols = new() { '\b', '\r', '>', '<', '[' };

            if (blockedSymbols.Contains(i))
            {
                __result = false;
                return false;
            }

            __result = true;
        }
        else
        {
            if (__instance.IpMode)
            {
                __result = (i >= '0' && i <= '9') || i == '.';
                return false;
            }

            __result = i == ' ' ||
            (i >= 'A' && i <= 'Z') ||
            (i >= 'a' && i <= 'z') ||
            (i >= '0' && i <= '9') ||
            (i >= 'À' && i <= 'ÿ') ||
            (i >= 'Ѐ' && i <= 'џ') ||
            (i >= '぀' && i <= '㆟') ||
            (i >= 'ⱡ' && i <= '힣') ||
            (__instance.AllowSymbols && TextBoxTMP.SymbolChars.Contains(i)) ||
            (__instance.AllowEmail && TextBoxTMP.EmailChars.Contains(i));
        }

        return false;
    }
}
