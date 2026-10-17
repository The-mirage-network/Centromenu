using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Centromenu;




[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class MainMenuBackgroundPatch
{
    private const string BackgroundResource = "Centromenu.Assets.CentroStartupBackground.png";
    private const string LogoResource = "Centromenu.Assets.CentroMainMenuLogo.png";

    public static void Postfix(MainMenuManager __instance)
    {
        GameObject.Find("Ambience")?.SetActive(false);
        ReplaceMainMenuLogo();
        __instance.screenTint.enabled = false;

        SetRendererEnabled(__instance.mainMenuUI.gameObject, "BackgroundTexture", false);
        SetRendererEnabled(__instance.mainMenuUI.gameObject, "WindowShine", false);
        SetObjectActive(__instance.mainMenuUI.gameObject, "ScreenCover", false);
        SetRendererEnabled(__instance.mainMenuUI.gameObject, "LeftPanel", false);
        SetRendererEnabled(__instance.mainMenuUI.gameObject, "RightPanel", false);
        SetRendererEnabled(__instance.mainMenuUI.gameObject, "MaskedBlackScreen", false);
        SetRendererEnabled(__instance.mainMenuUI.gameObject, "Divider", false);

        var leftPanel = FindTransform(__instance.mainMenuUI.gameObject, "LeftPanel");
        foreach (var shine in leftPanel?.GetComponentsInChildren<SpriteRenderer>(true)
                     .Where(renderer => renderer.name == "Shine") ?? [])
        {
            shine.enabled = false;
        }

        var backgroundObject = new GameObject("CentroStartupBackground");
        backgroundObject.transform.position = new Vector3(0f, 0f, 600f);
        var backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = Utils.LoadSprite(BackgroundResource, 150f);
    }

    private static void ReplaceMainMenuLogo()
    {
        var logoRenderer = GameObject.Find("LOGO-AU")?.GetComponent<SpriteRenderer>();
        var replacement = Utils.LoadSprite(LogoResource, 100f);
        if (logoRenderer == null || logoRenderer.sprite == null || replacement == null) return;

        var originalSize = logoRenderer.sprite.bounds.size;
        var replacementSize = replacement.bounds.size;
        var scale = logoRenderer.transform.localScale;

        logoRenderer.sprite = replacement;
        scale.x *= originalSize.x / replacementSize.x;
        scale.y *= originalSize.y / replacementSize.y;
        logoRenderer.transform.localScale = scale;
    }

    private static void SetRendererEnabled(GameObject root, string objectName, bool enabled)
    {
        var renderer = root.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(candidate => candidate.name == objectName);
        if (renderer != null) renderer.enabled = enabled;
    }

    private static void SetObjectActive(GameObject root, string objectName, bool active)
    {
        var child = FindTransform(root, objectName);
        child?.gameObject.SetActive(active);
    }

    private static Transform FindTransform(GameObject root, string objectName) =>
        root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == objectName);
}
