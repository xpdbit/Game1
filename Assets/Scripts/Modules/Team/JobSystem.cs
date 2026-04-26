using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 职业类型枚举
    /// </summary>
    public enum JobType
    {
        /// <summary>无职业（主角）</summary>
        None,

        /// <summary>商贾 - 交易增益</summary>
        Merchant,

        /// <summary>镖师 - 战斗增益</summary>
        Escort,

        /// <summary>学者 - 探索增益</summary>
        Scholar,

        /// <summary>医者 - 治疗增益</summary>
        Healer
    }

    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum AttributeType
    {
        /// <summary>体力 - 最大HP</summary>
        Vitality,

        /// <summary>攻击 - 伤害输出</summary>
        Attack,

        /// <summary>防御 - 减伤承受</summary>
        Defense,

        /// <summary>速度 - 行动顺序/旅行速度</summary>
        Speed,

        /// <summary>魅力 - NPC态度/交易折扣</summary>
        Charisma,

        /// <summary>智慧 - 事件判断/暴击率</summary>
        Wisdom
    }

    /// <summary>
    /// 职业系统
    /// 管理职业属性加成、专属技能和职业特效
    /// </summary>
    public class JobSystem
    {
        #region Singleton
        private static JobSystem _instance;
        public static JobSystem instance => _instance ??= new JobSystem();
        #endregion

        #region Configuration
        // 职业主属性加成倍率（每级）
        private const float PRIMARY_ATTRIBUTE_BONUS_PER_LEVEL = 2f;
        private const float SECONDARY_ATTRIBUTE_BONUS_PER_LEVEL = 1f;

        // 职业特效基础值
        private const float MERCHANT_TRADE_BONUS = 0.20f;      // 商贾交易+20%
        private const float ESCORT_COMBAT_BONUS = 0.15f;       // 镖师战斗+15%
        private const float SCHOLAR_DISCOVERY_BONUS = 0.30f;  // 学者探索+30%
        private const float HEALER_HEAL_BONUS = 0.50f;        // 医者治疗+50%
        #endregion

        #region Private Fields
        // 职业名称映射
        private readonly Dictionary<JobType, string> _jobNames = new()
        {
            { JobType.None, "无职业" },
            { JobType.Merchant, "商贾" },
            { JobType.Escort, "镖师" },
            { JobType.Scholar, "学者" },
            { JobType.Healer, "医者" }
        };

        // 职业主属性映射
        private readonly Dictionary<JobType, AttributeType> _primaryAttributes = new()
        {
            { JobType.None, AttributeType.Wisdom },     // 默认智慧
            { JobType.Merchant, AttributeType.Charisma },
            { JobType.Escort, AttributeType.Attack },
            { JobType.Scholar, AttributeType.Wisdom },
            { JobType.Healer, AttributeType.Vitality }
        };

        // 职业默认被动技能ID
        private readonly Dictionary<JobType, string> _defaultPassiveSkills = new()
        {
            { JobType.None, "" },
            { JobType.Merchant, "Core.Skill.Passive.Merchant" },
            { JobType.Escort, "Core.Skill.Passive.Escort" },
            { JobType.Scholar, "Core.Skill.Passive.Scholar" },
            { JobType.Healer, "Core.Skill.Passive.Healer" }
        };

        // 职业默认主动技能ID
        private readonly Dictionary<JobType, string> _defaultActiveSkills = new()
        {
            { JobType.None, "" },
            { JobType.Merchant, "Core.Skill.Active.Merchant" },
            { JobType.Escort, "Core.Skill.Active.Escort" },
            { JobType.Scholar, "Core.Skill.Active.Scholar" },
            { JobType.Healer, "Core.Skill.Active.Healer" }
        };
        #endregion

        #region Public Methods

        /// <summary>
        /// 获取职业名称
        /// </summary>
        public string GetJobName(JobType job)
        {
            return _jobNames.TryGetValue(job, out var name) ? name : "未知";
        }

        /// <summary>
        /// 获取职业主属性
        /// </summary>
        public AttributeType GetPrimaryAttribute(JobType job)
        {
            return _primaryAttributes.TryGetValue(job, out var attr) ? attr : AttributeType.Wisdom;
        }

        /// <summary>
        /// 计算职业属性加成
        /// </summary>
        /// <param name="job">职业类型</param>
        /// <param name="attr">属性类型</param>
        /// <param name="level">角色等级</param>
        /// <returns>属性加成值</returns>
        public int GetJobAttributeBonus(JobType job, AttributeType attr, int level)
        {
            if (job == JobType.None || level <= 0)
                return 0;

            var primaryAttr = GetPrimaryAttribute(job);

            if (attr == primaryAttr)
            {
                // 主属性：每级+2
                return (int)(PRIMARY_ATTRIBUTE_BONUS_PER_LEVEL * level);
            }
            else
            {
                // 副属性：每级+1
                return (int)(SECONDARY_ATTRIBUTE_BONUS_PER_LEVEL * level);
            }
        }

        /// <summary>
        /// 获取职业默认被动技能ID
        /// </summary>
        public string GetJobDefaultPassiveSkill(JobType job)
        {
            return _defaultPassiveSkills.TryGetValue(job, out var skillId) ? skillId : "";
        }

        /// <summary>
        /// 获取职业默认主动技能ID
        /// </summary>
        public string GetJobDefaultActiveSkill(JobType job)
        {
            return _defaultActiveSkills.TryGetValue(job, out var skillId) ? skillId : "";
        }

        /// <summary>
        /// 获取职业默认终极技能ID
        /// </summary>
        public string GetJobDefaultUltimateSkill(JobType job)
        {
            // 终极技能需要通过Prestige解锁，这里返回空
            return "";
        }

        /// <summary>
        /// 获取交易加成（商贾专属）
        /// </summary>
        public float GetTradeBonus(JobType job)
        {
            return job == JobType.Merchant ? MERCHANT_TRADE_BONUS : 0f;
        }

        /// <summary>
        /// 获取战斗加成（镖师专属）
        /// </summary>
        public float GetCombatBonus(JobType job)
        {
            return job == JobType.Escort ? ESCORT_COMBAT_BONUS : 0f;
        }

        /// <summary>
        /// 获取探索加成（学者专属）
        /// </summary>
        public float GetDiscoveryBonus(JobType job)
        {
            return job == JobType.Scholar ? SCHOLAR_DISCOVERY_BONUS : 0f;
        }

        /// <summary>
        /// 获取治疗加成（医者专属）
        /// </summary>
        public float GetHealBonus(JobType job)
        {
            return job == JobType.Healer ? HEALER_HEAL_BONUS : 0f;
        }

        /// <summary>
        /// 获取指定职业的所有加成
        /// </summary>
        public JobBonus GetJobBonus(JobType job)
        {
            return new JobBonus
            {
                tradeBonus = GetTradeBonus(job),
                combatBonus = GetCombatBonus(job),
                discoveryBonus = GetDiscoveryBonus(job),
                healBonus = GetHealBonus(job)
            };
        }

        /// <summary>
        /// 检查职业是否有效
        /// </summary>
        public bool IsValidJob(JobType job)
        {
            return job >= JobType.None && job <= JobType.Healer;
        }

        /// <summary>
        /// 获取所有职业类型
        /// </summary>
        public JobType[] GetAllJobTypes()
        {
            return new[]
            {
                JobType.None,
                JobType.Merchant,
                JobType.Escort,
                JobType.Scholar,
                JobType.Healer
            };
        }

        #endregion
    }

    /// <summary>
    /// 职业加成数据结构
    /// </summary>
    public struct JobBonus
    {
        /// <summary>交易加成（商贾）</summary>
        public float tradeBonus;

        /// <summary>战斗加成（镖师）</summary>
        public float combatBonus;

        /// <summary>探索加成（学者）</summary>
        public float discoveryBonus;

        /// <summary>治疗加成（医者）</summary>
        public float healBonus;

        /// <summary>
        /// 获取总加成值
        /// </summary>
        public float totalBonus => tradeBonus + combatBonus + discoveryBonus + healBonus;

        /// <summary>
        /// 是否没有任何加成
        /// </summary>
        public bool isEmpty => totalBonus == 0f;

        public override string ToString()
        {
            return $"[JobBonus: Trade={tradeBonus:P0}, Combat={combatBonus:P0}, Discovery={discoveryBonus:P0}, Heal={healBonus:P0}]";
        }
    }
}