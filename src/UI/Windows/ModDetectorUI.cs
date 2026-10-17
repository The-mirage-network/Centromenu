using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace Centromenu;

/// <summary>
/// Standalone plugin manager window. Deletions are delegated to a hidden helper
/// which waits for the current Among Us process to exit before touching files.
/// </summary>
public sealed class ModDetectorUI : MonoBehaviour
{
    private const int WindowWidth = 620;
    private const int WindowHeight = 480;

    public static Rect windowRect;
    public static bool IsVisible { get; private set; }

    private readonly List<PluginEntry> _plugins = new();
    private readonly HashSet<string> _queuedPaths = new(StringComparer.OrdinalIgnoreCase);
    private Vector2 _scrollPosition;
    private string _statusMessage = "";

    private string PluginDirectory => Path.GetFullPath(Paths.PluginPath);

    private void Start()
    {
        windowRect = new Rect(
            Screen.width / 2f - WindowWidth / 2f,
            Screen.height / 2f - WindowHeight / 2f,
            WindowWidth,
            WindowHeight);
    }

    public void Open()
    {
        IsVisible = true;
        RefreshPlugins();
    }

    private void OnGUI()
    {
        if (!IsVisible || Centromenu.isPanicked) return;

        Centromenu.menuUI?.InitStyles();
        UIHelpers.ApplyUIColor();
        UIHelpers.BeginPanelWindow();
        windowRect = GUI.Window(
            (int)WindowId.ModDetectorUI,
            windowRect,
            (GUI.WindowFunction)DrawWindow,
            "Mod detector");
        UIHelpers.RestoreContentColor();
    }

    private void DrawWindow(int windowId)
    {
        UIHelpers.RestoreContentColor();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Detected plugins: {_plugins.Count}", GUIStylePreset.TabSubtitle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUIStylePreset.NormalButton, GUILayout.Width(90f)))
        {
            RefreshPlugins();
        }
        if (GUILayout.Button("Close", GUIStylePreset.NormalButton, GUILayout.Width(75f)))
        {
            IsVisible = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Select plugin DLLs to remove after Among Us closes.");
        GUILayout.Space(5f);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.ExpandHeight(true));
        foreach (PluginEntry plugin in _plugins)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            bool selected = GUILayout.Toggle(plugin.Selected, "", GUILayout.Width(24f));
            plugin.Selected = selected && !_queuedPaths.Contains(plugin.FullPath);
            GUILayout.Label(plugin.DisplayName, GUILayout.ExpandWidth(true));
            if (_queuedPaths.Contains(plugin.FullPath))
            {
                GUILayout.Label("Queued", GUILayout.Width(65f));
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select all", GUIStylePreset.NormalButton))
        {
            foreach (PluginEntry plugin in _plugins)
                plugin.Selected = !_queuedPaths.Contains(plugin.FullPath);
        }
        if (GUILayout.Button("Clear selection", GUIStylePreset.NormalButton))
        {
            foreach (PluginEntry plugin in _plugins)
                plugin.Selected = false;
        }
        GUILayout.EndHorizontal();

        int selectedCount = _plugins.Count(plugin => plugin.Selected);
        GUI.enabled = selectedCount > 0;
        if (GUILayout.Button(
                selectedCount == 0
                    ? "Delete selected after exit"
                    : $"Delete {selectedCount} selected after exit",
                GUIStylePreset.NormalButton,
                GUILayout.Height(34f)))
        {
            QueueSelectedPlugins();
        }
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            GUILayout.Label(_statusMessage);
        }

        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    private void RefreshPlugins()
    {
        var previousSelections = _plugins
            .Where(plugin => plugin.Selected)
            .Select(plugin => plugin.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _plugins.Clear();

        try
        {
            Directory.CreateDirectory(PluginDirectory);
            foreach (string path in Directory
                         .EnumerateFiles(PluginDirectory, "*.dll", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                string fullPath = Path.GetFullPath(path);
                _plugins.Add(new PluginEntry
                {
                    FullPath = fullPath,
                    DisplayName = Path.GetRelativePath(PluginDirectory, fullPath),
                    Selected = previousSelections.Contains(fullPath) && !_queuedPaths.Contains(fullPath)
                });
            }

            _statusMessage = _plugins.Count == 0
                ? $"No plugin DLLs found in {PluginDirectory}"
                : "";
        }
        catch (Exception exception)
        {
            _statusMessage = $"Could not scan the plugins folder: {exception.Message}";
            Centromenu.Log.LogError(exception);
        }
    }

    private void QueueSelectedPlugins()
    {
        string[] selectedPaths = _plugins
            .Where(plugin => plugin.Selected)
            .Select(plugin => plugin.FullPath)
            .Where(IsSafePluginPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            _statusMessage = "No valid plugin DLLs were selected.";
            return;
        }

        try
        {
            Directory.CreateDirectory(Paths.CachePath);
            string manifestPath = Path.Combine(
                Paths.CachePath,
                $"Centromenu.PluginDeletes.{Guid.NewGuid():N}.txt");
            File.WriteAllLines(manifestPath, selectedPaths, Encoding.UTF8);

            StartDeletionHelper(manifestPath);

            foreach (string path in selectedPaths)
            {
                _queuedPaths.Add(path);
            }

            foreach (PluginEntry plugin in _plugins)
            {
                if (_queuedPaths.Contains(plugin.FullPath)) plugin.Selected = false;
            }

            _statusMessage = $"Queued {selectedPaths.Length} plugin(s). They remain active until Among Us closes.";
        }
        catch (Exception exception)
        {
            _statusMessage = $"Could not queue deletion: {exception.Message}";
            Centromenu.Log.LogError(exception);
        }
    }

    private bool IsSafePluginPath(string path)
    {
        try
        {
            string root = PluginDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase)
                   && File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    private void StartDeletionHelper(string manifestPath)
    {
        int processId = Process.GetCurrentProcess().Id;
        string escapedManifest = EscapePowerShellLiteral(manifestPath);
        string escapedRoot = EscapePowerShellLiteral(PluginDirectory);
        string script =
            $"Wait-Process -Id {processId} -ErrorAction SilentlyContinue; " +
            "Start-Sleep -Milliseconds 500; " +
            $"$manifest='{escapedManifest}'; $root=[IO.Path]::GetFullPath('{escapedRoot}').TrimEnd('\\')+'\\'; " +
            "if(Test-Path -LiteralPath $manifest){ " +
            "foreach($path in [IO.File]::ReadAllLines($manifest)){ " +
            "try{$full=[IO.Path]::GetFullPath($path); " +
            "if($full.StartsWith($root,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetExtension($full) -ieq '.dll'){ " +
            "Remove-Item -LiteralPath $full -Force -ErrorAction SilentlyContinue }}catch{} }; " +
            "Remove-Item -LiteralPath $manifest -Force -ErrorAction SilentlyContinue }";

        string encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {encodedScript}",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static string EscapePowerShellLiteral(string value) => value.Replace("'", "''");

    private sealed class PluginEntry
    {
        public string FullPath { get; init; }
        public string DisplayName { get; init; }
        public bool Selected { get; set; }
    }
}
