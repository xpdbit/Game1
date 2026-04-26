using System;

namespace Game1.Modules.Combat.State
{
    /// <summary>
    /// 战斗阶段枚举
    /// 定义战斗状态机的各个阶段
    /// </summary>
    public enum CombatPhase
    {
        /// <summary>空闲状态，战斗未开始</summary>
        Idle,

        /// <summary>战斗准备阶段</summary>
        Preparing,

        /// <summary>玩家回合</summary>
        PlayerTurn,

        /// <summary>敌人回合</summary>
        EnemyTurn,

        /// <summary>动画播放阶段</summary>
        Animating,

        /// <summary>胜利阶段</summary>
        Victory,

        /// <summary>失败阶段</summary>
        Defeat
    }
}