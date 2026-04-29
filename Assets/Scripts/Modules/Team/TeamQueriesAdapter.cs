using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 队伍查询适配器
    /// 实现 ITeamQueries 接口，委托给 TeamDesign.instance
    /// 用于解耦 consumers 对 TeamDesign 单例的直接依赖
    /// </summary>
    public class TeamQueriesAdapter : ITeamQueries
    {
        #region Singleton

        private static TeamQueriesAdapter _instance;

        /// <summary>
        /// 获取 TeamQueriesAdapter 单例实例
        /// </summary>
        public static TeamQueriesAdapter instance => _instance ??= new TeamQueriesAdapter();

        #endregion

        #region ITeamQueries Implementation

        /// <summary>
        /// 当前队伍人数
        /// </summary>
        public int memberCount => TeamDesign.instance.memberCount;

        /// <summary>
        /// 容量配置
        /// </summary>
        public TeamCapacity capacity => TeamDesign.instance.capacity;

        /// <summary>
        /// 所有成员（只读列表）
        /// </summary>
        public IReadOnlyList<TeamMemberData> members => TeamDesign.instance.members;

        /// <summary>
        /// 根据成员ID获取成员
        /// </summary>
        /// <param name="memberId">成员唯一ID</param>
        /// <returns>成员数据，若不存在返回null</returns>
        public TeamMemberData GetMember(int memberId)
            => TeamDesign.instance.GetMember(memberId);

        /// <summary>
        /// 获取所有成员
        /// </summary>
        /// <returns>成员只读列表</returns>
        public IReadOnlyList<TeamMemberData> GetAllMembers()
            => TeamDesign.instance.GetAllMembers();

        /// <summary>
        /// 获取队伍总战斗力
        /// </summary>
        /// <returns>总战斗力（攻击+防御+生命值/2）</returns>
        public int GetTotalCombatPower()
            => TeamDesign.instance.GetTotalCombatPower();

        /// <summary>
        /// 获取队伍平均等级
        /// </summary>
        /// <returns>平均等级，队伍为空时返回0</returns>
        public float GetAverageLevel()
            => TeamDesign.instance.GetAverageLevel();

        /// <summary>
        /// 检查是否可以添加成员
        /// </summary>
        /// <returns>可以添加返回true，队伍已满返回false</returns>
        public bool CanAddMember()
            => TeamDesign.instance.CanAddMember();

        /// <summary>
        /// 获取剩余位置
        /// </summary>
        /// <returns>剩余可加入位置数</returns>
        public int RemainingSlots()
            => TeamDesign.instance.RemainingSlots();

        #endregion
    }
}
