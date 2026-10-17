using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine;

namespace Centromenu;

public class DnoteTab : ITab
{
    private Vector2 _playerScrollPosition;
    private byte? _selectedPlayerId;

    public string name => "Dnote";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        DrawPlayerList();

        GUILayout.Space(15f);
        GUILayout.BeginVertical();
        DrawOverwriteControls();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawPlayerList()
    {
        GUILayout.BeginVertical(GUILayout.Width(250f));
        GUILayout.Label("Players", GUIStylePreset.TabSubtitle);

        _playerScrollPosition = GUILayout.BeginScrollView(
            _playerScrollPosition,
            false,
            true,
            GUILayout.Height(425f)
        );

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!IsValidTarget(player)) continue;

            var oldColor = GUI.backgroundColor;
            if (_selectedPlayerId == player.PlayerId)
            {
                GUI.backgroundColor = new Color(0.65f, 0.15f, 0.15f);
            }

            if (GUILayout.Button(player.Data.PlayerName, GUIStylePreset.NormalButton, GUILayout.Height(32f)))
            {
                _selectedPlayerId = player.PlayerId;
            }

            GUI.backgroundColor = oldColor;
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawOverwriteControls()
    {
        var target = FindSelectedTarget();
        GUILayout.Label(target ? $"Selected: {target.Data.PlayerName}" : "Select a player");
        GUILayout.Space(10f);

        var wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && target;

        if (GUILayout.Button("Overwrite", GUIStylePreset.NormalButton, GUILayout.Height(40f)))
        {
            Overwrite(target);
        }

        GUI.enabled = wasEnabled;
    }

    private PlayerControl FindSelectedTarget()
    {
        if (!_selectedPlayerId.HasValue) return null;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.PlayerId == _selectedPlayerId.Value && IsValidTarget(player))
            {
                return player;
            }
        }

        _selectedPlayerId = null;
        return null;
    }

    private static bool IsValidTarget(PlayerControl player)
    {
        return player && player.Data != null && !player.AmOwner &&
               !player.Data.IsDead && !player.Data.Disconnected;
    }

    private static void Overwrite(PlayerControl target)
    {
        if (!target || !IsValidTarget(target) || !PlayerControl.LocalPlayer) return;



        var originalPosition = PlayerControl.LocalPlayer.GetTruePosition();
        Utils.MurderPlayer(target, MurderResultFlags.Succeeded);
        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(originalPosition);
        AmongUsClient.Instance.StartCoroutine(Utils.DelayedSnapTo(originalPosition));
    }
}
