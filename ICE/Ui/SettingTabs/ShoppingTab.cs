using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons;
using ICE.Config;
using Lumina.Excel.Sheets;

namespace ICE.Ui.SettingTabs
{
    internal class ShoppingTab
    {
        private static string ItemSearch = string.Empty;

        public static unsafe void Draw()
        {
            CheckConfigState();
            if (Task_BuyCosmoItems.CanPurchaseAnyItem())
            {
                ImGui.Text("您现在可以购买列表中的宇宙信用点物品！");
            }
            else
            {
                ImGui.Text("您当前的宇宙信用点/物品不足以购买任何商品（好吧，这只是测试）");
            }

            if (ImGui.Button("添加物品到清单"))
            {
                ImGui.OpenPopup("CosmocreditMateriaPopup");
            }

            ImGui.SetNextWindowSize(new Vector2(400, 0), ImGuiCond.Appearing);

            if (ImGui.BeginPopup("CosmocreditMateriaPopup"))
            {
                ImGui.SetNextItemWidth(380);
                ImGui.InputText("##Item Search", ref ItemSearch, 256);

                ImGui.Spacing();

                // Remove BeginChild and use table scrolling instead
                if (ImGui.BeginTable("Cosmo Materia Shop", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg, new Vector2(0, 250)))
                {
                    ImGui.TableSetupColumn("Icons", ImGuiTableColumnFlags.WidthFixed, 20);
                    ImGui.TableSetupColumn("Names", ImGuiTableColumnFlags.WidthStretch);

                    foreach (var item in Shop_Cosmocredits.CosmocreditShop)
                    {
                        var id = item.Key;
                        if (Svc.Data.GetExcelSheet<Item>().TryGetRow(id, out var itemInfo))
                        {
                            var name = itemInfo.Name.ToString();

                            if (!ItemSearch.IsNullOrWhitespace() && !name.ToLower().Contains(ItemSearch.ToLower()))
                            {
                                continue;
                            }

                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.PushID(id);
                            if (itemInfo.Icon is { } itemIcon && Svc.Texture.TryGetFromGameIcon((int)itemIcon, out var texture))
                            {
                                ImGui.Image(texture.GetWrapOrEmpty().Handle, new Vector2(20, 20));
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text($"{itemInfo.Name}");
                            if (ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Left))
                            {
                                AddItem(id);
                                C.Save();
                            }
                            ImGui.PopID();
                        }
                    }
                    ImGui.EndTable();
                }

                ImGui.EndPopup();
            }

            ImGui.Text($"订单数量: {C.CosmoShoppingOrder.Count}");

            if (ImGui.BeginTable("Current Shopping List", 9, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("上移");
                ImGui.TableSetupColumn("下移");
                ImGui.TableSetupColumn("名称");
                ImGui.TableSetupColumn("价格");
                ImGui.TableSetupColumn("类型");
                ImGui.TableSetupColumn("库存数量");
                ImGui.TableSetupColumn("购买数量");
                ImGui.TableSetupColumn("持续购买");
                ImGui.TableSetupColumn("移除");

                ImGui.TableHeadersRow();

                for (int i = 0; i < C.CosmoShoppingOrder.Count; i++)
                {
                    uint itemId = C.CosmoShoppingOrder[i];
                    var setting = C.CosmoShopping[itemId];
                    var itemInfo = Svc.Data.GetExcelSheet<Item>().GetRow(itemId);

                    ImGui.TableNextRow();

                    // Move to Top Button
                    ImGui.TableSetColumnIndex(0);
                    using (ImRaii.Disabled(i == 0))
                    {
                        if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUp, $"##drag_{itemId}"))
                        {
                            MoveItemUp(itemId);
                            C.Save();
                        }
                    }

                    // Drag Handle Column
                    ImGui.TableNextColumn();
                    if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowDown, $"##drag_{itemId}"))
                    {
                        MoveItemDown(itemId);
                        C.Save();
                    }

                    // Name
                    ImGui.TableNextColumn();
                    if (itemInfo.Icon is { } itemIcon && Svc.Texture.TryGetFromGameIcon((int)itemIcon, out var texture))
                    {
                        ImGui.Image(texture.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                        ImGui.SameLine();
                    }
                    ImGui.Text($"{itemInfo.Name}");

                    // Cost
                    ImGui.TableNextColumn();
                    if (Shop_Cosmocredits.CosmocreditShop.TryGetValue(itemId, out var shopInfo))
                    {
                        ImGui.Text($"{shopInfo.Cost}");
                    }

                    // Kind (you can add logic for this)
                    ImGui.TableNextColumn();
                    ImGui.Text("材料"); // Replace with actual kind logic

                    // Keep Amount
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(80);
                    var keepAmount = setting.KeepAmount;
                    if (ImGui.InputInt($"##keep_{itemId}", ref keepAmount))
                    {
                        setting.KeepAmount = keepAmount;
                        C.SaveDebounced();
                    }

                    // Buy Amount
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(80);
                    var buyAmount = setting.BuyAmount;
                    if (ImGui.InputInt($"##buy_{itemId}", ref buyAmount))
                    {
                        setting.BuyAmount = buyAmount;
                        C.SaveDebounced();
                    }

                    // Keep Buying
                    ImGui.TableNextColumn();
                    var keepBuying = setting.KeepBuying;
                    if (ImGui.Checkbox($"##keepbuying_{itemId}", ref keepBuying))
                    {
                        foreach (var enabled in C.CosmoShopping)
                        {
                            enabled.Value.KeepBuying = false;
                        }

                        setting.KeepBuying = keepBuying;
                        C.Save();
                    }

                    // Remove Button
                    ImGui.TableNextColumn();
                    if (ImGuiEx.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash, "##Remove Item"))
                    {
                        RemoveItem(itemId);
                        C.Save();
                    }
                }

                ImGui.EndTable();
            }
        }
        private static void AddItem(uint itemId)
        {
            if (C.CosmoShopping.ContainsKey(itemId))
                return;


            C.CosmoShopping[itemId] = new CosmoShoppingList();
            C.CosmoShoppingOrder.Add(itemId);
        }

        private static void RemoveItem(uint itemId)
        {
            C.CosmoShopping.Remove(itemId);
            C.CosmoShoppingOrder.Remove(itemId);
        }

        public static void MoveItemUp(uint itemId)
        {
            int index = C.CosmoShoppingOrder.IndexOf(itemId);
            if (index > 0)
            {
                C.CosmoShoppingOrder.RemoveAt(index);
                C.CosmoShoppingOrder.Insert(index - 1, itemId);
            }
        }

        public static void MoveItemDown(uint itemId)
        {
            int index = C.CosmoShoppingOrder.IndexOf(itemId);
            if (index >= 0 && index < C.CosmoShoppingOrder.Count - 1)
            {
                C.CosmoShoppingOrder.RemoveAt(index);
                C.CosmoShoppingOrder.Insert(index + 1, itemId);
            }
        }

        private static void MoveItemToTop(uint itemId)
        {
            int index = C.CosmoShoppingOrder.IndexOf(itemId);
            if (index > 0)
            {
                C.CosmoShoppingOrder.RemoveAt(index);
                C.CosmoShoppingOrder.Insert(0, itemId);
            }
        }

        public static void CheckConfigState()
        {
            if (C.CosmoShopping == null)
            {
                C.CosmoShopping = new();
                C.Save();
            }
            if (C.CosmoShoppingOrder == null)
            {
                C.CosmoShoppingOrder = new();
                C.Save();
            }
        }
    }
}