using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game1
{
    /// <summary>
    /// 地图路径显示UI组件
    /// 显示当前可选路径和方向选择
    /// </summary>
    public class UIMapPath : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private UIText _titleText;
        [SerializeField] private UIListItems _pathOptionsList;
        [SerializeField] private RectTransform _pathOptionPrefab;
        [SerializeField] private Button _closeButton;

        [Header("设置")]
        public int maxVisibleChoices = 3;

        private List<Location> _currentChoices = new();
        private Action<Location> _onPathSelected;
        private Action _onClosed;

        #region Unity Lifecycle
        private void Awake()
        {
            if (_pathOptionsList != null && _pathOptionPrefab != null)
            {
                _pathOptionsList.templateRT = _pathOptionPrefab;
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }
        #endregion

        #region Public API

        /// <summary>
        /// 显示路径选择
        /// </summary>
        public void ShowPathChoices(List<Location> choices, Action<Location> onSelected, Action onClosed = null)
        {
            _currentChoices = choices ?? new List<Location>();
            _onPathSelected = onSelected;
            _onClosed = onClosed;

            if (_titleText != null)
            {
                _titleText.text = "选择前进方向";
            }

            BuildPathOptions();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏路径选择
        /// </summary>
        public void HidePathChoices()
        {
            _pathOptionsList?.Clear();
            _currentChoices.Clear();
            _onPathSelected = null;
            _onClosed = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void BuildPathOptions()
        {
            _pathOptionsList?.Clear();

            int count = Mathf.Min(_currentChoices.Count, maxVisibleChoices);
            for (int i = 0; i < count; i++)
            {
                var location = _currentChoices[i];
                CreatePathOption(location, i);
            }
        }

        private void CreatePathOption(Location location, int index)
        {
            var optionId = $"path_{location.id}_{index}";

            if (_pathOptionsList == null) return;

            var result = _pathOptionsList.AddItem(optionId);

            var rt = result.rectTransform;

            // 设置名称
            var nameText = rt.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = GetLocationDisplayName(location);
            }

            // 设置类型标签
            var typeText = rt.Find("TypeText")?.GetComponent<TMP_Text>();
            if (typeText != null)
            {
                typeText.text = location.type.ToString();
                typeText.color = GetLocationTypeColor(location.type);
            }

            // 设置距离/时间
            var distanceText = rt.Find("DistanceText")?.GetComponent<TMP_Text>();
            if (distanceText != null)
            {
                distanceText.text = $"{location.travelTime:F1}秒";
            }

            // 添加点击事件
            var button = rt.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnPathOptionClicked(location));
            }
        }

        private string GetLocationDisplayName(Location location)
        {
            if (string.IsNullOrEmpty(location.locationName))
                return location.id;

            var parts = location.locationName.Split('.');
            return parts.Length > 0 ? parts[^1] : location.locationName;
        }

        private Color GetLocationTypeColor(LocationType type)
        {
            return type switch
            {
                LocationType.Start => new Color(0.2f, 0.8f, 0.2f),
                LocationType.City => new Color(0.2f, 0.5f, 0.9f),
                LocationType.Market => new Color(0.9f, 0.8f, 0.2f),
                LocationType.Dungeon => new Color(0.8f, 0.2f, 0.2f),
                LocationType.Boss => new Color(0.9f, 0.3f, 0.1f),
                LocationType.Goal => new Color(0.7f, 0.2f, 0.9f),
                _ => Color.white
            };
        }

        private void OnPathOptionClicked(Location location)
        {
            _onPathSelected?.Invoke(location);
            HidePathChoices();
        }

        private void OnCloseClicked()
        {
            _onClosed?.Invoke();
            HidePathChoices();
        }

        #endregion
    }

    /// <summary>
    /// 进度条UI扩展
    /// 用于显示旅行进度和里程碑
    /// </summary>
    public class UIProgressWithMilestone : MonoBehaviour
    {
        [SerializeField] private UIProgressBar _progressBar;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _milestoneText;
        [SerializeField] private GameObject _milestoneIndicator;

        private int _currentMilestone = 0;

        /// <summary>
        /// 更新进度显示
        /// </summary>
        public void UpdateProgress(int currentPoints, int pointsPerEvent)
        {
            float progress = (float)currentPoints / pointsPerEvent;
            _progressBar?.UpdateBarImmediate(progress);

            if (_progressText != null)
            {
                _progressText.text = $"{currentPoints} / {pointsPerEvent}";
            }

            int milestone = currentPoints / pointsPerEvent;
            if (milestone > _currentMilestone)
            {
                _currentMilestone = milestone;
                OnMilestoneReached(milestone);
            }
        }

        /// <summary>
        /// 里程碑触发
        /// </summary>
        private void OnMilestoneReached(int milestone)
        {
            if (_milestoneIndicator != null)
            {
                // 闪烁效果
                _milestoneIndicator.SetActive(true);
            }

            if (_milestoneText != null)
            {
                _milestoneText.text = $"事件 #{milestone}";
            }
        }

        /// <summary>
        /// 隐藏里程碑指示
        /// </summary>
        public void HideMilestoneIndicator()
        {
            if (_milestoneIndicator != null)
            {
                _milestoneIndicator.SetActive(false);
            }
        }
    }
}
