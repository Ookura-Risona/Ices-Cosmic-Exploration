using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ICE.Utilities.Cosmic_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal class Task_RelicTurnin
    {
        public static Job NonRelicJob = 0;
        public static Job RelicJob = 0;
        public static void Enqueue()
        {
            RelicJob = Mission_Settings.StartJob;

            NonRelicJob = 0;

            P.TaskManager.EnqueueMulti
            (
                new(SwitchToNonRelicJob, "Switch to non-relic job"),
                new(Relic_PathTo, "Heading to relic NPC"),
                new(TalkToResearchWay, "Talk to researchway"),
                new(SelectReport, "Selecting report"),
                new(SelectRelicClass, "Selecting relic class", Utils.TaskConfig),
                new(SwitchToStartJob, "Switch back to StartJob")
            );
        }

        public static bool? Relic_PathTo()
        {
            string handle = "[Task_Relic: PathTo]";
            var zoneId = Player.Territory;
            var npcEntry = NpcData.MoonNpcs[zoneId.RowId]
                .FirstOrDefault(x => x.type == NpcData.NpcType.Relic);

            if (npcEntry != null)
            {
                Vector3 randomPos = NpcData.GetRandomPointInCircle(npcEntry.Location_Circle, 0.5f);

                if (!Task_NavmeshMove.Task_NavTo(randomPos, distance: 5, npcLoc: npcEntry.Location_Npc).Value)
                {
                    if (EzThrottler.Throttle("Relic move message", 1000))
                        IceLogging.Verbose($"Pathing to relic NPC. Distance: {Player.DistanceTo(npcEntry.Location_Npc)}", handle);
                }
                else
                {
                    IceLogging.Debug("Reached relic NPC.", handle);
                    return true;
                }
            }
            else
            {
                if (EzThrottler.Throttle("Error message: NPC", 5000))
                    IceLogging.Error($"Relic NPC not coded. Territory: {Player.Territory.RowId}", handle);
            }

            return false;
        }

        public static bool? TalkToResearchWay()
        {
            if (GenericHelpers.TryGetAddonMaster<SelectString>("SelectString", out var selectString) && selectString.IsAddonReady)
            {
                IceLogging.Info("Talk complete.");
                return true;
            }
            else if (GenericHelpers.TryGetAddonMaster<Talk>("Talk", out var talk) && talk.IsAddonReady)
            {
                if (EzThrottler.Throttle("Click talk", 100))
                    talk.Click();
            }

            var researchId = NpcData.MoonNpcs[Player.Territory.RowId]
                .FirstOrDefault(x => x.type == NpcData.NpcType.Relic).NpcId;

            Utils.TryGetObjectByDataId(researchId, out var researchNpc);

            if (EzThrottler.Throttle("Interact researchway"))
            {
                Utils.TargetgameObject(researchNpc);
                Utils.InteractWithObject(researchNpc);
            }

            return false;
        }

        public static bool? SelectReport()
        {
            if (GenericHelpers.TryGetAddonMaster<SelectIconString>("SelectIconString", out var selectIconString) && selectIconString.IsAddonReady)
            {
                IceLogging.Info("Proceed to relic class selection.");
                return true;
            }
            else if (GenericHelpers.TryGetAddonMaster<SelectString>("SelectString", out var selectString) && selectString.IsAddonReady)
            {
                if (EzThrottler.Throttle("Select report"))
                    selectString.Entries[0].Select();
            }

            return false;
        }

        public static bool? SelectRelicClass()
        {
            uint[] dohDolJobs = { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };

            uint selectedEntry = 0;

            foreach (var id in dohDolJobs)
            {
                if (Player.GetLevel((Job)id) == 0)
                    continue;

                if (id == (uint)RelicJob)
                    break;

                selectedEntry++;
            }

            if (GenericHelpers.TryGetAddonMaster<SelectIconString>("SelectIconString", out var selectIconString) && selectIconString.IsAddonReady)
            {
                if (EzThrottler.Throttle("Select relic class"))
                {
                    IceLogging.Debug($"Selecting entry {selectedEntry} for relic job {RelicJob}");
                    selectIconString.Entries[selectedEntry].Select();
                }
            }
            else if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var yesno) && yesno.IsAddonReady)
            {
                if (EzThrottler.Throttle("Select yes"))
                    yesno.Yes();
            }
            else if (GenericHelpers.TryGetAddonMaster<Talk>("Talk", out var talk) && talk.IsAddonReady)
            {
                if (EzThrottler.Throttle("Click talk", 50))
                    talk.Click();
            }
            else if (!Player.IsBusy)
            {
                IceLogging.Info("Relic turnin complete.");
                return true;
            }

            return false;
        }

        private static bool IsDoHDoL(byte jobId) => jobId >= 8 && jobId <= 18;

        internal unsafe static bool HasGearsetForJob(Job job)
        {
            var gearsets = RaptureGearsetModule.Instance();

            foreach (ref var gs in gearsets->Entries)
            {
                if (!gearsets->IsValidGearset(gs.Id))
                    continue;

                if ((Job)gs.ClassJob == job)
                    return true;
            }

            return false;
        }

        public static bool? SwitchToNonRelicJob()
        {
            if (!C.SwitchToRelicJob)
                return true;

            // 如果当前不是 relic 职业 → 不切换
            if (Player.Job != RelicJob)
                return true;

            // 当前是 relic 职业 → 必须切换到其他 DoH/DoL
            if (NonRelicJob == 0)
            {
                for (Job job = Job.CRP; job <= Job.FSH; job++)
                {
                    if (job == RelicJob)
                        continue;

                    if (Player.GetLevel(job) == 0)
                        continue;

                    if (!HasGearsetForJob(job))
                        continue;

                    NonRelicJob = job;
                    IceLogging.Debug($"Found non-relic job: {NonRelicJob}");
                    break;
                }

                if (NonRelicJob == 0)
                {
                    IceLogging.Error("[Relic Turnin] No other DoH/DoL gearset found!");
                    DuoLog.Warning($"未找到有效的生产采集职业套装! 请保证至少有 1 个可用的套装, 或者禁用\"切换其他职业套装进行提交\"选项关闭此功能。");
                    return true;
                }
            }

            if (Player.Job != NonRelicJob)
            {
                if (EzThrottler.Throttle("SwitchToNonRelicJob", 1000))
                {
                    IceLogging.Info($"Switching to non-relic job: {NonRelicJob}");
                    GearsetHandler.TaskClassChange(NonRelicJob);
                }
                return false;
            }

            return true;
        }

        public static bool? SwitchToStartJob()
        {
            if (Player.Job != RelicJob && RelicJob != 0)
            {
                if (EzThrottler.Throttle("SwitchToStartJob", 1000))
                {
                    IceLogging.Info($"Switching back to relic job: {RelicJob}");
                    GearsetHandler.TaskClassChange(RelicJob);
                }
                return false;
            }

            return true;
        }
    }
}
