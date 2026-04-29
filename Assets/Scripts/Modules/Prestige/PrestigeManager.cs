using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 轮回升级类型
    /// </summary>
    public enum PrestigeUpgradeType
    {
        /// <summary>资源保留+</summary>
        ResourceRetention,

        /// <summary>轮回点数加成</summary>
        PrestigePointBonus,

        /// <summary>初始装备</summary>
        StartingEquipment,

        /// <summary>技能解锁</summary>
        SkillUnlock,

        /// <summary>独特功能</summary>
        UniqueFeature
    }

    /// <summary>
    /// 轮回升级数据
    /// </summary>
    [Serializable]
    public class PrestigeUpgrade
    {
        public string id;
        public string nameTextId;
        public string descTextId;
        public int cost;
        public PrestigeUpgradeType type;
        public float effectValue;
        public bool isPurchased;
        public int currentLevel;
        public int maxLevel;

        /// <summary>
        /// 从XML元素解析升级配置
        /// </summary>
        public static PrestigeUpgrade ParseFromXml(XmlElement element)
        {
            var upgrade = new PrestigeUpgrade
            {
                id = element.SelectSingleNode("id")?.InnerText ?? string.Empty,
                nameTextId = element.SelectSingleNode("nameTextId")?.InnerText ?? string.Empty,
                descTextId = element.SelectSingleNode("descTextId")?.InnerText ?? string.Empty,
                cost = int.Parse(element.SelectSingleNode("cost")?.InnerText ?? "0"),
                effectValue = float.Parse(element.SelectSingleNode("effectValue")?.InnerText ?? "0"),
                maxLevel = int.Parse(element.SelectSingleNode("maxLevel")?.InnerText ?? "1")
            };

            // 解析type枚举
            var typeStr = element.SelectSingleNode("type")?.InnerText ?? string.Empty;
            if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse<PrestigeUpgradeType>(typeStr, out var parsedType))
                upgrade.type = parsedType;

            return upgrade;
        }
    }

    /// <summary>
    /// 轮回结果
    /// </summary>
    [Serializable]
    public class PrestigeResult
    {
        public int pointsEarned;
        public float retainedGold;
        public float retainedExpPercent;
        public List<string> retainedSkills;
        public int newPrestigeCount;
        public string message;
    }

    /// <summary>
    /// 轮回系统
    /// 管理轮回点数、轮回商店、资源保留
    /// 采用与InventoryDesign/TeamDesign相同的单例非MonoBehaviour模式
    /// </summary>
    public class PrestigeManager
    {
        #region Singleton
        private static PrestigeManager _instance;
        public static PrestigeManager instance => _instance ??= new PrestigeManager();
        #endregion

        #region Configuration
        // 基础轮回点数公式（可由XML覆盖）
        private int _basePrestigePoints = 100;
        private int _prestigePointsPerLevel = 50;

        // 资源保留基础值（可由XML覆盖）
        private float _baseGoldRetention = 0f;
        private float _baseExpRetention = 0f;

        // 轮回等级上限（可由XML覆盖）
        private int _maxPrestigeLevel = 100;
        #endregion

        #region Private Fields
        // 轮回次数
        private int _prestigeCount;

        // 轮回点数
        private int _prestigePoints;

        // 资源保留率
        private float _goldRetentionRate;
        private float _expRetentionRate;

        // 已购买的升级
        private readonly Dictionary<string, PrestigeUpgrade> _upgrades = new();

        // 保留的技能列表
        private readonly List<string> _retainedSkills = new();

        // 玩家数据引用（用于获取等级和金币）
        private PlayerActor _playerActor;
        #endregion

        #region Events
        public event Action<PrestigeResult> onPrestigeCompleted;
        public event Action<PrestigeUpgrade> onUpgradePurchased;
        #endregion

        #region Public Properties
        /// <summary>
        /// 轮回次数
        /// </summary>
        public int prestigeCount => _prestigeCount;

        /// <summary>
        /// 轮回点数
        /// </summary>
        public int prestigePoints => _prestigePoints;

        /// <summary>
        /// 金币保留率
        /// </summary>
        public float goldRetentionRate => _goldRetentionRate;

        /// <summary>
        /// 经验保留率
        /// </summary>
        public float expRetentionRate => _expRetentionRate;

        /// <summary>
        /// 获取资源保留率
        /// </summary>
        public float GetRetentionRate()
        {
            return (_goldRetentionRate + _expRetentionRate) / 2f;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            LoadUpgrades();
            Debug.Log("[PrestigeManager] Initialized");
        }

        /// <summary>
        /// 设置玩家数据引用（由GameLoopManager在创建PlayerActor后调用）
        /// </summary>
        public void SetPlayerActor(PlayerActor playerActor)
        {
            _playerActor = playerActor;
        }

        /// <summary>
        /// 导出轮回数据到存档文件
        /// </summary>
        public PrestigeSaveFile ExportToPrestigeSaveFile()
        {
            var saveFile = new PrestigeSaveFile
            {
                prestigeCount = _prestigeCount,
                prestigePoints = _prestigePoints,
                goldRetentionRate = _goldRetentionRate,
                expRetentionRate = _expRetentionRate,
                retainedSkills = new List<string>(_retainedSkills)
            };

            foreach (var kvp in _upgrades)
            {
                saveFile.purchasedUpgrades.Add(new PrestigeSaveFile.UpgradeEntry
                {
                    id = kvp.Key,
                    isPurchased = kvp.Value.isPurchased,
                    currentLevel = kvp.Value.currentLevel
                });
            }

            return saveFile;
        }

        /// <summary>
        /// 从存档文件恢复轮回数据
        /// </summary>
        public void ImportFromPrestigeSaveFile(PrestigeSaveFile saveFile)
        {
            if (saveFile == null) return;

            _prestigeCount = saveFile.prestigeCount;
            _prestigePoints = saveFile.prestigePoints;
            _goldRetentionRate = saveFile.goldRetentionRate;
            _expRetentionRate = saveFile.expRetentionRate;
            _retainedSkills.Clear();
            if (saveFile.retainedSkills != null)
            {
                _retainedSkills.AddRange(saveFile.retainedSkills);
            }

            if (saveFile.purchasedUpgrades != null)
            {
                foreach (var entry in saveFile.purchasedUpgrades)
                {
                    if (_upgrades.TryGetValue(entry.id, out var upgrade))
                    {
                        upgrade.isPurchased = entry.isPurchased;
                        upgrade.currentLevel = entry.currentLevel;
                    }
                }
            }
        }

        /// <summary>
        /// 获取玩家数据引用
        /// </summary>
        public PlayerActor GetPlayerActor()
        {
            return _playerActor;
        }

        /// <summary>
        /// 加载升级配置（优先从XML加载，失败时使用硬编码默认值）
        /// </summary>
        private void LoadUpgrades()
        {
            var loadFromXml = LoadUpgradesFromXml();
            if (!loadFromXml)
                LoadUpgradesDefault();
        }

        /// <summary>
        /// 从XML配置文件加载配置和升级
        /// </summary>
        private bool LoadUpgradesFromXml()
        {
            try
            {
                var textAsset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Data/Prestige/Prestige");
                if (textAsset == null)
                {
                    Debug.LogWarning("[PrestigeManager] Prestige XML not found, using hardcoded defaults");
                    return false;
                }

                var doc = new XmlDocument();
                doc.LoadXml(textAsset.text);

                // 1. 加载配置常数（可选，向后兼容）
                var configNode = doc.SelectSingleNode("/PrestigeUpgrades/PrestigeConfig");
                if (configNode != null && configNode.NodeType == XmlNodeType.Element)
                {
                    LoadConfigFromXml((XmlElement)configNode);
                }

                // 2. 加载升级
                var nodes = doc.SelectNodes("/PrestigeUpgrades/PrestigeUpgrade");
                if (nodes == null || nodes.Count == 0)
                {
                    Debug.LogWarning("[PrestigeManager] Prestige XML has no upgrades, using hardcoded defaults");
                    return false;
                }

                int count = 0;
                foreach (XmlNode node in nodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        var upgrade = PrestigeUpgrade.ParseFromXml((XmlElement)node);
                        if (!string.IsNullOrEmpty(upgrade.id))
                        {
                            AddUpgrade(upgrade);
                            count++;
                        }
                    }
                }

                Debug.Log($"[PrestigeManager] Loaded {count} upgrades from XML");
                return count > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PrestigeManager] Failed to load XML: {ex.Message}, using hardcoded defaults");
                return false;
            }
        }

        /// <summary>
        /// 硬编码默认升级（向后兼容）
        /// </summary>
        private void LoadUpgradesDefault()
        {
            AddUpgrade(new PrestigeUpgrade
            {
                id = "Core.Prestige.ResourceRetention",
                nameTextId = "Core.Prestige.ResourceRetention.Name",
                descTextId = "Core.Prestige.ResourceRetention.Desc",
                cost = 100,
                type = PrestigeUpgradeType.ResourceRetention,
                effectValue = 0.1f,
                maxLevel = 10
            });

            AddUpgrade(new PrestigeUpgrade
            {
                id = "Core.Prestige.PointBonus",
                nameTextId = "Core.Prestige.PointBonus.Name",
                descTextId = "Core.Prestige.PointBonus.Desc",
                cost = 150,
                type = PrestigeUpgradeType.PrestigePointBonus,
                effectValue = 0.1f,
                maxLevel = 10
            });

            AddUpgrade(new PrestigeUpgrade
            {
                id = "Core.Prestige.StartingEquipment",
                nameTextId = "Core.Prestige.StartingEquipment.Name",
                descTextId = "Core.Prestige.StartingEquipment.Desc",
                cost = 500,
                type = PrestigeUpgradeType.StartingEquipment,
                effectValue = 1,
                maxLevel = 1
            });

            AddUpgrade(new PrestigeUpgrade
            {
                id = "Core.Prestige.SkillUnlock",
                nameTextId = "Core.Prestige.SkillUnlock.Name",
                descTextId = "Core.Prestige.SkillUnlock.Desc",
                cost = 300,
                type = PrestigeUpgradeType.SkillUnlock,
                effectValue = 1,
                maxLevel = 5
            });

            Debug.Log("[PrestigeManager] Loaded 4 upgrades from hardcoded defaults");
        }

        /// <summary>
        /// 从XML加载配置常数，覆盖硬编码默认值
        /// </summary>
        private void LoadConfigFromXml(XmlElement configElement)
        {
            try
            {
                if (configElement == null) return;

                var basePoints = configElement.SelectSingleNode("basePrestigePoints")?.InnerText;
                if (!string.IsNullOrEmpty(basePoints))
                    _basePrestigePoints = int.Parse(basePoints);

                var perLevel = configElement.SelectSingleNode("pointsPerLevel")?.InnerText;
                if (!string.IsNullOrEmpty(perLevel))
                    _prestigePointsPerLevel = int.Parse(perLevel);

                var goldRet = configElement.SelectSingleNode("baseGoldRetention")?.InnerText;
                if (!string.IsNullOrEmpty(goldRet))
                    _baseGoldRetention = float.Parse(goldRet);

                var expRet = configElement.SelectSingleNode("baseExpRetention")?.InnerText;
                if (!string.IsNullOrEmpty(expRet))
                    _baseExpRetention = float.Parse(expRet);

                var maxLevel = configElement.SelectSingleNode("maxPrestigeLevel")?.InnerText;
                if (!string.IsNullOrEmpty(maxLevel))
                    _maxPrestigeLevel = int.Parse(maxLevel);

                Debug.Log($"[PrestigeManager] Loaded config: basePoints={_basePrestigePoints}, perLevel={_prestigePointsPerLevel}, maxLevel={_maxPrestigeLevel}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PrestigeManager] Failed to parse config XML: {ex.Message}, using hardcoded defaults");
            }
        }

        /// <summary>
        /// 添加升级
        /// </summary>
        private void AddUpgrade(PrestigeUpgrade upgrade)
        {
            if (upgrade == null || string.IsNullOrEmpty(upgrade.id))
                return;

            _upgrades[upgrade.id] = upgrade;
        }

        /// <summary>
        /// 检查是否可以轮回
        /// </summary>
        public bool CanPrestige()
        {
            // 至少需要达到一定等级才能轮回
            return _playerActor != null && GetPlayerLevel() >= 10;
        }

        /// <summary>
        /// 执行轮回
        /// </summary>
        public PrestigeResult ExecutePrestige()
        {
            if (!CanPrestige())
            {
                return new PrestigeResult { message = "未达到轮回条件" };
            }

            // 计算轮回点数
            int pointsEarned = CalculatePrestigePoints();

            // 计算资源保留
            int retainedGold = CalculateRetainedGold();
            float retainedExpPercent = _expRetentionRate;

            // 保留技能
            var retainedSkills = new List<string>(_retainedSkills);

            // 更新状态
            _prestigeCount++;
            _prestigePoints += pointsEarned;

            // 创建结果
            var result = new PrestigeResult
            {
                pointsEarned = pointsEarned,
                retainedGold = retainedGold,
                retainedExpPercent = retainedExpPercent,
                retainedSkills = retainedSkills,
                newPrestigeCount = _prestigeCount,
                message = $"轮回完成！获得 {pointsEarned} 轮回点数，保留 {retainedGold} 金币和 {retainedExpPercent:P0} 经验"
            };

            onPrestigeCompleted?.Invoke(result);

            return result;
        }

        /// <summary>
        /// 计算轮回点数（使用XML配置的公式常数）
        /// </summary>
        private int CalculatePrestigePoints()
        {
            // 基础点数 + (等级 * 每级点数) * (1 + 加成)
            float bonus = 0;
            if (_upgrades.TryGetValue("Core.Prestige.PointBonus", out var upgrade))
            {
                bonus = upgrade.effectValue * upgrade.currentLevel;
            }

            return (int)((_basePrestigePoints + _prestigePointsPerLevel * GetPlayerLevel()) * (1 + bonus));
        }

        /// <summary>
        /// 计算保留的金币
        /// </summary>
        private int CalculateRetainedGold()
        {
            int currentGold = _playerActor?.carryItems.gold ?? 0;
            return (int)(currentGold * _goldRetentionRate);
        }

        /// <summary>
        /// 获取玩家等级
        /// </summary>
        private int GetPlayerLevel()
        {
            return _playerActor?.level ?? 1;
        }

        /// <summary>
        /// 购买升级
        /// </summary>
        public bool PurchaseUpgrade(string upgradeId)
        {
            if (!_upgrades.TryGetValue(upgradeId, out var upgrade))
                return false;

            if (upgrade.isPurchased && upgrade.maxLevel <= 1)
                return false;

            if (upgrade.currentLevel >= upgrade.maxLevel)
                return false;

            if (_prestigePoints < upgrade.cost)
                return false;

            // 扣除点数
            _prestigePoints -= upgrade.cost;

            // 应用升级效果
            upgrade.currentLevel++;
            if (upgrade.maxLevel <= 1)
                upgrade.isPurchased = true;

            // 应用被动效果
            ApplyUpgradeEffect(upgrade);

            onUpgradePurchased?.Invoke(upgrade);

            return true;
        }

        /// <summary>
        /// 应用升级效果
        /// </summary>
        private void ApplyUpgradeEffect(PrestigeUpgrade upgrade)
        {
            switch (upgrade.type)
            {
                case PrestigeUpgradeType.ResourceRetention:
                    // 每级+effectValue资源保留（使用XML配置的基础值）
                    _goldRetentionRate = _baseGoldRetention + upgrade.effectValue * upgrade.currentLevel;
                    _expRetentionRate = _baseExpRetention + upgrade.effectValue * upgrade.currentLevel;
                    break;

                case PrestigeUpgradeType.PrestigePointBonus:
                    // 点数加成在计算时应用
                    break;

                case PrestigeUpgradeType.StartingEquipment:
                    // 初始装备效果
                    break;

                case PrestigeUpgradeType.SkillUnlock:
                    // 解锁技能
                    break;

                case PrestigeUpgradeType.UniqueFeature:
                    // 独特功能
                    break;
            }
        }

        /// <summary>
        /// 获取可用升级列表
        /// </summary>
        public List<PrestigeUpgrade> GetAvailableUpgrades()
        {
            var result = new List<PrestigeUpgrade>();
            foreach (var upgrade in _upgrades.Values)
            {
                if (upgrade.currentLevel < upgrade.maxLevel)
                    result.Add(upgrade);
            }
            return result;
        }

        /// <summary>
        /// 获取所有升级
        /// </summary>
        public List<PrestigeUpgrade> GetAllUpgrades()
        {
            return new List<PrestigeUpgrade>(_upgrades.Values);
        }

        /// <summary>
        /// 获取指定升级
        /// </summary>
        public PrestigeUpgrade GetUpgrade(string upgradeId)
        {
            return _upgrades.TryGetValue(upgradeId, out var upgrade) ? upgrade : null;
        }

        /// <summary>
        /// 添加保留技能
        /// </summary>
        public void AddRetainedSkill(string skillId)
        {
            if (!_retainedSkills.Contains(skillId))
                _retainedSkills.Add(skillId);
        }

        /// <summary>
        /// 检查是否有保留技能
        /// </summary>
        public bool HasRetainedSkill(string skillId)
        {
            return _retainedSkills.Contains(skillId);
        }

        /// <summary>
        /// 获取保留技能列表
        /// </summary>
        public List<string> GetRetainedSkills()
        {
            return new List<string>(_retainedSkills);
        }

        /// <summary>
        /// 重置轮回状态（用于新游戏）
        /// </summary>
        public void Reset()
        {
            _prestigeCount = 0;
            _prestigePoints = 0;
            _goldRetentionRate = _baseGoldRetention;
            _expRetentionRate = _baseExpRetention;
            _retainedSkills.Clear();

            foreach (var upgrade in _upgrades.Values)
            {
                upgrade.currentLevel = 0;
                upgrade.isPurchased = false;
            }
        }

        #endregion
    }
}