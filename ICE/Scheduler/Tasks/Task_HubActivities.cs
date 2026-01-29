using ECommons.GameHelpers;
using ICE.Utilities.Cosmic_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler
{
    internal static class Task_HubActivities
    {
        public static bool RepairNpc = false;
        public static bool RelicTurnin = false;
        public static bool CosmoBuy = false;
        public static bool CanGamba = false;
        public static bool OizysBagBuy = false;
        private static Vector3 craftingSpot = Vector3.Zero;

        public static void Enqueue()
        {
            P.TaskManager.Enqueue(RegisterCraftingPosition, "Registering crafting position for later");
            P.TaskManager.Enqueue(Task_Repair.HubCheck, "Checking to see if we're in hub area");
            if (RepairNpc)
            {
                P.TaskManager.EnqueueMulti
                (
                    new(() => IceLogging.Info("Starting repair task at the npc", "Task_HubActivities")),
                    new(Task_Repair.Repair_PathTo, "Pathing to the repair NPC"),
                    new(Task_Repair.RepairAtNpc, "Repairing at the NPC Vendor"),
                    new(Task_Repair.CloseRepair, "Closing the repair window")
                );
            }
            if (RelicTurnin)
            {
                P.TaskManager.Enqueue(() => IceLogging.Info("Starting Relic Turnin task at the npc", "Task_HubActivities"));
                Task_RelicTurnin.Enqueue();
                P.TaskManager.Enqueue(() => IceLogging.Info("Task_Relic turnin is Complete"));
            }
            if (CosmoBuy)
            {
                P.TaskManager.Enqueue(() => IceLogging.Info("Starting Relic Turnin task at the npc", "Task_HubActivities"));;
                Task_BuyCosmoItems.Enqueue();
            }
            if (OizysBagBuy)
            {
                P.TaskManager.Enqueue(() => IceLogging.Info("Starting the Oizys Bags purchasing task at the npc", "Task_HubActivities"));
                Task_BuyOizysBags.Enqueue();
            }
            if (CanGamba)
            {
                P.TaskManager.Enqueue(() => IceLogging.Info("Starting Gamba task at the npc", "Task_HubActivities"));
                Task_Gamba.Enqueue();
            }
            P.TaskManager.EnqueueMulti
            (
                new(() => ResetAll(), "Setting all task to false"),
                new(() => IceLogging.Info("Checking to see if we need to path back to the spot")),
                new(PathBackToCraftingSpot, "Pathing back to our crafting spot", Utils.TaskConfig),
                new(() => SchedulerMain.State = IceState.GrabMission, "Swapping back to start")
            );
        }

        public static bool? RegisterCraftingPosition()
        {
            if (CosmicHelper.CrafterJobList.Contains((uint)Player.Job))
                craftingSpot = Player.Position;

            return true;
        }

        public static unsafe bool? PathBackToCraftingSpot()
        {
            // 脱离 ShopExchangeCurrency 窗口卡死，因为执行间隔问题窗口有很大可能再次打开导致卡死，这是必要的。
            // 并且这个问题会发生在所有需要打开交易窗口的基地任务中，阻塞角色移动寻路
            if (GenericHelpers.TryGetAddonMaster<ShopExchangeCurrency>("ShopExchangeCurrency", out var shop) && shop.IsAddonReady)
            {
                if (EzThrottler.Throttle("ClosingShopExchangeCurrency"))
                    shop.Addon->Close(true);

                return false;
            }

            if (CosmicHelper.CrafterJobList.Contains((uint)Player.Job))
            {
                // 临时修复: 使用制作返回点，将忽略此任务保存的 craftingSpot 字段作为返回点
                var territory = Player.Territory.RowId;
                if (C.PersonalReturnSpot)
                {
                    if (C.CrafterLocations.TryGetValue(territory, out var location))
                    {
                        if (!Task_NavmeshMove.Task_NavTo(location).Value)
                        {
                            if (EzThrottler.Throttle("Log message", 1000))
                                IceLogging.Debug("Moving to crafting spot");

                            return false;
                        }
                        else
                        {
                            IceLogging.Debug("We're at the spot for crafter location!");
                            return true;
                        }
                    }
                    else
                    {
                        IceLogging.Debug("No location is set for this place, so continuing on");
                        return true;
                    }
                }
                else
                {
                    return true;
                }

                /*if (!Task_NavmeshMove.Task_NavTo(craftingSpot, true, 1, false).Value)
                {
                    return false;
                }
                else
                {
                    IceLogging.Debug("We're back at our spot, so continuing on");
                    return true;
                }*/
            }
            else
            {
                IceLogging.Info($"We're not on a crafting job. (Allegedly) which means that we don't need to path back | Player Job: {(uint)Player.Job}");
                return true;
            }
        }

        private static bool? ResetAll()
        {
            IceLogging.Info("Resetting all hub task to false");

            RepairNpc = false;
            RelicTurnin = false;
            CosmoBuy = false;
            CanGamba = false;
            OizysBagBuy = false;

            return true;
        }
    }
}
