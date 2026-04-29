namespace Game1.Events.Effect
{
    /// <summary>
    /// 统一效果类型枚举 - 用于 Unified Effect System
    /// </summary>
    public enum EffectType
    {
        /// <summary>
        /// 金币增减效果
        /// </summary>
        Gold,

        /// <summary>
        /// 生命值增减效果
        /// </summary>
        HP,

        /// <summary>
        /// 经验值增减效果
        /// </summary>
        EXP,

        /// <summary>
        /// 物品获取或移除效果
        /// </summary>
        Item,

        /// <summary>
        /// 游戏标志设置效果
        /// </summary>
        Flag,

        /// <summary>
        /// 模块添加效果（物品/技能等模块的添加）
        /// </summary>
        Module,

        /// <summary>
        /// 战斗触发效果
        /// </summary>
        Combat,

        /// <summary>
        /// 伤害效果（带缩放系数）
        /// </summary>
        Damage,

        /// <summary>
        /// 治疗效果
        /// </summary>
        Heal,

        /// <summary>
        /// 增益效果
        /// </summary>
        Buff,

        /// <summary>
        /// 功能解锁效果
        /// </summary>
        Unlock
    }
}
