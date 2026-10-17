using System.Collections.Generic;
using UnityEngine;

namespace Centromenu;
public static class MinimapHandler
{
    public static bool minimapActive;
    public static List<HerePoint> herePoints = new List<HerePoint>();
    public static List<HerePoint> herePointsToRemove = new List<HerePoint>();

    public static bool IsCheatEnabled()
    {
        return CheatToggles.mapCrew || CheatToggles.mapGhosts || CheatToggles.mapImps;
    }

    public static void HandleHerePoint(HerePoint herePoint)
    {
        Color herePointColor = new Color();

        try
        {
            herePoint.sprite.gameObject.SetActive(false);


            if (CheatToggles.mapCrew && !herePoint.player.Data.Role.IsImpostor)
            {
                if (!herePoint.player.Data.IsDead)
                {
                    herePoint.sprite.gameObject.SetActive(true);
                    if (CheatToggles.colorBasedMap)
                    {
                        herePointColor = herePoint.player.Data.Color;
                    }
                    else
                    {
                        herePointColor = herePoint.player.Data.Role.TeamColor;
                    }
                }
            }

            else if (CheatToggles.mapImps && herePoint.player.Data.Role.IsImpostor)
            {
                if (!herePoint.player.Data.IsDead)
                {
                    herePoint.sprite.gameObject.SetActive(true);
                    if (CheatToggles.colorBasedMap)
                    {
                        herePointColor = herePoint.player.Data.Color;
                    }
                    else
                    {
                        herePointColor = herePoint.player.Data.Role.TeamColor;
                    }
                }
            }

            if (CheatToggles.mapGhosts && herePoint.player.Data.IsDead)
            {
                herePoint.sprite.gameObject.SetActive(true);
                if (CheatToggles.colorBasedMap)
                {
                    herePointColor = herePoint.player.Data.Color;
                }
                else
                {
                    herePointColor = Palette.White;
                }
            }

            if (herePoint.sprite.gameObject.active)
            {

                herePoint.sprite.material.SetColor(PlayerMaterial.BackColor, herePointColor);
                herePoint.sprite.material.SetColor(PlayerMaterial.BodyColor, herePointColor);
                herePoint.sprite.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);


                var vector = herePoint.player.transform.position;
                vector /= ShipStatus.Instance.MapScale;
                vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                vector.z = -1f;
                herePoint.sprite.transform.localPosition = vector;
            }
        }
        catch
        {

            Object.Destroy(herePoint.sprite.gameObject);
            herePointsToRemove.Add(herePoint);
        }
    }
}
