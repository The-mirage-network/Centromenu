using UnityEngine;

namespace Centromenu;

public class AntiDatingTab : ITab
{
    private Vector2 _scrollPosition;

    public string name => "Anti-dating";

    public void Draw()
    {
        GUILayout.Label("Name moderation", GUIStylePreset.TabSubtitle);
        GUILayout.Label("Flags explicit dating, age/gender, and off-platform contact names. " +
                        "Obvious spacing and leetspeak are normalized before matching.");

        CheatToggles.antiDatingEnabled = GUILayout.Toggle(
            CheatToggles.antiDatingEnabled,
            " Enable name scanner");

        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && CheatToggles.antiDatingEnabled;
        CheatToggles.antiDatingAutoReport = GUILayout.Toggle(
            CheatToggles.antiDatingAutoReport,
            " Auto-report flagged names once per lobby");
        CheatToggles.antiDatingAutoVoteKick = GUILayout.Toggle(
            CheatToggles.antiDatingAutoVoteKick,
            " Auto vote-kick flagged names once per lobby");

        if (GUILayout.Button("Scan now", GUIStylePreset.NormalButton))
        {
            AntiDatingNameGuard.ScanNow();
        }

        GUI.enabled = previousEnabled;

        GUILayout.Space(8f);
        GUILayout.Label(AntiDatingNameGuard.Status);
        GUILayout.Label("Reports and vote-kicks are deduplicated per player and never retried in a loop.");

        GUILayout.Space(8f);
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(270f));
        int? reportClientId = null;
        string reportPlayerName = null;
        int? voteKickClientId = null;
        string voteKickPlayerName = null;

        if (AntiDatingNameGuard.Findings.Count == 0)
        {
            GUILayout.Label("No flagged players to show.");
        }
        else
        {
            foreach (var finding in AntiDatingNameGuard.Findings)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(finding.PlayerName);
                GUILayout.Label($"Reason: {finding.Reason}");

                GUI.enabled = previousEnabled && !finding.ReportAttempted;
                if (GUILayout.Button(
                        finding.ReportAttempted ? "Report already attempted" : "Report bad name once",
                        GUIStylePreset.NormalButton))
                {

                    reportClientId = finding.ClientId;
                    reportPlayerName = finding.PlayerName;
                }

                GUI.enabled = previousEnabled && !finding.VoteKickAttempted;
                if (GUILayout.Button(
                        finding.VoteKickAttempted ? "Vote-kick already attempted" : "Vote-kick once",
                        GUIStylePreset.NormalButton))
                {
                    voteKickClientId = finding.ClientId;
                    voteKickPlayerName = finding.PlayerName;
                }

                GUI.enabled = previousEnabled;
                GUILayout.EndVertical();
                GUILayout.Space(5f);
            }
        }

        GUILayout.EndScrollView();

        if (reportClientId.HasValue)
        {
            AntiDatingNameGuard.TryReportOnce(reportClientId.Value, reportPlayerName);
            AntiDatingNameGuard.ScanNow();
        }

        if (voteKickClientId.HasValue)
        {
            AntiDatingNameGuard.TryVoteKick(voteKickClientId.Value, voteKickPlayerName);
            AntiDatingNameGuard.ScanNow();
        }
    }
}
