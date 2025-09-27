using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Config;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ICE.Ui
{
    internal class OverlayWindow : Window
    {
        private uint selectedJob = C.SelectedJob;
        public OverlayWindow() : base("ICE 悬浮窗", ImGuiWindowFlags.AlwaysAutoResize)
        {
            P.windowSystem.AddWindow(this);
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }



        public override void Draw()
        {
            ImGui.Text($"当前状态: " + SchedulerMain.State.ToString());
#if DEBUG
            ImGui.Text($"当前收藏品状态: {Mission_Settings.CollectableStep}");
            ImGui.Text($"当前节点计数: {Mission_Settings.nodeTotal}");
#endif

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            (string currentWeather, string nextWeather, string nextWeatherTime) = WeatherForecastHandler.GetNextWeather();

            if (currentWeather != null)
            {
                ImGui.Text($"天气: {currentWeather} -> {nextWeather} 在 [{nextWeatherTime}] 后"); // Weather: 
            }

            (var currentTimedBonus, var nextTimedBonus) = PlayerHandlers.GetTimedJob();
            if (currentTimedBonus.Length == 0)
            {
                ImGui.Text($"限时任务: 无 -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]"); // Timed Mission(s)
            }
            else
            {
                ImGui.Text($"限时任务: {string.Join(", ", currentTimedBonus)} -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }

            /* Temporarily Disabling this until I can figure out wtf is causing it to crash on non-english clients *-sighs-*
            (string type, var locations) = AnnouncementHandlers.CheckForRedAlert();
            if (type != null && locations != null)
            {
                ImGui.Text($"[紧急情况] {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(type)}"); // [Red Alert]
                ImGui.Spacing();
                for (int i = 0; i < locations.Length; i++)
                {
                    if (locations.Length > 0)
                    {
                        ImGui.Text($"组合 [{i + 1}]"); // Variant
                        ImGui.SameLine();
                    }

                    (string job, uint territoryId, float x, float y) = locations[i].first;
                    if (ImGui.Button($"{job}"))
                    {
                        Utils.SetFlagForNPC(territoryId, x, y);
                    }

                    ImGui.SameLine();

                    (job, territoryId, x, y) = locations[i].second;
                    if (ImGui.Button($"{job}"))
                    {
                        Utils.SetFlagForNPC(territoryId, x, y);
                    }
                    ImGui.Spacing();
                }
            }

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();

            */
            ImGuiHelpers.ScaledDummy(2);

            DrawScore();

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            if (ImGuiEx.IconButton(FontAwesomeIcon.Home, "打开 ICE"))
            {
                P.mainWindow.IsOpen = true;
            }
            ImGui.SameLine();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !PlayerHelper.UsingSupportedJob()))
            {
                if (ImGui.Button("开始")) // Start
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("停止")) // Stop
                {
                    SchedulerMain.DisablePlugin();
                }
            }
            ImGui.SameLine();
            ImGui.Checkbox("当前任务结束后停止", ref Mission_Settings.StopAfterCurrent);

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            if (C.ShowExpBars)
            {
                var currentJobId = Player.JobId;

                bool showExp = (CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId));

                if (CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId))
                {
                    Relic_XP.DrawRelicXP((uint)currentJobId);
                }
            }
        }

        void DrawScore()
        {
            try
            {
                var (classScore, cappedClassScore, totalScores, classId) = CosmicHelper.GetCosmicClassScores();

                ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,
                    $"{Svc.Data.GetExcelSheet<ClassJob>().GetRow(classId).Name}: {(float)cappedClassScore / 500_000:P} ({classScore:N0})")); // 原始: Abbreviation
                ImGui.SameLine();
                using (ImRaii.Disabled())
                {
                    ImGui.TextUnformatted("--");
                    ImGui.SameLine();
                    ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,
                        $"总技巧点: {(float)totalScores / 11 / 500_000:P} ({SeIconChar.CrossWorld.ToIconChar()} {11 * 500_000 - totalScores:N0})")); // All:
                }
            }
            catch
            {
                // meh
            }
        }
    }
}