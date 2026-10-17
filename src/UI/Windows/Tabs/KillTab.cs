using UnityEngine;

namespace Centromenu;

public class KillTab : ITab
{
    public string name => "Kill";

    public void Draw()
    {
        var standardColor = GUI.backgroundColor;

        if (CheatToggles.kira)
        {
            GUI.backgroundColor = new Color(0.65f, 0.15f, 0.15f);
        }

        if (GUILayout.Button("Kira", GUIStylePreset.NormalButton))
        {
            CheatToggles.kira = !CheatToggles.kira;
        }

        GUI.backgroundColor = standardColor;
    }
}
