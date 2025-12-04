using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class SafetySettings
    {
        private static bool animationLockAbandon = C.AnimationLockAbandon;
        private static bool stopOnAbort = C.StopOnAbort;
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
                if (ImGui.SliderInt("ms###Crafting", ref delayCraftAmount, 6000, 20000))
                {
                    if (C.DelayCraftIncrease != delayCraftAmount)
                    {
                        C.DelayCraftIncrease = delayCraftAmount;
                        C.SaveDebounced();
                    }
                }
            }
        }
    }
}
