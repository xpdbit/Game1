using System;
using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat.Commands
{
    /// <summary>
    /// 治疗命令
    /// 执行玩家或敌人的治疗行动
    /// </summary>
    [Serializable]
    public class HealCommand : ICombatCommand
    {
        private int _healingDone;
        private int _hpBefore;
        private bool _isDefendingBefore;

        public string Description => "治疗";
        public bool IsPlayerAction { get; private set; }
        public int DamageDealt => 0;
        public int HealingDone => _healingDone;
        public bool WasCritical => false;

        /// <summary>
        /// 创建治疗命令
        /// </summary>
        /// <param name="isPlayerAction">是否为玩家行动</param>
        public HealCommand(bool isPlayerAction)
        {
            IsPlayerAction = isPlayerAction;
        }

        public void Execute(CombatContext context)
        {
            CombatantData target = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;

            // 保存治疗前状态
            _hpBefore = target.hp;
            _isDefendingBefore = target.isDefending;

            // 计算治疗量（基于最大生命值）
            int baseHealAmount = UnityEngine.Mathf.RoundToInt(target.maxHp * 0.3f);

            // 应用治疗
            target.Heal(baseHealAmount);

            // 计算实际治疗量
            _healingDone = target.hp - _hpBefore;
        }

        public void Undo(CombatContext context)
        {
            CombatantData target = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;

            // 恢复HP
            target.hp = _hpBefore;

            // 恢复防御状态
            target.isDefending = _isDefendingBefore;
        }
    }
}