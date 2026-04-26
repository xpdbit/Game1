using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Game1.UI.Dialog
{
    /// <summary>
    /// 对话事件数据
    /// </summary>
    [Serializable]
    public class DialogEventData
    {
        public string speakerId;           // 说话者ID
        public string speakerName;         // 说话者名称
        public Sprite portrait;            // 头像
        public CharacterExpression expression = CharacterExpression.Neutral; // 表情
        public string title;               // 对话框标题
        public string description;         // 对话文本
        public List<ChoiceOption> options; // 选项列表
        public Action<ChoiceOption> onOptionSelected;
        public Action onDialogClosed;
        public DialogAnimationType animationType = DialogAnimationType.Typewriter;
        
        // 记录用户选择的选项
        public ChoiceOption selectedOption;
    }

    /// <summary>
    /// 增强版选择对话框
    /// 支持打字机效果、淡入淡出、角色头像、选项动画
    /// </summary>
    public class UISelectionDialogEx : BaseUIPanel
    {
        public override string panelId => "SelectionDialogEx";

        [Header("UI引用 - 基础")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private Button _closeButton;
        [SerializeField] private UIListItems _optionsList;

        [Header("UI引用 - 动画组件")]
        [SerializeField] private DialogAnimator _dialogAnimator;
        [SerializeField] private TypewriterEffect _typewriter;
        [SerializeField] private FadeEffect _fadeEffect;
        [SerializeField] private CharacterPortrait _characterPortrait;

        [Header("选项按钮预设")]
        public RectTransform optionButtonPrefab;

        [Header("头像配置")]
        public bool enablePortrait = true;
        public CharacterAlignment defaultPortraitAlignment = CharacterAlignment.Left;

        [Header("动画配置")]
        public float defaultFadeDuration = 0.3f;
        public float optionShowDelay = 0.1f;  // 每个选项显示的延迟

        // 当前对话框数据
        private DialogEventData _currentData;
        private List<ChoiceOption> _options = new();
        private readonly List<RectTransform> _optionButtons = new();
        private readonly List<ChoiceButtonAnimator> _optionAnimators = new();

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
            // 淡出动画
            StartCoroutine(CloseAnimation());
        }

        private IEnumerator CloseAnimation()
        {
            if (_fadeEffect != null)
            {
                yield return _fadeEffect.FadeOut();
            }

            ClearOptions();
            _currentData = null;
            Close();
            gameObject.SetActive(false);
            onDialogClosed?.Invoke();
        }
        #endregion

        #region Public API

        /// <summary>
        /// 显示对话对话框（增强版）
        /// </summary>
        public void ShowDialog(DialogEventData data)
        {
            _currentData = data;
            _options = data.options ?? new List<ChoiceOption>();

            // 初始化动画组件
            if (_fadeEffect != null)
            {
                _fadeEffect.SetImmediate(0f);
                _fadeEffect.FadeIn();
            }

            this.Open();
        }

        /// <summary>
        /// 显示选择对话框（兼容旧API）
        /// </summary>
        public void ShowSelectionDialog(string title, string description, List<ChoiceOption> options, Action<ChoiceOption> onSelected = null)
        {
            var data = new DialogEventData
            {
                title = title,
                description = description,
                options = options,
                onOptionSelected = onSelected,
                animationType = DialogAnimationType.Typewriter
            };

            ShowDialog(data);
        }

        /// <summary>
        /// 显示角色对话
        /// </summary>
        public void ShowCharacterDialog(
            string speakerId,
            string speakerName,
            Sprite portrait,
            string text,
            List<ChoiceOption> options = null,
            Action<ChoiceOption> onSelected = null)
        {
            var portraitData = new CharacterPortraitData
            {
                characterId = speakerId,
                displayName = speakerName,
                portrait = portrait,
                alignment = defaultPortraitAlignment
            };

            _currentData = new DialogEventData
            {
                speakerId = speakerId,
                speakerName = speakerName,
                portrait = portrait,
                description = text,
                options = options ?? new List<ChoiceOption>(),
                onOptionSelected = onSelected,
                animationType = DialogAnimationType.Typewriter
            };

            _options = _currentData.options;

            // 显示头像
            if (_characterPortrait != null && enablePortrait)
            {
                _characterPortrait.Show(portraitData);
            }

            // 开始淡入
            if (_fadeEffect != null)
            {
                _fadeEffect.SetImmediate(0f);
                StartCoroutine(ShowDialogSequence());
            }
            else
            {
                this.Open();
                RefreshUI();
            }
        }

        private IEnumerator ShowDialogSequence()
        {
            // 等待淡入完成
            if (_fadeEffect != null)
            {
                yield return _fadeEffect.FadeIn();
            }

            this.Open();
            RefreshUI();
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

            ShowSelectionDialog(title, description, options, (option) =>
            {
                if (option.optionId == "confirm")
                    onConfirm?.Invoke();
                else
                    onCancel?.Invoke();
            });
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
            StartCoroutine(CloseAnimation());
        }

        /// <summary>
        /// 跳过当前打字动画
        /// </summary>
        public void SkipTyping()
        {
            if (_typewriter != null && _typewriter.isTyping)
            {
                _typewriter.Skip();
            }
        }

        /// <summary>
        /// 设置打字速度
        /// </summary>
        public void SetTypingSpeed(float charsPerSecond)
        {
            if (_typewriter != null)
            {
                _typewriter.charsPerSecond = charsPerSecond;
            }
        }

        #endregion

        #region Private Methods

        private void RefreshUI()
        {
            if (_currentData == null) return;

            // 设置标题
            if (_titleText != null)
                _titleText.text = _currentData.title;

            // 设置描述（打字机效果）
            if (_descriptionText != null)
            {
                if (_typewriter != null && !string.IsNullOrEmpty(_currentData.description))
                {
                    _typewriter.StartTyping(_currentData.description, () =>
                    {
                        // 打字完成后显示选项
                        ShowOptionsWithAnimation();
                    });
                }
                else
                {
                    _descriptionText.text = _currentData.description;
                    ShowOptionsWithAnimation();
                }
            }
        }

        private void ShowOptionsWithAnimation()
        {
            BuildOptionButtons();

            // 选项动画
            if (_optionAnimators.Count > 0 && optionShowDelay > 0)
            {
                StartCoroutine(AnimateOptionsIn());
            }
        }

        private IEnumerator AnimateOptionsIn()
        {
            for (int i = 0; i < _optionAnimators.Count; i++)
            {
                if (_optionButtons[i] != null)
                {
                    _optionButtons[i].localScale = Vector3.zero;
                }
            }

            for (int i = 0; i < _optionAnimators.Count; i++)
            {
                yield return new WaitForSeconds(optionShowDelay);

                if (_optionButtons[i] != null)
                {
                    var animator = _optionAnimators[i];
                    if (animator != null)
                    {
                        // 缩放动画
                        StartCoroutine(ScaleInOption(_optionButtons[i]));
                    }
                }
            }
        }

        private IEnumerator ScaleInOption(RectTransform rt)
        {
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = Vector3.zero;
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            rt.localScale = targetScale;
        }

        private void BuildOptionButtons()
        {
            ClearOptions();

            if (_options == null || _options.Count == 0) return;

            foreach (var option in _options)
            {
                var buttonRT = CreateOptionButton(option);
                _optionButtons.Add(buttonRT);
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

            // 获取或添加动画组件
            var animator = buttonRT.GetComponent<ChoiceButtonAnimator>();
            if (animator == null)
            {
                animator = buttonRT.gameObject.AddComponent<ChoiceButtonAnimator>();
            }
            animator.button = buttonRT.GetComponentInChildren<Button>();
            animator.buttonText = buttonText;
            animator.backgroundImage = background;
            _optionAnimators.Add(animator);

            // 添加事件
            var button = buttonRT.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnOptionClicked(option));

                // 添加悬停事件
                AddHoverEvents(buttonRT.gameObject, animator);

                button.interactable = option.isEnabled;
                animator.SetEnabled(option.isEnabled);
            }

            return buttonRT;
        }

        private void AddHoverEvents(GameObject go, ChoiceButtonAnimator animator)
        {
            var eventTrigger = go.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = go.AddComponent<EventTrigger>();
            }

            // 悬停进入
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => { animator.OnHoverEnter(); });
            eventTrigger.triggers.Add(enterEntry);

            // 悬停退出
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => { animator.OnHoverExit(); });
            eventTrigger.triggers.Add(exitEntry);

            // 选择
            var selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            selectEntry.callback.AddListener((data) => { animator.OnSelect(); });
            eventTrigger.triggers.Add(selectEntry);

            // 取消选择
            var deselectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
            deselectEntry.callback.AddListener((data) => { animator.OnDeselect(); });
            eventTrigger.triggers.Add(deselectEntry);
        }

        private Color GetChoiceTypeColor(ChoiceType type)
        {
            return type switch
            {
                ChoiceType.Confirm => new Color(0.2f, 0.8f, 0.2f, 0.3f),
                ChoiceType.Cancel => new Color(0.8f, 0.2f, 0.2f, 0.3f),
                ChoiceType.Danger => new Color(0.9f, 0.5f, 0.1f, 0.3f),
                ChoiceType.Special => new Color(0.2f, 0.5f, 0.9f, 0.3f),
                _ => Color.white
            };
        }

        private void ClearOptions()
        {
            _optionsList.Clear();
            _optionButtons.Clear();
            _optionAnimators.Clear();
        }

        private void OnOptionClicked(ChoiceOption option)
        {
            if (!option.isEnabled) return;

            _currentData.selectedOption = option;
            onOptionSelected?.Invoke(option);
            _currentData.onOptionSelected?.Invoke(option);

            // 选中动画
            StartCoroutine(CloseAnimation());
        }

        private void OnCloseButtonClicked()
        {
            CloseDialog();
        }

        private string GetLocationDisplayName(Location location)
        {
            if (string.IsNullOrEmpty(location.locationName))
                return location.id;

            var parts = location.locationName.Split('.');
            return parts.Length > 0 ? parts[^1] : location.locationName;
        }

        #endregion
    }
}