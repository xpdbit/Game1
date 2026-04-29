using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Game1.Modules.Achievement;

namespace Game1
{
    /// <summary>
    /// 成就UI组件
    /// 继承BaseUIPanel以支持UIManager状态管理
    /// </summary>
    public class UIAchievementPanel : BaseUIPanel
    {
        public override string panelId => "AchievementPanel";

        [Header("UI列表组件")]
        public UIListItems _achievementList;
        public Button _closeButton;

        [Header("分类标签页")]
        public Button _allTab;
        public Button _explorationTab;
        public Button _combatTab;
        public Button _collectionTab;
        public Button _teamTab;
        public Button _specialTab;

        [Header("统计信息")]
        public UIText _totalUnlockedText;

        [Header("设置")]
        public bool _autoRefreshOnEvent = true;

        private List<UIAchievementItem> _items = new();
        private Dictionary<string, UIAchievementItem> _itemsById = new();
        private Action<AchievementEventData> _onUnlocked;
        private Action<AchievementEventData> _onProgressUpdated;
        private AchievementCategory? _currentCategory;

        #region Unity Lifecycle
        private void Awake()
        {
            if (_achievementList == null)
                _achievementList = GetComponent<UIListItems>();

            _onUnlocked = OnAchievementUnlocked;
            _onProgressUpdated = OnProgressUpdated;

            // 绑定关闭按钮
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() =>
                {
                    Close();
                });
            }

