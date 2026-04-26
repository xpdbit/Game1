#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace Game1.Simulation
{
    // ============================================
    // 枚举定义
    // ============================================

    public enum ItemType { Food, Weapon, Armor, Accessory, Mount, Consumable, Material, QuestItem, Money }
    public enum CardRarity { N, R, SR, SSR, UR, GR }
    public enum CardType { Character, Equipment, Skill, Item, Event }
    public enum GachaType { Novice, Standard, Premium, Limited }
    public enum JobType { None, Merchant, Escort, Scholar, Healer }
    public enum TravelStatus { Idle, Traveling, AwaitingChoice, EventActive, ProgressMilestone }
    public enum GameEventType { Random, Trade, Combat, Discovery, Mystery }
    public enum LocationType { Start, City, Wilderness, Market, Dungeon, Boss, Goal }
    public enum PrestigeUpgradeType { ResourceRetention, PrestigePointBonus, StartingEquipment, SkillUnlock, UniqueFeature }

    // ============================================
    // 接口定义
    // ============================================

    public interface IModule
    {
        string moduleId { get; }
        string moduleName { get; }
        string GetBonus(string bonusType);
        void Tick(float deltaTime);
        void OnActivate();
        void OnDeactivate();
    }

    public interface IGameEvent
    {
        string eventId { get; }
        string title { get; }
        string description { get; }
        GameEventType eventType { get; }
        bool CanTrigger();
        EventResult Execute();
    }

    // ============================================
    // 数据结构
    // ============================================

    public class EventResult
    {
        public bool success;
        public bool playerVictory;
        public int goldReward;
        public int goldCost;
        public int expReward;
        public int hpReward;
        public int hpCost;
        public List<string> unlockedModuleIds = new();
        public List<string> removedModuleIds = new();
        public List<string> itemRewards = new();
        public List<string> itemCosts = new();
        public string message = "";
        public bool isGameOver;
        public string combatLog = "";

        public static EventResult CreateSuccess(string msg, int gold = 0, int exp = 0) =>
            new() { success = true, message = msg, goldReward = gold, expReward = exp };

        public static EventResult CreateFailure(string msg) =>
            new() { success = false, message = msg };
    }

    public class ItemTemplate
    {
        public string id = "";
        public string nameTextId = "";
        public ItemType type = ItemType.Material;
        public float weight = 1f;
        public int maxStack = 99;
        public int damage;
        public int armor;
        public float moveSpeedFactor = 1f;
    }

    public class ItemInstance
    {
        public ItemTemplate template = new();
        public int amount;
        public int instanceId;

        public float TotalWeight => template.weight * amount;
        public string Name => template.nameTextId;
    }

    public class TeamMemberData
    {
        public int id;
        public string name = "";
        public int level = 1;
        public int hp = 20, maxHp = 20;
        public int attack = 5, defense = 3;
        public float speed = 1f;
        public float critBonus;
        public JobType job = JobType.None;
        public string? weaponTemplateId;
        public string? armorTemplateId;

        public bool IsAlive => hp > 0;
        public float HpPercent => maxHp > 0 ? (float)hp / maxHp : 0;

        public void TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage - defense);
            hp = Math.Max(0, hp - actualDamage);
        }

        public void Heal(int amount) => hp = Math.Min(maxHp, hp + amount);
        public void LevelUp()
        {
            level++;
            maxHp += 5;
            hp = maxHp;
            attack += 2;
            defense += 1;
        }
    }

    public class CardData
    {
        public string id = "";
        public CardType cardType;
        public CardRarity rarity;
        public string nameTextId = "";
        public float attributeMultiplier = 1f;
        public bool isActivated;
    }

    public class PrestigeUpgrade
    {
        public string id = "";
        public string nameTextId = "";
        public int cost;
        public PrestigeUpgradeType type;
        public float effectValue;
        public bool isPurchased;
        public int currentLevel;
        public int maxLevel = 10;
    }

    public class PrestigeResult
    {
        public int pointsEarned;
        public float retainedGoldPercent;
        public float retainedExpPercent;
        public List<string> retainedSkills = new();
        public int newPrestigeCount;
        public string message = "";
    }

    public class CombatResult
    {
        public bool playerVictory;
        public int playerDamageTaken;
        public int goldReward;
        public int expReward;
        public string combatLog = "";
        public string endMessage = "";
    }

    public class Location
    {
        public string id = "";
        public string locationName = "";
        public LocationType type;
        public List<string> connections = new();
        public int nodeIndex;
        public bool hasEvent;
        public string? eventId;
        public float eventChance;
        public int baseReward;
        public float travelTime;
    }

    // ============================================
    // 模拟模块实现
    // ============================================

    /// <summary>
    /// 库存系统模拟 - 简化版InventoryDesign
    /// </summary>
    public class InventorySimulation
    {
        private static InventorySimulation? _instance;
        public static InventorySimulation instance => _instance ??= new InventorySimulation();

        private List<ItemInstance> _items = new();
        private int _nextInstanceId = 1;
        private int _maxSlots = 50;
        private float _maxWeight = 100f;
        private Dictionary<string, ItemTemplate> _templates = new();

        public event Action<string>? onInventoryChanged;

        public int SlotCount => _items.Count;
        public int MaxSlots => _maxSlots;
        public float TotalWeight => _items.Sum(i => i.TotalWeight);
        public float MaxWeight => _maxWeight;
        public IReadOnlyList<ItemInstance> Items => _items;

        public void RegisterTemplate(ItemTemplate template) => _templates[template.id] = template;

        public ItemInstance? GetItem(int instanceId) => _items.FirstOrDefault(i => i.instanceId == instanceId);

        public (bool success, string message, int instanceId) AddItem(string templateId, int amount = 1)
        {
            if (!_templates.TryGetValue(templateId, out var template))
                return (false, $"Template {templateId} not found", -1);

            if (_items.Count >= _maxSlots)
                return (false, "Inventory full", -1);

            if (TotalWeight + template.weight * amount > _maxWeight)
                return (false, "Too heavy", -1);

            var existing = _items.FirstOrDefault(i => i.template.id == templateId && i.amount + amount <= template.maxStack);
            if (existing != null)
            {
                existing.amount += amount;
            }
            else
            {
                _items.Add(new ItemInstance
                {
                    template = template,
                    amount = amount,
                    instanceId = _nextInstanceId++
                });
            }

            onInventoryChanged?.Invoke($"Added {amount}x {template.nameTextId}");
            return (true, "Success", _items.Last().instanceId);
        }

        public (bool success, string message, int actualAmount) RemoveItem(int instanceId, int amount = 1)
        {
            var item = GetItem(instanceId);
            if (item == null)
                return (false, "Item not found", 0);

            if (item.amount < amount)
            {
                amount = item.amount;
                _items.Remove(item);
            }
            else
            {
                item.amount -= amount;
                if (item.amount <= 0)
                    _items.Remove(item);
            }

            onInventoryChanged?.Invoke($"Removed {amount}x {item.Name}");
            return (true, "Success", amount);
        }

        public int GetItemCount(string templateId) => _items.Where(i => i.template.id == templateId).Sum(i => i.amount);

        public void Clear() => _items.Clear();

        public List<(string templateId, int amount, int instanceId)> Export()
            => _items.Select(i => (i.template.id, i.amount, i.instanceId)).ToList();

        public void Import(List<(string templateId, int amount, int instanceId)> data)
        {
            Clear();
            foreach (var (templateId, amount, instanceId) in data)
            {
                if (_templates.TryGetValue(templateId, out var template))
                {
                    _items.Add(new ItemInstance { template = template, amount = amount, instanceId = instanceId });
                    if (instanceId >= _nextInstanceId) _nextInstanceId = instanceId + 1;
                }
            }
        }
    }

    /// <summary>
    /// 队伍系统模拟 - 简化版TeamDesign
    /// </summary>
    public class TeamSimulation
    {
        private static TeamSimulation? _instance;
        public static TeamSimulation instance => _instance ??= new TeamSimulation();

        private List<TeamMemberData> _members = new();
        private int _nextMemberId = 1;
        private int _maxTeamSize = 6;

        public event Action<string>? onTeamChanged;

        public int MemberCount => _members.Count;
        public int MaxTeamSize => _maxTeamSize;
        public IReadOnlyList<TeamMemberData> Members => _members;

        public (bool success, string message, int memberId) AddMember(string name, int level = 1)
        {
            if (_members.Count >= _maxTeamSize)
                return (false, "Team full", -1);

            var member = new TeamMemberData
            {
                id = _nextMemberId++,
                name = name,
                level = level,
                hp = 20 + (level - 1) * 5,
                maxHp = 20 + (level - 1) * 5,
                attack = 5 + (level - 1) * 2,
                defense = 3 + (level - 1)
            };
            _members.Add(member);

            onTeamChanged?.Invoke($"Added member: {name}");
            return (true, "Success", member.id);
        }

        public (bool success, string message) RemoveMember(int memberId)
        {
            var member = _members.FirstOrDefault(m => m.id == memberId);
            if (member == null)
                return (false, "Member not found");

            _members.Remove(member);
            onTeamChanged?.Invoke($"Removed member: {member.name}");
            return (true, "Success");
        }

        public TeamMemberData? GetMember(int memberId) => _members.FirstOrDefault(m => m.id == memberId);

        public int GetTotalCombatPower() => _members.Sum(m => m.attack + m.defense + m.maxHp / 2);

        public void HealAll(int amount)
        {
            foreach (var m in _members)
                m.Heal(amount);
        }

        public void LevelUpAll()
        {
            foreach (var m in _members)
                m.LevelUp();
        }

        public List<TeamMemberData> GetAliveMembers() => _members.Where(m => m.IsAlive).ToList();

        public void Clear() => _members.Clear();

        public List<TeamMemberData> Export() => _members.ToList();

        public void Import(List<TeamMemberData> data)
        {
            Clear();
            _members.AddRange(data);
            if (data.Count > 0)
                _nextMemberId = data.Max(m => m.id) + 1;
        }
    }

    /// <summary>
    /// 卡牌系统模拟 - 简化版CardDesign
    /// </summary>
    public class CardSimulation
    {
        private static CardSimulation? _instance;
        public static CardSimulation instance => _instance ??= new CardSimulation();

        private List<CardData> _cards = new();
        private Dictionary<GachaType, (int guaranteeAfter, CardRarity minRarity)> _pityConfig = new()
        {
            { GachaType.Novice, (10, CardRarity.R) },
            { GachaType.Standard, (10, CardRarity.R) },
            { GachaType.Premium, (10, CardRarity.SR) },
            { GachaType.Limited, (20, CardRarity.SSR) }
        };
        private Dictionary<GachaType, int> _pityCounters = new()
        {
            { GachaType.Novice, 0 },
            { GachaType.Standard, 0 },
            { GachaType.Premium, 0 },
            { GachaType.Limited, 0 }
        };
        private Dictionary<GachaType, int> _gachaCosts = new()
        {
            { GachaType.Novice, 100 },
            { GachaType.Standard, 500 },
            { GachaType.Premium, 1000 },
            { GachaType.Limited, 2000 }
        };

        public event Action<CardData>? onCardAcquired;
        public event Action<GachaType, int>? onGachaCompleted;

        public List<CardData> Cards => _cards;
        public int GetCardCount(CardRarity rarity) => _cards.Count(c => c.rarity == rarity);

        public CardData? DrawCard(GachaType type)
        {
            var (guaranteeAfter, minRarity) = _pityConfig[type];
            _pityCounters[type]++;

            // 确定抽卡权重
            var weights = type switch
            {
                GachaType.Novice => new[] { (CardRarity.N, 1.0) },
                GachaType.Standard => new[] { (CardRarity.N, 0.7), (CardRarity.R, 0.3) },
                GachaType.Premium => new[] { (CardRarity.R, 0.5), (CardRarity.SR, 0.4), (CardRarity.SSR, 0.1) },
                GachaType.Limited => new[] { (CardRarity.SR, 0.3), (CardRarity.SSR, 0.6), (CardRarity.UR, 0.1) },
                _ => new[] { (CardRarity.N, 1.0) }
            };

            // 保底检查
            CardRarity drawnRarity;
            if (_pityCounters[type] >= guaranteeAfter)
            {
                drawnRarity = minRarity;
                _pityCounters[type] = 0;
            }
            else
            {
                var roll = Random.Shared.NextDouble();
                double cumulative = 0;
                drawnRarity = CardRarity.N;
                foreach (var (rarity, prob) in weights)
                {
                    cumulative += prob;
                    if (roll < cumulative)
                    {
                        drawnRarity = rarity;
                        break;
                    }
                }
            }

            var card = new CardData
            {
                id = $"Card_{type}_{_cards.Count + 1}",
                cardType = CardType.Character,
                rarity = drawnRarity,
                nameTextId = $"{drawnRarity} Character Card",
                attributeMultiplier = 1f + (int)drawnRarity * 0.1f
            };

            _cards.Add(card);
            onCardAcquired?.Invoke(card);
            onGachaCompleted?.Invoke(type, _gachaCosts[type]);

            return card;
        }

        public List<CardData> DrawCards(GachaType type, int count = 1)
        {
            var results = new List<CardData>();
            for (int i = 0; i < count; i++)
            {
                var card = DrawCard(type);
                if (card != null) results.Add(card);
            }
            return results;
        }

        public void ActivateCard(string cardId)
        {
            var card = _cards.FirstOrDefault(c => c.id == cardId);
            if (card != null) card.isActivated = true;
        }

        public bool IsCardActivated(string cardId) => _cards.FirstOrDefault(c => c.id == cardId)?.isActivated ?? false;

        public void Clear() => _cards.Clear();

        public List<CardData> Export() => _cards.ToList();

        public void Import(List<CardData> data)
        {
            Clear();
            _cards.AddRange(data);
        }
    }

    /// <summary>
    /// 轮回系统模拟 - 简化版PrestigeManager
    /// </summary>
    public class PrestigeSimulation
    {
        private static PrestigeSimulation? _instance;
        public static PrestigeSimulation instance => _instance ??= new PrestigeSimulation();

        private List<PrestigeUpgrade> _upgrades = new();
        private int _prestigePoints;
        private int _prestigeCount;
        private float _goldRetentionRate = 0.1f;
        private float _expRetentionRate = 0.5f;
        private List<string> _retainedSkills = new();

        public int PrestigePoints => _prestigePoints;
        public int PrestigeCount => _prestigeCount;
        public float GoldRetentionRate => _goldRetentionRate;
        public float ExpRetentionRate => _expRetentionRate;

        public event Action<PrestigeResult>? onPrestigeCompleted;
        public event Action<PrestigeUpgrade>? onUpgradePurchased;

        public PrestigeSimulation()
        {
            // 初始化升级
            _upgrades.Add(new PrestigeUpgrade { id = "Core.Prestige.ResourceRetention", nameTextId = "资源保留", cost = 100, type = PrestigeUpgradeType.ResourceRetention, effectValue = 0.1f, maxLevel = 10 });
            _upgrades.Add(new PrestigeUpgrade { id = "Core.Prestige.PointBonus", nameTextId = "点数加成", cost = 150, type = PrestigeUpgradeType.PrestigePointBonus, effectValue = 0.1f, maxLevel = 10 });
            _upgrades.Add(new PrestigeUpgrade { id = "Core.Prestige.StartingEquipment", nameTextId = "初始装备", cost = 500, type = PrestigeUpgradeType.StartingEquipment, effectValue = 1, maxLevel = 1 });
        }

        public bool CanPrestige(int playerLevel)
        {
            return playerLevel >= 10;
        }

        public PrestigeResult ExecutePrestige(int playerGold, int playerExp, int playerLevel)
        {
            if (!CanPrestige(playerLevel))
                return new PrestigeResult { message = "等级不足10级" };

            int pointsEarned = 100 + (playerLevel - 10) * 50;
            float retainedGold = playerGold * _goldRetentionRate;
            float retainedExp = playerExp * _expRetentionRate;

            var result = new PrestigeResult
            {
                pointsEarned = pointsEarned,
                retainedGoldPercent = _goldRetentionRate,
                retainedExpPercent = _expRetentionRate,
                retainedSkills = _retainedSkills.ToList(),
                newPrestigeCount = _prestigeCount + 1,
                message = $"轮回成功！获得{pointsEarned}轮回点数"
            };

            _prestigePoints += pointsEarned;
            _prestigeCount++;

            onPrestigeCompleted?.Invoke(result);
            return result;
        }

        public bool PurchaseUpgrade(string upgradeId, int gold)
        {
            var upgrade = _upgrades.FirstOrDefault(u => u.id == upgradeId);
            if (upgrade == null || upgrade.isPurchased || gold < upgrade.cost)
                return false;

            upgrade.isPurchased = true;
            ApplyUpgradeEffect(upgrade);
            onUpgradePurchased?.Invoke(upgrade);
            return true;
        }

        private void ApplyUpgradeEffect(PrestigeUpgrade upgrade)
        {
            switch (upgrade.type)
            {
                case PrestigeUpgradeType.ResourceRetention:
                    _goldRetentionRate += upgrade.effectValue;
                    break;
                case PrestigeUpgradeType.PrestigePointBonus:
                    // 轮回点数加成
                    break;
            }
        }

        public List<PrestigeUpgrade> GetUpgrades() => _upgrades;

        public void Reset()
        {
            _prestigePoints = 0;
            _prestigeCount = 0;
            foreach (var u in _upgrades) u.isPurchased = false;
            _retainedSkills.Clear();
        }
    }

    /// <summary>
    /// 战斗系统模拟 - 简化版CombatSystem
    /// </summary>
    public class CombatSimulation
    {
        private static CombatSimulation? _instance;
        public static CombatSimulation instance => _instance ??= new CombatSimulation();

        public CombatResult ExecuteCombat(int playerAttack, int playerDefense, int playerHp, int enemyHp, int enemyArmor, int enemyDamage, string enemyName = "敌人")
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine($"=== 战斗开始: 玩家 vs {enemyName} ===");

            int currentPlayerHp = playerHp;
            int currentEnemyHp = enemyHp;
            int playerDamageTaken = 0;
            int turn = 1;
            bool playerVictory = false;

            while (currentPlayerHp > 0 && currentEnemyHp > 0)
            {
                // 玩家攻击
                int playerDamage = Math.Max(1, playerAttack - enemyArmor / 2);
                bool isCrit = Random.Shared.NextDouble() < 0.1;
                if (isCrit) playerDamage *= 2;

                currentEnemyHp -= playerDamage;
                log.AppendLine(string.Format("回合{0}: 玩家造成 {1} 伤害" + (isCrit ? " (暴击!)" : ""), turn, playerDamage));

                if (currentEnemyHp <= 0)
                {
                    playerVictory = true;
                    break;
                }

                // 敌人攻击
                int enemyDamageDealt = Math.Max(1, enemyDamage - playerDefense / 2);
                currentPlayerHp -= enemyDamageDealt;
                playerDamageTaken += enemyDamageDealt;
                log.AppendLine(string.Format("回合{0}: {1}造成 {2} 伤害", turn, enemyName, enemyDamageDealt));

                turn++;
                if (turn > 100) break; // 防止无限循环
            }

            int goldReward = playerVictory ? Random.Shared.Next(10, 50) : 0;
            int expReward = playerVictory ? Random.Shared.Next(5, 20) : 0;

            log.AppendLine($"=== 战斗结束: {(playerVictory ? "胜利" : "失败")} ===");
            log.AppendLine(string.Format("获得金币: {0}, 经验: {1}", goldReward, expReward));

            return new CombatResult
            {
                playerVictory = playerVictory,
                playerDamageTaken = playerDamageTaken,
                goldReward = goldReward,
                expReward = expReward,
                combatLog = log.ToString(),
                endMessage = playerVictory ? string.Format("击败{0}!", enemyName) : "你被击败了..."
            };
        }

        public CombatResult ExecuteTeamCombat(List<TeamMemberData> team, int enemyHp, int enemyArmor, int enemyDamage, string enemyName = "敌人")
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine(string.Format("=== 队伍战斗: {0}人 vs {1} ===", team.Count, enemyName));

            int totalAttack = team.Sum(m => m.IsAlive ? m.attack : 0);
            int totalDefense = team.Sum(m => m.IsAlive ? m.defense : 0);
            int totalHp = team.Sum(m => m.IsAlive ? m.hp : 0);
            int aliveCount = team.Count(m => m.IsAlive);

            if (aliveCount == 0)
            {
                return new CombatResult
                {
                    playerVictory = false,
                    endMessage = "队伍全灭..."
                };
            }

            int currentEnemyHp = enemyHp;
            int playerDamageTaken = 0;
            int turn = 1;
            bool playerVictory = false;

            while (totalHp > 0 && currentEnemyHp > 0)
            {
                // 队伍攻击 (简化: 所有人一起打)
                int playerDamage = Math.Max(1, totalAttack - enemyArmor / 2);
                bool isCrit = Random.Shared.NextDouble() < 0.1;
                if (isCrit) playerDamage *= 2;

                currentEnemyHp -= playerDamage;
                log.AppendLine(string.Format("回合{0}: 队伍造成 {1} 伤害" + (isCrit ? " (暴击!)" : ""), turn, playerDamage));

                if (currentEnemyHp <= 0)
                {
                    playerVictory = true;
                    break;
                }

                // 敌人攻击 (随机打击一个存活成员)
                var alive = team.Where(m => m.IsAlive).ToList();
                if (alive.Count > 0)
                {
                    var target = alive[Random.Shared.Next(alive.Count)];
                    int enemyDamageDealt = Math.Max(1, enemyDamage - target.defense / 2);
                    target.TakeDamage(enemyDamageDealt);
                    playerDamageTaken += enemyDamageDealt;
                    totalHp -= enemyDamageDealt;
                    log.AppendLine(string.Format("回合{0}: {1}攻击{2}, 造成 {3} 伤害, 剩余HP: {4}", turn, enemyName, target.name, enemyDamageDealt, target.hp));
                }

                turn++;
                if (turn > 100) break;
            }

            int goldReward = playerVictory ? Random.Shared.Next(10, 100) * aliveCount : 0;
            int expReward = playerVictory ? Random.Shared.Next(5, 30) * aliveCount : 0;

            log.AppendLine($"=== 战斗结束: {(playerVictory ? "胜利" : "失败")} ===");
            log.AppendLine(string.Format("获得金币: {0}, 经验: {1}", goldReward, expReward));

            return new CombatResult
            {
                playerVictory = playerVictory,
                playerDamageTaken = playerDamageTaken,
                goldReward = goldReward,
                expReward = expReward,
                combatLog = log.ToString(),
                endMessage = playerVictory ? string.Format("击败{0}!", enemyName) : "队伍被击败..."
            };
        }
    }

    /// <summary>
    /// 进度系统模拟 - 简化版ProgressManager
    /// </summary>
    public class ProgressSimulation
    {
        private static ProgressSimulation? _instance;
        public static ProgressSimulation instance => _instance ??= new ProgressSimulation();

        private int _currentPoints;
        private int _totalEarnedPoints;
        private int _milestoneCount;
        private float _progressPercent;
        private int _pointsPerNormalEvent = 200;
        private int _pointsPerEventTree = 1000;
        private int _travelPointSize = 1000;

        public int CurrentPoints => _currentPoints;
        public int TotalEarnedPoints => _totalEarnedPoints;
        public int MilestoneCount => _milestoneCount;
        public float ProgressPercent => _progressPercent;

        public event Action<int>? onNormalEventTriggered;
        public event Action<int>? onEventTreeTriggered;
        public event Action? onPointsOverflow;

        public void AddPoints(float deltaTime)
        {
            int points = (int)(deltaTime * 10); // 10点/秒
            AddPointsInternal(points);
        }

        public void AddPointsClick()
        {
            AddPointsInternal(_pointsPerNormalEvent / 20); // 点击获得进度
        }

        public void AddPointsInternal(int amount)
        {
            _currentPoints += amount;
            _totalEarnedPoints += amount;

            // 检查溢出
            if (_currentPoints >= _travelPointSize)
            {
                _currentPoints -= _travelPointSize;
                onPointsOverflow?.Invoke();
            }

            // 检查里程碑
            int previousMilestone = _milestoneCount;
            _milestoneCount = _totalEarnedPoints / _pointsPerEventTree;
            _progressPercent = (float)(_totalEarnedPoints % _pointsPerEventTree) / _pointsPerEventTree;

            if (_milestoneCount > previousMilestone)
            {
                onEventTreeTriggered?.Invoke(_milestoneCount);
            }
            else if (_totalEarnedPoints / _pointsPerNormalEvent > (_totalEarnedPoints - amount) / _pointsPerNormalEvent)
            {
                onNormalEventTriggered?.Invoke(_totalEarnedPoints);
            }
        }

        public bool ConsumePoints(int amount)
        {
            if (_currentPoints < amount) return false;
            _currentPoints -= amount;
            return true;
        }

        public void Reset()
        {
            _currentPoints = 0;
            _totalEarnedPoints = 0;
            _milestoneCount = 0;
            _progressPercent = 0;
        }
    }

    /// <summary>
    /// 旅行系统模拟 - 简化版TravelManager
    /// </summary>
    public class TravelSimulation
    {
        private static TravelSimulation? _instance;
        public static TravelSimulation instance => _instance ??= new TravelSimulation();

        private TravelStatus _status = TravelStatus.Idle;
        private int _currentNodeIndex;
        private int _maxNodeIndex = 10;
        private float _currentProgress;
        private List<Location> _locations = new();

        public TravelStatus Status => _status;
        public int CurrentNodeIndex => _currentNodeIndex;
        public int MaxNodeIndex => _maxNodeIndex;
        public float CurrentProgress => _currentProgress;
        public Location? CurrentLocation => _currentNodeIndex < _locations.Count ? _locations[_currentNodeIndex] : null;

        public event Action<TravelStatus>? onStatusChanged;
        public event Action? onTravelCompleted;

        public void GeneratePath(string seed = "default")
        {
            _locations.Clear();
            var random = new Random(seed.GetHashCode());

            _locations.Add(new Location { id = "start", locationName = "起点", type = LocationType.Start, nodeIndex = 0 });

            for (int i = 1; i < _maxNodeIndex; i++)
            {
                var types = new[] { LocationType.City, LocationType.Wilderness, LocationType.Market, LocationType.Dungeon };
                var type = types[random.Next(types.Length)];
                _locations.Add(new Location
                {
                    id = $"node_{i}",
                    locationName = $"{type}节点{i}",
                    type = type,
                    nodeIndex = i,
                    hasEvent = random.NextDouble() < 0.3,
                    eventChance = 0.5f,
                    travelTime = (float)(2.0 + random.NextDouble() * 3.0)
                });
            }

            _locations.Add(new Location { id = "goal", locationName = "终点", type = LocationType.Goal, nodeIndex = _maxNodeIndex });
        }

        public void StartTravel()
        {
            if (_currentNodeIndex >= _maxNodeIndex) return;
            _status = TravelStatus.Traveling;
            _currentProgress = 0;
            onStatusChanged?.Invoke(_status);
        }

        public void UpdateProgress(float deltaTime)
        {
            if (_status != TravelStatus.Traveling) return;

            var location = CurrentLocation;
            if (location == null) return;

            _currentProgress += deltaTime / location.travelTime;

            if (_currentProgress >= 1f)
            {
                _currentProgress = 0;
                _currentNodeIndex++;
                onTravelCompleted?.Invoke();

                if (_currentNodeIndex >= _maxNodeIndex)
                {
                    _status = TravelStatus.ProgressMilestone;
                    onStatusChanged?.Invoke(_status);
                }
            }
        }

        public List<Location> GetCurrentChoices()
        {
            if (_currentNodeIndex >= _locations.Count - 1) return new();
            return new List<Location> { _locations[_currentNodeIndex + 1] };
        }

        public bool IsAtGoal() => _currentNodeIndex >= _maxNodeIndex;

        public void Reset()
        {
            _currentNodeIndex = 0;
            _currentProgress = 0;
            _status = TravelStatus.Idle;
        }
    }

    // ============================================
    // 玩家数据模拟
    // ============================================

    public class PlayerActorSimulation
    {
        public string id = "";
        public string actorName = "旅行者";
        public int level = 1;
        public int exp;
        public int gold = 500;
        public int maxHp = 100;
        public int currentHp = 100;
        public int attack = 10;
        public int defense = 5;
        public float speed = 1f;
        public float critChance = 0.1f;
        public float dodgeChance = 0.05f;
        public float critDamageMultiplier = 1.5f;

        private List<IModule> _modules = new();
        public IReadOnlyList<IModule> Modules => _modules;

        public event Action<string>? onPlayerDataChanged;

        public void AddModule(IModule module)
        {
            _modules.Add(module);
            module.OnActivate();
        }

        public void RemoveModule(string moduleId)
        {
            var module = _modules.FirstOrDefault(m => m.moduleId == moduleId);
            if (module != null)
            {
                module.OnDeactivate();
                _modules.Remove(module);
            }
        }

        public float GetTotalBonus(string bonusType)
        {
            return _modules.Sum(m => float.TryParse(m.GetBonus(bonusType), out var v) ? v : 0);
        }

        public void GainExp(int amount)
        {
            exp += amount;
            if (exp >= level * 100)
            {
                exp -= level * 100;
                LevelUp();
            }
            onPlayerDataChanged?.Invoke(string.Format("获得{0}经验", amount));
        }

        public void LevelUp()
        {
            level++;
            maxHp += 10;
            currentHp = maxHp;
            attack += 3;
            defense += 2;
            onPlayerDataChanged?.Invoke(string.Format("升级! Lv.{0}", level));
        }

        public void TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage - defense);
            currentHp = Math.Max(0, currentHp - actualDamage);
            onPlayerDataChanged?.Invoke(string.Format("受到{0}伤害, 剩余HP: {1}", actualDamage, currentHp));
        }

        public void Heal(int amount)
        {
            currentHp = Math.Min(maxHp, currentHp + amount);
            onPlayerDataChanged?.Invoke(string.Format("恢复{0}HP", amount));
        }

        public bool IsAlive => currentHp > 0;
    }

    // ============================================
    // 存档系统模拟
    // ============================================

    public class SaveDataSimulation
    {
        public int playerLevel;
        public int playerExp;
        public int playerGold;
        public int playerHp;
        public int playerMaxHp;
        public int playerAttack;
        public int playerDefense;
        public int teamMemberCount;
        public int inventorySlotCount;
        public int cardCount;
        public int prestigePoints;
        public int prestigeCount;
        public int travelNodeIndex;
        public int progressPoints;
        public long timestamp;
    }

    public class SaveManagerSimulation
    {
        private static SaveManagerSimulation? _instance;
        public static SaveManagerSimulation instance => _instance ??= new SaveManagerSimulation();

        private string _savePath = "GameSave.json";
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public SaveDataSimulation CreateSaveData(
            PlayerActorSimulation player,
            TeamSimulation team,
            InventorySimulation inventory,
            CardSimulation card,
            PrestigeSimulation prestige,
            TravelSimulation travel,
            ProgressSimulation progress)
        {
            return new SaveDataSimulation
            {
                playerLevel = player.level,
                playerExp = player.exp,
                playerGold = player.gold,
                playerHp = player.currentHp,
                playerMaxHp = player.maxHp,
                playerAttack = player.attack,
                playerDefense = player.defense,
                teamMemberCount = team.MemberCount,
                inventorySlotCount = inventory.SlotCount,
                cardCount = card.Cards.Count,
                prestigePoints = prestige.PrestigePoints,
                prestigeCount = prestige.PrestigeCount,
                travelNodeIndex = travel.CurrentNodeIndex,
                progressPoints = progress.CurrentPoints,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        public void Save(SaveDataSimulation data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(_savePath, json);
            Console.WriteLine(string.Format("[存档] 已保存到 {0}", _savePath));
        }

        public SaveDataSimulation? Load()
        {
            if (!File.Exists(_savePath))
            {
                Console.WriteLine("[存档] 未找到存档文件");
                return null;
            }

            var json = File.ReadAllText(_savePath);
            var data = JsonSerializer.Deserialize<SaveDataSimulation>(json);
            Console.WriteLine(string.Format("[存档] 已加载存档 (时间戳: {0})", data?.timestamp));
            return data;
        }

        public bool HasSave() => File.Exists(_savePath);
    }

    // ============================================
    // 事件系统模拟
    // ============================================

    public class RandomEvent : IGameEvent
    {
        public string eventId => "random_event";
        public string title => "随机事件";
        public string description => "一个随机的旅程事件";
        public GameEventType eventType => GameEventType.Random;

        private readonly Func<bool> _canTrigger;
        private readonly Func<EventResult> _execute;

        public RandomEvent(Func<bool> canTrigger, Func<EventResult> execute)
        {
            _canTrigger = canTrigger;
            _execute = execute;
        }

        public bool CanTrigger() => _canTrigger();
        public EventResult Execute() => _execute();
    }

    public class EventQueueSimulation
    {
        private static EventQueueSimulation? _instance;
        public static EventQueueSimulation instance => _instance ??= new EventQueueSimulation();

        private Queue<IGameEvent> _eventQueue = new();

        public event Action<IGameEvent>? onEventTriggered;
        public event Action<EventResult>? onEventCompleted;

        public void Enqueue(IGameEvent gameEvent)
        {
            _eventQueue.Enqueue(gameEvent);
            Console.WriteLine(string.Format("[事件] 入队: {0}", gameEvent.title));
        }

        public EventResult? ProcessNext()
        {
            if (_eventQueue.Count == 0) return null;

            var gameEvent = _eventQueue.Dequeue();
            Console.WriteLine(string.Format("[事件] 触发: {0}", gameEvent.title));
            onEventTriggered?.Invoke(gameEvent);

            if (!gameEvent.CanTrigger())
            {
                var result = EventResult.CreateFailure("事件无法触发");
                onEventCompleted?.Invoke(result);
                return result;
            }

            var eventResult = gameEvent.Execute();
            onEventCompleted?.Invoke(eventResult);
            return eventResult;
        }

        public int PendingCount => _eventQueue.Count;
        public bool HasEvents => _eventQueue.Count > 0;

        public void Clear() => _eventQueue.Clear();

        public void GenerateRandomEvents(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Enqueue(new RandomEvent(
                    () => true,
                    () =>
                    {
                        var roll = Random.Shared.Next(100);
                        if (roll < 30)
                            return EventResult.CreateSuccess("发现宝箱!", gold: Random.Shared.Next(10, 50));
                        else if (roll < 60)
                            return EventResult.CreateSuccess("遇到商人", gold: -Random.Shared.Next(5, 20));
                        else if (roll < 85)
                            return EventResult.CreateFailure("遭遇山贼!");
                        else
                            return EventResult.CreateSuccess("发现秘密通道!");
                    }
                ));
            }
        }
    }

    // ============================================
    // 主模拟器 - 游戏核心循环
    // ============================================

    public class GameSimulation
    {
        private PlayerActorSimulation _player = new();
        private InventorySimulation _inventory = InventorySimulation.instance;
        private TeamSimulation _team = TeamSimulation.instance;
        private CardSimulation _card = CardSimulation.instance;
        private PrestigeSimulation _prestige = PrestigeSimulation.instance;
        private CombatSimulation _combat = CombatSimulation.instance;
        private ProgressSimulation _progress = ProgressSimulation.instance;
        private TravelSimulation _travel = TravelSimulation.instance;
        private EventQueueSimulation _events = EventQueueSimulation.instance;
        private SaveManagerSimulation _save = SaveManagerSimulation.instance;

        private float _gameTime;
        private bool _isRunning;

        public void Initialize()
        {
            Console.WriteLine("=== 游戏初始化 ===");

            // 注册物品模板
            _inventory.RegisterTemplate(new ItemTemplate { id = "Core.Item.GoldCoin", nameTextId = "金币", type = ItemType.Money, weight = 0.01f });
            _inventory.RegisterTemplate(new ItemTemplate { id = "Core.Item.Bacon", nameTextId = "培根", type = ItemType.Food, weight = 0.2f });
            _inventory.RegisterTemplate(new ItemTemplate { id = "Core.Item.ShortBlade", nameTextId = "短剑", type = ItemType.Weapon, weight = 1f, damage = 5 });
            _inventory.RegisterTemplate(new ItemTemplate { id = "Core.Item.LeatherArmor", nameTextId = "皮甲", type = ItemType.Armor, weight = 2f, armor = 3 });

            // 初始化队伍
            _team.AddMember("剑客", 1);
            _team.AddMember("医师", 1);

            // 初始化旅行
            _travel.GeneratePath();
            _travel.StartTravel();

            // 事件订阅
            _progress.onNormalEventTriggered += points => Console.WriteLine(string.Format("[进度] 普通事件触发 @ {0}", points));
            _progress.onEventTreeTriggered += milestone => Console.WriteLine(string.Format("[进度] 事件树里程碑 @ {0}", milestone));
            _events.onEventCompleted += result => ApplyEventResult(result);

            Console.WriteLine(string.Format("[玩家] {0} Lv.{1}", _player.actorName, _player.level));
            Console.WriteLine(string.Format("[队伍] {0}人, 战力:{1}", _team.MemberCount, _team.GetTotalCombatPower()));
            Console.WriteLine("=== 初始化完成 ===\n");
        }

        public void Run(int ticks = 100, float tickInterval = 0.1f)
        {
            Initialize();
            _isRunning = true;
            _gameTime = 0;

            Console.WriteLine("=== 开始游戏模拟 ===\n");

            for (int i = 0; i < ticks && _isRunning; i++)
            {
                Tick(tickInterval);
            }

            Console.WriteLine(string.Format("\n=== 模拟结束 (游戏时间: {0:F1}秒) ===", _gameTime));
            ShowStatus();
        }

        public void Tick(float deltaTime)
        {
            _gameTime += deltaTime;

            // 1. 进度系统 - 挂机收益
            _progress.AddPoints(deltaTime);

            // 2. 旅行系统 - 更新进度
            _travel.UpdateProgress(deltaTime);

            // 3. 模块Tick
            foreach (var module in _player.Modules)
                module.Tick(deltaTime);

            // 4. 事件队列处理
            if (_events.HasEvents && _travel.Status == TravelStatus.Traveling)
            {
                var result = _events.ProcessNext();
                if (result != null)
                    ApplyEventResult(result);
            }

            // 5. 随机事件生成 (每5秒)
            if (_gameTime % 5 < deltaTime)
            {
                _events.GenerateRandomEvents(1);
            }

            // 6. 战斗检测 (每10秒有概率触发)
            if (_gameTime % 10 < deltaTime && _travel.Status == TravelStatus.Traveling)
            {
                if (Random.Shared.NextDouble() < 0.3)
                {
                    TriggerCombat();
                }
            }

            // 7. 状态输出 (每20秒)
            if (_gameTime % 20 < deltaTime)
            {
                ShowStatus();
            }
        }

        private void TriggerCombat()
        {
            var enemyHp = 30 + _player.level * 5;
            var enemyDamage = 5 + _player.level;
            var enemyArmor = _player.level;

            Console.WriteLine(string.Format("\n[战斗] 遭遇敌人! HP:{0} ATK:{1} DEF:{2}", enemyHp, enemyDamage, enemyArmor));

            var result = _combat.ExecuteCombat(
                _player.attack, _player.defense, _player.currentHp,
                enemyHp, enemyArmor, enemyDamage, "山贼"
            );

            Console.WriteLine(result.combatLog);
            ApplyEventResult(new EventResult
            {
                success = result.playerVictory,
                goldReward = result.goldReward,
                expReward = result.expReward,
                message = result.endMessage
            });
        }

        private void ApplyEventResult(EventResult result)
        {
            if (result.goldReward > 0)
            {
                _player.gold += result.goldReward;
                Console.WriteLine(string.Format("[获得] 金币 +{0}", result.goldReward));
            }
            else if (result.goldCost > 0)
            {
                _player.gold = Math.Max(0, _player.gold - result.goldCost);
                Console.WriteLine(string.Format("[消耗] 金币 -{0}", result.goldCost));
            }

            if (result.expReward > 0)
            {
                _player.GainExp(result.expReward);
            }

            if (result.hpCost > 0)
            {
                _player.TakeDamage(result.hpCost);
            }

            if (result.hpReward > 0)
            {
                _player.Heal(result.hpReward);
            }

            if (result.itemRewards.Count > 0)
            {
                foreach (var itemId in result.itemRewards)
                {
                    _inventory.AddItem(itemId, 1);
                }
            }

            if (!string.IsNullOrEmpty(result.message))
            {
                Console.WriteLine(string.Format("[事件结果] {0}", result.message));
            }

            if (result.isGameOver)
            {
                _isRunning = false;
                Console.WriteLine("[游戏] 游戏结束!");
            }
        }

        public void ShowStatus()
        {
            Console.WriteLine(string.Format("\n--- 状态面板 ---"));
            Console.WriteLine(string.Format("时间: {0:F1}秒 | 进度: {1}/{2}", _gameTime, _progress.CurrentPoints, _progress.TotalEarnedPoints));
            Console.WriteLine(string.Format("玩家: {0} Lv.{1} HP:{2}/{3} ATK:{4} DEF:{5}", _player.actorName, _player.level, _player.currentHp, _player.maxHp, _player.attack, _player.defense));
            Console.WriteLine(string.Format("金币: {0} | 背包: {1}/{2} | 队伍: {3}人", _player.gold, _inventory.SlotCount, _inventory.MaxSlots, _team.MemberCount));
            Console.WriteLine(string.Format("旅行: 节点{0}/{1} | 进度: {2:P0}", _travel.CurrentNodeIndex, _travel.MaxNodeIndex, _travel.CurrentProgress));
            Console.WriteLine(string.Format("轮回: {0}次 | 点数: {1}", _prestige.PrestigeCount, _prestige.PrestigePoints));
            Console.WriteLine(string.Format("卡牌: {0}张 | 待处理事件: {1}", _card.Cards.Count, _events.PendingCount));
        }

        public void TestSaveLoad()
        {
            Console.WriteLine("\n=== 测试存档系统 ===");

            var saveData = _save.CreateSaveData(_player, _team, _inventory, _card, _prestige, _travel, _progress);
            _save.Save(saveData);

            // 模拟修改数据
            _player.gold += 1000;
            Console.WriteLine(string.Format("[测试] 修改金币为: {0}", _player.gold));

            // 加载存档
            var loaded = _save.Load();
            if (loaded != null)
            {
                Console.WriteLine(string.Format("[测试] 加载后金币: {0}", loaded.playerGold));
                Console.WriteLine(string.Format("[测试] 加载后等级: {0}", loaded.playerLevel));
                Console.WriteLine(string.Format("[测试] 加载后节点: {0}", loaded.travelNodeIndex));
            }
        }

        public void TestPrestige()
        {
            Console.WriteLine("\n=== 测试轮回系统 ===");

            _player.level = 15;
            Console.WriteLine(string.Format("[测试] 设置玩家等级为: {0}", _player.level));

            if (_prestige.CanPrestige(_player.level))
            {
                var result = _prestige.ExecutePrestige(_player.gold, _player.exp, _player.level);
                Console.WriteLine(string.Format("[轮回] {0}", result.message));
                Console.WriteLine(string.Format("[轮回] 保留金币: {0:P0}", result.retainedGoldPercent));
                Console.WriteLine(string.Format("[轮回] 保留经验: {0:P0}", result.retainedExpPercent));
            }
            else
            {
                Console.WriteLine("[轮回] 等级不足,需要10级以上");
            }
        }

        public void TestGacha()
        {
            Console.WriteLine("\n=== 测试抽卡系统 ===");

            for (int i = 0; i < 5; i++)
            {
                var cards = _card.DrawCards(GachaType.Premium, 1);
                foreach (var card in cards)
                {
                    Console.WriteLine(string.Format("[抽卡] 获得: {0} ({1}) x{2:F1}", card.nameTextId, card.rarity, card.attributeMultiplier));
                }
            }

            Console.WriteLine(string.Format("[抽卡] 卡牌统计: N:{0} R:{1} SR:{2} SSR:{3}", _card.GetCardCount(CardRarity.N), _card.GetCardCount(CardRarity.R), _card.GetCardCount(CardRarity.SR), _card.GetCardCount(CardRarity.SSR)));
        }

        public void TestInventoryOperations()
        {
            Console.WriteLine("\n=== 测试背包操作 ===");

            var (ok1, msg1, id1) = _inventory.AddItem("Core.Item.GoldCoin", 100);
            Console.WriteLine(string.Format("[背包] 添加金币: {0} - {1}, ID:{2}", ok1, msg1, id1));

            var (ok2, msg2, id2) = _inventory.AddItem("Core.Item.Bacon", 5);
            Console.WriteLine(string.Format("[背包] 添加培根: {0} - {1}, ID:{2}", ok2, msg2, id2));

            var (ok3, msg3, _) = _inventory.AddItem("Core.Item.ShortBlade", 1);
            Console.WriteLine(string.Format("[背包] 添加短剑: {0} - {1}", ok3, msg3));

            var (ok4, msg4, _) = _inventory.AddItem("Core.Item.LeatherArmor", 1);
            Console.WriteLine(string.Format("[背包] 添加皮甲: {0} - {1}", ok4, msg4));

            Console.WriteLine(string.Format("[背包] 当前: {0}个槽位, 重量:{1:F1}/{2}", _inventory.SlotCount, _inventory.TotalWeight, _inventory.MaxWeight));

            // 移除物品测试
            var (ok5, msg5, amount5) = _inventory.RemoveItem(id2, 2);
            Console.WriteLine(string.Format("[背包] 移除培根2个: {0} - {1}, 实际移除:{2}", ok5, msg5, amount5));
        }

        public void TestTeamOperations()
        {
            Console.WriteLine("\n=== 测试队伍操作 ===");

            var (ok1, msg1, id1) = _team.AddMember("法师", 2);
            Console.WriteLine(string.Format("[队伍] 添加法师: {0} - {1}, ID:{2}", ok1, msg1, id1));

            var member = _team.GetMember(id1);
            if (member != null)
            {
                Console.WriteLine(string.Format("[队伍] 法师信息: Lv.{0} HP:{1} ATK:{2}", member.level, member.hp, member.attack));
            }

            _team.HealAll(10);
            Console.WriteLine("[队伍] 治疗全队+10");

            Console.WriteLine(string.Format("[队伍] 总战力: {0}", _team.GetTotalCombatPower()));
        }

        public void TestAllModules()
        {
            Console.WriteLine("\n" + "".PadRight(50, '='));
            Console.WriteLine("完整模块交互测试");
            Console.WriteLine("".PadRight(50, '='));

            // 1. 背包操作
            TestInventoryOperations();

            // 2. 队伍操作
            TestTeamOperations();

            // 3. 抽卡系统
            TestGacha();

            // 4. 轮回系统
            TestPrestige();

            // 5. 存档系统
            TestSaveLoad();

            // 6. 运行一小段游戏
            Console.WriteLine("\n=== 运行游戏模拟(50 ticks) ===");
            Run(50, 0.1f);
        }
    }

    // ============================================
    // 入口点
    // ============================================

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     Game1 核心玩法模拟器               ║");
            Console.WriteLine("║     Pure C# Game Loop Simulation       ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            var simulation = new GameSimulation();

            if (args.Length > 0 && args[0] == "--test")
            {
                // 运行完整测试
                simulation.TestAllModules();
            }
            else if (args.Length > 0 && args[0] == "--quick")
            {
                // 快速测试
                simulation.Initialize();
                simulation.Run(100, 0.1f);
            }
            else
            {
                // 默认: 运行完整测试
                simulation.TestAllModules();
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
