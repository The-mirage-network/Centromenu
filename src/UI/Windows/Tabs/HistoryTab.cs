using System;
using System.Collections.Generic;
using InnerNet;
using UnityEngine;

namespace Centromenu;

public sealed class HistoryTab : ITab
{
    private Vector2 _scrollPosition;
    private bool _hasRefreshed;

    public string name => "History";

    public void Draw()
    {
        IReadOnlyList<LobbyHistoryEntry> entries = LobbyHistory.Entries;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Recently joined lobbies", GUIStylePreset.TabSubtitle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", GUIStylePreset.NormalButton, GUILayout.Width(85f)))
            RefreshAll();

        GUI.enabled = entries.Count > 0;
        if (GUILayout.Button("Clear all", GUIStylePreset.NormalButton, GUILayout.Width(85f)))
        {
            LobbyHistory.Clear();
            _hasRefreshed = false;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);

        if (entries.Count == 0)
        {
            GUILayout.Label("Games you join will appear here with their lobby code.");
            GUILayout.Label("Open History again later to check whether they are still available.");
            return;
        }

        if (!_hasRefreshed)
        {
            _hasRefreshed = true;
            RefreshAll();
        }

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
        LobbyHistoryEntry removeEntry = null;

        foreach (LobbyHistoryEntry entry in entries)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(entry.Code, GUIStylePreset.TabSubtitle);
            GUILayout.Label($"Joined {entry.LastJoined:g}");
            GUILayout.Label(GetAvailabilityText(entry));
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            bool canJoin = entry.Availability == LobbyAvailability.Available &&
                           AmongUsClient.Instance && !AmongUsClient.Instance.AmConnected;
            GUI.enabled = canJoin;
            if (GUILayout.Button("Join", GUIStylePreset.NormalButton, GUILayout.Width(72f), GUILayout.Height(32f)))
                Join(entry);
            GUI.enabled = true;

            if (GUILayout.Button("Clear", GUIStylePreset.NormalButton, GUILayout.Width(72f), GUILayout.Height(32f)))
                removeEntry = entry;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        GUILayout.EndScrollView();

        if (removeEntry != null)
            LobbyHistory.Remove(removeEntry.Code);
    }

    private static string GetAvailabilityText(LobbyHistoryEntry entry)
    {
        if (IsCurrentlyJoined(entry.Code)) return "Current lobby";

        return entry.Availability switch
        {
            LobbyAvailability.Checking => "Checking availability...",
            LobbyAvailability.Available => "Available",
            LobbyAvailability.Unavailable => "Unavailable",
            _ => "Not checked"
        };
    }

    private static bool IsCurrentlyJoined(string code)
    {
        return AmongUsClient.Instance && AmongUsClient.Instance.AmConnected &&
               string.Equals(AmongUsClient_OnGameJoined.lastGameIdString, code, StringComparison.OrdinalIgnoreCase);
    }

    private static void Join(LobbyHistoryEntry entry)
    {
        if (!AmongUsClient.Instance || entry.Availability != LobbyAvailability.Available) return;

        int gameId = GameCode.GameNameToInt(entry.Code);
        AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoFindGameInfoFromCodeAndJoin(gameId));
    }

    private static void RefreshAll()
    {
        if (!AmongUsClient.Instance) return;
        LobbyLookupQueue.EnqueueAll();
    }
}
