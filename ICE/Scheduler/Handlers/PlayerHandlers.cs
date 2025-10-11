using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Config;
using Dalamud.Game.Gui.Toast;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using Time = (int start, int end);

namespace ICE.Scheduler.Handlers;

internal static unsafe class PlayerHandlers
{
    public static readonly Dictionary<Time, string[]> stage9TimeMap = new()
    {
        { (0, 1), new[] { "刻木匠", "炼金术士", "雕金匠" } },
        { (2, 3), new[] { "采矿工" } },
        { (4, 5), new[] { "锻铁匠", "烹调师", "制革匠" } },
        { (6, 7), new[] { "捕鱼人" } },
        { (8, 9), new[] { "铸甲匠", "裁衣匠",  } },
        { (10, 11), new[] { "园艺工" } },
        { (12, 13), new[] { "雕金匠", "刻木匠", "炼金术士" } },
        { (14, 15), new[] { "采矿工" } },
        { (16, 17), new[] { "制革匠", "锻铁匠", "烹调师" } },
        { (18, 19), new[] { "捕鱼人" } },
        { (20, 21), new[] { "裁衣匠", "铸甲匠" } },
        { (22, 23), new[] { "园艺工" } }
    };

    public static readonly Dictionary<Time, string[]> PhaennaMap = new()
    {
        { (0, 2), new [] { "刻木匠", "制革匠", "炼金术士", "园艺工" } },
        { (2, 4), new [] { "采矿工" } },
        { (0, 4), new [] { "铸甲匠" } },
        { (4, 6), new [] { "锻铁匠", "制革匠", "裁衣匠", "烹调师" } },
        { (4, 8), new [] { "雕金匠", "炼金术士", "捕鱼人" } },
        { (6, 8), new [] { "捕鱼人" } },
        { (8, 10), new [] { "刻木匠", "铸甲匠", "裁衣匠", "炼金术士", "捕鱼人" } },
        { (8, 12), new [] { "制革匠", "烹调师", "园艺工" } },
        { (10, 12), new [] { "园艺工"} },
        { (12, 14), new [] { "锻铁匠", "雕金匠", "烹调师" } },
        { (12, 16), new [] { "裁衣匠" } },
        { (16, 18), new [] { "铸甲匠", "采矿工" } },
        { (16, 20), new [] { "刻木匠" } },
        { (20, 22), new [] { "雕金匠" } },
        { (20, 24), new [] { "锻铁匠" } },
    };

    private static readonly uint stellarSprintID = 4398;
    public static bool IsAutohookLoaded { get; private set; }
    public static bool IsMissfisherLoaded { get; private set; }

    public static float Distance(this Vector3 v, Vector3 v2)
    {
        return new Vector2(v.X - v2.X, v.Z - v2.Z).Length();
    }
    public static unsafe bool IsMoving()
    {
        return AgentMap.Instance()->IsPlayerMoving;
    }

    internal static void Tick()
    {
        P.overlayWindow.IsOpen = C.ShowOverlay && PlayerHelper.IsInCosmicZone() && PlayerHelper.UsingSupportedJob();

        if (C.MoonSprint && PlayerHelper.IsInCosmicZone() && !PlayerHelper.HasStatusId(stellarSprintID) && Svc.Condition[ConditionFlag.NormalConditions] && IsMoving()) UseSprint();

        if ((!PlayerHelper.IsInCosmicZone() || !PlayerHelper.UsingSupportedJob()) && SchedulerMain.State != IceState.Idle)
        {
            DisablePlugin();
        }
    }

