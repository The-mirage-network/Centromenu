using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using InnerNet;
using UnityEngine;

namespace Centromenu;

/// <summary>
/// Detects names that explicitly advertise dating, age/gender, or off-platform
/// contact. Reports are intentionally deduplicated to one attempt per client in
/// each lobby so a false positive cannot turn into report spam.
/// </summary>
public static class AntiDatingNameGuard
{
    public sealed class Finding
    {
        public int ClientId { get; init; }
        public string PlayerName { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public bool ReportAttempted { get; init; }
        public bool VoteKickAttempted { get; init; }
    }

    private static readonly string[] DatingPhrases =
    {
        "dateme", "lookingfordate", "lookingadate", "lookingforlove", "lookingalove",
        "needabf", "needagf", "wantabf", "wantagf", "needboyfriend",
        "needgirlfriend", "boyfriendwanted", "girlfriendwanted", "bfneeded",
        "gfneeded", "singleboy", "singlegirl", "singlemale", "singlefemale",
        "addmysnap", "snapme", "mydiscord", "myinsta"
    };

    private static readonly string[] NonSolicitingPhrases =
    {
        "dontdateme", "nodating", "antidating"
    };

    private static readonly string[] ContactPlatforms =
    {
        "snapchat", "snap", "discord", "insta", "instagram", "tiktok", "kik"
    };

    private static readonly string[] ContactInvites =
    {
        "addme", "addmy", "dmme", "msgme", "message", "followme", "hmu"
    };

    private static readonly string[] ContactFalsePositives =
    {
        "instant", "snapshot", "snapdragon"
    };

    // Focused subset of the SaferAmongUs vocabulary. Broad terms from that
    // project (ordinary names, greetings, boy/girl/man, and every two-digit
    // number) are deliberately excluded because they create many false reports.
    private static readonly string[] DatingFragments =
    {
        "dating", "datin", "datng", "single", "singl", "sngl", "flirt",
        "romance", "romant", "bemine", "beyour", "urmine", "roleplay",
        "needbf", "needgf", "wantbf", "wantgf", "subbf", "subgf",
        "dombf", "domgf", "wifey", "hubby", "lovr", "luver", "luvr"
    };

    private static readonly string[] ExplicitFragments =
    {
        "horny", "hrny", "hxrny", "sex", "segg", "segs", "nude", "nudes",
        "noods", "kinky", "fetish", "threesome", "threesum", "touchme",
        "useme", "rideme", "naughty", "seduce", "virgin", "daddy", "mommy",
        "papi", "pawg", "bbg", "boob", "bigpp", "bigdk", "tits", "booty",
        "bussy", "jerkoff", "jackoff", "givehead", "gimmehead", "freeuse",
        "stepbro", "stepsis", "submissive", "domme", "condom", "penis",
        "vagina", "pussy", "cock", "cum", "cunt", "slut", "whore", "pedo" ,
        "horny", "horne"
    };

    private static readonly string[] MaskedExplicitFragments =
    {
        "s3x", "53x", "h0rny", "hxrny", "b00b", "b0ob", "d1ck", "d1k",
        "fck", "fuck", "fuk", "fvck", "fxck", "n0ods", "p3do", "p3d0"
    };

    private static readonly Regex AgeGenderPattern = new(
        @"(?:^|[^a-z0-9])(?:(?:1[3-9]|[2-9][0-9])\s*[mf]|[mf]\s*(?:1[3-9]|[2-9][0-9]))(?:$|[^a-z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly List<Finding> CurrentFindings = new();
    private static readonly HashSet<int> ReportAttempts = new();
    private static readonly Dictionary<int, int> VoteKickCounts = new();
    private static float _nextScanAt;
    private static int _lobbyGameId = int.MinValue;
    private static string _status = "Waiting for a lobby.";

    private const int MaxVoteKicks = 3;

    public static IReadOnlyList<Finding> Findings => CurrentFindings;
    public static string Status => _status;

    public static void Tick()
    {
        if (!CheatToggles.antiDatingEnabled) return;
        if (Time.unscaledTime < _nextScanAt) return;

        _nextScanAt = Time.unscaledTime + 2f;
        ScanNow();
    }

    public static void ScanNow()
    {
        CurrentFindings.Clear();

        if (!Utils.isPlayer || !Utils.isOnlineGame || AmongUsClient.Instance == null)
        {
            _status = "Join an online lobby to scan player names.";
            return;
        }

        int gameId = AmongUsClient.Instance.GameId;
        if (_lobbyGameId != gameId)
        {
            _lobbyGameId = gameId;
            ReportAttempts.Clear();
            VoteKickCounts.Clear();
        }

        if (PlayerControl.AllPlayerControls == null)
        {
            _status = "No connected players found.";
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!IsValidTarget(player)) continue;
            if (!TryClassify(player.Data.PlayerName, out string reason)) continue;

            int clientId = player.Data.ClientId;
            
            if (CheatToggles.antiDatingAutoReport && !ReportAttempts.Contains(clientId))
            {
                TryReportOnce(clientId, player.Data.PlayerName);
            }

            int currentVotes = VoteKickCounts.TryGetValue(clientId, out int count) ? count : 0;
            if (CheatToggles.antiDatingAutoVoteKick && currentVotes < MaxVoteKicks)
            {
                TryVoteKick(clientId, player.Data.PlayerName);
            }

            CurrentFindings.Add(new Finding
            {
                ClientId = clientId,
                PlayerName = player.Data.PlayerName,
                Reason = reason,
                ReportAttempted = ReportAttempts.Contains(clientId),
                VoteKickAttempted = currentVotes > 0
            });
        }

        _status = CurrentFindings.Count == 0
            ? "No suspicious dating-style names detected."
            : $"Flagged {CurrentFindings.Count} player{(CurrentFindings.Count == 1 ? string.Empty : "s")}.";
    }

