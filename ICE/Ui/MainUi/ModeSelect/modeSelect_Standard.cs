using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ICE.Ui.MainUi.Settings.Settings_Table;
using ICE.Utilities.ImGuiTools;
using System.Collections.Generic;
using System.Reflection;

namespace ICE.Ui.MainUi.ModeSelect
{
    internal class modeSelect_Standard
    {
        public static void Draw()
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 10).Push(ImGuiStyleVar.ChildBorderSize, 1);

            // Header at the top
            float scale = ImGuiHelpers.GlobalScale;

            using (var headerChild = ImRaii.Child("##modeSelect_StandardHeader", new Vector2(0, 45 * scale), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!headerChild.Success) return;

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10 * scale);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5 * scale);

                string modeType = string.Empty;
                FontAwesomeIcon modeIcon = FontAwesomeIcon.List;

                bool relicMode = C.XPRelicGrind;
                bool provisionalMode = C.GrindProvisionals;
                bool standard = (!relicMode && !provisionalMode);

                if (standard)
                    modeType = "标准模式"; // Standard
                else if (relicMode)
                {
                    modeType = "宇宙工具研究数据刷取模式"; // Relic Grind
                    modeIcon = FontAwesomeIcon.ArrowUpRightDots;
                }
                else if (provisionalMode)
                {
                    modeType = "临时性任务模式"; // Provisional
                    modeIcon = FontAwesomeIcon.Cloud;
                }

                ImGuiEx.IconWithText(modeIcon, $"{modeType}"); // Mode | 这里去掉直接使用原始文本

                ImGui.SameLine(0, 10 * scale);

                // Adjust the Y position to center the button vertically with the text
                float textHeight = ImGui.GetTextLineHeight();
                float buttonHeight = ImGui.GetFrameHeight();
                float yOffset = (textHeight - buttonHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "模式选择")) // Mode Selection
                {
                    ImGui.OpenPopup("Mode Select | Select Mode Window");
                }
                if (ImGui.BeginPopup("Mode Select | Select Mode Window"))
                {
                    ImGui.Text("选择模式"); // Select Mode
                    ImGui.Separator();

                    if (ImGui.RadioButton("标准模式", standard)) // Standard
                    {
                        C.XPRelicGrind = false;
                        C.GrindProvisionals = false;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("标准模式 \n" + // Stand Mode (typo!)
                                       "-> 用于选择您想要刷取的任务。此模式按以下优先级顺序执行:\n" + // Used to select which missions you want to grind. It'll priortize in the following order:\n
                                       "-> 紧急探索任务 -> 临时性任务 [连续/时间限定/天气限定任务] -> 标准任务 [A->D]\n" + // Critical -> Provisional [Sequence/Timed/Weather] -> Standard [A->D]\n
                                       "-> 选择你想要做的任务, 然后开始吧。"); // Select which missions you want to do, and go at it.
                    if (ImGui.RadioButton("宇宙工具研究数据刷取模式", relicMode)) // Relic Grind
                    {
                        C.XPRelicGrind = true;
                        C.GrindProvisionals = false;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("宇宙工具研究数据刷取模式\n" + // Relic Grind\n
                                       "-> 自动选择最适合完成宇宙工具的任务\n" + // Automatically select which missions that are best to finish up your relic\n
                                       "-> 任务选择的权重依据为完成宇宙工具到下一阶段所需的研究数据\n" + // These are weighed based on what is needed to complete the tool to the next step\n
                                       "-> 如果您只想做特定任务, 可以启用此选项并选择您要做的任务"); // If you want to only do certain missions, enable the option and select which ones you want to do
                    if (ImGui.RadioButton("临时性任务模式", provisionalMode)) // Provisional Grind
                    {
                        C.XPRelicGrind = false;
                        C.GrindProvisionals = true;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("临时性任务模式\n" + // Provisional Grind\n
                                       "-> 刷取您启用的临时性任务 [天气限定任务 | 时间限定任务 | 连续任务]\n" + // Grind provisional missions [Weather | Timed | Sequence] that you have enabled\n
                                       "-> 可用于所有职业, 您可以设置职业与任务类型的优先级\n" + // Use this to grind all classes. You can set the priority for which classes and types of missions that you want to do\n
                                       "-> 适用于在所有职业中刷取技巧点/票据, 或在特定时间完成指定任务"); // Useful if you're aiming to grind out score/tokens across all classes, or want to do specific missions at certain times

                    ImGui.EndPopup();
                }

                uint currentJobId = Player.JobId;
                bool usingSupportedJob = CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId);

                ImGui.SameLine(0, 10 * scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
                {
                    if (ImGui.Button("开始", new Vector2(150 * scale, 0)))
                    {
                        SchedulerMain.EnablePlugin();
                    }
                }

                ImGui.SameLine(0, 10 * scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
                using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f)))
                    {
                        if (ImGui.Button("停止", new Vector2(150 * scale, 0)))
                        {
                            SchedulerMain.DisablePlugin();
                        }
                    }
                }
            }

            if (ImGui.BeginTable("modeSelect_TableHeader", 4, ImGuiTableFlags.SizingFixedFit, Vector2.Zero))
            {
                ImGui.TableSetupColumn("Class Selector");
                ImGui.TableSetupColumn("Other Settings");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                bool tableSettingExpanded = modeSelect_Tools.DrawCompactCategoryHeader("表格设置", FontAwesomeIcon.Table); // Table Settings

                ImGui.TableNextColumn();
                bool missionSettingExpanded = modeSelect_Tools.DrawCompactCategoryHeader("任务设置", FontAwesomeIcon.UserCog); // Mission Settings

                bool relicGrindExpanded = false;
                if (C.XPRelicGrind)
                {
                    ImGui.TableNextColumn();
                    relicGrindExpanded = modeSelect_Tools.DrawCompactCategoryHeader("宇宙工具研究数据刷取设置", FontAwesomeIcon.ArrowUpRightDots); // Relic Grind Settings
                }

                bool completionExpanded = false;
                if (C.ShowCompletionWindow)
                {
                    ImGui.TableNextColumn();
                    completionExpanded = modeSelect_Tools.DrawCompactCategoryHeader("完成情况表格设置", FontAwesomeIcon.Trophy); // Completion Table Settings
                }

                bool showNextColumn = tableSettingExpanded || missionSettingExpanded || (relicGrindExpanded && C.XPRelicGrind) || (completionExpanded && C.ShowCompletionWindow);

                if (showNextColumn)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (tableSettingExpanded)
                    {
                        Settings_TableColumns.ColumnSettings();
                    }

                    ImGui.TableNextColumn();
                    if (missionSettingExpanded)
                    {
                        Settings_TableColumns.GeneralMissionSettings();
                    }

                    if (C.XPRelicGrind && relicGrindExpanded)
                    {
                        ImGui.TableNextColumn();

                        bool relicTurnin = C.TurninRelic;
                        if (ImGui.Checkbox($"宇宙工具可报告时提交##RelicTurnin_RelicGrind", ref relicTurnin))
                        {
                            if (relicTurnin)
                                C.GrindProvisionals = false;

                            C.TurninRelic = relicTurnin;
                            C.Save();
                        }
                        ImGui.SameLine();
                        ImGui.TextDisabled("?");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("这是此功能的工作方式说明。如果我未来修改了这个功能，这个提示也会随之改变。\n" +
                                             "1: 此功能将检查你的当前职业 [不是菜单中选择的职业, 是实际当前职业] 提交宇宙工具。\n" +
                                             "2: 你必须不装备宇宙工具，才能让此功能完全地自动运行。\n" +
                                             "\t- 原因是我现在懒得写这部分逻辑。（将来可能会改主意 *耸肩*）\n" +
                                             "3: 此功能的优先级高于 \"宇宙工具可报告时停止\" 选项，如果两个都启用, 它会选择提交宇宙工具而不是停止, 并继续执行任务。\n" +
                                             "4: 如果你当前是能工巧匠职业，报告后会自动返回你之前正在制作的位置。\n" +
                                             "\t- 这是可选的，你可以自由关闭。我个人喜欢这样设置，方便我回到自己选定的安静区域。");
                        }

                        ImGui.Separator();

                        bool EnableRelicXp = C.XPRelicGrind;
                        if (ImGui.Checkbox("自动根据研究数据挑选任务", ref EnableRelicXp)) // Auto-Pick For Relic XP
                        {
                            if (EnableRelicXp)
                            {
                                C.GrindProvisionals = false;
                            }
                            C.XPRelicGrind = EnableRelicXp;
                            C.Save();
                        }
                        ImGui.SameLine();
                        ImGui.TextDisabled("?");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("请注意: 此功能仅会执行基础任务标签下的任务刷取研究数据。\n" + // Please note. This will ONLY grind for relic Exp under the basic mission tab. \n
                                               "即使您选择启用了连续/时间限定/天气限定/紧急探索任务, 也不会执行这些任务。"); // This will NOT work (even with missions selected) on the Sequence/Timed/Weather/Critical Missions
                        }
                        if (EnableRelicXp)
                        {
                            bool OnlySelected = C.XPRelicOnlyEnabled;
                            if (ImGui.Checkbox("仅限启用的任务", ref OnlySelected))
                            {
                                C.XPRelicOnlyEnabled = OnlySelected;
                                C.Save();
                            }
                            if (C.ShowManualMode)
                            {
                                bool IgnoreManual = C.XPRelicIgnoreManual;
                                if (ImGui.Checkbox("忽略手动模式任务", ref IgnoreManual))
                                {
                                    C.XPRelicIgnoreManual = IgnoreManual;
                                    C.Save();
                                }
                            }
                        }
                    }

                    if (C.ShowCompletionWindow && completionExpanded)
                    {
                        // 根据是否选择宇宙工具研究数据刷取模式决定完成情况表格设置的列索引，防止展开时错位
                        int completionColumnIndex = C.XPRelicGrind ? 3 : 2;
                        ImGui.TableSetColumnIndex(completionColumnIndex);

                        bool showSelectedJobOnly = C.ShowSelectedJobOnly;
                        if (ImGui.Checkbox("只显示选择的职业", ref showSelectedJobOnly)) // Show only selected job
                        {
                            C.ShowSelectedJobOnly = showSelectedJobOnly;
                            if (showSelectedJobOnly)
                                C.ShowCompletionOnlyJob = false;
                            C.Save();
                        }

                        bool nonGold = C.ShowCompletion_MissingGold;
                        if (ImGui.Checkbox("只显示非金星评价任务", ref nonGold)) // Show Only Non-Gold Missions
                        {
                            C.ShowCompletion_MissingGold = nonGold;
                            C.Save();
                        }
                    }
                }

                ImGui.EndTable();
            }

            using (var bodyChild = ImRaii.Child("##modeSelect_Body", new Vector2(0, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (!bodyChild.Success) return;

                foreach (var missionType in modeSelect_TableInfo.missionList)
                {
                    missionType.Value.Clear();
                }

                foreach (var mission in CosmicHelper.SheetMissionDict)
                {
                    var Jobs = mission.Value.Jobs;
                    var territoryId = mission.Value.TerritoryId;
                    uint selectedJob = C.SelectedJob;
                    bool sinusEnabled = C.ShowSinusMissions;
                    bool phaennaEnabled = C.ShowPhaennaMissions;

                    if (C.ShowCompletionWindow)
                    {
                        if (C.ShowCompletionOnlyJob)
                        {
                            if (!Jobs.Contains(selectedJob))
                                continue;
                        }
                    }
                    else if (C.GrindProvisionals)
                    {
                        // honestly do nothing here, this is just to catch and show all the jobs for this mode here. Kinda lazy I realize but *-shrugs-*
                    }
                    else if (!Jobs.Contains(selectedJob))
                        continue;


                    if (!sinusEnabled && territoryId == 1237)
                        continue;

                    if (!phaennaEnabled && territoryId == 1291)
                        continue;

                    if (C.GrindProvisionals)
                    {
                        bool provisional = mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather)
                                        || mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed)
                                        || mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential);

                        if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            modeSelect_TableInfo.missionList["Weather"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            modeSelect_TableInfo.missionList["Timed"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            modeSelect_TableInfo.missionList["Sequence"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });

                        if (C.MissionConfig.ContainsKey(mission.Key) && C.MissionConfig[mission.Key].Enabled && provisional)
                        {
                            modeSelect_TableInfo.missionList["All Enabled"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        }
                    }
                    else
                    {
                        if (mission.Value.Attributes.HasFlag(MissionAttributes.Critical))
                            modeSelect_TableInfo.missionList["Critical"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            modeSelect_TableInfo.missionList["Weather"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            modeSelect_TableInfo.missionList["Timed"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            modeSelect_TableInfo.missionList["Sequence"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank > 3)
                            modeSelect_TableInfo.missionList["ARank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 3)
                            modeSelect_TableInfo.missionList["BRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 2)
                            modeSelect_TableInfo.missionList["CRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 1)
                            modeSelect_TableInfo.missionList["DRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });

                        if (C.MissionConfig.ContainsKey(mission.Key) && C.MissionConfig[mission.Key].Enabled)
                        {
                            modeSelect_TableInfo.missionList["All Enabled"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        }
                    }
                }

                int criticalEnabled = modeSelect_TableInfo.missionList.ContainsKey("Critical") ? modeSelect_TableInfo.missionList["Critical"].Count(mission => mission.enabled) : 0;
                int sequenceEnabled = modeSelect_TableInfo.missionList.ContainsKey("Sequence") ? modeSelect_TableInfo.missionList["Sequence"].Count(mission => mission.enabled) : 0;
                int weatherEnabled = modeSelect_TableInfo.missionList.ContainsKey("Weather") ? modeSelect_TableInfo.missionList["Weather"].Count(mission => mission.enabled) : 0;
                int timedEnabled = modeSelect_TableInfo.missionList.ContainsKey("Timed") ? modeSelect_TableInfo.missionList["Timed"].Count(mission => mission.enabled) : 0;
                int aRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("ARank") ? modeSelect_TableInfo.missionList["ARank"].Count(mission => mission.enabled) : 0;
                int bRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("BRank") ? modeSelect_TableInfo.missionList["BRank"].Count(mission => mission.enabled) : 0;
                int cRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("CRank") ? modeSelect_TableInfo.missionList["CRank"].Count(mission => mission.enabled) : 0;
                int dRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("DRank") ? modeSelect_TableInfo.missionList["DRank"].Count(mission => mission.enabled) : 0;
                int allEnabled = modeSelect_TableInfo.missionList.ContainsKey("All Enabled") ? modeSelect_TableInfo.missionList["All Enabled"].Count(mission => mission.enabled) : 0;

                float scrollbarSize = ImGui.GetStyle().ScrollbarSize;
                float buttonRowHeight = (ImGui.GetTextLineHeight() + 8 * scale + 4 * scale) + scrollbarSize;

                using (var missionButtons = ImRaii.Child("##tab_scroll", new Vector2(0, buttonRowHeight), false, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    if (!missionButtons.Success)
                        return;

                    if (C.GrindProvisionals)
                    {
                        ImGui_Tools.DrawCategoryButton($"全部启用 [{allEnabled}]", "main_AllEnabled");
                        ImGui_Tools.DrawCategoryButton($"连续任务 [{sequenceEnabled}]", "main_Sequence");
                        ImGui_Tools.DrawCategoryButton($"天气限定任务 [{weatherEnabled}]", "main_Weather");
                        ImGui_Tools.DrawCategoryButton($"时间限定任务 [{timedEnabled}]", "main_Timed");
                        ImGui_Tools.EndCategoryButtonRow();
                    }
                    else
                    {
                        ImGui_Tools.DrawCategoryButton($"全部启用 [{allEnabled}]", "main_AllEnabled");
                        ImGui_Tools.DrawCategoryButton($"紧急探索任务 [{criticalEnabled}]", "main_Critical");
                        ImGui_Tools.DrawCategoryButton($"连续任务 [{sequenceEnabled}]", "main_Sequence");
                        ImGui_Tools.DrawCategoryButton($"天气限定任务 [{weatherEnabled}]", "main_Weather");
                        ImGui_Tools.DrawCategoryButton($"时间限定任务 [{timedEnabled}]", "main_Timed");
                        ImGui_Tools.DrawCategoryButton($"A 类任务 [{aRankEnabled}]", "main_ARank");
                        ImGui_Tools.DrawCategoryButton($"B 类任务 [{bRankEnabled}]", "main_BRank");
                        ImGui_Tools.DrawCategoryButton($"C 类任务 [{cRankEnabled}]", "main_CRank");
                        ImGui_Tools.DrawCategoryButton($"D 类任务 [{dRankEnabled}]", "main_DRank", spacingAfter: 0);
                        ImGui_Tools.EndCategoryButtonRow();
                    }
                }

                if (C.ShowExtraMissionInfo)
                {
                    if (ImGui.BeginTable("Mission Info | Extra Details", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable, Vector2.Zero))
                    {
                        ImGui.TableSetupColumn("Mission Selection Viewer", ImGuiTableColumnFlags.WidthFixed, 200f);
                        ImGui.TableSetupColumn("Specific Mission Info", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        MissionTableInfo();

                        ImGui.TableNextColumn();
                        using (var missionInfoChild = ImRaii.Child("##modeSelect_MissionInfo", new Vector2(0, 0), false))
                        {
                            modeSelect_TableInfo.DrawMissionDetails();
                        }

                        ImGui.EndTable();
                    }
                }
                else
                {
                    MissionTableInfo();
                }
            }
        }

        private static void MissionTableInfo()
        {
            using (var missionTableChild = ImRaii.Child("##modeSelect_MissionTables", new Vector2(0, 0), false))
            {
                var enabledTabs = ImGui_Tools.CategoryStates;
                if (C.GrindProvisionals)
                {
                    modeSelect_TableInfo.missionList["All Enabled"] = modeSelect_TableInfo.missionList["All Enabled"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList(); // ToList() if you need a List<T>, otherwise the IOrderedEnumerable is fine

                    modeSelect_TableInfo.missionList["Sequence"] = modeSelect_TableInfo.missionList["Sequence"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList();

                    modeSelect_TableInfo.missionList["Weather"] = modeSelect_TableInfo.missionList["Weather"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList();

                    modeSelect_TableInfo.missionList["Timed"] = modeSelect_TableInfo.missionList["Timed"]
                        .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                        .ToList();

                    if (enabledTabs["main_AllEnabled"])
                        modeSelect_TableInfo.DrawMissionTablev2("全部启用", "All_Enabled", modeSelect_TableInfo.missionList["All Enabled"]);
                    if (enabledTabs["main_Sequence"])
                        modeSelect_TableInfo.DrawMissionTablev2("连续", "Sequence_Missions", modeSelect_TableInfo.missionList["Sequence"]);
                    if (enabledTabs["main_Weather"])
                        modeSelect_TableInfo.DrawMissionTablev2("天气限定", "Weather_Missions", modeSelect_TableInfo.missionList["Weather"]);
                    if (enabledTabs["main_Timed"])
                        modeSelect_TableInfo.DrawMissionTablev2("时间限定", "Timed_Missions", modeSelect_TableInfo.missionList["Timed"]);
                }
                else
                {
                    if (enabledTabs["main_AllEnabled"])
                    {
                        if (modeSelect_TableInfo.missionList["All Enabled"].Count > 0)
                        {
                            modeSelect_TableInfo.DrawMissionTablev2("全部启用", "All_Enabled", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["All Enabled"]));
                        }
                        else
                        {
                            ImGui.Text("嘿！启用一些任务, 我们才能在这里显示内容。");
                        }
                    }
                    if (enabledTabs["main_Critical"])
                        modeSelect_TableInfo.DrawMissionTablev2("紧急探索", "Critical_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Critical"]));
                    if (enabledTabs["main_Sequence"])
                        modeSelect_TableInfo.DrawMissionTablev2("连续", "Sequence_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Sequence"]));
                    if (enabledTabs["main_Weather"])
                        modeSelect_TableInfo.DrawMissionTablev2("天气限定", "Weather_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Weather"]));
                    if (enabledTabs["main_Timed"])
                        modeSelect_TableInfo.DrawMissionTablev2("时间限定", "Timed_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Timed"]));
                    if (enabledTabs["main_ARank"])
                        modeSelect_TableInfo.DrawMissionTablev2("A 类", "A_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["ARank"]));
                    if (enabledTabs["main_BRank"])
                        modeSelect_TableInfo.DrawMissionTablev2("B 类", "B_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["BRank"]));
                    if (enabledTabs["main_CRank"])
                        modeSelect_TableInfo.DrawMissionTablev2("C 类", "C_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["CRank"]));
                    if (enabledTabs["main_DRank"])
                        modeSelect_TableInfo.DrawMissionTablev2("D 类", "D_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["DRank"]));
                }
            }
        }
    }
}
