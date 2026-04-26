using System;

namespace Game1
{
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        /// <summary>被动技能</summary>
        Passive,

        /// <summary>主动技能</summary>
        Active,

        /// <summary>终极技能</summary>
        Ultimate
    }

    /// <summary>
    /// 技能效果类型枚举
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>伤害加成</summary>
        damage_boost,

        /// <summary>治疗</summary>
        heal,

        /// <summary>治疗加成</summary>
        heal_bonus,

        /// <summary>交易加成</summary>
        trade_bonus,

        /// <summary>旅行速度</summary>
        travel_speed,

        /// <summary>旅行速度提升（临时的）</summary>
        travel_speed_boost,

        /// <summary>防御加成</summary>
        defense_boost,

        /// <summary>暴击率加成</summary>
        crit_rate,

        /// <summary>经验加成</summary>
        exp_bonus,

        /// <summary>范围伤害</summary>
        area_damage,

        /// <summary>护盾</summary>
        shield,

        /// <summary>金币换生命</summary>
        gold_to_health,

        /// <summary>生命值提升（被动）</summary>
        hp_boost,

        /// <summary>单体暴击伤害（终极）</summary>
        single_target_crit,

        /// <summary>群体治疗（终极）</summary>
        group_heal
    }

    /// <summary>
    /// 技能条件类型
    /// </summary>
    [Flags]
    public enum SkillCondition
    {
        None = 0,
        InCombat = 1 << 0,       // 战斗中
        OutOfCombat = 1 << 1,    // 战斗外
        InTravel = 1 << 2,       // 旅行中
        HasTarget = 1 << 3,      // 有目标
        LowHealth = 1 << 4,      // 低血量
        FullHealth = 1 << 5,     // 满血
        InTeam = 1 << 6,         // 在队伍中
        Solo = 1 << 7            // 单独行动
    }

    /// <summary>
    /// 技能数据（运行时实例）
    /// </summary>
    [Serializable]
    public class SkillData
    {
        /// <summary>技能ID</summary>
        public string id;

        /// <summary>本地化名称键</summary>
        public string nameTextId;

        /// <summary>本地化描述键</summary>
        public string descTextId;

        /// <summary>技能类型</summary>
        public SkillType type;

        /// <summary>最大等级</summary>
        public int maxLevel;

        /// <summary>当前等级</summary>
        public int currentLevel;

        /// <summary>效果类型</summary>
        public SkillEffectType effectType;

        /// <summary>效果数值（每级）</summary>
        public float effectValue;

        /// <summary>冷却时间（秒）- 主动技能</summary>
        public float cooldown;

        /// <summary>消耗（生命/魔法等）</summary>
        public int cost;

        /// <summary>触发条件</summary>
        public SkillCondition condition;

        /// <summary>效果范围 - 1表示单体</summary>
        public float range;

        /// <summary>
        /// 获取当前效果数值（根据等级）
        /// </summary>
        public float GetEffectValue()
        {
            return effectValue * currentLevel;
        }

        /// <summary>
        /// 检查是否满足条件
        /// </summary>
        public bool CheckCondition(SkillCondition currentCondition)
        {
            if (condition == SkillCondition.None)
                return true;

            return (currentCondition & condition) != 0;
        }

        /// <summary>
        /// 技能是否就绪（冷却是否结束）
        /// </summary>
        public bool isReady;
    }

    /// <summary>
    /// 技能模板（配置数据）
    /// </summary>
    [Serializable]
    public class SkillTemplate
    {
        public string id;
        public string nameTextId;
        public string descTextId;
        public SkillType type;
        public int maxLevel;
        public SkillEffectType effectType;
        public float baseEffectValue;
        public float effectValuePerLevel;
        public float cooldown;
        public int baseCost;
        public SkillCondition condition;
        public float range;

        /// <summary>
        /// 创建运行时技能数据
        /// </summary>
        public SkillData ToSkillData()
        {
            return new SkillData
            {
                id = id,
                nameTextId = nameTextId,
                descTextId = descTextId,
                type = type,
                maxLevel = maxLevel,
                currentLevel = 1,
                effectType = effectType,
                effectValue = baseEffectValue,
                cooldown = cooldown,
                cost = baseCost,
                condition = condition,
                range = range,
                isReady = true
            };
        }
    }

    /// <summary>
    /// 技能执行结果
    /// </summary>
    public struct SkillResult
    {
        public bool success;
        public string message;
        public float value;          // 效果数值
        public int healthCost;        // 生命消耗
        public int manaCost;         // 魔法消耗
        public int goldCost;         // 金币消耗

        public static SkillResult Success(string message, float value = 0)
        {
            return new SkillResult
            {
                success = true,
                message = message,
                value = value
            };
        }

        public static SkillResult Failure(string message)
        {
            return new SkillResult
            {
                success = false,
                message = message
            };
        }
    }

    /// <summary>
    /// 被动技能效果（无实例，数据驱动）
    /// </summary>
    [Serializable]
    public class PassiveSkillEffect
    {
        /// <summary>效果ID</summary>
        public string id;

        /// <summary>效果名称</summary>
        public string name;

        /// <summary>效果类型</summary>
        public SkillEffectType effectType;

        /// <summary>效果数值</summary>
        public float value;

        /// <summary>叠加方式</summary>
        public bool isStackable;

        /// <summary>
        /// 应用效果到角色属性
        /// </summary>
        public void Apply(TeamMemberData member)
        {
            if (member == null) return;

            switch (effectType)
            {
                case SkillEffectType.damage_boost:
                    member.attack += (int)value;
                    break;

                case SkillEffectType.defense_boost:
                    member.defense += (int)value;
                    break;

                case SkillEffectType.heal:
                    // 被动治疗效果
                    break;

                case SkillEffectType.trade_bonus:
                    // 交易加成效果
                    break;

                case SkillEffectType.travel_speed:
                    member.speed += value;
                    break;

                case SkillEffectType.crit_rate:
                    // 暴击率效果需要在计算时应用
                    break;

                case SkillEffectType.exp_bonus:
                    // 经验加成效果
                    break;
            }
        }

        /// <summary>
        /// 移除效果
        /// </summary>
        public void Remove(TeamMemberData member)
        {
            if (member == null) return;

            switch (effectType)
            {
                case SkillEffectType.damage_boost:
                    member.attack -= (int)value;
                    break;

                case SkillEffectType.defense_boost:
                    member.defense -= (int)value;
                    break;

                case SkillEffectType.travel_speed:
                    member.speed -= value;
                    break;
            }
        }
    }
}