    public static bool TryClassify(string playerName, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrWhiteSpace(playerName)) return false;

        string rawCompact = NormalizeCompact(playerName, false);
        string searchable = NormalizeForPhraseMatch(playerName);
        string collapsed = CollapseRepeatedCharacters(searchable);
        bool explicitlyNonSoliciting = ContainsAny(searchable, NonSolicitingPhrases);

        foreach (string phrase in DatingPhrases)
        {
            if (explicitlyNonSoliciting) break;
            if (!searchable.Contains(phrase)) continue;
            reason = "explicit dating phrase";
            return true;
        }

        if (!explicitlyNonSoliciting &&
            (ContainsAny(searchable, DatingFragments) || ContainsAny(collapsed, DatingFragments)))
        {
            reason = "dating or roleplay phrase";
            return true;
        }

        if (ContainsExplicitFragment(searchable) || ContainsExplicitFragment(collapsed) ||
            ContainsAny(rawCompact, MaskedExplicitFragments))
        {
            reason = "explicit or sexual phrase";
            return true;
        }

        bool hasPlatform = ContainsAny(searchable, ContactPlatforms) &&
                           !ContainsAny(searchable, ContactFalsePositives);
        bool hasInvite = ContainsAny(searchable, ContactInvites);
        if (hasPlatform)
        {
            reason = hasInvite ? "off-platform contact invitation" : "off-platform contact reference";
            return true;
        }

        string readable = NormalizeForAgeMatch(playerName);
        if (AgeGenderPattern.IsMatch(readable))
        {
            reason = "age/gender advertisement";
            return true;
        }

        return false;
    }

    public static bool TryReportOnce(int clientId, string playerName)
    {
        if (ReportAttempts.Contains(clientId))
        {
            _status = $"{playerName} was already reported in this lobby.";
            return false;
        }

        // Record before invoking game code. Even if the native service rejects the
        // request, this guard will not retry in a loop.
        ReportAttempts.Add(clientId);

        try
        {
            if (!HudManager.InstanceExists || !HudManager.Instance.Chat)
            {
                _status = "The native report menu is not available right now.";
                return false;
            }

            var reportMenu = HudManager.Instance.Chat.GetComponentInChildren<BanMenu>(true);
            if (!reportMenu)
            {
                _status = "The native report menu could not be found.";
                return false;
            }

            reportMenu.Select(clientId);
            reportMenu.ReportPlayer(ReportReasons.InappropriateName);
            _status = $"Submitted one bad-name report for {playerName}.";
            return true;
        }
        catch (Exception exception)
        {
            Centromenu.Log.LogWarning($"Anti-dating report failed for client {clientId}: {exception.Message}");
            _status = $"Could not report {playerName}; no retry will be sent.";
            return false;
        }
    }

    public static bool TryVoteKick(int clientId, string playerName)
    {
        int currentVotes = VoteKickCounts.TryGetValue(clientId, out int count) ? count : 0;
        
        if (currentVotes >= MaxVoteKicks)
        {
            _status = $"Vote-kick limit reached for {playerName} in this lobby.";
            return false;
        }

        try
        {
            // If you are the host, you cannot use the vote system. 
            // Instead, directly kick the player using host authority.
            if (Utils.isHost)
            {
                PlayerControl targetPlayer = null;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data != null && p.Data.ClientId == clientId)
                    {
                        targetPlayer = p;
                        break;
                    }
                }

                if (targetPlayer == null || targetPlayer.Data.Disconnected)
                {
                    _status = $"Target {playerName} is not available to kick.";
                    return false;
                }

                // Host kick command
                AmongUsClient.Instance.KickPlayer(clientId, false);
                
                VoteKickCounts[clientId] = currentVotes + 1;
                _status = $"Host kicked {playerName} (attempt {currentVotes + 1}/{MaxVoteKicks}).";
                return true;
            }

