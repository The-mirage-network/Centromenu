using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;

namespace Centromenu;

public enum LobbyAvailability
{
    Unknown,
    Checking,
    Available,
    Unavailable
}

public sealed class LobbyHistoryEntry
{
    public string Code { get; init; }
    public DateTime LastJoined { get; init; }
    public LobbyAvailability Availability { get; set; }
    public string OwnerName { get; set; }
    public int ServerAgeSeconds { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime LastChecked { get; set; }
}

public static class LobbyHistory
{
    private static readonly string HistoryPath = Path.Combine(Paths.ConfigPath, "CentromenuLobbyHistory.txt");
    private static readonly List<LobbyHistoryEntry> EntriesInternal = new();
    private static bool _loaded;

    public static IReadOnlyList<LobbyHistoryEntry> Entries
    {
        get
        {
            EnsureLoaded();
            return EntriesInternal;
        }
    }

    public static void Record(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;

        EnsureLoaded();
        code = code.Trim().ToUpperInvariant();
        EntriesInternal.RemoveAll(entry => string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase));
        EntriesInternal.Insert(0, new LobbyHistoryEntry
        {
            Code = code,
            LastJoined = DateTime.Now,
            Availability = LobbyAvailability.Available
        });

        Save();
    }

    public static void Remove(string code)
    {
        EnsureLoaded();
        EntriesInternal.RemoveAll(entry => string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public static void Clear()
    {
        EnsureLoaded();
        EntriesInternal.Clear();
        Save();
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        if (!File.Exists(HistoryPath)) return;

        try
        {
            foreach (string line in File.ReadAllLines(HistoryPath))
            {
                string[] fields = line.Split('|');
                if (fields.Length != 2 || string.IsNullOrWhiteSpace(fields[0])) continue;

                if (!long.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
                    continue;

                EntriesInternal.Add(new LobbyHistoryEntry
                {
                    Code = fields[0].Trim().ToUpperInvariant(),
                    LastJoined = new DateTime(ticks, DateTimeKind.Local),
                    Availability = LobbyAvailability.Unknown
                });
            }
        }
        catch (Exception exception)
        {
            Centromenu.Log.LogWarning($"Could not load lobby history: {exception.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            var lines = new string[EntriesInternal.Count];
            for (int i = 0; i < EntriesInternal.Count; i++)
            {
                LobbyHistoryEntry entry = EntriesInternal[i];
                lines[i] = $"{entry.Code}|{entry.LastJoined.Ticks.ToString(CultureInfo.InvariantCulture)}";
            }

            File.WriteAllLines(HistoryPath, lines);
        }
        catch (Exception exception)
        {
            Centromenu.Log.LogWarning($"Could not save lobby history: {exception.Message}");
        }
    }
}
