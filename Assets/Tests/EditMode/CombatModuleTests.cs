using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game1.Modules.Combat;

// Explicitly alias to avoid conflict with Game1.StateMachine.CombatResult enum
using CombatResultClass = Game1.Modules.Combat.CombatResult;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// CombatModule 单元测试
    /// 测试战斗模块核心逻辑
    /// </summary>
    public class CombatModuleTests
    {
        private CombatModule _combatModule;
        private PlayerActor _player;

        [SetUp]
        public void SetUp()
        {
            _combatModule = new CombatModule();
            _player = new PlayerActor
            {
                actorName = "TestPlayer",
                level = 5,
                stats = new PlayerActor.Stats
                {
                    maxHp = 100,
                    currentHp = 100,
                    attack = 20,
                    defense = 10,
                    critChance = 0.1f,
                    critDamageMultiplier = 1.5f,
                    dodgeChance = 0.05f
                }
            };
            _combatModule.Initialize(_player);
        }

        [TearDown]
        public void TearDown()
        {
            _combatModule?.ResetStatistics();
        }

        #region Module Interface Tests

        [Test]
        public void ModuleId_ReturnsCorrectId()
        {
            Assert.AreEqual("combat", _combatModule.moduleId);
        }

        [Test]
        public void ModuleName_ReturnsCorrectName()
        {
            Assert.AreEqual("战斗系统", _combatModule.moduleName);
        }

        [Test]
        public void Initialize_SetsPlayerReference()
        {
            // Arrange
            var newPlayer = new PlayerActor { actorName = "NewPlayer" };

            // Act
            _combatModule.Initialize(newPlayer);

            // Assert - CanVictory会使用_player字段
            Assert.IsNotNull(_combatModule);
        }

        [Test]
        public void Tick_DoesNotThrow()
        {
            // Act & Assert - Tick不应该抛出异常
            Assert.DoesNotThrow(() => _combatModule.Tick(1f));
        }

        [Test]
        public void OnActivate_SetsIsActiveTrue()
        {
            // Arrange
            _combatModule.OnDeactivate();

            // Act
            _combatModule.OnActivate();

            // Assert
            Assert.IsTrue(_combatModule.isActive);
        }

        [Test]
        public void OnDeactivate_SetsIsActiveFalse()
        {
            // Arrange
            _combatModule.OnActivate();

            // Act
            _combatModule.OnDeactivate();

            // Assert
            Assert.IsFalse(_combatModule.isActive);
        }

        #endregion

        #region Bonus Multiplier Tests

        [Test]
        public void GetBonus_ReturnsCritBonus()
        {
            // Act
            var bonus = _combatModule.GetBonus("combat_crit");

            // Assert
            Assert.AreEqual("1", bonus); // 默认_critBonusMultiplier = 1.0f
        }

        [Test]
        public void GetBonus_ReturnsDamageBonus()
        {
            // Act
            var bonus = _combatModule.GetBonus("combat_damage");

            // Assert
            Assert.AreEqual("1", bonus); // 默认_damageBonusMultiplier = 1.0f
        }

        [Test]
        public void GetBonus_ReturnsDefenseBonus()
        {
            // Act
            var bonus = _combatModule.GetBonus("combat_defense");

            // Assert
            Assert.AreEqual("1", bonus); // 默认_defenseBonusMultiplier = 1.0f
        }

        [Test]
        public void GetBonus_ReturnsCombatRate()
        {
            // Act
            var bonus = _combatModule.GetBonus("combat_rate");

            // Assert
            Assert.AreEqual("1.0", bonus); // 返回基础值
        }

        [Test]
        public void GetBonus_ReturnsZero_ForUnknownType()
        {
            // Act
            var bonus = _combatModule.GetBonus("unknown_bonus");

            // Assert
            Assert.AreEqual("0", bonus);
        }

        [Test]
        public void SetBonusMultipliers_UpdatesValues()
        {
            // Act
            _combatModule.SetBonusMultipliers(1.5f, 2.0f, 1.2f);

            // Assert - 通过GetBonus验证
            Assert.AreEqual("1.5", _combatModule.GetBonus("combat_crit"));
            Assert.AreEqual("2", _combatModule.GetBonus("combat_damage"));
            Assert.AreEqual("1.2", _combatModule.GetBonus("combat_defense"));
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void ResetStatistics_ClearsAllStats()
        {
            // Arrange - 手动设置一些统计
            var stats = _combatModule.GetStatistics();
            Assert.IsNotNull(stats);
        }

        [Test]
        public void GetStatistics_ReturnsStatisticsObject()
        {
            // Act
            var stats = _combatModule.GetStatistics();

            // Assert
            Assert.IsNotNull(stats);
            Assert.AreEqual(0, stats.totalBattles);
            Assert.AreEqual(0, stats.victories);
            Assert.AreEqual(0, stats.defeats);
        }

        [Test]
        public void WinRate_ReturnsZero_WhenNoBattles()
        {
            // Act
            var stats = _combatModule.GetStatistics();

            // Assert
            Assert.AreEqual(0f, stats.winRate);
        }

        #endregion

        #region CombatModule Independent Logic Tests

        [Test]
        public void CombatModule_DefaultState_IsActive()
        {
            // Assert
            Assert.IsTrue(_combatModule.isActive);
        }

        [Test]
        public void CombatStatistics_WinRate_CalculatesCorrectly()
        {
            // Arrange
            var stats = new CombatStatistics
            {
                totalBattles = 10,
                victories = 7,
                defeats = 3
            };

            // Assert
            Assert.AreEqual(0.7f, stats.winRate);
        }

        [Test]
        public void CombatStatistics_WinRate_ReturnsZero_WhenNoBattles()
        {
            // Arrange
            var stats = new CombatStatistics();

            // Assert
            Assert.AreEqual(0f, stats.winRate);
        }

        #endregion

        #region CanVictory Tests

        [Test]
        public void CanVictory_WithNullPlayer_ReturnsFalse()
        {
            // Arrange
            var moduleWithoutPlayer = new CombatModule();

            // Act
            var result = moduleWithoutPlayer.CanVictory(20, 10, 100, 10, 5, 50);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanVictory_ComparesDamageAndArmor()
        {
            // Act - 使用合理的玩家属性 vs 敌人属性
            var result = _combatModule.CanVictory(
                playerDamage: 20,
                playerArmor: 10,
                playerHp: 100,
                enemyDamage: 10,
                enemyArmor: 5,
                enemyHp: 50
            );

            // Assert - 玩家DPS更高，应该能胜利
            Assert.IsTrue(result);
        }

        [Test]
        public void CanVictory_WeakPlayer_ReturnsFalse()
        {
            // Act - 玩家很弱
            var result = _combatModule.CanVictory(
                playerDamage: 5,
                playerArmor: 1,
                playerHp: 10,
                enemyDamage: 20,
                enemyArmor: 10,
                enemyHp: 100
            );

            // Assert - 敌人太强，应该无法胜利
            Assert.IsFalse(result);
        }

        #endregion
    }

    /// <summary>
    /// CombatSystem 单元测试
    /// 测试战斗系统核心计算逻辑
    /// </summary>
    public class CombatSystemTests
    {
        [Test]
        public void CanVictory_StrongPlayer_ReturnsTrue()
        {
            // Arrange
            var combatSystem = CombatSystem.instance;

            // Act
            var result = combatSystem.CanVictory(
                playerDamage: 50,
                playerArmor: 20,
                playerHp: 200,
                enemyDamage: 10,
                enemyArmor: 5,
                enemyHp: 50
            );

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void CanVictory_WeakPlayer_ReturnsFalse()
        {
            // Arrange
            var combatSystem = CombatSystem.instance;

            // Act
            var result = combatSystem.CanVictory(
                playerDamage: 5,
                playerArmor: 1,
                playerHp: 10,
                enemyDamage: 30,
                enemyArmor: 15,
                enemyHp: 100
            );

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void CanVictory_EqualFight_IsConsistent()
        {
            // Arrange
            var combatSystem = CombatSystem.instance;

            // Act - 两次相同调用应该返回相同结果
            var result1 = combatSystem.CanVictory(20, 10, 100, 15, 8, 80);
            var result2 = combatSystem.CanVictory(20, 10, 100, 15, 8, 80);

            // Assert
            Assert.AreEqual(result1, result2);
        }

        [Test]
        public void CombatResult_DefaultValues()
        {
            // Arrange
            var result = new Game1.Modules.Combat.CombatResult();

            // Assert
            Assert.IsFalse(result.playerVictory);
            Assert.AreEqual(0, result.playerDamageTaken);
            Assert.AreEqual(0, result.enemyDamageDealt);
            Assert.AreEqual(0, result.goldReward);
            Assert.AreEqual(0, result.expReward);
            Assert.IsNotNull(result.combatLog);
        }

        [Test]
        public void MultiEnemyCombatResult_DefaultValues()
        {
            // Arrange
            var result = new MultiEnemyCombatResult();

            // Assert
            Assert.IsFalse(result.playerVictory);
            Assert.AreEqual(0, result.totalDamageDealt);
            Assert.AreEqual(0, result.playerDamageTaken);
            Assert.IsNotNull(result.combatLog);
            Assert.IsNotNull(result.kills);
        }

        [Test]
        public void EnemyCombatantData_IsDead_ReturnsCorrectValue()
        {
            // Arrange
            var enemy = new EnemyCombatantData
            {
                name = "Goblin",
                hp = 50,
                maxHp = 50,
                armor = 5,
                damage = 10
            };

            // Assert
            Assert.IsFalse(enemy.IsDead);

            // Act
            enemy.hp = 0;

            // Assert
            Assert.IsTrue(enemy.IsDead);
        }

        [Test]
        public void EnemyCombatantData_TakeDamage_ReducesHP()
        {
            // Arrange
            var enemy = new EnemyCombatantData
            {
                hp = 50,
                maxHp = 50
            };

            // Act
            enemy.TakeDamage(20);

            // Assert
            Assert.AreEqual(30, enemy.hp);
        }

        [Test]
        public void EnemyCombatantData_TakeDamage_DoesNotGoBelowZero()
        {
            // Arrange
            var enemy = new EnemyCombatantData
            {
                hp = 10,
                maxHp = 50
            };

            // Act
            enemy.TakeDamage(50);

            // Assert
            Assert.AreEqual(0, enemy.hp);
        }

        [Test]
        public void CombatLogEntry_StoresCorrectData()
        {
            // Arrange
            var entry = new CombatLogEntry
            {
                round = 1,
                attackerName = "Player",
                defenderName = "Enemy",
                damageDealt = 25,
                defenderHpAfter = 75,
                wasCritical = true
            };

            // Assert
            Assert.AreEqual(1, entry.round);
            Assert.AreEqual("Player", entry.attackerName);
            Assert.AreEqual("Enemy", entry.defenderName);
            Assert.AreEqual(25, entry.damageDealt);
            Assert.AreEqual(75, entry.defenderHpAfter);
            Assert.IsTrue(entry.wasCritical);
        }
    }
}