            // Non-host: use the vote system
            if (!VoteBanSystem.Instance)
            {
                _status = "The vote-kick system is not available right now.";
                return false;
            }

            if (VoteBanSystem.Instance.HasMyVote(clientId))
            {
                // If we already have a vote for this specific cycle, 
                // don't increment our local counter as we didn't send a new vote.
                _status = $"You already voted to kick {playerName} this cycle.";
                return false;
            }

            VoteBanSystem.Instance.CmdAddVote(clientId);
            
            // Increment our local tracking counter
            VoteKickCounts[clientId] = currentVotes + 1;
            
            _status = $"Submitted vote-kick {currentVotes + 1}/{MaxVoteKicks} for {playerName}.";
            return true;
        }
        catch (Exception exception)
        {
            Centromenu.Log.LogWarning($"Anti-dating vote-kick failed for client {clientId}: {exception.Message}");
            _status = $"Could not vote-kick {playerName}; no retry will be sent.";
            return false;
        }
    }

    private static bool IsValidTarget(PlayerControl player)
    {
        // Include the host (player.AmOwner) so the scanner evaluates the host's name too
        return player != null && player.Data != null &&
               !player.Data.Disconnected && !string.IsNullOrWhiteSpace(player.Data.PlayerName);
    }

    private static bool ContainsAny(string value, IEnumerable<string> candidates)
    {
        foreach (string candidate in candidates)
        {
            if (value.Contains(candidate)) return true;
        }

        return false;
    }

    private static bool ContainsExplicitFragment(string value)
    {
        foreach (string candidate in ExplicitFragments)
        {
            if (!value.Contains(candidate)) continue;

            // Small exception list for ordinary words which contain a filtered
            // fragment. This mirrors the useful part of SaferAmongUs' dictionary
            // checks without importing its very broad personal-name blacklist.
            if (candidate == "sex" && (value.Contains("essex") || value.Contains("sextant"))) continue;
            if (candidate == "cock" && (value.Contains("peacock") || value.Contains("cocktail"))) continue;
            if (candidate == "cum" && (value.Contains("cucumber") || value.Contains("cumbersome"))) continue;
            if (candidate == "cunt" && value.Contains("scunthorpe")) continue;
            if (candidate == "pedo" && value.Contains("torpedo")) continue;

            return true;
        }

        return false;
    }

    private static string NormalizeForPhraseMatch(string value)
    {
        return NormalizeCompact(value, true);
    }

    private static string NormalizeCompact(string value, bool translateLeetspeak)
    {
        string decomposed = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed.ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;

            char normalized = NormalizeConfusable(character);
            if (translateLeetspeak)
            {
                normalized = normalized switch
                {
                    '0' => 'o',
                    '1' => 'i',
                    '3' => 'e',
                    '4' => 'a',
                    '5' => 's',
                    '7' => 't',
                    '@' => 'a',
                    '$' => 's',
                    _ => normalized
                };
            }

            if (char.IsLetterOrDigit(normalized)) builder.Append(normalized);
        }

        return builder.ToString();
    }

    private static string NormalizeForAgeMatch(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed.ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;

            char normalized = NormalizeConfusable(character) switch
            {
                '0' => 'o',
                '3' => 'e',
                '4' => 'a',
                '5' => 's',
                '7' => 't',
                '@' => 'a',
                '$' => 's',
                _ => character
            };

            builder.Append(char.IsLetterOrDigit(normalized) ? normalized : ' ');
        }

        return builder.ToString();
    }

    private static string CollapseRepeatedCharacters(string value)
    {
        if (value.Length < 2) return value;

        var builder = new StringBuilder(value.Length);
        char previous = '\0';
        foreach (char character in value)
        {
            if (character == previous) continue;
            builder.Append(character);
            previous = character;
        }

        return builder.ToString();
    }

    private static char NormalizeConfusable(char character)
    {
        return character switch
        {
            // Common Greek/Cyrillic lookalikes used to bypass ASCII filters.
            'α' or 'а' => 'a',
            'β' or 'в' or 'ь' or 'ъ' => 'b',
            'с' => 'c',
            'δ' or 'д' => 'd',
            'ε' or 'е' or 'ё' => 'e',
            'ι' or 'і' => 'i',
            'κ' or 'к' => 'k',
            'м' => 'm',
            'η' or 'п' => 'n',
            'ο' or 'о' or 'σ' => 'o',
            'ρ' or 'р' => 'p',
            'т' or 'τ' => 't',
            'υ' or 'ц' => 'u',
            'χ' or 'х' => 'x',
            'у' => 'y',
            'з' => '3',
            _ => character
        };
    }
}
