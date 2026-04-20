using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Game1
{
    /// <summary>
    /// 背包物品行组件
    /// 左：Image + Name
    /// 右：Amount
    /// 状态：高亮、默认、禁用
    /// 支持勾选功能
    /// </summary>
    public class UIInventoryItem : MonoBehaviour, IPointerClickHandler
    {
        #region UI References
        [Header("物品信息")]
        public Image itemImage;
        public UIText itemNameText;
        public UIText amountText;

        [Header("勾选")]
        public Image checkmarkImage;
        public Image checkboxBackground;

        [Header("状态视觉")]
        public Image backgroundImage;
        public Color normalColor = new Color(1f, 1f, 1f, 0.5f);
        public Color highlightedColor = new Color(1f, 0.9f, 0.5f, 0.8f);
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public GameObject disabledOverlay;

        [Header("可勾选配置")]
        public bool isCheckable = true;
        #endregion

        #region Properties
        public string itemId { get; private set; }
        public bool isSelected => _isSelected;
        public InventoryItemState state => _state;

        private bool _isSelected = false;
        private InventoryItemState _state = InventoryItemState.Normal;
        private InventoryItemData _data;
        #endregion

        #region Events
        public event Action<UIInventoryItem> onItemClicked;
        public event Action<UIInventoryItem, bool> onSelectionChanged; // item, isSelected
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            UpdateCheckmarkVisibility();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 设置物品数据
        /// </summary>
        public void SetData(InventoryItemData data)
        {
            _data = data;
            itemId = data.id;

            if (itemImage != null && data.image != null)
                itemImage.sprite = data.image;

            if (itemNameText != null)
                itemNameText.text = data.name;

            if (amountText != null)
                amountText.text = data.amount.ToString();

            // 应用当前状态
            SetState(data.state);
            SetSelected(data.isSelected);
        }

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public InventoryItemData GetData()
        {
            if (_data != null)
            {
                _data.amount = int.TryParse(amountText?.text ?? "0", out int amt) ? amt : 0;
            }
            return _data;
        }

        /// <summary>
        /// 设置物品状态
        /// </summary>
        public void SetState(InventoryItemState newState)
        {
            _state = newState;
            UpdateVisuals();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected, bool triggerEvent = true)
        {
            if (!isCheckable) return;

            _isSelected = selected;
            if (_data != null)
                _data.isSelected = selected;

            UpdateCheckmark();
            UpdateVisuals();

            if (triggerEvent)
                onSelectionChanged?.Invoke(this, _isSelected);
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection()
        {
            SetSelected(!_isSelected);
        }

        /// <summary>
        /// 更新数量显示
        /// </summary>
        public void UpdateAmount(int newAmount)
        {
            if (amountText != null)
                amountText.text = newAmount.ToString();

            if (_data != null)
                _data.amount = newAmount;
        }

        /// <summary>
        /// 设置是否可勾选
        /// </summary>
        public void SetCheckable(bool checkable)
        {
            isCheckable = checkable;
            UpdateCheckmarkVisibility();
        }

        /// <summary>
        /// 设置是否可用(禁用状态)
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (!interactable)
            {
                SetState(InventoryItemState.Disabled);
            }
            else if (_state == InventoryItemState.Disabled)
            {
                SetState(InventoryItemState.Normal);
            }
        }
        #endregion

        #region Private Methods
        private void UpdateVisuals()
        {
            if (backgroundImage == null) return;

            switch (_state)
            {
                case InventoryItemState.Normal:
                    backgroundImage.color = normalColor;
                    if (disabledOverlay != null)
                        disabledOverlay.SetActive(false);
                    break;

                case InventoryItemState.Highlighted:
                    backgroundImage.color = highlightedColor;
                    if (disabledOverlay != null)
                        disabledOverlay.SetActive(false);
                    break;

                case InventoryItemState.Disabled:
                    backgroundImage.color = disabledColor;
                    if (disabledOverlay != null)
                        disabledOverlay.SetActive(true);
                    break;
            }
        }

        private void UpdateCheckmark()
        {
            if (checkmarkImage == null) return;
            checkmarkImage.gameObject.SetActive(_isSelected);
        }

        private void UpdateCheckmarkVisibility()
        {
            if (checkmarkImage != null)
                checkmarkImage.gameObject.SetActive(isCheckable && _isSelected);

            if (checkboxBackground != null)
                checkboxBackground.gameObject.SetActive(isCheckable);
        }
        #endregion

        #region Event Handlers
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_state == InventoryItemState.Disabled) return;

            // 处理勾选逻辑
            if (isCheckable)
            {
                // 左键切换选中状态
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    ToggleSelection();
                }
            }

            // 触发点击事件
            onItemClicked?.Invoke(this);
        }
        #endregion

        #region Editor Helper
        [ContextMenu("同步名称到Inspector")]
        public void SyncNameToInspector()
        {
            if (_data != null)
                this.gameObject.name = $"Item_{_data.name}";
        }
        #endregion
    }
}