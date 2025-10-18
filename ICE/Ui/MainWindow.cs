using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using ECommons.Automation;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Config;
using ICE.Sounds;
using ICE.Utilities.Cosmic;
using SharpDX.D3DCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static MissionTimer;
using static System.Windows.Forms.AxHost;

namespace ICE.Ui
{
    internal class MainWindow : Window
    {
        public MainWindow() :
#if DEBUG
        base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} [Debug Build] ###ICEMainWindow2")
#else
        base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow2")
#endif
        {
            Flags = ImGuiWindowFlags.NoScrollbar;
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(100, 100),
                MaximumSize = new Vector2(4000, 4000),
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

        public override void Draw()
        {
            if (ImGui.BeginTable("Main_Ui_Table", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Resizable, ImGui.GetContentRegionAvail()))
            {
                ImGui.TableSetupColumn("Main Settings", ImGuiTableColumnFlags.WidthFixed, 200);
                ImGui.TableSetupColumn("Mission Infomation", ImGuiTableColumnFlags.WidthFixed, 500);
                ImGui.TableSetupColumn("Main Settings", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                var currentAvail = ImGui.GetContentRegionAvail().Y - 5;
                var childWindowSize = new Vector2(0, currentAvail);

                ImGui.TableSetColumnIndex(0);
                if (ImGui.BeginChild("Mission Settings Window", childWindowSize, true))
                {
                    LeftWindow();
                }
                ImGui.EndChild();

                ImGui.TableSetColumnIndex(1);
                if (ImGui.BeginChild("Mission Selection Window", childWindowSize, true))
                {
                    if (C.ShowCompletionWindow)
                    {
                        CompletionWindow();
                    }
                    else
                    {
                        MiddleWindow();
                    }
                }
                ImGui.EndChild();

                ImGui.TableSetColumnIndex(2);
                if (ImGui.BeginChild("Mission Info Window", childWindowSize, true))
                {
                    RightWindow();
                }
                ImGui.EndChild();

                ImGui.EndTable();
            }
        }

        #region Left Window

        private string SinusAsset = "ICE.Resources.Sinus_Ardorum.png";
        private string PhaennaAsset = "ICE.Resources.Phaenna.png";

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

        // Do this once when you initialize, not every frame
        static float CalculateComboWidth(List<string> options)
        {
            float maxWidth = 0;
            foreach (var option in options)
            {
                var textSize = ImGui.CalcTextSize(option).X;
                if (textSize > maxWidth)
                    maxWidth = textSize;
            }
            return maxWidth + 40; // Padding for arrow and margins
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

        public void LeftWindow()
        {
            // - - - - - - - - - - - - - - 
            // 1st Section, Main Buttons
            // - - - - - - - - - - - - - - 

            uint currentJobId = Player.JobId;
            bool usingSupportedJob = CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId);
            float textLineHeight = ImGui.GetTextLineHeight();

            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
            {
                if (ImGui.Button("开始", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f)))
                {
                    SchedulerMain.EnablePlugin();
                }
            }
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("停止", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f)))
                {
                    SchedulerMain.DisablePlugin();
                }
            }
            if (ImGui.Button("设置", new Vector2(ImGui.GetContentRegionAvail().X, 30)))
            {
                P.settingsWindowV2.IsOpen = !P.settingsWindowV2.IsOpen;
            }
            bool onlyGrabMission = C.OnlyGrabMission;
            if (C.ShowInfoButton)
            {
                if (ImGui.Button("额外信息", new Vector2(ImGui.GetContentRegionAvail().X, 30)))
                {
                    P.infoWindow.IsOpen = true;
                }
            }

            if (ImGui.Checkbox($"只刷取任务", ref onlyGrabMission))
            {
                C.OnlyGrabMission = onlyGrabMission;
                C.Save();
            }
            bool removeGold = C.RemoveAfterGold;
            if (ImGui.Checkbox("金星完成时移除任务", ref removeGold)) // Remove Mission Upon Gold Completion
            {
                C.RemoveAfterGold = removeGold;
                C.Save();
            }

            if (ImGui.CollapsingHeader("任务完成情况窗口设置")) // Completion Window Settings
            {
                bool showCompletionWindow = C.ShowCompletionWindow;
                if (ImGui.Checkbox("显示任务完成情况", ref showCompletionWindow)) // Show Mission Completions
                {
                    C.ShowCompletionWindow = showCompletionWindow;
                    C.Save();
                }
                if (showCompletionWindow)
                {
                    bool onlyCurrentJob = C.ShowCompletionOnlyJob;
                    if (ImGui.Checkbox("只显示当前职业", ref onlyCurrentJob)) // Show only current job
                    {
                        C.ShowCompletionOnlyJob = onlyCurrentJob;
                        if (onlyCurrentJob)
                            C.ShowSelectedJobOnly = false;
                        C.Save();
                    }

                    bool showSelectedJobOnly = C.ShowSelectedJobOnly;
                    if (ImGui.Checkbox("只显示选择的职业", ref showSelectedJobOnly)) // Show only selected job
                    {
                        C.ShowSelectedJobOnly = showSelectedJobOnly;
                        if (showSelectedJobOnly)
                            C.ShowCompletionOnlyJob = false;
                        C.Save();
                    }

                    bool nonGold = C.ShowCompletion_MissingGold;
                    if (ImGui.Checkbox("只显示非金星完成的任务", ref nonGold)) // Show Non-Gold Missions
                    {
                        C.ShowCompletion_MissingGold = nonGold;
                        C.Save();
                    }
                }

            }
            WindowSpacer();

            // - - - - - - - - - - - - - - - - 
            // 2nd Section, Stop @ Conditions
            // - - - - - - - - - - - - - - - - 

            if (ImGui.CollapsingHeader("停止条件"))
            {
                ImGui.Checkbox("当前任务结束后停止", ref Mission_Settings.StopAfterCurrent); // Stop after current mission

                bool stopCosmic = C.StopOnceHitCosmoCredits;
                if (ImGui.Checkbox($"宇宙信用点达到阈值时", ref stopCosmic)) // Stop at Cosmic Credits
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
                if (ImGui.Checkbox($"月球信用点达到阈值时", ref stopLunar)) // Stop at Lunar Credits
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
                if (ImGui.Checkbox($"技巧点达到阈值时", ref stopScore)) // Stop at Cosmic Score
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
                if (ImGui.Checkbox($"等级达到阈值时", ref stopWhenLevel)) // Stop at Level
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
                if (ImGui.Checkbox($"宇宙工具可报告时", ref relicStop))
                {
                    C.StopOnceRelicFinished = relicStop;
                    C.Save();
                }

                bool playSoundAlert = C.PlaySoundAlert;
                if (ImGui.Checkbox("完成后播放提示音", ref playSoundAlert)) // Play Sound Alert on Stop
                {
                    C.PlaySoundAlert = playSoundAlert;
                    C.Save();
                }
                if (playSoundAlert)
                {
                    var soundVolume = C.SoundVolume;
                    ImGui.Text("音量"); // Sound Volume
                    if (ImGui.SliderFloat("##Sound Volume", ref soundVolume, 0f, 1f, "%.2f"))
                    {
                        C.SoundVolume = soundVolume;
                        C.Save();
                    }
                    if (ImGui.Button("测试提示音")) // Test Sound Alert
                    {
                        _ = SoundPlayer.PlaySoundAsync();
                    }
                }
            }
            WindowSpacer();

            // - - - - - - - - - - - - - - - - -
            // 3rd Section, Relic XP Conditions
            // - - - - - - - - - - - - - - - - -

            if (ImGui.CollapsingHeader("研究数据设置")) // Relic XP Settings
            {
                bool relicTurnin = C.TurninRelic;
                if (ImGui.Checkbox($"自动提交可报告的宇宙工具", ref relicTurnin)) // Turnin if relic is complete
                {
                    C.TurninRelic = relicTurnin;
                    C.Save();
                }
                ImGui.SameLine();
                ImGui.TextDisabled("?");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("这是关于这个功能的提示说明。如果我将来修改了这个功能，这个提示也会随之改变。 \n" +
                                     "1: 此功能会检查你的当前职业（不是菜单中选择的职业，是实际当前职业）进行提交宇宙工具。 \n" +
                                     "2: 你必须不装备宇宙工具，才能让此功能完全地自动运行。 \n" +
                                     "\t- 原因是我现在懒得写这部分逻辑。（将来可能会改主意 *耸肩*） \n" +
                                     "3: 此功能的优先级高于 \"宇宙工具可报告时停止\" 选项，如果两个都启用，它会选择报告而不是停止，并继续执行任务。 \n" +
                                     "4: 如果你当前是能工巧匠职业，报告后会自动返回你之前正在制作的位置。 \n" +
                                     "\t- 这是可选的，你可以自由关闭。我个人喜欢这样设置，方便我回到自己选定的安静区域。");
                }
                bool EnableRelicXp = C.XPRelicGrind;
                if (ImGui.Checkbox("自动根据研究数据选择任务", ref EnableRelicXp)) // Auto-Pick For Relic XP
                {
                    C.XPRelicGrind = EnableRelicXp;
                    C.Save();
                }
                ImGui.SameLine();
                ImGui.TextDisabled("?");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("请注意: 此功能只会在基础任务标签下刷取宇宙工具研究数据。 \n" + // Please note. This will ONLY grind for relic Exp under the basic mission tab. 
                                       "在 连续任务/时间限定任务/天气限定任务/紧急探索任务 中, 即使启用了这些任务也不会生效。"); // This will NOT work (even with missions selected) on the Sequence/Timed/Weather/Critical Missions
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
            }
            WindowSpacer();

            // - - - - - - - - - - - - - - - - - - - -
            // 3.1 Section, Provisional Grind Button
            // - - - - - - - - - - - - - - - - - - - - 

            bool grindProvisionals = C.GrindProvisionals;
            if (ImGui.Checkbox("刷取临时性任务", ref grindProvisionals))
            {
                C.GrindProvisionals = grindProvisionals;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("根据 Discord 的更新日志解释此功能: \n" +
                    "1. 此选项会根据任务与职业的优先级，自动切换职业，刷取已启用的 连续/天气限定/紧急探索任务 此类临时性任务 \n" +
                    "2. 您可以用此功能去追踪这些限时性质(天气、ET)的任务，设置好需要的任务后让插件循环执行这些任务 \n" +
                    "3. 优先级关系: 自动根据研究数据选择任务 > 刷取临时性任务 > 其他"
                );
            }
            ImGui.SameLine();
            if (ImGuiEx.IconButton(FontAwesomeIcon.Cog, "##Open Settings to Provisional Grind"))
            {
                P.settingsWindowV2.IsOpen = true;
                P.settingsWindowV2.SelectedSetting = "任务设置";
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("打开临时性任务设置");
            }

                WindowSpacer();

            // - - - - - - - - - - - - - - - - -
            // 4th Section, Planet Selection
            // - - - - - - - - - - - - - - - - -

            bool sinusEnabled = C.ShowSinusMissions;
            var SinusTexture = Svc.Texture.GetFromManifestResource(Assembly.GetExecutingAssembly(), SinusAsset).GetWrapOrEmpty();
            if (StyledImageButton.DrawStyledImageButton(SinusTexture, new Vector2(23, 23), sinusEnabled))
            {
                C.ShowSinusMissions = !sinusEnabled;
                C.Save();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("憧憬湾");
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
                ImGui.SetTooltip("法恩娜行星");
            }
            WindowSpacer();

            // - - - - - - - - - - - - - - - - -
            // 5th Section, Job Selection
            // - - - - - - - - - - - - - - - - -

            bool autoPickCurrentJob = C.AutoPickCurrentJob;
            if (ImGui.Checkbox("自动选择当前职业", ref autoPickCurrentJob)) // Auto Pick Current Job
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

            WindowSpacer();

            // - - - - - - - - - - - - - - - - -
            // 6th Section, Quick Mission Apply
            // - - - - - - - - - - - - - - - - -
            if (ImGui.CollapsingHeader("快速任务应用"))
            {
                ImGui.SetNextItemWidth(100);
                if (ImGui.Button("选择模式")) // Select Modes
                {
                    ImGui.OpenPopup("Select Mission Profiles");
                }

                if (ImGui.BeginPopup("Select Mission Profiles"))
                {
                    ImGui.Text("快速设置模式");

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


                ImGui.SetNextItemWidth(CalculateComboWidth(QuickApplyOptions));
                if (ImGui.BeginCombo("快速应用", QuickApplyOptions[QuickSelectedOption]))
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

                ImGui.Text($"选项索引: {QuickSelectedOption}");
                ImGui.Text($"选定职业 ID: {C.SelectedJob}");
                if (ImGui.Button("应用到选择的配置"))
                {
                    var currentJob = Player.JobId;

                    foreach (var mission in C.MissionConfig)
                    {
                        var id = mission.Key;
                        var missionDict = CosmicHelper.SheetMissionDict[id];
                        bool isSelectedJob = missionDict.Jobs.Contains(selectedJob);
                        bool TimedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);

                        PluginLog.Information($"ID: {id} | Enabled: {mission.Value.Enabled}");

                        if (QuickSelectedOption == 0 && isSelectedJob)
                        {
                            if (!isSelectedJob)
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

                WindowSpacer();

                bool disable = SchedulerMain.State != IceState.Idle;

                using (ImRaii.Disabled(disable))
                {
                    if (ImGui.Button("清理 _anon Autohook 预设"))
                    {
                        P.AutoHook.DeleteAllAnonymousPresets();
                        IceLogging.Info("This *-should-* clear all the _anon presets. If it hasn't, then there's something going on and I need to get to the bottom of this. Please copy the name of the preset when you get a chance and ping me in discord");
                    }
                }
            }

            WindowSpacer();
            // - - - - - - - - - - - - - - - - -
            // 7th Section, Relic XP Infomation
            // - - - - - - - - - - - - - - - - -
            Relic_XP.DrawRelicXP(selectedJob, true);
        }

        #endregion

        #region Middle Window

        private static Dictionary<string, List<(uint id, bool gather, bool enabled)>> missionList = new()
        {
            ["Critical"] = new List<(uint id, bool gather, bool enabled)>(),
            ["Weather"] = new List<(uint id, bool gather, bool enabled)>(),
            ["Timed"] = new List<(uint id, bool gather, bool enabled)>(),
            ["Sequence"] = new List<(uint id, bool gather, bool enabled)>(),
            ["ARank"] = new List<(uint id, bool gather, bool enabled)>(),
            ["BRank"] = new List<(uint id, bool gather, bool enabled)>(),
            ["CRank"] = new List<(uint id, bool gather, bool enabled)>(),
            ["DRank"] = new List<(uint id, bool gather, bool enabled)>()
        };
        private string[] missionSortOptions = ["ID", "名称", "宇宙信用点", "月球信用点", "研究数据 I", "研究数据 II", "研究数据 III", "研究数据 IV", "研究数据 V", "地图位置"];
        private Dictionary<string, bool> headerStates = new();

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
            int totalColumns = 16; // Enabled, Manual, ID, Completion Status, Mission Name, Cosmo, Lunar, I, II, III, IV, Turnin, Gather, Notes

            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.Reorderable |         // Allow column reordering
                                        ImGuiTableFlags.Hideable |             // Allow hiding columns via right-click
                                        ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable($"MissionList###{tableName}_{selectedJob}", totalColumns, tableFlags))
            {
                float padding = 10f;

                // Setup ALL columns
                ImGui.TableSetupColumn("启用");
                ImGui.TableSetupColumn("手动");
                ImGui.TableSetupColumn("ID");
                ImGui.TableSetupColumn("✓");
                ImGui.TableSetupColumn("任务名称");
                ImGui.TableSetupColumn("宇宙信用点");
                ImGui.TableSetupColumn("月球信用点");
                ImGui.TableSetupColumn("技巧点");

                // XP columns
                float xpWidth = ImGui.CalcTextSize("III").X + padding;
                ImGui.TableSetupColumn("I");
                ImGui.TableSetupColumn("II");
                ImGui.TableSetupColumn("III");
                ImGui.TableSetupColumn("IV");
                ImGui.TableSetupColumn("V");

                ImGui.TableSetupColumn("汇报模式");
                ImGui.TableSetupColumn("采集配置");
                ImGui.TableSetupColumn("任务备注");

                // Draw custom header row with tooltips
                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

                // Column 0: Enabled
                ImGui.TableSetColumnIndex(0);
                ImGui.TableHeader("启用");
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.OpenPopup("Enabled Options");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("启用/禁用 自动执行任务");
                    ImGui.Text($"左键点击查看选项");
                    ImGui.EndTooltip();
                }
                if (ImGui.BeginPopup("Enabled Options"))
                {
                    if (ImGui.Button("全部启用"))
                    {
                        foreach (var mission in missions)
                        {
                            C.MissionConfig[mission.id].Enabled = true;
                            if (GetOnlyPreviousMissionsRecursive(mission.id).Count > 0)
                            {
                                foreach (var prevMission in GetOnlyPreviousMissionsRecursive(mission.id))
                                {
                                    var prevMissionConfig = C.MissionConfig[prevMission];
                                    prevMissionConfig.Enabled = true;
                                }
                            }
                        }
                        C.Save();
                    }

                    if (ImGui.Button("全部禁用"))
                    {
                        foreach (var mission in missions)
                        {
                            C.MissionConfig[mission.id].Enabled = false;
                        }
                        C.Save();
                    }

                    ImGui.EndPopup();
                }

                // Column 1: Manual
                ImGui.TableSetColumnIndex(1);
                ImGui.AlignTextToFramePadding();
                ImGui.TableHeader("手动");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("手动模式 - 需要手动干预");
                    ImGui.EndTooltip();
                }

                // Column 2: ID
                ImGui.TableSetColumnIndex(2);
                ImGui.TableHeader("ID");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("任务 ID 数字");
                    ImGui.EndTooltip();
                }

                // Column 3: Completed (with Unicode checkmark)
                ImGui.TableSetColumnIndex(3);
                ImGui.TableHeader("✓");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("任务完成状态");
                    ImGui.EndTooltip();
                }

                // Column 4: Mission Name
                ImGui.TableSetColumnIndex(4);
                ImGui.TableHeader("任务名称");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("点击任务名称查看详情");
                    ImGui.EndTooltip();
                }

                // Continue this pattern for all your columns...
                // Column 5: Cosmo
                ImGui.TableSetColumnIndex(5);
                ImGui.TableHeader("宇宙信用点");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("宇宙信用点奖励");
                    ImGui.EndTooltip();
                }

                // Column 6: Lunar
                ImGui.TableSetColumnIndex(6);
                ImGui.TableHeader("月球信用点");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("月球信用点奖励");
                    ImGui.EndTooltip();
                }

                // Column 7: Score
                ImGui.TableSetColumnIndex(7);
                ImGui.TableHeader("技巧点");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("职业技巧点奖励");
                    ImGui.EndTooltip();
                }

                // XP Columns (8-12)
                string[] xpLabels = { "I", "II", "III", "IV", "V" };
                for (int i = 0; i < 5; i++)
                {
                    ImGui.TableSetColumnIndex(8 + i);
                    ImGui.TableHeader(xpLabels[i]);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"宇宙研究数据类型: {xpLabels[i]} 奖励");
                        ImGui.EndTooltip();
                    }
                }

                // Column 13: Turnin Mode
                ImGui.TableSetColumnIndex(13);
                ImGui.TableHeader("汇报模式");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("配置任务汇报设置");
                    ImGui.EndTooltip();
                }

                // Column 14: Gathering Profile
                ImGui.TableSetColumnIndex(14);
                ImGui.TableHeader("采集配置");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("为采集任务选择采集配置");
                    ImGui.EndTooltip();
                }

                // Column 15: Mission Notes
                ImGui.TableSetColumnIndex(15);
                ImGui.TableHeader("任务备注");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("任务的补充说明与要求");
                    ImGui.EndTooltip();
                }

                foreach (var entry in missions)
                {
                    var Id = entry.id;
                    var missionConfig = C.MissionConfig[Id];
                    var missionInfo = CosmicHelper.SheetMissionDict[Id];

                    bool craftMission = missionInfo.Attributes.HasFlag(MissionAttributes.Craft);
                    bool gatherMission = missionInfo.Attributes.HasFlag(MissionAttributes.Gather);
                    bool fishMission = missionInfo.Attributes.HasFlag(MissionAttributes.Fish);
                    bool critical = missionInfo.Attributes.HasFlag(MissionAttributes.Critical);

                    bool dualclass = craftMission && (gatherMission || fishMission);
                    bool unsupported = UnsupportedMissions.Ids.Contains(Id);
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
                        if (missionConfig.Enabled == true)
                        {
                            if (GetOnlyPreviousMissionsRecursive(Id).Count >0)
                            {
                                foreach (var prevMission in GetOnlyPreviousMissionsRecursive(Id))
                                {
                                    var prevMissionConfig = C.MissionConfig[prevMission];
                                    prevMissionConfig.Enabled = true;
                                }
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

                    // Completion Status
                    ImGui.TableNextColumn();
                    CompletionStatus_Formatted(Id);

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
                    bool gatherProfile = missionInfo.Attributes.HasFlag(MissionAttributes.Gather);
                    bool collectable = missionInfo.Attributes.HasFlag(MissionAttributes.Collectables) || missionInfo.Attributes.HasFlag(MissionAttributes.ReducedItems);

                    // Gather Mission Profile Settings
                    ImGui.TableNextColumn();
                    if (gatherProfile && !collectable)
                    {
                        string profileName = "???";
                        var profileSettings = C.GatherSettings.Where(x => x.Id == missionConfig.GatherProfileId).FirstOrDefault();

                        if (profileSettings != null)
                        {
                            profileName = profileSettings.Name;
                        }
                        else
                        {
                            profileName = "???";
                        }

                        if (CenterButton($"{profileName}##GatherProfile_{profileName}"))
                        {
                            ImGui.OpenPopup("Selecting Gathering Profile");
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("选择要使用的配置");
                            ImGui.EndTooltip();
                        }
                        if (ImGui.BeginPopup("Selecting Gathering Profile"))
                        {
                            ImGui.Text($"当前已选择: {profileName}");
                            ImGui.Separator();

                            foreach (var profile in C.GatherSettings)
                            {
                                if (profile != null)
                                {
                                    var profileId = profile.Id;
                                    bool profileSelected = missionConfig.GatherProfileId == profileId;
                                    if (ImGui.RadioButton(profile.Name, profileSelected))
                                    {
                                        missionConfig.GatherProfileId = profileId;
                                        C.Save();
                                    }
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

                        if (CosmicHelper.WeatherIds.ContainsKey(missionInfo.Weather))
                        {
                            ISharedImmediateTexture? weatherIcon = CosmicHelper.WeatherIconDict[missionInfo.Weather];
                            Vector2 ImageSize = new Vector2(23, 23);
                            ImGui.Image(weatherIcon.GetWrapOrEmpty().Handle, ImageSize);
                        }
                        else
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Text(FontAwesomeIcon.Cloud.ToIconString());
                            ImGui.PopFont();
                        }

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
                    if (CosmicHelper.MissionUnlock.TryGetValue(Id, out var unlock))
                    {
                        if (notesCount > 0)
                            ImGui.SameLine();

                        if (Svc.Texture.GetFromGame("ui/uld/WKSMission_hr1.tex") is { } tex)
                        {
                            if (tex.TryGetWrap(out var wrap, out var exc))
                            {
                                ImGui.Image(wrap.Handle, new Vector2(23, 23), new Vector2(0.2347f, 0.3500f), new Vector2(0.2959f, 0.6500f));
                            }
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("需要以下任务达到金星评价后, 才能进行此任务");
                            foreach (var mission in unlock)
                            {
                                CompletionStatus_Normal(mission);
                                ImGui.SameLine();
                                ImGui.Text($"[{mission}] - {CosmicHelper.SheetMissionDict[mission].Name}");
                            }
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
        private List<(uint id, bool gather, bool enabled)> SortMissionList(List<(uint id, bool gather, bool enabled)> missions)
        {
            int sortOption = C.TableSortOption;
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

        private string _idSearchText = "";
        private string _nameSearchText = "";

        private void MiddleWindow()
        {
            bool hideUnsupported = C.HideUnsupportedMissions;
            if (ImGui.Checkbox("隐藏不支持的任务", ref hideUnsupported))
            {
                C.HideUnsupportedMissions = hideUnsupported;
                C.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);

            int missionSelectedOption = C.TableSortOption;
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

            ImGui.SameLine();

            ImGui.Text("表格帮助: ");
            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("以下表格包含一些实用功能，包括: \n" +
                 "-> 右键点击表格顶部行可以选择隐藏某些列。这完全取决于您的个人偏好, 不会影响任何功能。如果有些列对您来说没用或不关心, 可以自由隐藏。\n" +
                 "-> 您可以自由调整列的顺序。不想让“手动”列紧挨着“启用”？或者您希望将“研究数据”相关的列移到前面？都可以, 只需拖动列标题到您想要的位置即可。");
            }

            WindowSpacer();

            bool showCritical = C.ShowCritical;
            bool showSequential = C.ShowSequential;
            bool showWeather = C.ShowWeather;
            bool showTimeRestricted = C.ShowTimeRestricted;
            bool showClassA = C.ShowClassA;
            bool showClassB = C.ShowClassB;
            bool showClassC = C.ShowClassC;
            bool showClassD = C.ShowClassD;

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
        }

        private unsafe void CompletionWindow()
        {
            List<uint> missionIds = new();
            foreach (var mission in CosmicHelper.SheetMissionDict)
            {
                if (C.ShowCompletionOnlyJob && !mission.Value.Jobs.Contains(Player.JobId))
                    continue;

                if (C.ShowSelectedJobOnly && !mission.Value.Jobs.Contains(C.SelectedJob))
                    continue;

                if (!C.ShowSinusMissions && mission.Value.TerritoryId == 1237)
                    continue;

                if (!C.ShowPhaennaMissions && mission.Value.TerritoryId == 1291)
                    continue;

                if (C.ShowCompletion_MissingGold)
                {
                    var managerPtr = WKSManager.Instance();
                    if (managerPtr == null) continue;

                    var manager = (WKSManagerCustom*)managerPtr;
                    var isGold = manager->IsMissionGolded(mission.Key);

                    if (isGold)
                        continue;
                }

                if (!string.IsNullOrEmpty(_idSearchText) && !mission.Key.ToString().Contains(_idSearchText, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(_nameSearchText) && !mission.Value.Name.Contains(_nameSearchText, StringComparison.OrdinalIgnoreCase))
                    continue;

                missionIds.Add(mission.Key);
            }



            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                            ImGuiTableFlags.Borders |
                            ImGuiTableFlags.Reorderable |         // Allow column reordering
                            ImGuiTableFlags.Hideable |             // Allow hiding columns via right-click
                            ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("Completion Window", 7, tableFlags))
            {
                ImGui.TableSetupColumn("职业");
                ImGui.TableSetupColumn("任务完成状态");
                ImGui.TableSetupColumn("启用");
                ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, -1);
                ImGui.TableSetupColumn("任务名称", ImGuiTableColumnFlags.WidthFixed , -1);
                ImGui.TableSetupColumn("手动");
                ImGui.TableSetupColumn("类别");

                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

                // Column 0: Enabled
                ImGui.TableSetColumnIndex(0);
                ImGui.TableHeader("职业"); // Class
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("任务对应的职业");
                    ImGui.EndTooltip();
                }

                // Column 1: Completion Status
                ImGui.TableSetColumnIndex(1);
                ImGui.TableHeader("✓");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("任务完成状态"); // Completion Status
                    ImGui.EndTooltip();
                }

                // Column 2: Enabled
                ImGui.TableSetColumnIndex(2);
                ImGui.TableHeader("启用");
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ImGui.OpenPopup("Enabled Options");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("启用任务以完成");
                    ImGui.EndTooltip();
                }
                if (ImGui.BeginPopup("Enabled Options"))
                {
                    if (ImGui.Button("全部启用"))
                    {
                        foreach (var mission in missionIds)
                        {
                            C.MissionConfig[mission].Enabled = true;
                            if (GetOnlyPreviousMissionsRecursive(mission).Count > 0)
                            {
                                foreach (var prevMission in GetOnlyPreviousMissionsRecursive(mission))
                                {
                                    var prevMissionConfig = C.MissionConfig[prevMission];
                                    prevMissionConfig.Enabled = true;
                                }
                            }
                        }
                        C.Save();
                    }

                    if (ImGui.Button("全部禁用"))
                    {
                        foreach (var mission in missionIds)
                        {
                            C.MissionConfig[mission].Enabled = false;
                        }
                        C.Save();
                    }

                    ImGui.EndPopup();
                }

                // Column 3: ID
                ImGui.TableSetColumnIndex(3);
                ImGui.SetNextItemWidth(25); // Use full column width
                if (ImGui.InputTextWithHint("##IDSearch", "ID", ref _idSearchText, 100))
                {

                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("按 任务ID 搜索"); // Search by Mission ID Number
                }

                // Column 4: Mission Name
                ImGui.TableSetColumnIndex(4);
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputTextWithHint("##NameSearch", "名称", ref _nameSearchText, 1000))
                {

                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("按 任务名称 搜索");
                    ImGui.EndTooltip();
                }

                // Column 5: Manual Mode
                ImGui.TableSetColumnIndex(5);
                ImGui.TableHeader("手动");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("快速切换手动模式启用/禁用"); // Quick way to toggle on/off manual mode
                    ImGui.EndTooltip();
                }

                // Column 6: Ranking
                ImGui.TableSetColumnIndex(6);
                ImGui.TableHeader("类别");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("任务的类别等级"); // Rank of the mission
                }

                foreach (var mission in CosmicHelper.SheetMissionDict)
                {
                    if (C.ShowCompletionOnlyJob && !mission.Value.Jobs.Contains(Player.JobId))
                        continue;

                    if (C.ShowSelectedJobOnly && !mission.Value.Jobs.Contains(C.SelectedJob))
                        continue;

                    if (!C.ShowSinusMissions && mission.Value.TerritoryId == 1237)
                    {
                        continue;
                    }

                    if (!C.ShowPhaennaMissions && mission.Value.TerritoryId == 1291)
                        continue;

                    if (C.ShowCompletion_MissingGold)
                    {
                        var managerPtr = WKSManager.Instance();
                        if (managerPtr == null) continue;

                        var manager = (WKSManagerCustom*)managerPtr;
                        var isGold = manager->IsMissionGolded(mission.Key);

                        if (isGold)
                            continue;
                    }

                    if (!string.IsNullOrEmpty(_idSearchText) && !mission.Key.ToString().Contains(_idSearchText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrEmpty(_nameSearchText) && !mission.Value.Name.Contains(_nameSearchText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ImGui.PushID($"{mission.Value.Name}_{mission.Key}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    foreach (var job in mission.Value.Jobs)
                    {
                        ISharedImmediateTexture? icon = CosmicHelper.JobIconDict[job];
                        Vector2 size = new Vector2(25, 25);
                        ImGui.Image(icon.GetWrapOrEmpty().Handle, size);
                        ImGui.SameLine();
                    }

                    ImGui.TableNextColumn();
                    CompletionStatus_Normal(mission.Key);
                    UpdateSelectedMission(mission.Key);

                    ImGui.TableNextColumn();
                    if (C.MissionConfig.TryGetValue(mission.Key, out var config))
                    {
                        bool enabled = config.Enabled;
                        if (ImGui.Checkbox($"##Enabled", ref enabled))
                        {
                            config.Enabled = enabled;
                            if (enabled)
                            {
                                if (GetOnlyPreviousMissionsRecursive(mission.Key).Count > 0)
                                {
                                    foreach (var prevMission in GetOnlyPreviousMissionsRecursive(mission.Key))
                                    {
                                        var prevMissionConfig = C.MissionConfig[prevMission];
                                        prevMissionConfig.Enabled = true;
                                    }
                                }
                            }
                            C.Save();
                        }
                        UpdateSelectedMission(mission.Key);
                    }

                    ImGui.TableNextColumn();
                    CenterTextInTableCell($"{mission.Key}");
                    UpdateSelectedMission(mission.Key);

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.Value.Name}");
                    UpdateSelectedMission(mission.Key);

                    ImGui.TableNextColumn();
                    bool manual = config.ManualMode;
                    if (ImGui.Checkbox($"##Manual", ref manual))
                    {
                        config.ManualMode = manual;
                        C.Save();
                    }
                    UpdateSelectedMission(mission.Key);

                    ImGui.TableNextColumn();
                    string rank = mission.Value.Rank switch
                    {
                        1 => "D",
                        2 => "C",
                        3 => "B",
                        4 => "A",
                        5 => "EX",
                        6 => "Ex+",
                        _ => "???"
                    };
                    CenterTextInTableCell(rank);  

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        #endregion

        #region Right Window

        private uint selectedMission = 0;

        private void RightWindow()
        {
            if (CosmicHelper.SheetMissionDict.TryGetValue(selectedMission, out var mission))
            {
                ImGui.Text("任务信息(详细)");

                WindowSpacer();

                if (ImGui.BeginTable("Detailed Mission Info", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Info");

                    // Row 1
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("ID:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{selectedMission}");

                    // Row 2
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("名称:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.Name}");

                    // Row 3
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"宇宙信用点:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.CosmoCredit}");

                    // Row 4
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"月球信用点:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.LunarCredit}");

                    // Row 5
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"职业技巧点:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.ClassScore}");

                    // Row 6
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"职业");

                    ImGui.TableNextColumn();
                    foreach (var job in mission.Jobs)
                    {
                        ISharedImmediateTexture? icon = CosmicHelper.JobIconDict[job];
                        Vector2 size = new Vector2(20, 20);
                        ImGui.Image(icon.GetWrapOrEmpty().Handle, size);
                        ImGui.SameLine();
                    }

                    // Row 7
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"研究数据奖励"); // Relic XP Amounts

                    foreach (var xp in mission.RelicXpInfo.OrderBy(x => x.Key))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
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
                        ImGui.TableNextColumn();
                        ImGui.Text($"{xp.Value}");
                    }

                    // Row 8
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"完成情况: "); // Completed:

                    ImGui.TableNextColumn();
                    CompletionStatus_Normal(selectedMission);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"铜星需求: ");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.BronzeScore}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"银星需求: ");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.SilverScore}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("金星需求: ");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{mission.GoldScore}");

                    ImGui.EndTable();
                }

                WindowSpacer();

                ImGui.Text("任务属性"); // Mission Atributes
                if (mission.Attributes == MissionAttributes.None)
                {
                    ImGui.Text("无");
                    return;
                }
                else
                {
                    foreach (MissionAttributes flag in Enum.GetValues<MissionAttributes>())
                    {
                        if (flag != MissionAttributes.None && mission.Attributes.HasFlag(flag))
                        {
                            ImGui.Text(flag.ToString());
                        }
                    }
                }

                if (CosmicHelper.MissionUnlock.TryGetValue(selectedMission, out var unlock))
                {
                    ImGui.Text("需要以下任务达到金星评价后, 才能进行此任务"); // The following missions are required to have gold before you can do this one
                    foreach (var lockedMission in unlock)
                    {
                        CompletionStatus_Normal(lockedMission);
                        ImGui.SameLine();
                        ImGui.Text($"[{lockedMission}] - {CosmicHelper.SheetMissionDict[lockedMission].Name}");
                    }

                }

                WindowSpacer();
                ImGui.Text($"任务时间!");

                if (C.MissionConfig.TryGetValue(selectedMission, out var config))
                {
                    bool allowDelete = (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)) && (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl));

                    using (ImRaii.Disabled(!allowDelete))
                    {
                        if (ImGui.Button("重置统计"))
                        {
                            P.MissionTimer.ResetTimers(selectedMission);
                        }
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("按住 Shift + Ctrl");
                        ImGui.EndTooltip();
                    }

                    if (config.TurninRecords.Count > 0)
                    {
                        ImGui.Text($"最佳时间: {TimeSpan.FromSeconds(config.BestTime):mm\\:ss\\.ff}");
                        ImGui.Text($"平均时间: {TimeSpan.FromSeconds(config.AverageTime):mm\\:ss\\.ff}");
                    }
                    else
                    {
                        ImGui.Text("最佳时间: --:--:--");
                        ImGui.Text("平均时间: --:--:--");
                    }

                    ImGui.Text($"完成次数: {config.TotalCompletions}");
                    ImGui.Text($"超时放弃次数: {config.FailedCounters}");

                    if (CosmicHelper.SheetMissionDict.TryGetValue(selectedMission, out var missionInfo))
                    {
                        var baseScore = missionInfo.ClassScore; // Adjust this based on your actual property name

                        ImGui.Separator();
                        ImGui.Text("预估每小时技巧点:");
                        ImGui.SameLine();
                        ImGui.TextDisabled("?");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("这里的假设前提是:");
                            ImGui.Text("1: 你每次都能以完美的随机数运气拿到想要的任务");
                            ImGui.Text("2: 你每次都能达到评价阈值");
                            ImGui.Text("这些计算基于你的平均用时。 \n所以最好多跑几轮任务来得到更准确的时间。");
                            ImGui.EndTooltip();
                        }

                        if (mission.Attributes.HasFlag(MissionAttributes.Critical))
                        {
                            var criticalScore = MissionStatsCalculator.CalculateScorePerHour(config.AverageTime, baseScore, 1.0);
                            ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), $"紧急探索任务: {criticalScore:F0} 技巧点/小时");
                        }
                        else
                        {
                            var actualScorePerMinute = MissionStatsCalculator.CalculateActualScorePerHour(config.TurninRecords, baseScore);

                            ImGui.Text($"实际每小时技巧点: {actualScorePerMinute:F2}");
                            ImGui.SameLine();
                            ImGui.TextDisabled("?");
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("此数值基于你当前的铜星/银星/金星完成率计算");
                                ImGui.Text("它会计算你在所有任务中获得的平均技巧点，并假设你在一小时内保持这个水平，从而估算这一任务每小时获得的技巧点");
                                ImGui.Text("这是一种基于你的完成率，用于计算更精确平均值的技术性方法。");
                                ImGui.EndTooltip();
                            }

                            var bronzePerHour = MissionStatsCalculator.CalculateScorePerHour(config.AverageTime, baseScore, 1.0);
                            var silverPerHour = MissionStatsCalculator.CalculateScorePerHour(config.AverageTime, baseScore, 4.0);
                            var goldPerHour = MissionStatsCalculator.CalculateScorePerHour(config.AverageTime, baseScore, 5.0);

                            ImGui.TextColored(new Vector4(0.8f, 0.5f, 0.3f, 1.0f), $"铜星: {bronzePerHour:F0} 技巧点/小时 [{config.BronzeCompletion}/{config.TotalCompletions}]");
                            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"银星: {silverPerHour:F0} 技巧点/小时 [{config.SilverCompletions}/{config.TotalCompletions}]");
                            ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), $"金星: {goldPerHour:F0} 技巧点/小时 [{config.GoldCompletions}/{config.TotalCompletions}]");
                        }
                    }


                    if (config.TurninRecords.Count > 0 && ImGui.CollapsingHeader("查看所有完成时间"))
                    {
                        for (int i = 0; i < config.TurninRecords.Count; i++)
                        {
                            var record = config.TurninRecords[i];

                            ImGui.Text($"[{i+1}] \u2192 {TimeSpan.FromSeconds(record.Time):mm\\:ss\\.ff}");
                            ImGui.SameLine();
                            DrawColoredStar(record.State);

                        }
                    }
                }
            }
            else
            {
                ImGui.TextWrapped("海盗最喜欢的字母是什么?\n" +
                                  "你可能会觉得是 \"Rrrrr\", 但他真正的初恋其实是 \"C(Sea)\"。\n" +
                                  "(如果没反应过来, 可以试着读出来 lol)");
            }
        }

        #endregion

        #region Table Tools
        private void WindowSpacer()
        {
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 5));
        }
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
        public static List<uint> GetOnlyPreviousMissionsRecursive(uint missionId)
        {
            if (!CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var missionInfo) || missionInfo.PreviousMissions.Contains(0))
                return [];

            var chain = GetOnlyPreviousMissionsRecursive(missionInfo.PreviousMissions.First());
            chain.Add(missionInfo.PreviousMissions.First());
            return chain;
        }
        private List<uint> GetOnlyNextMissionsRecursive(uint missionId)
        {
            uint? nextMissionId = CosmicHelper.SheetMissionDict
                .Where(m => m.Value.PreviousMissions.First() == missionId)
                .Select(m => (uint?)m.Key)
                .FirstOrDefault();

            if (!nextMissionId.HasValue)
                return [];

            var chain = new List<uint> { nextMissionId.Value };
            chain.AddRange(GetOnlyNextMissionsRecursive(nextMissionId.Value));
            return chain;
        }
        private static unsafe void CompletionStatus_Formatted(uint id)
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return;

            var manager = (WKSManagerCustom*)managerPtr;
            var isCompleted = manager->IsMissionCompleted(id);
            var isGold = manager->IsMissionGolded(id);

            float availableWidth = ImGui.GetContentRegionAvail().X;

            if (isCompleted)
            {
                if (isGold)
                {
                    // Center the image
                    float imageWidth = 23f;
                    float offsetX = (availableWidth - imageWidth) * 0.5f;

                    if (offsetX > 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

                    if (Svc.Texture.GetFromGame("ui/uld/WKSMission_hr1.tex") is { } tex)
                    {
                        if (tex.TryGetWrap(out var wrap, out var exc))
                        {
                            ImGui.Image(wrap.Handle, new Vector2(23, 23), new Vector2(0.2347f, 0.3500f), new Vector2(0.2959f, 0.6500f));
                        }
                    }
                }
                else
                {
                    // Center the font icon - you'll need to measure or estimate its width
                    var iconText = FontAwesome.Check.ToString();
                    var iconWidth = ImGui.CalcTextSize(iconText).X;
                    float offsetX = (availableWidth - iconWidth) * 0.5f;

                    if (offsetX > 0)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

                    FontAwesome.Print(EColor.Green, FontAwesome.Check);
                }
            }
            else
            {
                // Center the cross icon
                var iconText = FontAwesome.Cross.ToString();
                var iconWidth = ImGui.CalcTextSize(iconText).X;
                float offsetX = (availableWidth - iconWidth) * 0.5f;

                if (offsetX > 0)
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

                FontAwesome.Print(EColor.Red, FontAwesome.Cross);
            }
        }

        private static unsafe void CompletionStatus_Normal(uint id)
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return;

            var manager = (WKSManagerCustom*)managerPtr;
            var isCompleted = manager->IsMissionCompleted(id);
            var isGold = manager->IsMissionGolded(id);

            var containerSize = new Vector2(23, 23);

            // Create a consistent container for all elements
            var cursorPos = ImGui.GetCursorPos();
            ImGui.InvisibleButton("##status_container", containerSize);
            ImGui.SetCursorPos(cursorPos);

            if (isCompleted)
            {
                if (isGold)
                {
                    if (Svc.Texture.GetFromGame("ui/uld/WKSMission_hr1.tex") is { } tex)
                    {
                        if (tex.TryGetWrap(out var wrap, out var exc))
                        {
                            ImGui.Image(wrap.Handle, containerSize, new Vector2(0.2347f, 0.3500f), new Vector2(0.2959f, 0.6500f));
                        }
                    }
                }
                else
                {
                    // Center the FontAwesome icon within the container
                    var textSize = ImGui.CalcTextSize(FontAwesome.Check);
                    var offset = (containerSize - textSize) * 0.5f;
                    offset += new Vector2(-2f, 1f);
                    ImGui.SetCursorPos(cursorPos + offset);
                    FontAwesome.Print(EColor.Green, FontAwesome.Check);
                }
            }
            else
            {
                var textSize = ImGui.CalcTextSize(FontAwesome.Cross);
                var offset = (containerSize - textSize) * 0.5f;
                offset += new Vector2(-2f, 1f);
                ImGui.SetCursorPos(cursorPos + offset);
                FontAwesome.Print(EColor.Red, FontAwesome.Cross);
            }

            // Reset cursor to after the container
            ImGui.SetCursorPos(cursorPos + new Vector2(containerSize.X, 0));
        }

        private void UpdateSelectedMission(uint missionId)
        {
            if (ImGui.IsItemClicked())
            {
                selectedMission = missionId;
            }
        }

        private void DrawColoredStar(TurninState state)
        {
            Vector4 color = state switch
            {
                TurninState.Bronze => new Vector4(0.8f, 0.5f, 0.3f, 1.0f),  // Bronze
                TurninState.Silver => new Vector4(0.75f, 0.75f, 0.75f, 1.0f), // Silver
                TurninState.Gold => new Vector4(1.0f, 0.84f, 0.0f, 1.0f),    // Gold
                _ => new Vector4(0, 0, 0, 0) // Transparent/none
            };

            if (state != TurninState.None)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.PushFont(UiBuilder.IconFont); // Make sure you're using the icon font
                ImGui.Text(FontAwesomeIcon.Star.ToIconString());
                ImGui.PopFont();
                ImGui.PopStyleColor();
            }
        }

        #endregion
    }
}
