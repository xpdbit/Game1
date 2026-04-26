using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 卡牌背包UI面板
    /// 继承BaseUIPanel以支持UIManager状态管理
    /// 使用UIListItems对象池管理卡牌列表
    /// </summary>
    public class UICardPanel : BaseUIPanel
    {
        public override string panelId => "CardPanel";

        #region UI Components
        [Header("列表组件")]
        public UIListItems uIListItems;
        public Button closeButton;
        public Button gachaButton;
        public Button sortButton;
        public Button filterButton;

        [Header("抽卡区域")]
        public Button noviceGachaButton;
        public Button standardGachaButton;
        public Button premiumGachaButton;
        public Button limitedGachaButton;
        public UIText gachaCostText;
        public UIText pityInfoText;

        [Header("卡牌详情")]
        public GameObject cardDetailPanel;
        public UIText detailNameText;
        public UIText detailDescText;
        public UIText detailRarityText;
        public UIText detailAttrText;
        public Button equipButton;
        public Button sellButton;
        public Button closeDetailButton;

        [Header("设置")]
        public bool autoRefreshOnEvent = true;
        public int cardsPerRow = 4;
        public float cardSpacing = 10f;
        #endregion

        #region Private Fields
        private List<UICardItem> _cards = new();
        private Dictionary<string, UICardItem> _cardsById = new();
        private Action<CardData> _onCardEvent;

        // 当前筛选/排序状态
        private CardRarity? _filterRarity = null;
        private CardType? _filterType = null;
        private bool _sortByRarityDesc = true;

        // 当前选中卡牌
        private UICardItem _selectedCard = null;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (uIListItems == null)
                uIListItems = GetComponentInChildren<UIListItems>();

            _onCardEvent = OnCardEvent;

            // 关闭按钮
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Close());

            // 排序按钮
            if (sortButton != null)
                sortButton.onClick.RemoveAllListeners();
                sortButton.onClick.AddListener(OnSortButtonClicked);

            // 筛选按钮
            if (filterButton != null)
                filterButton.onClick.RemoveAllListeners();
                filterButton.onClick.AddListener(OnFilterButtonClicked);

            // 抽卡按钮
            if (noviceGachaButton != null)
                noviceGachaButton.onClick.RemoveAllListeners();
                noviceGachaButton.onClick.AddListener(() => OnGachaClicked(GachaType.Novice));

            if (standardGachaButton != null)
                standardGachaButton.onClick.RemoveAllListeners();
                standardGachaButton.onClick.AddListener(() => OnGachaClicked(GachaType.Standard));

            if (premiumGachaButton != null)
                premiumGachaButton.onClick.RemoveAllListeners();
                premiumGachaButton.onClick.AddListener(() => OnGachaClicked(GachaType.Premium));

            if (limitedGachaButton != null)
                limitedGachaButton.onClick.RemoveAllListeners();
                limitedGachaButton.onClick.AddListener(() => OnGachaClicked(GachaType.Limited));

            // 卡牌详情关闭
            if (closeDetailButton != null)
                closeDetailButton.onClick.RemoveAllListeners();
                closeDetailButton.onClick.AddListener(() => HideCardDetail());

            // 装备按钮
            if (equipButton != null)
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(OnEquipButtonClicked);

            // 出售按钮
            if (sellButton != null)
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(OnSellButtonClicked);

            // 默认隐藏详情面板
            if (cardDetailPanel != null)
                cardDetailPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (autoRefreshOnEvent)
            {
                CardDesign.instance.onCardAcquired += _onCardEvent;
                CardDesign.instance.onCardActivated += _onCardEvent;
                CardDesign.instance.onCardDeactivated += _onCardEvent;
            }
            Refresh();
            UpdateGachaInfo();
        }

        private void OnDisable()
        {
            CardDesign.instance.onCardAcquired -= _onCardEvent;
            CardDesign.instance.onCardActivated -= _onCardEvent;
            CardDesign.instance.onCardDeactivated -= _onCardEvent;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            Refresh();
            UpdateGachaInfo();
        }

        public override void OnClose()
        {
            base.OnClose();
            HideCardDetail();
        }

        public override void OnUpdate(float deltaTime)
        {
            // 可以添加抽卡动画更新等
        }
        #endregion

        #region Public API

        /// <summary>
        /// 刷新UI（从CardDesign同步数据）
        /// </summary>
        public void Refresh()
        {
            Clear();

            var allCards = CardDesign.instance.GetAllCards();

            // 筛选
            if (_filterRarity.HasValue)
            {
                allCards = allCards.FindAll(c => c.rarity == _filterRarity.Value);
            }
            if (_filterType.HasValue)
            {
                allCards = allCards.FindAll(c => c.type == _filterType.Value);
            }

            // 排序
            if (_sortByRarityDesc)
                allCards.Sort((a, b) => b.rarity.CompareTo(a.rarity));
            else
                allCards.Sort((a, b) => a.rarity.CompareTo(b.rarity));

            foreach (var card in allCards)
            {
                Append(card);
            }
        }

        /// <summary>
        /// 添加卡牌到UI
        /// </summary>
        public UICardItem Append(CardData cardData)
        {
            if (cardData == null)
                return null;

            // 检查是否已存在
            if (_cardsById.ContainsKey(cardData.id))
            {
                var existing = _cardsById[cardData.id];
                existing.UpdateUI();
                return existing;
            }

            if (uIListItems == null)
                return null;

            // 使用卡牌ID作为bindingId
            var result = uIListItems.AddItem(cardData.id);
            var cardItem = new UICardItem
            {
                cardData = cardData,
                listItemRect = result.rectTransform,
                panel = this
            };

            // 设置点击事件
            var button = result.rectTransform.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCardClicked(cardItem));
            }

            _cards.Add(cardItem);
            _cardsById[cardData.id] = cardItem;
            cardItem.UpdateUI();

            return cardItem;
        }

        /// <summary>
        /// 移除卡牌
        /// </summary>
        public void Remove(UICardItem cardItem)
        {
            if (cardItem == null || cardItem.cardData == null)
                return;

            if (_cardsById.TryGetValue(cardItem.cardData.id, out var existing) && existing == cardItem)
            {
                _cards.Remove(cardItem);
                _cardsById.Remove(cardItem.cardData.id);
                uIListItems?.RemoveItem(cardItem.cardData.id);
            }
        }

        /// <summary>
        /// 清空所有卡牌
        /// </summary>
        public void Clear()
        {
            _cards.Clear();
            _cardsById.Clear();
            if (uIListItems != null)
                uIListItems.Clear();
        }

        /// <summary>
        /// 显示卡牌详情
        /// </summary>
        public void ShowCardDetail(CardData cardData)
        {
            if (cardDetailPanel == null || cardData == null)
                return;

            cardDetailPanel.SetActive(true);

            // 更新详情文本
            if (detailNameText != null)
                detailNameText.text = GetDisplayName(cardData);

            if (detailDescText != null)
                detailDescText.text = cardData.descTextId;

            if (detailRarityText != null)
                detailRarityText.text = cardData.GetRarityName();

            if (detailAttrText != null)
                detailAttrText.text = $"属性倍率: {cardData.attributeMultiplier:F2}";

            // 更新按钮状态
            if (equipButton != null)
                equipButton.gameObject.SetActive(!cardData.isActivated);

            // 保存选中
            _selectedCard = _cardsById.TryGetValue(cardData.id, out var item) ? item : null;
        }

        /// <summary>
        /// 隐藏卡牌详情
        /// </summary>
        public void HideCardDetail()
        {
            if (cardDetailPanel != null)
                cardDetailPanel.SetActive(false);
            _selectedCard = null;
        }

        /// <summary>
        /// 更新抽卡信息显示
        /// </summary>
        public void UpdateGachaInfo()
        {
            if (gachaCostText != null)
            {
                int cost = CardDesign.instance.GetGachaCost(GachaType.Standard);
                gachaCostText.text = $"抽卡费用: {cost} 金币";
            }

            if (pityInfoText != null)
            {
                var (current, required) = CardDesign.instance.GetPityInfo(GachaType.Standard);
                pityInfoText.text = $"保底进度: {current}/{required}";
            }
        }

        #endregion

        #region Event Handling

        private void OnCardEvent(CardData cardData)
        {
            Refresh();
            UpdateGachaInfo();
        }

        private void OnCardClicked(UICardItem cardItem)
        {
            if (cardItem?.cardData == null)
                return;

            ShowCardDetail(cardItem.cardData);
        }

        private void OnSortButtonClicked()
        {
            _sortByRarityDesc = !_sortByRarityDesc;
            Refresh();

            if (sortButton != null)
            {
                var colors = sortButton.colors;
                colors.highlightedColor = _sortByRarityDesc ? Color.red : Color.blue;
                sortButton.colors = colors;
            }
        }

        private void OnFilterButtonClicked()
        {
            // 循环切换筛选状态: 无筛选 -> N -> R -> SR -> SSR -> UR -> GR -> 无筛选
            if (!_filterRarity.HasValue)
                _filterRarity = CardRarity.N;
            else
            {
                var nextRarity = _filterRarity.Value switch
                {
                    CardRarity.N => CardRarity.R,
                    CardRarity.R => CardRarity.SR,
                    CardRarity.SR => CardRarity.SSR,
                    CardRarity.SSR => CardRarity.UR,
                    CardRarity.UR => CardRarity.GR,
                    CardRarity.GR => (CardRarity?)null,
                    _ => (CardRarity?)null
                };
                _filterRarity = nextRarity;
            }

            Refresh();

            if (filterButton != null && _filterRarity.HasValue)
            {
                var colors = filterButton.colors;
                colors.highlightedColor = Color.green;
                filterButton.colors = colors;
            }
        }

        private void OnGachaClicked(GachaType type)
        {
            int cost = CardDesign.instance.GetGachaCost(type);

            // TODO: 检查玩家金币是否足够
            // 目前简化处理：直接抽
            var result = CardDesign.instance.DrawCards(type, 1);

            if (result.success && result.cards.Count > 0)
            {
                Debug.Log($"[UICardPanel] Gacha {type} result: {result.cards[0].id} (Rarity: {result.cards[0].GetRarityName()})");

                // 播放抽卡动画（TODO：后续完善）
                ShowGachaResult(result.cards[0]);

                Refresh();
                UpdateGachaInfo();
            }
        }

        private void OnEquipButtonClicked()
        {
            if (_selectedCard?.cardData == null)
                return;

            if (_selectedCard.cardData.isActivated)
                CardDesign.instance.DeactivateCard(_selectedCard.cardData.id);
            else
                CardDesign.instance.ActivateCard(_selectedCard.cardData.id);

            _selectedCard.UpdateUI();
            HideCardDetail();
        }

        private void OnSellButtonClicked()
        {
            if (_selectedCard?.cardData == null)
                return;

            var template = CardDesign.instance.GetTemplate(_selectedCard.cardData.id);
            int sellPrice = template?.sellPrice ?? 0;

            // TODO: 确认出售 UI 对话框
            Debug.Log($"[UICardPanel] Sell card {_selectedCard.cardData.id} for {sellPrice} gold");

            // 简化处理：直接移除
            CardDesign.instance.DeactivateCard(_selectedCard.cardData.id);
            Remove(_selectedCard);
            HideCardDetail();
            Refresh();
        }

        /// <summary>
        /// 显示抽卡结果（简化版本）
        /// </summary>
        private void ShowGachaResult(CardData cardData)
        {
            // TODO: 实现抽卡动画效果
            // 可以使用DOTween或粒子系统实现
            Debug.Log($"[UICardPanel] Gacha animation for: {cardData.id}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 获取卡牌显示名称
        /// </summary>
        private string GetDisplayName(CardData cardData)
        {
            if (string.IsNullOrEmpty(cardData.nameTextId))
                return "Unknown";

            // 从ID提取最后一部分作为显示名
            var parts = cardData.nameTextId.Split('.');
            return parts.Length > 0 ? parts[parts.Length - 1] : cardData.nameTextId;
        }

        #endregion

        #region Editor Helper
#if UNITY_EDITOR
        [ContextMenu("Refresh In Editor")]
        public void RefreshInEditor()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[UICardPanel] Refresh only available in Play mode");
                return;
            }
            Refresh();
        }
