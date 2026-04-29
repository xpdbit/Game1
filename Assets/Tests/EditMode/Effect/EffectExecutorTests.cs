#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game1;
using static Game1.Events.Effect.EffectType;
using static Game1.Events.Effect.EffectOperator;
using static Game1.Events.Effect.EffectCategory;
using UnifiedEffect = Game1.Events.Effect.UnifiedEffect;
using EffectExecutor = Game1.Events.Effect.EffectExecutor;

namespace Game1.Tests.EditMode.Effect
{
    /// <summary>
    /// EffectExecutor 单元测试
    /// 测试效果执行器的所有方法、运算符和边界情况
    /// </summary>
    public class EffectExecutorTests
    {
        /// <summary>
        /// 创建具有已知初始状态的 PlayerActor
        /// </summary>
        private PlayerActor CreatePlayerWithKnownState()
        {
            var player = new PlayerActor();
            player.stats.currentHp = 100;
            player.stats.maxHp = 100;
            player.carryItems.gold = 1000;
            player.level = 1;
            player.stats.attack = 5; // 用于缩放测试
            return player;
        }

        #region Gold Tests

        [Test]
        public void Execute_Gold_Add_IncreasesGold()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 50 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1050, player.carryItems.gold);
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("获得 50 金币"));
        }

        [Test]
        public void Execute_Gold_Subtract_DecreasesGold()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Subtract, Value = 30 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(970, player.carryItems.gold);
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("消耗 30 金币"));
        }

        [Test]
        public void Execute_Gold_Set_OverridesGold()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Set, Value = 500 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(500, player.carryItems.gold);
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("金币设为 500"));
        }

        [Test]
        public void Execute_Gold_Multiply_MultipliesGold()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Multiply, Value = 2.0f }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(2000, player.carryItems.gold);
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("金币乘以 2.0"));
        }

        [Test]
        public void Execute_Gold_Percent_IncreasesGold()
        {
            // Arrange
            var player = CreatePlayerWithKnownState(); // gold = 1000
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Percent, Value = 0.1f }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1100, player.carryItems.gold); // 1000 + 10% = 1100
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("金币 +100 (10%)"));
        }

        [Test]
        public void Execute_Gold_RandomRange_ValueInRange()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 0, RandomMin = 10, RandomMax = 20 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1, descriptions.Count);
            // Gold should increase by between 10 and 20 (inclusive)
            Assert.That(player.carryItems.gold, Is.GreaterThanOrEqualTo(1010));
            Assert.That(player.carryItems.gold, Is.LessThanOrEqualTo(1020));
        }

        #endregion

        #region HP Tests

        [Test]
        public void Execute_Heal_NotExceedingMaxHp()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 50; // 低于最大值
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Heal, Operator = Add, Value = 100 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(100, player.stats.currentHp); // 应该是100，而不是150
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("恢复 50 生命值")); // 实际只恢复了50
        }

        [Test]
        public void Execute_Damage_DecreasesHp()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 80;
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Damage, Operator = Subtract, Value = 30 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(50, player.stats.currentHp);
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("受到 30 点伤害"));
        }

        [Test]
        public void Execute_Damage_NotBelowZero()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 80;
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Damage, Operator = Subtract, Value = 999 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(0, player.stats.currentHp); // 不会低于0
            Assert.AreEqual(1, descriptions.Count);
        }

        [Test]
        public void Execute_Damage_WithScalingStat()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 100;
            player.stats.attack = 5; // ScalingStat = "attack", player.attack = 5
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Damage, Operator = Subtract, Value = 10, ScalingStat = "attack", ScalingFactor = 2.0f }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            // scaledValue = value * statValue * scalingFactor = 10 * 5 * 2 = 100
            Assert.AreEqual(0, player.stats.currentHp); // 100 - 100 = 0
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("受到 100 点伤害"));
        }

        #endregion

        #region Item Tests

        [Test]
        public void Execute_Item_Add_ReturnsDescription()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Item, Operator = Add, TargetId = "HealthPotion", Quantity = 2 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("获得"));
            Assert.That(descriptions[0], Does.Contain("2"));
        }

        #endregion

        #region Module Tests

        [Test]
        public void Execute_Module_Remove_WithExistingModule()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Module, Operator = Subtract, TargetId = "testModule" }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("移除"));
            Assert.That(descriptions[0], Does.Contain("testModule"));
        }

        #endregion

        #region Multiple Effects Tests

        [Test]
        public void Execute_MultipleEffects_ReturnsMultipleDescriptions()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 50 },
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 50 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(2, descriptions.Count);
            Assert.AreEqual(1100, player.carryItems.gold);
        }

        #endregion

        #region Empty/Null Tests

        [Test]
        public void Execute_EmptyEffects_NoChanges()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>();

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(0, descriptions.Count);
            Assert.AreEqual(1000, player.carryItems.gold);
            Assert.AreEqual(100, player.stats.currentHp);
        }

        [Test]
        public void Execute_NullEffects_ThrowsArgumentNullException()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EffectExecutor.Execute(null!, player));
        }

        #endregion

        #region ToEventResult Tests

        [Test]
        public void ToEventResult_GoldReward_CalculatesCorrectly()
        {
            // Arrange
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 100, Category = Reward },
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 50, Category = Reward }
            };

            // Act
            var result = EffectExecutor.ToEventResult(effects);

            // Assert
            Assert.AreEqual(150, result.goldReward);
            Assert.AreEqual(0, result.goldCost);
        }

        [Test]
        public void ToEventResult_GoldCost_CalculatesCorrectly()
        {
            // Arrange
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 100, Category = Reward },
                new UnifiedEffect { Type = Gold, Operator = Subtract, Value = 30 }, // Cost via Subtract
                new UnifiedEffect { Type = Gold, Operator = Add, Value = 20, Category = Reward }
            };

            // Act
            var result = EffectExecutor.ToEventResult(effects);

            // Assert
            Assert.AreEqual(120, result.goldReward); // 100 + 20 = 120
            Assert.AreEqual(30, result.goldCost);
        }

        [Test]
        public void ToEventResult_ModuleAdd_AddsToList()
        {
            // Arrange
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Module, Operator = Add, TargetId = "testModule" }
            };

            // Act
            var result = EffectExecutor.ToEventResult(effects);

            // Assert
            Assert.AreEqual(1, result.unlockedModuleIds.Count);
            Assert.Contains("testModule", result.unlockedModuleIds);
        }

        #endregion

        #region HP Type Tests

        [Test]
        public void Execute_HP_Add_HealPositiveValue()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 50;
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = HP, Operator = Add, Value = 30 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(80, player.stats.currentHp); // 50 + 30 = 80
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("恢复 30 生命值"));
        }

        [Test]
        public void Execute_HP_Subtract_DamagePositiveValue()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 80;
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = HP, Operator = Subtract, Value = 30 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(50, player.stats.currentHp); // 80 - 30 = 50
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("受到 30 点伤害"));
        }

        #endregion

        #region Other Effect Types Tests

        [Test]
        public void Execute_Buff_ReturnsBuffDescription()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Buff, TargetId = "SpeedBoost", Value = 50 }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("增益"));
            Assert.That(descriptions[0], Does.Contain("SpeedBoost"));
        }

        [Test]
        public void Execute_Unlock_ReturnsUnlockDescription()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Unlock, TargetId = "NewFeature" }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("解锁"));
            Assert.That(descriptions[0], Does.Contain("NewFeature"));
        }

        [Test]
        public void Execute_Combat_ReturnsCombatDescription()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = Combat }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(1, descriptions.Count);
            Assert.That(descriptions[0], Does.Contain("战斗触发"));
        }

        [Test]
        public void Execute_HP_Percent_IncreasesHp()
        {
            // Arrange
            var player = CreatePlayerWithKnownState();
            player.stats.currentHp = 50;
            var effects = new List<UnifiedEffect>
            {
                new UnifiedEffect { Type = HP, Operator = Percent, Value = 0.2f }
            };

            // Act
            var descriptions = EffectExecutor.Execute(effects, player);

            // Assert
            Assert.AreEqual(60, player.stats.currentHp); // 50 + 10% = 60 (actually 50 + 10 = 60)
            Assert.AreEqual(1, descriptions.Count);
        }

        #endregion
    }
}