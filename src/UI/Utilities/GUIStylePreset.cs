using UnityEngine;

namespace Centromenu;

public static class GUIStylePreset
{
    private static GUIStyle _separator;
    private static GUIStyle _darkSeparator;
    private static GUIStyle _normalButton;
    private static GUIStyle _normalToggle;
    private static GUIStyle _textField;
    private static GUIStyle _tabButton;
    private static GUIStyle _selectedTabButton;
    private static GUIStyle _compactTabButton;
    private static GUIStyle _tabTitle;
    private static GUIStyle _tabSubtitle;
    private static GUIStyle _roundedPanel;
    private static Texture2D _sharpWindowTexture;
    private static Texture2D _sharpButtonTexture;
    private static Texture2D _sharpButtonHoverTexture;
    private static Texture2D _sharpButtonActiveTexture;
    private static Texture2D _tabButtonTexture;
    private static Texture2D _tabButtonHoverTexture;
    private static Texture2D _tabButtonActiveTexture;
    private static Texture2D _selectedTabButtonTexture;
    private static Texture2D _selectedTabButtonHoverTexture;
    private static bool _lastLightTheme;
    private static bool _themeInitialized;

    public static void ApplyMenuStyle(Texture2D panelBackground)
    {
        var light = CheatToggles.lightTheme;
        if (_themeInitialized && light != _lastLightTheme) ResetThemeAssets();
        _themeInitialized = true;
        _lastLightTheme = light;

        _sharpWindowTexture ??= CreateSolidTexture(light
            ? new Color(0.96f, 0.96f, 0.97f, 0.99f)
            : new Color(0.075f, 0.075f, 0.075f, 0.98f));
        _sharpButtonTexture ??= CreateRoundedTexture(light
            ? new Color(0.88f, 0.88f, 0.91f, 1f)
            : new Color(0.22f, 0.22f, 0.22f, 1f));
        _sharpButtonHoverTexture ??= CreateRoundedTexture(light
            ? new Color(0.79f, 0.79f, 0.84f, 1f)
            : new Color(0.32f, 0.32f, 0.32f, 1f));
        _sharpButtonActiveTexture ??= CreateRoundedTexture(light
            ? new Color(0.70f, 0.70f, 0.76f, 1f)
            : new Color(0.14f, 0.14f, 0.14f, 1f));
        _tabButtonTexture ??= CreateRoundedGradientTexture(
            light ? new Color(0.91f, 0.91f, 0.94f, 1f) : new Color(0.19f, 0.19f, 0.21f, 0.96f),
            light ? new Color(0.84f, 0.84f, 0.88f, 1f) : new Color(0.14f, 0.14f, 0.16f, 0.96f));
        _tabButtonHoverTexture ??= CreateRoundedGradientTexture(
            light ? new Color(0.82f, 0.82f, 0.87f, 1f) : new Color(0.30f, 0.30f, 0.34f, 1f),
            light ? new Color(0.74f, 0.74f, 0.80f, 1f) : new Color(0.22f, 0.22f, 0.25f, 1f));
        _tabButtonActiveTexture ??= CreateRoundedGradientTexture(
            light ? new Color(0.68f, 0.68f, 0.74f, 1f) : new Color(0.10f, 0.10f, 0.12f, 1f),
            light ? new Color(0.61f, 0.61f, 0.68f, 1f) : new Color(0.07f, 0.07f, 0.09f, 1f));
        _selectedTabButtonTexture ??= CreateRoundedGradientTexture(
            light ? new Color(0.74f, 0.74f, 0.80f, 1f) : new Color(0.17f, 0.17f, 0.20f, 1f),
            light ? new Color(0.66f, 0.66f, 0.73f, 1f) : new Color(0.11f, 0.11f, 0.14f, 1f));
        _selectedTabButtonHoverTexture ??= CreateRoundedGradientTexture(
            light ? new Color(0.70f, 0.70f, 0.77f, 1f) : new Color(0.22f, 0.22f, 0.26f, 1f),
            light ? new Color(0.60f, 0.60f, 0.68f, 1f) : new Color(0.15f, 0.15f, 0.18f, 1f));

        var windowBackground = !light && panelBackground ? panelBackground : _sharpWindowTexture;
        GUI.skin.window.normal.background = windowBackground;
        GUI.skin.window.onNormal.background = windowBackground;
        GUI.skin.window.border = new RectOffset();
        GUI.skin.window.richText = true;

        GUI.skin.button.normal.background = _sharpButtonTexture;
        GUI.skin.button.onNormal.background = _sharpButtonTexture;
        GUI.skin.button.hover.background = _sharpButtonHoverTexture;
        GUI.skin.button.onHover.background = _sharpButtonHoverTexture;
        GUI.skin.button.active.background = _sharpButtonActiveTexture;
        GUI.skin.button.onActive.background = _sharpButtonActiveTexture;
        GUI.skin.button.focused.background = _sharpButtonTexture;
        GUI.skin.button.onFocused.background = _sharpButtonTexture;
        GUI.skin.button.border = new RectOffset { left = 6, right = 6, top = 6, bottom = 6 };
        var textColor = light ? new Color(0.10f, 0.10f, 0.12f, 1f) : Color.white;
        GUI.skin.label.normal.textColor = textColor;
        GUI.skin.button.normal.textColor = textColor;
        GUI.skin.button.hover.textColor = textColor;
        GUI.skin.button.active.textColor = textColor;
    }

