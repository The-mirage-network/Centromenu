using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Centromenu;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class MeetingHud_Start
{
    public static void Postfix()
    {
        Centromenu.meetingMusicPlayer?.StartMeetingMusic();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
public static class MeetingHud_CloseMusic
{
    public static void Prefix()
    {
        Centromenu.meetingMusicPlayer?.StopMeetingMusic();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class MeetingHud_Update
{
    public static List<int> votedPlayers = new List<int>();


    public static void Prefix(MeetingHud __instance)
    {
        if (__instance.state < MeetingHud.MeetingStates.Results)
        {
            foreach (var playerVoteArea in __instance.playerStates)
            {
                if (!playerVoteArea) continue;

                var playerData = GameData.Instance.GetPlayerById(playerVoteArea.PlayerId);

                if (playerData != null && !playerData.Disconnected && playerVoteArea.VotedForId != PlayerVoteArea.HasNotVoted && playerVoteArea.VotedForId != PlayerVoteArea.MissedVote && playerVoteArea.VotedForId != PlayerVoteArea.DeadVote && !votedPlayers.Contains(playerVoteArea.PlayerId))
                {
                    votedPlayers.Add(playerVoteArea.PlayerId);

                    if (playerVoteArea.VotedForId != PlayerVoteArea.SkippedVote)
                    {
                        foreach (var votedForArea in __instance.playerStates)
                        {
                            if (votedForArea.PlayerId == playerVoteArea.VotedForId)
                            {
                                __instance.BloopAVoteIcon(playerData, 0, votedForArea.transform);
                                break;
                            }
                        }
                    }
                    else if (__instance.SkippedVoting)
                    {
                        __instance.BloopAVoteIcon(playerData, 0, __instance.SkippedVoting.transform);
                    }
                }
            }

            foreach (var votedForArea in __instance.playerStates)
            {
                if (!votedForArea) continue;

                var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
                if (!voteSpreader) continue;

                foreach (var spriteRenderer in voteSpreader.Votes)
                {
                    spriteRenderer.gameObject.SetActive(CheatToggles.revealVotes);
                }
            }


            if (__instance.SkippedVoting)
            {
                __instance.SkippedVoting.SetActive(CheatToggles.revealVotes);
            }
        }
    }

    public static void Postfix(MeetingHud __instance)
    {


        CentromenuCheats.KiraCheat();
        CentromenuESP.MeetingNametags(__instance);


        PlayerControl.LocalPlayer.onLadder = false;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
public static class MeetingHud_PopulateResults
{

    public static void Prefix(MeetingHud __instance)
    {
        foreach (var votedForArea in __instance.playerStates)
        {
            if (!votedForArea) continue;

            var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
            if (!voteSpreader) continue;

            var length = voteSpreader.Votes.Count;
            if (length == 0) continue;

            foreach (var spriteRenderer in voteSpreader.Votes)
            {
                Object.DestroyImmediate(spriteRenderer);
            }

            voteSpreader.Votes.Clear();
        }

        if (__instance.SkippedVoting)
        {
            var voteSpreader = __instance.SkippedVoting.transform.GetComponent<VoteSpreader>();

            foreach (var spriteRenderer in voteSpreader.Votes)
            {
                Object.DestroyImmediate(spriteRenderer);
            }

            voteSpreader.Votes.Clear();
        }

        MeetingHud_Update.votedPlayers.Clear();
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
public static class MeetingHud_CheckForEndVoting
{

    public static bool Prefix(MeetingHud __instance)
    {
        if (!CheatToggles.voteImmune) return true;

        if (!__instance.playerStates.All(ps => ps.AmDead || ps.DidVote)) return true;

        var max = __instance.CalculateVotes().MaxPair(out var tie);
        var exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => !tie && v.PlayerId == max.Key);

        bool wasOverruled = false;
        ushort overruleNonce = 0;
        JudgeOverrule judgeOverrule;
        NetworkedPlayerInfo networkedPlayerInfo;
        NetworkedPlayerInfo networkedPlayerInfo2;

        if (__instance.TryGetWinningOverrule(out judgeOverrule, out networkedPlayerInfo, out networkedPlayerInfo2))
        {
            wasOverruled = true;
            overruleNonce = judgeOverrule.OverruleNonce;

            if (networkedPlayerInfo2.Role.TeamType == RoleTeamTypes.Impostor)
            {
                exiled = GameData.Instance.GetPlayerById(judgeOverrule.OverruledPlayerId);
            }
            else
            {
                exiled = networkedPlayerInfo;
            }
        }


        if (exiled != null && exiled == PlayerControl.LocalPlayer.Data)
        {
            exiled = null;
        }

        var states = new MeetingHud.VoterState[__instance.playerStates.Length];

        for (var index = 0; index < __instance.playerStates.Length; ++index)
        {
            var playerState = __instance.playerStates[index];
            states[index] = new MeetingHud.VoterState
            {
                VoterId = playerState.PlayerId,
                VotedForId = playerState.VotedForId
            };
        }

        __instance.RpcVotingComplete(states, exiled, tie, wasOverruled, overruleNonce);

        return false;
    }
}
