using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table;

public static class Settings_TableColumns
{
    private static string[] missionSortOptions = ["ID", "任务名称", "宇宙信用点", "行星信用点", "研究数据 I", "研究数据 II", "研究数据 III", "研究数据 IV", "研究数据 V", "地图位置"];

    public static void ColumnSettings()
    {
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

        bool hideUnsupported = C.HideUnsupportedMissions;
        if (ImGui.Checkbox("隐藏不支持的任务", ref hideUnsupported))
        {
            C.HideUnsupportedMissions = hideUnsupported;
            C.Save();
        }

        bool showExtraInfo = C.ShowExtraMissionInfo;
        if (ImGui.Checkbox("显示额外任务信息侧边窗口", ref showExtraInfo))
        {
            C.ShowExtraMissionInfo = showExtraInfo;
            C.Save();
        }

        bool autoShowToken = C.Auto_ShowTokens;
        if (ImGui.Checkbox("自动显示/隐藏行星票据", ref autoShowToken))
        {
            C.Auto_ShowTokens = autoShowToken;
            C.Save();
        }



        bool showManualMode = C.ShowManualMode;
        if (ImGui.Checkbox("显示手动模式表格列", ref showManualMode))
        {
            C.ShowManualMode = showManualMode;
            C.Save();
        }
        ImGuiEx.HelpMarker("只在您打算亲自完成任务, 而不是依靠插件自动化完成时, 才需要启用此选项。\n" +
                           "另外, 如果您使用其他插件来处理汇报、制作、采集等自动化操作, 并且不希望 I.C.E. 与这些插件交互, 也可以启用此选项。");
    }
    public static void GeneralMissionSettings()
    {
        bool onlyGrabMission = C.OnlyGrabMission;
        if (ImGui.Checkbox($"只刷取任务", ref onlyGrabMission))
        {
            C.OnlyGrabMission = onlyGrabMission;
            C.Save();
        }

        bool removeGold = C.RemoveAfterGold;
        if (ImGui.Checkbox("金星完成时移除任务", ref removeGold))
        {
            C.RemoveAfterGold = removeGold;
            C.Save();
        }

        ImGui.Checkbox("当前任务结束后停止", ref Mission_Settings.StopAfterCurrent);
        bool relicTurnin = C.TurninRelic;
        if (ImGui.Checkbox($"宇宙工具可报告时提交", ref relicTurnin))
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
    }
}