    public static void AutoAntiAFK()
    {
        if (C.AutoAntiAFK)
        {
            // 启用自动暂离
            if (Svc.GameConfig.TryGet(SystemConfigOption.AutoAfkSwitchingTime, out uint val))
            {
                if (val != 0)
                {
                    Svc.GameConfig.Set(SystemConfigOption.AutoAfkSwitchingTime, 0u);
                    Svc.Toasts.ShowQuest("超过时间后自动切换为离开状态 已更改为 \"不切换\"", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
                    DuoLog.Warning($"您的 自动离开设置 处于危险设置状态，已为您更改到 \"不切换\"，这是为了避免游戏自动切换为离开状态后带着椅子进行任务。");
                }
            }
        }
    }

    public static void UpdateFishingPluginStatus()
    {
        Svc.PluginInterface.InstalledPlugins.TryGetFirst(x => x.InternalName == "AutoHook", out var Autohook);
        Svc.PluginInterface.InstalledPlugins.TryGetFirst(x => x.InternalName == "MissFisher", out var Missfisher);
        IsAutohookLoaded = Autohook?.IsLoaded ?? false;
        IsMissfisherLoaded = Missfisher?.IsLoaded ?? false;
        //DuoLog.Debug($"Autohook状态: {IsAutohookLoaded}");
        //DuoLog.Debug($"Missfisher状态: {IsMissfisherLoaded}");
    }

    // For MissFisher plugin
    public static void CheckHookPluginConflicts()
    {
        UpdateFishingPluginStatus();
        if (IsAutohookLoaded && IsMissfisherLoaded)
        {
            Svc.Toasts.ShowQuest($"注意！您同时启用了 MissFisher 与 AutoHook 插件，请禁用不使用的钓鱼插件以保证插件正常运行！", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
            DuoLog.Warning("注意！您同时启用了 MissFisher 与 AutoHook 插件，请禁用不使用的钓鱼插件以保证插件正常运行！");
        }
        else if (!IsAutohookLoaded && !IsMissfisherLoaded)
        {
            //Svc.Toasts.ShowQuest("请安装并启用 AutoHook 或 MissFisher 其中一个插件作为钓鱼插件，否则钓鱼将无法自动运行。", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
            //DuoLog.Warning("请安装并启用 AutoHook 或 MissFisher 其中一个插件作为钓鱼插件，否则钓鱼将无法自动运行。");
            //DuoLog.Warning("此报错也可能发生在 UpdateFishingPluginStatus() 没有被正确执行时");
        }
        else if (C.AutoFisherCastSwitch)
        {
            if (IsAutohookLoaded && !IsMissfisherLoaded && !C.AutoFisherCast)
            {
                Svc.Toasts.ShowQuest($"检测到您正在使用 AutoHook 作为钓鱼插件，自动启用了\"自动在钓鱼任务开始时抛竿\"设置", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
                DuoLog.Warning("检测到您正在使用 AutoHook 作为钓鱼插件，自动启用了\"自动在钓鱼任务开始时抛竿\"设置");
                C.AutoFisherCast = true;
                C.Save();
            }
            else if (!IsAutohookLoaded && IsMissfisherLoaded && C.AutoFisherCast)
            {
                Svc.Toasts.ShowQuest($"检测到您正在使用 MissFisher 作为钓鱼插件，自动禁用了\"自动在钓鱼任务开始时抛竿\"设置", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
                DuoLog.Warning("检测到您正在使用 MissFisher 作为钓鱼插件，自动禁用了\"自动在钓鱼任务开始时抛竿\"设置");
                C.AutoFisherCast = false;
                C.Save();
            }
        }
        else if (!C.AutoFisherCastSwitch)
        {
            if (IsAutohookLoaded && !IsMissfisherLoaded && !C.AutoFisherCast)
            {
                Svc.Toasts.ShowQuest($"检测到您正在使用 AutoHook 作为钓鱼插件，请在设置中勾选\"自动在钓鱼任务开始时抛竿\"选项", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
                DuoLog.Warning("检测到您正在使用 AutoHook 作为钓鱼插件，请在设置中勾选\"自动在钓鱼任务开始时抛竿\"选项");
            }
            else if (!IsAutohookLoaded && IsMissfisherLoaded && C.AutoFisherCast)
            {
                Svc.Toasts.ShowQuest($"检测到您正在使用 MissFisher 作为钓鱼插件，请在设置中取消勾选\"自动在钓鱼任务开始时抛竿\"选项", new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
                DuoLog.Warning("检测到您正在使用 MissFisher 作为钓鱼插件，请在设置中取消勾选\"自动在钓鱼任务开始时抛竿\"选项");
            }
        }
    }

    internal static void DisablePlugin()
    {
        if (SchedulerMain.State != IceState.Idle)
        {
            P.TaskManager.Abort();
            SchedulerMain.DisablePlugin();
        }
    }

    private static void UseSprint()
    {
        var am = ActionManager.Instance();
        var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;

        if (isSprintReady) am->UseAction(ActionType.GeneralAction, 4);
    }

    private static (long, long) GetEorzeaTime()
    {
        var eorzeaTime = Framework.Instance()->ClientTime.EorzeaTime;
        long hours = eorzeaTime / 3600 % 24;
        long minutes = eorzeaTime / 60 % 60;
        return (hours, minutes);
    }

    internal static (string[], KeyValuePair<(int start, int end), string[]>) GetTimedJob()
    {
        var currentTimeBonuses = new List<string>();
        KeyValuePair<(int start, int end), string[]> nextTimeBonus = default;
        Dictionary<Time, string[]> currentTimeMap = new();

        if (PlayerHelper.IsInSinusArdorum()) currentTimeMap = stage9TimeMap;
        if (PlayerHelper.IsInPhaenna()) currentTimeMap = PhaennaMap;

        (long hours, _) = GetEorzeaTime();

        // Find ALL current active bonuses and flatten them
        var currentTimes = currentTimeMap.Where(time => hours >= time.Key.start && hours <= time.Key.end);
        foreach (var timeBonus in currentTimes)
        {
            currentTimeBonuses.AddRange(timeBonus.Value);
        }

        // Remove duplicates if needed
        var uniqueCurrentBonuses = currentTimeBonuses.Distinct().ToArray();

        // Find next time bonus
        var nextTime = currentTimeMap
            .Where(time => hours < time.Key.start)
            .OrderBy(time => time.Key.start)
            .FirstOrDefault();

        if (!nextTime.Equals(default(KeyValuePair<(int, int), string[]>)))
            nextTimeBonus = nextTime;
        else
            nextTimeBonus = currentTimeMap.OrderBy(time => time.Key.start).First();

        return (uniqueCurrentBonuses, nextTimeBonus);
    }
}
