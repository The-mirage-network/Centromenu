using BepInEx.Logging;
using CentroFeatures.modules;
using CentroFeatures.routines;
using CentroFeatures.ui;

namespace CentroFeatures;





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


        mainUI = plugin.AddComponent<MainUI>();
        mainUI.enabled = false;

        notifications = plugin.AddComponent<NotificationManager>();
        modules = plugin.AddComponent<ModuleManager>();
        routines = plugin.AddComponent<RoutineManager>();



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