    private static void ResetThemeAssets()
    {
        Object.Destroy(_sharpWindowTexture);
        Object.Destroy(_sharpButtonTexture);
        Object.Destroy(_sharpButtonHoverTexture);
        Object.Destroy(_sharpButtonActiveTexture);
        Object.Destroy(_tabButtonTexture);
        Object.Destroy(_tabButtonHoverTexture);
        Object.Destroy(_tabButtonActiveTexture);
        Object.Destroy(_selectedTabButtonTexture);
        Object.Destroy(_selectedTabButtonHoverTexture);

        _sharpWindowTexture = _sharpButtonTexture = _sharpButtonHoverTexture = _sharpButtonActiveTexture = null;
        _tabButtonTexture = _tabButtonHoverTexture = _tabButtonActiveTexture = null;
        _selectedTabButtonTexture = _selectedTabButtonHoverTexture = null;
        _normalButton = _normalToggle = _textField = _tabButton = _selectedTabButton = null;
        _compactTabButton = _tabTitle = _tabSubtitle = _roundedPanel = null;
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
        return texture;
    }

    private static Texture2D CreateRoundedTexture(Color color)
    {
        const int size = 16;
        const int radius = 5;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0;
                var dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0;
                var outsideCorner = dx > 0 && dy > 0 && dx * dx + dy * dy > radius * radius;
                texture.SetPixel(x, y, outsideCorner ? Color.clear : color);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
        return texture;
    }

    private static Texture2D CreateRoundedGradientTexture(Color top, Color bottom)
    {
        const int size = 32;
        const float radius = 8f;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);

        for (var y = 0; y < size; y++)
        {
            var verticalBlend = y / (size - 1f);
            var rowColor = Color.Lerp(bottom, top, verticalBlend);

            for (var x = 0; x < size; x++)
            {
                var cornerX = Mathf.Max(radius - (x + 0.5f), x + 0.5f - (size - radius));
                var cornerY = Mathf.Max(radius - (y + 0.5f), y + 0.5f - (size - radius));
                var distance = Mathf.Sqrt(
                    Mathf.Max(0f, cornerX) * Mathf.Max(0f, cornerX) +
                    Mathf.Max(0f, cornerY) * Mathf.Max(0f, cornerY));
                var coverage = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, new Color(rowColor.r, rowColor.g, rowColor.b, rowColor.a * coverage));
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
        return texture;
    }

    public static GUIStyle Separator
    {
        get
        {
            if (_separator == null)
            {
                _separator = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = Texture2D.whiteTexture },
                    margin = new RectOffset { top = 4, bottom = 4 },
                    padding = new RectOffset(),
                    border = new RectOffset()
                };
            }

