using System;
using HarmonyLib;
using UnityEngine;

namespace Centromenu;

/// <summary>
/// Keeps lobby refreshes from repeatedly tripping the matchmaker rate limit.
/// This is intentionally always enabled: there is no menu toggle or profile setting.
/// </summary>
internal static class LobbyRequestRecovery
{
    private const float MinimumRequestIntervalSeconds = 6f;
    private const float InitialRateLimitCooldownSeconds = 30f;
    private const float MaximumRateLimitCooldownSeconds = 120f;

    private static float _nextAllowedRequestTime;
    private static float _rateLimitCooldownSeconds = InitialRateLimitCooldownSeconds;
    private static float _lastRateLimitTime = float.NegativeInfinity;

    public static bool TryBeginRequest()
    {
        var now = Time.realtimeSinceStartup;
        if (now < _nextAllowedRequestTime)
        {
            return false;
        }

        _nextAllowedRequestTime = now + MinimumRequestIntervalSeconds;
        return true;
    }

    public static void OnRateLimited()
    {
        var now = Time.realtimeSinceStartup;

        // The same matchmaker failure can reach both the popup hook and the
        // find-by-code callback. Treat those as one event so the backoff does
        // not jump from 30 to 60 seconds for a single response.
        if (now - _lastRateLimitTime < 1f) return;
        _lastRateLimitTime = now;

        _nextAllowedRequestTime = Math.Max(
            _nextAllowedRequestTime,
            now + _rateLimitCooldownSeconds);

        Centromenu.Log.LogWarning(
            $"Lobby requests were rate limited. Refresh is paused for {_rateLimitCooldownSeconds:0} seconds.");

        _rateLimitCooldownSeconds = Math.Min(
            _rateLimitCooldownSeconds * 2f,
            MaximumRateLimitCooldownSeconds);
    }

    public static void OnRequestSucceeded()
    {
        _rateLimitCooldownSeconds = InitialRateLimitCooldownSeconds;
        _nextAllowedRequestTime = Time.realtimeSinceStartup + MinimumRequestIntervalSeconds;
    }

    public static bool IsLobbyRateLimit(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.IndexOf("too many lobby requests", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsMissingGameMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        return message.IndexOf("could not find the game", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("game you're looking for", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("game your looking for", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

[HarmonyPatch(typeof(HttpMatchmakerManager), "SetDisconnectInfoAndShowPopup")]
public static class HttpMatchmakerManager_SetDisconnectInfoAndShowPopup_LobbyRequestRecovery
{
    public static bool Prefix(object __0)
    {
        // This recovery code exists for the silent lobby-history probes only.
        // Never consume errors from the game's public browser or an explicit
        // join attempt: doing so can leave the Find Game screen looking as if
        // it is loading forever with no useful error shown to the player.
        if (!LobbyLookupQueue.ShouldSuppressLookupErrors) return true;

        string message = null;
        try
        {
            message = Traverse.Create(__0).Property("CustomDisconnect").GetValue<string>();
        }
        catch (Exception exception)
        {
            Centromenu.Log.LogDebug($"Could not inspect matchmaker popup: {exception.Message}");
        }

        if (LobbyRequestRecovery.IsLobbyRateLimit(message))
        {
            LobbyRequestRecovery.OnRateLimited();
            return false;
        }

        // History checks are silent probes. An expired code should update its row,
        // not display a full-screen join error intended for an explicit join attempt.
        return !LobbyRequestRecovery.IsMissingGameMessage(message);
    }
}
