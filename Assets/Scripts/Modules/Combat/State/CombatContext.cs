using System;
using System.Collections.Generic;
using Game1.Modules.Combat.Commands;

namespace Game1.Modules.Combat.State
{
    /// <summary>
    /// 战斗上下文数据
    /// 可序列化的数据结构，用于mid-combat保存和状态恢复
    /// </summary>
    [Serializable]
    public class CombatContext
    {
        /// <summary>当前战斗阶段</summary>
        public CombatPhase currentPhase;

        /// <summary>当前回合数</summary>
        public int round;

        /// <summary>玩家战斗者数据</summary>
        public CombatantData playerCombatant;

        /// <summary>敌人战斗者数据</summary>
        public CombatantData enemyCombatant;

        /// <summary>行动历史记录</summary>
        public List<ActionRecord> actionHistory;

        /// <summary>时间戳</summary>
        public long timestamp;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CombatContext()
        {
            currentPhase = CombatPhase.Idle;
            round = 0;
            playerCombatant = new CombatantData();
            enemyCombatant = new CombatantData();
            actionHistory = new List<ActionRecord>();
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 创建战斗上下文
        /// </summary>
        public CombatContext(CombatantData player, CombatantData enemy) : this()
        {
            playerCombatant = player;
            enemyCombatant = enemy;
        }

        /// <summary>
        /// 记录一个行动
        /// </summary>
        public void RecordAction(ICombatCommand command, bool isPlayerAction)
        {
            actionHistory.Add(new ActionRecord
            {
                commandType = command.GetType().Name,
                isPlayerAction = isPlayerAction,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        /// <summary>
        /// 获取战斗持续时间（秒）
        /// </summary>
        public float GetDuration()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (currentTimestamp - timestamp) / 1000f;
        }
    }

    /// <summary>
    /// 战斗者数据
    /// </summary>
    [Serializable]
    public class CombatantData
    {
        private const float DEFAULT_CRIT_CHANCE = 0.1f;
        private const float DEFAULT_CRIT_MULTIPLIER = 1.5f;

        public string name;
        public int hp;
        public int maxHp;
        public int armor;
        public int damage;
        public int attack;
        public int defense;
        public float critChance;
        public float critDamageMultiplier;
        public bool isDefending;
        public List<string> activeBuffs;
        public List<string> activeDebuffs;

        public CombatantData()
        {
            activeBuffs = new List<string>();
            activeDebuffs = new List<string>();
            isDefending = false;
            critChance = 0f;
            critDamageMultiplier = DEFAULT_CRIT_MULTIPLIER;
        }

        /// <summary>
        /// 从TeamMemberData创建战斗者数据
        /// </summary>
        public static CombatantData FromTeamMember(TeamMemberData member)
        {
            if (member == null)
                return new CombatantData();

            return new CombatantData
            {
                name = member.name,
                hp = member.hp,
                maxHp = member.maxHp,
                armor = member.defense,
                damage = member.attack,
                attack = member.attack,
                defense = member.defense,
                critChance = DEFAULT_CRIT_CHANCE,
                critDamageMultiplier = 1.5f,
                isDefending = false,
                activeBuffs = new List<string>(),
                activeDebuffs = new List<string>()
            };
        }

        /// <summary>
        /// 检查是否存活
        /// </summary>
        public bool IsAlive() => hp > 0;

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            int actualDamage = damage;
            if (isDefending)
            {
                actualDamage = System.Math.Max(1, damage - armor / 2);
                isDefending = false;
            }
            else
            {
                actualDamage = System.Math.Max(1, damage - armor);
            }
            hp = System.Math.Max(0, hp - actualDamage);
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            hp = System.Math.Min(maxHp, hp + amount);
        }
    }

    /// <summary>
    /// 行动记录
    /// </summary>
    [Serializable]
    public class ActionRecord
    {
        public string commandType;
        public bool isPlayerAction;
        public long timestamp;
        public int damageDealt;
        public int healingDone;
        public bool wasCritical;
    }
}