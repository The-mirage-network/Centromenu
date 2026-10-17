using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Centromenu;

/// <summary>
/// Shared game-state API and owner of Centro's non-modal notification overlay.
/// </summary>
public sealed class CentroAPI : MonoBehaviour
{
    private const float DefaultDuration = 4f;
    private const float AnimationDuration = 0.3f;
    private const float CardWidth = 320f;
    private const float CardHeight = 118f;
    private const float CardSpacing = 10f;
    private const float ScreenMargin = 18f;

    private static readonly List<CentroNotification> Notifications = new();
    private static CentroAPI _instance;
    private Canvas _canvas;

    public static bool IsInGame { get; private set; }
    public static bool HasGameStarted { get; private set; }
    public static string CurrentGameId { get; private set; } = string.Empty;
    public static DateTime? GameJoinedAt { get; private set; }
    public static DateTime? GameStartedAt { get; private set; }

    public static event Action<string> GameJoined;
    public static event Action GameStarted;
    public static event Action<EndGameResult> GameEnded;

    public void Awake()
    {
        _instance = this;
        EnsureCanvas();
        Centromenu.Log?.LogInfo("[CentroAPI] Centro notification canvas initialized.");
    }

    public void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (_canvas) Destroy(_canvas.gameObject);
    }

    public static void Notification(string message, float duration = DefaultDuration)
    {
        Notification("CENTRO", message, duration);
    }

    public static void Notification(string title, string message, float duration = DefaultDuration)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(message)) return;

        var item = new CentroNotification
        {
            Message = !string.IsNullOrWhiteSpace(message) ? message : title,
            CreatedAt = Time.unscaledTime,
            Duration = Mathf.Max(1f, duration)
        };
        Notifications.Add(item);

        if (Notifications.Count > 5)
        {
            DestroyCard(Notifications[0]);
            Notifications.RemoveAt(0);
        }
    }

    public static void ClearNotifications()
    {
        foreach (CentroNotification item in Notifications) DestroyCard(item);
        Notifications.Clear();
    }

    public static void OnGameJoined(string gameId)
    {
        IsInGame = true;
        HasGameStarted = false;
        CurrentGameId = gameId ?? string.Empty;
        GameJoinedAt = DateTime.UtcNow;
        GameStartedAt = null;
        GameJoined?.Invoke(CurrentGameId);
        Notification("Centromenu by Centroscape Loaded! Enjoy.");
    }

    public static void OnGameStart()
    {
        IsInGame = true;
        HasGameStarted = true;
        GameStartedAt = DateTime.UtcNow;
        GameStarted?.Invoke();
    }

    public static void OnGameEnd(EndGameResult result)
    {
        IsInGame = false;
        HasGameStarted = false;
        GameStartedAt = null;
        GameEnded?.Invoke(result);
    }

    public void Update()
    {
        if (Centromenu.isPanicked)
        {
            if (_canvas) _canvas.gameObject.SetActive(false);
            return;
        }

        EnsureCanvas();
        if (!_canvas) return;
        if (!_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);

        float now = Time.unscaledTime;
        for (int i = Notifications.Count - 1; i >= 0; i--)
        {
            CentroNotification item = Notifications[i];
            if (!item.Root) CreateCard(item);

            float age = now - item.CreatedAt;
            if (age >= item.Duration + AnimationDuration)
            {
                DestroyCard(item);
                Notifications.RemoveAt(i);
                continue;
            }

            float dismissAge = Mathf.Max(0f, age - item.Duration);
            float visibility = Mathf.Min(
                Mathf.Clamp01(age / AnimationDuration),
                1f - Mathf.Clamp01(dismissAge / AnimationDuration));
            float eased = 1f - Mathf.Pow(1f - visibility, 3f);
            float offscreenX = CardWidth + ScreenMargin;

            item.Root.anchoredPosition = new Vector2(
                -ScreenMargin + offscreenX * (1f - eased),
                ScreenMargin + i * (CardHeight + CardSpacing));
            item.Group.alpha = eased;
            item.ProgressFill.anchorMax = new Vector2(Mathf.Clamp01(age / item.Duration), 1f);
        }
    }

    private void EnsureCanvas()
    {
        if (_canvas) return;

        var canvasObject = new GameObject(
            "CentroNotificationCanvas",
            new Il2CppReferenceArray<Il2CppSystem.Type>(new[]
            {
                Il2CppType.Of<RectTransform>(),
                Il2CppType.Of<Canvas>(),
                Il2CppType.Of<CanvasScaler>(),
                Il2CppType.Of<GraphicRaycaster>()
            }));
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32760;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void CreateCard(CentroNotification item)
    {
        GameObject rootObject = CreateUiObject("CentroNotification", _canvas.transform);
        item.Root = rootObject.GetComponent<RectTransform>();
        item.Root.anchorMin = new Vector2(1f, 0f);
        item.Root.anchorMax = new Vector2(1f, 0f);
        item.Root.pivot = new Vector2(1f, 0f);
        item.Root.sizeDelta = new Vector2(CardWidth, CardHeight);

        var background = rootObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.06f, 0.075f, 0.96f);
        background.raycastTarget = false;
        item.Group = rootObject.AddComponent<CanvasGroup>();
        item.Group.interactable = true;
        item.Group.blocksRaycasts = true;

        GameObject headerObject = CreateUiObject("Header", item.Root);
        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
        SetRect(headerRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -31f), new Vector2(-14f, -7f));
        var header = headerObject.AddComponent<TextMeshProUGUI>();
        header.text = "CENTRO";
        header.fontSize = 16f;
        header.fontStyle = FontStyles.Bold;
        header.alignment = TextAlignmentOptions.MidlineLeft;
        header.color = Color.white;
        header.raycastTarget = false;

        GameObject viewportObject = CreateUiObject("InformationViewport", item.Root);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(14f, 10f), new Vector2(-14f, -36f));
        var viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        viewportObject.AddComponent<RectMask2D>();

        var scroll = viewportObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 22f;
        scroll.viewport = viewport;

        GameObject contentObject = CreateUiObject("Information", viewport);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var information = contentObject.AddComponent<TextMeshProUGUI>();
        information.text = item.Message;
        information.fontSize = 13f;
        information.alignment = TextAlignmentOptions.TopLeft;
        information.color = new Color(0.84f, 0.87f, 0.92f, 1f);
        information.enableWordWrapping = true;
        information.overflowMode = TextOverflowModes.Overflow;
        information.raycastTarget = false;

        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        GameObject trackObject = CreateUiObject("LifetimeTrack", item.Root);
        RectTransform track = trackObject.GetComponent<RectTransform>();
        SetRect(track, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 0f), new Vector2(0f, 5f));
        var trackImage = trackObject.AddComponent<Image>();
        trackImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        trackImage.raycastTarget = false;

        GameObject fillObject = CreateUiObject("LifetimeFill", track);
        item.ProgressFill = fillObject.GetComponent<RectTransform>();
        SetRect(item.ProgressFill, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        var fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.72f, 0.95f, 1f);
        fillImage.raycastTarget = false;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var result = new GameObject(
            name,
            new Il2CppReferenceArray<Il2CppSystem.Type>(new[] { Il2CppType.Of<RectTransform>() }));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void DestroyCard(CentroNotification item)
    {
        if (item?.Root) Destroy(item.Root.gameObject);
        if (item != null) item.Root = null;
    }

    private sealed class CentroNotification
    {
        public string Message;
        public float CreatedAt;
        public float Duration;
        public RectTransform Root;
        public RectTransform ProgressFill;
        public CanvasGroup Group;
    }
}
