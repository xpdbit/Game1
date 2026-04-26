using System;
using System.Collections.Generic;
using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat.Commands
{
    /// <summary>
    /// 攻击命令
    /// 执行玩家或敌人的攻击行动
    /// </summary>
    [Serializable]
    public class AttackCommand : ICombatCommand
    {
        private int _damageDealt;
        private int _healingDone;
        private bool _wasCritical;
        private CombatantData _attackerSnapshot;
        private CombatantData _defenderSnapshot;

        public string Description => "攻击";
        public bool IsPlayerAction { get; private set; }
        public int DamageDealt => _damageDealt;
        public int HealingDone => _healingDone;
        public bool WasCritical => _wasCritical;

        public AttackCommand(bool isPlayerAction)
        {
            IsPlayerAction = isPlayerAction;
        }

        public void Execute(CombatContext context)
        {
            // 保存攻击前状态快照（用于Undo）
            _attackerSnapshot = CloneCombatant(IsPlayerAction ? context.playerCombatant : context.enemyCombatant);
            _defenderSnapshot = CloneCombatant(IsPlayerAction ? context.enemyCombatant : context.playerCombatant);

            CombatantData attacker = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;
            CombatantData defender = IsPlayerAction ? context.enemyCombatant : context.playerCombatant;

            // 计算伤害
            _damageDealt = CalculateDamage(attacker, defender, out _wasCritical);
            _healingDone = 0;

            // 应用伤害
            defender.TakeDamage(_damageDealt);
        }

        public void Undo(CombatContext context)
        {
            // 恢复攻击者状态
            CombatantData attacker = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;
            CombatantData defender = IsPlayerAction ? context.enemyCombatant : context.playerCombatant;

            attacker.hp = _attackerSnapshot.hp;
            attacker.isDefending = _attackerSnapshot.isDefending;

            // 恢复防御者状态
            defender.hp = _defenderSnapshot.hp;
            defender.isDefending = _defenderSnapshot.isDefending;
        }

        private int CalculateDamage(CombatantData attacker, CombatantData defender, out bool isCritical)
        {
            isCritical = false;

            // 检查是否暴击
            if (UnityEngine.Random.value < attacker.critChance)
            {
                isCritical = true;
            }

            // 计算基础伤害
            float baseDamage = attacker.damage;

            // 暴击加成
            if (isCritical)
            {
                baseDamage *= attacker.critDamageMultiplier;
            }

            // 护甲减伤
            float armorReduction = defender.isDefending ? defender.armor * 0.5f : defender.armor;
            float finalDamage = System.Math.Max(1, baseDamage - armorReduction);

            return UnityEngine.Mathf.RoundToInt(finalDamage);
        }

        private CombatantData CloneCombatant(CombatantData source)
        {
            return new CombatantData
            {
                name = source.name,
                hp = source.hp,
                maxHp = source.maxHp,
                armor = source.armor,
                damage = source.damage,
                attack = source.attack,
                defense = source.defense,
                critChance = source.critChance,
                critDamageMultiplier = source.critDamageMultiplier,
                isDefending = source.isDefending,
                activeBuffs = new List<string>(source.activeBuffs),
                activeDebuffs = new List<string>(source.activeDebuffs)
            };
        }
    }
}