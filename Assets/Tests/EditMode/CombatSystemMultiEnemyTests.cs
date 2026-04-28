using System.Collections.Generic;
using NUnit.Framework;
using Game1.Modules.Combat;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// CombatSystem 多敌战斗回合制单元测试
    /// 验证多敌战斗的顺序攻击逻辑、闪避计算、伤害公式
    /// </summary>
    public class CombatSystemMultiEnemyTests
    {
        private CombatSystem _combatSystem;
        private PlayerActor _player;

        [SetUp]
        public void SetUp()
        {
            _combatSystem = CombatSystem.instance;
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
        }

        [Test]
        public void MultiEnemyCombat_EmptyEnemies_ReturnsNoEnemies()
        {
            // Arrange
            var enemies = new List<EnemyCombatantData>();

            // Act
            var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert
            Assert.AreEqual("没有敌人", result.endMessage);
            Assert.IsFalse(result.playerVictory);
        }

        [Test]
        public void MultiEnemyCombat_AllEnemiesDeadAtStart_ReturnsAllDead()
        {
            // Arrange
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "DeadGoblin", hp = 0, maxHp = 30, armor = 3, damage = 5 }
            };

            // Act
            var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert
            Assert.AreEqual("所有敌人都已死亡", result.endMessage);
            Assert.IsFalse(result.playerVictory);
        }

        /// <summary>
        /// 验证：玩家攻击一个敌人后，只有一个敌人能反击
        /// 当前Bug: 所有敌人同时攻击玩家，无视中间死亡
        /// </summary>
        [Test]
        public void SequentialEnemyAttacks_OnlyOneEnemyAttacksPerSubRound()
        {
            // Arrange: 3个敌人，每个伤害30，玩家HP=100
            // 第一回合：玩家攻击敌人A → 敌人A反击(30伤害) → HP=70
            // 第二回合：玩家攻击敌人B → 敌人B反击(30伤害) → HP=40
            // 如果Bug(所有敌人同时攻击): HP = 100 - 30*3 = 10 (100-90=10)
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "EnemyA", hp = 50, maxHp = 50, armor = 3, damage = 30 },
                new EnemyCombatantData { name = "EnemyB", hp = 50, maxHp = 50, armor = 3, damage = 30 },
                new EnemyCombatantData { name = "EnemyC", hp = 50, maxHp = 50, armor = 3, damage = 30 }
            };
            // 移除随机性
            _player.stats.critChance = 0f;
            _player.stats.dodgeChance = 0f;

            // Act
            var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert: 如果顺序攻击，第一回合后HP=100-30=70
            // 如果是Bug(所有敌人同时攻击)，第1回合后HP=100-90=10
            // 验证第1回合后HP应为70，而非10
            var firstEnemyRound = result.combatLog.FindAll(e => e.round == 1 && e.defenderName == _player.actorName);
            if (firstEnemyRound.Count > 0)
            {
                int damageFromFirstRound = 0;
                foreach (var entry in firstEnemyRound)
                    damageFromFirstRound += entry.damageDealt;

                // 如果顺序攻击：1个敌人反击（30伤害）
                // 如果Bug：所有敌人同时攻击（90伤害）
                Assert.AreEqual(30, damageFromFirstRound,
                    "第1回合敌人总伤害应为30（仅1个敌人反击），但当前Bug导致所有敌人同时攻击");
            }
        }

        /// <summary>
        /// 验证：玩家在敌人反击中死亡时，战斗立即结束
        /// 当前Bug: 所有敌人反击完才检查玩家死亡
        /// </summary>
        [Test]
        public void SequentialEnemyAttacks_PlayerCanDieMidRound()
        {
            // Arrange: 3个强力敌人，每个50伤害，玩家HP=100
            // 正常逻辑：第1个敌人反击(50伤害)→HP=50，第2个敌人反击(50伤害)→HP=0战斗结束
            // 第3个敌人不应攻击
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "EnemyA", hp = 50, maxHp = 50, armor = 3, damage = 50 },
                new EnemyCombatantData { name = "EnemyB", hp = 50, maxHp = 50, armor = 3, damage = 50 },
                new EnemyCombatantData { name = "EnemyC", hp = 50, maxHp = 50, armor = 3, damage = 50 }
            };
            _player.stats.critChance = 0f;
            _player.stats.dodgeChance = 0f;

            // Act
            var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert: 玩家应死亡
            Assert.IsFalse(result.playerVictory, "玩家应该被击败");

            // 验证EnemyC从未攻击（玩家在第2个敌人反击后死亡）
            int enemyCAttacks = 0;
            foreach (var entry in result.combatLog)
            {
                if (entry.attackerName == "EnemyC")
                    enemyCAttacks++;
            }
            Assert.AreEqual(0, enemyCAttacks,
                "玩家在EnemyB反击后死亡，EnemyC不应有任何攻击记录，" +
                "但当前Bug导致所有敌人攻击后才检查死亡");
        }

        /// <summary>
        /// 验证：敌人有独立的闪避属性，而非使用混用暴击率
        /// </summary>
        [Test]
        public void EnemyDodge_HasIndependentStat()
        {
            // Arrange: 敌人100%闪避（独立于critChance）
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData
                {
                    name = "DodgyGoblin",
                    hp = 50,
                    maxHp = 50,
                    armor = 3,
                    damage = 10,
                    dodgeChance = 1.0f, // 100%闪避
                    critChance = 0f     // 0%暴击（验证独立性）
                }
            };

            // Act
            var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert: 敌人有闪避属性且独立于critChance
            Assert.IsNotNull(result);

            // 验证战斗日志中有0伤害条目（闪避）
            int dodgeCount = 0;
            foreach (var entry in result.combatLog)
            {
                if (entry.damageDealt == 0 && entry.attackerName == _player.actorName)
                    dodgeCount++;
            }
            Assert.Greater(dodgeCount, 0,
                "敌人100%闪避，应记录至少一次闪避，" +
                "当前Bug使用playerDodgeChance替代敌人闪避");
        }

        /// <summary>
        /// 验证：伤害公式使用百分比减伤而非扁平减伤
        /// 当前: Mathf.Max(1, attack - defense)
        /// 预期: defense/(defense+100) 百分比减伤
        /// </summary>
        [Test]
        public void DamageReduction_WithHighDefense_PreventsZeroDamage()
        {
            // Arrange: 攻击=10，防御=100（扁平减伤→伤害=1，百分比减伤→伤害≈5）
            // 激活crit但通过输出参数拦截
            bool isCrit;

            // Act
            int damage = _combatSystem.CalculateDamage(
                attack: 10,
                defense: 100,
                critChance: 0f,
                critMultiplier: 1.5f,
                out isCrit);

            // Assert: 百分比公式下 damage > 1
            // 扁平公式下 (attack - defense = -90, Mathf.Max=1)
            Assert.Greater(damage, 1,
                $"防御远超攻击时伤害应为{5}(百分比减伤)而非1(扁平减伤)," +
                "当前公式 Mathf.Max(1, attack-defense) 导致护甲完全抵消攻击");
        }

        [Test]
        public void DamageReduction_Formula_IsPercentageBased()
        {
            // Arrange: 攻击=100，防御=100
            // 百分比减伤: 100/(100+100) = 50% → 伤害=50
            // 扁平减伤: 100-100=0, Mathf.Max=1
            bool isCrit;

            // Act
            int damage = _combatSystem.CalculateDamage(100, 100, 0f, 1.5f, out isCrit);

            // Assert
            Assert.Greater(damage, 1, "伤害应大于1（百分比减伤不归零）");
            Assert.Less(damage, 100, "伤害应小于攻击力（防御起了作用）");
        }

        [Test]
        public void MultiEnemyCombat_GoldReward_CalculatedCorrectly()
        {
            // Arrange
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "WeakGoblin", hp = 20, maxHp = 20, armor = 2, damage = 5 }
            };
            _player.stats.attack = 100; // 秒杀

            // Act
            var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);

            // Assert
            Assert.IsTrue(result.playerVictory, "玩家应胜利");
            Assert.Greater(result.goldReward, 0, "应有金币奖励");
        }

        [Test]
        public void MultiEnemyCombat_WithDodge_DoesNotCrash()
        {
            // Arrange: 给玩家高闪避
            _player.stats.dodgeChance = 1.0f; // 100%闪避
            var enemies = new List<EnemyCombatantData>
            {
                new EnemyCombatantData { name = "Goblin", hp = 50, maxHp = 50, armor = 5, damage = 10 }
            };

            // Act & Assert: 不应崩溃
            Assert.DoesNotThrow(() =>
            {
                var result = _combatSystem.ExecuteMultiEnemyCombat(_player, enemies);
                Assert.IsNotNull(result);
            });
        }
    }
}
