using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;

namespace ICE.Ui;

// This isn't currently wired up to anything. Can actually use this to place all the general settings for all the windows...
internal class SettingsWindow : Window
{
    public SettingsWindow() :
        base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICESettingsWindow")
    {
        Flags = ImGuiWindowFlags.None;
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(2000, 2000),
        };
        P.windowSystem.AddWindow(this);
        AllowPinning = true;
    }

    public void Dispose()
    {
        P.windowSystem.RemoveWindow(this);
    }


    public override void Draw()
    {
        Kofi.DrawRight();
        ImGuiEx.EzTabBar("Ice Cosmic Settings Tab", Kofi.Text 
            ,("安全设置", SafetySettings, null, true) // Safety Settings
            ,("采集配置", GatherSettings, null, true) // Gathering Config
            ,("悬浮窗", Overlay, null, true) // Overlay
            ,("杂项", Misc, null, true) // Misc
            ,("宇宙好运道设置", GambaWheel, null, true) // Gamble Wheel Settings
#if DEBUG
            ,("Debug", Debug, null, true)
#endif
        );
    }

    private bool animationLockAbandon = C.AnimationLockAbandon;
    private bool stopOnAbort = C.StopOnAbort;
    private bool rejectUnknownYesNo = C.RejectUnknownYesno;
    private bool delayGrabMission = C.DelayGrabMission;
    private int delayAmount = C.DelayIncrease;
    private bool delayCraft = C.DelayCraft;
    private int delayCraftAmount = C.DelayCraftIncrease;

    private void SafetySettings()
    {
        if (ImGui.Checkbox("[实验性] 解除动画锁", ref animationLockAbandon)) // [Experimental] Animation Lock Unstuck
        {
            C.AnimationLockAbandon = animationLockAbandon;
            C.Save();
        }
        ImGui.Checkbox("[实验性] 手动解除动画锁", ref SchedulerMain.AnimationLockAbandonState); // [Experimental] Animation Lock Manual Unstuck

        if (ImGui.Checkbox("发生错误时停止", ref stopOnAbort)) // Stop on Errors
        {
            C.StopOnAbort = stopOnAbort;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "警告！此安全功能将在出现异常时强制停止运行！\n" + // Warning! This is a safety feature to stop if something goes wrong!
            "您已获知风险，禁用后果自负。" // You have been warned. Disable at your own risk.
        );

        if (ImGui.Checkbox("忽略非宇宙探索相关提示", ref rejectUnknownYesNo)) // Ignore non-Cosmic prompts
        {
            C.RejectUnknownYesno = rejectUnknownYesNo;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "警告！此安全功能用于防止误入随机小队！\n" + // Warning! This is a safety feature to avoid joining random parties!
            "若取消勾选，您将自动接受随机小队的邀请。\n" + // If you you uncheck this, YOU WILL JOIN random party invites.
            "您已获知风险，禁用后果自负。" // You have been warned. Disable at your own risk.
        );
        if (ImGui.Checkbox("任务菜单添加延迟", ref delayGrabMission)) // Add delay to mission menu
        {
            C.DelayGrabMission = delayGrabMission;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "此为安全保护机制！若想缩短任务间隔时间，请随意调整。\n" + // This is here for safety! If you want to decrease the delay between missions be my guest.
            "安全值大概在... 250? 若遇到动画锁卡顿，完全可以调得更高。\n" + // Safety is around... 250? If you're having animation locks you can absolutely increase it higher
            "当然，想追求刺激的话...调低也行。反正我不是你老爸（不过老爸笑话管够）。"); // Or if you're feeling daredevil. Lower it. I'm not your dad (will tell dad jokes though.
        if (delayGrabMission)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            if (ImGui.SliderInt("ms###Mission", ref delayAmount, 0, 1000))
            {
                if (C.DelayIncrease != delayAmount)
                {
                    C.DelayIncrease = delayAmount;
                    C.Save();
                }
            }
        }
        if (ImGui.Checkbox("制作菜单添加延迟", ref delayCraft)) // Add delay to crafting menu
        {
            C.DelayCraft = delayCraft;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "此为安全保护机制！若想缩短汇报延迟时间，请随意调整。\n" + // This is here for safety! If you want to decrease the delay before turnin be my guest.
            "安全值大概在... 2500？若遇到动画锁卡顿，完全可以调得更高。\n" + // Safety is around... 2500? If you're having animation locks you can absolutely increase it higher
            "当然，想追求刺激的话...调低也行。反正我不是你老爸（不过老爸笑话管够）。"); // Or if you're feeling daredevil. Lower it. I'm not your dad (will tell dad jokes though.
        if (delayCraft)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            if (ImGui.SliderInt("ms###Crafting", ref delayCraftAmount, 0, 10000))
            {
                if (C.DelayCraftIncrease != delayCraftAmount)
                {
                    C.DelayCraftIncrease = delayCraftAmount;
                    C.Save();
                }
            }
        }
    }

    private bool SelfRepairGather = C.SelfRepairGather;
    private float SelfRepairPercent = C.RepairPercent;
    private bool SelfSpiritbondGather = C.SelfSpiritbondGather;
    private bool AutoCordial = C.AutoCordial;
    private bool InverseCordialPrio = C.inverseCordialPrio;
    private bool UseOnFisher = C.UseOnFisher;
    private bool PreventOvercap = C.PreventOvercap;
    private int CordialMinGp = C.CordialMinGp;
    private bool useOnlyInMission = C.UseOnlyInMission;
    private string newProfileName = "";

    private string[] MissionTypes = ["限量采集点", "采集 X 个物品", "时间竞速", "连锁冲分", "恩惠冲分", "连锁 + 恩惠冲分", "双职业"]; // ["Limited Nodes", "Gather x Amount", "Time Attack", "Chained Scoring", "Boon Scoring", "Chain + Boon Scoring", "Dual Class"]; 恩惠 = 采集暴击(或者馈赠技能)
    private int MissionIndex = 0;

    private void GatherSettings()
    {
        void DrawBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange, int currentMaxUse, Action<int> onMaxUseChange)
        {
            bool enabled = currentEnabled;
            if (ImGui.Checkbox($"{label}###Enable{uniqueId}", ref enabled))
            {
                if (enabled != currentEnabled)
                    onEnabledChange(enabled);
            }
            ImGuiEx.HelpMarker(ActionInfo);

            if (enabled)
            {
                ImGui.Indent(15);

                if (ImGui.TreeNode($"{label} 设置###Tree{uniqueId}{entryName}")) // Settings
                {
                    int minGp = currentMinGp;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("最低 GP"); // Minimum GP
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                    {
                        if (minGp != currentMinGp)
                            onMinGpChange(minGp);
                    }
                    int maxUse = currentMaxUse;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("最大使用次数"); // Maximum use count
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                    {
                        if (maxUse != currentMaxUse)
                            onMaxUseChange(maxUse);
                    }
                    ImGuiEx.HelpMarker("设置为 -1 时，允许无限使用\n" + // Set to -1 to allow for infinite uses 
                                       "设置为 1 -> X 时，设定为每次任务的最大使用次数上限"); // Set to 1-> X to set maximum amount of uses per mission

                    ImGui.TreePop();
                }
                ImGui.Unindent(15);
            }
        }

        void DrawCustomBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange, int currentMaxUse, Action<int> onMaxUseChange, int MinItemUsage, Action<int> onMinItemMaxUseChange)
        {
            bool enabled = currentEnabled;
            if (ImGui.Checkbox($"{label}###Enable{uniqueId}", ref enabled))
            {
                if (enabled != currentEnabled)
                    onEnabledChange(enabled);
            }
            ImGuiEx.HelpMarker(ActionInfo);

            if (enabled)
            {
                ImGui.Indent(15);

                if (ImGui.TreeNode($"{label} 设置###Tree{uniqueId}{entryName}")) // Settings
                {
                    int minGp = currentMinGp;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("最低 GP"); // Minimum GP
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                    {
                        if (minGp != currentMinGp)
                            onMinGpChange(minGp);
                    }
                    int maxUse = currentMaxUse;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("最大使用次数"); // Maximum use count
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                    {
                        if (maxUse != currentMaxUse)
                            onMaxUseChange(maxUse);
                    }
                    ImGuiEx.HelpMarker("设置为 -1 时，允许无限使用\n" + // Set to -1 to allow for infinite uses 
                                       "设置为 1 -> X 时，设定为每次任务的最大使用次数上限"); // Set to 1-> X to set maximum amount of uses per mission

                    int MinItem = MinItemUsage;
                    ImGui.Text($"最低高产使用所需数量"); // Minimum BYII Item
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.SliderInt($"###MinItemsBYII{uniqueId}{entryName}_1", ref MinItem, 2, 4))
                    {
                        if (MinItem != MinItemUsage)
                            onMinItemMaxUseChange(MinItem);
                    }
                    ImGuiEx.HelpMarker($"设置使用高产所需的最低物品数量\n" + // Set the minimum amount of items that you want BYII to activate on
                                       $"示例：设为 2 时，仅在需要收集 2 个或以上物品时才会使用\n" + // Ex. Setting it to 2 will make it to where if you only activate if you need need 2 or more items
                                       $"适用场景：节省GP（采集力），适用于\"收集 X 个物品\"或 双职业任务"); // Useful if you're trying to save gp on gather x amount or dual class missions

                    ImGui.TreePop();
                }
                ImGui.Unindent(15);
            }
        }

        int maxGp = 1200;

        if (ImGui.Checkbox("采集时自动修理", ref SelfRepairGather)) // Self Repair on Gather
        {
            if (C.SelfRepairGather != SelfRepairGather)
            {
                C.SelfRepairGather = SelfRepairGather;
                C.Save();
            }
        }
        if (SelfRepairGather)
        {
            ImGui.Indent(15);
            ImGui.Text("修理阈值"); // Repair at
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("###Repair %", ref SelfRepairPercent, 0f, 99f, "%.0f%%"))
            {
                if (C.RepairPercent != SelfRepairPercent)
                {
                    C.RepairPercent = (int)SelfRepairPercent;
                    C.Save();
                }
            }
            ImGui.Unindent(15);
        }
        if (ImGui.Checkbox("采集时精制魔晶石", ref SelfSpiritbondGather)) // Extract Spiritbond on Gather
        {
            if (C.SelfSpiritbondGather != SelfSpiritbondGather)
            {
                C.SelfSpiritbondGather = SelfSpiritbondGather;
                C.Save();
            }
        }
        if (ImGui.Checkbox("自动强心剂", ref AutoCordial)) // Auto Cordial
        {
            C.AutoCordial = AutoCordial;
            C.Save();
        }
        ImGuiEx.HelpMarker("仅在 ICE 插件运行时生效，手动模式无效\n" + // Will only work while using ICE and not manual mode
                           "在月球探索地图中将暂停 Pandora 插件的自动强心剂功能"); // Will also pause pandora cordial usage while on the moon
        if (AutoCordial)
        {
            if (ImGui.TreeNode("强心剂设置")) // Cordial Settings
            {
                if (ImGui.Checkbox("反转优先级 (轻型 -> 普通 -> 高级)", ref InverseCordialPrio)) // Inverse Priority (Watered -> Regular -> Hi)
                {
                    C.inverseCordialPrio = InverseCordialPrio;
                    C.Save();
                }
                if (ImGui.Checkbox("防止溢出", ref PreventOvercap)) // Prevent Overcap
                {
                    C.PreventOvercap = PreventOvercap;
                    C.Save();
                }
                if (ImGui.Checkbox("应用于捕鱼人", ref UseOnFisher)) // Use on Fisher
                {
                    C.UseOnFisher = UseOnFisher;
                    C.Save();
                }
                if (ImGui.Checkbox("仅任务中使用", ref useOnlyInMission)) // Only use in mission
                {
                    C.UseOnlyInMission = useOnlyInMission;
                    C.Save();
                }
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("GP 阈值", ref CordialMinGp, 0, maxGp)) // Gp Threshold
                {
                    C.CordialMinGp = CordialMinGp;
                    C.Save();
                }

                ImGui.TreePop();
            }
        }

        ImGui.Dummy(new(0, 5));

        ImGui.SetNextItemWidth(200);
        ImGui.InputText("新配置名称", ref newProfileName, 64); // New Profile Name
        using (ImRaii.Disabled(newProfileName == ""))
        {
            if (ImGui.Button("添加配置") && !string.IsNullOrWhiteSpace(newProfileName)) // Add Profile
            {
                if (!C.GatherSettings.Any(x => x.Name == newProfileName))
                {
                    int newId = C.GatherSettings.Max(x => x.Id) + 1;
                    C.GatherSettings.Add(new GatherBuffProfile { Id = newId, Name = newProfileName });
                    C.Save();
                    newProfileName = ""; // Reset input
                }
            }
        }

        ImGui.Columns(2, "Gather Settings Columns", false);

        // ------------------ 
        //  Left Column, Profile Settings
        // ------------------
        ImGui.SetColumnWidth(0, 350);

        ImGui.Text("采集配置"); // Gather Profiles

        bool canDelete = C.GatherSettings.Count > 1 && C.SelectedGatherIndex != 0;
        using (ImRaii.Disabled(!canDelete))
        {
            if (ImGui.Button("删除选中的配置")) // Delete Selected Profile
            {
                var deletedProfile = C.GatherSettings[C.SelectedGatherIndex];
                int deletedId = deletedProfile.Id;

                // Remove the profile
                C.GatherSettings.RemoveAt(C.SelectedGatherIndex);

                // Update all missions using this GatherSettingId
                foreach (var mission in C.Missions)
                {
                    if (mission.GatherSettingId == deletedId)
                    {
                        mission.GatherSettingId = C.GatherSettings[0].Id; // fallback to default
                    }
                }

                // Clamp the selected index and save
                C.SelectedGatherIndex = Math.Clamp(C.SelectedGatherIndex, 0, C.GatherSettings.Count - 1);
                C.Save();
            }
        }

        ImGui.BeginChild("GatherProfileChild", new Vector2(300, ImGui.GetTextLineHeightWithSpacing() * 5 + 10), true);
        for (int i = 0; i < C.GatherSettings.Count; i++)
        {
            bool isSelected = (i == C.SelectedGatherIndex);

            if (ImGui.Selectable(C.GatherSettings[i].Name, isSelected))
            {
                C.SelectedGatherIndex = i;
                C.Save();
            }

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }
        ImGui.EndChild();

        GatherBuffProfile entry = C.GatherSettings[C.SelectedGatherIndex];

        ImGui.Combo("任务类型", ref MissionIndex, MissionTypes, MissionTypes.Length); // Mission Type
        if (ImGui.Button("应用到任务类型")) // Apply to Mission Types
        {
            foreach (var mission in C.Missions)
            {
                var id = mission.Id;

                var missionDict = CosmicHelper.MissionInfoDict[id];

                bool craftMission = missionDict.Attributes.HasFlag(MissionAttributes.Craft);
                bool gatherMission = missionDict.Attributes.HasFlag(MissionAttributes.Gather);

                bool LimitedQuant = missionDict.Attributes.HasFlag(MissionAttributes.Limited);
                // Gather X Amount is just "Gather" 
                bool TimedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);
                bool ChainedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreChains);
                bool BoonMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreGatherersBoon);
                bool collectableMission = missionDict.Attributes.HasFlag(MissionAttributes.Collectables);
                bool stellerReductionMission = missionDict.Attributes.HasFlag(MissionAttributes.ReducedItems);

                bool GatherX = !stellerReductionMission && !collectableMission && !BoonMission && !ChainedMission && !TimedMission && !LimitedQuant;

                void UpdateMissions()
                {
                    mission.GatherSettingId = entry.Id;
                }

                if (gatherMission && (!collectableMission && !stellerReductionMission))
                {
                    if (MissionIndex == 0 && LimitedQuant)
                        UpdateMissions();
                    else if (MissionIndex == 2 && TimedMission)
                        UpdateMissions();
                    else if (MissionIndex == 3 && ChainedMission && !BoonMission)
                        UpdateMissions();
                    else if (MissionIndex == 4 && BoonMission && !ChainedMission)
                        UpdateMissions();
                    else if (MissionIndex == 5 && ChainedMission && BoonMission)
                        UpdateMissions();
                    else if (MissionIndex == 6 && craftMission)
                        UpdateMissions();
                    else if (MissionIndex == 1 && GatherX)
                        UpdateMissions();
                }
            }

            C.Save();
        }

        // ---------------------------------
        // Right Column, Gathering setttings
        // ---------------------------------

        ImGui.NextColumn();
        ImGui.SetColumnWidth(1, ImGui.GetWindowWidth() - 300);

        // Pathfinding
        int pathfinding = entry.Pathfinding;
        string[] modes = ["简单", "最近", "循环"]; // ["Simple", "Nearest", "Cyclic"]
        ImGui.SetNextItemWidth(100);
        if (ImGui.Combo("寻路模式", ref pathfinding, modes, modes.Length)) // Pathfinding mode
        {
            entry.Pathfinding = pathfinding;
            C.Save();
        }
        ImGuiEx.HelpMarker("简单 - 从列表中的第一个节点开始，依次经过直至最后一个节点。\n最近 - 始终前往最近的节点，然后寻找一条经过所有剩余节点的最短路径。\n循环 - 寻找位置相近的节点群，并仅在这些节点之间循环移动。"); // Simple - From 1st node in list until the last.\nNearest - Always go to Nearest node then find a path that minimises distance through all remaining nodes.\nCyclic - Find nodes that are close together and stick to those nodes only.
        if (pathfinding == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            int cycle = entry.TSPCycleSize;
            if (ImGui.InputInt("循环节点数", ref cycle, 1)) // Cycle size
            {
                entry.TSPCycleSize = cycle >= 2 ? cycle : 2;
                C.Save();
            }
        }

        // GP Settings
        int minGP = entry.MinimumGP;
        ImGui.SetNextItemWidth(100);
        if (ImGui.SliderInt("开始任务所需最低 GP", ref minGP, -1, maxGp)) // Minimum GP to start mission
        {
            entry.MinimumGP = minGP;
            C.Save();
        }

        // Multiply gathered items on FIRST gather loop only. Should only be used for Dual Class really.
        int gatherMult = entry.InitialGatheringItemMultiplier;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("双职业任务制作数量", ref gatherMult, 1)) // Dual Class Craft Amount
        {
            entry.InitialGatheringItemMultiplier = gatherMult >= 1 ? gatherMult : 1;
            C.Save();
        }
        ImGuiEx.HelpMarker("此选项将增加您在切换到制作流程前需要收集的物品数量(即达到\"完成\"状态)。\n根据您需要制作多少物品才能达到目标技巧点来调整此数值。\n只影响双职业任务。"); // This increases how many items you gather before you are 'done' before switching to crafting.\nSet this to however many items you need to craft to reach your target score.\nOnly affects Dual Class missions.

        // Boon Increase 2 (+30% Increase)
        DrawBuffSetting(
            label: "沃土 / 富矿的馈赠 II",
            uniqueId: $"Boon2Inc{entry.Id}",
            currentEnabled: entry.Buffs.BoonIncrease2,
            currentMinGp: entry.Buffs.BoonIncrease2Gp,
            minGpLimit: 100,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "额外采集奖励发生率提升30%", // Apply a 30% buff to your boon chance.
            onEnabledChange: newVal =>
            {
                entry.Buffs.BoonIncrease2 = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BoonIncrease2Gp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BoonIncrease2MaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BoonIncrease2MaxUse = newVal;
                C.Save();
            }
        );

        // Boon Increase 1 (+10% Increase)
        DrawBuffSetting(
            label: "沃土 / 富矿的馈赠 I",
            uniqueId: $"Boon1Inc{entry.Id}",
            currentEnabled: entry.Buffs.BoonIncrease1,
            currentMinGp: entry.Buffs.BoonIncrease1Gp,
            minGpLimit: 50,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "额外采集奖励发生率提升10%", // Apply a 10% buff to your boon chance.
            onEnabledChange: newVal =>
            {
                entry.Buffs.BoonIncrease1 = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BoonIncrease1Gp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BoonIncrease1MaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BoonIncrease1MaxUse = newVal;
                C.Save();
            }
        );

        // Tidings (+2 to boon instead of +1)
        DrawBuffSetting(
            label: "诺菲卡 / 纳尔札尔 福音", // Nophica's / Nald'thal's Tidings Buff
            uniqueId: $"TidingsBuff{entry.Id}",
            currentEnabled: entry.Buffs.TidingsBool,
            currentMinGp: entry.Buffs.TidingsGp,
            minGpLimit: 200,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "额外采集奖励发生时的获得数增加1个", // Increases item yield from Gatherer's Boon by 1
            onEnabledChange: newVal =>
            {
                entry.Buffs.TidingsBool = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.TidingsGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.TidingsMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.TidingsMaxUse = newVal;
                C.Save();
            }
        );

        // Yield II (+2 to all items on node)
        DrawBuffSetting(
            label: "天赐收成 / 莫非王土 II",
            uniqueId: $"Blessed/KingsYieldIIBuff{entry.Id}",
            currentEnabled: entry.Buffs.YieldII,
            currentMinGp: entry.Buffs.YieldIIGp,
            minGpLimit: 500,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "令获得数增加2个\n" + // Increases the number of items obtained when gathering by 2
                        "只在采集点满耐久时使用", // Will only apply when the gathering node has full durability
            onEnabledChange: newVal =>
            {
                entry.Buffs.YieldII = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.YieldIIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.YieldIIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.YieldIIMaxUse = newVal;
                C.Save();
            }
        );

        // Yield I (+1 to all items on node)
        DrawBuffSetting(
            label: "天赐收成 / 莫非王土 I", // Blessed / Kings Yield I
            uniqueId: $"Blessed/KingsYieldIBuff{entry.Id}",
            currentEnabled: entry.Buffs.YieldI,
            currentMinGp: entry.Buffs.YieldIGp,
            minGpLimit: 400,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "令获得数增加1个\n" + // Increases the number of items obtained when gathering by 1
                        "只在采集点满耐久时使用", // Will only apply when the gathering node has full durability
            onEnabledChange: newVal =>
            {
                entry.Buffs.YieldI = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.YieldIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.YieldIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.YieldIMaxUse = newVal;
                C.Save();
            }
        );

        // Bonus Integrity (+1 integrity)
        DrawBuffSetting(
            label: "农夫之智 / 石工之理", // Ageless Words / Solid Reason
            uniqueId: $"Incrase Intregity{entry.Id}",
            currentEnabled: entry.Buffs.BonusIntegrity,
            currentMinGp: entry.Buffs.BonusIntegrityGp,
            minGpLimit: 300,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "恢复1次采集次数\n" + // Increase the Integrity by 1   
                        "50%几率附加理智同兴预备状态", // 50% chance to grant Eureka Moment
            onEnabledChange: newVal =>
            {
                entry.Buffs.BonusIntegrity = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BonusIntegrityGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BonusIntegrityMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BonusIntegrityMaxUse = newVal;
                C.Save();
            }
        );

        // Bountiful Yield/Harvest II (+Amount based on gathering)
        DrawCustomBuffSetting(
            label: "高产 II / 丰收 II", // Bountiful Yield II / Bountiful Harvest II
            uniqueId: $"Bountiful Yield II {entry.Id}",
            currentEnabled: entry.Buffs.BountifulYieldII,
            currentMinGp: entry.Buffs.BountifulYieldIIGp,
            minGpLimit: 100,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "令下一次采集的获得数增加\n" + // Increase item's gained on next gathering attempt by 1, 2, or 3 
                        "获得力影响获得数的增加量（最小1～最大3）", // This is based on your gathering rating
            onEnabledChange: newVal =>
            {
                entry.Buffs.BountifulYieldII = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BountifulYieldIIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BountifulYieldIIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BountifulYieldIIMaxUse = newVal;
                C.Save();
            },
            entry.Buffs.BountifulMinItem,
            onMinItemMaxUseChange: newVal =>
            {
                entry.Buffs.BountifulMinItem = newVal;
                C.Save();
            }
        );

        ImGui.Columns(1);
    }

    private bool gambaEnabled = C.GambaEnabled;
    private int gambaDelay = C.GambaDelay;
    private int gambaCreditsMinimum = C.GambaCreditsMinimum;
    private bool gambaPreferSmallerWheel = C.GambaPreferSmallerWheel;

    private void GambaWheel()
    {
        if (ImGui.Checkbox("启用 宇宙好运道", ref gambaEnabled)) // Enable Gamba
        {
            C.GambaEnabled = gambaEnabled;
            C.Save();
        }
        ImGuiEx.HelpMarker("运行此功能前，请确保已在 环行威 显示宇宙好运道界面窗口，然后按下开始，之后将会自动运行。"); // To run this, make sure you have the gamble wheels shown at Orbitingway, and press start. It will full auto from there.
        if (gambaEnabled)
        {
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("宇宙好运道 延迟", ref gambaDelay, 50, 2000)) // Gamba Delay
            {
                C.GambaDelay = gambaDelay;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("保留最低信用点数量", ref gambaCreditsMinimum, 0, 10000)) // Mininum credits to keep
            {
                C.GambaCreditsMinimum = gambaCreditsMinimum;
                C.Save();
            }
        }
        if (ImGui.Checkbox("优先更小的转盘", ref gambaPreferSmallerWheel)) // Prefer smaller wheel
        {
            C.GambaPreferSmallerWheel = gambaPreferSmallerWheel;
            C.Save();
        }
        ImGuiEx.HelpMarker("此选项将优先选择物品数量更少的转盘"); // This will make the Gamba prefer wheels with less items.
        ImGui.Separator();
        ImGui.TextUnformatted("配置宇宙好运道每个物品的权重，物品权重越高 = 越优先获取该物品"); // Configure the weights for each item in the Gamba. Higher weight = more desirable.
        ImGui.Spacing();
        foreach (GambaType type in Enum.GetValues(typeof(GambaType)))
        {
            var itemsType = C.GambaItemWeights.Where(x => x.Type == type).OrderBy(x => x.ItemId).ToList();
            if (itemsType.Count == 0) continue;
            if (ImGui.TreeNodeEx($"{type} ({itemsType.Count})##gamba_type_{type}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                foreach (var gamba in itemsType)
                {
                    var itemName = ExcelItemHelper.GetName(gamba.ItemId);
                    int weight = gamba.Weight;
                    ImGui.SetNextItemWidth(120f);
                    if (ImGui.InputInt($"[{gamba.ItemId}] {itemName}##gamba_weight", ref weight))
                    {
                        gamba.Weight = weight;
                        C.Save();
                    }
                }
                ImGui.Unindent();
                ImGui.TreePop();
            }
        }
        if (ImGui.Button("重置权重")) // Reset Weights
        {
            TaskGamba.EnsureGambaWeightsInitialized(true);
        }
    }

    private bool showOverlay = C.ShowOverlay;
    private bool ShowSeconds = C.ShowSeconds;

    private void Overlay()
    {
        if (ImGui.Checkbox("显示 悬浮窗", ref showOverlay)) // Show Overlay
        {
            C.ShowOverlay = showOverlay;
            C.Save();
        }

        if (ImGui.Checkbox("显示 秒数", ref ShowSeconds)) // Show Seconds
        {
            C.ShowSeconds = ShowSeconds;
            C.Save();
        }
    }

    private bool EnableAutoSprint = C.EnableAutoSprint;
    private bool EnableAutoAntiAFK = C.AutoAntiAFK;

    private void Misc()
    {
        if (ImGui.Checkbox("启用 自动冲刺", ref EnableAutoSprint)) // Enable Auto Sprint
        {
            C.EnableAutoSprint = EnableAutoSprint;
            C.Save();
        }

        if (ImGui.Checkbox("启用 自动离开设置为不切换", ref EnableAutoAntiAFK))
        {
            C.AutoAntiAFK = EnableAutoAntiAFK;
            C.Save();
        }

    }

