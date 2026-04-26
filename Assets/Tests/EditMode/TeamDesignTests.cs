using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// TeamDesign 单元测试
    /// 测试队伍核心逻辑
    /// </summary>
    public class TeamDesignTests
    {
        private TeamDesign _team;

        [SetUp]
        public void SetUp()
        {
            // 每个测试创建独立的TeamDesign实例
            _team = new TeamDesign();
        }

        [TearDown]
        public void TearDown()
        {
            if (_team != null)
            {
                _team.Clear();
            }
        }

        #region AddMember Tests

        [Test]
        public void AddMember_Success_SingleMember()
        {
            // Arrange
            var member = new TeamMemberData(" warrior", 5, 100, 20, 10);

            // Act
            var result = _team.AddMember(member);

            // Assert
            Assert.IsTrue(result.success, $"AddMember failed: {result.message}");
            Assert.AreEqual(1, _team.memberCount);
            Assert.AreEqual(1, result.memberId);
        }

        [Test]
        public void AddMember_Success_MultipleMembers()
        {
            // Arrange
            var member1 = new TeamMemberData("Warrior", 5, 100, 20, 10);
            var member2 = new TeamMemberData("Mage", 4, 80, 15, 5);

            // Act
            var result1 = _team.AddMember(member1);
            var result2 = _team.AddMember(member2);

            // Assert
            Assert.IsTrue(result1.success);
            Assert.IsTrue(result2.success);
            Assert.AreEqual(2, _team.memberCount);
        }

        [Test]
        public void AddMember_Fail_NullMember()
        {
            // Act
            var result = _team.AddMember(null);

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Member is null", result.message);
        }

        [Test]
        public void AddMember_Fail_EmptyName()
        {
            // Arrange
            var member = new TeamMemberData("", 1, 20, 5, 3);

            // Act
            var result = _team.AddMember(member);

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Member name is empty", result.message);
        }

        [Test]
        public void AddMember_Fail_AlreadyExists()
        {
            // Arrange
            var member1 = new TeamMemberData("Warrior", 5, 100, 20, 10);
            var member2 = new TeamMemberData("Warrior", 3, 80, 15, 8);

            // Act
            _team.AddMember(member1);
            var result = _team.AddMember(member2);

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("already in team"));
        }

        [Test]
        public void AddMember_Fail_TeamFull()
        {
            // Arrange
            _team.capacity = new TeamCapacity(2);
            _team.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _team.AddMember(new TeamMemberData("Member2", 1, 20, 5, 3));

            // Act
            var result = _team.AddMember(new TeamMemberData("Member3", 1, 20, 5, 3));

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("Team is full"));
        }

        #endregion

        #region RemoveMember Tests

        [Test]
        public void RemoveMember_Success()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 5, 100, 20, 10);
            var result = _team.AddMember(member);
            var memberId = result.memberId;

            // Act
            var removeResult = _team.RemoveMember(memberId);

            // Assert
            Assert.IsTrue(removeResult.success);
            Assert.AreEqual(0, _team.memberCount);
            Assert.IsNull(_team.GetMember(memberId));
        }

        [Test]
        public void RemoveMember_Fail_NotFound()
        {
            // Act
            var result = _team.RemoveMember(99999);

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("not found"));
        }

        #endregion

        #region UpdateMember Tests

        [Test]
        public void UpdateMember_Success_UpdatesProperties()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 5, 100, 20, 10);
            var addResult = _team.AddMember(member);
            var memberId = addResult.memberId;

            var updatedMember = new TeamMemberData
            {
                id = memberId,
                name = "Warrior",
                level = 6,
                hp = 110,
                maxHp = 110,
                attack = 25,
                defense = 12
            };

            // Act
            _team.UpdateMember(updatedMember);
            var retrieved = _team.GetMember(memberId);

            // Assert
            Assert.AreEqual(6, retrieved.level);
            Assert.AreEqual(110, retrieved.hp);
            Assert.AreEqual(25, retrieved.attack);
            Assert.AreEqual(12, retrieved.defense);
        }

        [Test]
        public void UpdateMember_Fail_NullMember()
        {
            // Act & Assert - 不应抛出异常
            _team.UpdateMember(null);
        }

        [Test]
        public void UpdateMember_Fail_ZeroId()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 5, 100, 20, 10);
            member.id = 0; // 无效ID

            // Act
            _team.UpdateMember(member);

            // Assert - 没有成员被更新
            Assert.AreEqual(0, _team.memberCount);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_Success_RemovesAllMembers()
        {
            // Arrange
            _team.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _team.AddMember(new TeamMemberData("Member2", 1, 20, 5, 3));

            // Act
            _team.Clear();

            // Assert
            Assert.AreEqual(0, _team.memberCount);
        }

        #endregion

        #region Query Tests

        [Test]
        public void GetMember_Success()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 5, 100, 20, 10);
            var addResult = _team.AddMember(member);
            var memberId = addResult.memberId;

            // Act
            var retrieved = _team.GetMember(memberId);

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Warrior", retrieved.name);
        }

        [Test]
        public void GetMember_Fail_NonExistent()
        {
            // Act
            var retrieved = _team.GetMember(99999);

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public void GetAllMembers_ReturnsCorrectList()
        {
            // Arrange
            _team.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _team.AddMember(new TeamMemberData("Member2", 1, 20, 5, 3));

            // Act
            var members = _team.GetAllMembers();

            // Assert
            Assert.AreEqual(2, members.Count);
        }

        [Test]
        public void GetTotalCombatPower_CalculatesCorrectly()
        {
            // Arrange
            _team.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3)); // 5+3+10=18
            _team.AddMember(new TeamMemberData("Member2", 1, 20, 10, 5)); // 10+5+10=25

            // Act
            var totalPower = _team.GetTotalCombatPower();

            // Assert
            Assert.AreEqual(43, totalPower); // 18 + 25
        }

        [Test]
        public void GetAverageLevel_CalculatesCorrectly()
        {
            // Arrange
            _team.AddMember(new TeamMemberData("Member1", 5, 20, 5, 3));
            _team.AddMember(new TeamMemberData("Member2", 3, 20, 5, 3));
            _team.AddMember(new TeamMemberData("Member3", 7, 20, 5, 3));

            // Act
            var avgLevel = _team.GetAverageLevel();

            // Assert
            Assert.AreEqual(5f, avgLevel); // (5+3+7)/3 = 5
        }

        [Test]
        public void GetAverageLevel_ReturnsZero_WhenEmpty()
        {
            // Act
            var avgLevel = _team.GetAverageLevel();

            // Assert
            Assert.AreEqual(0, avgLevel);
        }

        #endregion

        #region Capacity Tests

        [Test]
        public void CanAddMember_Success_HasCapacity()
        {
            // Assert
            Assert.IsTrue(_team.CanAddMember());
        }

        [Test]
        public void CanAddMember_Fail_Full()
        {
            // Arrange
            _team.capacity = new TeamCapacity(1);
            _team.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));

            // Assert
            Assert.IsFalse(_team.CanAddMember());
        }

        [Test]
        public void RemainingSlots_ReturnsCorrectValue()
        {
            // Arrange
            _team.capacity = new TeamCapacity(5);
            _team.AddMember(new TeamMemberData("Member1", 1, 20, 5, 3));
            _team.AddMember(new TeamMemberData("Member2", 1, 20, 5, 3));

            // Assert
            Assert.AreEqual(3, _team.RemainingSlots());
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void Export_ReturnsCorrectData()
        {
            // Arrange
            _team.AddMember(new TeamMemberData("Member1", 5, 100, 20, 10));
            _team.AddMember(new TeamMemberData("Member2", 3, 80, 15, 8));

            // Act
            var exportData = _team.Export();

            // Assert
            Assert.AreEqual(2, exportData.Count);
            Assert.AreEqual("Member1", exportData[0].name);
            Assert.AreEqual("Member2", exportData[1].name);
        }

        [Test]
        public void Import_RestoresCorrectState()
        {
            // Arrange
            _team.AddMember(new TeamMemberData("Member1", 5, 100, 20, 10));
            var exportData = _team.Export();

            // Act - 创建新实例并导入
            var newTeam = new TeamDesign();
            newTeam.Import(exportData);

            // Assert
            Assert.AreEqual(1, newTeam.memberCount);
            var importedMember = newTeam.GetAllMembers()[0];
            Assert.AreEqual("Member1", importedMember.name);
            Assert.AreEqual(5, importedMember.level);
        }

        #endregion

        #region TeamMemberData Tests

        [Test]
        public void TeamMemberData_TakeDamage_AppliesCorrectDamage()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 10); // 防御10

            // Act
            member.TakeDamage(15); // 实际伤害 = 15 - 10 = 5

            // Assert
            Assert.AreEqual(15, member.hp); // 20 - 5
        }

        [Test]
        public void TeamMemberData_TakeDamage_MinimumOneDamage()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 100); // 高防御

            // Act
            member.TakeDamage(5); // 伤害应该至少为1

            // Assert
            Assert.AreEqual(19, member.hp); // 20 - 1
        }

        [Test]
        public void TeamMemberData_Heal_IncreasesHP()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 3);
            member.TakeDamage(10);

            // Act
            member.Heal(5);

            // Assert
            Assert.AreEqual(15, member.hp);
        }

        [Test]
        public void TeamMemberData_Heal_DoesNotExceedMax()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 3);
            member.hp = 18;

            // Act
            member.Heal(10);

            // Assert
            Assert.AreEqual(20, member.hp); // 不超过maxHp
        }

        [Test]
        public void TeamMemberData_LevelUp_IncreasesStats()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 3);
            var originalAttack = member.attack;
            var originalDefense = member.defense;

            // Act
            member.LevelUp();

            // Assert
            Assert.AreEqual(2, member.level);
            Assert.AreEqual(25, member.maxHp); // 20 + 5
            Assert.AreEqual(25, member.hp); // hp = maxHp after level up
            Assert.AreEqual(originalAttack + 2, member.attack);
            Assert.AreEqual(originalDefense + 1, member.defense);
        }

        [Test]
        public void TeamMemberData_IsAlive_ReturnsTrue()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 3);

            // Assert
            Assert.IsTrue(member.IsAlive);
        }

        [Test]
        public void TeamMemberData_IsAlive_ReturnsFalse_WhenHPZero()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 20, 5, 3);
            member.hp = 0;

            // Assert
            Assert.IsFalse(member.IsAlive);
        }

        [Test]
        public void TeamMemberData_HpPercent_CalculatesCorrectly()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 100, 5, 3);
            member.hp = 50;

            // Assert
            Assert.AreEqual(0.5f, member.hpPercent);
        }

        [Test]
        public void TeamMemberData_GetCombatPower_CalculatesCorrectly()
        {
            // Arrange
            var member = new TeamMemberData("Warrior", 1, 100, 20, 10);

            // Act
            var power = member.GetCombatPower();

            // Assert
            Assert.AreEqual(80, power); // 20 + 10 + 100/2 = 80
        }

        #endregion
    }
}
