using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using XUtilities;

namespace Game1
{
    /// <summary>
    /// 背包选择模式
    /// </summary>
    public enum InventorySelectionMode
    {
        Single,     // 单选
        Multi       // 多选
    }

    /// <summary>
    /// 背包组件 - List式背包，支持勾选、多选
    /// 使用UIListItems作为列表管理
    /// </summary>
    public class UIInventory : MonoBehaviour
    {
        #region UI References
        [Header("列表管理")]
        public UIListItems listItems;
        public RectTransform itemTemplate;

        [Header("列表布局")]
        public UILayout layout;

        [Header("顶部操作")]
        public UnityEngine.UI.Button selectAllButton;
        public UnityEngine.UI.Button deselectAllButton;
        public UIText selectedCountText;

        [Header("底部操作")]
        public UnityEngine.UI.Button useButton;
        public UnityEngine.UI.Button discardButton;
        public UIText totalItemsText;

        [Header("配置")]
        public InventorySelectionMode selectionMode = InventorySelectionMode.Multi;
        public bool allowMultiSelect = true;
        #endregion

        #region Properties
        public InventoryData data => _data;
        public int selectedCount => _selectedItems.Count;
        public IReadOnlyList<InventoryItemData> selectedItems => _selectedItems;
        public IReadOnlyList<UIInventoryItem> selectedItemComponents => _selectedItemComponents;

        private InventoryData _data = new();
        private Dictionary<string, UIInventoryItem> _itemComponents = new();
        private List<InventoryItemData> _selectedItems = new();
        private List<UIInventoryItem> _selectedItemComponents = new();
        private UIInventoryItem _lastSelectedItem;
        #endregion

        #region Events
        public event Action<InventoryItemData> onItemAdded;
        public event Action<InventoryItemData> onItemRemoved;
        public event Action onSelectionChanged;
        public event Action<InventoryItemData> onItemClicked;
        public event Action<List<InventoryItemData>> onItemsUsed;
        public event Action<List<InventoryItemData>> onItemsDiscarded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            SetupEventListeners();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (listItems == null)
                listItems = this.GetComponent<UIListItems>();

            if (layout == null)
                layout = this.GetComponent<UILayout>();

            if (listItems != null && itemTemplate != null)
            {
                listItems.templateRT = itemTemplate;
            }
        }

        private void SetupEventListeners()
        {
            if (selectAllButton != null)
                selectAllButton.onClick.AddListener(SelectAll);

            if (deselectAllButton != null)
                deselectAllButton.onClick.AddListener(DeselectAll);

            if (useButton != null)
                useButton.onClick.AddListener(OnUseClicked);

            if (discardButton != null)
                discardButton.onClick.AddListener(OnDiscardClicked);
        }
        #endregion

        #region Public Methods - Data Management
        /// <summary>
        /// 设置背包数据
        /// </summary>
        public void SetData(InventoryData data)
        {
            _data = data;
            Refresh();
        }

        /// <summary>
        /// 添加物品
        /// </summary>
        public void AddItem(InventoryItemData item)
        {
            _data.AddItem(item);
            AddItemComponent(item);
            UpdateUI();
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public void RemoveItem(string id)
        {
            // 取消选中
            var itemData = _data.GetItem(id);
            if (itemData != null)
            {
                DeselectItem(itemData);
            }

            // 移除组件
            if (_itemComponents.TryGetValue(id, out var component))
            {
                listItems.RemoveItem(id);
                _itemComponents.Remove(id);
                UnityEngine.Object.Destroy(component.gameObject);
            }

            _data.RemoveItem(id);
            UpdateUI();
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public void Clear()
        {
            DeselectAll();
            _itemComponents.Clear();
            listItems?.Clear();
            _data.Clear();
            UpdateUI();
        }

        /// <summary>
        /// 刷新背包显示
        /// </summary>
        public void Refresh()
        {
            Clear();
            foreach (var item in _data.items)
            {
                AddItemComponent(item);
            }
            UpdateUI();
        }
        #endregion

        #region Public Methods - Selection
        /// <summary>
        /// 全选
        /// </summary>
        public void SelectAll()
        {
            foreach (var kvp in _itemComponents)
            {
                if (kvp.Value.state != InventoryItemState.Disabled)
                {
                    SelectItem(kvp.Value.GetData(), false);
                }
            }
            UpdateSelectionUI();
            onSelectionChanged?.Invoke();
        }

        /// <summary>
        /// 取消全选
        /// </summary>
        public void DeselectAll()
        {
            _selectedItems.Clear();
            _selectedItemComponents.Clear();

            foreach (var component in _itemComponents.Values)
            {
                component.SetSelected(false, false);
            }

            UpdateSelectionUI();
            onSelectionChanged?.Invoke();
        }

        /// <summary>
        /// 选择物品
        /// </summary>
        public void SelectItem(InventoryItemData item, bool triggerEvent = true)
        {
            if (item == null) return;

            if (!_selectedItems.Contains(item))
            {
                _selectedItems.Add(item);
            }

            if (_itemComponents.TryGetValue(item.id, out var component))
            {
                if (!_selectedItemComponents.Contains(component))
                {
                    _selectedItemComponents.Add(component);
                }
                component.SetSelected(true, false);
            }

            _lastSelectedItem = component;

            if (triggerEvent)
            {
                UpdateSelectionUI();
                onSelectionChanged?.Invoke();
            }
        }

        /// <summary>
        /// 取消选择物品
        /// </summary>
        public void DeselectItem(InventoryItemData item)
        {
            if (item == null) return;

            _selectedItems.Remove(item);

            if (_itemComponents.TryGetValue(item.id, out var component))
            {
                _selectedItemComponents.Remove(component);
                component.SetSelected(false, false);
            }

            UpdateSelectionUI();
            onSelectionChanged?.Invoke();
        }

        /// <summary>
        /// 范围选择（Shift+点击）
        /// </summary>
        public void SelectRange(UIInventoryItem target)
        {
            if (target == null || _lastSelectedItem == null) return;

            var lastIndex = -1;
            var targetIndex = -1;
            var items = listItems.children.ToList();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i].GetComponent<UIInventoryItem>();
                if (item == null) continue;

                if (item == _lastSelectedItem)
                    lastIndex = i;
                if (item == target)
                    targetIndex = i;
            }

            if (lastIndex < 0 || targetIndex < 0) return;

            int start = Mathf.Min(lastIndex, targetIndex);
            int end = Mathf.Max(lastIndex, targetIndex);

            for (int i = start; i <= end; i++)
            {
                var item = items[i].GetComponent<UIInventoryItem>();
                if (item != null && item.state != InventoryItemState.Disabled)
                {
                    SelectItem(item.GetData(), false);
                }
            }

            UpdateSelectionUI();
            onSelectionChanged?.Invoke();
        }

