using Dalamud.Interface.Utility.Raii;
using ICE.Config;
using System.Text;
using System.Text.Json;

namespace ICE.Ui.SettingTabs
{
    internal class GatherSettings
    {
        private static bool SelfRepairGather = C.SelfRepairGather;
        private static float SelfRepairPercent = C.RepairPercent;
        private static bool SelfSpiritbondGather = C.SelfSpiritbondGather;
        private static bool AutoFisherCast = C.AutoFisherCast;
        private static bool AutoCordial = C.AutoCordial;
        private static bool InverseCordialPrio = C.inverseCordialPrio;
        private static bool UseOnFisher = C.UseOnFisher;
        private static bool PreventOvercap = C.PreventOvercap;
        private static int CordialMinGp = C.CordialMinGp;
        private static bool useOnlyInMission = C.UseOnlyInMission;
        private static string newProfileName = "";

        private static string[] MissionTypes = ["限量采集点", "采集 X 个物品", "时间竞速", "连锁冲分", "恩惠冲分", "连锁 + 恩惠冲分", "双职业"];
        private static int MissionIndex = 0;

        private static string exportString = "";
        private static string importString = "";
        private static string exportError = "";
        private static string importError = "";

        public static class GatherProfileExporter
        {
            private const string PROFILE_PREFIX = "ICEPROFILE_";
            private const int CURRENT_VERSION = 1;

            public class ExportableProfile
            {
                public int Version { get; set; } = CURRENT_VERSION;
                public string Name { get; set; }
                public int MinimumGp { get; set; }
                public int DualClassCraftAmount { get; set; }
                public Config.GatherBuffs GatherBuffs { get; set; }
            }

#nullable disable
            public static string ExportProfile(GatherProfile profile)
            {
                try
                {
                    var exportable = new ExportableProfile
                    {
                        Name = profile.Name,
                        MinimumGp = profile.MinimumGp,
                        DualClassCraftAmount = profile.DualClassCraftAmount,
                        GatherBuffs = profile.GatherBuffs
                    };

                    string json = JsonSerializer.Serialize(exportable, new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    string base64 = Convert.ToBase64String(bytes);

                    return PROFILE_PREFIX + base64;
                }
                catch (Exception ex)
                {
                    // Log error appropriately for your plugin
                    return null;
                }
            }

            public static (bool Success, ExportableProfile Profile, string Error) ImportProfile(string importString)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(importString))
                        return (false, null, "导入字符串为空");

                    if (!importString.StartsWith(PROFILE_PREFIX))
                        return (false, null, "无效的配置格式 - 缺少前缀");

                    string base64 = importString.Substring(PROFILE_PREFIX.Length);

                    byte[] bytes = Convert.FromBase64String(base64);
                    string json = Encoding.UTF8.GetString(bytes);

                    var profile = JsonSerializer.Deserialize<ExportableProfile>(json);

                    if (profile.Version > CURRENT_VERSION)
                        return (false, null, "配置版本高于当前支持的版本");

                    if (string.IsNullOrWhiteSpace(profile.Name))
                        return (false, null, "配置名称不能为空");

                    return (true, profile, null);
                }
                catch (FormatException)
                {
                    return (false, null, "无效的 base64 格式");
                }
                catch (JsonException)
                {
                    return (false, null, "无效的 JSON 格式");
                }
                catch (Exception ex)
                {
                    return (false, null, $"导入失败: {ex.Message}");
                }
            }
#nullable enable