            return _separator;
        }
    }

    public static GUIStyle DarkSeparator
    {
        get
        {
            if (_darkSeparator == null)
            {
                _darkSeparator = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = Texture2D.grayTexture },
                    margin = new RectOffset { top = 4, bottom = 4 },
                    padding = new RectOffset(),
                    border = new RectOffset()
                };
            }

            return _darkSeparator;
        }
    }

    public static GUIStyle NormalButton
    {
        get
        {
            if (_normalButton == null)
            {
                _normalButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13
                };
            }

            return _normalButton;
        }
    }

    public static GUIStyle NormalToggle
    {
        get
        {
            if (_normalToggle == null)
            {
                _normalToggle = new GUIStyle(GUI.skin.toggle)
                {
                    fontSize = 13
                };
            }

            return _normalToggle;
        }
    }

    public static GUIStyle TextField
    {
        get
        {
            if (_textField == null)
            {
                _textField = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset { left = 10, right = 10, top = 5, bottom = 5 },
                    border = new RectOffset { left = 6, right = 6, top = 6, bottom = 6 }
                };
                _textField.normal.background = _sharpButtonTexture;
                _textField.hover.background = _sharpButtonHoverTexture;
                _textField.focused.background = _sharpButtonActiveTexture;
                _textField.active.background = _sharpButtonActiveTexture;
                var textColor = CheatToggles.lightTheme ? new Color(0.10f, 0.10f, 0.12f, 1f) : Color.white;
                _textField.normal.textColor = textColor;
                _textField.hover.textColor = textColor;
                _textField.focused.textColor = textColor;
                _textField.active.textColor = textColor;
            }

            return _textField;
        }
    }

    public static GUIStyle TabButton
    {
        get
        {
            if (_tabButton == null)
            {
                _tabButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    padding = new RectOffset { left = 14, right = 10, top = 5, bottom = 5 },
                    margin = new RectOffset { left = 4, right = 5, top = 3, bottom = 3 },
                    border = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 },
                };
                _tabButton.normal.background = _tabButtonTexture;
                _tabButton.onNormal.background = _tabButtonTexture;
                _tabButton.hover.background = _tabButtonHoverTexture;
                _tabButton.onHover.background = _tabButtonHoverTexture;
                _tabButton.active.background = _tabButtonActiveTexture;
                _tabButton.onActive.background = _tabButtonActiveTexture;
                _tabButton.focused.background = _tabButtonTexture;
                _tabButton.onFocused.background = _tabButtonTexture;
            }

            return _tabButton;
        }
    }

    public static GUIStyle SelectedTabButton
    {
        get
        {
            if (_selectedTabButton == null)
            {
                _selectedTabButton = new GUIStyle(TabButton)
                {
                    fontStyle = FontStyle.Bold,
                };
                _selectedTabButton.normal.background = _selectedTabButtonTexture;
                _selectedTabButton.onNormal.background = _selectedTabButtonTexture;
                _selectedTabButton.hover.background = _selectedTabButtonHoverTexture;
                _selectedTabButton.onHover.background = _selectedTabButtonHoverTexture;
                _selectedTabButton.active.background = _tabButtonActiveTexture;
                _selectedTabButton.onActive.background = _tabButtonActiveTexture;
                _selectedTabButton.focused.background = _selectedTabButtonTexture;
                _selectedTabButton.onFocused.background = _selectedTabButtonTexture;
            }

            return _selectedTabButton;
        }
    }

    public static GUIStyle CompactTabButton
    {
        get
        {
            if (_compactTabButton == null)
            {
                _compactTabButton = new GUIStyle(TabButton)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                };
            }

            return _compactTabButton;
        }
    }

    public static GUIStyle TabTitle
    {
        get
        {
            if (_tabTitle == null)
            {
                _tabTitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            return _tabTitle;
        }
    }

    public static GUIStyle TabSubtitle
    {
        get
        {
            if (_tabSubtitle == null)
            {
                _tabSubtitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            return _tabSubtitle;
        }
    }

    public static GUIStyle RoundedPanel
    {
        get
        {
            if (_roundedPanel == null)
            {
                var background = CreateRoundedTexture(new Color(0.105f, 0.105f, 0.12f, 0.94f));
                _roundedPanel = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = background },
                    padding = new RectOffset { left = 10, right = 10, top = 10, bottom = 10 },
                    margin = new RectOffset { left = 2, right = 2, top = 3, bottom = 3 },
                    border = new RectOffset { left = 6, right = 6, top = 6, bottom = 6 }
                };
            }

            return _roundedPanel;
        }
    }
}
