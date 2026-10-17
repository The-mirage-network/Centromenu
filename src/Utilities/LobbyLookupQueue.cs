using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using InnerNet;
using UnityEngine;

namespace Centromenu;

/// <summary>
/// Serializes history lookups so opening a tab cannot send a burst of requests
/// to the matchmaker. Both History and Votekick share this queue.
/// </summary>
public static class LobbyLookupQueue
{
    private const float RequestTimeoutSeconds = 20f;

    private static readonly Queue<LobbyHistoryEntry> Pending = new();
    private static readonly HashSet<string> PendingCodes = new(StringComparer.OrdinalIgnoreCase);
    private static LobbyHistoryEntry _activeEntry;
    private static Coroutine _activeCoroutine;
    private static float _requestStartedAt;
    private static float _suppressErrorsUntil;
    private static int _requestVersion;

    public static bool IsRequestInFlight => _activeEntry != null;
    public static bool ShouldSuppressLookupErrors =>
        IsRequestInFlight || Time.realtimeSinceStartup < _suppressErrorsUntil;

    public static void EnqueueAll()
    {
        // CoFindGameInfoFromCode uses the same matchmaker state as the live
        // connection. Running it from the History/Votekick UI while connected
        // can display a stale-code error and stall every player in the lobby.
        if (!AmongUsClient.Instance || AmongUsClient.Instance.AmConnected) return;

        foreach (LobbyHistoryEntry entry in LobbyHistory.Entries)
        {
            if (IsCurrentlyJoined(entry.Code))
            {
                entry.Availability = LobbyAvailability.Available;
                continue;
            }

            // Keep a known-good row visible while its details are refreshed.
            if (entry.Availability != LobbyAvailability.Available)
                entry.Availability = LobbyAvailability.Checking;

            if ((_activeEntry == null ||
                 !string.Equals(_activeEntry.Code, entry.Code, StringComparison.OrdinalIgnoreCase)) &&
                PendingCodes.Add(entry.Code))
            {
                Pending.Enqueue(entry);
            }
        }
    }

    public static void Tick()
    {
        if (!AmongUsClient.Instance) return;

        // A user can join while a history lookup is still completing. Stop and
        // invalidate that lookup before it can touch the active lobby.
        if (AmongUsClient.Instance.AmConnected)
        {
            CancelActiveRequest();
            return;
        }

        if (_activeEntry != null)
        {
            if (Time.realtimeSinceStartup - _requestStartedAt < RequestTimeoutSeconds) return;

            _activeEntry.Availability = LobbyAvailability.Unknown;
            _activeEntry = null;
            _activeCoroutine = null;
            _requestVersion++;
        }

        if (Pending.Count == 0 || !LobbyRequestRecovery.TryBeginRequest()) return;

        LobbyHistoryEntry entry = Pending.Dequeue();
        PendingCodes.Remove(entry.Code);
        _activeEntry = entry;
        _requestStartedAt = Time.realtimeSinceStartup;
        int requestVersion = ++_requestVersion;

        int gameId = GameCode.GameNameToInt(entry.Code);
        var callback = DelegateSupport.ConvertDelegate<
            Il2CppSystem.Action<HttpMatchmakerManager.FindGameByCodeResponse, string>>(
            new Action<HttpMatchmakerManager.FindGameByCodeResponse, string>((response, error) =>
            {
                HandleResponse(entry, requestVersion, response, error);
            }));

        _activeCoroutine = AmongUsClient.Instance.StartCoroutine(
            AmongUsClient.Instance.CoFindGameInfoFromCode(gameId, callback));
    }

    private static void HandleResponse(
        LobbyHistoryEntry entry,
        int requestVersion,
        HttpMatchmakerManager.FindGameByCodeResponse response,
        string error)
    {
        if (requestVersion != _requestVersion) return;

        // The game can show the error popup just after invoking this callback.
        // Keep a short grace period so a failed silent probe stays silent.
        _suppressErrorsUntil = Time.realtimeSinceStartup + 2f;

        if (LobbyRequestRecovery.IsLobbyRateLimit(error))
        {
            entry.Availability = LobbyAvailability.Unknown;
            LobbyRequestRecovery.OnRateLimited();
        }
        else if (response?.Game == null)
        {
            entry.Availability = LobbyAvailability.Unavailable;
        }
        else
        {
            entry.OwnerName = response.Game.TrueHostName;
            entry.ServerAgeSeconds = response.Game.Age;
            entry.PlayerCount = response.Game.PlayerCount;
            entry.MaxPlayers = response.Game.MaxPlayers;
            entry.LastChecked = DateTime.Now;
            entry.Availability = LobbyAvailability.Available;
            LobbyRequestRecovery.OnRequestSucceeded();
        }

        if (ReferenceEquals(_activeEntry, entry))
        {
            _activeEntry = null;
            _activeCoroutine = null;
        }
    }

    private static void CancelActiveRequest()
    {
        if (_activeEntry == null) return;

        if (_activeCoroutine != null && AmongUsClient.Instance)
            AmongUsClient.Instance.StopCoroutine(_activeCoroutine);

        _activeEntry.Availability = LobbyAvailability.Unknown;
        _activeEntry = null;
        _activeCoroutine = null;
        _suppressErrorsUntil = 0f;
        _requestVersion++;
    }

    private static bool IsCurrentlyJoined(string code)
    {
        return AmongUsClient.Instance && AmongUsClient.Instance.AmConnected &&
               string.Equals(AmongUsClient_OnGameJoined.lastGameIdString, code,
                   StringComparison.OrdinalIgnoreCase);
    }
}
