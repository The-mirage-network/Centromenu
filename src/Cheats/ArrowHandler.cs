using UnityEngine;
using System.Linq;

namespace Centromenu;

public static class ArrowHandler
{

    private static GameObject _cachedArrowTemplate;


    public static bool IsOwnedAndIncomplete(NormalPlayerTask task)
    {
        if (task.Owner == null || !task.Owner.AmOwner) return false;

        return !task.IsComplete;
    }


    private static void CacheArrowFromShipStatus()
    {
        if (_cachedArrowTemplate != null) return;

        NormalPlayerTask[][] allTasksArrays = new NormalPlayerTask[][]
        {
            ShipStatus.Instance.CommonTasks,
            ShipStatus.Instance.LongTasks,
            ShipStatus.Instance.ShortTasks
        };

        foreach (var tasks in allTasksArrays)
        {
            foreach (var task in tasks)
            {
                if (task.Arrow != null)
                {
                    _cachedArrowTemplate = task.Arrow.gameObject;
                    Centromenu.Log.LogInfo($"Cached Arrow.gameObject for task {task.TaskType}");
                    return;
                }
                Centromenu.Log.LogInfo($"No Arrow.gameObject found for task {task.TaskType}");
            }
        }
    }


    public static ArrowBehaviour CreateArrowForTask(NormalPlayerTask task)
    {

        CacheArrowFromShipStatus();


        var arrowObj = Object.Instantiate(_cachedArrowTemplate, task.transform, false);

        return arrowObj.GetComponent<ArrowBehaviour>();
    }


    public static void EnsureArrowExists(NormalPlayerTask task)
    {

        if (!IsOwnedAndIncomplete(task) || task.Arrow != null) return;

        task.Arrow = CreateArrowForTask(task);
    }



    public static bool NeedsSpecialTarget(NormalPlayerTask task)
    {
        return task.TaskType is TaskTypes.AlignEngineOutput or TaskTypes.ReplaceParts or TaskTypes.RoastMarshmallow or TaskTypes.StartFans or TaskTypes.PickUpTowels;
    }


    private static void SetArrowTarget(NormalPlayerTask task, Console targetConsole)
    {
        if (targetConsole == null) return;

        task.Arrow.target = targetConsole.transform.position;
        task.StartAt = targetConsole.Room;
    }



    public static void SetArrowTargetForSpecialTasks(NormalPlayerTask task)
    {
        if (task.Arrow == null) return;

        switch (task.TaskType)
        {

            case TaskTypes.AlignEngineOutput when task.TaskStep == 0:
            {
                Il2CppSystem.Collections.Generic.List<Console> consoles = task.FindConsoles();

                if (consoles is { Count: > 0 })
                {
                    SetArrowTarget(task, consoles[0]);
                }

                break;
            }

            case TaskTypes.ReplaceParts when task.taskStep == 0:
            {
                Il2CppSystem.Collections.Generic.List<Console> consoles = NormalPlayerTask.PickRandomConsoles(0, TaskTypes.ReplaceParts);

                if (consoles is { Count: > 0 })
                {
                    var firstConsole = consoles.ToArray().FirstOrDefault(c => c.ConsoleId == task.Data[0]);
                    SetArrowTarget(task, firstConsole);
                }

                break;
            }

            case TaskTypes.RoastMarshmallow when task.taskStep == 0:
            {
                Il2CppSystem.Collections.Generic.List<Console> consoles = NormalPlayerTask.PickRandomConsoles(0, TaskTypes.RoastMarshmallow);

                if (consoles is { Count: > 0 })
                {
                    var stickConsole = consoles.ToArray().FirstOrDefault(c => c.ConsoleId == task.Data[0]);
                    SetArrowTarget(task, stickConsole);
                }

                break;
            }

            case TaskTypes.StartFans when task.taskStep == 0:
            {
                var targetConsole = task.FindSpecialConsole((Il2CppSystem.Func<Console, bool>)((Console c) => task.ValidConsole(c) && c.ConsoleId == 0));
                SetArrowTarget(task, targetConsole);

                break;
            }

            case TaskTypes.PickUpTowels when task.TaskStep == 0:
            {
                var targetConsole = task.FindSpecialConsole((Il2CppSystem.Func<Console, bool>)((Console c) => task.ValidConsole(c)));
                SetArrowTarget(task, targetConsole);

                break;
            }
        }
    }
}
