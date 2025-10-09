using Dalamud.Interface.Utility.Raii;
using ECommons.Reflection;
using FFXIVClientStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ICE.Ui
{
    internal class InfoWindow : Window
    {
        public InfoWindow() : base($"Ice's Cosmic Exploration - 信息")
        {
            Flags = ImGuiWindowFlags.None;
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(100, 100),
                MaximumSize = new Vector2(4000, 4000),
            };

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
            DrawSection("这是什么插件？",
            new[]
            {
                "这是一个帮助你自动化完成月球犯罪的万事通，帮助你获得需要的宇宙工具/探索任务。它能做到以下几点：",
                "选择你想要刷取的任务，插件会自动重刷直到任务出现。",
                "查看任务完成状态，了解哪些任务已做过、已完成以及取得金星评价。",
                "允许你自动化刷取宇宙工具的研究数据。你需要安装一些额外的依赖插件（更多信息见下方职业部分）。",
                "想刷技巧点？选择你想要的任务，运行即可。我不建议通宵挂机运行，这很容易导致账号被封，尤其是在公共区域。",
            });
            DrawSection("制作",
            [
                "需要 Artisan 插件才能实现全自动制作",
                "你只需要确保已选择好所有你想要的任务，然后运行即可",
                "Artisan 也有一个内置的在设定阈值修理装备的功能，你可以选择使用它，或者使用 ICE 插件内置的修理选项在任务开始前进行修理。",
                "一些注意事项：如果你使用 \"Raphael 求解器\" 设置，确保根据自身情况来设置宏生成超时时间。",
                "另外，除非你启用了 \"在专家配方上生成\"，否则不会在专家/高难度配方上生成宏。（这些配方看上去与普通配方相似，但制作过程中有特殊 Buff 轮换出现）"
            ],
            new[]
            {
                new SectionButton 
                {
                    Label = "安装 Artisan Repo", 
                    OnClick = () => InstallArtisanRepo(), 
                    IsVisible = () => !DalamudReflector.HasRepo("https://love.puni.sh/ment.json")
                },
                new SectionButton
                {
                    Label = isInstallingArtisan ? "正在安装..." : "安装 Artisan",
                    OnClick = () => _ = InstallArtisan(),
                    IsVisible = () => !Utils.HasPlugin("Artisan") && !isInstallingArtisan
                }
            });
            DrawSection("采集 [园艺工 + 采矿工]",
            [
                "插件已经内置了园艺工与采矿工的采集功能！",
                "只需设置一个采集配置文件（在 设置 -> 采集 标签页完成），并根据任务类型进行应用。请确保该配置文件已被选中用于对应任务。",
                "插件同样支持采集收藏品。实现这一功能的关键在于 Vnavmesh 插件，它可以让你在采集点之间全自动移动。",
            ],
            new[]
            {
                new SectionButton
                {
                    Label = "安装 Navmesh Repo",
                    OnClick = () => InstallNavmeshRepo(),
                    IsVisible = () => !DalamudReflector.HasRepo("https://puni.sh/api/repository/veyn")
                },
                new SectionButton
                {
                    Label = isInstallingArtisan ? "正在安装..." : "安装 Navmesh",
                    OnClick = () => _ = InstallNavmesh(),
                    IsVisible = () => !Utils.HasPlugin("vnavmesh") && !isInstallingNavmesh
                }
            });
            DrawSection("采集 [捕鱼人]",
            [
                "宇宙钓鱼... 真的很折磨。这是没办法的事，随机性真是太极致了。无法保证一定能拿到金星评价。",
                "你需要安装 AutoHook 插件，它能帮助你自动化整个钓鱼流程。",
                "对于已支持的任务，你实际上什么都不用做，预设已在内置的插件中，在任务开始时自动导入，任务结束时自动删除。",
                "如果你想使用自己的预设，只需在 \"选择配置\" 按钮中进行启用，输入你所的使用预设名称。",
                "同时需要 Navmesh 插件，以确保你能站在钓场的正确位置进行钓鱼。",
                "如果你需要 自动确认收藏品 的帮助指南，下方还附有一个按钮，点击后会打开对应的 Wiki 指南页面。"
            ],
            new[]
            {
                new SectionButton
                {
                    Label = "安装 Autohook Repo",
                    OnClick = () => InstallAutoHookRepo(),
                    IsVisible = () => !DalamudReflector.HasRepo("https://love.puni.sh/ment.json")
                },
                new SectionButton
                {
                    Label = isInstallingArtisan ? "正在安装..." : "安装 Autohook",
                    OnClick = () => _ = InstallAutoHook(),
                    IsVisible = () => !Utils.HasPlugin("AutoHook") && !isInstallingAutoHook
                },
                new SectionButton
                {
                    Label = "如何自动确认收藏品",
                    OnClick = () => GenericHelpers.ShellStart("https://github.com/PunishXIV/AutoHook/blob/main/AcceptCollectable.md")
                }
            });
        }

        private bool isInstallingArtisan = false;
        private void InstallArtisanRepo()
        {
            DalamudReflector.AddRepo("https://love.puni.sh/ment.json", true);
            DalamudReflector.SaveDalamudConfig();
        }
        private async Task InstallArtisan()
        {
            if (isInstallingArtisan) return; // Already installing

            isInstallingArtisan = true;
            try
            {
                await DalamudReflector.AddPlugin("https://love.puni.sh/ment.json", "Artisan");
                DalamudReflector.SaveDalamudConfig();
            }
            finally
            {
                isInstallingArtisan = false;
            }
        }

        private bool isInstallingNavmesh = false;
        private void InstallNavmeshRepo()
        {
            DalamudReflector.AddRepo("https://puni.sh/api/repository/veyn", true);
            DalamudReflector.SaveDalamudConfig();
        }

        private async Task InstallNavmesh()
        {
            if (isInstallingNavmesh) return; // Already installing

            isInstallingNavmesh = true;
            try
            {
                await DalamudReflector.AddPlugin("https://puni.sh/api/repository/veyn", "vnavmesh");
                DalamudReflector.SaveDalamudConfig();
            }
            finally
            {
                isInstallingNavmesh = false;
            }
        }

        private bool isInstallingAutoHook = false;

        private void InstallAutoHookRepo()
        {
            DalamudReflector.AddRepo("https://love.puni.sh/ment.json", true);
            DalamudReflector.SaveDalamudConfig();
        }

        private async Task InstallAutoHook()
        {
            if (isInstallingAutoHook) return; // Already installing

            isInstallingAutoHook = true;
            try
            {
                await DalamudReflector.AddPlugin("https://love.puni.sh/ment.json", "AutoHook");
                DalamudReflector.SaveDalamudConfig();
            }
            finally
            {
                isInstallingAutoHook = false;
            }
        }

        public class SectionButton
        {
            public string Label { get; set; }
            public Action OnClick { get; set; }
            public Func<bool> IsVisible { get; set; } // Returns true to show the button
        }

        private void DrawSection(string title, string[] bulletPoints, SectionButton[] buttons = null, string[] RequiredPlugins = null)
        {
            // Get the current window background color
            uint windowBg = ImGui.GetColorU32(ImGuiCol.WindowBg);

            // Convert to Vector4 for easier manipulation
            var bgColor = ImGui.ColorConvertU32ToFloat4(windowBg);

            // Darken it by a factor (e.g., 0.85 = 85% brightness)
            bgColor.X *= 0.85f; // R
            bgColor.Y *= 0.85f; // G
            bgColor.Z *= 0.85f; // B
                                // bgColor.W stays the same (alpha)

            // Convert back to uint
            uint darkerBg = ImGui.ColorConvertFloat4ToU32(bgColor);

            using var colorBackground = ImRaii.PushColor(ImGuiCol.ChildBg, darkerBg);

            // For border, you could also derive it from the current border or keep it as-is
            // Option 1: Use current border color
            uint currentBorder = ImGui.GetColorU32(ImGuiCol.Border);
            using var colorBorder = ImRaii.PushColor(ImGuiCol.Border, currentBorder);

            // Calculate the height needed
            float height = ImGui.GetStyle().WindowPadding.Y; // Top padding

            // Title height
            height += ImGui.CalcTextSize(title).Y;
            height += ImGui.GetStyle().ItemSpacing.Y;

            // Separator (roughly 1 pixel + spacing)
            height += 1 + ImGui.GetStyle().ItemSpacing.Y;

            // Calculate bullet point heights with proper wrapping width
            float availableWidth = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X * 2;
            float bulletIndent = ImGui.GetFontSize() + ImGui.GetStyle().ItemSpacing.X; // Bullet size + spacing

            foreach (var point in bulletPoints)
            {
                var textSize = ImGui.CalcTextSize(point, true, availableWidth - bulletIndent);
                height += textSize.Y;
                height += ImGui.GetStyle().ItemSpacing.Y;
            }

            // Add button height if any visible buttons exist
            if (buttons != null && buttons.Any(b => b.IsVisible?.Invoke() ?? true))
            {
                height += ImGui.GetStyle().ItemSpacing.Y; // Extra spacing before buttons
                height += ImGui.GetFrameHeight(); // Button height
                height += ImGui.GetStyle().ItemSpacing.Y;
            }

            // Bottom padding
            height += ImGui.GetStyle().WindowPadding.Y;

            using var section = ImRaii.Child(title, new(0, height), false,
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.AlwaysUseWindowPadding);
            if (!section)
                return;

            using (ImRaii.PushColor(ImGuiCol.Text, Utils.ToUintABGR(EColor.YellowBright)))
            {
                ImGui.TextUnformatted(title);
            }

            ImGui.Separator();

            foreach (var point in bulletPoints)
            {
                ImGui.Bullet();
                ImGui.SameLine();
                ImGui.TextWrapped(point);
            }

            // Draw buttons at the bottom
            if (buttons != null && buttons.Length > 0)
            {
                ImGui.Spacing();

                bool firstButton = true;
                for (int i = 0; i < buttons.Length; i++)
                {
                    // Check if button should be visible
                    bool isVisible = buttons[i].IsVisible?.Invoke() ?? true;

                    if (!isVisible)
                        continue;

                    if (!firstButton)
                        ImGui.SameLine();

                    if (ImGui.Button($"{buttons[i].Label}##{title}_{i}"))
                    {
                        buttons[i].OnClick?.Invoke();
                    }

                    firstButton = false;
                }
            }
        }
    }
}
