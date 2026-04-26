using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TextAlignmentOptions = TMPro.TextAlignmentOptions;

namespace Game1.UI.Map
{
    /// <summary>
    /// 地图路径显示UI组件 V2 - 增强版
    /// 支持完整路径显示、当前位置、动态路径动画和触摸选择
    /// </summary>
    public class UIMapPathV2 : MonoBehaviour
    {
        #region Serializable Fields

        [Header("路径画布")]
        [SerializeField] private RectTransform _pathContainer;
        [SerializeField] private RectTransform _pathLineTemplate;
        [SerializeField] private float _nodeSpacing = 120f;
        [SerializeField] private float _nodeSize = 60f;

        [Header("节点图标")]
        [SerializeField] private Sprite _iconStart;
        [SerializeField] private Sprite _iconCity;
        [SerializeField] private Sprite _iconWilderness;
        [SerializeField] private Sprite _iconMarket;
        [SerializeField] private Sprite _iconDungeon;
        [SerializeField] private Sprite _iconBoss;
        [SerializeField] private Sprite _iconGoal;
        [SerializeField] private Sprite _iconCurrent;
        [SerializeField] private Sprite _iconUnexplored;

        [Header("路径线条")]
        [SerializeField] private Color _lineExploredColor = new Color(0.2f, 0.8f, 0.3f, 0.8f);
        [SerializeField] private Color _lineCurrentColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _lineUnexploredColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        [SerializeField] private float _lineWidth = 4f;

        [Header("UI引用")]
        [SerializeField] private UIText _titleText;
        [SerializeField] private UIText _currentLocationText;
        [SerializeField] private UIListItems _pathChoicesList;
        [SerializeField] private RectTransform _choiceItemPrefab;
        [SerializeField] private Button _closeButton;

        [Header("动画设置")]
        [SerializeField] private float _pathAnimationDuration = 0.5f;
        [SerializeField] private float _nodePulseInterval = 1.5f;
        [SerializeField] private AnimationCurve _nodePulseCurve;

        [Header("触摸设置")]
        [SerializeField] private float _touchRadius = 40f;

        #endregion

        #region Private Fields

        private List<PathNodeUI> _pathNodes = new();
        private List<Location> _currentChoices = new();
        private Action<Location> _onPathSelected;
        private Action _onClosed;
        private WorldMap _worldMap;
        private int _currentIndex = 0;
        private bool _isAnimating = false;

        // 动画相关
        private float _pathAnimationProgress = 0f;
        private float _nodePulseTimer = 0f;
        private Vector3 _currentNodeBaseScale = Vector3.one;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializePathLineTemplate();
            InitializeCloseButton();
        }

