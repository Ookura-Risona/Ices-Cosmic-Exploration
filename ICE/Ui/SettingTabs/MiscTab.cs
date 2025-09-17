using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Pictomancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.SettingTabs
{
    internal class MiscTab
    {
        // Overlay Settings
        private static bool showOverlay = C.ShowOverlay;
        private static bool ShowSeconds = C.ShowSeconds;
        private static Dictionary<uint, string> availableMounts = new();

        private static string mountSearchText = "";
        private static int mountDisplayOffset = 0;
        private static int mountItemsPerPage = 10;

        private static bool AutoMoonSprint = C.MoonSprint;
        private static bool EnableAutoAntiAFK = C.AutoAntiAFK;

        // Mission Priority Settings

        public static void Draw()
        {
            ImGui.Text("悬浮窗设置");

            if (ImGui.Checkbox("显示 悬浮窗", ref showOverlay))
            {
                C.ShowOverlay = showOverlay;
                C.Save();
            }

            if (ImGui.Checkbox("显示 秒数", ref ShowSeconds))
            {
                C.ShowSeconds = ShowSeconds;
                C.Save();
            }

            bool showExpOverlay = C.ShowExpBars;
            if (ImGui.Checkbox("显示 宇宙工具研究经验", ref showExpOverlay))
            {
                C.ShowExpBars = showExpOverlay;
                C.Save();
            }

            ImGui.Dummy(new(0, 2));

            ImGui.Separator();

            ImGui.Dummy(new (0, 2));

            if (ImGui.Checkbox("自动使用宇宙冲刺", ref AutoMoonSprint))
            {
                C.MoonSprint = AutoMoonSprint;
                C.Save();
            }

            if (ImGui.Checkbox("启用 自动离开设置为不切换", ref EnableAutoAntiAFK))
            {
                C.AutoAntiAFK = EnableAutoAntiAFK;
                C.Save();
            }

            ImGui.Dummy(new(0, 2));

            ImGui.Separator();

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
                    C.Save();
                }
            }

            ImGui.Separator();

            MountSelection();

            ImGui.Separator();

            ImGui.Text("任务优先级管理");

            ImGui.Text("拖动项目以重新排序任务优先级(优先级越高 = 越优先处理)");
            ImGui.Separator();

            // Create a copy for manipulation
            var currentOrder = C.MissionPrio.ToList();
            bool orderChanged = false;

            for (int i = 0; i < currentOrder.Count; i++)
            {
                ImGui.PushID(i);

                var missionType = currentOrder[i];

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text(GetMissionTypeIcon(missionType));
                ImGui.PopFont();

                ImGui.SameLine();

                // Making the text the only selectable thing... trying to make it WITH the icon is messy
                string displayText = GetMissionTypeName(missionType);
                bool isSelected = false;
                ImGui.Selectable(displayText, isSelected, ImGuiSelectableFlags.None);

                // Handle drag and drop
                if (ImGui.BeginDragDropSource())
                {
                    // Store the index being dragged
                    unsafe
                    {
                        int draggedIndex = i;
                        byte* data = (byte*)&draggedIndex;
                        ImGui.SetDragDropPayload("MISSION_TYPE", new ReadOnlySpan<byte>(data, sizeof(int)));
                    }
                    ImGui.Text($"正在移动: {GetMissionTypeName(missionType)}");
                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget())
                {
                    unsafe
                    {
                        var payload = ImGui.AcceptDragDropPayload("MISSION_TYPE");
                        if (!payload.IsNull)
                        {
                            int draggedIndex = *(int*)payload.Data;

                            // Perform the reorder
                            if (draggedIndex != i && draggedIndex >= 0 && draggedIndex < currentOrder.Count)
                            {
                                var draggedItem = currentOrder[draggedIndex];
                                currentOrder.RemoveAt(draggedIndex);
                                currentOrder.Insert(i, draggedItem);
                                orderChanged = true;
                            }
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                // Show priority number
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"(优先级: {i + 1})");

                ImGui.PopID();
            }

            if (orderChanged)
            {
                // Priority has been changed. Updating the config now.
                C.MissionPrio = currentOrder;
                C.Save();
            }

            ImGui.Separator();

            // Reset to default button
            if (ImGui.Button("重置为默认"))
            {
                C.MissionPrio = new List<ProvisionalTypes>
                {
                    ProvisionalTypes.ProvisionalWeather,
                    ProvisionalTypes.ProvisionalSequential,
                    ProvisionalTypes.ProvisionalTimed
                };
            }

            ImGui.SameLine();

            // Show current order as text (for debugging/confirmation)
            if (ImGui.Button("显示当前顺序"))
            {
                string orderText = string.Join(" → ", C.MissionPrio.Select(GetMissionTypeName));
                ImGui.SetClipboardText(orderText);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("复制当前优先级顺序到剪贴板");
            }
        }

        // Quick way of assigning a name to the missions (useful for enums, saves me a lot of typing
        private static string GetMissionTypeName(ProvisionalTypes type)
        {
            return type switch
            {
                ProvisionalTypes.ProvisionalWeather => "天气限定任务",
                ProvisionalTypes.ProvisionalSequential => "连续任务",
                ProvisionalTypes.ProvisionalTimed => "时间限定任务",
                _ => type.ToString()
            };
        }

        // Quick way of assigning Icons to the mission types
        private static string GetMissionTypeIcon(ProvisionalTypes type)
        {
            return type switch
            {
                ProvisionalTypes.ProvisionalWeather => FontAwesomeIcon.Cloud.ToIconString(),
                ProvisionalTypes.ProvisionalSequential => FontAwesomeIcon.ListOl.ToIconString(),
                ProvisionalTypes.ProvisionalTimed => FontAwesomeIcon.Clock.ToIconString(),
                _ => FontAwesomeIcon.Question.ToIconString()
            };
        }

        private static bool visualizeRadius = false;

        private static unsafe void MountSelection()
        {
            bool mountOutsideMission = C.UseMountOutsideMission;
            bool mountInMission = C.UseMountInMission;
            float minMountRange = C.MountRadius;

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
            ImGui.Checkbox("半径可视化", ref visualizeRadius);
            if (visualizeRadius)
            {
                using (var drawList = PictoService.Draw())
                {
                    if (drawList == null)
                        return;

                    var playerPos = Player.Position;

                    // drawList.AddCircleFilled(playerPos, C.MountRadius, 2616716297, 2616716297);
                    PictoService.VfxRenderer.AddCircle("Mount_Radius Circle", playerPos, C.MountRadius, Utils.FromUintABGR(2616716297));
                }
            }
        }
    }
}