            // 绑定分类标签页
            BindCategoryTab(_allTab, null);
            BindCategoryTab(_explorationTab, AchievementCategory.Exploration);
            BindCategoryTab(_combatTab, AchievementCategory.Combat);
            BindCategoryTab(_collectionTab, AchievementCategory.Collection);
            BindCategoryTab(_teamTab, AchievementCategory.Team);
            BindCategoryTab(_specialTab, AchievementCategory.Special);
        }

        private void OnEnable()
        {
            if (_autoRefreshOnEvent)
            {
                AchievementManager.SubscribeUnlocked(_onUnlocked);
                AchievementManager.SubscribeProgressUpdated(_onProgressUpdated);
            }
            Refresh();
        }

        private void OnDisable()
        {
            AchievementManager.UnsubscribeUnlocked(_onUnlocked);
            AchievementManager.UnsubscribeProgressUpdated(_onProgressUpdated);
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
        /// 刷新UI（从AchievementManager同步数据）
        /// </summary>
        public void Refresh()
        {
            Clear();

            // 根据当前分类获取成就列表
            List<AchievementUIData> achievements;
            if (_currentCategory.HasValue)
            {
                achievements = AchievementManager.GetByCategory(_currentCategory.Value);
            }
            else
            {
                achievements = AchievementManager.GetAllAchievements();
            }

            // 按解锁状态排序（已解锁在前），然后按进度排序
            achievements.Sort((a, b) =>
            {
                if (a.isUnlocked != b.isUnlocked)
                    return a.isUnlocked ? -1 : 1;
                return b.progress.CompareTo(a.progress);
            });

            foreach (var achievement in achievements)
            {
                // 隐藏未解锁的隐藏成就
                if (achievement.isHidden && !achievement.isUnlocked)
                    continue;

                Append(achievement);
            }

            UpdateUnlockedCount();
        }

        /// <summary>
        /// 添加成就到UI
        /// </summary>
        public UIAchievementItem Append(AchievementUIData data)
        {
            if (data == null)
                return null;

            // 检查是否已存在
            if (_itemsById.ContainsKey(data.templateId))
            {
                var existing = _itemsById[data.templateId];
                existing.UpdateUI(data);
                return existing;
            }

            // 添加新项
            if (_achievementList == null)
                return null;

            var result = _achievementList.AddItem($"Achievement_{data.templateId}");
            var item = new UIAchievementItem
            {
                data = data,
                listItemRect = result.rectTransform
            };

            _items.Add(item);
            _itemsById[data.templateId] = item;
            item.UpdateUI(data);

            return item;
        }

        /// <summary>
        /// 移除所有成就显示
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _itemsById.Clear();
            if (_achievementList != null)
                _achievementList.Clear();
        }

        /// <summary>
        /// 更新解锁计数显示
        /// </summary>
        public void UpdateUnlockedCount()
        {
            if (_totalUnlockedText == null)
                return;

            int unlocked = AchievementManager.GetTotalUnlockedCount();
            int total = AchievementManager.GetAllAchievements().Count;
            _totalUnlockedText.text = $"{unlocked} / {total}";
        }

        #endregion

        #region Category Filtering

        /// <summary>
        /// 绑定分类标签页按钮
        /// </summary>
        private void BindCategoryTab(Button button, AchievementCategory? category)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                SetCategory(category);
            });
        }

        /// <summary>
        /// 设置当前分类
        /// </summary>
        public void SetCategory(AchievementCategory? category)
        {
            _currentCategory = category;
            Refresh();
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// 响应成就解锁事件
        /// </summary>
        private void OnAchievementUnlocked(AchievementEventData data)
        {
            if (data == null)
                return;

            // 如果是当前显示的成就，刷新UI
            if (_itemsById.TryGetValue(data.achievementId, out var item))
            {
                item.UpdateUI();
            }

            UpdateUnlockedCount();
        }

        /// <summary>
        /// 响应进度更新事件
        /// </summary>
        private void OnProgressUpdated(AchievementEventData data)
        {
            if (data == null)
                return;

            // 如果是当前显示的成就，刷新UI
            if (_itemsById.TryGetValue(data.achievementId, out var item))
            {
                item.UpdateUI();
            }
        }

        #endregion
    }

    /// <summary>
    /// 成就行UI组件
    /// </summary>
    public class UIAchievementItem
    {
        public AchievementUIData data;           // 成就数据
        public RectTransform listItemRect;       // 列表项RectTransform

        // UI组件缓存
        private Button _button;
        private UIText _nameText;
        private UIText _descText;
        private UIText _progressText;
        private Image _iconImage;
        private Image _backgroundImage;
        private Image _progressBarImage;
        private Image _unlockedIcon;

        // UI组件路径（相对于listItemRect）
        private Button button => _button ??= listItemRect.Find("Button")?.GetComponent<Button>();
        private UIText nameText => _nameText ??= listItemRect.Find("Button/NameText")?.GetComponentInChildren<UIText>();
        private UIText descText => _descText ??= listItemRect.Find("Button/DescText")?.GetComponentInChildren<UIText>();
        private UIText progressText => _progressText ??= listItemRect.Find("Button/ProgressText")?.GetComponentInChildren<UIText>();
        private Image iconImage => _iconImage ??= listItemRect.Find("Button/Icon")?.GetComponent<Image>();
        private Image backgroundImage => _backgroundImage ??= listItemRect.Find("Button/Background")?.GetComponent<Image>();
        private Image progressBarImage => _progressBarImage ??= listItemRect.Find("Button/ProgressBar")?.GetComponent<Image>();
        private Image unlockedIcon => _unlockedIcon ??= listItemRect.Find("Button/UnlockedIcon")?.GetComponent<Image>();

        /// <summary>
        /// 更新UI显示
        /// </summary>
        public void UpdateUI(AchievementUIData newData = null)
        {
            if (newData != null)
                data = newData;

            if (data == null)
                return;

            listItemRect.name = data.displayName;

            if (nameText != null)
                nameText.text = data.displayName;

            if (descText != null)
                descText.text = data.description;

            // 更新进度显示
            if (progressText != null)
            {
                if (data.isUnlocked)
                {
                    progressText.text = "已解锁";
                }
                else
                {
                    progressText.text = $"{data.currentValue} / {data.targetValue}";
                }
            }

            // 更新进度条
            if (progressBarImage != null)
            {
                progressBarImage.fillAmount = data.progress;
            }

            // 更新解锁状态视觉
            UpdateUnlockedVisual();
        }

        /// <summary>
        /// 更新解锁状态视觉
        /// </summary>
        private void UpdateUnlockedVisual()
        {
            if (data == null)
                return;

            if (backgroundImage != null)
            {
                if (data.isUnlocked)
                {
                    // 已解锁：金色高亮
                    backgroundImage.color = new Color(1f, 0.84f, 0f, 0.3f);
                }
                else
                {
                    // 未解锁：默认白色
                    backgroundImage.color = Color.white;
                }
            }

            if (unlockedIcon != null)
            {
                unlockedIcon.gameObject.SetActive(data.isUnlocked);
            }

            if (progressBarImage != null)
            {
                progressBarImage.gameObject.SetActive(!data.isUnlocked);
            }
        }

        /// <summary>
        /// 获取成就模板ID
        /// </summary>
        public string templateId => data?.templateId;

        /// <summary>
        /// 是否已解锁
        /// </summary>
        public bool isUnlocked => data?.isUnlocked ?? false;

        /// <summary>
        /// 获取进度百分比 (0-1)
        /// </summary>
        public float progress => data?.progress ?? 0f;
    }
}