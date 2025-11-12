using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ICE.Ui.DebugWindowTabs;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using ICE.Sounds;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Utilities.GatheringHelper.GatheringUtil;
using ECommons.Automation;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_Fishing
    {
        // Something to note. 42, 43, 85 are the conditions that you get while you're fishing
        // 43 and 85 are active while you're fishing
        // 42 is active when reeling in a fish
        // Something to consider, start fishing... (that's condition 42 when you start)
        // Whenever all the conditions are cleared, check the inventory for the frame, see if you have enough/meet the score

        private static FishingDebug _fishingDebug = null;

        public static void Enqueue()
        {
            // think the process should be:
            // check score
            // if score not complete, check if can craft
            // if craft not required, fish
            // wait for fishing to be done

            // 追加检查任务是否正在进行中，重走处理提交任务的流程，这是为了处理 ICE 插件被其他钓鱼插件汇报任务所导致的混乱
            // 其他几个任务也没有任务中检查... 出问题再修吧
            var id = CosmicHelper.CurrentLunarMission;

            if (id == 0)
            {
                if (P.AutoHook.Installed)
                {
                    P.AutoHook.DeleteAllAnonymousPresets();
                }

                if (Mission_Settings.StopAfterCurrent)
                {
                    IceLogging.Debug($"Stop after current was enabled. Stopping now", "[Fishing Patch]");
                    SchedulerMain.State = IceState.Idle;
                    Mission_Settings.StopAfterCurrent = false;
                    P.TaskManager.Tasks.Clear();

                    if (C.RemoveAfterGold)
                    {
                        P.TaskManager.Enqueue(() => Task_TurninMission.GoldCheck());
                    }
                    if (C.PlaySoundAlert)
                    {
                        _ = SoundPlayer.PlaySoundAsync();
                    }
                }
                else
                {
                    IceLogging.Debug($"Stop after current wasn't enabled. Grabbing another mission", "[Fishing Patch]");
                    SchedulerMain.State = IceState.Start;
                    if (C.RemoveAfterGold)
                    {
                        P.TaskManager.Enqueue(() => Task_TurninMission.GoldCheck());
                    }
                }

                //SchedulerMain.State = IceState.Start;
                //IceLogging.Debug("Restarting...", "[Fishing Patch]");
                return;
            }
            else
            {
                Task_TurninMission.PreviousMissionId = id;
                P.TaskManager.Enqueue(() => Task_CheckScore.Fish());
                P.TaskManager.Enqueue(() => FishingCheck());
            }
        }

        private static int BaitCounter = 0;

        private static unsafe bool? FishingCheck()
        {
            if (_fishingDebug == null)
            {
                _fishingDebug = new FishingDebug();
            }

            if (Player.Mounted)
            {
                Utils.Dismount();
                return false;
            }

            string handle = "[Standard Fishing: Fishing Check]";
            if (EzThrottler.Throttle("Throttling intro message", 1000))
            {
                IceLogging.Debug("Checking to see where we need to be here", handle);
            }
            bool hasBait = false;

            if (CosmicHelper.CurrentBait == 0)
            {
                if (EzThrottler.Throttle("Equipping bait"))
                {
                    foreach (var bait in GatheringUtil.MoonBaits)
                    {
                        foreach (var baitId in bait.Value)
                        {
                            if (PlayerHelper.GetItemCount(baitId, out var count) && count > 0)
                            {
                                P.AutoHook.SwapBaitById(baitId);
                                IceLogging.Debug($"Telling it to equip bait ID: {baitId}", handle);
                                return false;
                            }
                        }
                    }

                    IceLogging.Info("If we've gotten here, that means we're out of bait. Proceeding to turnin/abandon the mission");
                    SchedulerMain.State = IceState.AbandonMission;
                    P.TaskManager.Tasks.Clear();
                    return true;
                }
                return false;
            }

            // little check here for seeing if we have any baits
            foreach (var bait in GatheringUtil.MoonBaits)
            {
                foreach (var baitId in bait.Value)
                {
                    if (PlayerHelper.GetItemCount(baitId, out var count) && count > 0)
                    {
                        if (EzThrottler.Throttle("Throttling bait message", 1000))
                            IceLogging.Debug("We have the bait! Continuing onwards");
                        hasBait = true;
                        break;
                    }
                }
            }

            if (!hasBait)
            {
                IceLogging.Info("If we've gotten here, that means we're out of bait. Proceeding to turnin/abandon the mission");
                SchedulerMain.State = IceState.AbandonMission;
                P.TaskManager.Tasks.Clear();
                return true;
            }
            else if (!Svc.Condition[ConditionFlag.Gathering])
            {
                if (!_fishingDebug.IsFishable())
                {
                    if (_fishingDebug.FindFishableLocation(out var fishablePos, searchSteps:64))
                    {
                        IceLogging.Info("We're not in a fishable angle, so going to face one", handle);
                        P.TaskManager.Tasks.Clear();
                        P.TaskManager.Enqueue(() => FacePosition(fishablePos.Value));
                        return true;
                    }
                    else
                    {
                        IceLogging.Debug("Our current fishing position isn't viable. So going to move to the next fishing spot");
                        var mission = CosmicHelper.CurrentMissionInfo;
                        var flag = mission.MapPosition;
                        var territoryId = mission.TerritoryId;

                        var nextFishingSpot = GetNextFishingSpot(territoryId, flag, Player.Position);
                        if (nextFishingSpot != null)
                        {
                            IceLogging.Info($"We found another fishing spot to move to! {nextFishingSpot.FishingSpot} | moving to it");
                            P.TaskManager.Tasks.Clear();
                            P.TaskManager.Enqueue(() => InitiateMoving(nextFishingSpot.FishingSpot), "Vnav moving to fishing");
                            return true;
                        }
                    }
                }
                else if (EzThrottler.Throttle("Starting to fish", 1000))
                {
                    PlayerHandlers.UpdateFishingPluginStatus();
                    IceLogging.Debug("Telling it to start fishing", handle);
                    if (C.AutoFisherCast)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Action, 289); // 任务中自动抛竿
                    }
                    else if (!C.AutoFisherCast && C.MissFisherStartingFix && PlayerHandlers.IsMissfisherLoaded && !PlayerHandlers.IsAutohookLoaded)
                    {
                        Chat.ExecuteCommand("/mf cosmic");
                        //DuoLog.Debug("[测试] 尝试为MF启动预设");
                    }
                }
                else if (EzThrottler.Throttle("Adding counter for bait not equipped"))
                {
                    if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Collectables) && !PlayerHelper.HasStatusId(805))
                    {


                        if (EzThrottler.Throttle("Attempting to turn on collectability"))
                            if (!PlayerHandlers.IsMissfisherLoaded && PlayerHandlers.IsAutohookLoaded)
                            {
                                Svc.Commands.ProcessCommand("/ahstart");
                            }
                            else if (PlayerHandlers.IsMissfisherLoaded && !PlayerHandlers.IsAutohookLoaded)
                            {
                                ActionManager.Instance()->UseAction(ActionType.Ability, 4101);
                            }
                            else
                            {
                                PluginLog.Debug("You haven't installed any fishing plugins!");
                            }
                          return false;
                    }

                    BaitCounter++;
                    IceLogging.Debug($"Adding 1 to the counter. Counter is at: {BaitCounter}");
                    if (BaitCounter >= 2)
                    {
                        /*string message = "嘿！你的下坐骑距离设置太低了(可能设置为 0)。把它改成大约 10？也许吧。反正不要设为 0。这样可以避免这种情况再次发生";
                        IceLogging.ChatError(message, "[I.C.E. Fishing]");*/
                        IceLogging.Debug($"Fishing task encountered obstacles, possibly due to dismount setting too low or other conflicts.");

                        foreach (var bait in GatheringUtil.MoonBaits)
                        {
                            foreach (var baitId in bait.Value)
                            {
                                if (PlayerHelper.GetItemCount(baitId, out var count) && count > 0)
                                {
                                    P.AutoHook.SwapBaitById(baitId);
                                    IceLogging.Debug($"Telling it to equip bait ID: {baitId}", handle);
                                    return false;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            else
            {
                // Means we are fishing, all we need to do is enable autohook then wait for us to get the amount of fish we need
                P.AutoHook.SetPluginState(true);
                IceLogging.Info("We're starting to fish. So kicking it over to checking the fish items", handle);
                P.TaskManager.Insert(() => WaitToStartFishing(), "Waiting till we actually start fishing", Utils.TaskConfig);
                BaitCounter = 0;
                return true;
            }
        }

        private static unsafe bool? WaitToStartFishing()
        {
            if (Svc.Condition[ConditionFlag.Fishing])
            {
                IceLogging.Info("We've started fishing, just going to wait for it to finish", "[Fishing: Waiting]");
                P.TaskManager.Insert(() => FinishFishing(), "Waiting for fishing to finish", Utils.TaskConfig);
                return true;
            }
            if (!Svc.Condition[ConditionFlag.Gathering])
            {
                IceLogging.Info("Somehow, we've gotten here and we're not fishing?? Going to check the score for a timer check", "[Fishing: Waiting]");
                P.TaskManager.Tasks.Clear();
                return true;
            }

            return false;
        }

        private static unsafe bool? FinishFishing()
        {
            if (!Svc.Condition[ConditionFlag.Fishing])
            {
                IceLogging.Info("We're done fishing, time to go back to the score check", "[Fishing: Finished]");
                return true;
            }

            return false;
        }

        public static unsafe bool? FacePosition(Vector3 pos, float tolerance = 0.1f)
        {
            float currentRotation = Player.Rotation;

            // If rotation is still changing, wait for it to stabilize
            if (Math.Abs(Player.Rotation - currentRotation) > tolerance)
            {
                return false;
            }

            Vector3 direction = pos - Player.Position;
            float targetRotation = (float)Math.Atan2(direction.X, direction.Z);

            float angleDifference = GetShortestAngleDifference(Player.Rotation, targetRotation);

            if (Math.Abs(angleDifference) < tolerance)
            {
                return true;
            }

            if (EzThrottler.Throttle("Facing toward the fishing hole"))
            {
                var fwk = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();

                var autoRotateConfig = fwk->SystemConfig.GetConfigOption((uint)ConfigOption.AutoFaceTargetOnAction);
                var autoRotateOriginal = autoRotateConfig->Value.UInt;

                autoRotateConfig->Value.UInt = 1;

                IceLogging.Debug($"Telling the game to face you to: {pos}");
                Vector3 temp = pos;
                ActionManager.Instance()->AutoFaceTargetPosition(&temp);

                autoRotateConfig->Value.UInt = autoRotateOriginal;
            }

            return false;
        }
        public static float GetShortestAngleDifference(float currentAngle, float targetAngle)
        {
            float difference = targetAngle - currentAngle;

            // Normalize to [-π, π] for shortest path
            while (difference > Math.PI) difference -= (float)(2 * Math.PI);
            while (difference < -Math.PI) difference += (float)(2 * Math.PI);

            return difference;
        }
        public static FisherSpotInfo? GetNextFishingSpot(uint zone, Vector2 flag, Vector3 playerPosition)
        {
            // Check if the zone and flag exist
            if (!MoonFishingLocations.TryGetValue(zone, out var zoneData) ||
                !zoneData.TryGetValue(flag, out var spots) ||
                spots.Count == 0)
            {
                return null;
            }

            // Find the index of the spot close to the player (within distance of 2)
            int currentIndex = -1;
            for (int i = 0; i < spots.Count; i++)
            {
                float distance = Vector3.Distance(playerPosition, spots[i].FacePosition);
                if (distance < 2f)
                {
                    currentIndex = i;
                    break;
                }
            }

            // If a close spot was found, return the next one (cycling back to 0 if at the end)
            if (currentIndex != -1)
            {
                int nextIndex = (currentIndex + 1) % spots.Count;
                return spots[nextIndex];
            }

            // If no close spot found, return the first entry
            return spots[0];
        }
        public static bool? InitiateMoving(Vector3 fishingPos)
        {
            if (P.Navmesh.IsRunning())
            {
                P.TaskManager.Enqueue(() => !P.Navmesh.IsRunning());
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Navmesh movement"))
                {
                    P.Navmesh.PathfindAndMoveTo(fishingPos, false);
                }
                return false;
            }
        }
    }
}
