using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 卡牌类型
    /// </summary>
    public enum CardType
    {
        /// <summary>角色卡</summary>
        Character,

        /// <summary>装备卡</summary>
        Equipment,

        /// <summary>技能卡</summary>
        Skill,

        /// <summary>道具卡</summary>
        Item,

        /// <summary>事件卡</summary>
        Event
    }

    /// <summary>
    /// 卡牌稀有度
    /// </summary>
    public enum CardRarity
    {
        /// <summary>普通</summary>
        N,

        /// <summary>优秀</summary>
        R,

        /// <summary>稀有</summary>
        SR,

        /// <summary>史诗</summary>
        SSR,

        /// <summary>传说</summary>
        UR,

        /// <summary>神级</summary>
        GR
    }

    /// <summary>
    /// 抽卡类型
    /// </summary>
    public enum GachaType
    {
        /// <summary>新手包</summary>
        Novice,

        /// <summary>标准包</summary>
        Standard,

        /// <summary>高级包</summary>
        Premium,

        /// <summary>限定包</summary>
        Limited
    }

    /// <summary>
    /// 卡牌数据
    /// </summary>
    [Serializable]
    public class CardData
    {
        /// <summary>卡牌ID</summary>
        public string id;

        /// <summary>卡牌类型</summary>
        public CardType type;

        /// <summary>稀有度</summary>
        public CardRarity rarity;

        /// <summary>本地化名称键</summary>
        public string nameTextId;

        /// <summary>本地化描述键</summary>
        public string descTextId;

        /// <summary>属性倍率</summary>
        public float attributeMultiplier;

        /// <summary>附加词缀ID列表</summary>
        public List<string> affixIds;

        /// <summary>是否已激活</summary>
        public bool isActivated;

        /// <summary>获取稀有度名称</summary>
        public string GetRarityName()
        {
            return rarity switch
            {
                CardRarity.N => "N",
                CardRarity.R => "R",
                CardRarity.SR => "SR",
                CardRarity.SSR => "SSR",
                CardRarity.UR => "UR",
                CardRarity.GR => "GR",
                _ => "?"
            };
        }

        /// <summary>
        /// 获取稀有度颜色（用于UI）
        /// </summary>
        public string GetRarityColor()
        {
            return rarity switch
            {
                CardRarity.N => "#808080",
                CardRarity.R => "#00FF00",
                CardRarity.SR => "#0000FF",
                CardRarity.SSR => "#800080",
                CardRarity.UR => "#FFA500",
                CardRarity.GR => "#FF0000",
                _ => "#FFFFFF"
            };
        }
    }

    /// <summary>
    /// 抽卡结果
    /// </summary>
    public struct GachaResult
    {
        public bool success;
        public List<CardData> cards;
        public bool isGuaranteed;       // 保底触发
        public int pityCounter;         // 保底计数
        public int totalCost;           // 总消耗

        public static GachaResult Success(List<CardData> cards, bool isGuaranteed, int pityCounter, int cost)
        {
            return new GachaResult
            {
                success = true,
                cards = cards,
                isGuaranteed = isGuaranteed,
                pityCounter = pityCounter,
                totalCost = cost
            };
        }

        public static GachaResult Failure(string message)
        {
            return new GachaResult { success = false };
        }
    }

    /// <summary>
    /// 卡牌战斗加成
    /// </summary>
    public struct CardCombatBonus
    {
        public float damageMultiplier;   // 伤害倍率加成
        public float defenseMultiplier; // 防御倍率加成
        public float critMultiplier;    // 暴击倍率加成
        public float hpMultiplier;      // 生命倍率加成
        public float goldMultiplier;    // 金币倍率加成

        public float GetTotalBonus()
        {
            return damageMultiplier + defenseMultiplier + critMultiplier + hpMultiplier + goldMultiplier;
        }
    }

    /// <summary>
    /// 卡牌模板
    /// </summary>
    [Serializable]
    public class CardTemplate
    {
        public string id;
        public CardType type;
        public CardRarity rarity;
        public string nameTextId;
        public string descTextId;
        public float attributeMultiplier;
        public int affixCount;
        public List<string> tags;  // 标签（用于限定池）
        public int sellPrice;      // 出售价格
    }

    /// <summary>
    /// 保底配置
    /// </summary>
    [Serializable]
    public class PityConfig
    {
        public GachaType gachaType;
        public int guaranteeAfter;        // 第几次后保底
        public CardRarity minRarity;      // 保底最低稀有度
        public int count;                 // 保底数量
    }

    /// <summary>
    /// 卡牌系统核心逻辑
    /// 管理卡牌收藏、抽卡、激活/停用
    /// 采用与InventoryDesign/TeamDesign相同的单例非MonoBehaviour模式
    /// </summary>
    public class CardDesign
    {
        #region Singleton
        private static CardDesign _instance;
        public static CardDesign instance => _instance ??= new CardDesign();
        #endregion

        #region Configuration
        // 保底配置
        private readonly PityConfig[] _pityConfigs = new[]
        {
            new PityConfig { gachaType = GachaType.Novice, guaranteeAfter = 10, minRarity = CardRarity.R, count = 1 },
            new PityConfig { gachaType = GachaType.Standard, guaranteeAfter = 10, minRarity = CardRarity.R, count = 1 },
            new PityConfig { gachaType = GachaType.Premium, guaranteeAfter = 10, minRarity = CardRarity.SR, count = 1 },
            new PityConfig { gachaType = GachaType.Limited, guaranteeAfter = 20, minRarity = CardRarity.SSR, count = 1 }
        };

        // 抽卡费用
        private readonly Dictionary<GachaType, int> _gachaCosts = new()
        {
            { GachaType.Novice, 100 },
            { GachaType.Standard, 500 },
            { GachaType.Premium, 1000 },
            { GachaType.Limited, 2000 }
        };

        // 稀有度权重配置
        private readonly Dictionary<GachaType, Dictionary<CardRarity, float>> _rarityWeights = new()
        {
            { GachaType.Novice, new Dictionary<CardRarity, float>
                {
                    { CardRarity.N, 1.0f }
                }
            },
            { GachaType.Standard, new Dictionary<CardRarity, float>
                {
                    { CardRarity.N, 0.7f },
                    { CardRarity.R, 0.3f }
                }
            },
            { GachaType.Premium, new Dictionary<CardRarity, float>
                {
                    { CardRarity.R, 0.5f },
                    { CardRarity.SR, 0.4f },
                    { CardRarity.SSR, 0.1f }
                }
            },
            { GachaType.Limited, new Dictionary<CardRarity, float>
                {
                    { CardRarity.SR, 0.3f },
                    { CardRarity.SSR, 0.6f },
                    { CardRarity.UR, 0.1f }
                }
            }
        };
        #endregion

        #region Private Fields
        // 卡牌模板缓存
        private readonly Dictionary<string, CardTemplate> _templates = new();

        // 玩家拥有的卡牌
        private readonly List<CardData> _ownedCards = new();

        // 已激活的卡牌ID
        private readonly HashSet<string> _activatedCardIds = new();

        // 保底计数器
        private readonly Dictionary<GachaType, int> _pityCounters = new()
        {
            { GachaType.Novice, 0 },
            { GachaType.Standard, 0 },
            { GachaType.Premium, 0 },
            { GachaType.Limited, 0 }
        };

        // 已拥有的卡牌ID集合（去重）
        private readonly HashSet<string> _ownedCardIds = new();
        #endregion

        #region Events
        public event Action<CardData> onCardAcquired;      // 获得卡牌
        public event Action<CardData> onCardActivated;    // 激活卡牌
        public event Action<CardData> onCardDeactivated; // 停用卡牌
        public event Action<GachaResult> onGachaCompleted; // 抽卡完成
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            LoadCardTemplates();
            Debug.Log("[CardDesign] Initialized");
        }

        /// <summary>
        /// 加载卡牌模板
        /// </summary>
        private void LoadCardTemplates()
        {
            // 先尝试从XML加载
            if (!LoadFromXml())
            {
                // XML加载失败时使用默认模板
                Debug.LogWarning("[CardDesign] Failed to load from XML, using default templates");
                LoadDefaultTemplates();
            }
        }

        /// <summary>
        /// 从XML文件加载卡牌模板
        /// </summary>
        private bool LoadFromXml()
        {
            try
            {
                var xmlPath = "Data/Cards/Cards";
                var textAsset = Resources.Load<TextAsset>(xmlPath);
                if (textAsset == null)
                {
                    Debug.LogWarning($"[CardDesign] Cards.xml not found at {xmlPath}");
                    return false;
                }

                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(textAsset.text);

                var root = doc.DocumentElement;
                if (root.Name != "Cards")
                {
                    Debug.LogError($"[CardDesign] Invalid root element: {root.Name}");
                    return false;
                }

                int loadedCount = 0;
                foreach (System.Xml.XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType != System.Xml.XmlNodeType.Element) continue;
                    if (node.Name != "Card") continue;

                    var template = ParseCardFromXml(node);
                    if (template != null)
                    {
                        AddTemplate(template);
                        loadedCount++;
                    }
                }

                Debug.Log($"[CardDesign] Loaded {loadedCount} card templates from XML");
                return loadedCount > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardDesign] Failed to load Cards.xml: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解析XML节点为卡牌模板
        /// </summary>
        private CardTemplate ParseCardFromXml(System.Xml.XmlNode node)
        {
            try
            {
                var template = new CardTemplate
                {
                    id = node.Attributes["id"]?.Value ?? "",
                    nameTextId = node.Attributes["nameTextId"]?.Value ?? "",
                    descTextId = node.Attributes["descTextId"]?.Value ?? "",
                    type = ParseCardType(node.Attributes["type"]?.Value ?? "Character"),
                    rarity = ParseCardRarity(node.Attributes["rarity"]?.Value ?? "N"),
                    attributeMultiplier = float.Parse(node.Attributes["attributeMultiplier"]?.Value ?? "1.0"),
                    affixCount = string.IsNullOrEmpty(node.Attributes["affixIds"]?.Value) ? 0 :
                        node.Attributes["affixIds"].Value.Split(',').Length
                };

                // 解析SellPrice
                var sellPriceNode = node.SelectSingleNode("SellPrice");
                if (sellPriceNode != null && int.TryParse(sellPriceNode.InnerText, out int sellPrice))
                {
                    template.sellPrice = sellPrice;
                }

                return template;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardDesign] Failed to parse card node: {ex.Message}");
                return null;
            }
        }

        private CardType ParseCardType(string value)
        {
            return value switch
            {
                "Character" => CardType.Character,
                "Equipment" => CardType.Equipment,
                "Skill" => CardType.Skill,
                "Item" => CardType.Item,
                "Event" => CardType.Event,
                _ => CardType.Character
            };
        }

        private CardRarity ParseCardRarity(string value)
        {
            return value switch
            {
                "N" => CardRarity.N,
                "R" => CardRarity.R,
                "SR" => CardRarity.SR,
                "SSR" => CardRarity.SSR,
                "UR" => CardRarity.UR,
                "GR" => CardRarity.GR,
                _ => CardRarity.N
            };
        }

        /// <summary>
        /// 加载默认模板（当XML不可用时）
        /// </summary>
        private void LoadDefaultTemplates()
        {
            AddTemplate(new CardTemplate
            {
                id = "Core.Card.Character.ZhangFei",
                type = CardType.Character,
                rarity = CardRarity.SR,
                nameTextId = "Core.Card.Character.ZhangFei.NameText",
                descTextId = "Core.Card.Character.ZhangFei.DescText",
                attributeMultiplier = 1.5f,
                affixCount = 2
            });

            AddTemplate(new CardTemplate
            {
                id = "Core.Card.Equipment.LongSword",
                type = CardType.Equipment,
                rarity = CardRarity.R,
                nameTextId = "Core.Card.Equipment.LongSword.NameText",
                descTextId = "Core.Card.Equipment.LongSword.DescText",
                attributeMultiplier = 1.2f,
                affixCount = 1
            });
        }

        /// <summary>
        /// 添加卡牌模板
        /// </summary>
        public void AddTemplate(CardTemplate template)
        {
            if (template == null || string.IsNullOrEmpty(template.id))
                return;

            _templates[template.id] = template;
        }

        /// <summary>
        /// 获取卡牌模板
        /// </summary>
        public CardTemplate GetTemplate(string cardId)
        {
            return _templates.TryGetValue(cardId, out var template) ? template : null;
        }

        /// <summary>
        /// 获取所有卡牌
        /// </summary>
        public List<CardData> GetAllCards()
        {
            return new List<CardData>(_ownedCards);
        }

        /// <summary>
        /// 按稀有度获取卡牌
        /// </summary>
        public List<CardData> GetCardsByRarity(CardRarity rarity)
        {
            var result = new List<CardData>();
            foreach (var card in _ownedCards)
            {
                if (card.rarity == rarity)
                    result.Add(card);
            }
            return result;
        }

        /// <summary>
        /// 获取指定稀有度卡牌数量
        /// </summary>
        public int GetCardCount(CardRarity rarity)
        {
            int count = 0;
            foreach (var card in _ownedCards)
            {
                if (card.rarity == rarity)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 检查是否拥有指定卡牌
        /// </summary>
        public bool HasCard(string cardId)
        {
            return _ownedCardIds.Contains(cardId);
        }

        /// <summary>
        /// 抽卡
        /// </summary>
        public GachaResult DrawCards(GachaType type, int count)
        {
            if (count <= 0)
                return GachaResult.Failure("抽卡数量必须大于0");

            int cost = _gachaCosts[type] * count;

            var player = GameLoopManager.instance?.player;
            if (player == null || player.carryItems.gold < cost)
                return GachaResult.Failure("金币不足");

            var drawnCards = new List<CardData>();
            bool isGuaranteed = false;
            int pityCounter = _pityCounters[type];

            for (int i = 0; i < count; i++)
            {
                // 保底计数
                pityCounter++;
                _pityCounters[type] = pityCounter;

                // 获取保底配置
                var pityConfig = GetPityConfig(type);
                CardRarity rarity;

                // 检查是否触发保底
                if (pityConfig != null && pityCounter >= pityConfig.guaranteeAfter)
                {
                    rarity = pityConfig.minRarity;
                    isGuaranteed = true;
                    pityCounter = 0;
                    _pityCounters[type] = 0;
                }
                else
                {
                    // 正常抽卡
                    rarity = DrawRarity(type);
                }

                // 根据稀有度抽取卡牌
                var card = DrawCardByRarity(type, rarity);
                if (card != null)
                {
                    drawnCards.Add(card);
                    AddCardToCollection(card);
                }
            }

            var result = GachaResult.Success(drawnCards, isGuaranteed, pityCounter, cost);
            onGachaCompleted?.Invoke(result);

            return result;
        }

        /// <summary>
        /// 检查是否触发保底（高级/限定包）
        /// </summary>
        public bool IsGuaranteedSSR(GachaType type)
        {
            if (type != GachaType.Premium && type != GachaType.Limited)
                return false;

            var pityConfig = GetPityConfig(type);
            if (pityConfig == null)
                return false;

            return _pityCounters[type] >= pityConfig.guaranteeAfter;
        }

        /// <summary>
        /// 激活卡牌
        /// </summary>
        public void ActivateCard(string cardId)
        {
            if (!_ownedCardIds.Contains(cardId))
                return;

            _activatedCardIds.Add(cardId);

            // 找到卡牌数据并更新
            foreach (var card in _ownedCards)
            {
                if (card.id == cardId)
                {
                    card.isActivated = true;
                    onCardActivated?.Invoke(card);
                    break;
                }
            }
        }

        /// <summary>
        /// 停用卡牌
        /// </summary>
        public void DeactivateCard(string cardId)
        {
            if (!_ownedCardIds.Contains(cardId))
                return;

            _activatedCardIds.Remove(cardId);

            // 找到卡牌数据并更新
            foreach (var card in _ownedCards)
            {
                if (card.id == cardId)
                {
                    card.isActivated = false;
                    onCardDeactivated?.Invoke(card);
                    break;
                }
            }
        }

        /// <summary>
        /// 检查卡牌是否已激活
        /// </summary>
        public bool IsCardActivated(string cardId)
        {
            return _activatedCardIds.Contains(cardId);
        }

        /// <summary>
        /// 获取已激活卡牌列表
        /// </summary>
        public List<string> GetActivatedCardIds()
        {
            return new List<string>(_activatedCardIds);
        }

        /// <summary>
        /// 获取已激活卡牌数据列表
        /// </summary>
        public List<CardData> GetActivatedCards()
        {
            var result = new List<CardData>();
            foreach (var card in _ownedCards)
            {
                if (card.isActivated)
                    result.Add(card);
            }
            return result;
        }

        /// <summary>
        /// 获取战斗加成（从已激活的卡牌计算）
        /// </summary>
        public CardCombatBonus GetCombatBonus()
        {
            var bonus = new CardCombatBonus();

            foreach (var card in _ownedCards)
            {
                if (!card.isActivated)
                    continue;

                // 根据卡牌类型应用加成
                switch (card.type)
                {
                    case CardType.Character:
                        // 角色卡提供属性倍率加成
                        bonus.damageMultiplier += card.attributeMultiplier - 1f;
                        break;

                    case CardType.Equipment:
                        // 装备卡提供防御加成
                        bonus.defenseMultiplier += card.attributeMultiplier - 1f;
                        break;

                    case CardType.Skill:
                        // 技能卡提供暴击加成
                        bonus.critMultiplier += card.attributeMultiplier - 1f;
                        break;

                    case CardType.Item:
                        // 道具卡提供生命加成
                        bonus.hpMultiplier += card.attributeMultiplier - 1f;
                        break;

                    case CardType.Event:
                        // 事件卡提供金币加成
                        bonus.goldMultiplier += card.attributeMultiplier - 1f;
                        break;
                }
            }

            return bonus;
        }

        /// <summary>
        /// 获取抽卡费用
        /// </summary>
        public int GetGachaCost(GachaType type)
        {
            return _gachaCosts.TryGetValue(type, out var cost) ? cost : 0;
        }

        /// <summary>
        /// 获取保底信息
        /// </summary>
        public (int current, int required) GetPityInfo(GachaType type)
        {
            var pityConfig = GetPityConfig(type);
            if (pityConfig == null)
                return (0, 0);

            return (_pityCounters[type], pityConfig.guaranteeAfter);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 根据抽卡类型获取保底配置
        /// </summary>
        private PityConfig GetPityConfig(GachaType type)
        {
            foreach (var config in _pityConfigs)
            {
                if (config.gachaType == type)
                    return config;
            }
            return null;
        }

        /// <summary>
        /// 根据抽卡类型抽取稀有度
        /// </summary>
        private CardRarity DrawRarity(GachaType type)
        {
            if (!_rarityWeights.TryGetValue(type, out var weights))
                return CardRarity.N;

            float totalWeight = 0;
            foreach (var w in weights.Values)
                totalWeight += w;

            float random = UnityEngine.Random.Range(0, totalWeight);
            float cumulative = 0;

            foreach (var pair in weights)
            {
                cumulative += pair.Value;
                if (random <= cumulative)
                    return pair.Key;
            }

            return CardRarity.N;
        }

        /// <summary>
        /// 根据稀有度抽取卡牌
        /// </summary>
        private CardData DrawCardByRarity(GachaType type, CardRarity rarity)
        {
            // 找到所有符合条件的模板
            var candidates = new List<CardTemplate>();
            foreach (var template in _templates.Values)
            {
                if (template.rarity == rarity)
                {
                    // 限定包需要检查标签
                    if (type == GachaType.Limited && template.tags != null && template.tags.Count > 0)
                    {
                        // 限定包只抽带标签的卡
                        candidates.Add(template);
                    }
                    else if (type != GachaType.Limited)
                    {
                        candidates.Add(template);
                    }
                }
            }

            if (candidates.Count == 0)
                return null;

            // 随机选择
            var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            return new CardData
            {
                id = selected.id,
                type = selected.type,
                rarity = selected.rarity,
                nameTextId = selected.nameTextId,
                descTextId = selected.descTextId,
                attributeMultiplier = selected.attributeMultiplier,
                affixIds = new List<string>(),
                isActivated = false
            };
        }

        /// <summary>
        /// 添加卡牌到收藏
        /// </summary>
        private void AddCardToCollection(CardData card)
        {
            // 如果已有，累加数量（需要添加count字段）
            // 如果没有，添加新记录
            if (!_ownedCardIds.Contains(card.id))
            {
                _ownedCards.Add(card);
                _ownedCardIds.Add(card.id);
                onCardAcquired?.Invoke(card);
            }
        }

        #endregion
    }
}