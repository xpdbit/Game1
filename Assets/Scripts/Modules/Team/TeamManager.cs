using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 队伍管理器
    /// 负责队伍成员管理和事件订阅
    /// 委托给 TeamDesign 处理核心逻辑
    /// </summary>
    public static class TeamManager
    {
        #region Team Delegation

        /// <summary>
        /// 添加成员到队伍
        /// </summary>
        public static TeamOperationResult AddMember(string name, int level = 1, int hp = 20, int attack = 5, int defense = 3)
        {
            var member = new TeamMemberData(name, level, hp, attack, defense);
            return TeamDesign.instance.AddMember(member);
        }

        /// <summary>
        /// 添加成员（使用TeamMemberData）
        /// </summary>
        public static TeamOperationResult AddMember(TeamMemberData member)
        {
            return TeamDesign.instance.AddMember(member);
        }

        /// <summary>
        /// 移除成员
        /// </summary>
        public static TeamOperationResult RemoveMember(int memberId)
        {
            return TeamDesign.instance.RemoveMember(memberId);
        }

        /// <summary>
        /// 清空队伍
        /// </summary>
        public static void ClearTeam()
        {
            TeamDesign.instance.Clear();
        }

        /// <summary>
        /// 更新成员数据
        /// </summary>
        public static void UpdateMember(TeamMemberData member)
        {
            TeamDesign.instance.UpdateMember(member);
        }

        #endregion

        #region Team Query

        /// <summary>
        /// 获取成员
        /// </summary>
        public static TeamMemberData GetMember(int memberId)
        {
            return TeamDesign.instance.GetMember(memberId);
        }

        /// <summary>
        /// 获取所有成员
        /// </summary>
        public static IReadOnlyList<TeamMemberData> GetAllMembers()
        {
            return TeamDesign.instance.GetAllMembers();
        }

        /// <summary>
        /// 获取队伍人数
        /// </summary>
        public static int GetMemberCount()
        {
            return TeamDesign.instance.memberCount;
        }

        /// <summary>
        /// 获取队伍总战斗力
        /// </summary>
        public static int GetTotalCombatPower()
        {
            return TeamDesign.instance.GetTotalCombatPower();
        }

        /// <summary>
        /// 获取队伍平均等级
        /// </summary>
        public static float GetAverageLevel()
        {
            return TeamDesign.instance.GetAverageLevel();
        }

        /// <summary>
        /// 获取剩余位置
        /// </summary>
        public static int RemainingSlots()
        {
            return TeamDesign.instance.RemainingSlots();
        }

        /// <summary>
        /// 检查是否可以添加成员
        /// </summary>
        public static bool CanAddMember()
        {
            return TeamDesign.instance.CanAddMember();
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 导出队伍数据用于存档
        /// </summary>
        public static List<TeamMemberSaveData> ExportTeam()
        {
            return TeamDesign.instance.Export();
        }

        /// <summary>
        /// 从存档恢复队伍数据
        /// </summary>
        public static void ImportTeam(List<TeamMemberSaveData> saveData)
        {
            TeamDesign.instance.Import(saveData);
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// 订阅队伍变化事件
        /// </summary>
        public static void SubscribeTeamChanged(Action<TeamEventData> callback)
        {
            TeamDesign.instance.onTeamChanged += callback;
        }

        /// <summary>
        /// 取消订阅队伍变化事件
        /// </summary>
        public static void UnsubscribeTeamChanged(Action<TeamEventData> callback)
        {
            TeamDesign.instance.onTeamChanged -= callback;
        }

        #endregion

        #region Team Operations

        /// <summary>
        /// 治疗所有成员
        /// </summary>
        public static void HealAll(int amount)
        {
            foreach (var member in TeamDesign.instance.members)
            {
                member.Heal(amount);
            }
        }

        /// <summary>
        /// 全体升级
        /// </summary>
        public static void LevelUpAll()
        {
            foreach (var member in TeamDesign.instance.members)
            {
                member.LevelUp();
            }
        }

        #endregion
    }
}