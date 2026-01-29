using System.Collections.Generic;

namespace ICE.ConfigFiles;

public partial class Config
{
    public bool Tmp_OizysDronebitBuyItems { get; set; } = false; // 购买开关
    public int Tmp_OizysDronebitBuyAtAmount { get; set; } = 4500; // 起购货币数量
    public int Tmp_OizysDronebitBuyAmount { get; set; } = 0; // 购买数量
    public int Tmp_OizysDronebitKeepAmount { get; set; } = 0; // 目标库存
    public bool Tmp_OizysDronebitKeepBuying { get; set; } = false; // 持续购买
}
