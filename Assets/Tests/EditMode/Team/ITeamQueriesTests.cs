using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game1;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// ITeamQueries接口契约测试
    /// 验证TeamQueriesAdapter正确实现ITeamQueries接口并委托给TeamDesign
    /// </summary>
    public class ITeamQueriesTests
    {
        private ITeamQueries _teamQueries;
        private TeamDesign _teamDesign;

        [SetUp]
        public void SetUp()
        {
            // 创建TeamDesign实例并通过TeamQueriesAdapter访问
            // TeamQueriesAdapter委托给TeamDesign.instance，所以需要确保instance存在
            _teamDesign = TeamDesign.instance;
            _teamQueries = TeamQueriesAdapter.instance;

            // 清空状态确保测试隔离
            _teamDesign.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理团队状态防止测试间泄漏
            if (_teamDesign != null)
            {
                _teamDesign.Clear();
            }
        }

        #region memberCount Tests

        [Test]
        public void memberCount_ReturnsZero_WhenEmpty()
        {
            // Assert
            Assert.AreEqual(0, _teamQueries.memberCount);
        }

        [Test]
        public void memberCount_ReturnsCorrectCount_AfterAddingMembers()
        {
            // Arrange
            _teamDesign.AddMember(new TeamMemberData("Warrior", 5, 100, 20, 10));
            _teamDesign.AddMember(new TeamMemberData("Mage", 4, 80, 15, 5));

            // Assert
            Assert.AreEqual(2, _teamQueries.memberCount);
        }

        #endregion

        #region GetMember Tests

        [Test]
        public void GetMember_ReturnsCorrectMember_WhenExists()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 5, 100, 20, 10);
            var result = _teamDesign.AddMember(member);
            var memberId = result.memberId;

            // Act
            var retrieved = _teamQueries.GetMember(memberId);

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Warrior", retrieved.name);
            Assert.AreEqual(5, retrieved.level);
        }

        [Test]
        public void GetMember_ReturnsNull_WhenNotExists()
        {
            // Act
            var retrieved = _teamQueries.GetMember(99999);

            // Assert
            Assert.IsNull(retrieved);
        }

        #endregion

        #region GetAllMembers Tests

        [Test]
        public void GetAllMembers_ReturnsAllMembers()
        {
            // Arrange
            _teamDesign.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _teamDesign.AddMember(new TeamMemberData("Member2", 2, 30, 10, 5));

            // Act
            var members = _teamQueries.GetAllMembers();

            // Assert
            Assert.AreEqual(2, members.Count);
        }

        [Test]
        public void GetAllMembers_ReturnsEmptyList_WhenNoMembers()
        {
            // Act
            var members = _teamQueries.GetAllMembers();

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(0, members.Count);
        }

        #endregion

        #region GetTotalCombatPower Tests

        [Test]
        public void GetTotalCombatPower_ReturnsZero_WhenEmpty()
        {
            // Act
            var totalPower = _teamQueries.GetTotalCombatPower();

            // Assert
            Assert.AreEqual(0, totalPower);
        }

        [Test]
        public void GetTotalCombatPower_ReturnsCorrectValue_WithMembers()
        {
            // Arrange
            // Member1: attack=5, defense=3, maxHp=20 -> 5+3+20/2=18
            // Member2: attack=10, defense=5, maxHp=30 -> 10+5+30/2=30
            _teamDesign.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _teamDesign.AddMember(new TeamMemberData("Member2", 1, 30, 10, 5));

            // Act
            var totalPower = _teamQueries.GetTotalCombatPower();

            // Assert
            Assert.AreEqual(48, totalPower); // 18 + 30
        }

        #endregion

        #region GetAverageLevel Tests

        [Test]
        public void GetAverageLevel_ReturnsZero_WhenEmpty()
        {
            // Act
            var avgLevel = _teamQueries.GetAverageLevel();

            // Assert
            Assert.AreEqual(0, avgLevel);
        }

        [Test]
        public void GetAverageLevel_ReturnsCorrectValue()
        {
            // Arrange
            _teamDesign.AddMember(new TeamMemberData("Member1", 5, 20, 5, 3));
            _teamDesign.AddMember(new TeamMemberData("Member2", 3, 20, 5, 3));
            _teamDesign.AddMember(new TeamMemberData("Member3", 7, 20, 5, 3));

            // Act
            var avgLevel = _teamQueries.GetAverageLevel();

            // Assert
            Assert.AreEqual(5f, avgLevel); // (5+3+7)/3 = 5
        }

        #endregion

        #region CanAddMember Tests

        [Test]
        public void CanAddMember_ReturnsTrue_WhenNotFull()
        {
            // Assert
            Assert.IsTrue(_teamQueries.CanAddMember());
        }

        [Test]
        public void CanAddMember_ReturnsFalse_WhenFull()
        {
            // Arrange
            _teamDesign.capacity = new TeamCapacity(1);
            _teamDesign.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));

            // Assert
            Assert.IsFalse(_teamQueries.CanAddMember());
        }

        #endregion

        #region RemainingSlots Tests

        [Test]
        public void RemainingSlots_ReturnsCorrectValue()
        {
            // Arrange
            _teamDesign.capacity = new TeamCapacity(5);
            _teamDesign.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _teamDesign.AddMember(new TeamMemberData("Member2", 1, 20, 5, 3));

            // Act
            var remaining = _teamQueries.RemainingSlots();

            // Assert
            Assert.AreEqual(3, remaining);
        }

        [Test]
        public void RemainingSlots_ReturnsMaxTeamSize_WhenEmpty()
        {
            // Act
            var remaining = _teamQueries.RemainingSlots();

            // Assert
            Assert.AreEqual(6, remaining); // default maxTeamSize is 6
        }

        #endregion

        #region members Property Tests

        [Test]
        public void members_ReturnsIReadOnlyList()
        {
            // Act & Assert
            Assert.IsInstanceOf<IReadOnlyList<TeamMemberData>>(_teamQueries.members);
        }

        [Test]
        public void members_ReturnsSameAsGetAllMembers()
        {
            // Arrange
            _teamDesign.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _teamDesign.AddMember(new TeamMemberData("Member2", 1, 20, 5, 3));

            // Act
            var membersProperty = _teamQueries.members;
            var allMembersMethod = _teamQueries.GetAllMembers();

            // Assert
            Assert.AreEqual(membersProperty.Count, allMembersMethod.Count);
        }

        #endregion

        #region capacity Property Tests

        [Test]
        public void capacity_ReturnsCorrectCapacity()
        {
            // Arrange
            _teamDesign.capacity = new TeamCapacity(3);

            // Act & Assert
            Assert.AreEqual(3, _teamQueries.capacity.maxTeamSize);
        }

        #endregion
    }
}