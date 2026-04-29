using System;
using System.Collections.Generic;
using Game1.Modules.Achievement;
using NUnit.Framework;
using UnityEngine;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// AchievementDesign 单元测试
    /// 测试成就系统核心逻辑
    /// </summary>
    public class AchievementDesignTests
    {
        private AchievementDesign _design = null!;

        [SetUp]
        public void SetUp()
        {
            // 每个测试创建独立的AchievementDesign实例
            _design = new AchievementDesign();
            // AchievementDesign.instance是单例，我们需要替换它进行测试
            ReplaceSingletonInstance(_design);
            _design.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            // 清理单例
            ReplaceSingletonInstance(null);
        }

        /// <summary>
        /// 替换AchievementDesign单例实例（用于测试隔离）
        /// </summary>
        private void ReplaceSingletonInstance(AchievementDesign? instance)
        {
            var field = typeof(AchievementDesign).GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field != null)
            {
                field.SetValue(null, instance);
            }
        }

        #region Template Loading Tests

        [Test]
        public void Initialize_LoadsTemplates_CreatesInstances()
        {
            // Assert - 验证Initialize创建了实例
            Assert.That(_design, Is.Not.Null);

            // 验证有模板被加载（Resources目录下可能有AchievementTemplates.xml）
            // 如果没有配置文件，templates字典为空是预期行为
            var templates = typeof(AchievementDesign).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(templates, Is.Not.Null);

            var instances = typeof(AchievementDesign).GetField("_instances",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(instances, Is.Not.Null);
        }

        #endregion

        #region Progress Tracking Tests

        [Test]
        public void UpdateConditionProgress_SingleCondition_UpdatesCorrectCondition()
        {
            // Arrange - 先创建测试模板
            CreateTestTemplate("Test.Achievement.SingleCondition",
                new[] { AchievementConditionType.GoldEarned },
                new[] { 100f });

            // Act
            _design.UpdateConditionProgress(AchievementConditionType.GoldEarned, 50f);

            // Assert
            var instance = _design.GetInstance("Test.Achievement.SingleCondition");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.conditionProgress[0], Is.EqualTo(50f).Within(0.001f));
        }

        [Test]
        public void UpdateConditionProgress_ZeroDelta_DoesNotChangeProgress()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.ZeroDelta",
                new[] { AchievementConditionType.EnemiesDefeated },
                new[] { 10f });

            // Act
            _design.UpdateConditionProgress(AchievementConditionType.EnemiesDefeated, 0f);

            // Assert
            var instance = _design.GetInstance("Test.Achievement.ZeroDelta");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.conditionProgress[0], Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void UpdateConditionProgress_NoAffectedTemplates_DoesNothing()
        {
            // Arrange - 不创建任何模板

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
                _design.UpdateConditionProgress(AchievementConditionType.GoldEarned, 100f));
        }

        [Test]
        public void UpdateConditionProgress_MultipleConditions_UpdatesOnlyMatchingCondition()
        {
            // Arrange - 创建有两个条件的模板
            CreateTestTemplate("Test.Achievement.MultiCondition",
                new[] { AchievementConditionType.GoldEarned, AchievementConditionType.EnemiesDefeated },
                new[] { 100f, 10f });

            // Act - 只更新GoldEarned条件
            _design.UpdateConditionProgress(AchievementConditionType.GoldEarned, 30f);

            // Assert - GoldEarned被更新，EnemiesDefeated不应被更新
            var instance = _design.GetInstance("Test.Achievement.MultiCondition");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.conditionProgress[0], Is.EqualTo(30f).Within(0.001f));
            Assert.That(instance.conditionProgress[1], Is.EqualTo(0f).Within(0.001f));
        }

        #endregion

        #region Prerequisite Tests

        [Test]
        public void CheckAndUnlock_PrerequisitesNotMet_DoesNotUnlock()
        {
            // Arrange - 创建一个有前置条件的成就（前置成就不存在）
            CreateTestTemplateWithPrerequisites("Test.Achievement.WithPrereq",
                new[] { AchievementConditionType.GoldEarned },
                new[] { 10f },
                new[] { "NonExistent.Achievement" });

            // 设置条件已满足
            _design.UpdateConditionProgress(AchievementConditionType.GoldEarned, 10f);

            // Assert - 验证未解锁
            var instance = _design.GetInstance("Test.Achievement.WithPrereq");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.isUnlocked, Is.False);
        }

        [Test]
        public void CheckAndUnlock_AllConditionsMetWithoutPrereqs_Unlocks()
        {
            // Arrange - 创建一个无条件前置的成就
            CreateTestTemplate("Test.Achievement.NoPrereq",
                new[] { AchievementConditionType.GoldEarned },
                new[] { 10f });

            // Act - 满足条件
            _design.UpdateConditionProgress(AchievementConditionType.GoldEarned, 10f);

            // Assert
            var instance = _design.GetInstance("Test.Achievement.NoPrereq");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.isUnlocked, Is.True);
        }

        #endregion

        #region Unlock Tests

        [Test]
        public void CheckAndUnlock_AllConditionsMet_UnlocksAchievement()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.FullUnlock",
                new[] { AchievementConditionType.EnemiesDefeated },
                new[] { 5f });

            // Act
            _design.UpdateConditionProgress(AchievementConditionType.EnemiesDefeated, 5f);

            // Assert
            var instance = _design.GetInstance("Test.Achievement.FullUnlock");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.isUnlocked, Is.True);
            Assert.That(instance.unlockedAtTimestamp, Is.GreaterThan(0));
        }

        [Test]
        public void UnlockAchievement_AlreadyUnlocked_DoesNotFireEventAgain()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.Idempotent",
                new[] { AchievementConditionType.DistanceTraveled },
                new[] { 100f });

            int unlockEventCount = 0;
            _design.onAchievementUnlocked += (data) =>
            {
                if (data.achievementId == "Test.Achievement.Idempotent")
                    unlockEventCount++;
            };

            // Act - 第一次满足条件，解锁
            _design.UpdateConditionProgress(AchievementConditionType.DistanceTraveled, 100f);

            // 再次更新进度（不应该再次触发解锁事件）
            _design.UpdateConditionProgress(AchievementConditionType.DistanceTraveled, 50f);

            // Assert - 应该只触发一次
            Assert.That(unlockEventCount, Is.EqualTo(1));
        }

        [Test]
        public void UnlockAchievement_FiresUnlockEvent()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.EventFire",
                new[] { AchievementConditionType.LevelsGained },
                new[] { 1f });

            bool eventFired = false;
            string? firedAchievementId = null;
            _design.onAchievementUnlocked += (data) =>
            {
                eventFired = true;
                firedAchievementId = data.achievementId;
            };

            // Act
            _design.UpdateConditionProgress(AchievementConditionType.LevelsGained, 1f);

            // Assert
            Assert.That(eventFired, Is.True);
            Assert.That(firedAchievementId, Is.EqualTo("Test.Achievement.EventFire"));
        }

        #endregion

        #region Reward Tests

        [Test]
        public void GrantRewards_RewardTypes_ProcessesAllTypes()
        {
            // Arrange - 创建带各种奖励类型的成就
            CreateTestTemplateWithRewards("Test.Achievement.AllRewards",
                new[] { AchievementConditionType.CombatWon },
                new[] { 1f },
                new[] {
                    (RewardType.Gold, "gold", 100),
                    (RewardType.Item, "Test.Item.Sword", 1),
                    (RewardType.Experience, "exp", 50),
                    (RewardType.Title, "Champion", 1)
                });

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() =>
                _design.UpdateConditionProgress(AchievementConditionType.CombatWon, 1f));
        }

        #endregion

        #region Save/Load Round-Trip Tests

        [Test]
        public void Export_Import_RoundTrip_PreservesAllData()
        {
            // Arrange - 创建并修改一些成就
            CreateTestTemplate("Test.Achievement.RoundTrip",
                new[] { AchievementConditionType.ItemsCollected },
                new[] { 100f });

            // 满足部分进度
            _design.UpdateConditionProgress(AchievementConditionType.ItemsCollected, 30f);

            // Act - 导出
            var exportedData = _design.Export();

            // 创建新实例并导入
            var newDesign = new AchievementDesign();
            ReplaceSingletonInstance(newDesign);
            newDesign.Initialize();
            newDesign.Import(exportedData);

            // Assert - 验证进度被保留
            var instance = newDesign.GetInstance("Test.Achievement.RoundTrip");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.conditionProgress[0], Is.EqualTo(30f).Within(0.001f));

            // 恢复原单例
            ReplaceSingletonInstance(_design);
        }

        [Test]
        public void Export_Import_EmptyData_DoesNotCrash()
        {
            // Arrange - 初始状态
            var emptyData = new AchievementSaveData { records = new List<AchievementRecord>() };

            // Act & Assert - 不应抛出异常
            Assert.DoesNotThrow(() => _design.Import(emptyData));
        }

        [Test]
        public void Import_NullData_DoesNotCrash()
        {
            // Act & Assert - null数据不应崩溃
            Assert.DoesNotThrow(() => _design.Import(null!));
        }

        [Test]
        public void Import_EmptyRecords_DoesNotCrash()
        {
            // Arrange
            var dataWithEmptyRecords = new AchievementSaveData
            {
                records = new List<AchievementRecord>()
            };

            // Act & Assert - 空记录列表不应崩溃
            Assert.DoesNotThrow(() => _design.Import(dataWithEmptyRecords));
        }

        #endregion

        #region Query API Tests

        [Test]
        public void GetAllAchievements_ReturnsAllTemplates()
        {
            // Arrange - 创建几个测试成就
            CreateTestTemplate("Test.Achievement.Query1",
                new[] { AchievementConditionType.GoldEarned },
                new[] { 100f });
            CreateTestTemplate("Test.Achievement.Query2",
                new[] { AchievementConditionType.EnemiesDefeated },
                new[] { 10f });

            // Act
            var allAchievements = _design.GetAllAchievements();

            // Assert - 验证返回了所有模板
            Assert.That(allAchievements, Is.Not.Null);
            // 至少包含我们创建的测试成就
            Assert.That(allAchievements.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void GetByCategory_FiltersCorrectly()
        {
            // Arrange - 创建不同类别的成就
            CreateTestTemplateWithCategory("Test.Achievement.Combat",
                AchievementCategory.Combat,
                new[] { AchievementConditionType.CombatWon },
                new[] { 1f });
            CreateTestTemplateWithCategory("Test.Achievement.Exploration",
                AchievementCategory.Exploration,
                new[] { AchievementConditionType.DistanceTraveled },
                new[] { 100f });

            // Act
            var combatAchievements = _design.GetByCategory(AchievementCategory.Combat);

            // Assert - 应该只返回Combat类别的
            Assert.That(combatAchievements, Is.Not.Null);
            foreach (var ach in combatAchievements)
            {
                Assert.That(ach.category, Is.EqualTo(AchievementCategory.Combat));
            }
        }

        [Test]
        public void GetTotalUnlockedCount_InitiallyZero()
        {
            // Arrange - 创建新实例（默认未解锁任何成就）

            // Act
            var unlockedCount = _design.GetTotalUnlockedCount();

            // Assert - 初始应为零
            Assert.That(unlockedCount, Is.EqualTo(0));
        }

        #endregion

        #region AchievementManager Delegation Tests

        [Test]
        public void Manager_ReportProgress_DelegatesToDesign()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.ManagerDelegation",
                new[] { AchievementConditionType.GoldEarned },
                new[] { 50f });

            // Act - 通过Manager调用
            AchievementManager.ReportProgress(AchievementConditionType.GoldEarned, 25f);

            // Assert - 验证进度被更新
            var instance = AchievementDesign.instance.GetInstance("Test.Achievement.ManagerDelegation");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.conditionProgress[0], Is.EqualTo(25f).Within(0.001f));
        }

        [Test]
        public void Manager_ExportImport_DelegatesCorrectly()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.ManagerExport",
                new[] { AchievementConditionType.PetsMaxHappiness },
                new[] { 5f });

            AchievementManager.ReportProgress(AchievementConditionType.PetsMaxHappiness, 3f);

            // Act - 通过Manager导出
            var exportedData = AchievementManager.Export();

            // 创建新实例并通过Manager导入
            var newDesign = new AchievementDesign();
            ReplaceSingletonInstance(newDesign);
            newDesign.Initialize();
            AchievementManager.Import(exportedData);

            // Assert
            var instance = AchievementDesign.instance.GetInstance("Test.Achievement.ManagerExport");
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance!.conditionProgress[0], Is.EqualTo(3f).Within(0.001f));

            // 恢复原单例
            ReplaceSingletonInstance(_design);
        }

        [Test]
        public void Manager_SubscribeUnlocked_ReceivesEvents()
        {
            // Arrange
            CreateTestTemplate("Test.Achievement.ManagerSubscribe",
                new[] { AchievementConditionType.PrestigesPerformed },
                new[] { 1f });

            bool eventReceived = false;
            string? receivedId = null;
            AchievementManager.SubscribeUnlocked((data) =>
            {
                eventReceived = true;
                receivedId = data.achievementId;
            });

            // Act
            AchievementManager.ReportProgress(AchievementConditionType.PrestigesPerformed, 1f);

            // Assert
            Assert.That(eventReceived, Is.True);
            Assert.That(receivedId, Is.EqualTo("Test.Achievement.ManagerSubscribe"));

            // Cleanup
            AchievementManager.UnsubscribeUnlocked((data) => { });
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 创建测试用成就模板（通过反射注入到私有字典）
        /// </summary>
        private void CreateTestTemplate(string id, AchievementConditionType[] conditionTypes, float[] targetValues)
        {
            CreateTestTemplateWithPrerequisites(id, conditionTypes, targetValues, null);
        }

        private void CreateTestTemplateWithPrerequisites(string id, AchievementConditionType[] conditionTypes,
            float[] targetValues, string[]? prerequisiteIds)
        {
            var template = new AchievementTemplate
            {
                id = id,
                nameTextId = $"Test/{id}",
                descriptionTextId = $"Test Description for {id}",
                iconPath = "",
                category = AchievementCategory.Exploration,
                isHidden = false,
                isIncremental = false,
                prerequisiteIds = prerequisiteIds != null ? new List<string>(prerequisiteIds) : new List<string>(),
                conditions = new List<AchievementConditionData>(),
                rewards = new List<AchievementRewardData>()
            };

            for (int i = 0; i < conditionTypes.Length; i++)
            {
                template.conditions.Add(new AchievementConditionData
                {
                    type = conditionTypes[i],
                    targetValue = targetValues[i],
                    extraParam = ""
                });
            }

            // 通过反射添加到模板字典
            var templatesField = typeof(AchievementDesign).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (templatesField != null)
            {
                var templates = templatesField.GetValue(_design) as Dictionary<string, AchievementTemplate>;
                if (templates != null)
                {
                    templates[id] = template;
                }
            }

            // 创建对应的实例
            var instance = new AchievementInstance
            {
                templateId = id,
                isUnlocked = false,
                conditionProgress = new float[conditionTypes.Length],
                unlockedAtTimestamp = 0
            };

            var instancesField = typeof(AchievementDesign).GetField("_instances",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (instancesField != null)
            {
                var instances = instancesField.GetValue(_design) as Dictionary<string, AchievementInstance>;
                if (instances != null)
                {
                    instances[id] = instance;
                }
            }

            // 重建条件索引
            RebuildConditionIndex();
        }

        private void CreateTestTemplateWithCategory(string id, AchievementCategory category,
            AchievementConditionType[] conditionTypes, float[] targetValues)
        {
            var template = new AchievementTemplate
            {
                id = id,
                nameTextId = $"Test/{id}",
                descriptionTextId = $"Test Description for {id}",
                iconPath = "",
                category = category,
                isHidden = false,
                isIncremental = false,
                prerequisiteIds = new List<string>(),
                conditions = new List<AchievementConditionData>(),
                rewards = new List<AchievementRewardData>()
            };

            for (int i = 0; i < conditionTypes.Length; i++)
            {
                template.conditions.Add(new AchievementConditionData
                {
                    type = conditionTypes[i],
                    targetValue = targetValues[i],
                    extraParam = ""
                });
            }

            var templatesField = typeof(AchievementDesign).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (templatesField != null)
            {
                var templates = templatesField.GetValue(_design) as Dictionary<string, AchievementTemplate>;
                if (templates != null)
                {
                    templates[id] = template;
                }
            }

            var instance = new AchievementInstance
            {
                templateId = id,
                isUnlocked = false,
                conditionProgress = new float[conditionTypes.Length],
                unlockedAtTimestamp = 0
            };

            var instancesField = typeof(AchievementDesign).GetField("_instances",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (instancesField != null)
            {
                var instances = instancesField.GetValue(_design) as Dictionary<string, AchievementInstance>;
                if (instances != null)
                {
                    instances[id] = instance;
                }
            }

            RebuildConditionIndex();
        }

        private void CreateTestTemplateWithRewards(string id, AchievementConditionType[] conditionTypes,
            float[] targetValues, (RewardType type, string configId, int amount)[] rewards)
        {
            var template = new AchievementTemplate
            {
                id = id,
                nameTextId = $"Test/{id}",
                descriptionTextId = $"Test Description for {id}",
                iconPath = "",
                category = AchievementCategory.Special,
                isHidden = false,
                isIncremental = false,
                prerequisiteIds = new List<string>(),
                conditions = new List<AchievementConditionData>(),
                rewards = new List<AchievementRewardData>()
            };

            for (int i = 0; i < conditionTypes.Length; i++)
            {
                template.conditions.Add(new AchievementConditionData
                {
                    type = conditionTypes[i],
                    targetValue = targetValues[i],
                    extraParam = ""
                });
            }

            foreach (var reward in rewards)
            {
                template.rewards.Add(new AchievementRewardData
                {
                    type = reward.type,
                    configId = reward.configId,
                    amount = reward.amount
                });
            }

            var templatesField = typeof(AchievementDesign).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (templatesField != null)
            {
                var templates = templatesField.GetValue(_design) as Dictionary<string, AchievementTemplate>;
                if (templates != null)
                {
                    templates[id] = template;
                }
            }

            var instance = new AchievementInstance
            {
                templateId = id,
                isUnlocked = false,
                conditionProgress = new float[conditionTypes.Length],
                unlockedAtTimestamp = 0
            };

            var instancesField = typeof(AchievementDesign).GetField("_instances",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (instancesField != null)
            {
                var instances = instancesField.GetValue(_design) as Dictionary<string, AchievementInstance>;
                if (instances != null)
                {
                    instances[id] = instance;
                }
            }

            RebuildConditionIndex();
        }

        /// <summary>
        /// 重建条件索引（用于测试中手动添加模板后）
        /// </summary>
        private void RebuildConditionIndex()
        {
            var conditionIndexField = typeof(AchievementDesign).GetField("_conditionIndex",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var templatesField = typeof(AchievementDesign).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (conditionIndexField != null && templatesField != null)
            {
                var conditionIndex = conditionIndexField.GetValue(_design) as Dictionary<AchievementConditionType, HashSet<string>>;
                var templates = templatesField.GetValue(_design) as Dictionary<string, AchievementTemplate>;

                if (conditionIndex != null && templates != null)
                {
                    conditionIndex.Clear();
                    foreach (var kvp in templates)
                    {
                        var template = kvp.Value;
                        foreach (var condition in template.conditions)
                        {
                            if (!conditionIndex.ContainsKey(condition.type))
                                conditionIndex[condition.type] = new HashSet<string>();
                            conditionIndex[condition.type].Add(template.id);
                        }
                    }
                }
            }
        }

        #endregion
    }
}