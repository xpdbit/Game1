#nullable enable

using System;
using System.Collections.Generic;
using Game1;
using UnityEngine;

namespace Game1.Modules.Achievement
{
    public class AchievementDesign
    {
        #region Singleton
        private static AchievementDesign _instance;
        public static AchievementDesign instance => _instance ??= new AchievementDesign();
        #endregion

        // 内部数据
        private Dictionary<string, AchievementTemplate> _templates;
        private Dictionary<string, AchievementInstance> _instances;  // templateId -> instance
        private Dictionary<AchievementConditionType, HashSet<string>> _conditionIndex; // 条件类型索引

        // 事件
        public event Action<AchievementEventData>? onAchievementUnlocked;
        public event Action<AchievementEventData>? onProgressUpdated;

        #region 初始化
        public void Initialize()
        {
            _templates = new Dictionary<string, AchievementTemplate>();
            _instances = new Dictionary<string, AchievementInstance>();
            _conditionIndex = new Dictionary<AchievementConditionType, HashSet<string>>();
            LoadTemplates();
            CreateInstances();
            BuildConditionIndex();
        }

        private void LoadTemplates()
        {
            // 从Resources加载AchievementTemplates.xml
            // 使用 ResourceManager.Load<TextAsset>("Data/Achievements/AchievementTemplates")
            // 解析XML创建AchievementTemplate
            var asset = Resources.Load<TextAsset>("Data/Achievements/AchievementTemplates");
            if (asset == null)
            {
                Debug.LogWarning("[AchievementDesign] AchievementTemplates asset not found at Resources/Data/Achievements/AchievementTemplates");
                return;
            }

            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(asset.text);
            var root = doc.SelectSingleNode("AchievementTemplates");
            if (root == null) return;

            var nodes = root.SelectNodes("Achievement");
            if (nodes == null) return;

            foreach (System.Xml.XmlNode node in nodes)
            {
                var template = ParseAchievementTemplate(node);
                if (template != null && !string.IsNullOrEmpty(template.id))
                {
                    _templates[template.id] = template;
                }
            }
        }

