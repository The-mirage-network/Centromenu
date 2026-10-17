using CentroFeatures;
using UnityEngine;

namespace Centromenu;

public sealed class FunTab : ITab
{
    public string name => "Fun";

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));
        GUILayout.Label("Teleport Flooder", GUIStylePreset.TabSubtitle);
        GUILayout.Label("Repeatedly teleports everybody, including you, to random vents.");

        var flooder = Centro.routines?.teleportSpammer;
        if (flooder == null)
        {
            GUILayout.Label("Teleport Flooder is not ready.");
            GUILayout.EndVertical();
            return;
        }

        bool enabled = GUILayout.Toggle(flooder.IsGlobal, " Teleport Flood Everybody");
        if (enabled != flooder.IsGlobal)
            flooder.SetGlobal(enabled);

        GUILayout.Space(15f);
        GUILayout.Label("Whoever Moves First", GUIStylePreset.TabSubtitle);
        var whoeverMoves = Centro.routines?.whoeverMovesFirstIsGay;
        if (whoeverMoves != null)
        {
            whoeverMoves.Enabled = GUILayout.Toggle(
                whoeverMoves.Enabled,
                " Whoever moves first is gay.");
        }
        else
        {
            GUILayout.Label("Whoever Moves First is not ready.");
        }

        GUILayout.EndVertical();
    }
}