            public static GatherProfile ConvertToGatherProfile(ExportableProfile exportable, int newId)
            {
                return new GatherProfile
                {
                    Id = newId,
                    Name = exportable.Name,
                    MinimumGp = exportable.MinimumGp,
                    DualClassCraftAmount = exportable.DualClassCraftAmount,
                    GatherBuffs = exportable.GatherBuffs
                };
            }
        }

        public static void Draw()
        {
            void DrawBuffSetting(string label, string uniqueId, bool currentEnabled, 
                                 int currentMinGp, int minGpLimit, int maxGpLimit, 
                                 string entryName, string ActionInfo, 
                                 Action<bool> onEnabledChange, Action<int> onMinGpChange, 
                                 int currentMaxUse, Action<int> onMaxUseChange)
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

                    if (ImGui.TreeNode($"{label} 设置###Tree{uniqueId}{entryName}"))
                    {
                        int minGp = currentMinGp;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("低于此 GP 使用强心剂");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200);
                        if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                        {
                            if (minGp != currentMinGp)
                                onMinGpChange(minGp);
                        }
                        int maxUse = currentMaxUse;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("最大使用次数");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                        {
                            if (maxUse != currentMaxUse)
                                onMaxUseChange(maxUse);
                        }
                        ImGuiEx.HelpMarker("设置为 -1 时，允许无限使用 \n" +
                                           "设置为 1 -> X 时，设定为每次任务的最大使用次数上限");

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

                    if (ImGui.TreeNode($"{label} 设置###Tree{uniqueId}{entryName}"))
                    {
                        int minGp = currentMinGp;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("最低 GP");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200);
                        if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                        {
                            if (minGp != currentMinGp)
                                onMinGpChange(minGp);
                        }
                        int maxUse = currentMaxUse;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("最大使用次数");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                        {
                            if (maxUse != currentMaxUse)
                                onMaxUseChange(maxUse);
                        }
                        ImGuiEx.HelpMarker("设置为 -1 时，允许无限使用 \n" +
                                           "设置为 1 -> X 时，设定为每次任务的最大使用次数上限");

                        int MinItem = MinItemUsage;
                        ImGui.Text($"最低高产使用所需数量");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.SliderInt($"###MinItemsBYII{uniqueId}{entryName}_1", ref MinItem, 2, 4))
                        {
                            if (MinItem != MinItemUsage)
                                onMinItemMaxUseChange(MinItem);
                        }
                        ImGuiEx.HelpMarker($"设置使用高产所需的最低物品数\n" +
                                           $"示例：设为 2 时，仅在需要收集 2 个或以上物品时才会使用\n" +
                                           $"适用场景：节省GP（采集力），适用于\"收集 X 个物品\"或 双职业任务");

                        ImGui.TreePop();
                    }
                    ImGui.Unindent(15);
                }
            }

            int maxGp = 1200;

            if (ImGui.Checkbox("采集时自动修理", ref SelfRepairGather))
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
                ImGui.Text("修理阈值");
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
            if (ImGui.Checkbox("采集时精制魔晶石", ref SelfSpiritbondGather))
            {
                if (C.SelfSpiritbondGather != SelfSpiritbondGather)
                {
                    C.SelfSpiritbondGather = SelfSpiritbondGather;
                    C.Save();
                }
            }
            if (ImGui.Checkbox("自动在钓鱼任务开始时抛竿", ref AutoFisherCast)) // 新增: 自动在钓鱼任务开始时抛竿，用于适配 MissFisher 的钓鱼逻辑
            {
                if (C.AutoFisherCast != AutoFisherCast)
                {
                    C.AutoFisherCast = AutoFisherCast;
                    C.Save();
                }
            }
            ImGuiEx.HelpMarker("自动在捕鱼人任务开始时执行\"抛竿\"技能, 取消勾选则不会在任务开始时自动抛竿。");
            if (ImGui.Checkbox("自动强心剂", ref AutoCordial))
            {
                C.AutoCordial = AutoCordial;
                C.Save();
            }
            ImGuiEx.HelpMarker("仅在 ICE 运行时生效，手动模式无效\n" +
                               "在宇宙探索地图中将暂停 Pandora 插件的自动强心剂功能");
            if (AutoCordial)
            {
                if (ImGui.TreeNode("强心剂设置"))
                {
                    if (ImGui.Checkbox("反转优先级 (轻型 -> 普通 -> 高级)", ref InverseCordialPrio))
                    {
                        C.inverseCordialPrio = InverseCordialPrio;
                        C.Save();
                    }
                    if (ImGui.Checkbox("防止溢出", ref PreventOvercap))
                    {
                        C.PreventOvercap = PreventOvercap;
                        C.Save();
                    }
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt("GP 低于设定值时使用强心剂", ref CordialMinGp, 0, maxGp))
                    {
                        C.CordialMinGp = CordialMinGp;
                        C.Save();
                    }
                    ImGui.SameLine();
                    ImGuiEx.HelpMarker("此值为在使用强心剂之前, 最低所需的 GP(采集力)\n" +
                                       "如果设置为 0, 即使启动了此功能也不会使用强心剂(因为... 您的 GP(采集力) 永远不会为 0)\n" +
                                       "补充说明: 设定值不要为 0 或溢出 GP(采集力) 上限, 比如 900 上限则设定为 500 以下(举例高级强心剂), 设置为 0 无效是代码层面的问题");

                    ImGui.TreePop();
                }
            }

            ImGui.Dummy(new(0, 5));

            ImGui.SetNextItemWidth(200);
            ImGui.InputText("新配置名称", ref newProfileName, 64);
            using (ImRaii.Disabled(newProfileName == ""))
            {
                if (ImGui.Button("添加配置") && !string.IsNullOrWhiteSpace(newProfileName))
                {
                    if (!C.GatherSettings.Any(x => x.Name == newProfileName))
                    {
                        int newId = C.GatherSettings.Max(x => x.Id) + 1;
                        C.GatherSettings.Add(new Config.GatherProfile { Id = newId, Name = newProfileName });
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

            ImGui.Text("采集配置");

            bool canDelete = C.GatherSettings.Count > 1 && C.SelectedGatherIndex != 0;
            using (ImRaii.Disabled(!canDelete))
            {
                if (ImGui.Button("删除选中的配置"))
                {
                    var deletedProfile = C.GatherSettings[C.SelectedGatherIndex];
                    int deletedId = deletedProfile.Id;

                    // Remove the profile
                    C.GatherSettings.RemoveAt(C.SelectedGatherIndex);

                    // Update all missions using this GatherSettingId
                    foreach (var mission in C.MissionConfig)
                    {
                        if (mission.Value.GatherProfileId == deletedId)
                        {
                            mission.Value.GatherProfileId = C.GatherSettings[0].Id; // fallback to default
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

            GatherProfile entry = C.GatherSettings[C.SelectedGatherIndex];

            ImGui.Combo("任务类型", ref MissionIndex, MissionTypes, MissionTypes.Length);
            if (ImGui.Button("应用到任务类型"))
            {
                foreach (var mission in C.MissionConfig)
                {
                    var id = mission.Key;

                    var missionDict = CosmicHelper.SheetMissionDict[id];

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
                        mission.Value.GatherProfileId = entry.Id;
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

            /* Don't necessary want to get rid of this, it's good in practicality. Just need to bring to new system eventually...
             * 
            // Pathfinding
            int pathfinding = entry.Pathfinding;
            string[] modes = ["Simple", "Nearest", "Cyclic"];
            ImGui.SetNextItemWidth(100);
            if (ImGui.Combo("Pathfinding mode", ref pathfinding, modes, modes.Length))
            {
                entry.Pathfinding = pathfinding;
                C.Save();
            }
            ImGuiEx.HelpMarker("Simple - From 1st node in list until the last.\nNearest - Always go to Nearest node then find a path that minimises distance through all remaining nodes.\nCyclic - Find nodes that are close together and stick to those nodes only.");
            if (pathfinding == 2)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                int cycle = entry.TSPCycleSize;
                if (ImGui.InputInt("Cycle size", ref cycle, 1))
                {
                    entry.TSPCycleSize = cycle >= 2 ? cycle : 2;
                    C.Save();
                }
            }
            */

            // GP Settings
            int minGP = entry.MinimumGp;
            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("开始任务所需最低 GP", ref minGP, -1, maxGp))
            {
                entry.MinimumGp = minGP;
                C.Save();
            }

            ImGui.Text("双职业任务制作数量去哪了?"); // Where'd the dual craft amount go?
            ImGui.SameLine();
            ImGui.Dummy(new(5, 0));
            ImGui.SameLine();
            ImGui.TextDisabled("?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetNextWindowSize(new(400.0f, 0.0f)); // Fixed width, auto height
                ImGui.BeginTooltip();

                ImGui.TextWrapped("简答: 现在已经内置了\n" +
                 "详解: 说实话, 这个系统本身就很繁琐。而且随着 SE 决定第二个星球不再有双职业制作任务, 我觉得直接把它绑到评价系统里会更好。实际上你只需要:\n" +
                 "金星: 3 个物品\n" +
                 "银星: 2 个物品\n" +
                 "铜星: 1 个物品\n" +
                 "就能达到门槛。即使第一次没达成, 它也会继续采集。 再加上这样一来，我就不用为了...... 4 个任务去管理钓鱼配置文件了？在我看来有点小小的多余。\n" +
                 "所以现在的运作方式是: 选择一项汇报选项(金星/任意 都一样), 它会采集制作所需数量 -> 完成后自动汇报任务。\n" +
                 "现在你们谁都不能再让它去做 27 个物品了。停下！它说的是制作 (╯°Д°)╯︵/(.□ . \\)");
                ImGui.EndTooltip();
            }
            // Multiply gathered items on FIRST gather loop only. Should only be used for Dual Class really.
            /*
            int gatherMult = entry.DualClassCraftAmount;
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("双职业任务制作数量", ref gatherMult, 1))
            {
                entry.DualClassCraftAmount = gatherMult >= 1 ? gatherMult : 1;
                C.Save();
            }
            ImGuiEx.HelpMarker("此选项将增加您在切换到制作流程前需要收集的物品数量(即达到\"完成\"状态)。\n根据您需要制作多少物品才能达到目标技巧点来调整此数值。\n只影响双职业任务。");
            */

            // Boon Increase 2 (+30% Increase)
            DrawBuffSetting(
                label: "沃土 / 富矿的馈赠 II",
                uniqueId: $"Boon2Inc{entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["BoonIncrease2"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["BoonIncrease2"].MinGp,
                minGpLimit: 100,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "额外采集奖励发生率提升30%",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BoonIncrease2"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BoonIncrease2"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["BoonIncrease2"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BoonIncrease2"].MaxUse = newVal;
                    C.Save();
                }
            );

            // Boon Increase 1 (+10% Increase)
            DrawBuffSetting(
                label: "沃土 / 富矿的馈赠 I",
                uniqueId: $"Boon1Inc{entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["BoonIncrease1"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["BoonIncrease1"].MinGp,
                minGpLimit: 50,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "额外采集奖励发生率提升10%",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BoonIncrease1"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BoonIncrease1"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["BoonIncrease1"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BoonIncrease1"].MaxUse = newVal;
                    C.Save();
                }
            );

            // Tidings (+2 to boon instead of +1)
            DrawBuffSetting(
                label: "诺菲卡 / 纳尔札尔 福音",
                uniqueId: $"TidingsBuff{entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["Tidings"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["Tidings"].MinGp,
                minGpLimit: 200,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "额外采集奖励发生时的获得数增加1个",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["Tidings"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["Tidings"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["Tidings"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["Tidings"].MaxUse = newVal;
                    C.Save();
                }
            );

            // Yield II (+2 to all items on node)
            DrawBuffSetting(
                label: "天赐收成 / 莫非王土 II",
                uniqueId: $"Blessed/KingsYieldIIBuff{entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["YieldII"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["YieldII"].MinGp,
                minGpLimit: 500,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "令获得数增加2个\n" +
                            "只在采集点满耐久时使用",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["YieldII"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["YieldII"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["YieldII"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["YieldII"].MaxUse = newVal;
                    C.Save();
                }
            );

            // Yield I (+1 to all items on node)
            DrawBuffSetting(
                label: "天赐收成 / 莫非王土 I",
                uniqueId: $"Blessed/KingsYieldIBuff{entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["YieldI"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["YieldI"].MinGp,
                minGpLimit: 400,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "令获得数增加1个\n" +
                            "只在采集点满耐久时使用",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["YieldI"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["YieldI"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["YieldI"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["YieldI"].MaxUse = newVal;
                    C.Save();
                }
            );

            // Bonus Integrity (+1 integrity)
            DrawBuffSetting(
                label: "农夫之智 / 石工之理",
                uniqueId: $"Incrase Intregity{entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["BonusIntegrity"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["BonusIntegrity"].MinGp,
                minGpLimit: 300,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "恢复1次采集次数\n" +
                            "50%几率附加理智同兴预备状态",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BonusIntegrity"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BonusIntegrity"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["BonusIntegrity"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BonusIntegrity"].MaxUse = newVal;
                    C.Save();
                }
            );

            // Bountiful Yield/Harvest II (+Amount based on gathering)
            DrawCustomBuffSetting(
                label: "高产 II / 丰收 II",
                uniqueId: $"Bountiful Yield II {entry.Id}",
                currentEnabled: entry.GatherBuffs.Buffs["BountifulYieldII"].Enabled,
                currentMinGp: entry.GatherBuffs.Buffs["BountifulYieldII"].MinGp,
                minGpLimit: 100,
                maxGpLimit: maxGp,
                entryName: entry.Name,
                ActionInfo: "令下一次采集的获得数增加\n" +
                            "获得力影响获得数的增加量（最小1～最大3）",
                onEnabledChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BountifulYieldII"].Enabled = newVal;
                    C.Save();
                },
                onMinGpChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BountifulYieldII"].MinGp = newVal;
                    C.Save();
                },
                currentMaxUse: entry.GatherBuffs.Buffs["BountifulYieldII"].MaxUse,
                onMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.Buffs["BountifulYieldII"].MaxUse = newVal;
                    C.Save();
                },
                entry.GatherBuffs.BountifulMinItem,
                onMinItemMaxUseChange: newVal =>
                {
                    entry.GatherBuffs.BountifulMinItem = newVal;
                    C.Save();
                }
            );

            ImGui.Columns(1);

            // Add the export/import section
            DrawExportImportSection();
        }

        public static void DrawExportImportSection()
        {
            ImGui.Separator();
            ImGui.Text("导出/导入配置");
            ImGui.Spacing();

            var currentProfile = C.GatherSettings[C.SelectedGatherIndex];

            // Export Section
            if (ImGui.Button("导出当前配置"))
            {
                exportString = GatherProfileExporter.ExportProfile(currentProfile);
                exportError = string.IsNullOrEmpty(exportString) ? "导出失败!" : "";
            }

            if (!string.IsNullOrEmpty(exportString))
            {
                ImGui.Text("已导出配置 (点击复制):");
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f));

                if (ImGui.InputTextMultiline("##ExportOutput", ref exportString, 4096,
                    new Vector2(-1, ImGui.GetTextLineHeight() * 3), ImGuiInputTextFlags.ReadOnly))
                {
                    ImGui.SetClipboardText(exportString);
                }

                ImGui.PopStyleColor();

                if (ImGui.Button("复制到剪贴板"))
                {
                    ImGui.SetClipboardText(exportString);
                }
            }

            if (!string.IsNullOrEmpty(exportError))
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), exportError);
            }

            ImGui.Spacing();

            // Import Section
            ImGui.Text("导入配置:");
            ImGui.InputTextMultiline("##ImportInput", ref importString, 4096,
                new Vector2(-1, ImGui.GetTextLineHeight() * 3));

            if (ImGui.Button("导入配置"))
            {
                var (success, profile, error) = GatherProfileExporter.ImportProfile(importString);

                if (success)
                {
                    // Check if profile name already exists
                    string finalName = profile.Name;
                    int counter = 1;
                    while (C.GatherSettings.Any(x => x.Name == finalName))
                    {
                        finalName = $"{profile.Name} ({counter})";
                        counter++;
                    }

                    int newId = C.GatherSettings.Max(x => x.Id) + 1;
                    var newProfile = GatherProfileExporter.ConvertToGatherProfile(profile, newId);
                    newProfile.Name = finalName;

                    C.GatherSettings.Add(newProfile);
                    C.Save();

                    importString = "";
                    importError = $"导入配置成功: {finalName}";
                }
                else
                {
                    importError = error;
                }
            }

            if (!string.IsNullOrEmpty(importError))
            {
                var color = importError.StartsWith("Successfully") ?
                    new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1);
                ImGui.TextColored(color, importError);
            }
        }
    }
}