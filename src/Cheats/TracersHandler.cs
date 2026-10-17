using UnityEngine;

namespace Centromenu;

public static class TracersHandler
{

    public static void DrawPlayerTracer(PlayerPhysics playerPhysics)
    {
        try
        {
            var color = Color.clear;

            if (!playerPhysics.myPlayer.Data.IsDead)
            {
                if (CheatToggles.tracersCrew && !playerPhysics.myPlayer.Data.Role.IsImpostor ||
                    CheatToggles.tracersImps && playerPhysics.myPlayer.Data.Role.IsImpostor)
                {
                    if (CheatToggles.distanceBasedTracers)
                    {
                        color = GetDistanceBasedColor(playerPhysics.myPlayer.transform.position);
                    }
                    else if (CheatToggles.colorBasedTracers)
                    {
                        color = playerPhysics.myPlayer.Data.Color;
                    }
                    else
                    {
                        color = playerPhysics.myPlayer.Data.Role.TeamColor;
                    }
                }
            }
            else
            {
                if (CheatToggles.tracersGhosts)
                {
                    if (CheatToggles.distanceBasedTracers)
                    {
                        color = GetDistanceBasedColor(playerPhysics.myPlayer.transform.position);
                    }
                    else if (CheatToggles.colorBasedTracers)
                    {
                        color = playerPhysics.myPlayer.Data.Color;
                    }
                    else
                    {
                        color = Palette.White;
                    }
                }
            }


            Utils.DrawTracer(playerPhysics.myPlayer.gameObject, PlayerControl.LocalPlayer.gameObject, color);
            Utils.DrawPlayerBox(playerPhysics.myPlayer.gameObject, color);
        } catch { }
    }


    public static void DrawBodyTracer(DeadBody deadBody)
    {
        var color = Color.clear;

        if (CheatToggles.tracersBodies)
        {
            if (CheatToggles.distanceBasedTracers)
            {
                color = GetDistanceBasedColor(deadBody.transform.position);
            }
            else if (CheatToggles.colorBasedTracers)
            {
                color = GameData.Instance.GetPlayerById(deadBody.ParentId).Color;
            }
            else
            {
                color = Color.yellow;
            }
        }


        Utils.DrawTracer(deadBody.gameObject, PlayerControl.LocalPlayer.gameObject, color);
    }



    private static Color GetDistanceBasedColor(Vector3 targetPosition)
    {
        const float maxDistance = 20f;
        const float minDistance = 2f;

        var distance = Vector3.Distance(targetPosition, PlayerControl.LocalPlayer.transform.position);
        var normalized = Mathf.InverseLerp(minDistance, maxDistance, distance);


        return normalized < 0.5f
            ? Color.Lerp(Color.red, Color.yellow, normalized * 2f)
            : Color.Lerp(Color.yellow, Color.green, (normalized - 0.5f) * 2f);
    }
}
