using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 队伍查询接口
    /// 提供只读的队伍状态查询方法
    /// </summary>
    public interface ITeamQueries
    {
        /// <summary>
        /// 当前队伍人数
        /// </summary>
        int memberCount { get; }

        /// <summary>
        /// 容量配置
        /// </summary>
        TeamCapacity capacity { get; }

        /// <summary>
        /// 所有成员（只读列表）
        /// </summary>
        IReadOnlyList<TeamMemberData> members { get; }

        /// <summary>
        /// 根据成员ID获取成员
        /// </summary>
        /// <param name="memberId">成员唯一ID</param>
        /// <returns>成员数据，若不存在返回null</returns>
        TeamMemberData GetMember(int memberId);

        /// <summary>
        /// 获取所有成员
        /// </summary>
        /// <returns>成员只读列表</returns>
        IReadOnlyList<TeamMemberData> GetAllMembers();

        /// <summary>
        /// 获取队伍总战斗力
        /// </summary>
        /// <returns>总战斗力（攻击+防御+生命值/2）</returns>
        int GetTotalCombatPower();

        /// <summary>
        /// 获取队伍平均等级
        /// </summary>
        /// <returns>平均等级，队伍为空时返回0</returns>
        float GetAverageLevel();

        /// <summary>
        /// 检查是否可以添加成员
        /// </summary>
        /// <returns>可以添加返回true，队伍已满返回false</returns>
        bool CanAddMember();

        /// <summary>
        /// 获取剩余位置
        /// </summary>
        /// <returns>剩余可加入位置数</returns>
        int RemainingSlots();
    }
}
