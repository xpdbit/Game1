using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 技能系统核心逻辑
    /// 管理技能查询、效果应用、技能执行
    /// 采用与InventoryDesign/TeamDesign相同的单例非MonoBehaviour模式
    /// </summary>
    public class SkillDesign
    {
        #region Singleton
        private static SkillDesign _instance;
        public static SkillDesign instance => _instance ??= new SkillDesign();
        #endregion

        #region Private Fields
        // 技能模板缓存
        private readonly Dictionary<string, SkillTemplate> _templates = new();

        // 角色已学习的技能
        private readonly Dictionary<int, List<SkillData>> _memberSkills = new();

        // 技能冷却管理
        private readonly Dictionary<string, float> _skillCooldowns = new();
        #endregion

        #region Events
        public event Action<int, SkillData> onSkillLearned;      // 技能学习
        public event Action<int, SkillData> onSkillUpgraded;     // 技能升级
        public event Action<int, SkillData> onSkillUsed;         // 技能使用
        public event Action<int, SkillData> onSkillCooldownEnd;  // 冷却结束
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化（从配置加载技能模板）
        /// </summary>
        public void Initialize()
        {
            // TODO: 从Skills.xml加载技能模板
            LoadSkillTemplates();
            Debug.Log("[SkillDesign] Initialized");
        }

        /// <summary>
        /// 加载技能模板
        /// </summary>
        private void LoadSkillTemplates()
        {
            // 示例：添加默认技能模板
            AddTemplate(new SkillTemplate
            {
                id = "Core.Skill.Passive.Merchant",
                nameTextId = "Core.Skill.Passive.Merchant.NameText",
                descTextId = "Core.Skill.Passive.Merchant.DescText",
                type = SkillType.Passive,
                maxLevel = 5,
                effectType = SkillEffectType.trade_bonus,
                baseEffectValue = 0.06f,
                effectValuePerLevel = 0.06f,
                condition = SkillCondition.None
            });

            AddTemplate(new SkillTemplate
            {
                id = "Core.Skill.Passive.SwiftStep",
                nameTextId = "Core.Skill.Passive.SwiftStep.NameText",
                descTextId = "Core.Skill.Passive.SwiftStep.DescText",
                type = SkillType.Passive,
                maxLevel = 5,
                effectType = SkillEffectType.travel_speed,
                baseEffectValue = 0.04f,
                effectValuePerLevel = 0.04f,
                condition = SkillCondition.InTravel
            });

            AddTemplate(new SkillTemplate
            {
                id = "Core.Skill.Active.Sweep",
                nameTextId = "Core.Skill.Active.Sweep.NameText",
                descTextId = "Core.Skill.Active.Sweep.DescText",
                type = SkillType.Active,
                maxLevel = 3,
                effectType = SkillEffectType.area_damage,
                baseEffectValue = 0.5f,
                effectValuePerLevel = 0.1f,
                cooldown = 30f,
                cost = 0,
                condition = SkillCondition.InCombat
            });

            AddTemplate(new SkillTemplate
            {
                id = "Core.Skill.Active.Heal",
                nameTextId = "Core.Skill.Active.Heal.NameText",
                descTextId = "Core.Skill.Active.Heal.DescText",
                type = SkillType.Active,
                maxLevel = 3,
                effectType = SkillEffectType.heal,
                baseEffectValue = 0.3f,
                effectValuePerLevel = 0.05f,
                cooldown = 45f,
                cost = 0,
                condition = SkillCondition.InCombat
            });
        }

        /// <summary>
        /// 添加技能模板
        /// </summary>
        public void AddTemplate(SkillTemplate template)
        {
            if (template == null || string.IsNullOrEmpty(template.id))
                return;

            _templates[template.id] = template;
        }

        /// <summary>
        /// 获取技能模板
        /// </summary>
        public SkillTemplate GetTemplate(string skillId)
        {
            return _templates.TryGetValue(skillId, out var template) ? template : null;
        }

        /// <summary>
        /// 检查角色是否拥有指定技能
        /// </summary>
        public bool HasSkill(int memberId, string skillId)
        {
            if (!_memberSkills.TryGetValue(memberId, out var skills))
                return false;

            foreach (var skill in skills)
            {
                if (skill.id == skillId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取角色所有可用技能
        /// </summary>
        public List<string> GetAvailableSkills(TeamMemberData member)
        {
            var result = new List<string>();

            if (member == null)
                return result;

            if (_memberSkills.TryGetValue(member.id, out var skills))
            {
                foreach (var skill in skills)
                {
                    result.Add(skill.id);
                }
            }

            // 添加职业默认技能
            var jobDefaultPassive = JobSystem.instance.GetJobDefaultPassiveSkill(member.job);
            if (!string.IsNullOrEmpty(jobDefaultPassive) && !result.Contains(jobDefaultPassive))
            {
                result.Add(jobDefaultPassive);
            }

            return result;
        }

        /// <summary>
        /// 学习技能
        /// </summary>
        public SkillResult LearnSkill(int memberId, string skillId)
        {
            var template = GetTemplate(skillId);
            if (template == null)
                return SkillResult.Failure($"技能模板不存在: {skillId}");

            // 检查是否已学习
            if (HasSkill(memberId, skillId))
                return SkillResult.Failure("已经学习过该技能");

            // 检查技能类型上限
            if (!CanLearnMoreOfType(memberId, template.type))
                return SkillResult.Failure($"已达{template.type}技能数量上限");

            // 创建技能数据
            var skillData = template.ToSkillData();

            // 添加到角色技能列表
            if (!_memberSkills.TryGetValue(memberId, out var skills))
            {
                skills = new List<SkillData>();
                _memberSkills[memberId] = skills;
            }

            skills.Add(skillData);

            // 触发事件
            onSkillLearned?.Invoke(memberId, skillData);

            return SkillResult.Success($"成功学习技能: {template.nameTextId}");
        }

        /// <summary>
        /// 升级技能
        /// </summary>
        public SkillResult UpgradeSkill(int memberId, string skillId)
        {
            if (!_memberSkills.TryGetValue(memberId, out var skills))
                return SkillResult.Failure("未学习该技能");

            SkillData skill = null;
            foreach (var s in skills)
            {
                if (s.id == skillId)
                {
                    skill = s;
                    break;
                }
            }

            if (skill == null)
                return SkillResult.Failure("未学习该技能");

            if (skill.currentLevel >= skill.maxLevel)
                return SkillResult.Failure("技能已达最大等级");

            // TODO: 检查升级费用

            skill.currentLevel++;

            onSkillUpgraded?.Invoke(memberId, skill);

            return SkillResult.Success($"技能升级成功: {skill.nameTextId} Lv.{skill.currentLevel}");
        }

        /// <summary>
        /// 应用被动技能效果
        /// </summary>
        public void ApplyPassiveSkill(TeamMemberData member, string skillId)
        {
            var template = GetTemplate(skillId);
            if (template == null || template.type != SkillType.Passive)
                return;

            var skillData = template.ToSkillData();
            skillData.currentLevel = GetSkillLevel(member.id, skillId);

            ApplyPassiveEffect(member, skillData);
        }

        /// <summary>
        /// 应用被动效果
        /// </summary>
        private void ApplyPassiveEffect(TeamMemberData member, SkillData skill)
        {
            if (member == null)
                return;

            switch (skill.effectType)
            {
                case SkillEffectType.damage_boost:
                    member.attack += (int)skill.GetEffectValue();
                    break;

                case SkillEffectType.defense_boost:
                    member.defense += (int)skill.GetEffectValue();
                    break;

                case SkillEffectType.travel_speed:
                    member.speed += skill.GetEffectValue();
                    break;

                case SkillEffectType.trade_bonus:
                    // 交易加成需要通过其他系统计算
                    break;

                case SkillEffectType.crit_rate:
                    // 暴击率在战斗计算时应用
                    break;

                case SkillEffectType.exp_bonus:
                    // 经验加成在获得经验时应用
                    break;
            }
        }

        /// <summary>
        /// 执行主动技能
        /// </summary>
        public SkillResult ExecuteActiveSkill(string skillId, TeamMemberData caster, object target)
        {
            var template = GetTemplate(skillId);
            if (template == null || template.type != SkillType.Active)
                return SkillResult.Failure("技能不存在或不是主动技能");

            // 检查冷却
            if (IsOnCooldown(skillId))
                return SkillResult.Failure("技能冷却中");

            // 检查消耗
            if (template.cost > 0)
            {
                // TODO: 检查并消耗资源
            }

            // 创建技能数据
            var skillData = template.ToSkillData();
            skillData.currentLevel = GetSkillLevel(caster.id, skillId);

            // 执行效果
            var result = ExecuteSkillEffect(skillData, caster, target);

            // 设置冷却
            SetCooldown(skillId, template.cooldown);

            // 触发事件
            onSkillUsed?.Invoke(caster.id, skillData);

            return result;
        }

        /// <summary>
        /// 执行技能效果
        /// </summary>
        private SkillResult ExecuteSkillEffect(SkillData skill, TeamMemberData caster, object target)
        {
            switch (skill.effectType)
            {
                case SkillEffectType.heal:
                    if (target is TeamMemberData targetMember)
                    {
                        int healAmount = (int)(targetMember.maxHp * skill.GetEffectValue());
                        targetMember.currentHp = Mathf.Min(targetMember.maxHp, targetMember.currentHp + healAmount);
                        return SkillResult.Success($"恢复了 {healAmount} 点生命", healAmount);
                    }
                    break;

                case SkillEffectType.area_damage:
                    // 范围伤害
                    return SkillResult.Success($"造成 {skill.GetEffectValue():P0} 范围伤害", skill.GetEffectValue());

                case SkillEffectType.shield:
                    // 护盾效果
                    return SkillResult.Success("获得护盾");

                case SkillEffectType.gold_to_health:
                    // 消耗生命换取金币
                    int healthCost = skill.cost;
                    int goldGain = healthCost * 10;
                    caster.currentHp -= healthCost;
                    // TODO: 增加金币
                    return SkillResult.Success($"消耗 {healthCost} HP 获得 {goldGain} 金币", goldGain);
            }

            return SkillResult.Success("技能执行成功");
        }

        /// <summary>
        /// 执行被动技能效果（触发型）
        /// </summary>
        public void ExecutePassiveEffect(string skillId, TeamMemberData member)
        {
            var template = GetTemplate(skillId);
            if (template == null || template.type != SkillType.Passive)
                return;

            var skillData = template.ToSkillData();
            skillData.currentLevel = GetSkillLevel(member.id, skillId);

            ApplyPassiveEffect(member, skillData);
        }

        /// <summary>
        /// 获取技能等级
        /// </summary>
        public int GetSkillLevel(int memberId, string skillId)
        {
            if (!_memberSkills.TryGetValue(memberId, out var skills))
                return 1;

            foreach (var skill in skills)
            {
                if (skill.id == skillId)
                    return skill.currentLevel;
            }

            return 1;
        }

        /// <summary>
        /// 检查技能是否冷却中
        /// </summary>
        public bool IsOnCooldown(string skillId)
        {
            if (_skillCooldowns.TryGetValue(skillId, out var endTime))
            {
                return Time.time < endTime;
            }

            return false;
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        public float GetCooldownRemaining(string skillId)
        {
            if (_skillCooldowns.TryGetValue(skillId, out var endTime))
            {
                return Mathf.Max(0, endTime - Time.time);
            }

            return 0;
        }

        /// <summary>
        /// 设置技能冷却
        /// </summary>
        private void SetCooldown(string skillId, float duration)
        {
            _skillCooldowns[skillId] = Time.time + duration;
        }

        /// <summary>
        /// 检查是否能学习更多指定类型的技能
        /// </summary>
        private bool CanLearnMoreOfType(int memberId, SkillType type)
        {
            if (!_memberSkills.TryGetValue(memberId, out var skills))
                return true;

            int count = 0;
            foreach (var skill in skills)
            {
                if (skill.type == type)
                    count++;
            }

            return type switch
            {
                SkillType.Passive => count < 4,
                SkillType.Active => count < 2,
                SkillType.Ultimate => count < 1,
                _ => true
            };
        }

        /// <summary>
        /// 获取技能描述
        /// </summary>
        public string GetSkillDescription(string skillId)
        {
            var template = GetTemplate(skillId);
            if (template == null)
                return "";

            string effectDesc = template.effectType switch
            {
                SkillEffectType.damage_boost => $"伤害+{template.baseEffectValue:P0}/级",
                SkillEffectType.heal => $"治疗{template.baseEffectValue:P0}最大生命/级",
                SkillEffectType.trade_bonus => $"交易+{template.baseEffectValue:P0}/级",
                SkillEffectType.travel_speed => $"旅行速度+{template.baseEffectValue:P0}/级",
                SkillEffectType.area_damage => $"范围伤害{template.baseEffectValue:P0}/级",
                _ => ""
            };

            return effectDesc;
        }

        #endregion
    }
}