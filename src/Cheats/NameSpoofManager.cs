using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Centromenu;

/// <summary>
/// Client-only display-name overrides. Nothing in this class changes GameData or
/// sends an RPC, so spoofed names are visible only to the local Centromenu user.
/// </summary>
public static class NameSpoofManager
{
    private const string OwnerPlayerName = "Centro404";
    private const string OwnerDisplayName = "OWNER OF CENTROMENU - Centroscape";
    private const string OwnerChatName = "Centroscape";

    public enum ColorMode
    {
        Solid,
        Rainbow,
        Animate
    }

    private sealed class NameSpoof
    {
        public string Name;
        public ColorMode Mode;
        public Color BaseColor;
    }

    private static readonly Dictionary<byte, NameSpoof> Spoofs = new();

    public static void Set(byte playerId, string name, ColorMode mode, Color baseColor)
    {
        Spoofs[playerId] = new NameSpoof
        {
            Name = name,
            Mode = mode,
            BaseColor = baseColor
        };
    }

    public static bool Remove(byte playerId)
    {
        return Spoofs.Remove(playerId);
    }

    public static bool HasSpoof(byte playerId)
    {
        return Spoofs.ContainsKey(playerId);
    }

    public static void ClearAll()
    {
        Spoofs.Clear();
    }

    public static string GetDisplayName(NetworkedPlayerInfo playerInfo, string fallbackName, bool compact = false)
    {
        // Every Centro client renders the project owner's display name locally
        // without changing GameData or sending a name-changing RPC.
        if (IsOwner(playerInfo, fallbackName))
        {
            int gradientOffset = Mathf.FloorToInt(Time.unscaledTime * 10f);
            string ownerName = Utils.GetGradientText(
                compact ? OwnerChatName : OwnerDisplayName,
                new Color32(0x7d, 0x00, 0x00, 0xff),
                new Color32(0x9c, 0x1f, 0x1f, 0xff),
                new Color32(0xe3, 0x22, 0x22, 0xff),
                gradientOffset
            );

            // Chat bubbles only reserve a narrow strip beside the portrait for
            // the sender. Use the compact owner name there so the message keeps
            // the game's normal layout; the full title remains above the player.
            return ownerName;
        }

        if (playerInfo == null || !Spoofs.TryGetValue(playerInfo.PlayerId, out var spoof))
            return fallbackName;

        string safeName = EscapeRichText(spoof.Name);
        if (string.IsNullOrEmpty(safeName)) return fallbackName;

        switch (spoof.Mode)
        {
            case ColorMode.Rainbow:
            {
                // A slow, uniform color transmission across the entire name.
                float hue = Mathf.Repeat(Time.unscaledTime * 0.08f, 1f);
                return WrapColor(safeName, Color.HSVToRGB(hue, 0.82f, 1f));
            }
            case ColorMode.Animate:
                return BuildAnimatedGradient(safeName, spoof.BaseColor);
            default:
                return WrapColor(safeName, spoof.BaseColor);
        }
    }

    public static bool IsOwner(NetworkedPlayerInfo playerInfo, string fallbackName = null)
    {
        if (MatchesOwnerName(fallbackName)) return true;
        if (playerInfo == null) return false;

        return MatchesOwnerName(playerInfo.PlayerName) ||
               MatchesOwnerName(playerInfo.DefaultOutfit.PlayerName);
    }

    private static bool MatchesOwnerName(string playerName)
    {
        return string.Equals(
            playerName?.Trim(),
            OwnerPlayerName,
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    private static string BuildAnimatedGradient(string safeName, Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float baseHue, out float saturation, out float value);
        saturation = Mathf.Max(0.45f, saturation);
        value = Mathf.Max(0.65f, value);

        var output = new StringBuilder(safeName.Length * 28);
        float travel = Time.unscaledTime * 0.12f;
        int visibleIndex = 0;

        // EscapeRichText converts special characters to entities. Keep each entity
        // together so an ampersand sequence cannot be split by TMP color tags.
        for (int i = 0; i < safeName.Length;)
        {
            int tokenLength = 1;
            if (safeName[i] == '&')
            {
                int semicolon = safeName.IndexOf(';', i);
                if (semicolon >= i && semicolon - i <= 5) tokenLength = semicolon - i + 1;
            }

            float wave = Mathf.Sin((visibleIndex * 0.72f) - (travel * Mathf.PI * 2f));
            float hue = Mathf.Repeat(baseHue + wave * 0.14f, 1f);
            Color color = Color.HSVToRGB(hue, saturation, value);
            output.Append("<color=#");
            output.Append(ColorUtility.ToHtmlStringRGB(color));
            output.Append('>');
            output.Append(safeName, i, tokenLength);
            output.Append("</color>");

            i += tokenLength;
            visibleIndex++;
        }

        return output.ToString();
    }

    private static string WrapColor(string value, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{value}</color>";
    }

    private static string EscapeRichText(string value)
    {
        return (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
