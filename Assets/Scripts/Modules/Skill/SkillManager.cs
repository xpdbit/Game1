using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 技能管理器
    /// 提供静态API，委托给SkillDesign实现
    /// 采用与TeamManager相同的静态API模式
    /// </summary>
    public static class SkillManager
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Initialize()
        {
            SkillDesign.instance.Initialize();
        }

        /// <summary>
        /// 学习技能
        /// </summary>
        public static SkillResult LearnSkill(int memberId, string skillId)
        {
            return SkillDesign.instance.LearnSkill(memberId, skillId);
        }

        /// <summary>
        /// 升级技能
        /// </summary>
        public static SkillResult UpgradeSkill(int memberId, string skillId)
        {
            return SkillDesign.instance.UpgradeSkill(memberId, skillId);
        }

        /// <summary>
        /// 执行主动技能
        /// </summary>
        public static SkillResult ExecuteActiveSkill(string skillId, TeamMemberData caster, object target)
        {
            return SkillDesign.instance.ExecuteActiveSkill(skillId, caster, target);
        }

        /// <summary>
        /// 应用被动技能
        /// </summary>
        public static void ApplyPassiveSkill(TeamMemberData member, string skillId)
        {
            SkillDesign.instance.ApplyPassiveSkill(member, skillId);
        }

        /// <summary>
        /// 执行被动效果
        /// </summary>
        public static void ExecutePassiveEffect(string skillId, TeamMemberData member)
        {
            SkillDesign.instance.ExecutePassiveEffect(skillId, member);
        }

        /// <summary>
        /// 检查是否拥有技能
        /// </summary>
        public static bool HasSkill(int memberId, string skillId)
        {
            return SkillDesign.instance.HasSkill(memberId, skillId);
        }

        /// <summary>
        /// 获取可用技能列表
        /// </summary>
        public static List<string> GetAvailableSkills(TeamMemberData member)
        {
            return SkillDesign.instance.GetAvailableSkills(member);
        }

        /// <summary>
        /// 获取技能等级
        /// </summary>
        public static int GetSkillLevel(int memberId, string skillId)
        {
            return SkillDesign.instance.GetSkillLevel(memberId, skillId);
        }

        /// <summary>
        /// 检查技能是否冷却中
        /// </summary>
        public static bool IsOnCooldown(string skillId)
        {
            return SkillDesign.instance.IsOnCooldown(skillId);
        }

        /// <summary>
        /// 获取冷却剩余时间
        /// </summary>
        public static float GetCooldownRemaining(string skillId)
        {
            return SkillDesign.instance.GetCooldownRemaining(skillId);
        }

        /// <summary>
        /// 获取技能模板
        /// </summary>
        public static SkillTemplate GetTemplate(string skillId)
        {
            return SkillDesign.instance.GetTemplate(skillId);
        }

        /// <summary>
        /// 获取技能描述
        /// </summary>
        public static string GetSkillDescription(string skillId)
        {
            return SkillDesign.instance.GetSkillDescription(skillId);
        }

        // 事件转发
        public static event System.Action<int, SkillData> onSkillLearned
        {
            add => SkillDesign.instance.onSkillLearned += value;
            remove => SkillDesign.instance.onSkillLearned -= value;
        }

        public static event System.Action<int, SkillData> onSkillUpgraded
        {
            add => SkillDesign.instance.onSkillUpgraded += value;
            remove => SkillDesign.instance.onSkillUpgraded -= value;
        }

        public static event System.Action<int, SkillData> onSkillUsed
        {
            add => SkillDesign.instance.onSkillUsed += value;
            remove => SkillDesign.instance.onSkillUsed -= value;
        }

        public static event System.Action<int, SkillData> onSkillCooldownEnd
        {
            add => SkillDesign.instance.onSkillCooldownEnd += value;
            remove => SkillDesign.instance.onSkillCooldownEnd -= value;
        }
    }
}