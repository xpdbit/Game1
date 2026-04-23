using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 卡牌管理器
    /// 提供静态API，委托给CardDesign实现
    /// 采用与TeamManager相同的静态API模式
    /// </summary>
    public static class CardManager
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Initialize()
        {
            CardDesign.instance.Initialize();
        }

        /// <summary>
        /// 获取所有卡牌
        /// </summary>
        public static List<CardData> GetAllCards()
        {
            return CardDesign.instance.GetAllCards();
        }

        /// <summary>
        /// 按稀有度获取卡牌
        /// </summary>
        public static List<CardData> GetCardsByRarity(CardRarity rarity)
        {
            return CardDesign.instance.GetCardsByRarity(rarity);
        }

        /// <summary>
        /// 获取卡牌数量
        /// </summary>
        public static int GetCardCount(CardRarity rarity)
        {
            return CardDesign.instance.GetCardCount(rarity);
        }

        /// <summary>
        /// 抽卡
        /// </summary>
        public static GachaResult DrawCards(GachaType type, int count = 1)
        {
            return CardDesign.instance.DrawCards(type, count);
        }

        /// <summary>
        /// 检查是否拥有卡牌
        /// </summary>
        public static bool HasCard(string cardId)
        {
            return CardDesign.instance.HasCard(cardId);
        }

        /// <summary>
        /// 激活卡牌
        /// </summary>
        public static void ActivateCard(string cardId)
        {
            CardDesign.instance.ActivateCard(cardId);
        }

        /// <summary>
        /// 停用卡牌
        /// </summary>
        public static void DeactivateCard(string cardId)
        {
            CardDesign.instance.DeactivateCard(cardId);
        }

        /// <summary>
        /// 检查卡牌是否已激活
        /// </summary>
        public static bool IsCardActivated(string cardId)
        {
            return CardDesign.instance.IsCardActivated(cardId);
        }

        /// <summary>
        /// 获取已激活卡牌ID列表
        /// </summary>
        public static List<string> GetActivatedCardIds()
        {
            return CardDesign.instance.GetActivatedCardIds();
        }

        /// <summary>
        /// 获取抽卡费用
        /// </summary>
        public static int GetGachaCost(GachaType type)
        {
            return CardDesign.instance.GetGachaCost(type);
        }

        /// <summary>
        /// 获取保底信息
        /// </summary>
        public static (int current, int required) GetPityInfo(GachaType type)
        {
            return CardDesign.instance.GetPityInfo(type);
        }

        /// <summary>
        /// 检查是否触发保底
        /// </summary>
        public static bool IsGuaranteedSSR(GachaType type)
        {
            return CardDesign.instance.IsGuaranteedSSR(type);
        }

        /// <summary>
        /// 获取卡牌模板
        /// </summary>
        public static CardTemplate GetTemplate(string cardId)
        {
            return CardDesign.instance.GetTemplate(cardId);
        }

        // 事件转发
        public static event System.Action<CardData> onCardAcquired
        {
            add => CardDesign.instance.onCardAcquired += value;
            remove => CardDesign.instance.onCardAcquired -= value;
        }

        public static event System.Action<CardData> onCardActivated
        {
            add => CardDesign.instance.onCardActivated += value;
            remove => CardDesign.instance.onCardActivated -= value;
        }

        public static event System.Action<CardData> onCardDeactivated
        {
            add => CardDesign.instance.onCardDeactivated += value;
            remove => CardDesign.instance.onCardDeactivated -= value;
        }

        public static event System.Action<GachaResult> onGachaCompleted
        {
            add => CardDesign.instance.onGachaCompleted += value;
            remove => CardDesign.instance.onGachaCompleted -= value;
        }
    }
}