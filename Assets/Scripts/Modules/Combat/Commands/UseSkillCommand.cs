using System;
using System.Collections.Generic;
using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat.Commands
{
    /// <summary>
    /// 使用技能命令
    /// 执行玩家或敌人使用技能的行动
    /// </summary>
    [Serializable]
    public class UseSkillCommand : ICombatCommand
    {
        private string _skillId;
        private string _skillName;
        private int _damageDealt;
        private int _healingDone;
        private bool _wasCritical;
        private List<BuffEffect> _appliedBuffs;
        private Dictionary<string, int> _targetHpBefore;
        private bool _isDefendingBefore;

        public string Description => $"使用技能: {_skillName}";
        public bool IsPlayerAction { get; private set; }
        public int DamageDealt => _damageDealt;
        public int HealingDone => _healingDone;
        public bool WasCritical => _wasCritical;

        /// <summary>
        /// 技能效果类型
        /// </summary>
        public enum SkillEffectType
        {
            Damage,
            Heal,
            Buff,
            Debuff,
            MultiDamage
        }

        /// <summary>
        /// Buff效果数据结构
        /// </summary>
        [Serializable]
        public class BuffEffect
        {
            public string buffId;
            public int duration;
            public float value;
        }

        /// <summary>
        /// 创建使用技能命令
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <param name="skillName">技能名称</param>
        /// <param name="isPlayerAction">是否为玩家行动</param>
        public UseSkillCommand(string skillId, string skillName, bool isPlayerAction)
        {
            _skillId = skillId;
            _skillName = skillName;
            IsPlayerAction = isPlayerAction;
            _appliedBuffs = new List<BuffEffect>();
            _targetHpBefore = new Dictionary<string, int>();
        }

        public void Execute(CombatContext context)
        {
            CombatantData caster = IsPlayerAction ? context.playerCombatant : context.enemyCombatant;
            CombatantData target = IsPlayerAction ? context.enemyCombatant : context.playerCombatant;

            // 保存目标HP状态
            _targetHpBefore[target.name] = target.hp;
            _isDefendingBefore = target.isDefending;

            // 根据技能ID执行不同效果
            // 这里使用技能ID的前缀来判断技能类型，实际项目中应该查询技能配置
            ExecuteSkillEffect(caster, target, _skillId);
        }

        public void Undo(CombatContext context)
        {
            CombatantData target = IsPlayerAction ? context.enemyCombatant : context.playerCombatant;

            // 恢复目标HP
            if (_targetHpBefore.TryGetValue(target.name, out int hpBefore))
            {
                target.hp = hpBefore;
            }

            // 恢复防御状态
            target.isDefending = _isDefendingBefore;

            // 移除添加的buff/debuff
            foreach (var buff in _appliedBuffs)
            {
                if (IsPlayerAction)
                {
                    target.activeBuffs.Remove(buff.buffId);
                }
                else
                {
                    target.activeDebuffs.Remove(buff.buffId);
                }
            }
        }

        private void ExecuteSkillEffect(CombatantData caster, CombatantData target, string skillId)
        {
            // 根据技能ID前缀判断效果类型
            // 实际项目中应该查询技能模板配置
            if (skillId.StartsWith("Skill_Damage") || skillId.StartsWith("Skill_Attack"))
            {
                ExecuteDamageEffect(caster, target);
            }
            else if (skillId.StartsWith("Skill_Heal"))
            {
                ExecuteHealEffect(caster);
            }
            else if (skillId.StartsWith("Skill_Buff"))
            {
                ExecuteBuffEffect(caster, true);
            }
            else if (skillId.StartsWith("Skill_Debuff"))
            {
                ExecuteBuffEffect(target, false);
            }
            else
            {
                // 默认：造成魔法伤害
                ExecuteDamageEffect(caster, target);
            }
        }

        private void ExecuteDamageEffect(CombatantData caster, CombatantData target)
        {
            // 计算魔法伤害（使用攻击和防御）
            float magicDamage = caster.attack * 1.2f;
            float armorReduction = target.isDefending ? target.armor * 0.3f : target.armor * 0.5f;
            float finalDamage = System.Math.Max(1, magicDamage - armorReduction);

            _damageDealt = UnityEngine.Mathf.RoundToInt(finalDamage);
            _wasCritical = UnityEngine.Random.value < caster.critChance * 0.5f; // 技能暴击率减半

            if (_wasCritical)
            {
                _damageDealt = UnityEngine.Mathf.RoundToInt(_damageDealt * caster.critDamageMultiplier);
            }

            _healingDone = 0;
            target.TakeDamage(_damageDealt);
        }

        private void ExecuteHealEffect(CombatantData caster)
        {
            int healAmount = UnityEngine.Mathf.RoundToInt(caster.maxHp * 0.3f);
            caster.Heal(healAmount);

            _damageDealt = 0;
            _healingDone = healAmount;
            _wasCritical = false;
        }

        private void ExecuteBuffEffect(CombatantData target, bool isBuff)
        {
            string buffId = isBuff ? $"buff_{UnityEngine.Random.Range(1000, 9999)}" : $"debuff_{UnityEngine.Random.Range(1000, 9999)}";
            int duration = 2; // 默认持续2回合

            _appliedBuffs.Add(new BuffEffect
            {
                buffId = buffId,
                duration = duration,
                value = 1.2f
            });

            if (isBuff)
            {
                target.activeBuffs.Add(buffId);
            }
            else
            {
                target.activeDebuffs.Add(buffId);
            }

            _damageDealt = 0;
            _healingDone = 0;
            _wasCritical = false;
        }
    }
}