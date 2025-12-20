using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class SafetySettings
    {
        private static bool rejectUnknownYesNo = C.RejectUnknownYesno;
        private static bool delayGrabMission = C.DelayGrabMission;
        private static int delayAmount = C.DelayIncrease;
        private static bool delayCraft = C.DelayCraft;
        private static int delayCraftAmount = C.DelayCraftIncrease;

        public static void Draw()
        {
            if (ImGui.Checkbox("忽略非宇宙探索相关提示", ref rejectUnknownYesNo))
            {
                C.RejectUnknownYesno = rejectUnknownYesNo;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "警告！此安全功能用于防止误入随机小队！\n" +
                "若取消勾选，您将自动接受随机小队的邀请。\n" +
                "您已获知风险，禁用后果自负。"
            );
            if (ImGui.Checkbox("任务菜单添加延迟", ref delayGrabMission))
            {
                C.DelayGrabMission = delayGrabMission;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "此为安全保护机制！若想缩短任务间隔时间，请随意调整。\n" +
                "安全值大概在... 250? 若遇到动画锁卡顿，完全可以调得更高。\n" +
                "当然，想追求刺激的话...调低也行。反正我不是你老爸（不过老爸笑话管够）。");
            if (delayGrabMission)
            {
                ImGui.SetNextItemWidth(150);
                ImGui.SameLine();
                if (ImGui.SliderInt("ms###Mission", ref delayAmount, 0, 1000))
                {
                    if (C.DelayIncrease != delayAmount)
                    {
                        C.DelayIncrease = delayAmount;
                        C.SaveDebounced();
                    }
                }
            }
            if (ImGui.Checkbox("制作菜单添加延迟", ref delayCraft))
            {
                C.DelayCraft = delayCraft;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "此为安全保护机制！若想缩短汇报延迟时间，请随意调整。\n" +
                "安全值大概在... 2500？若遇到动画锁卡顿，完全可以调得更高。\n" +
                "当然，想追求刺激的话...调低也行。反正我不是你老爸（不过老爸笑话管够）。");
            if (delayCraft)
            {
                ImGui.SetNextItemWidth(150);
                ImGui.SameLine();
                if (ImGui.SliderInt("ms###Crafting", ref delayCraftAmount, 500, 5000))
                {
                    if (C.DelayCraftIncrease != delayCraftAmount)
                    {
                        C.DelayCraftIncrease = delayCraftAmount;
                        C.SaveDebounced();
                    }
                }
            }
            bool jumpIfStuck = C.JumpIfStuck;
            if (ImGui.Checkbox("寻路移动卡住时跳跃", ref jumpIfStuck))
            {
                C.JumpIfStuck = jumpIfStuck;
                C.Save();
            }
            ImGuiEx.HelpMarker(
                "如果您在寻路移动过程中卡住了, 此选项将允许您经过特定时间后执行跳跃(当前为 3 秒)。\n" +
                "注意: 这是实验性功能。虽然有效, 但实际表现仍看起来很可疑。\n如果您发现某个位置会卡住角色, 请通过日志功能报告,\n" +
                "并提供相关信息以便我们修复这些问题。");
        }
    }
}
