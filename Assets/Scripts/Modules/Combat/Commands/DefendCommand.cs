using System;
using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat.Commands
{
    /// <summary>
    /// 防御命令
    /// 执行玩家或敌人的防御行动
    /// </summary>
    [Serializable]
    public class DefendCommand : ICombatCommand
    {
        private bool _wasDefending;

        public string Description => "防御";
        public bool IsPlayerAction { get; private set; }
        public int DamageDealt => 0;
        public int HealingDone => 0;
        public bool WasCritical => false;

        public DefendCommand(bool isPlayerAction)
        {
            IsPlayerAction = isPlayerAction;
        }

        public void Execute(CombatContext context)
        {
            CombatantData combatant = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;

            // 保存之前的状态
            _wasDefending = combatant.isDefending;

            // 设置防御状态
            combatant.isDefending = true;
        }

        public void Undo(CombatContext context)
        {
            CombatantData combatant = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;
            combatant.isDefending = _wasDefending;
        }
    }
}