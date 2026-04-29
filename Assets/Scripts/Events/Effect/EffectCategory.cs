#nullable enable
using System.ComponentModel;

namespace Game1.Events.Effect
{
    /// <summary>
    /// 效果类别，区分效果的用途方向。
    /// </summary>
    public enum EffectCategory
    {
        /// <summary>奖励类效果，给玩家增加收益（金币、经验、物品等）。</summary>
        [Description("奖励")]
        Reward,

        /// <summary>消耗类效果，玩家需要付出代价（花费金币、消耗物品等）。</summary>
        [Description("消耗")]
        Cost,

        /// <summary>状态类效果，修改游戏状态（设置标志、添加模块、解锁功能等）。</summary>
        [Description("状态")]
        State,
    }
}
