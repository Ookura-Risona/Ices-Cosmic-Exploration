using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Utilities;

public static partial class CosmicHelper
{
    public class CustomNotes
    {
        public string NoteInfo { get; set; }
        public float SPM { get; set; }
    };

    public static Dictionary<uint, CustomNotes> CustomMissionNotes =
        CreateMissionNotes();

    private static Dictionary<uint, CustomNotes> CreateMissionNotes()
    {
        var dict = new Dictionary<uint, CustomNotes>();

        // Basic Sinus A Ranks
        AddMissions(dict, 241f, "最佳每分钟技巧点任务(天气限定任务以外)", // Best Score Per Minute outside of weather missions
                    295, 115, 70, 205, 25, 340, 160, 250);

        // Dual Craft MIN/BTN
        AddMissions(dict, 256f,
                   "采集职业的冲分最佳 A 类任务, 同时兼顾对应的生产职业, 在两个月球探索地图中都是最佳的\n" + // Best A Rank for gathering, while also double dipping into crafters which is best for both worlds\n
                   "推荐银星提交, 因为可以节省制作时间", // Silver turnin is recommended for this, due to the time not spent on crafting
                   496, 497, 498, 502, 503, 504);

        // Dual Craft FSH
        AddMissions(dict, 228f,
                   "捕鱼人的冲分最佳 A 类任务, 同时兼顾对应的生产职业, 在两个月球探索地图中都是最佳的\n" + // Best A Rank for FISHING, while also double dipping into crafters which is best for both worlds\n
                   "强烈推荐银星提交, 因为可以节省制作时间与具有随机性的钓鱼时间, 银星汇报大约 228 技巧点/分钟, 金星汇报大约 152 技巧点/分钟", // Silver turnin is HIGHLY recommended for this, due to the time not spent on crafting and fish RNG. Silver ends up being ~228 SPM, VS. Gold being ~152 SPM
                   509);

        // Sinus Weather Missions
        AddMissions(dict, 490f,
                   "憧憬湾地图最佳每分钟技巧点任务, 理想情况下您应该尽可能专注于这些任务\n" + // Best Score Per Minute on Sinus. Ideally you would want to focus these as much as possible.\n
                   "以银星评价为目标完成这些任务, 这是您在 100 级装备下所能获得的最佳每分钟技巧点", // Aim to complete these on silver, that's the best SPM you'll get with the lv. 100 gear
                    31, 32, 76, 77, 121, 122, 166, 167, 211, 212, 256, 257, 301, 302, 346, 347);

        // Sinus A Rank Missions
        AddMissions(dict, 207f,
                   "如果您不做双职业任务(建议您做, 这些任务效率更低), 这是第二好的\n" + // If you're not doing the dual craft missions (you should be, this is worse), then this is the 2nd best \n
                   "以银星或金星评价为目标完成这些任务, 两者平均值大致相同", // Aim for silver or gold on this, they average about the same
                   387, 432);

        // Sinus Critical Missions
        AddMissions(dict, 406f,
                    "只要有机会, 就应以完成紧急探索任务为目标来获取技巧点", // Always aim to do criticals when you can for score
                    536, 537, 538, 539, 540, 541, 542, 543, 544);

        // Basic Phaenna
        AddMissions(dict, 336f, "最佳每分钟技巧点任务(天气限定任务以外)", // Best Score Per Minute out of weather missions.
                    569, 611, 653, 695, 737, 779, 821, 863);

        // Phaenna Sequence
        AddMissions(dict, 362f,
                   "如果您正在连续任务链中, 这组连续任务是最佳选择\n" + // This set of sequence missions is the best if you're chaining missions \n
                   "平均收益优于普通任务", // This averages out to being better than a normal mission.
                    580, 622, 664, 706, 748, 790, 832, 874);

        // Phaenna Weather
        AddMissions(dict, 281f,
                   "这是最佳的天气限定任务, 但个人认为不值得优先于普通任务。相关信息仍在此提供给您参考。", // Best Weather missions that are here, not really worth doing over the basic missions IMHO. But The info is here for you
                   573, 615, 657, 699, 741, 783, 825, 867);

        AddMissions(dict, 379f,
                   "紧急探索任务总是值得为了技巧点而去完成", // Criticals are always worth doing for score
                    1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 
                    1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 1030);



        return dict;
    }

    private static void AddMissions(Dictionary<uint, CustomNotes> dict, float spm, string note, params uint[] missionIds)
    {
        foreach (var id in missionIds)
        {
            dict[id] = new CustomNotes { SPM = spm, NoteInfo = note };
        }
    }
}