        private void Update()
        {
            UpdatePathAnimation();
            UpdateNodePulse();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 显示地图路径
        /// </summary>
        public void ShowMapPath(WorldMap worldMap, List<Location> choices, Action<Location> onSelected, Action onClosed = null)
        {
            _worldMap = worldMap;
            _currentChoices = choices ?? new List<Location>();
            _onPathSelected = onSelected;
            _onClosed = onClosed;
            _currentIndex = worldMap?.currentNodeIndex ?? 0;

            BuildCompletePath();
            BuildChoiceButtons();

            UpdateTitle();
            UpdateCurrentLocationText();

            gameObject.SetActive(true);
            StartPathAnimation();
        }

        /// <summary>
        /// 隐藏地图路径
        /// </summary>
        public void HideMapPath()
        {
            ClearPath();
            _pathChoicesList?.Clear();
            _currentChoices.Clear();
            _onPathSelected = null;
            _onClosed = null;
            _worldMap = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 更新路径（旅行中调用）
        /// </summary>
        public void UpdatePath(int currentNodeIndex, List<Location> choices)
        {
            _currentIndex = currentNodeIndex;
            _currentChoices = choices ?? new List<Location>();

            RefreshPathDisplay();
            BuildChoiceButtons();
            UpdateCurrentLocationText();
        }

        #endregion

        #region Private Methods - Initialization

        private void InitializePathLineTemplate()
        {
            if (_pathLineTemplate != null)
            {
                _pathLineTemplate.gameObject.SetActive(false);
            }
        }

        private void InitializeCloseButton()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        #endregion

        #region Private Methods - Path Building

        private void BuildCompletePath()
        {
            ClearPath();

            if (_worldMap == null || _pathContainer == null) return;

            int totalNodes = _worldMap.totalNodes;
            float containerWidth = _pathContainer.rect.width;
            float startX = Mathf.Max(_nodeSpacing, (containerWidth - (totalNodes - 1) * _nodeSpacing) / 2);

            for (int i = 0; i < totalNodes; i++)
            {
                var location = _worldMap.GetLocationByIndex(i);
                if (location == null) continue;

                var nodeUI = CreatePathNode(location, i, startX + i * _nodeSpacing);
                _pathNodes.Add(nodeUI);

                // 创建连线
                if (i > 0)
                {
                    CreatePathLine(_pathNodes[i - 1].rectTransform, nodeUI.rectTransform, i <= _currentIndex);
                }
            }
        }

        private PathNodeUI CreatePathNode(Location location, int index, float xPos)
        {
            var nodeObj = new GameObject($"Node_{index}_{location.type}");
            nodeObj.transform.SetParent(_pathContainer);
            nodeObj.layer = gameObject.layer;

            var rt = nodeObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_nodeSize, _nodeSize);
            rt.anchoredPosition = new Vector2(xPos - (index - _currentIndex) * 10f, 0); // 偏移让当前节点居中

            // 添加图像组件
            var image = nodeObj.AddComponent<Image>();
            image.raycastTarget = true;

            // 设置图标
            Sprite icon = GetIconForLocationType(location.type, index);
            image.sprite = icon;
            image.color = GetNodeColor(location.type, index);

            // 添加脉冲动画组件
            var pulseAnim = nodeObj.AddComponent<PathNodePulse>();
            pulseAnim.Initialize(_nodePulseCurve, _nodePulseInterval);

            // 添加碰撞器用于触摸
            var eventTrigger = nodeObj.AddComponent<EventTrigger>();

            // 添加点击事件
            AddClickTrigger(nodeObj, () => OnNodeClicked(location));

            // 添加路径节点UI组件
            var nodeUI = nodeObj.AddComponent<PathNodeUI>();
            nodeUI.Initialize(location, index, rt, image);

            // 添加工具提示
            AddTooltip(nodeObj, location);

            return nodeUI;
        }

        private void CreatePathLine(RectTransform from, RectTransform to, bool isExplored)
        {
            if (_pathLineTemplate == null) return;

            var lineObj = Instantiate(_pathLineTemplate, _pathContainer);
            lineObj.gameObject.SetActive(true);

            float length = Vector3.Distance(from.position, to.position);
            float angle = Mathf.Atan2(to.position.y - from.position.y, to.position.x - from.position.x) * Mathf.Rad2Deg;

            var rt = lineObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(length, _lineWidth);
            rt.position = from.position;
            rt.rotation = Quaternion.Euler(0, 0, angle);

            var image = lineObj.GetComponent<Image>();
            image.color = isExplored ? _lineExploredColor : _lineUnexploredColor;
        }

        private Sprite GetIconForLocationType(LocationType type, int index)
        {
            bool isCurrentNode = (index == _currentIndex);
            bool isExplored = (index <= (_worldMap?.maxNodeIndex ?? 0));

            if (isCurrentNode)
                return _iconCurrent;
            if (!isExplored)
                return _iconUnexplored;

            return type switch
            {
                LocationType.Start => _iconStart,
                LocationType.City => _iconCity,
                LocationType.Wilderness => _iconWilderness,
                LocationType.Market => _iconMarket,
                LocationType.Dungeon => _iconDungeon,
                LocationType.Boss => _iconBoss,
                LocationType.Goal => _iconGoal,
                _ => _iconUnexplored
            };
        }

        private Color GetNodeColor(LocationType type, int index)
        {
            bool isCurrentNode = (index == _currentIndex);
            bool isExplored = (index <= (_worldMap?.maxNodeIndex ?? 0));

            if (isCurrentNode)
                return Color.white;

            if (!isExplored)
                return new Color(0.6f, 0.6f, 0.6f, 0.5f);

            float alpha = isExplored ? 1f : 0.4f;

            return type switch
            {
                LocationType.Start => new Color(0.3f, 0.9f, 0.3f, alpha),
                LocationType.City => new Color(0.2f, 0.5f, 0.9f, alpha),
                LocationType.Wilderness => new Color(0.4f, 0.7f, 0.3f, alpha),
                LocationType.Market => new Color(0.9f, 0.8f, 0.2f, alpha),
                LocationType.Dungeon => new Color(0.8f, 0.2f, 0.2f, alpha),
                LocationType.Boss => new Color(0.9f, 0.3f, 0.1f, alpha),
                LocationType.Goal => new Color(0.7f, 0.2f, 0.9f, alpha),
                _ => new Color(0.7f, 0.7f, 0.7f, alpha)
            };
        }

        #endregion

        #region Private Methods - Choice Buttons

        private void BuildChoiceButtons()
        {
            _pathChoicesList?.Clear();

            foreach (var choice in _currentChoices)
            {
                CreateChoiceButton(choice);
            }
        }

        private void CreateChoiceButton(Location location)
        {
            if (_pathChoicesList == null) return;

            var result = _pathChoicesList.AddItem($"choice_{location.id}");
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
                typeText.text = GetLocationTypeIcon(location.type) + " " + location.type.ToString();
                typeText.color = GetLocationTypeColor(location.type);
            }

            // 设置距离/时间
            var distanceText = rt.Find("DistanceText")?.GetComponent<TMP_Text>();
            if (distanceText != null)
            {
                distanceText.text = $"⏱ {location.travelTime:F1}秒";
            }

            // 设置事件标记
            var eventMarker = rt.Find("EventMarker")?.gameObject;
            if (eventMarker != null)
            {
                eventMarker.SetActive(location.hasEvent);
            }

            // 添加点击事件
            var button = rt.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChoiceSelected(location));
            }

            // 添加触摸/点击事件到整个条目
            AddClickTrigger(rt.gameObject, () => OnChoiceSelected(location));
        }

        private string GetLocationDisplayName(Location location)
        {
            if (string.IsNullOrEmpty(location.locationName))
                return location.id;

            var parts = location.locationName.Split('.');
            return parts.Length > 0 ? parts[^1] : location.locationName;
        }

        private string GetLocationTypeIcon(LocationType type)
        {
            return type switch
            {
                LocationType.Start => "🏠",
                LocationType.City => "🏛️",
                LocationType.Wilderness => "🌲",
                LocationType.Market => "🏪",
                LocationType.Dungeon => "⚔️",
                LocationType.Boss => "💀",
                LocationType.Goal => "🎯",
                _ => "📍"
            };
        }

        private Color GetLocationTypeColor(LocationType type)
        {
            return type switch
            {
                LocationType.Start => new Color(0.2f, 0.8f, 0.2f),
                LocationType.City => new Color(0.2f, 0.5f, 0.9f),
                LocationType.Wilderness => new Color(0.4f, 0.7f, 0.3f),
                LocationType.Market => new Color(0.9f, 0.8f, 0.2f),
                LocationType.Dungeon => new Color(0.8f, 0.2f, 0.2f),
                LocationType.Boss => new Color(0.9f, 0.3f, 0.1f),
                LocationType.Goal => new Color(0.7f, 0.2f, 0.9f),
                _ => Color.white
            };
        }

        #endregion

        #region Private Methods - Interaction

        private void AddClickTrigger(GameObject target, Action callback)
        {
            var eventTrigger = target.GetComponent<EventTrigger>();
            if (eventTrigger == null)
                eventTrigger = target.AddComponent<EventTrigger>();

            // 鼠标点击
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener((data) => { callback?.Invoke(); });
            eventTrigger.triggers.Add(clickEntry);

            // 触摸支持
            var touchEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            touchEntry.callback.AddListener((data) => { callback?.Invoke(); });
            eventTrigger.triggers.Add(touchEntry);
        }

        private void AddTooltip(GameObject target, Location location)
        {
            // 简单的Tooltip实现
            var tooltip = target.AddComponent<PathNodeTooltip>();
            tooltip.Initialize(location);
        }

        private void OnNodeClicked(Location location)
        {
            // 节点点击效果
            Debug.Log($"[UIMapPathV2] 节点点击: {location.id}");
        }

        private void OnChoiceSelected(Location location)
        {
            if (_isAnimating) return;

            _onPathSelected?.Invoke(location);
            HideMapPath();
        }

        private void OnCloseClicked()
        {
            _onClosed?.Invoke();
            HideMapPath();
        }

        #endregion

        #region Private Methods - Animation

        private void StartPathAnimation()
        {
            _isAnimating = true;
            _pathAnimationProgress = 0f;
        }

        private void UpdatePathAnimation()
        {
            if (!_isAnimating) return;

            _pathAnimationProgress += Time.deltaTime / _pathAnimationDuration;

            if (_pathAnimationProgress >= 1f)
            {
                _pathAnimationProgress = 1f;
                _isAnimating = false;
                CompletePathAnimation();
            }

            // 更新路径节点位置插值
            float animProgress = _pathAnimationCurve.Evaluate(_pathAnimationProgress);
            UpdatePathAnimationProgress(animProgress);
        }

        private void UpdatePathAnimationProgress(float progress)
        {
            if (_worldMap == null) return;

            float centerOffset = (_currentIndex - _worldMap.totalNodes / 2f) * _nodeSpacing;

            for (int i = 0; i < _pathNodes.Count; i++)
            {
                var node = _pathNodes[i];
                float targetX = i * _nodeSpacing - centerOffset;
                float animatedX = Mathf.Lerp(0, targetX, progress);

                node.rectTransform.anchoredPosition = new Vector2(animatedX, 0);

                // 节点入场动画
                float nodeDelay = (float)i / _pathNodes.Count * 0.3f;
                float nodeProgress = Mathf.Clamp01((progress - nodeDelay) / (1f - nodeDelay));
                float scale = _pathAnimationCurve.Evaluate(nodeProgress);
                node.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void CompletePathAnimation()
        {
            // 确保最终位置正确
            UpdatePathAnimationProgress(1f);
        }

        private void UpdateNodePulse()
        {
            _nodePulseTimer += Time.deltaTime;
            float pulsePhase = (_nodePulseTimer % _nodePulseInterval) / _nodePulseInterval;

            // 找到当前节点并应用脉冲
            for (int i = 0; i < _pathNodes.Count; i++)
            {
                if (_pathNodes[i].nodeIndex == _currentIndex)
                {
                    float pulse = _nodePulseCurve.Evaluate(pulsePhase);
                    float scale = 1f + pulse * 0.15f;
                    _pathNodes[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private void RefreshPathDisplay()
        {
            if (_worldMap == null) return;

            for (int i = 0; i < _pathNodes.Count; i++)
            {
                var node = _pathNodes[i];
                var location = _worldMap.GetLocationByIndex(i);
                if (location == null) continue;

                // 更新图标
                node.image.sprite = GetIconForLocationType(location.type, i);
                node.image.color = GetNodeColor(location.type, i);

                // 更新连线颜色
                bool isExplored = i <= _worldMap.maxNodeIndex;
                var lineColor = isExplored ? _lineExploredColor : _lineUnexploredColor;
                // Note: 连线颜色需要单独追踪，这里简化处理
            }
        }

        private void ClearPath()
        {
            foreach (var node in _pathNodes)
            {
                if (node != null && node.gameObject != null)
                {
                    Destroy(node.gameObject);
                }
            }
            _pathNodes.Clear();

            // 清除连线（路径容器的子对象除了模板）
            if (_pathContainer != null)
            {
                foreach (Transform child in _pathContainer)
                {
                    if (child != null && child != _pathLineTemplate && child.gameObject.activeSelf)
                    {
                        // 保留非节点对象（可能是连线）
                        if (!child.name.StartsWith("Node_"))
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
            }
        }

        private void UpdateTitle()
        {
            if (_titleText != null)
            {
                int currentNode = _worldMap?.currentNodeIndex ?? 0;
                int totalNodes = _worldMap?.totalNodes ?? 0;
                _titleText.text = $"旅途 {currentNode + 1} / {totalNodes}";
            }
        }

        private void UpdateCurrentLocationText()
        {
            if (_currentLocationText != null && _worldMap != null)
            {
                var currentLoc = _worldMap.currentLocation;
                if (currentLoc != null)
                {
                    string icon = GetLocationTypeIcon(currentLoc.type);
                    string name = GetLocationDisplayName(currentLoc);
                    _currentLocationText.text = $"{icon} {name}";

                    // 如果有事件，显示事件标记
                    if (currentLoc.hasEvent)
                    {
                        _currentLocationText.text += " ⚡";
                    }
                }
            }
        }

        #endregion

        #region Animation Curve

        [SerializeField] private AnimationCurve _pathAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion
    }

    #region Helper Components

    /// <summary>
    /// 路径节点UI数据
    /// </summary>
    public class PathNodeUI : MonoBehaviour
    {
        public Location location { get; private set; }
        public int nodeIndex { get; private set; }
        public RectTransform rectTransform { get; private set; }
        public Image image { get; private set; }

        public void Initialize(Location loc, int index, RectTransform rt, Image img)
        {
            location = loc;
            nodeIndex = index;
            rectTransform = rt;
            image = img;
        }
    }

    /// <summary>
    /// 路径节点脉冲动画组件
    /// </summary>
    public class PathNodePulse : MonoBehaviour
    {
        private AnimationCurve _curve;
        private float _interval;
        private float _timer;
        private Vector3 _baseScale;

        public void Initialize(AnimationCurve curve, float interval)
        {
            _curve = curve;
            _interval = interval;
            _timer = 0f;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float phase = (_timer % _interval) / _interval;

            if (_curve != null)
            {
                float pulse = _curve.Evaluate(phase);
                float scale = 1f + pulse * 0.1f;
                transform.localScale = _baseScale * scale;
            }
        }
    }

    /// <summary>
    /// 路径节点工具提示
    /// </summary>
    public class PathNodeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Location _location;
        private GameObject _tooltipObj;
        private TMP_Text _tooltipText;

        public void Initialize(Location location)
        {
            _location = location;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (_tooltipObj == null)
            {
                _tooltipObj = new GameObject("Tooltip");
                _tooltipObj.transform.SetParent(transform);
                _tooltipObj.layer = gameObject.layer;

                var canvas = GameObject.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    _tooltipObj.transform.SetParent(canvas.transform);
                }

                var rt = _tooltipObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 60);
                rt.anchoredPosition = new Vector2(0, 50);

                var bg = _tooltipObj.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

                _tooltipText = _tooltipObj.AddComponent<TMP_Text>();
                _tooltipText.fontSize = 12;
                _tooltipText.color = Color.white;
                _tooltipText.alignment = TextAlignmentOptions.Center;
            }

            UpdateTooltipText();
            _tooltipObj.SetActive(true);
        }

        private void UpdateTooltipText()
        {
            if (_tooltipText != null && _location != null)
            {
                _tooltipText.text = $"{_location.type}\n";
                _tooltipText.text += $"奖励: {_location.baseReward}\n";
                if (_location.hasEvent)
                {
                    _tooltipText.text += $"⚡ 事件: {_location.eventId}";
                }
            }
        }

        private void HideTooltip()
        {
            if (_tooltipObj != null)
            {
                _tooltipObj.SetActive(false);
            }
        }
    }

    #endregion
}