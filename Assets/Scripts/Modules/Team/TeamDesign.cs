using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 队伍操作结果
    /// </summary>
    public class TeamOperationResult
    {
        public bool success;
        public string message;
        public int memberId;

        public static TeamOperationResult Ok(int memberId = 0)
            => new TeamOperationResult { success = true, message = "Success", memberId = memberId };

        public static TeamOperationResult Fail(string message)
            => new TeamOperationResult { success = false, message = message };

        public static TeamOperationResult Full(string reason = "Team is full")
            => new TeamOperationResult { success = false, message = reason };

        public static TeamOperationResult NotFound(string reason = "Member not found")
            => new TeamOperationResult { success = false, message = reason };

        public static TeamOperationResult AlreadyExists(string reason = "Member already in team")
            => new TeamOperationResult { success = false, message = reason };
    }

    /// <summary>
    /// 队伍容量配置
    /// </summary>
    [Serializable]
    public class TeamCapacity
    {
        /// <summary>
        /// 最大队伍人数
        /// </summary>
        public int maxTeamSize = 6;

        public TeamCapacity() { }

        public TeamCapacity(int maxTeamSize)
        {
            this.maxTeamSize = maxTeamSize;
        }
    }

    /// <summary>
    /// 队伍事件数据
    /// </summary>
    public class TeamEventData
    {
        public enum TeamEventType
        {
            MemberAdded,
            MemberRemoved,
            MemberUpdated,
            TeamCleared,
            CapacityChanged
        }

        public TeamEventType eventType;
        public int memberId;
        public string memberName;

        public static TeamEventData MemberAdded(string name, int id)
            => new TeamEventData { eventType = TeamEventType.MemberAdded, memberName = name, memberId = id };

        public static TeamEventData MemberRemoved(string name, int id)
            => new TeamEventData { eventType = TeamEventType.MemberRemoved, memberName = name, memberId = id };

        public static TeamEventData MemberUpdated(string name, int id)
            => new TeamEventData { eventType = TeamEventType.MemberUpdated, memberName = name, memberId = id };

        public static TeamEventData TeamCleared()
            => new TeamEventData { eventType = TeamEventType.TeamCleared };

        public static TeamEventData CapacityChanged()
            => new TeamEventData { eventType = TeamEventType.CapacityChanged };
    }

    /// <summary>
    /// 队伍设计类（核心逻辑）
    /// 非MonoBehaviour，纯数据+逻辑
    /// </summary>
    public class TeamDesign
    {
        #region Singleton
        private static TeamDesign _instance;
        public static TeamDesign instance => _instance ??= new TeamDesign();
        #endregion

        #region Events
        public event Action<TeamEventData> onTeamChanged;
        #endregion

        #region Private Fields
        private readonly Dictionary<int, TeamMemberData> _membersById = new();
        private readonly List<TeamMemberData> _members = new();
        private TeamCapacity _capacity = new();
        private int _memberIdCounter = 0;
        #endregion

        #region Properties
        /// <summary>
        /// 当前队伍人数
        /// </summary>
        public int memberCount => _members.Count;

        /// <summary>
        /// 容量配置
        /// </summary>
        public TeamCapacity capacity
        {
            get => _capacity;
            set => _capacity = value ?? new TeamCapacity();
        }

        /// <summary>
        /// 所有成员（只读）
        /// </summary>
        public IReadOnlyList<TeamMemberData> members => _members;
        #endregion

        #region Core API - Add/Remove

        /// <summary>
        /// 添加成员到队伍
        /// </summary>
        public TeamOperationResult AddMember(TeamMemberData member)
        {
            if (member == null)
                return TeamOperationResult.Fail("Member is null");

            if (string.IsNullOrEmpty(member.name))
                return TeamOperationResult.Fail("Member name is empty");

            // 检查是否已存在
            foreach (var existing in _members)
            {
                if (existing.name == member.name)
                    return TeamOperationResult.AlreadyExists($"Member '{member.name}' already in team");
            }

            // 检查容量
            if (!CanAddMember())
                return TeamOperationResult.Full($"Team is full (max: {_capacity.maxTeamSize})");

            int memberId = GenerateMemberId();
            member.id = memberId;

            _members.Add(member);
            _membersById[memberId] = member;

            PublishEvent(TeamEventData.MemberAdded(member.name, memberId));
            PublishEvent(TeamEventData.CapacityChanged());

            return TeamOperationResult.Ok(memberId);
        }

        /// <summary>
        /// 移除成员
        /// </summary>
        public TeamOperationResult RemoveMember(int memberId)
        {
            if (!_membersById.TryGetValue(memberId, out var member))
                return TeamOperationResult.NotFound($"Member not found: {memberId}");

            _members.Remove(member);
            _membersById.Remove(memberId);

            PublishEvent(TeamEventData.MemberRemoved(member.name, memberId));
            PublishEvent(TeamEventData.CapacityChanged());

            return TeamOperationResult.Ok(memberId);
        }

        /// <summary>
        /// 清空队伍
        /// </summary>
        public void Clear()
        {
            _members.Clear();
            _membersById.Clear();

            PublishEvent(TeamEventData.TeamCleared());
            PublishEvent(TeamEventData.CapacityChanged());
        }

        /// <summary>
        /// 更新成员数据
        /// </summary>
        public void UpdateMember(TeamMemberData member)
        {
            if (member == null || member.id == 0)
                return;

            if (!_membersById.TryGetValue(member.id, out var existing))
                return;

            existing.name = member.name;
            existing.level = member.level;
            existing.hp = member.hp;
            existing.maxHp = member.maxHp;
            existing.attack = member.attack;
            existing.defense = member.defense;

            PublishEvent(TeamEventData.MemberUpdated(existing.name, existing.id));
        }

        #endregion

        #region Core API - Query

        /// <summary>
        /// 获取成员
        /// </summary>
        public TeamMemberData GetMember(int memberId)
        {
            return _membersById.TryGetValue(memberId, out var member) ? member : null;
        }

        /// <summary>
        /// 获取所有成员
        /// </summary>
        public IReadOnlyList<TeamMemberData> GetAllMembers()
        {
            return _members;
        }

        /// <summary>
        /// 获取队伍总战斗力
        /// </summary>
        public int GetTotalCombatPower()
        {
            int total = 0;
            foreach (var member in _members)
            {
                total += member.attack + member.defense + member.maxHp / 2;
            }
            return total;
        }

        /// <summary>
        /// 获取队伍平均等级
        /// </summary>
        public float GetAverageLevel()
        {
            if (_members.Count == 0) return 0;
            int total = 0;
            foreach (var member in _members)
            {
                total += member.level;
            }
            return (float)total / _members.Count;
        }

        #endregion

        #region Capacity Checks

        /// <summary>
        /// 检查是否可以添加成员
        /// </summary>
        public bool CanAddMember()
        {
            return _members.Count < _capacity.maxTeamSize;
        }

        /// <summary>
        /// 获取剩余位置
        /// </summary>
        public int RemainingSlots()
        {
            return Mathf.Max(0, _capacity.maxTeamSize - _members.Count);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 导出数据用于存档
        /// </summary>
        public List<TeamMemberSaveData> Export()
        {
            var list = new List<TeamMemberSaveData>(_members.Count);
            foreach (var member in _members)
            {
                list.Add(new TeamMemberSaveData
                {
                    memberId = member.id,
                    name = member.name,
                    level = member.level,
                    currentHp = member.hp,
                    maxHp = member.maxHp,
                    attack = member.attack,
                    defense = member.defense,
                    speed = member.speed
                });
            }
            return list;
        }

        /// <summary>
        /// 从存档恢复
        /// </summary>
        public void Import(List<TeamMemberSaveData> saveData)
        {
            Clear();
            foreach (var data in saveData)
            {
                var member = new TeamMemberData
                {
                    id = data.memberId,
                    name = data.name,
                    level = data.level,
                    hp = data.currentHp,
                    maxHp = data.maxHp,
                    attack = data.attack,
                    defense = data.defense,
                    speed = data.speed
                };

                _members.Add(member);
                _membersById[member.id] = member;

                if (data.memberId > _memberIdCounter)
                    _memberIdCounter = data.memberId;
            }

            PublishEvent(TeamEventData.CapacityChanged());
        }

        #endregion

        #region Private Methods
        private int GenerateMemberId()
        {
            return ++_memberIdCounter;
        }

        private void PublishEvent(TeamEventData data)
        {
            onTeamChanged?.Invoke(data);
        }
        #endregion
    }
}