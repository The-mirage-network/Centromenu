using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Centromenu;

/// <summary>
/// A second report button that leads the local player to the nearest
/// unreported body, while leaving the game's normal report flow intact.
/// </summary>
public static class BodyFinderButton
{
    private const string ButtonName = "Centromenu_BodyFinderButton";
    private const string IconResource = "Centromenu.Assets.BodyFinderButton.png";

    private static readonly FieldInfo GraphicField = AccessTools.Field(typeof(ActionButton), "graphic");

    private static ReportButton _button;

    public static void Create(HudManager hudManager)
    {
        Destroy();

        if (!hudManager || !hudManager.ReportButton) return;

        var buttonObject = Object.Instantiate(
            hudManager.ReportButton.gameObject,
            hudManager.ReportButton.transform.parent);

        buttonObject.name = ButtonName;
        buttonObject.transform.localPosition =
            hudManager.ReportButton.transform.localPosition + new Vector3(0f, 1.25f, 0f);
        buttonObject.transform.localScale = hudManager.ReportButton.transform.localScale;

        _button = buttonObject.GetComponent<ReportButton>();
        if (!_button)
        {
            Object.Destroy(buttonObject);
            return;
        }

        // Keep the cloned ReportButton behavior so a click uses ReportClosest(),
        // including all of Among Us' ordinary reporting checks and meeting flow.
        if (GraphicField?.GetValue(_button) is SpriteRenderer graphic)
        {
            graphic.sprite = Utils.LoadSprite(IconResource, 1000f);
        }

        _button.gameObject.SetActive(false);
    }

    public static void Update(HudManager hudManager)
    {
        if (!_button || !hudManager || _button.transform.parent != hudManager.ReportButton.transform.parent)
        {
            Create(hudManager);
        }

        if (!_button) return;

        var localPlayer = PlayerControl.LocalPlayer;
        var canUseFinder = localPlayer &&
                           localPlayer.Data != null &&
                           !localPlayer.Data.IsDead &&
                           (Utils.isInGame || Utils.isFreePlay) &&
                           !Utils.isMeeting &&
                           !ExileController.Instance;

        _button.gameObject.SetActive(canUseFinder);
        if (!canUseFinder)
        {
            return;
        }

        var nearestBody = FindNearestUnreportedBody(localPlayer);
        if (!nearestBody)
        {
            _button.SetActive(false);
            return;
        }

        // Match the game's body-report radius. The cloned ReportButton performs
        // the final normal ReportClosest() validation when it is clicked.
        var distance = Vector2.Distance(localPlayer.GetTruePosition(), nearestBody.TruePosition);
        _button.SetActive(distance <= localPlayer.MaxReportDistance);
    }

    public static void Destroy()
    {
        if (_button)
        {
            Object.Destroy(_button.gameObject);
        }

        _button = null;
    }

    private static DeadBody FindNearestUnreportedBody(PlayerControl localPlayer)
    {
        DeadBody nearestBody = null;
        var nearestDistance = float.MaxValue;

        foreach (var bodyObject in GameObject.FindGameObjectsWithTag("DeadBody"))
        {
            var body = bodyObject.GetComponent<DeadBody>();
            if (!body || body.Reported) continue;

            var distance = Vector2.SqrMagnitude(body.TruePosition - localPlayer.GetTruePosition());
            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            nearestBody = body;
        }

        return nearestBody;
    }

}
