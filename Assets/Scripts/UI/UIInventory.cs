using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 背包UI组件
    /// 继承BaseUIPanel以支持UIManager状态管理
    /// </summary>
    public class UIInventory : BaseUIPanel
    {
        public override string panelId => "InventoryPanel";

        [Header("UI列表组件")]
        public UIListItems uIListItems;
        public Button closeButton;

        [Header("模板引用（用于对象池）")]
        public RectTransform itemTemplate;

        [Header("设置")]
        public bool autoRefreshOnEvent = true;  // 自动刷新开关

        private List<UIInventoryItem> _items = new();
        private Dictionary<int, UIInventoryItem> _itemsByInstanceId = new();
        private Action<InventoryEventData> _onInventoryChanged;

        public UIInventoryItem[] items => _items.ToArray();

        #region Unity Lifecycle
        private void Awake()
        {
            if (uIListItems == null)
                uIListItems = GetComponent<UIListItems>();

            _onInventoryChanged = OnInventoryEvent;

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                Close();
            });
        }

        private void OnEnable()
        {
            if (autoRefreshOnEvent)
            {
                ItemManager.SubscribeInventoryChanged(_onInventoryChanged);
            }
            Refresh();
        }

        private void OnDisable()
        {
            ItemManager.UnsubscribeInventoryChanged(_onInventoryChanged);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            Refresh();
        }

        public override void OnClose()
        {
            base.OnClose();
        }
        #endregion

        #region Public API

        /// <summary>
        /// 刷新UI（从InventoryDesign同步数据）
        /// </summary>
        public void Refresh()
        {
            Clear();

            var inventory = ItemManager.GetInventory().ToList();

            // 按照名字排序
            inventory.Sort((a, b) => string.Compare(a.itemTemplate.id, b.itemTemplate.id));
            
            foreach (var itemInstance in inventory)
            {
                Append(itemInstance);
            }

        }

        /// <summary>
        /// 添加物品到UI
        /// </summary>
        public UIInventoryItem Append(ItemInstance itemInstance)
        {
            if (itemInstance == null || itemInstance.itemTemplate == null)
                return null;

            // 检查是否已存在
            if (_itemsByInstanceId.ContainsKey(itemInstance.instanceId))
            {
                // 更新现有项
                var existing = _itemsByInstanceId[itemInstance.instanceId];
                existing.UpdateUI();
                return existing;
            }

            // 添加新项（仅当uIListItems存在时）
            if (uIListItems == null)
                return null;

            var result = uIListItems.AddItem(itemInstance.itemTemplate.id + "_" + itemInstance.instanceId);
            var item = new UIInventoryItem
            {
                itemInstance = itemInstance,
                listItemRect = result.rectTransform
            };

            _items.Add(item);
            _itemsByInstanceId[itemInstance.instanceId] = item;
            item.UpdateUI();

            return item;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public void Remove(UIInventoryItem uiItem, int amount = -1)
        {
            if (uiItem == null || uiItem.itemInstance == null)
                return;

            int removeAmount = amount < 0 ? uiItem.itemInstance.amount : Math.Min(amount, uiItem.itemInstance.amount);

            // 调用InventoryDesign移除
            var removeResult = ItemManager.RemoveItem(uiItem.itemInstance.instanceId, removeAmount);
            if (!removeResult.success)
            {
                Debug.LogWarning($"[UIInventory] Failed to remove item: {removeResult.message}");
            }

            // UI会在事件回调中自动更新
        }

        /// <summary>
        /// 移除所有物品
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _itemsByInstanceId.Clear();
            if (uIListItems != null)
                uIListItems.Clear();
        }

        /// <summary>
        /// 获取选中的物品
        /// </summary>
        public List<UIInventoryItem> GetSelectedItems()
        {
            var selected = new List<UIInventoryItem>();
            foreach (var item in _items)
            {
                if (item.isSelected)
                    selected.Add(item);
            }
            return selected;
        }

        /// <summary>
        /// 选中物品
        /// </summary>
        public void SelectItem(UIInventoryItem item, bool selected)
        {
            if (item == null) return;
            item.SetSelected(selected);
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        public void SelectAll(bool selected)
        {
            foreach (var item in _items)
            {
                item.SetSelected(selected);
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// 响应背包变化事件
        /// </summary>
        private void OnInventoryEvent(InventoryEventData data)
        {
            switch (data.eventType)
            {
                case InventoryEventType.ItemAdded:
                    OnItemAdded(data);
                    break;
                case InventoryEventType.ItemRemoved:
                    OnItemRemoved(data);
                    break;
                case InventoryEventType.ItemUpdated:
                    OnItemUpdated(data);
                    break;
                case InventoryEventType.InventoryCleared:
                    OnInventoryCleared();
                    break;
                case InventoryEventType.CapacityChanged:
                    // 可以更新容量显示
                    break;
            }
        }

        private void OnItemAdded(InventoryEventData data)
        {
            if (uIListItems == null)
                return;

            var item = ItemManager.GetItem(data.instanceId);
            if (item != null)
            {
                Append(item);
            }
        }

        private void OnItemRemoved(InventoryEventData data)
        {
            if (uIListItems == null)
                return;

            if (_itemsByInstanceId.TryGetValue(data.instanceId, out var uiItem))
            {
                _items.Remove(uiItem);
                _itemsByInstanceId.Remove(data.instanceId);

                // 通知UIListItems移除（通过bindingId）
                if (uiItem.itemInstance?.itemTemplate != null)
                {
                    uIListItems.RemoveItem(uiItem.itemInstance.itemTemplate.id + "_" + data.instanceId);
                }
            }
        }

        private void OnItemUpdated(InventoryEventData data)
        {
            if (_itemsByInstanceId.TryGetValue(data.instanceId, out var uiItem))
            {
                uiItem.UpdateUI();
            }
        }

        private void OnInventoryCleared()
        {
            Clear();
        }

        #endregion
    }

    /// <summary>
    /// 背包物品行UI组件
    /// </summary>
    public class UIInventoryItem
    {
        public ItemInstance itemInstance;      // 物品实例
        public RectTransform listItemRect;     // 列表项RectTransform
        public bool isSelected { get; private set; }  // 是否选中

        // UI组件缓存
        private Button _button;
        private UIText _nameText;
        private UIText _amountText;
        private UIText _descText;
        private Image _iconImage;
        private Image _backgroundImage;
        private Toggle _toggle;

        public Button button => _button ??= listItemRect.Find("Button")?.GetComponent<Button>();
        private UIText nameText => _nameText ??= listItemRect.Find("Button/NameText")?.GetComponentInChildren<UIText>();
        private UIText amountText => _amountText ??= listItemRect.Find("Button/AmountText")?.GetComponentInChildren<UIText>();
        private UIText descText => _descText ??= listItemRect.Find("Button/DescText")?.GetComponentInChildren<UIText>();
        private Image iconImage => _iconImage ??= listItemRect.Find("Button/Icon")?.GetComponent<Image>();
        private Image backgroundImage => _backgroundImage ??= listItemRect.Find("Button/Background")?.GetComponent<Image>();
        private Toggle toggle => _toggle ??= listItemRect.Find("Toggle")?.GetComponent<Toggle>();

        public event Action<bool> onSelectedChanged;

        public void UpdateUI()
        {
            if (itemInstance?.itemTemplate == null) return;

            listItemRect.name = itemInstance.itemTemplate.id;

            if (nameText != null)
                nameText.text = GetDisplayName();

            if (amountText != null)
                amountText.text = "x" + itemInstance.amount.ToString();

            if (descText != null)
                descText.text = itemInstance.itemTemplate.descTextId;

            // 更新选中状态视觉
            UpdateSelectionVisual();
        }

        /// <summary>
        /// 获取显示名称（从nameTextId提取最后一部分）
        /// </summary>
        private string GetDisplayName()
        {
            if (string.IsNullOrEmpty(itemInstance.itemTemplate.nameTextId))
                return "Unknown";

            return Utils.GetIDPart(itemInstance.itemTemplate.id, 2);
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;
            UpdateSelectionVisual();
            onSelectedChanged?.Invoke(selected);
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelected()
        {
            SetSelected(!isSelected);
        }

        /// <summary>
        /// 更新选中状态视觉
        /// </summary>
        private void UpdateSelectionVisual()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected
                    ? new Color(0.3f, 0.6f, 1f, 0.3f)  // 选中高亮色
                    : Color.white;
            }
        }

        /// <summary>
        /// 设置锁定状态
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (button != null)
                button.interactable = !locked;

            // 可以添加其他锁定视觉反馈
        }

        /// <summary>
        /// 获取物品数据（用于UI绑定）
        /// </summary>
        public InventoryItemData GetItemData()
        {
            return new InventoryItemData(itemInstance)
            {
                isSelected = isSelected
            };
        }
    }
}
