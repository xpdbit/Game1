using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game1
{
    /// <summary>
    /// 选择对话框面板
    /// 用于显示路径选择、事件选项、确认对话框等
    /// </summary>
    public class UISelectionDialog : BaseUIPanel
    {
        public override string panelId => "SelectionDialog";

        [Header("UI引用")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private UIListItems _optionsList;

        [Header("选项按钮预设")]
        public RectTransform optionButtonPrefab;

        [Header("设置")]
        public DialogType dialogType = DialogType.Selection;

        // 当前对话框数据
        private SelectionEventData _currentData;
        private List<ChoiceOption> _options = new();
        private readonly List<RectTransform> _optionButtons = new();

        // 事件
        public event Action<ChoiceOption> onOptionSelected;
        public event Action onDialogClosed;

        #region Unity Lifecycle
        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);

            if (_optionsList != null && optionButtonPrefab != null)
            {
                _optionsList.templateRT = optionButtonPrefab;
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();
        }
        #endregion

        #region BaseUIPanel
        public override void OnOpen()
        {
            base.OnOpen();
            RefreshUI();
        }

        public override void OnClose()
        {
            base.OnClose();
            ClearOptions();
            _currentData = null;
            onDialogClosed?.Invoke();
        }
        #endregion

        #region Public API

        /// <summary>
        /// 显示选择对话框
        /// </summary>
        public void ShowSelectionDialog(string title, string description, List<ChoiceOption> options, Action<ChoiceOption> onSelected = null)
        {
            _currentData = new SelectionEventData
            {
                dialogType = DialogType.Selection,
                title = title,
                description = description,
                options = options,
                onOptionSelected = onSelected
            };

            _options = options ?? new List<ChoiceOption>();
            this.Open();
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public void ShowConfirmDialog(string title, string description, Action onConfirm = null, Action onCancel = null)
        {
            var options = new List<ChoiceOption>
            {
                ChoiceOption.Create("confirm", "确认", ChoiceType.Confirm),
                ChoiceOption.Create("cancel", "取消", ChoiceType.Cancel)
            };

            _currentData = new SelectionEventData
            {
                dialogType = DialogType.Confirm,
                title = title,
                description = description,
                options = options,
                onOptionSelected = (option) =>
                {
                    if (option.optionId == "confirm")
                        onConfirm?.Invoke();
                    else
                        onCancel?.Invoke();
                }
            };

            _options = options;
            this.Open();
        }

        /// <summary>
        /// 显示路径选择对话框（专用）
        /// </summary>
        public void ShowPathSelectionDialog(string title, List<Location> choices, Action<Location> onPathSelected)
        {
            var options = new List<ChoiceOption>();
            foreach (var location in choices)
            {
                var option = ChoiceOption.Create(location.id, GetLocationDisplayName(location), ChoiceType.Normal);
                option.tagRequirements.Add(location.type.ToString());
                options.Add(option);
            }

            ShowSelectionDialog(title, "选择一个方向继续前进：", options, (choice) =>
            {
                var selectedLocation = choices.Find(l => l.id == choice.optionId);
                onPathSelected?.Invoke(selectedLocation);
            });
        }

        /// <summary>
        /// 显示事件选项对话框
        /// </summary>
        public void ShowEventOptionsDialog(string title, string description, List<ChoiceOption> options, Action<ChoiceOption> onSelected)
        {
            ShowSelectionDialog(title, description, options, onSelected);
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        public void CloseDialog()
        {
            this.Close();
        }

        #endregion

        #region Private Methods

        private void RefreshUI()
        {
            if (_currentData == null) return;

            if (_titleText != null)
                _titleText.text = _currentData.title;

            if (_descriptionText != null)
                _descriptionText.text = _currentData.description;

            BuildOptionButtons();
        }

        private void BuildOptionButtons()
        {
            ClearOptions();

            if (_options == null || _options.Count == 0) return;

            foreach (var option in _options)
            {
                var button = CreateOptionButton(option);
                _optionButtons.Add(button);
            }
        }

        private RectTransform CreateOptionButton(ChoiceOption option)
        {
            var result = _optionsList.AddItem(option.optionId);
            var buttonRT = result.rectTransform;

            // 设置选项文本
            var buttonText = buttonRT.Find("Text")?.GetComponent<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = option.text;
            }

            // 设置选项颜色（根据类型）
            var background = buttonRT.Find("Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.color = GetChoiceTypeColor(option.choiceType);
            }

            // 添加点击事件
            var button = buttonRT.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnOptionClicked(option));
                button.interactable = option.isEnabled;
            }

            return buttonRT;
        }

        private Color GetChoiceTypeColor(ChoiceType type)
        {
            return type switch
            {
                ChoiceType.Confirm => new Color(0.2f, 0.8f, 0.2f, 0.3f),   // 绿色
                ChoiceType.Cancel => new Color(0.8f, 0.2f, 0.2f, 0.3f),    // 红色
                ChoiceType.Danger => new Color(0.9f, 0.5f, 0.1f, 0.3f),   // 橙色
                ChoiceType.Special => new Color(0.2f, 0.5f, 0.9f, 0.3f), // 蓝色
                _ => Color.white
            };
        }

        private void ClearOptions()
        {
            _optionsList.Clear();
            _optionButtons.Clear();
        }

        private void OnOptionClicked(ChoiceOption option)
        {
            if (!option.isEnabled) return;

            _currentData.selectedOption = option;
            onOptionSelected?.Invoke(option);
            _currentData.onOptionSelected?.Invoke(option);

            this.Close();
        }

        private void OnCloseButtonClicked()
        {
            this.Close();
        }

        private string GetLocationDisplayName(Location location)
        {
            if (string.IsNullOrEmpty(location.locationName))
                return location.id;

            var parts = location.locationName.Split('.');
            return parts.Length > 0 ? parts[parts.Length - 1] : location.locationName;
        }

        #endregion
    }
}
