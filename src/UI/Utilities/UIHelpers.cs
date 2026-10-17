using UnityEngine;

namespace Centromenu;

public static class UIHelpers
{
    private const float PanelOpacity = 0.78f;
    private static Color _contentColor;

    public static void BeginPanelWindow()
    {
        _contentColor = GUI.color;
        var panelColor = _contentColor;
        panelColor.a *= PanelOpacity;
        GUI.color = panelColor;
    }

    public static void RestoreContentColor()
    {
        // Keep the window background translucent without fading its controls.
        GUI.color = _contentColor;
    }

    public static void ApplyUIColor()
    {
        if (CheatToggles.rgbMode)
        {
            GUI.backgroundColor = Color.HSVToRGB(MenuUI.hue, 1f, 1f); // Set background color based on hue
        }
        else
        {
            var configHtmlColor = Centromenu.menuHtmlColor.Value;

            if (!ColorUtility.TryParseHtmlString(configHtmlColor, out var uiColor))
            {
                if (!configHtmlColor.StartsWith("#"))
                {
                    if (ColorUtility.TryParseHtmlString("#" + configHtmlColor, out uiColor))
                    {
                        GUI.backgroundColor = uiColor;
                    }
                }
            }
            else
            {
                GUI.backgroundColor = uiColor;
            }
        }
    }
}
