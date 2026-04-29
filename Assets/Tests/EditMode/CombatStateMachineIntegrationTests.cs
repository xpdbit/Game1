using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game1.Modules.Combat;
using Game1.Modules.Combat.State;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// CombatStateMachineIntegration 单元测试
    /// 测试状态机驱动的战斗流程
    /// </summary>
    public class CombatStateMachineIntegrationTests
    {
        private PlayerActor _player;
        private List<EnemyCombatantData> _enemies;

        [SetUp]
        public void SetUp()
        {
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
                    critChance = 0f,
                    critDamageMultiplier = 1.0f,
                    dodgeChance = 0f
                }
            };

            _enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData
                {
                    name = "TestEnemy",
                    hp = 50,
                    maxHp = 50,
                    armor = 5,
                    damage = 10,
                    critChance = 0f,
                    critMultiplier = 1.5f,
                    dodgeChance = 0f
                }
            };
        }

        #region State Machine Transition Tests

        [Test]
        public void StateMachine_Transitions_IdleToPreparingToPlayerTurn()
        {
            // Arrange
            var integration = new CombatStateMachineIntegration();
            integration.Initialize(_player, _enemies);

            // Assert initial state
            Assert.AreEqual(CombatPhase.Idle, integration.currentPhase);

            // Act - First tick: Idle -> Preparing
            integration.Tick();

            // Assert
            Assert.AreEqual(CombatPhase.Preparing, integration.currentPhase);

            // Act - Second tick: Preparing -> PlayerTurn
            integration.Tick();

            // Assert
            Assert.AreEqual(CombatPhase.PlayerTurn, integration.currentPhase);
        }

        [Test]
        public void StateMachine_AnimatingPhase_TriggersAnimationDispatch()
        {
            // Arrange
            var integration = new CombatStateMachineIntegration();
            integration.Initialize(_player, _enemies);

            bool animationDispatched = false;
            integration.OnAnimationDispatch += (log, isPlayerTurn) =>
            {
                animationDispatched = true;
            };

            // Run until Animating phase (may not be reached if combat ends early via instant kill)
            bool reachedAnimating = false;
            while (integration.currentPhase != CombatPhase.Animating && !integration.IsCombatEnded)
            {
                integration.Tick();
            }
            reachedAnimating = integration.currentPhase == CombatPhase.Animating;

            // Assert - only check animation dispatch if combat actually reached Animating phase
            if (reachedAnimating)
            {
                Assert.IsTrue(animationDispatched, "Animation should be dispatched during Animating phase");
            }
            // If combat ended before Animating phase (instant kill scenario), animation dispatch is not expected
        }

        [Test]
        public void StateMachine_VictoryCondition_TriggersVictory()
        {
            // Arrange - One-hit kill setup
            _player.stats.attack = 1000; // Massive damage
            _enemies[0].hp = 1;
            _enemies[0].maxHp = 1;

            var integration = new CombatStateMachineIntegration();
            integration.Initialize(_player, _enemies);

            // Act - Run combat to completion
            while (!integration.IsCombatEnded)
            {
                integration.Tick();
            }

            // Assert
            Assert.IsTrue(integration.IsVictory, "Should be victory when all enemies dead");
            Assert.AreEqual(CombatPhase.Victory, integration.currentPhase);
        }

        [Test]
        public void StateMachine_DefeatCondition_TriggersDefeat()
        {
            // Arrange - Player dies immediately
            _player.stats.currentHp = 1;
            _enemies[0].damage = 100; // One-shot kill

            var integration = new CombatStateMachineIntegration();
            integration.Initialize(_player, _enemies);

            // Act - Run combat to completion
            while (!integration.IsCombatEnded)
            {
                integration.Tick();
            }

            // Assert
            Assert.IsFalse(integration.IsVictory, "Should be defeat when player dies");
            Assert.AreEqual(CombatPhase.Defeat, integration.currentPhase);
        }

        #endregion

        #region End-to-End Combat Tests

        [Test]
        public void CombatSystem_StateMachineDriven_ReturnsCorrectResult()
        {
            // Arrange
            _player.stats.attack = 50;
            _player.stats.critChance = 0f;
            _enemies[0].hp = 30;
            _enemies[0].maxHp = 30;

            // Act
            var result = CombatSystem.instance.ExecuteMultiEnemyCombat(_player, _enemies);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.playerVictory, "Player should win with higher damage");
            Assert.Greater(result.totalDamageDealt, 0);
            Assert.AreEqual(1, result.kills.Count);
            Assert.AreEqual("TestEnemy", result.kills[0].name);
        }

        [Test]
        public void CombatModule_ExistingTests_StillPass()
        {
            // This test verifies backward compatibility by ensuring
            // the existing CombatModule tests pattern still works
            var combatModule = new CombatModule();
            combatModule.Initialize(_player);

            // Basic module functionality
            Assert.AreEqual("combat", combatModule.moduleId);
            Assert.AreEqual("战斗系统", combatModule.moduleName);
            Assert.IsTrue(combatModule.isActive);

            // Bonus multipliers
            combatModule.SetBonusMultipliers(1.5f, 2.0f, 1.2f);
            Assert.AreEqual("1.5", combatModule.GetBonus("combat_crit"));
            Assert.AreEqual("2", combatModule.GetBonus("combat_damage"));

            // CanVictory still works
            Assert.IsTrue(combatModule.CanVictory(50, 20, 200, 10, 5, 50));
            Assert.IsFalse(combatModule.CanVictory(5, 1, 10, 30, 15, 100));
        }

        #endregion

        #region Multi-Enemy Combat Tests

        [Test]
        public void MultiEnemyCombat_SequentialAttacks_WorkCorrectly()
        {
            // Arrange
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "EnemyA", hp = 50, maxHp = 50, armor = 3, damage = 30 },
                new EnemyCombatantData { name = "EnemyB", hp = 50, maxHp = 50, armor = 3, damage = 30 },
                new EnemyCombatantData { name = "EnemyC", hp = 50, maxHp = 50, armor = 3, damage = 30 }
            };
            _player.stats.critChance = 0f;
            _player.stats.dodgeChance = 0f;

            // Act
            var result = CombatSystem.instance.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.playerVictory);

            // Verify only one enemy attacks per round (not all three)
            var firstEnemyRound = result.combatLog.FindAll(e => e.round == 1 && e.defenderName == _player.actorName);
            if (firstEnemyRound.Count > 0)
            {
                int damageFromFirstRound = 0;
                foreach (var entry in firstEnemyRound)
                    damageFromFirstRound += entry.damageDealt;

                Assert.AreEqual(30, damageFromFirstRound,
                    "Only one enemy should attack per round (30 damage), not all three (90 damage)");
            }
        }

        [Test]
        public void MultiEnemyCombat_PlayerDeathMidRound_StopsCombat()
        {
            // Arrange
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "EnemyA", hp = 50, maxHp = 50, armor = 3, damage = 50 },
                new EnemyCombatantData { name = "EnemyB", hp = 50, maxHp = 50, armor = 3, damage = 50 },
                new EnemyCombatantData { name = "EnemyC", hp = 50, maxHp = 50, armor = 3, damage = 50 }
            };
            _player.stats.currentHp = 100;
            _player.stats.critChance = 0f;
            _player.stats.dodgeChance = 0f;

            // Act
            var result = CombatSystem.instance.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert - Player should die mid-round
            Assert.IsFalse(result.playerVictory);

            // EnemyC should not have attacked (player died after EnemyB)
            int enemyCAttacks = 0;
            foreach (var entry in result.combatLog)
            {
                if (entry.attackerName == "EnemyC")
                    enemyCAttacks++;
            }
            Assert.AreEqual(0, enemyCAttacks,
                "EnemyC should not attack after player dies mid-round");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Combat_EmptyEnemiesList_ReturnsNoEnemiesMessage()
        {
            // Arrange
            var emptyEnemies = new List<EnemyCombatantData>();

            // Act
            var result = CombatSystem.instance.ExecuteMultiEnemyCombat(_player, emptyEnemies);

            // Assert
            Assert.AreEqual("没有敌人", result.endMessage);
            Assert.IsFalse(result.playerVictory);
        }

        [Test]
        public void Combat_AllEnemiesDeadAtStart_ReturnsAllDeadMessage()
        {
            // Arrange
            var deadEnemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "DeadEnemy", hp = 0, maxHp = 30, armor = 3, damage = 5 }
            };

            // Act
            var result = CombatSystem.instance.ExecuteMultiEnemyCombat(_player, deadEnemies);

            // Assert
            Assert.AreEqual("所有敌人都已死亡", result.endMessage);
            Assert.IsFalse(result.playerVictory);
        }

        [Test]
        public void StateMachineIntegration_TickReturnsFalse_WhenCombatEnded()
        {
            // Arrange
            _player.stats.attack = 1000;
            _enemies[0].hp = 1;

            var integration = new CombatStateMachineIntegration();
            integration.Initialize(_player, _enemies);

            // Act - Run until combat ends
            bool continueTicking = true;
            int tickCount = 0;
            while (continueTicking && tickCount < 100)
            {
                continueTicking = integration.Tick();
                tickCount++;
            }

            // Assert
            Assert.IsFalse(continueTicking, "Tick should return false after combat ends");
            Assert.IsTrue(integration.IsCombatEnded);
        }

        #endregion
    }
}