#if DEBUG

    private void Debug()
    {
        ImGui.Checkbox("Force OOM Main", ref SchedulerMain.DebugOOMMain);
        ImGui.Checkbox("Force OOM Sub", ref SchedulerMain.DebugOOMSub);
        ImGui.Checkbox("Legacy Failsafe WKSRecipe Select", ref C.FailsafeRecipeSelect);

        var missionMap = new List<(string name, Func<byte> get, Action<byte> set)>
                {
                    ("Sequence Missions", new Func<byte>(() => C.SequenceMissionPriority), new Action<byte>(v => { C.SequenceMissionPriority = v; C.Save(); })),
                    ("Timed Missions", new Func<byte>(() => C.TimedMissionPriority), new Action<byte>(v => { C.TimedMissionPriority = v; C.Save(); })),
                    ("Weather Missions", new Func<byte>(() => C.WeatherMissionPriority), new Action<byte>(v => { C.WeatherMissionPriority = v; C.Save(); }))
                };

        var sorted = missionMap
            .Select((m, i) => new { Index = i, Name = m.name, Priority = m.get() })
            .OrderBy(m => m.Priority)
            .ToList();
        ImGuiHelpers.ScaledDummy(5, 0);
        ImGui.SameLine();
        if (ImGui.CollapsingHeader("Provision Mission Priority"))
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                var item = sorted[i];
                ImGuiHelpers.ScaledDummy(5, 0);
                ImGui.SameLine();
                ImGui.Selectable(item.Name);
                if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                {
                    int nextIndex = i + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                    if (nextIndex >= 0 && nextIndex < sorted.Count)
                    {
                        // Swap the priority values
                        var otherItem = sorted[nextIndex];

                        // Swap their priority values via the original setters
                        byte temp = missionMap[item.Index].get();
                        missionMap[item.Index].set(missionMap[otherItem.Index].get());
                        missionMap[otherItem.Index].set(temp);
                        ImGui.ResetMouseDragDelta();
                    }
                }
            }
        }

        if (ImGui.Button("Get Sinus Forecast"))
        {
            List<WeatherForecast> forecast = WeatherForecastHandler.GetTerritoryForecast(1237);
            Func<WeatherForecast, string> formatTime = (forecast) => WeatherForecastHandler.FormatForecastTime(forecast.Time);

            Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
            {
                Message = $"Sinus Ardorum Weather - {forecast[0].Name}",
                Type = Dalamud.Game.Text.XivChatType.Echo,
            });
            for (int i = 1; i < forecast.Count; i++)
            {
                Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                {
                    Message = $"{forecast[i].Name} In {formatTime(forecast[i])}",
                    Type = Dalamud.Game.Text.XivChatType.Echo,
                });
            }
        }

        using (ImRaii.Disabled(!PlayerHelper.IsInCosmicZone()))
        {
            if (ImGui.Button("Refresh Forecast"))
            {
                WeatherForecastHandler.GetForecast();
            }
        }
    }

#endif
}
