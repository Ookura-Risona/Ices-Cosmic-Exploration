using System.Collections.Generic;

namespace ICE.ConfigFiles;

public partial class Config
{
    public bool MoonSprint { get; set; } = true;
    public uint MountId { get; set; } = 0;
    public string MountName { get; set; } = "随机坐骑";
    public float MountRadius { get; set; } = 15.0f;
    public float DismountRadius { get; set; } = 7.0f;
    public bool UseMountOutsideMission { get; set; } = true;
    public bool UseMountInMission { get; set; } = true;
    public float LeftColumnWidth { get; set; } = 300f;
    public bool PlaySoundAlert { get; set; } = false;
    public float SoundVolume { get; set; } = 0.5f;
    public int TimeHistoryLimit { get; set; } = 100;
    public bool RemoveStellarStatus { get; set; } = false;
    public bool ShowSPM { get; set; } = false;
    public bool StartUponEnterMoon { get; set; } = false;
    public bool PersonalReturnSpot { get; set; } = false;
    public bool AutoAntiAFK { get; set; } = true; // 新增: 自动切换离开状态 默认启用
    public bool OizysBagBuyEnabled { get; set; } = false; // 临时: 俄匊斯资源包购买开关
    public int OizysBagBuyAtAmount { get; set; } = 4500; // 临时: 俄匊斯资源包开始购买触发数量，默认 4500
    public int OizysBagBuyAmount { get; set; } = 0 ; // 临时: 俄匊斯资源包购买数量
    public int OizysBagKeepAmount { get; set; } = 0; // 临时: 俄匊斯资源包保留数量
    public bool OizysBagKeepBuying { get; set; } = false; // 临时: 俄匊斯资源包是否持续购买 // 主分支不会有这些功能，仅为临时测试
    public Dictionary<uint, Vector3> CrafterLocations { get; set; } = new();
    public List<MissionCommand> PostMissionCommands { get; set; } = new();

    public class MissionCommand
    {
        public required string command { get; set; }
        public int Delay { get; set; } = 0;
    }
}