#endif
        #endregion
    }

    /// <summary>
    /// 卡牌列表项UI组件
    /// </summary>
    public class UICardItem
    {
        public CardData cardData;           // 卡牌数据
        public RectTransform listItemRect;  // 列表项RectTransform
        public UICardPanel panel;           // 父面板引用

        // UI组件缓存
        private Button _button;
        private UIText _nameText;
        private UIText _rarityText;
        private UIText _typeText;
        private Image _backgroundImage;
        private Image _iconImage;
        private GameObject _activatedBadge;

        public Button button => _button ??= listItemRect?.Find("Button")?.GetComponent<Button>();
        private UIText nameText => _nameText ??= listItemRect?.Find("Button/NameText")?.GetComponentInChildren<UIText>();
        private UIText rarityText => _rarityText ??= listItemRect?.Find("Button/RarityText")?.GetComponentInChildren<UIText>();
        private UIText typeText => _typeText ??= listItemRect?.Find("Button/TypeText")?.GetComponentInChildren<UIText>();
        private Image backgroundImage => _backgroundImage ??= listItemRect?.Find("Button/Background")?.GetComponent<Image>();
        private Image iconImage => _iconImage ??= listItemRect?.Find("Button/Icon")?.GetComponent<Image>();
        private GameObject activatedBadge => _activatedBadge ??= listItemRect?.Find("Button/ActivatedBadge")?.gameObject;

        public event Action<bool> onSelectedChanged;

        /// <summary>
        /// 更新UI显示
        /// </summary>
        public void UpdateUI()
        {
            if (cardData == null) return;

            listItemRect.name = cardData.id;

            if (nameText != null)
                nameText.text = GetDisplayName();

            if (rarityText != null)
                rarityText.text = cardData.GetRarityName();

            if (typeText != null)
                typeText.text = cardData.type.ToString();

            // 更新背景颜色（根据稀有度）
            if (backgroundImage != null)
            {
                Color rarityColor;
                if (ColorUtility.TryParseHtmlString(cardData.GetRarityColor(), out rarityColor))
                    backgroundImage.color = rarityColor;
            }

            // 更新激活状态
            if (activatedBadge != null)
                activatedBadge.SetActive(cardData.isActivated);
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        private string GetDisplayName()
        {
            if (string.IsNullOrEmpty(cardData.nameTextId))
                return "Unknown";

            var parts = cardData.nameTextId.Split('.');
            return parts.Length > 0 ? parts[parts.Length - 1] : cardData.nameTextId;
        }

        /// <summary>
        /// 点击事件
        /// </summary>
        public void OnClick()
        {
            panel?.ShowCardDetail(cardData);
        }
    }
}