using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Game1.Events
{
    /// <summary>
    /// 事件树UI面板
    /// 集成EventTreeRunner与UISelectionDialog
    /// 显示事件树叙事和选择分支
    /// </summary>
    public class UIEventTreePanel : MonoBehaviour
    {
        #region Singleton
        private static UIEventTreePanel _instance;
        public static UIEventTreePanel instance => _instance;
        #endregion

        [Header("面板引用")]
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _narrativeText;
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _historyPanel;
        [SerializeField] private Transform _historyContainer;

        [Header("选项预设")]
        [SerializeField] private RectTransform _choiceButtonPrefab;
        [SerializeField] private float _choiceSpacing = 10f;
        [SerializeField] private RectTransform _historyItemPrefab;

        [Header("设置")]
        [SerializeField] private bool _autoShowOnTreeStart = true;
        [SerializeField] private float _typewriterSpeed = 30f; // 每秒字符数

        [Header("选项动画")]
        [SerializeField] private float _choiceAppearDelay = 0.1f;
        [SerializeField] private float _choiceAnimDuration = 0.3f;
        [SerializeField] private AnimationCurve _choiceAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 状态
        private EventTreeRunner _runner;
        private bool _isOpen = false;
        private bool _isTyping = false;
        private string _fullText = "";
        private float _typewriterTimer = 0f;
        private int _currentCharIndex = 0;
        private List<RectTransform> _choiceButtons = new();
        private float _choiceAnimTimer = 0f;
        private int _choicesAnimatedCount = 0;

        // 事件
        public event Action onPanelOpened;
        public event Action onPanelClosed;
        public event Action onTreeCompleted;

        // 历史记录
        private List<EventTreeRunner.EventTreeHistoryEntry> _eventHistory = new();
        private const int MaxHistoryDisplay = 20;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _runner = EventTreeRunner.instance;
            RegisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            // 打字机效果
            if (_isTyping && !string.IsNullOrEmpty(_fullText))
            {
                _typewriterTimer += Time.deltaTime;
                int charsToShow = Mathf.FloorToInt(_typewriterTimer * _typewriterSpeed);
                charsToShow = Mathf.Min(charsToShow, _fullText.Length);

                if (_narrativeText != null)
                {
                    _narrativeText.text = _fullText.Substring(0, charsToShow);
                }

                if (charsToShow >= _fullText.Length)
                {
                    _isTyping = false;
                    ShowChoicesWithAnimation();
                }
            }

            // 选项动画
            UpdateChoiceAnimations();
        }

        private void UpdateChoiceAnimations()
        {
            if (_choicesAnimatedCount >= _pendingChoices?.Count)
                return;

            _choiceAnimTimer += Time.deltaTime;

            // 检查是否应该显示下一个选项
            float delay = _choiceAppearDelay + (_choicesAnimatedCount * _choiceAnimDuration);

            if (_choiceAnimTimer >= delay && _choicesAnimatedCount < _pendingChoices.Count)
            {
                CreateChoiceButton(_pendingChoices[_choicesAnimatedCount], true);
                _choicesAnimatedCount++;
            }

            // 更新已有选项的动画
            float animProgress = _choiceAnimTimer / _choiceAnimDuration;
            animProgress = Mathf.Clamp01(animProgress);

            for (int i = 0; i < _choicesAnimatedCount && i < _choiceButtons.Count; i++)
            {
                var button = _choiceButtons[i];
                if (button != null && button.localScale.x < 0.99f)
                {
                    float scale = _choiceAnimCurve.Evaluate(animProgress);
                    button.localScale = Vector3.one * scale;
                    button.gameObject.SetActive(true);
                }
            }
        }

        #endregion

        #region Event Registration

        private void RegisterEvents()
        {
            if (_runner == null) return;

            _runner.onTreeStarted += OnTreeStarted;
            _runner.onNodeEntered += OnNodeEntered;
            _runner.onWaitingForChoice += OnWaitingForChoice;
            _runner.onTreeCompleted += OnTreeCompleted;
            _runner.onTreeCancelled += OnTreeCancelled;
        }

        private void UnregisterEvents()
        {
            if (_runner == null) return;

            _runner.onTreeStarted -= OnTreeStarted;
            _runner.onNodeEntered -= OnNodeEntered;
            _runner.onWaitingForChoice -= OnWaitingForChoice;
            _runner.onTreeCompleted -= OnTreeCompleted;
            _runner.onTreeCancelled -= OnTreeCancelled;
        }

        #endregion

        #region Event Handlers

        private void OnTreeStarted(EventTreeTemplate template)
        {
            if (!_autoShowOnTreeStart) return;

            Debug.Log($"[UIEventTreePanel] Tree started: {template.id}");
            OpenPanel();

            if (_titleText != null && template != null)
            {
                _titleText.text = template.name ?? template.id;
            }
        }

        private void OnNodeEntered(EventTreeNode node)
        {
            if (node == null) return;

            Debug.Log($"[UIEventTreePanel] Node entered: {node.id}, type={node.type}");
            ClearChoices();

            // 记录历史
            AddToHistory(node);

            // 显示节点内容（使用description作为叙事文本）
            if (!string.IsNullOrEmpty(node.description))
            {
                PlayNarrative(node.description);
            }
            else
            {
                // 没有叙事内容，直接显示选项
                _isTyping = false;
                if (_narrativeText != null)
                    _narrativeText.text = "";
                ShowChoicesWithAnimation();
            }
        }

        private void OnWaitingForChoice(List<EventTreeChoice> choices)
        {
            if (choices == null || choices.Count == 0) return;

            Debug.Log($"[UIEventTreePanel] Waiting for choice: {choices.Count} options");
            _pendingChoices = choices;

            // 如果没有打字机效果正在播放，立即显示选项
            if (!_isTyping)
            {
                ShowChoicesWithAnimation();
            }
        }

        private void OnTreeCompleted()
        {
            Debug.Log("[UIEventTreePanel] Tree completed");
            ClosePanel();
            onTreeCompleted?.Invoke();
        }

        private void OnTreeCancelled()
        {
            Debug.Log("[UIEventTreePanel] Tree cancelled");
            ClosePanel();
        }

        #endregion

        #region Panel Control

        public void OpenPanel()
        {
            if (_isOpen) return;

            _isOpen = true;
            if (_contentPanel != null)
                _contentPanel.SetActive(true);

            onPanelOpened?.Invoke();
            Debug.Log("[UIEventTreePanel] Panel opened");
        }

        public void ClosePanel()
        {
            if (!_isOpen) return;

            _isOpen = false;
            if (_contentPanel != null)
                _contentPanel.SetActive(false);

            ClearChoices();
            ClearHistoryUI();
            _isTyping = false;
            _pendingChoices = null;

            onPanelClosed?.Invoke();
            Debug.Log("[UIEventTreePanel] Panel closed");
        }

        #endregion

        #region Narrative & Choices

        private List<EventTreeChoice> _pendingChoices;

        /// <summary>
        /// 播放叙事文本（打字机效果）
        /// </summary>
        public void PlayNarrative(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (_narrativeText != null)
                    _narrativeText.text = "";
                ShowChoices();
                return;
            }

            _fullText = text;
            _currentCharIndex = 0;
            _typewriterTimer = 0f;
            _isTyping = true;

            if (_narrativeText != null)
                _narrativeText.text = "";

            Debug.Log($"[UIEventTreePanel] Playing narrative: {text.Substring(0, Mathf.Min(50, text.Length))}...");
        }

        /// <summary>
        /// 跳过当前叙事，直接显示选项
        /// </summary>
        public void SkipNarrative()
        {
            if (!_isTyping) return;

            _isTyping = false;
            if (_narrativeText != null)
                _narrativeText.text = _fullText;

            ShowChoicesWithAnimation();
        }

        /// <summary>
        /// 显示选项按钮
        /// </summary>
        public void ShowChoices()
        {
            if (_pendingChoices == null || _pendingChoices.Count == 0) return;

            ClearChoices();

            foreach (var choice in _pendingChoices)
            {
                CreateChoiceButton(choice, false);
            }
        }

        /// <summary>
        /// 显示选项按钮（带动画）
        /// </summary>
        public void ShowChoicesWithAnimation()
        {
            if (_pendingChoices == null || _pendingChoices.Count == 0) return;

            ClearChoices();
            _choicesAnimatedCount = 0;
            _choiceAnimTimer = 0f;

            // 立即显示第一个选项
            if (_pendingChoices.Count > 0)
            {
                CreateChoiceButton(_pendingChoices[0], true);
                _choicesAnimatedCount = 1;
            }
        }

        private void CreateChoiceButton(EventTreeChoice choice, bool animate)
        {
            if (_choiceButtonPrefab == null || _choicesContainer == null) return;

            var buttonObj = Instantiate(_choiceButtonPrefab, _choicesContainer);
            var buttonRT = buttonObj.GetComponent<RectTransform>();

            // 设置按钮文本
            var text = buttonRT.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = choice.text;
            }

            // 添加点击事件
            var button = buttonRT.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChoiceSelected(choice));
            }

            buttonObj.gameObject.SetActive(true);
            _choiceButtons.Add(buttonRT);

            // 如果需要动画，初始设置为透明/缩小
            if (animate)
            {
                buttonRT.localScale = Vector3.zero;
                buttonRT.gameObject.SetActive(false);
            }

            Debug.Log($"[UIEventTreePanel] Created choice button: {choice.text}");
        }

        private void ClearChoices()
        {
            foreach (var button in _choiceButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _choiceButtons.Clear();
        }

        private void OnChoiceSelected(EventTreeChoice choice)
        {
            if (choice == null) return;

            Debug.Log($"[UIEventTreePanel] Choice selected: {choice.id} - {choice.text}");

            // 通知EventTreeRunner
            _runner?.SelectChoice(choice.id);

            // 清空选项
            ClearChoices();
            _pendingChoices = null;
        }

        #endregion

        #region History Management

        /// <summary>
        /// 添加节点到历史记录
        /// </summary>
        private void AddToHistory(EventTreeNode node)
        {
            var entry = new EventTreeRunner.EventTreeHistoryEntry
            {
                nodeId = node.id,
                title = node.title,
                description = node.description,
                nodeType = node.type
            };

            _eventHistory.Add(entry);

            // 限制历史记录数量
            while (_eventHistory.Count > MaxHistoryDisplay)
            {
                _eventHistory.RemoveAt(0);
            }

            Debug.Log($"[UIEventTreePanel] Added to history: {node.id} ({node.title})");
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory()
        {
            _eventHistory.Clear();
            ClearHistoryUI();
            Debug.Log("[UIEventTreePanel] History cleared");
        }

        /// <summary>
        /// 显示历史记录面板
        /// </summary>
        public void ShowHistory()
        {
            if (_historyPanel != null)
                _historyPanel.SetActive(true);

            ClearHistoryUI();

            // 逆序显示（最新在前）
            for (int i = _eventHistory.Count - 1; i >= 0; i--)
            {
                CreateHistoryItem(_eventHistory[i]);
            }
        }

        /// <summary>
        /// 隐藏历史记录面板
        /// </summary>
        public void HideHistory()
        {
            if (_historyPanel != null)
                _historyPanel.SetActive(false);
        }

        private void CreateHistoryItem(EventTreeRunner.EventTreeHistoryEntry entry)
        {
            if (_historyItemPrefab == null || _historyContainer == null) return;

            var itemObj = Instantiate(_historyItemPrefab, _historyContainer);
            var text = itemObj.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = $"[{entry.nodeType}] {entry.title}";
            }
            itemObj.gameObject.SetActive(true);
        }

        private void ClearHistoryUI()
        {
            if (_historyContainer == null) return;

            foreach (Transform child in _historyContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 获取历史记录
        /// </summary>
        public IReadOnlyList<EventTreeRunner.EventTreeHistoryEntry> GetHistory()
        {
            return _eventHistory;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 开始事件树
        /// </summary>
        public void StartEventTree(string templateId)
        {
            if (_runner == null)
            {
                Debug.LogError("[UIEventTreePanel] EventTreeRunner not found");
                return;
            }

            _runner.StartTree(templateId);
        }

        /// <summary>
        /// 开始随机事件树
        /// </summary>
        public void StartRandomEventTree()
        {
            if (_runner == null)
            {
                Debug.LogError("[UIEventTreePanel] EventTreeRunner not found");
                return;
            }

            _runner.StartRandomTree();
        }

        /// <summary>
        /// 返回上一个节点
        /// </summary>
        public void GoBack()
        {
            _runner?.GoBack();
        }

        /// <summary>
        /// 取消当前事件树
        /// </summary>
        public void CancelTree()
        {
            _runner?.Cancel();
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public bool IsOpen => _isOpen;
        public bool IsRunning => _runner?.isRunning ?? false;

        #endregion
    }
}