using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using static ICE.ConfigFiles.Config;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class DronebitShoppingTab
    {
        public static unsafe void Draw()
        {
            bool OizysDronebitBuyEnabled = C.Tmp_OizysDronebitBuyItems;
            int OizysDronebitBuyAtAmount = C.Tmp_OizysDronebitBuyAtAmount;
            int OizysDronebitBuyAmount = C.Tmp_OizysDronebitBuyAmount;
            int OizysDronebitKeepAmount = C.Tmp_OizysDronebitKeepAmount;
            bool OizysDronebitKeepBuying = C.Tmp_OizysDronebitKeepBuying;

            if (ImGui.Checkbox("启用 购买俄匊斯能源包", ref OizysDronebitBuyEnabled))
            {
                C.Tmp_OizysDronebitBuyItems = OizysDronebitBuyEnabled;
                C.Save();
            }
            ImGui.SameLine();
            ImGuiEx.Icon(FontAwesomeIcon.QuestionCircle);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("此功能的逻辑等同于信用点购物, 所以简单说明。");
                ImGui.Text("因为上游还没有实现消费无人机晶片所以尝试性添加, 只支持俄匊斯行星, 具有实验性。");
                ImGui.BulletText("开始购买阈值: 无人机晶片达到此数量时, 执行购买俄匊斯资源包任务");
                ImGui.BulletText("购买数量: 目标购买数量, 在购买后会减少, 直到为 0");
                ImGui.BulletText("目标库存: 目标持有数量, 会购买直到你身上有这么多数量为止");
                ImGui.BulletText("持续购买: 前 2 个条件都不满足时持续进行购买, 会尝试花光所有的无人机晶片购买物品");

                ImGui.EndTooltip();
            }
            ImGui.NewLine();


            ImGui.SetNextItemWidth(150);
            if (ImGui.InputInt("开始购买阈值", ref OizysDronebitBuyAtAmount, 1))
            {
                if (OizysDronebitBuyAtAmount < 0)
                    OizysDronebitBuyAtAmount = 0;
                if (OizysDronebitBuyAtAmount > 5000)
                    OizysDronebitBuyAtAmount = 5000;
                C.Tmp_OizysDronebitBuyAtAmount = OizysDronebitBuyAtAmount;
                C.Save();
            }

            ImGui.SetNextItemWidth(80);
            if (ImGui.InputInt($"##buy_Dronebit", ref OizysDronebitBuyAmount))
            {
                OizysDronebitBuyAmount = Math.Clamp(OizysDronebitBuyAmount, 0, int.MaxValue);
                C.Tmp_OizysDronebitBuyAmount = OizysDronebitBuyAmount;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Text("购买数量");

            ImGui.SetNextItemWidth(80);
            if (ImGui.InputInt($"##keep_Dronebit", ref OizysDronebitKeepAmount))
            {
                OizysDronebitKeepAmount = Math.Clamp(OizysDronebitKeepAmount, 0, int.MaxValue);
                C.Tmp_OizysDronebitKeepAmount = OizysDronebitKeepAmount;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.Text("目标库存");


            if (ImGui.Checkbox("持续购买", ref OizysDronebitKeepBuying))
            {
                C.Tmp_OizysDronebitKeepBuying = OizysDronebitKeepBuying;
                C.Save();
            }
        }
    }
}