        /// <summary>
        /// 切换选择（Ctrl+点击）
        /// </summary>
        public void ToggleSelection(UIInventoryItem target)
        {
            if (target == null) return;

            var data = target.GetData();
            if (data == null) return;

            if (target.isSelected)
                DeselectItem(data);
            else
                SelectItem(data);
        }
        #endregion

        #region Private Methods - Component Management
        private void AddItemComponent(InventoryItemData item)
        {
            if (listItems == null || itemTemplate == null) return;

            var result = listItems.AddItem(item.id);
            var go = result.rectTransform.gameObject;

            // 设置层级名称方便调试
            go.name = $"Item_{item.name}";

            var component = go.GetComponent<UIInventoryItem>();
            if (component == null)
                component = go.AddComponent<UIInventoryItem>();

            component.SetData(item);
            component.isCheckable = true;
            component.onItemClicked += OnItemClicked;
            component.onSelectionChanged += OnItemSelectionChanged;

            _itemComponents[item.id] = component;

            // 设置布局
            if (layout != null)
            {
                layout.Layout(new UILayout.LayoutSender()).ExecuteImmediatelyAsync();
            }
        }

        private void OnItemClicked(UIInventoryItem item)
        {
            // 处理多选逻辑
            if (allowMultiSelect)
            {
                // 使用EventSystem检测按键
                bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (isShift)
                {
                    SelectRange(item);
                }
                else if (isCtrl)
                {
                    ToggleSelection(item);
                }
                else
                {
                    // 普通点击：如果是单选模式则先清空
                    if (selectionMode == InventorySelectionMode.Single)
                    {
                        DeselectAll();
                        SelectItem(item.GetData());
                    }
                    else
                    {
                        DeselectAll();
                        SelectItem(item.GetData());
                    }
                }
            }

            var data = item.GetData();
            if (data != null)
            {
                onItemClicked?.Invoke(data);
            }
        }

        private void OnItemSelectionChanged(UIInventoryItem item, bool isSelected)
        {
            var data = item.GetData();
            if (data == null) return;

            if (isSelected)
            {
                if (!_selectedItems.Contains(data))
                    _selectedItems.Add(data);
                if (!_selectedItemComponents.Contains(item))
                    _selectedItemComponents.Add(item);
            }
            else
            {
                _selectedItems.Remove(data);
                _selectedItemComponents.Remove(item);
            }

            UpdateSelectionUI();
            onSelectionChanged?.Invoke();
        }
        #endregion

        #region Private Methods - UI Updates
        private void UpdateUI()
        {
            UpdateSelectionUI();
            UpdateTotalText();
        }

        private void UpdateSelectionUI()
        {
            if (selectedCountText != null)
                selectedCountText.text = $"已选择: {selectedCount}";

            if (useButton != null)
                useButton.interactable = selectedCount > 0;

            if (discardButton != null)
                discardButton.interactable = selectedCount > 0;
        }

        private void UpdateTotalText()
        {
            if (totalItemsText != null)
                totalItemsText.text = $"共 {_data.items.Count} 个物品";
        }
        #endregion

        #region Button Actions
        private void OnUseClicked()
        {
            if (_selectedItems.Count > 0)
            {
                onItemsUsed?.Invoke(_selectedItems.ToList());
            }
        }

        private void OnDiscardClicked()
        {
            if (_selectedItems.Count > 0)
            {
                // 确认后丢弃
                var itemsToRemove = _selectedItems.ToList();
                onItemsDiscarded?.Invoke(itemsToRemove);

                // 清空选择
                DeselectAll();
            }
        }
        #endregion

        #region Editor Helper
        [ContextMenu("刷新显示")]
        public void EditorRefresh()
        {
            Initialize();
            Refresh();
        }
        #endregion
    }
}