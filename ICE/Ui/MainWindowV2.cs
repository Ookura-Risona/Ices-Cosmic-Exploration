using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility.Table;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Enums;
using ICE.Utilities.Cosmic;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Xml.Schema;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Ui
{
    internal class MainWindowV2 : Window
    {
        public MainWindowV2() :
#if DEBUG
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} Debug build ###ICEMainWindowV2")
#else
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow")
#endif
        {
            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes.
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(100, 100),
                MaximumSize = new Vector2(2000, 3000)
            };

            TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("♥ Ko-fi (请我喝杯冰咖啡)"), Icon = FontAwesomeIcon.Heart, IconOffset = new(1, 1), Click = _ => GenericHelpers.ShellStart("https://ko-fi.com/ice643269") });

            P.windowSystem.AddWindow(this);

            AllowPinning = true;
            AllowClickthrough = true;
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        // Available jobs and their IDs.
        // Matching up to the sheet `ClassJob` vs `ClassJobCategory` for future use (idk why that sheet even exist...)
        public static List<(string Name, uint Id)> jobOptions = new()
        {
            ("刻木匠", 8), // CRP
            ("锻铁匠", 9), // BSM
            ("铸甲匠", 10), // ARM
            ("雕金匠", 11), // GSM
            ("制革匠", 12), // LTW
            ("裁衣匠", 13), // WVR
            ("炼金术士", 14), // ALC
            ("烹调师", 15), // CUL
            ("采矿工", 16), // MIN
            ("园艺工", 17), // BTN
            ("捕鱼人", 18), // FSH
        };

        private uint currentJobId => Player.JobId;
        private bool usingSupportedJob => jobOptions.Any(job => job.Id == currentJobId);

        private bool showCritical = C.ShowCritical;
        private bool showSequential = C.ShowSequential;
        private bool showWeather = C.ShowWeather;
        private bool showTimeRestricted = C.ShowTimeRestricted;
        private bool showClassA = C.ShowClassA;
        private bool showClassB = C.ShowClassB;
        private bool showClassC = C.ShowClassC;
        private bool showClassD = C.ShowClassD;

        private string SinusAsset = "ICE.Moons.Sinus_Ardorum.png";
        private string PhaennaAsset = "ICE.Moons.Phaenna.png";

        // Middle Column stuff
        private Dictionary<string, bool> headerStates = new();

        private bool showTableSetting = false;

        private static Dictionary<string, List<(uint id, bool gather, bool enabled)>> missionList = new()
        {
            ["Critical"] = new List<(uint id, bool gather, bool enabled)>(),
            ["Weather"] = new List<(uint id, bool gather, bool enabled)>(),
            ["Timed"] = new List<(uint id, bool gather, bool enabled)> (),
            ["Sequence"] = new List<(uint id, bool gather, bool enabled)> (),
            ["ARank"] = new List<(uint id, bool gather, bool enabled)> (),
            ["BRank"] = new List<(uint id, bool gather, bool enabled)> (),
            ["CRank"] = new List<(uint id, bool gather, bool enabled)> (),
            ["DRank"] = new List<(uint id, bool gather, bool enabled)> ()
        };

        private string[] missionSortOptions = ["ID", "名称", "宇宙信用点", "月球信用点", "研究数据 I", "研究数据 II", "研究数据 III", "研究数据 IV", "研究数据 V", "地图位置"];
        private int missionSelectedOption = C.TableSortOption;
        private List<(uint id, bool gather, bool enabled)> SortMissionList(List<(uint id, bool gather, bool enabled)> missions)
        {
            int sortOption = missionSelectedOption;
            var missionInfo = CosmicHelper.SheetMissionDict;

            switch (sortOption)
            {
                case 0: // Sorting by Id
                    return missions.ToList();
                case 1: // Name 
                    return missions.OrderBy(m => missionInfo[m.id].Name).ToList();
                case 2: // Cosmo Credits
                    return missions.OrderByDescending(m => missionInfo[m.id].CosmoCredit).ToList();
                case 3: // Lunar Credits
                    return missions.OrderByDescending(m => missionInfo[m.id].LunarCredit).ToList();
                case 4: // Exp Type 1:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 1)
                                                     .Sum(exp => exp.Value)).ToList();
                case 5: // Exp Type 2:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 2)
                                                     .Sum(exp => exp.Value)).ToList();
                case 6: // Exp Type 3:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 3)
                                                     .Sum(exp => exp.Value)).ToList();
                case 7: // Exp Type 4:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 4)
                                                     .Sum(exp => exp.Value)).ToList();
                case 8: // Exp Type 5:
                    return missions.OrderByDescending(m => missionInfo[m.id].RelicXpInfo
                                                     .Where(exp => exp.Key == 5)
                                                     .Sum(exp => exp.Value)).ToList();
                case 9: // Map Location
                    return missions.OrderBy(m => missionInfo[m.id].MarkerId).ToList();
                default:
                    return missions.ToList();
            }
        }

        private string[] missionOptions = ["Current Class", "All Missions", "Currently Enabled"];
        private string selectedOption = "Current Class";

        // Right Column stuff
        private uint selectedMission = 0;

        private static HashSet<uint> VisibleZones = new HashSet<uint>() { 1237, 1291 };

        public override void Draw()
        {
            // Calculate scaling factors based on current font size
            float fontScale = ImGui.GetIO().FontGlobalScale;
            float textLineHeight = ImGui.GetTextLineHeight();
            float scaledSpacing = ImGui.GetStyle().ItemSpacing.Y * fontScale;
            float headerPadding = textLineHeight * 1.2f;

            float headerHeight = textLineHeight + headerPadding * 2;
            float contentAreaHeight = ImGui.GetWindowHeight() - headerHeight - 4;
            float labelHeight = ImGui.GetTextLineHeightWithSpacing();
            float childHeight = ImGui.GetContentRegionAvail().Y;

            // Get total available width
            float totalWidth = ImGui.GetContentRegionAvail().X;

            // Ensure minimum widths and validate stored widths
            float minLeftWidth = Math.Max(220, textLineHeight * 14);
            float minMiddleWidth = Math.Max(200, textLineHeight * 12);
            float minRightWidth = 150;

            // Initialize column widths if not set
            if (C.LeftColumnWidth < minLeftWidth)
                C.LeftColumnWidth = minLeftWidth;
            if (C.MiddleColumnWidth < minMiddleWidth)
                C.MiddleColumnWidth = minMiddleWidth;

            // Calculate actual widths (use the config values directly)
            float leftWidth = C.LeftColumnWidth;
            float middleWidth = C.MiddleColumnWidth;
            float splitterWidth = 4.0f;
            float rightWidth = Math.Max(minRightWidth, totalWidth - leftWidth - middleWidth - (splitterWidth * 2));

            // ----------------------------
            // LEFT PANEL
            // ----------------------------
            if (ImGui.BeginChild("Filter Panel##Filter Panel", new Vector2(leftWidth, childHeight), true))
            {
                // ... your existing left panel content ...
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

                using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
                {
                    if (ImGui.Button("开始", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f))) // Start
                    {
                        SchedulerMain.EnablePlugin();
                    }
                }

                using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
                {
                    if (ImGui.Button("停止", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f))) // Stop
                    {
                        SchedulerMain.DisablePlugin();
                    }
                }

                if (ImGui.Button("设置", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f)))
                {
                    P.settingsWindowV2.IsOpen = !P.settingsWindowV2.IsOpen;
                }

                bool onlyGrabMission = C.OnlyGrabMission;
                if (ImGui.Checkbox($"仅刷取任务", ref onlyGrabMission))
                {
                    C.OnlyGrabMission = onlyGrabMission;
                    C.Save();
                }

                ImGui.PopStyleVar();

                ImGui.Spacing();

                ImGui.Separator();

                ImGui.Spacing();

                ImGui.Checkbox("当前任务结束后停止", ref Mission_Settings.StopAfterCurrent);

                bool stopCosmic = C.StopOnceHitCosmoCredits;
                if (ImGui.Checkbox($"宇宙信用点达到阈值后停止", ref stopCosmic))
                {
                    C.StopOnceHitCosmoCredits = stopCosmic;
                    C.Save();
                }
                if (stopCosmic)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);

                    int cosmicCap = C.CosmoCreditsCap;
                    if (ImGui.SliderInt("##CosmicStop", ref cosmicCap, 0, 30000))
                    {
                        if (cosmicCap > 30000)
                            cosmicCap = 30000;
                        else if (cosmicCap < 0)
                            cosmicCap = 0;

                        C.CosmoCreditsCap = cosmicCap;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                bool stopLunar = C.StopOnceHitLunarCredits;
                if (ImGui.Checkbox($"月球信用点达到阈值后停止", ref stopLunar))
                {
                    C.StopOnceHitLunarCredits = stopLunar;
                    C.Save();
                }
                if (stopLunar)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    int lunarCap = C.LunarCreditsCap;
                    if (ImGui.SliderInt("##LunarStop", ref lunarCap, 0, 10000))
                    {
                        C.LunarCreditsCap = lunarCap;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                bool stopScore = C.StopOnceHitCosmicScore;
                if (ImGui.Checkbox($"技巧点达到阈值后停止", ref stopScore))
                {
                    C.StopOnceHitCosmicScore = stopScore;
                    C.Save();
                }
                if (stopScore)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    int scoreCap = C.CosmicScoreCap;
                    if (ImGui.InputInt("###ScoreStop", ref scoreCap, 10000, 50000))
                    {
                        C.CosmicScoreCap = scoreCap >= 0 ? scoreCap : 0;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                bool stopWhenLevel = C.StopWhenLevel;
                if (ImGui.Checkbox($"等级达到阈值后停止", ref stopWhenLevel))
                {
                    C.StopWhenLevel = stopWhenLevel;
                    C.Save();
                }
                if (stopWhenLevel)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    int targetLevel = C.TargetLevel;
                    if (ImGui.SliderInt("##Level", ref targetLevel, 10, 100))
                    {
                        C.TargetLevel = targetLevel;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }
                bool relicStop = C.StopOnceRelicFinished;
                if (ImGui.Checkbox($"宇宙工具可报告时停止", ref relicStop))
                {
                    C.StopOnceRelicFinished = relicStop;
                    C.Save();
                }

                ImGui.Spacing();

                ImGui.Separator();

                ImGui.Dummy(new(0, 10));

                bool EnableRelicXp = C.XPRelicGrind;
                if (ImGui.Checkbox("自动根据研究数据挑选任务", ref EnableRelicXp)) // Auto-Pick For Relic XP
                {
                    C.XPRelicGrind = EnableRelicXp;
                    C.Save();
                }
                if (EnableRelicXp)
                {
                    bool IgnoreManual = C.XPRelicIgnoreManual;
                    if (ImGui.Checkbox("忽略手动模式任务", ref IgnoreManual)) // Ignore Manual Mode Missions
                    {
                        C.XPRelicIgnoreManual = IgnoreManual;
                        C.Save();
                    }

                    bool OnlySelected = C.XPRelicOnlyEnabled;
                    if (ImGui.Checkbox("仅限已启用的任务", ref OnlySelected)) // Only selected missions
                    {
                        C.XPRelicOnlyEnabled = OnlySelected;
                        C.Save();
                    }
                }

                ImGui.Spacing();

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                bool sinusEnabled = C.ShowSinusMissions;
                var SinusTexture = Svc.Texture.GetFromManifestResource(Assembly.GetExecutingAssembly(), SinusAsset).GetWrapOrEmpty();
                if (StyledImageButton.DrawStyledImageButton(SinusTexture, new Vector2(23, 23), sinusEnabled))
                {
                    C.ShowSinusMissions = !sinusEnabled;
                    C.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("憧憬湾");
                    ImGui.EndTooltip();
                }

                ImGui.SameLine();
                bool phaennaEnabled = C.ShowPhaennaMissions;
                var PhaennaTextures = Svc.Texture.GetFromManifestResource(Assembly.GetExecutingAssembly(), PhaennaAsset).GetWrapOrEmpty();
                if (StyledImageButton.DrawStyledImageButton(PhaennaTextures, new Vector2(23, 23), phaennaEnabled))
                {
                    C.ShowPhaennaMissions = !phaennaEnabled;
                    C.Save();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("法恩娜行星");
                    ImGui.EndTooltip();
                }

                ImGui.Dummy(new Vector2(0, 5));


                ImGui.Separator();

                ImGui.Dummy(new(0, 10));
                bool autoPickCurrentJob = C.AutoPickCurrentJob;
                if (ImGui.Checkbox("自动挑选当前职业", ref autoPickCurrentJob))
                {
                    C.AutoPickCurrentJob = autoPickCurrentJob;
                    C.Save();
                }

                uint selectedJob = C.SelectedJob;
                if (autoPickCurrentJob && usingSupportedJob)
                {
                    if (currentJobId != selectedJob)
                    {
                        selectedJob = currentJobId;
                        C.SelectedJob = selectedJob;
                        C.Save();
                    }
                }

                ImGui.Dummy(new(0, 5));

                float iconSize = 32;
                float iconSpacing = 8;
                float availWidth = ImGui.GetContentRegionAvail().X;
                float startX = (availWidth - (iconSize + iconSpacing) * 4 + iconSpacing) * 0.5f;
                ImGui.SetCursorPosX(startX);

                // Row 1: CRP, BSM, ARM, GSM
                DrawJobButtons(8, "刻木匠");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(9, "锻铁匠");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(10, "铸甲匠");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(11, "雕金匠");

                // Row 2: LTW, WVR, ALC, CUL
                ImGui.SetCursorPosX(startX);

                DrawJobButtons(12, "制革匠");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(13, "裁衣匠");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(14, "炼金术士");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(15, "烹调师");

                // Row 3: MIN, BTN, FSH
                ImGui.SetCursorPosX(startX);
                DrawJobButtons(16, "采矿工");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(17, "园艺工");
                ImGui.SameLine(0, iconSpacing);
                DrawJobButtons(18, "捕鱼人");

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Text("快捷任务应用"); // Quick Mission Apply

                ImGui.Dummy(new Vector2(0, 5));
                UpdateMissions();

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                Relic_XP.DrawRelicXP(selectedJob);
            }

            ImGui.EndChild();

            // First splitter
            ImGui.SameLine();
            ImGui.Button("##vsplitter1", new Vector2(splitterWidth, childHeight));
            if (ImGui.IsItemActive())
            {
                C.LeftColumnWidth += ImGui.GetIO().MouseDelta.X;
                C.LeftColumnWidth = Math.Max(C.LeftColumnWidth, minLeftWidth);
                C.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

            // ----------------------------
            // MIDDLE PANEL
            // ----------------------------
            ImGui.SameLine();
            if (ImGui.BeginChild("##MissionList", new Vector2(middleWidth, childHeight), true))
            {
                bool hideUnsupported = C.HideUnsupportedMissions;
                if (ImGui.Checkbox("隐藏不支持的任务", ref hideUnsupported))
                {
                    C.HideUnsupportedMissions = hideUnsupported;
                    C.Save();
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(150);
                if (ImGui.BeginCombo("排序方式", missionSortOptions[missionSelectedOption]))
                {
                    for (int i = 0; i < missionSortOptions.Length; i++)
                    {
                        bool isSelected = (i == missionSelectedOption);
                        if (ImGui.Selectable(missionSortOptions[i], isSelected))
                        {
                            missionSelectedOption = i;
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                        if (missionSelectedOption != C.TableSortOption)
                        {
                            C.TableSortOption = missionSelectedOption;
                            C.Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                // Mission Dropdown Sorting + Dropdowns themselves

                #region Mission Dropdowns

                foreach (var missionType in missionList)
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

                    if (!Jobs.Contains(selectedJob))
                        continue;

                    if (!sinusEnabled && territoryId == 1237)
                    {
                        continue;
                    }

                    if (!phaennaEnabled && territoryId == 1291)
                        continue;

                    bool isGatherMission = CosmicHelper.GatheringJobList.Overlaps(mission.Value.Jobs) || CosmicHelper.GatheringJobList.Overlaps(mission.Value.Jobs);
                    if (mission.Value.Attributes.HasFlag(MissionAttributes.Critical))
                            missionList["Critical"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                        missionList["Weather"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                        missionList["Timed"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                        missionList["Sequence"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Rank > 3)
                        missionList["ARank"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Rank == 3)
                        missionList["BRank"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Rank == 2)
                        missionList["CRank"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                    else if (mission.Value.Rank == 1)
                        missionList["DRank"].Add((mission.Key, isGatherMission, C.MissionConfig[mission.Key].Enabled));
                }


                if (showCritical)
                {
                    int amountEnabled = missionList.ContainsKey("Critical") ? missionList["Critical"].Count(mission => mission.enabled) : 0;
                    DrawCollapsibleHeader($"Critical Missions", $"紧急探索任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("Critical Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("Critical Missions", SortMissionList(missionList["Critical"]));
                    }
                }

                if (showSequential)
                {
                    int amountEnabled = missionList.ContainsKey("Sequence") ? missionList["Sequence"].Count(mission => mission.enabled) : 0;
                    DrawCollapsibleHeader($"Sequential Missions", $"连续任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("Sequential Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("Sequence Missions", SortMissionList(missionList["Sequence"]));
                    }
                }

                if (showWeather)
                {
                    int amountEnabled = missionList.ContainsKey("Weather") ? missionList["Weather"].Count(mission => mission.enabled) : 0;

                    DrawCollapsibleHeader($"Weather Missions", $"天气限定任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("Weather Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("Weather Missions", SortMissionList(missionList["Weather"]));
                    }
                }

                if (showTimeRestricted)
                {
                    int amountEnabled = missionList.ContainsKey("Timed") ? missionList["Timed"].Count(mission => mission.enabled) : 0;

                    DrawCollapsibleHeader($"Time-Restricted Missions", $"时间限定任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("Time-Restricted Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("Timed Missions", SortMissionList(missionList["Timed"]));
                    }
                }

                if (showClassA)
                {
                    int amountEnabled = missionList.ContainsKey("ARank") ? missionList["ARank"].Count(mission => mission.enabled) : 0;

                    DrawCollapsibleHeader($"A Rank Missions", $"A 类任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("A Rank Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("A Rank Missions", SortMissionList(missionList["ARank"]));
                    }
                }

                if (showClassB)
                {
                    int amountEnabled = missionList.ContainsKey("BRank") ? missionList["BRank"].Count(mission => mission.enabled) : 0;
                    DrawCollapsibleHeader($"B Rank Missions", $"B 类任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("B Rank Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("B Rank Missions", SortMissionList(missionList["BRank"]));
                    }
                }

                if (showClassC)
                {
                    int amountEnabled = missionList.ContainsKey("CRank") ? missionList["CRank"].Count(mission => mission.enabled) : 0;
                    DrawCollapsibleHeader($"C Rank Missions", $"C 类任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("C Rank Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("C Rank Missions", SortMissionList(missionList["CRank"]));
                    }
                }

                if (showClassD)
                {
                    int amountEnabled = missionList.ContainsKey("DRank") ? missionList["DRank"].Count(mission => mission.enabled) : 0;
                    DrawCollapsibleHeader($"D Rank Missions", $"D 类任务 | 启用: {amountEnabled}");
                    if (headerStates.TryGetValue("D Rank Missions", out var isOpen) && isOpen)
                    {
                        MissionInfoV2("D Rank Missions", SortMissionList(missionList["DRank"]));
                    }
                }

                #endregion
            }

            ImGui.EndChild();

            // Second splitter
            ImGui.SameLine();
            ImGui.Button("##vsplitter2", new Vector2(splitterWidth, childHeight));
            if (ImGui.IsItemActive())
            {
                C.MiddleColumnWidth += ImGui.GetIO().MouseDelta.X;
                C.MiddleColumnWidth = Math.Max(C.MiddleColumnWidth, minMiddleWidth);
                C.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

            // ----------------------------
            // RIGHT PANEL
            // ----------------------------

            ImGui.SameLine();
            if (ImGui.BeginChild("###MissionDetailPanel", new Vector2(0, childHeight), true))
            {
                if (selectedMission != 0)
                {
                    ImGui.Text($"任务信息(详细)"); // Mission Info (More Detailed)
                    ImGui.Separator();

                    var mission = SheetMissionDict[selectedMission];

                    var MissionInfo = new List<(string Label, string Value)>
                    {
                        ("ID:", $"{selectedMission}"),
                        ("任务名称:", mission.Name),
                        ("宇宙信用点:", mission.CosmoCredit.ToString()),
                        ("月球信用点", mission.LunarCredit.ToString()),
                        ("银星需求:", mission.SilverScore.ToString()),
                        ("金星需求:", mission.GoldScore.ToString())
                    };

                    float infoSize1 = MissionInfo.Max(row => ImGui.CalcTextSize(row.Label).X) + 10;
                    float infoSize2 = MissionInfo.Max(row => ImGui.CalcTextSize(row.Value).X) + 10;

                    if (ImGui.BeginTable("Detail##DetailPanelTable", 2, ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("##Label");
                        ImGui.TableSetupColumn("##Value");

                        foreach (var row in MissionInfo)
                        {
                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text(row.Label);

                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(row.Value);
                        }

                        // used as a dummy spacer because don't wanna make a whole new table / CBA
                        ImGui.TableNextRow();

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($"宇宙研究数据"); // Tool XP Reward

                        foreach (var xp in mission.RelicXpInfo.OrderBy(x => x.Key))
                        {
                            ImGui.TableNextRow();
                            string type = "";
                            switch (xp.Key)
                            {
                                case 1:
                                    type = "I";
                                    break;
                                case 2:
                                    type = "II";
                                    break;
                                case 3:
                                    type = "III";
                                    break;
                                case 4:
                                    type = "IV";
                                    break;
                                case 5:
                                    type = "V";
                                    break;
                                default:
                                    type = "???";
                                    break;
                            }

                            ImGui.Text($"Lv. {type}");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text($"{xp.Value}");
                        }

                        ImGui.EndTable();

                        ImGui.Dummy(new Vector2(0, 5));

                        ImGui.Separator();

                        ImGui.Dummy(new Vector2(0, 5));

                        MissionAttributes flags = mission.Attributes;
                        var activeFlags = Enum.GetValues(typeof(MissionAttributes))
                                              .Cast<MissionAttributes>()
                                              .Where(f => f != MissionAttributes.None && flags.HasFlag(f))
                                              .ToList();

                        var entry = C.MissionConfig.Where(e => e.Key == selectedMission);

                        ImGui.Text("备注:");
                        bool hasPreviousNotes = false;
                        if (mission.Weather != CosmicWeather.晴朗)
                        {
                            hasPreviousNotes = true;

                            ImGui.TextWrapped(mission.Weather.ToString());
                        }
                        else if (mission.StartTime != 0 && mission.EndTime != 0)
                        {
                            hasPreviousNotes = true;

                            ImGui.TextWrapped($"{mission.StartTime}:00 - {mission.EndTime}:00");
                        }
                        else if (!mission.PreviousMissions.Contains(0))
                        {
                            hasPreviousNotes = true;

                            var (Id, Name) = SheetMissionDict.Where(m => m.Key == mission.PreviousMissions.First()).Select(m => (Id: m.Key, Name: m.Value.Name)).FirstOrDefault();
                            ImGui.TextWrapped($"[{Id}] {Name}");
                        }
                        if (mission.Jobs.Last() != 0)
                        {
                            if (hasPreviousNotes) ImGui.SameLine();
                            ImGui.TextWrapped($"{jobOptions.Find(job => job.Id == mission.Jobs.First()).Name}/{jobOptions.Find(job => job.Id == mission.Jobs.Last()).Name}");
                        }

                        if (mission.Attributes.HasFlag(MissionAttributes.Gather))
                        {
                            ImGui.Dummy(new Vector2(0, 5));

                            ImGui.Separator();

                            ImGui.Dummy(new Vector2(0, 5));

                            bool craftMission = mission.Attributes.HasFlag(MissionAttributes.Craft);

                            bool LimitedQuant = mission.Attributes.HasFlag(MissionAttributes.Limited);
                            // Gather X Amount is just "Gather" 
                            bool TimedMission = mission.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);
                            bool ChainedMission = mission.Attributes.HasFlag(MissionAttributes.ScoreChains);
                            bool BoonMission = mission.Attributes.HasFlag(MissionAttributes.ScoreGatherersBoon);
                            bool collectableMission = mission.Attributes.HasFlag(MissionAttributes.Collectables);
                            bool stellerReductionMission = mission.Attributes.HasFlag(MissionAttributes.ReducedItems);

                            bool GatherX = !stellerReductionMission && !collectableMission && !BoonMission && !ChainedMission && !TimedMission && !LimitedQuant;

                            string MissionType = "";
                            if (craftMission)
                            {
                                MissionType = "双职业任务"; // Dual Class Mission
                            }
                            else if (LimitedQuant)
                            {
                                MissionType = "限制数量/采集点"; // Limited Quantity/Nodes
                            }
                            else if (TimedMission)
                                MissionType = "限时冲分/时间竞速"; // Timed Scoring/Time Attack
                            else if (ChainedMission && !BoonMission)
                                MissionType = "连锁采集冲分"; // Chained Gather Scoring
                            else if (BoonMission && !ChainedMission)
                                MissionType = "采集者的恩惠冲分"; // Gatherer's Boon Scoring
                            else if (BoonMission && ChainedMission)
                                MissionType = "连锁 + 采集者的恩惠冲分"; // Chained + Gatherer's Boon Scoring
                            else if (collectableMission && !stellerReductionMission)
                                MissionType = "收藏品冲分"; // Collectable Scoring
                            else if (stellerReductionMission)
                                MissionType = "宇宙精选/收藏品"; // Steller Reduction/Collectables
                            else if (GatherX)
                                MissionType = "采集 X 个物品"; // Gather X Amount of Items

                            ImGui.Text("任务类型: " + MissionType); // Mission Type:
                        }
#if DEBUG
                        ImGui.Dummy(new(0, 10));
                        ImGui.Text($"Debug Section");
                        ImGui.Spacing();

                        ImGui.Text($"[Debug] Active Mission Flags:");
                        foreach (var flag in activeFlags)
                        {
                            ImGui.Text($"{flag}");
                        }
#endif
                    }
                }
                else
                {
                    ImGui.TextWrapped("海盗最喜欢的字母是什么？");
                    ImGui.TextWrapped("你可能觉得是 R，但他们最爱的其实是 C（sea）<3");
                    ImGui.Dummy(new Vector2(0, 10));
                    ImGui.Text("感谢你欣赏我的老爸笑话");
                }
            }
            ImGui.EndChild();
        }
        public void DrawJobSelection(uint jobId, string tooltip)
        {
            uint selectedJob = C.SelectedJob;
            bool state = selectedJob == jobId;
            ISharedImmediateTexture? icon = state ? CosmicHelper.JobIconDict[jobId] : CosmicHelper.GreyTexture[jobId];

            // Slight padding around the button
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));

            int styleCount = 1;
            int colorCount = 0;

            if (state)
            {
                // Dalamud theme
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.35f, 0.7f));
                ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.ParsedGold);
                colorCount = 2;

                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);
                styleCount++;
            }
            else
            {
                // Disabled job with Dalamud theme
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.4f, 0.4f, 0.4f, 0.5f));
                colorCount = 2;
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.5f);
                styleCount++;
            }

            // Rounded corners
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2.0f);
            styleCount++;

            Vector2 size = new Vector2(26, 26);
            float zoomFactor = 0.25f; // 25% zoom-in
            float cropAmount = zoomFactor / 2; // Crop equally from all sides

            Vector2 uv0 = state ? new Vector2(0, 0) : new Vector2(cropAmount, cropAmount);
            Vector2 uv1 = state ? new Vector2(1, 1) : new Vector2(1 - cropAmount, 1 - cropAmount);

            bool autoPickCurrentJob = C.AutoPickCurrentJob;
            if (ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, size, uv0, uv1))
            {
                if (autoPickCurrentJob)
                {
                    autoPickCurrentJob = false;
                    C.AutoPickCurrentJob = autoPickCurrentJob;
                }
                C.SelectedJob = jobId;
                selectedJob = jobId;
                C.Save();
            }

            // Pop style variables and colors
            ImGui.PopStyleVar(styleCount);
            if (colorCount > 0)
            {
                ImGui.PopStyleColor(colorCount);
            }

            // Show tooltip on hover
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"{tooltip}");
                ImGui.EndTooltip();
            }
        }

        public void DrawJobButtons(uint jobId, string tooltip)
        {
            uint selectedJob = C.SelectedJob;
            bool state = selectedJob == jobId;
            ISharedImmediateTexture? icon = state ? CosmicHelper.JobIconDict[jobId] : CosmicHelper.GreyTexture[jobId];
            Vector2 size = new Vector2(26, 26);
            bool autoPickCurrentJob = C.AutoPickCurrentJob;

            if (StyledImageButton.DrawStyledImageButton(icon, size, state))
            {
                if (autoPickCurrentJob)
                {
                    autoPickCurrentJob = false;
                    C.AutoPickCurrentJob = autoPickCurrentJob;
                }

                C.SelectedJob = jobId;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(tooltip);
                ImGui.EndTooltip();
            }
        }

        private void DrawCollapsibleHeader(string id, string label, float spacing = 4f)
        {
            var drawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var windowWidth = ImGui.GetContentRegionAvail().X;

            var padding = 6.0f;
            var textSize = ImGui.CalcTextSize(label);
            var bgHeight = textSize.Y + padding * 2;

            if (!headerStates.ContainsKey(id))
                headerStates[id] = false;

            var headerRectMin = cursorPos;
            var headerRectMax = new Vector2(cursorPos.X + windowWidth, cursorPos.Y + bgHeight);

            // Draw background
            drawList.AddRectFilled(headerRectMin, headerRectMax, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)), 2f);
            drawList.AddRect(headerRectMin, headerRectMax, ImGui.GetColorU32(ImGuiColors.ParsedGold), 2f);

            // Draw centered label text
            var textPos = new Vector2(
                cursorPos.X + (windowWidth - textSize.X) * 0.5f,
                cursorPos.Y + padding
            );
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), label);

            // Register invisible button for interaction using a unique ID
            ImGui.SetCursorScreenPos(cursorPos);
            ImGui.PushID(id); // Use internal ID
            ImGui.InvisibleButton("##header", new Vector2(windowWidth, bgHeight));
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                headerStates[id] = !headerStates[id];
            ImGui.PopID();

            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + bgHeight + spacing));
        }
        private void MissionInfoV2(string tableName, List<(uint id, bool ShowGather, bool enabled)> missions)
        {
            uint selectedJob = C.SelectedJob;
            // Fixed column count - include ALL possible columns
            int totalColumns = 15; // Enabled, Manual, ID, Mission Name, Cosmo, Lunar, I, II, III, IV, Turnin, Gather, Notes

            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.SizingFixedFit |
                                        ImGuiTableFlags.Resizable |           // Allow column resizing
                                        ImGuiTableFlags.Reorderable |         // Allow column reordering
                                        ImGuiTableFlags.Hideable;             // Allow hiding columns via right-click

            if (ImGui.BeginTable($"MissionList###{tableName}_{selectedJob}", totalColumns, tableFlags))
            {
                float padding = 10f;

                // Setup ALL columns - all visible by default, users can hide what they don't want via right-click
                ImGui.TableSetupColumn("启用", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("启用").X + padding);
                ImGui.TableSetupColumn("手动", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("手动").X + padding);
                ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("99999").X + padding);
                ImGui.TableSetupColumn("任务名称", ImGuiTableColumnFlags.WidthFixed, 250f);
                ImGui.TableSetupColumn("宇宙信用点", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("宇宙信用点").X + padding);
                ImGui.TableSetupColumn("月球信用点", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("月球信用点").X + padding);
                ImGui.TableSetupColumn("技巧点", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("技巧点").X + padding);

                // XP columns
                float xpWidth = ImGui.CalcTextSize("III").X + padding;
                ImGui.TableSetupColumn("I", ImGuiTableColumnFlags.WidthFixed, xpWidth);
                ImGui.TableSetupColumn("II", ImGuiTableColumnFlags.WidthFixed, xpWidth);
                ImGui.TableSetupColumn("III", ImGuiTableColumnFlags.WidthFixed, xpWidth);
                ImGui.TableSetupColumn("IV", ImGuiTableColumnFlags.WidthFixed, xpWidth);
                ImGui.TableSetupColumn("V", ImGuiTableColumnFlags.WidthFixed, xpWidth);

                ImGui.TableSetupColumn("汇报模式", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("汇报模式").X + padding + 15);
                ImGui.TableSetupColumn("采集配置", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("采集配置").X + padding + 15);
                ImGui.TableSetupColumn("任务备注", ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize("任务备注").X + padding, 75));

                // Render headers with right-click menu
                ImGui.TableHeadersRow();

                foreach (var entry in missions)
                {
                    var Id = entry.id;
                    var missionConfig = C.MissionConfig[Id];
                    var missionInfo = CosmicHelper.SheetMissionDict[Id];

                    bool unsupported = UnsupportedMissions.Ids.Contains(Id) || missionInfo.Jobs.Overlaps(CosmicHelper.GatheringJobList);

                    bool craftMission = missionInfo.Attributes.HasFlag(MissionAttributes.Craft);
                    bool gatherMission = missionInfo.Attributes.HasFlag(MissionAttributes.Gather);
                    bool fishMission = missionInfo.Attributes.HasFlag(MissionAttributes.Fish);
                    bool collectableMission = missionInfo.Attributes.HasFlag(MissionAttributes.Collectables);
                    bool stellerReductionMission = missionInfo.Attributes.HasFlag(MissionAttributes.ReducedItems);

                    bool dualclass = craftMission && (gatherMission || fishMission);
                    bool hideUnsupported = C.HideUnsupportedMissions;

                    if (unsupported && hideUnsupported)
                        continue;

                    ImGui.TableNextRow();

                    // Mission Enable/Disable Checkbox
                    ImGui.PushID(Id);

                    // Enable | Disable Mission Selection
                    ImGui.TableSetColumnIndex(0);
                    bool enabled = missionConfig.Enabled;
                    if (CenterCheckbox("##EnableMission", ref enabled))
                    {
                        missionConfig.Enabled = enabled;
                        if (GetOnlyPreviousMissionsRecursive(Id).Count >0)
                        {
                            foreach (var prevMission in GetOnlyPreviousMissionsRecursive(Id))
                            {
                                var prevMissionConfig = C.MissionConfig[prevMission];
                                prevMissionConfig.Enabled = true;
                            }
                        }

                        C.Save();
                    }
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = Id;
                    }


                    // Manual mode checkbox
                    ImGui.TableNextColumn();
                    bool manualMode = missionConfig.ManualMode;
                    if (CenterCheckbox("##Manual Mode", ref manualMode))
                    {
                        missionConfig.ManualMode = manualMode;
                        C.Save();
                    }
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = Id;
                    }

                    // Mission ID
                    ImGui.TableNextColumn();
                    CenterTextInTableCell(Id.ToString());

                    // Mission Name
                    ImGui.TableNextColumn();
                    if (unsupported)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f)); // Red color (RGBA)
                        ImGuiEx.IconWithTooltip(FontAwesomeIcon.ExclamationTriangle, "这些现在尚未支持，我正在努力迁移过来。\n" +
                                                "只是需要一些时间。");
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                    }

                    ImGui.Text(missionInfo.Name);
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = Id;
                    }
                    if (missionInfo.MarkerId != 0)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.Flag.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemClicked())
                        {
                            selectedMission = Id;
                            Utils.SetGatheringRing(missionInfo.TerritoryId, (int)missionInfo.MapPosition.X, (int)missionInfo.MapPosition.Y, missionInfo.Radius, missionInfo.Name);
                        }
                    }

                    // Cosmo/Lunar Credits
                    ImGui.TableNextColumn();
                    CenterTextInTableCell(missionInfo.CosmoCredit.ToString());

                    ImGui.TableNextColumn();
                    CenterTextInTableCell(missionInfo.LunarCredit.ToString());

                    ImGui.TableNextColumn();
                    CenterTextInTableCell(missionInfo.ClassScore.ToString());

                    // XP Columns
                    for (int i = 1; i < 6; i++)
                    {
                        ImGui.TableNextColumn();
                        var expReward = missionInfo.RelicXpInfo.Where(exp => exp.Key == i).FirstOrDefault();
                        var relicXp = expReward.Value.ToString();

                        if (relicXp == "0")
                        {
                            relicXp = "-";
                        }

                        CenterTextInTableCell(relicXp);
                    }

                    // Mission Turnin Settings
                    ImGui.TableNextColumn();
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining))
                    {
                        CenterTextInTableCell("自动");
                        if (missionConfig.AutoTurnin == false)
                        {
                            missionConfig.AutoTurnin = true;
                            missionConfig.TurninGold = false;
                            missionConfig.TurninSilver = false;
                            missionConfig.TurninBronze = false;

                            C.Save();
                        }
                    }
                    else
                    {
                        if (CenterButton("选择汇报"))
                        {
                            ImGui.OpenPopup("Mission Turnin Settings");
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            if (missionConfig.AutoTurnin)
                                ImGui.Text($"自动 - True");
                            else
                            {
                                if (missionConfig.TurninGold)
                                    ImGui.Text($"金星");
                                if (missionConfig.TurninSilver)
                                    ImGui.Text($"银星");
                                if (missionConfig.TurninBronze)
                                    ImGui.Text($"铜星");
                            }

                            ImGui.EndTooltip();
                        }

                        if (ImGui.BeginPopup("Mission Turnin Settings"))
                        {
                            bool anyTurnin = missionConfig.AutoTurnin;
                            bool goldTurnin = missionConfig.TurninGold;
                            bool silverTurnin = missionConfig.TurninSilver;
                            bool bronzeTurnin = missionConfig.TurninBronze;

                            ImGui.Text("选择汇报选项");
                            ImGui.Dummy(new Vector2(0, 2));

                            if (ImGui.Checkbox("自动", ref anyTurnin))
                            {
                                if (anyTurnin)
                                {
                                    missionConfig.TurninGold = false;
                                    missionConfig.TurninSilver = false;
                                    missionConfig.TurninBronze = false;

                                    missionConfig.AutoTurnin = anyTurnin;
                                }
                                else
                                {
                                    if (!(bronzeTurnin && silverTurnin && goldTurnin))
                                    {
                                        missionConfig.AutoTurnin = true;
                                    }
                                }

                                C.Save();
                            }
                            ImGuiEx.HelpMarker("此选项将尽力获得最佳结果，但在必要时也会汇报任意结果而避免中止。");

                            ImGui.Separator();

                            if (ImGui.Checkbox("金星", ref goldTurnin))
                            {
                                if (anyTurnin && goldTurnin)
                                    missionConfig.AutoTurnin = false;

                                missionConfig.TurninGold = goldTurnin;
                                C.Save();
                            }
                            if (ImGui.Checkbox("银星", ref silverTurnin))
                            {
                                if (anyTurnin && silverTurnin)
                                    missionConfig.AutoTurnin = false;

                                missionConfig.TurninSilver = silverTurnin;
                                C.Save();
                            }
                            if (ImGui.Checkbox("铜星", ref bronzeTurnin))
                            {
                                if (anyTurnin && bronzeTurnin)
                                    missionConfig.AutoTurnin = false;

                                missionConfig.TurninBronze = bronzeTurnin;
                                C.Save();
                            }

                            ImGui.EndPopup();
                        }
                    }

                    // Gather Mission Profile Settings
                    ImGui.TableNextColumn();
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.Gather))
                    {
                        string profileName = C.GatherSettings[missionConfig.GatherProfileId].Name;

                        if (CenterButton($"{profileName}##GatherProfile_{profileName}"))
                        {
                            ImGui.OpenPopup("Selecting Gathering Profile");
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("选择要应用的配置");
                            ImGui.EndTooltip();
                        }
                        if (ImGui.BeginPopup("Selecting Gathering Profile"))
                        {
                            ImGui.Text($"当前已选择: {profileName}");
                            ImGui.Separator();
                            for (int i = 0; i < C.GatherSettings.Count; i++)
                            {
                                var GProfile = C.GatherSettings[i];
                                bool GProfileSelected = missionConfig.GatherProfileId == i;

                                if (ImGui.RadioButton(GProfile.Name, GProfileSelected))
                                {
                                    missionConfig.GatherProfileId = i;
                                    C.Save();
                                }
                            }

                            ImGui.EndPopup();
                        }
                    }
                    else if (missionInfo.Attributes.HasFlag(MissionAttributes.Fish))
                    {
                        if (CenterButton($"选择配置##Select_Fishing_Profile"))
                        {
                            ImGui.OpenPopup("Select Fishing Profile");
                        }
                        if (ImGui.BeginPopup("Select Fishing Profile"))
                        {
                            ImGui.Text($"钓鱼配置: {missionInfo.Name}");
                            ImGui.Separator();
                            bool builtInPreset = missionConfig.Use_BuildinPreset;
                            if (ImGui.Checkbox("使用内置预设", ref builtInPreset))
                            {
                                missionConfig.Use_BuildinPreset = builtInPreset;
                                C.Save();
                            }
                            ImGuiEx.HelpMarker("启用此选项表示将使用插件中内置的 Autohook 默认预设。 \n" +
                                               "如果您希望使用自己在 Autohook 中已有的预设，可以取消勾选此项，并在下方输入预设名称。");
                            using (ImRaii.Disabled(builtInPreset))
                            {
                                string presetName = missionConfig.AutoHookPresetName;
                                ImGui.SetNextItemWidth(200);
                                if (ImGui.InputText("预设名称", ref presetName))
                                {
                                    missionConfig.AutoHookPresetName = presetName;
                                    C.Save();
                                }
                            }

                            ImGui.EndPopup();
                        }
                    }

                        ImGui.TableNextColumn();
                    int notesCount = 0;

                    ImGui.Dummy(new(2, 0));
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.ListOl.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            var prevMissions = GetOnlyPreviousMissionsRecursive(Id);

                            ImGui.BeginTooltip();
                            ImGui.Text("连续任务");
                            ImGui.Separator();
                            for (int i = 0; i < prevMissions.Count; i++)
                            {
                                var prevMission = prevMissions[i];
                                ImGui.Text($"{i + 1}: [{prevMission}] - {CosmicHelper.SheetMissionDict[prevMission].Name}");
                            }
                            ImGui.EndTooltip();
                        }
                        notesCount++;
                    }
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                    {
                        if (notesCount > 0)
                            ImGui.SameLine();

                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.Cloud.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"天气: {missionInfo.Weather}");
                            ImGui.EndTooltip();
                        }
                        notesCount++;
                    }
                    if (missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                    {
                        if (notesCount > 0)
                            ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.Clock.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{missionInfo.StartTime}:00 - {missionInfo.EndTime}:00");
                            ImGui.EndTooltip();
                        }
                        notesCount++;
                    }
                    if (missionInfo.Jobs.Count > 1)
                    {
                        if (notesCount > 0)
                            ImGui.SameLine();

                        ISharedImmediateTexture? job1Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.First()];
                        ISharedImmediateTexture? job2Icon = CosmicHelper.JobIconDict[missionInfo.Jobs.Last()];
                        Vector2 imageSize = new Vector2(23, 23);

                        ImGui.Image(job1Icon.GetWrapOrEmpty().Handle, imageSize);
                        ImGui.SameLine();
                        ImGui.Image(job2Icon.GetWrapOrEmpty().Handle, imageSize);
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        private Dictionary<string, bool> QuickSetModes = new()
        {
            ["Any"] = true,
            ["Gold"] = false,
            ["Silver"] = false,
            ["Bronze"] = false,
            ["Manual"] = false,
        };

        private List<string> QuickApplyOptions = new() { "当前职业", "所有启用的任务", "所有任务" };
        private int QuickSelectedOption = 0;

        private void UpdateMissions()
        {
            ImGui.SetNextItemWidth(100);
            if (ImGui.Button("选择模式"))
            {
                ImGui.OpenPopup("Select Mission Profiles");
            }

            if (ImGui.BeginPopup("Select Mission Profiles"))
            {
                ImGui.Text("快捷设置模式");

                // Any checkbox
                bool anyValue = QuickSetModes["Any"];
                bool goldValue = QuickSetModes["Gold"];
                bool silverValue = QuickSetModes["Silver"];
                bool bronzeValue = QuickSetModes["Bronze"];
                if (!(anyValue ||  goldValue || silverValue || bronzeValue))
                {
                    QuickSetModes["Any"] = true;
                }

                if (ImGui.Checkbox("任意", ref anyValue))
                {
                    QuickSetModes["Any"] = anyValue;
                    if (anyValue && (goldValue ||  silverValue || bronzeValue))
                    {
                        QuickSetModes["Gold"] = false;
                        QuickSetModes["Silver"] = false;
                        QuickSetModes["Bronze"] = false;
                    }
                }

                // Separator between Any and medal types
                ImGui.Separator();

                // Medal checkboxes
                if (ImGui.Checkbox("金星", ref goldValue))
                {
                    QuickSetModes["Gold"] = goldValue;
                    QuickSetModes["Any"] = false;
                }

                if (ImGui.Checkbox("银星", ref silverValue))
                {
                    QuickSetModes["Silver"] = silverValue;
                    QuickSetModes["Any"] = false;
                }

                if (ImGui.Checkbox("铜星", ref bronzeValue))
                {
                    QuickSetModes["Bronze"] = bronzeValue;
                    QuickSetModes["Any"] = false;
                }

                // Separator between medals and Manual
                ImGui.Separator();

                // Manual checkbox
                bool manualValue = QuickSetModes["Manual"];
                if (ImGui.Checkbox("手动", ref manualValue))
                {
                    QuickSetModes["Manual"] = manualValue;
                }

                ImGui.EndPopup();
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo("快捷应用", QuickApplyOptions[QuickSelectedOption]))
            {
                for (int i = 0; i < QuickApplyOptions.Count; i++)
                {
                    bool isSelected = (QuickSelectedOption == i);
                    if (ImGui.Selectable(QuickApplyOptions[i], isSelected))
                    {
                        QuickSelectedOption = i;
                        string selectedOption = QuickApplyOptions[QuickSelectedOption];
                    }

                    // Set the initial focus when opening the combo
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.Text($"选项: {QuickSelectedOption}");
            ImGui.Text($"选定的职业ID: {C.SelectedJob}");
            if (ImGui.Button("应用到所选配置"))
            {
                var currentJob = Player.JobId;

                foreach (var mission in C.MissionConfig)
                {
                    var id = mission.Key;
                    var missionDict = CosmicHelper.SheetMissionDict[id];
                    bool selectedJob = missionDict.Jobs.Contains(C.SelectedJob);
                    bool TimedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);

                    PluginLog.Information($"ID: {id} | Enabled: {mission.Value.Enabled}");

                    if (QuickSelectedOption == 0 && selectedJob)
                    {
                        if (!selectedJob)
                        {
                            PluginLog.Information($"Option 0 was checked, and returned false");
                            continue;
                        }
                        else
                        {
                            PluginLog.Information($"Option 0 was valid! Continuing on");
                        }
                    }
                    if (QuickSelectedOption == 1)
                    {
                        if (!mission.Value.Enabled)
                        {
                            PluginLog.Information($"Option 1 was selected, but not enabled." +
                                                  $"Mission Id: {mission.Key} | Enabled? : {mission.Value.Enabled}" +
                                                  $"Skipping for now");
                            continue;
                        }
                        else
                        {
                            PluginLog.Information($"Option 1 was valid! Continuing on");
                        }
                    }

                    PluginLog.Information($"Mission is being modified: {id}");

                    if (TimedMission)
                    {
                        PluginLog.Information($"Timed Mission: \n" +
                                              $"Any: {QuickSetModes["Any"]}" +
                                              $"Gold: {QuickSetModes["Gold"]}" +
                                              $"Silver: {QuickSetModes["Silver"]}" +
                                              $"Bronze: {QuickSetModes["Bronze"]}" +
                                              $"Manual: {QuickSetModes["Manual"]}");

                        mission.Value.AutoTurnin = QuickSetModes["Any"];
                        mission.Value.TurninGold = false;
                        mission.Value.TurninSilver = false;
                        mission.Value.TurninBronze = false;
                        mission.Value.ManualMode = QuickSetModes["Manual"];
                    }
                    else
                    {
                        PluginLog.Information($"Non-timed Mission \n" +
                                              $"Any: {QuickSetModes["Any"]}" +
                                              $"Gold: {QuickSetModes["Gold"]}" +
                                              $"Silver: {QuickSetModes["Silver"]}" +
                                              $"Bronze: {QuickSetModes["Bronze"]}" +
                                              $"Manual: {QuickSetModes["Manual"]}");

                        mission.Value.AutoTurnin = QuickSetModes["Any"];
                        mission.Value.TurninGold = QuickSetModes["Gold"];
                        mission.Value.TurninSilver = QuickSetModes["Silver"];
                        mission.Value.TurninBronze = QuickSetModes["Bronze"];
                        mission.Value.ManualMode = QuickSetModes["Manual"];
                    }
                }

                C.Save();
            }
        }

        private class XPType
        {
            public uint CurrentXP { get; set; }
            public uint NeededXP { get; set; }
            public uint MaxXP { get; set; }
        }

        #region Table Tools

        private void CenterTextInTableCell(string text)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float textWidth = ImGui.CalcTextSize(text).X;
            float offset = (cellWidth - textWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            ImGui.TextUnformatted(text);
        }
        private bool CenterCheckbox(string label, ref bool value)
        {
            // Checkbox size is roughly the font size
            float checkboxSize = ImGui.GetFontSize();
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float offset = Math.Max(0f, (availableWidth - checkboxSize) * 0.5f);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            return ImGui.Checkbox(label, ref value);
        }
        private bool CenterButton(string label, Vector2? size = null)
        {
            Vector2 buttonSize = size ?? ImGui.CalcTextSize(label) + ImGui.GetStyle().FramePadding * 2;
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float offset = Math.Max(0f, (availableWidth - buttonSize.X) * 0.5f);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            return size.HasValue ? ImGui.Button(label, size.Value) : ImGui.Button(label);
        }
        private List<uint> GetOnlyPreviousMissionsRecursive(uint missionId)
        {
            if (!SheetMissionDict.TryGetValue(missionId, out var missionInfo) || missionInfo.PreviousMissions.Contains(0))
                return [];

            var chain = GetOnlyPreviousMissionsRecursive(missionInfo.PreviousMissions.First());
            chain.Add(missionInfo.PreviousMissions.First());
            return chain;
        }
        private List<uint> GetOnlyNextMissionsRecursive(uint missionId)
        {
            uint? nextMissionId = SheetMissionDict
                .Where(m => m.Value.PreviousMissions.First() == missionId)
                .Select(m => (uint?)m.Key)
                .FirstOrDefault();

            if (!nextMissionId.HasValue)
                return [];

            var chain = new List<uint> { nextMissionId.Value };
            chain.AddRange(GetOnlyNextMissionsRecursive(nextMissionId.Value));
            return chain;
        }

        #endregion
    }
}
