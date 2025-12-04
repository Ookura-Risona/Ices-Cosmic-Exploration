using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.SettingTabs
{
    internal class GambaWheel
    {
        private static bool gambaEnabled = C.GambaEnabled;
        private static int gambaDelay = C.GambaDelay;
        private static int gambaCreditsMinimum = C.GambaCreditsMinimum;
        private static bool gambaPreferSmallerWheel = C.GambaPreferSmallerWheel;

        // 字典映射
        private static readonly Dictionary<GambaType, string> GambaTypeDisplayNames = new()
        {
            { GambaType.Mount, "坐骑" },
            { GambaType.Emote, "情感动作" },
            { GambaType.Minion, "宠物" },
            { GambaType.Outfit, "套装" },
            { GambaType.Accessory, "时尚配饰" },
            { GambaType.Orchestrion, "管弦乐琴乐谱" },
            { GambaType.Housing, "房屋用品" },
            { GambaType.Dye, "染剂" },
            { GambaType.Other, "其他" },
            { GambaType.Materia, "魔晶石" },
        };

        public static void Draw()
        {
            if (ImGui.Checkbox("启用 自动宇宙好运道", ref gambaEnabled))
            {
                C.GambaEnabled = gambaEnabled;
                C.Save();
            }
            ImGuiEx.HelpMarker("启用此选项将自动选择转盘进行宇宙好运道。如果您不希望玩宇宙好运道时自动执行, 请禁用此选项。");
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("保留最低信用点数量", ref gambaCreditsMinimum, 0, 10000))
            {
                C.GambaCreditsMinimum = gambaCreditsMinimum;
                C.SaveDebounced();
            }
            bool gambaBetween = C.GambaBetweenRuns;
            if (ImGui.Checkbox("运行期间玩宇宙好运道", ref gambaBetween))
            {
                C.GambaBetweenRuns = gambaBetween;
                C.Save();
            }
            ImGui.SameLine();
            GambaSlider();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("抽奖延迟(ms)", ref gambaDelay, 50, 2000))
            {
                C.GambaDelay = gambaDelay;
                C.SaveDebounced();
            }

            if (ImGui.Checkbox("优先更小的转盘", ref gambaPreferSmallerWheel))
            {
                C.GambaPreferSmallerWheel = gambaPreferSmallerWheel;
                C.Save();
            }
            ImGuiEx.HelpMarker("此选项将优先选择物品数量更少的转盘");
            ImGui.Separator();
            ImGui.TextUnformatted("配置宇宙好运道每个物品的权重，物品权重越高 = 越优先获取该物品");
            ImGui.Spacing();
            foreach (GambaType type in Enum.GetValues(typeof(GambaType)))
            {
                var itemsType = C.GambaItemWeights.Where(x => x.Type == type).OrderBy(x => x.ItemId).ToList();
                if (itemsType.Count == 0) continue;

                // 使用映射表获取显示名
                string displayName = GambaTypeDisplayNames.TryGetValue(type, out var name) ? name : type.ToString();

                if (ImGui.TreeNodeEx($"{displayName} ({itemsType.Count})##gamba_type_{type}", ImGuiTreeNodeFlags.DefaultOpen))
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
            if (ImGui.Button("重置权重"))
            {
                Task_Gamba.EnsureGambaWeightsInitialized(true);
            }
        }

        private static int[] allowedValues = { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000 };
        private static void GambaSlider()
        {
            int currentIndex = Array.IndexOf(allowedValues, C.GambaAtAmount);
            if (currentIndex == -1)
            {
                currentIndex = 0;
                C.GambaAtAmount = allowedValues[0];
                C.SaveDebounced();
            }

            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("开始抽奖阈值", ref currentIndex, 0, allowedValues.Length - 1,
                allowedValues[currentIndex].ToString()))
            {
                C.GambaAtAmount = allowedValues[currentIndex];
                C.SaveDebounced();
            }
        }
    }
}
