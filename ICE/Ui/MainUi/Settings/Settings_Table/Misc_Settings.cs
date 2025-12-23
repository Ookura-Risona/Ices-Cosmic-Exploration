using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ICE.Config;
using Lumina.Excel.Sheets;
using Pictomancy;
using System.Collections.Generic;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class Misc_Settings
    {
        public static void Draw()
        {
            OverlaySettings();
            Separator();

            AutoUse();
            Separator();

            RepairSettings();
            Separator();

            TimeRecords();
            Separator();

            MountSelection();
            Separator();

            ShowSystemButtons();
            Separator();

            PostMissionCommands();
            Separator();

            ImGuiEx.IconWithText(FontAwesomeIcon.ExclamationTriangle, "安全设置");
            ImGui.Dummy(new Vector2(0, 5));
            SafetySettings.Draw();

#if DEBUG
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));
            DebugTab.Draw();
#endif
        }

        private static void OverlaySettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.WindowMaximize, "悬浮窗");
            ImGui.Dummy(new (0, 5));

            bool showOverlay = C.ShowOverlay;
            if (ImGui.Checkbox("显示 悬浮窗", ref showOverlay))
            {
                C.ShowOverlay = showOverlay;
                C.Save();
            }

            bool ShowSeconds = C.ShowSeconds;
            if (ImGui.Checkbox("显示 秒数", ref ShowSeconds))
            {
                C.ShowSeconds = ShowSeconds;
                C.Save();
            }

            bool showExpOverlay = C.ShowExpBars;
            if (ImGui.Checkbox("显示 宇宙工具研究数据", ref showExpOverlay))
            {
                C.ShowExpBars = showExpOverlay;
                C.Save();
            }

            bool showTotalScore = C.ShowTotalScore;
            if (ImGui.Checkbox("显示 总技巧点", ref showTotalScore))
            {
                C.ShowTotalScore = showTotalScore;
                C.Save();
            }

            bool disableHudClipping = C.DisableHudClipping;
            if (ImGui.Checkbox("禁用 HUD 裁切", ref disableHudClipping))
            {
                C.DisableHudClipping = disableHudClipping;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("启用后, 绘制叠加层将渲染在原生 UI 元素上方");
            }

        }

        private static void AutoUse()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.PersonRays, "自动使用");
            ImGui.Dummy(new Vector2(0, 5));

            bool AutoMoonSprint = C.MoonSprint;
            if (ImGui.Checkbox("自动使用宇宙冲刺", ref AutoMoonSprint))
            {
                C.MoonSprint = AutoMoonSprint;
                C.Save();
            }

            bool DisableLunarAura = C.RemoveStellarStatus;
            if (ImGui.Checkbox("自动取消贡献之星状态效果", ref DisableLunarAura))
            {
                C.RemoveStellarStatus = DisableLunarAura;
                C.Save();
            }

            bool EnableAutoAntiAFK = C.AutoAntiAFK;
            if (ImGui.Checkbox("启用 自动离开设置为不切换", ref EnableAutoAntiAFK))
            {
                C.AutoAntiAFK = EnableAutoAntiAFK;
                C.Save();
            }

            bool DisableRedAlertPathing = C.DisablePathfindingToRedAlert;
            if (ImGui.Checkbox("紧急探索任务中禁用寻路", ref DisableRedAlertPathing))
            {
                C.DisablePathfindingToRedAlert = DisableRedAlertPathing;
                C.Save();
            }
        }

        private static void RepairSettings()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Hammer, "修理设置");
            ImGui.Dummy(new Vector2(0, 5));

            bool repairAtVendor = C.RepairAtVendor;
            if (ImGui.Checkbox("NPC 修理工修理", ref repairAtVendor))
            {
                C.RepairAtVendor = repairAtVendor;
                C.Save();
            }

            using (ImRaii.Disabled(repairAtVendor))
            {
                bool selfRepairGather = C.SelfRepairGather;
                if (ImGui.Checkbox("采集时自己修理", ref selfRepairGather))
                {
                    C.SelfRepairGather = selfRepairGather;
                    C.Save();
                }

                bool selfRepairCrafter = C.SelfRepairCrafter;
                if (ImGui.Checkbox("制作时自己修理", ref selfRepairCrafter))
                {
                    C.SelfRepairCrafter= selfRepairCrafter;
                    C.Save();
                }
            }

            float repairAmount = C.RepairPercent;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("###Repair %", ref repairAmount, 0f, 99f, "%.0f%%"))
            {
                if (C.RepairPercent != repairAmount)
                {
                    C.RepairPercent = (int)repairAmount;
                    C.SaveDebounced();
                }
            }
        }

        private static void TimeRecords()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Clock, "统计设置");
            ImGui.Dummy(new Vector2(0, 5));

            int TimeHistory = C.TimeHistoryLimit;
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("平均时间历史保留数", ref TimeHistory))
            {
                C.TimeHistoryLimit = TimeHistory;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("小于 0: 保留全部日志\n" +
                                 "大于 0: 按设定数量保留");
            }
        }

        private static bool visualizeRadius = false;
        private static bool visualizeDismountRadius = false;
        private static Dictionary<uint, string> availableMounts = new();

        private static string mountSearchText = "";
        private static int mountDisplayOffset = 0;
        private static int mountItemsPerPage = 10;

        private static unsafe void MountSelection()
        {
            bool mountOutsideMission = C.UseMountOutsideMission;
            bool mountInMission = C.UseMountInMission;
            float minMountRange = C.MountRadius;
            float dismountRange = C.DismountRadius;

            ImGuiEx.IconWithText(FontAwesomeIcon.Feather, "坐骑设置");
            ImGui.Dummy(new Vector2(0, 5));

            if (ImGui.Button("选择坐骑"))
            {
                availableMounts.Clear();
                availableMounts[0] = "随机坐骑";

                var mountSheet = Svc.Data.GetExcelSheet<Mount>();

                foreach (var mountItem in mountSheet)
                {
                    //Checking to see if the current mount is unlocked
                    if (!PlayerState.Instance()->IsMountUnlocked(mountItem.RowId)) continue;

                    string mountName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(mountItem.Singular.ToString().ToLower());
                    uint id = mountItem.RowId;

                    availableMounts[id] = mountName;
                }

                mountSearchText = "";
                mountDisplayOffset = 0;

                ImGui.OpenPopup("Mount Options");
            }
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"坐骑: {C.MountName}");

            if (ImGui.BeginPopup("Mount Options"))
            {
                // Search box
                ImGui.InputText("搜索", ref mountSearchText, 100);

                // Filter mounts based on search
                var filteredMounts = availableMounts
                    .Where(kvp => string.IsNullOrEmpty(mountSearchText) ||
                                  kvp.Value.Contains(mountSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Calculate page count here, just to peeps know how many pages there are
                int totalItems = filteredMounts.Count;
                int maxOffset = Math.Max(0, totalItems - mountItemsPerPage);
                mountDisplayOffset = Math.Min(mountDisplayOffset, maxOffset);

                // Display current page of mounts
                var displayMounts = filteredMounts
                    .Skip(mountDisplayOffset)
                    .Take(mountItemsPerPage);

                foreach (var mount in displayMounts)
                {
                    if (ImGui.Selectable($"{mount.Value}##{mount.Key}"))
                    {
                        C.MountId = mount.Key;
                        C.MountName = mount.Value;
                        C.Save();
                        ImGui.CloseCurrentPopup();
                    }
                }

                // Navigation buttons
                ImGui.Separator();

                if (ImGui.Button("上页") && mountDisplayOffset > 0)
                {
                    mountDisplayOffset = Math.Max(0, mountDisplayOffset - mountItemsPerPage);
                }

                ImGui.SameLine();
                ImGui.Text($"{mountDisplayOffset + 1}-{Math.Min(mountDisplayOffset + mountItemsPerPage, totalItems)} of {totalItems}");

                ImGui.SameLine();
                if (ImGui.Button("下页") && mountDisplayOffset < maxOffset)
                {
                    mountDisplayOffset = Math.Min(maxOffset, mountDisplayOffset + mountItemsPerPage);
                }

                ImGui.EndPopup();
            }

            if (ImGui.Checkbox("任务外使用坐骑", ref mountOutsideMission))
            {
                C.UseMountOutsideMission = mountOutsideMission;
                C.Save();
            }

            if (ImGui.Checkbox("任务内使用坐骑", ref mountInMission))
            {
                C.UseMountInMission = mountInMission;
                C.Save();
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("使用坐骑的最小范围", ref minMountRange, 1))
            {
                C.MountRadius = minMountRange;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Checkbox("可视化半径范围", ref visualizeRadius);
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("下坐骑目标范围", ref dismountRange, 1))
            {
                C.DismountRadius = dismountRange;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Checkbox("可视化下坐骑半径范围", ref visualizeDismountRadius);

            using (var drawList = PictoService.Draw(hints: Utils.GetPictoHints()))
            {
                if (drawList == null)
                    return;

                var playerPos = Player.Position;

                if (visualizeRadius)
                    PictoService.VfxRenderer.AddCircle("Mount_Radius Circle", playerPos, C.MountRadius, Utils.FromUintABGR(2616716297));
                if (visualizeDismountRadius)
                    PictoService.VfxRenderer.AddCircle("Dismount_Radius Circle", playerPos, C.DismountRadius, Utils.FromUintABGR(2601121571));
            }
        }

        private static void ShowSystemButtons()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.WindowRestore, "显示/隐藏 标签栏");
            ImGui.Dummy(new(0, 5));

            bool showStopWhen = C.Show_StopWhen;
            if (ImGui.Checkbox("显示 停止条件 标签栏", ref showStopWhen))
            {
                C.Show_StopWhen = showStopWhen;
                C.Save();
            }

            bool showGProfile = C.Show_GatheringProfile;
            if (ImGui.Checkbox("显示 采集配置 标签栏", ref showGProfile))
            {
                C.Show_GatheringProfile = showGProfile;
                C.Save();
            }

            bool showMissionPrio = C.Show_MissionPriority;
            if (ImGui.Checkbox("显示 任务优先级 标签栏", ref showMissionPrio))
            {
                C.Show_MissionPriority = showMissionPrio;
                C.Save();
            }

            bool showMisc = C.Show_MiscSettings;
            if (ImGui.Checkbox("显示 杂项设置 标签栏", ref showMisc))
            {
                C.Show_MiscSettings = showMisc;
                C.Save();
            }

            bool showHubActivities = C.Show_HubActivities;
            if (ImGui.Checkbox("显示 基地活动 选项", ref showHubActivities))
            {
                C.Show_HubActivities = showHubActivities;
                C.Save();
            }
        }
        private static void PostMissionCommands()
        {
            ImGuiEx.IconWithText(FontAwesomeIcon.Play, "任务后命令");
            ImGui.Dummy(new Vector2(0, 5));

            ImGui.TextWrapped("请在下方输入希望在运行完成后自动执行的一系列命令。\n" +
                              "这算是我提供的一种方式, 让您能够在一定程度上编写脚本/设置一系列您想做但插件本身可能未包含的其他操作。\n" +
                              "如果您需要更复杂的功能, 直接编写一个 SND 脚本即可, 然后在任务结束后自动执行脚本。");

            if (ImGui.Button("添加新命令"))
            {
                C.PostMissionCommands.Add(new Config.MissionCommand 
                { 
                    command = "", 
                    Delay = 0,
                });
                C.Save();
            }

            MissionCommand? toRemove = null;
            int entryCounter = 0;

            if (ImGui.BeginTable("Mission Commands", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("命令");
                ImGui.TableSetupColumn("延迟");
                ImGui.TableSetupColumn("移除");

                ImGui.TableHeadersRow();

                foreach (var entry in C.PostMissionCommands)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.SetNextItemWidth(200);

                    ImGui.PushID($"{entryCounter}_MissionCommand");
                    string command = entry.command;
                    if (ImGui.InputText("##Command", ref command))
                    {
                        entry.command = command;
                        C.SaveDebounced();
                    }

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(100);
                    int delay = entry.Delay;
                    if (ImGui.InputInt("###Delay", ref delay))
                    {
                        entry.Delay = delay;
                        C.SaveDebounced();
                    }

                    ImGui.TableNextColumn();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, $"remove{C.PostMissionCommands.IndexOf(entry)}"))
                    {
                        toRemove = entry;
                    }
                    ImGui.PopID();
                    entryCounter += 1;
                }

                if (toRemove != null)
                {
                    C.PostMissionCommands.Remove(toRemove);
                    C.Save();
                }

                ImGui.EndTable();
            }
        }

        private static void Separator()
        {
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));
        }
    }
}
