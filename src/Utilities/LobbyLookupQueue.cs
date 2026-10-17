using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using InnerNet;
using UnityEngine;

namespace Centromenu;





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



        if (!AmongUsClient.Instance || AmongUsClient.Instance.AmConnected) return;

        foreach (LobbyHistoryEntry entry in LobbyHistory.Entries)
        {
            if (IsCurrentlyJoined(entry.Code))
            {
                entry.Availability = LobbyAvailability.Available;
                continue;
            }


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
