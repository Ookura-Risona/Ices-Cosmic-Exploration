using ECommons.GameHelpers;
using ICE.ConfigFiles;
using ICE.Utilities.Cosmic_Helper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.ConfigFiles.Config;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_BuyOizysBags
    {
        private const uint OizysBagItemId = 50414;
        private static int _pendingBuyAmount = 0;

        public static void Enqueue()
        {
            P.TaskManager.EnqueueMulti
                (
                    new(OizysBags_PathToVendor, "Pathing to the OizysBags vendor"),
                    new(TalkToOizysBagsNPC, "Talking to the OizysBags NPC to start the buying process"),
                    new(SelectShop, "Selecting the shop entry we want to go to"),
                    new(BuyItems, "Buying items from the vendor", Utils.TaskConfig),
                    new(CloseShop, "Closing the shop menu", Utils.TaskConfig)
                );
        }

        private static unsafe bool? OizysBags_PathToVendor()
        {
            // 寻路前，检查商店是否已经打开，若已打开则关闭
            if (GenericHelpers.TryGetAddonMaster<ShopExchangeCurrency>("ShopExchangeCurrency", out var shop) && shop.IsAddonReady)
            {
                if (EzThrottler.Throttle("ClosingShopExchangeCurrency"))
                    shop.Addon->Close(true);

                return false;
            }

            string handle = "[Task_OizysBags: PathTo]";
            var zoneId = Player.Territory.RowId;
            var npcEntry = NpcData.MoonNpcs[zoneId].Where(x => x.type == NpcData.NpcType.Bag).FirstOrDefault();

            if (npcEntry != null)
            {
                Vector3 randomPos = NpcData.GetRandomPointInCircle(npcEntry.Location_Circle, 0.5f);
                if (!Task_NavmeshMove.Task_NavTo(randomPos, distance: 6, npcLoc: npcEntry.Location_Npc).Value)
                {
                    if (EzThrottler.Throttle("Repair move message", 1000))
                        IceLogging.Verbose($"Pathing to OizysBags NPC. Current distance: {Player.DistanceTo(npcEntry.Location_Npc)}", handle);
                }
                else
                {
                    IceLogging.Debug("We're close enough to the OizysBags npc! Continuing on", handle);
                    return true;
                }
            }
            else
            {
                if (EzThrottler.Throttle("Error message: NPC", 5000))
                    IceLogging.Error("Hey! We don't have this npc coded yet, which means I forgot bout it, could you let me know\n" +
                                     $"Planet Territory ID: {Player.Territory.RowId}", handle);
            }

            return false;
        }

        private static bool? TalkToOizysBagsNPC()
        {
            if (GenericHelpers.TryGetAddonMaster<SelectString>("SelectString", out var SelectString) && SelectString.IsAddonReady)
            {
                IceLogging.Info("SelectString is visible! Time to shop");
                return true;
            }
            else
            {
                var researchId = NpcData.MoonNpcs[Player.Territory.RowId].Where(x => x.type == NpcData.NpcType.Bag).FirstOrDefault().NpcId;

                Utils.TryGetObjectByDataId(researchId, out var researchNpc);
                if (EzThrottler.Throttle("Interacting with researchingway"))
                {
                    Utils.TargetgameObject(researchNpc);
                    Utils.InteractWithObject(researchNpc);
                }
            }

            return false;
        }
        private static bool? SelectShop()
        {
            const string tag = "[SelectShop]";
            IceLogging.Debug($"start to SelectShop", tag);

            if (GenericHelpers.TryGetAddonMaster<SelectString>("SelectString", out var ss)
                && ss.IsAddonReady)
            {
                if (ss.EntryCount <= 0)
                {
                    IceLogging.Error($"{tag} SelectString has no entries!", tag);
                    return false;
                }

                if (EzThrottler.Throttle("OizysBag_SelectShop", 500))
                {
                    var entry = ss.Entries[0];
                    IceLogging.Debug($"{tag} Selecting entry: {entry.Text}", tag);
                    entry.Select();
                }

                return false;
            }
            else if (GenericHelpers.TryGetAddonMaster<ShopExchangeCurrency>("ShopExchangeCurrency", out var shop)
                && shop.IsAddonReady)
            {
                IceLogging.Debug($"{tag} ShopExchangeCurrency detected, selection complete.", tag);
                return true;
            }

            return false;
        }
        private static unsafe bool? CloseShop()
        {
            if (GenericHelpers.TryGetAddonMaster<ShopExchangeCurrency>("ShopExchangeCurrency", out var shopExchange) && shopExchange.IsAddonReady)
            {
                if (EzThrottler.Throttle("Close Shop"))
                    shopExchange.Addon->Close(true);
                return false;
            }
            else
                return true;
        }

        private static bool? BuyItems()
        {
            if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var yesno) && yesno.IsAddonReady)
            {
                if (EzThrottler.Throttle("OizysBag_ConfirmBuy", 500))
                {
                    yesno.Yes();

                    if (_pendingBuyAmount > 0)
                    {
                        C.OizysBagBuyAmount -= _pendingBuyAmount;
                        if (C.OizysBagBuyAmount < 0)
                            C.OizysBagBuyAmount = 0;

                        C.Save();
                    }

                    _pendingBuyAmount = 0;
                }

                return false;
            }

            if (!GenericHelpers.TryGetAddonMaster<ShopExchangeCurrency>("ShopExchangeCurrency", out var shop) || !shop.IsAddonReady)
                return false;

            uint currency = shop.CurrencyAmount;

            if (currency < C.OizysBagBuyAtAmount)
                return true;

            var item = shop.BasicShopItems.FirstOrDefault(x => x.ItemId == OizysBagItemId);
            if (item == null)
                return true;

            int maxAffordable = (int)(currency / item.CostAmount);
            if (maxAffordable <= 0)
                return true;

            int targetBuyAmount = C.OizysBagBuyAmount;

            PlayerHelper.GetItemCount(OizysBagItemId, out int currentCount);
            int targetKeepAmount = Math.Max(0, C.OizysBagKeepAmount - currentCount);

            int targetKeepBuying = C.OizysBagKeepBuying ? int.MaxValue : 0;

            int target = 0;
            if (targetBuyAmount > 0)
                target = targetBuyAmount;
            else if (targetKeepAmount > 0)
                target = targetKeepAmount;
            else if (targetKeepBuying > 0)
                target = targetKeepBuying;

            if (target <= 0)
                return true;

            int buyAmount = Math.Min(target, maxAffordable);
            buyAmount = Math.Min(buyAmount, 99);

            if (EzThrottler.Throttle("OizysBag_SelectItem", 500))
            {
                item.Select(buyAmount);
                _pendingBuyAmount = buyAmount;
            }

            return false;
        }

        public static bool CanPurchaseAnyItem()
        {
            if (C.OizysBagBuyAmount > 0)
                return true;

            PlayerHelper.GetItemCount(OizysBagItemId, out int currentCount);
            if (C.OizysBagKeepAmount > currentCount)
                return true;

            if (C.OizysBagKeepBuying)
                return true;

            return false;
        }
    }
}
