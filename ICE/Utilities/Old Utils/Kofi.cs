using Dalamud.Interface.Utility;
using ECommons.EzSharedDataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Utilities;

internal class Kofi // Heavily borrowed from the Ecommons version. 
{
    public static void DrawRight()
    {
        var cur = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(cur.X + ImGui.GetContentRegionAvail().X - ImGuiHelpers.GetButtonSize(Text).X);
        DrawRaw();
        ImGui.SetCursorPos(cur);
    }

    public static void DrawRaw()
    {
        DrawButton();
    }

    private static uint ColorNormal
    {
        get
        {
            var vector1 = ImGuiEx.Vector4FromRGB(0x022594);
            var vector2 = ImGuiEx.Vector4FromRGB(0x940238);

            var gen = GradientColor.Get(vector1, vector2).ToUint();
            var data = EzSharedData.GetOrCreate<uint[]>("ECommonsPatreonBannerRandomColor", [gen]);
            if (!GradientColor.IsColorInRange(data[0].ToVector4(), vector1, vector2))
            {
                data[0] = gen;
            }
            return data[0];
        }
    }

    public static string Text = "♥ Ko-fi";
    public static string DonateLink => "https://ko-fi.com/ice643269";

    private static uint ColorHovered => ColorNormal;

    private static uint ColorActive => ColorNormal;

    private static readonly uint ColorText = 0xFFFFFFFF;

    private static string PatreonButtonTooltip => $"""
				如果您喜欢 {Svc.PluginInterface.Manifest.Name}，请考虑通过 Ko-Fi 支持开发者！
				通过讲优质老爸笑话保持温暖，助力对抗全球变暖。
				左键点击 - 前往 Ko-Fi;	
				""";

    public static void DrawButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ColorNormal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorActive);
        ImGui.PushStyleColor(ImGuiCol.Text, ColorText);
        if (ImGui.Button(Text))
        {
            GenericHelpers.ShellStart(DonateLink);
        }
        Popup();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        ImGui.PopStyleColor(4);
    }

    private static void Popup()
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGuiEx.Text(PatreonButtonTooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
    }
}