        private AchievementTemplate? ParseAchievementTemplate(System.Xml.XmlNode node)
        {
            var template = new AchievementTemplate
            {
                id = node.Attributes["id"]?.Value ?? "",
                nameTextId = node.SelectSingleNode("NameTextId")?.InnerText ?? "",
                descriptionTextId = node.SelectSingleNode("DescriptionTextId")?.InnerText ?? "",
                iconPath = node.SelectSingleNode("IconPath")?.InnerText ?? "",
                category = ParseCategory(node.Attributes["category"]?.Value ?? "Exploration"),
                isHidden = bool.TryParse(node.Attributes["isHidden"]?.Value, out var hidden) && hidden,
                isIncremental = bool.TryParse(node.Attributes["isIncremental"]?.Value, out var incremental) && incremental,
                prerequisiteIds = new List<string>(),
                conditions = new List<AchievementConditionData>(),
                rewards = new List<AchievementRewardData>()
            };

            // 解析前置成就
            var prereqAttr = node.Attributes["prerequisiteIds"]?.Value ?? "";
            if (!string.IsNullOrEmpty(prereqAttr))
            {
                foreach (var id in prereqAttr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    template.prerequisiteIds.Add(id.Trim());
            }

            // 解析条件
            var conditionNodes = node.SelectSingleNode("Conditions")?.SelectNodes("Condition");
            if (conditionNodes != null)
            {
                foreach (System.Xml.XmlNode condNode in conditionNodes)
                {
                    var condition = new AchievementConditionData
                    {
                        type = ParseConditionType(condNode.Attributes["type"]?.Value ?? "GoldEarned"),
                        targetValue = float.TryParse(condNode.Attributes["targetValue"]?.Value, out var tv) ? tv : 0,
                        extraParam = condNode.Attributes["extraParam"]?.Value ?? ""
                    };
                    template.conditions.Add(condition);
                }
            }

            // 解析奖励
            var rewardNodes = node.SelectSingleNode("Rewards")?.SelectNodes("Reward");
            if (rewardNodes != null)
            {
                foreach (System.Xml.XmlNode rewardNode in rewardNodes)
                {
                    var reward = new AchievementRewardData
                    {
                        type = ParseRewardType(rewardNode.Attributes["type"]?.Value ?? "Gold"),
                        configId = rewardNode.Attributes["configId"]?.Value ?? "",
                        amount = int.TryParse(rewardNode.Attributes["amount"]?.Value, out var amt) ? amt : 0
                    };
                    template.rewards.Add(reward);
                }
            }

            return template;
        }

        private void CreateInstances()
        {
            foreach (var kvp in _templates)
            {
                var template = kvp.Value;
                var instance = new AchievementInstance
                {
                    templateId = template.id,
                    isUnlocked = false,
                    conditionProgress = new float[template.conditions.Count],
                    unlockedAtTimestamp = 0
                };
                _instances[template.id] = instance;
            }
        }

        private void BuildConditionIndex()
        {
            // 按条件类型建立索引，优化性能
            // 遍历所有模板，对于每个模板的每个条件，添加到_conditionIndex
            // AchievementConditionType -> HashSet<templateId>
            _conditionIndex.Clear();
            foreach (var kvp in _templates)
            {
                var template = kvp.Value;
                foreach (var condition in template.conditions)
                {
                    if (!_conditionIndex.ContainsKey(condition.type))
                        _conditionIndex[condition.type] = new HashSet<string>();
                    _conditionIndex[condition.type].Add(template.id);
                }
            }
        }

        private static AchievementCategory ParseCategory(string value)
        {
            return value switch
            {
                "Combat" => AchievementCategory.Combat,
                "Collection" => AchievementCategory.Collection,
                "Team" => AchievementCategory.Team,
                "Special" => AchievementCategory.Special,
                "Hidden" => AchievementCategory.Hidden,
                _ => AchievementCategory.Exploration
            };
        }

        private static AchievementConditionType ParseConditionType(string value)
        {
            return value switch
            {
                "GoldEarned" => AchievementConditionType.GoldEarned,
                "EnemiesDefeated" => AchievementConditionType.EnemiesDefeated,
                "DistanceTraveled" => AchievementConditionType.DistanceTraveled,
                "ItemsCollected" => AchievementConditionType.ItemsCollected,
                "TeamMembers" => AchievementConditionType.TeamMembers,
                "LevelsGained" => AchievementConditionType.LevelsGained,
                "EventsCompleted" => AchievementConditionType.EventsCompleted,
                "PrestigesPerformed" => AchievementConditionType.PrestigesPerformed,
                "CombatWon" => AchievementConditionType.CombatWon,
                "BossesDefeated" => AchievementConditionType.BossesDefeated,
                "LocationsDiscovered" => AchievementConditionType.LocationsDiscovered,
                "PetsMaxHappiness" => AchievementConditionType.PetsMaxHappiness,
                _ => AchievementConditionType.GoldEarned
            };
        }

        private static RewardType ParseRewardType(string value)
        {
            return value switch
            {
                "Item" => RewardType.Item,
                "Experience" => RewardType.Experience,
                "Title" => RewardType.Title,
                _ => RewardType.Gold
            };
        }
        #endregion

        #region 条件检查 - 核心路径
        // Oracle审查建议: 使用条件类型索引替代全量遍历
        public void UpdateConditionProgress(AchievementConditionType type, float deltaValue)
        {
            if (!_conditionIndex.TryGetValue(type, out var affectedTemplateIds))
                return;  // 没有成就关心这个条件类型

            foreach (var templateId in affectedTemplateIds)
            {
                var instance = _instances[templateId];
                if (instance.isUnlocked) continue;

                // 更新对应条件的进度
                var template = _templates[templateId];
                for (int i = 0; i < template.conditions.Count; i++)
                {
                    if (template.conditions[i].type == type)
                    {
                        instance.conditionProgress[i] += deltaValue;
                    }
                }

                // 事件通知
                onProgressUpdated?.Invoke(new AchievementEventData
                {
                    eventType = AchievementEventType.AchievementProgressUpdated,
                    achievementId = templateId,
                    progress = GetProgressPercent(instance),
                });

                // 检查是否可解锁
                CheckAndUnlock(instance);
            }
        }

        private float GetProgressPercent(AchievementInstance instance)
        {
            var template = _templates[instance.templateId];
            if (template.conditions.Count == 0) return 0;
            float total = 0;
            for (int i = 0; i < template.conditions.Count; i++)
            {
                if (template.conditions[i].targetValue > 0)
                    total += Mathf.Clamp01(instance.conditionProgress[i] / template.conditions[i].targetValue);
            }
            return total / template.conditions.Count;
        }
        #endregion

        #region 解锁检查
        private void CheckAndUnlock(AchievementInstance instance)
        {
            var template = _templates[instance.templateId];

            // 检查前置成就 (Momus问题#4)
            if (template.prerequisiteIds != null && template.prerequisiteIds.Count > 0)
            {
                foreach (var prereqId in template.prerequisiteIds)
                {
                    if (!_instances.ContainsKey(prereqId) || !_instances[prereqId].isUnlocked)
                        return;  // 前置未解锁
                }
            }

            // 检查所有条件是否满足
            for (int i = 0; i < template.conditions.Count; i++)
            {
                if (instance.conditionProgress[i] < template.conditions[i].targetValue)
                    return;  // 条件未满足
            }

            // 全部满足 → 解锁
            UnlockAchievement(instance);
        }

        private void UnlockAchievement(AchievementInstance instance)
        {
            instance.isUnlocked = true;
            instance.unlockedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var template = _templates[instance.templateId];
            onAchievementUnlocked?.Invoke(new AchievementEventData
            {
                eventType = AchievementEventType.AchievementUnlocked,
                achievementId = instance.templateId,
                achievementName = template.nameTextId,
                progress = 1.0f,
                targetProgress = 1.0f
            });

            // 发放奖励
            GrantRewards(instance.templateId);
        }

        private void GrantRewards(string templateId)
        {
            var template = _templates[templateId];
            var player = GameLoopManager.instance?.player;
            foreach (var reward in template.rewards)
            {
                switch (reward.type)
                {
                    case RewardType.Gold:
                        if (player != null)
                            player.carryItems.gold += reward.amount;
                        Debug.Log($"[AchievementDesign] Granting Gold reward: {reward.amount}");
                        break;
                    case RewardType.Item:
                        if (!string.IsNullOrEmpty(reward.configId))
                            InventoryDesign.instance.AddItem(reward.configId, reward.amount);
                        Debug.Log($"[AchievementDesign] Granting Item reward: {reward.configId} x{reward.amount}");
                        break;
                    case RewardType.Experience:
                        if (player != null)
                            player.AddExp(reward.amount);
                        Debug.Log($"[AchievementDesign] Granting EXP reward: {reward.amount}");
                        break;
                    case RewardType.Title:
                        Debug.Log($"[AchievementDesign] Granting Title reward: {reward.configId} (title system TBD)");
                        break;
                }
            }
        }
        #endregion

        #region 存档
        public AchievementSaveData Export()
        {
            var data = new AchievementSaveData();
            data.records = new List<AchievementRecord>();
            foreach (var kvp in _instances)
            {
                data.records.Add(new AchievementRecord
                {
                    templateId = kvp.Key,
                    isUnlocked = kvp.Value.isUnlocked,
                    conditionProgress = kvp.Value.conditionProgress,
                    unlockedAtTimestamp = kvp.Value.unlockedAtTimestamp
                });
            }
            return data;
        }

        public void Import(AchievementSaveData data)
        {
            if (data?.records == null) return;
            foreach (var record in data.records)
            {
                if (_instances.ContainsKey(record.templateId))
                {
                    _instances[record.templateId].isUnlocked = record.isUnlocked;
                    _instances[record.templateId].conditionProgress = record.conditionProgress;
                    _instances[record.templateId].unlockedAtTimestamp = record.unlockedAtTimestamp;
                }
            }
        }
        #endregion

        #region 查询API
        public AchievementTemplate? GetTemplate(string templateId)
            => _templates.TryGetValue(templateId, out var t) ? t : null;

        public AchievementInstance? GetInstance(string templateId)
            => _instances.TryGetValue(templateId, out var i) ? i : null;

        public List<AchievementUIData> GetAllAchievements()
        {
            var list = new List<AchievementUIData>();
            foreach (var kvp in _instances)
            {
                var template = _templates[kvp.Key];
                var instance = kvp.Value;
                list.Add(new AchievementUIData
                {
                    templateId = kvp.Key,
                    displayName = template.nameTextId,
                    description = template.descriptionTextId,
                    isUnlocked = instance.isUnlocked,
                    progress = GetProgressPercent(instance),
                    category = template.category,
                    isHidden = template.isHidden,
                    iconPath = template.iconPath,
                });
            }
            return list;
        }

        public List<AchievementUIData> GetByCategory(AchievementCategory category)
        {
            return GetAllAchievements().FindAll(a => a.category == category);
        }

        public int GetTotalUnlockedCount()
        {
            int count = 0;
            foreach (var kvp in _instances)
                if (kvp.Value.isUnlocked) count++;
            return count;
        }
        #endregion

        #region 事件处理器 (存根 - 等待EventBus集成)
        // 这些方法将由EventBus事件触发
        public void OnGoldChanged(float delta) => UpdateConditionProgress(AchievementConditionType.GoldEarned, delta);
        public void OnEnemyDefeated() => UpdateConditionProgress(AchievementConditionType.EnemiesDefeated, 1);
        public void OnTravelCompleted(float distance) => UpdateConditionProgress(AchievementConditionType.DistanceTraveled, distance);
        public void OnItemCollected() => UpdateConditionProgress(AchievementConditionType.ItemsCollected, 1);
        public void OnLevelUp() => UpdateConditionProgress(AchievementConditionType.LevelsGained, 1);
        public void OnCombatWon() => UpdateConditionProgress(AchievementConditionType.CombatWon, 1);
        public void OnBossDefeated() => UpdateConditionProgress(AchievementConditionType.BossesDefeated, 1);
        public void OnPrestige() => UpdateConditionProgress(AchievementConditionType.PrestigesPerformed, 1);
        public void OnLocationDiscovered() => UpdateConditionProgress(AchievementConditionType.LocationsDiscovered, 1);
        public void OnPetMaxHappiness() => UpdateConditionProgress(AchievementConditionType.PetsMaxHappiness, 1);
        public void OnTeamMemberChanged() => UpdateConditionProgress(AchievementConditionType.TeamMembers, 1);
        public void OnEventCompleted() => UpdateConditionProgress(AchievementConditionType.EventsCompleted, 1);
        #endregion
    }

    // 存档数据结构
    [System.Serializable]
    public class AchievementSaveData
    {
        public int version = 1;
        public List<AchievementRecord> records = new List<AchievementRecord>();
    }
}