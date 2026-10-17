using BepInEx.Logging;
using CentroFeatures.modules;
using CentroFeatures.routines;
using CentroFeatures.ui;

namespace CentroFeatures;

/// <summary>
/// Compatibility host for the Centro feature set. CentroMenu remains the only
/// BepInEx plugin and the only visible menu.
/// </summary>
internal static class Centro
{
    internal static ManualLogSource Log { get; private set; }
    internal static MainUI mainUI;
    internal static NotificationManager notifications;
    internal static ModuleManager modules;
    internal static RoutineManager routines;
    internal static readonly ConfigManager config = new();

    internal static void Initialize(Centromenu.Centromenu plugin)
    {
        Log = Centromenu.Centromenu.Log;

        // Retained only as a config-data holder; Centro's separate window never renders.
        mainUI = plugin.AddComponent<MainUI>();
        mainUI.enabled = false;

        notifications = plugin.AddComponent<NotificationManager>();
        modules = plugin.AddComponent<ModuleManager>();
        routines = plugin.AddComponent<RoutineManager>();

        // Imported configuration is intentionally not loaded: Teleport Flooder
        // is the only retained Centro option and starts disabled every session.
        Log.LogInfo("Teleport Flooder compatibility layer loaded into CentroMenu.");
    }

    internal static void Eject()
    {
        notifications?.ClearNotifications();

        if (modules != null)
        {
            foreach (Module module in modules.moduleList)
                module.Enabled = false;
        }

        if (routines != null)
        {
            foreach (Routine routine in routines.routineList)
                routine.Enabled = false;
        }

        notifications?.Send("CentroMenu", "Centro extras have been disabled.", 5f);
    }
}
