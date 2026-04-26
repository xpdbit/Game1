using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat.Commands
{
    /// <summary>
    /// 战斗命令接口
    /// 所有战斗行动（攻击、防御、技能等）都实现此接口
    /// </summary>
    public interface ICombatCommand
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="context">战斗上下文</param>
        void Execute(CombatContext context);

        /// <summary>
        /// 撤销命令
        /// </summary>
        /// <param name="context">战斗上下文</param>
        void Undo(CombatContext context);

        /// <summary>
        /// 获取命令描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 是否为玩家行动
        /// </summary>
        bool IsPlayerAction { get; }

        /// <summary>
        /// 获取此命令造成的伤害
        /// </summary>
        int DamageDealt { get; }

        /// <summary>
        /// 获取此命令的治疗量
        /// </summary>
        int HealingDone { get; }

        /// <summary>
        /// 是否为暴击
        /// </summary>
        bool WasCritical { get; }
    }
}