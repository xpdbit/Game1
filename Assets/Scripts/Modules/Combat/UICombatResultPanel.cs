using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game1.UI.Utils;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 伤害数字弹出数据
    /// </summary>
    [Serializable]
    public class DamagePopupData
    {
        public string targetName;
        public int damage;
        public bool isCritical;
        public bool isDodge;
        public Vector3 position;
        public float lifetime;
        public Color textColor;

        public static Color NormalColor = new Color(1f, 0.8f, 0.2f);      // 金色
        public static Color CritColor = new Color(1f, 0.2f, 0.2f);        // 红色
        public static Color DodgeColor = new Color(0.4f, 0.8f, 1f);       // 蓝色
    }

    /// <summary>
    /// 单个伤害数字弹出项
    /// </summary>
    public class DamagePopupItem
    {
        public GameObject go;
        public TextMeshProUGUI text;
        public float lifetime;
        public float speed;
        public Vector3 direction;

        public void Update(float deltaTime)
        {
            if (go == null || !go.activeSelf) return;

            lifetime -= deltaTime;
            if (lifetime <= 0f)
            {
                go.SetActive(false);
                return;
            }

            // 向上飘动
            var pos = go.transform.position;
            pos += direction * speed * deltaTime;
            go.transform.position = pos;

            // 淡出
            float alpha = lifetime / 1.5f; // 假设总生命周期1.5秒
            text.alpha = alpha;
        }

        public bool IsAlive => go != null && go.activeSelf && lifetime > 0f;
    }

    /// <summary>
    /// 伤害数字弹出管理器
    /// 使用对象池避免频繁Instantiate/Destroy
    /// </summary>
    public class DamagePopupManager
    {
        #region Singleton
        private static DamagePopupManager _instance;
        public static DamagePopupManager instance => _instance ??= new DamagePopupManager();
        #endregion

        private const int POOL_SIZE = 20;
        private const string POPUP_PREFAB_PATH = "UI/Prefabs/DamagePopup"; // 预设路径（需提前创建）

        private Queue<DamagePopupItem> _popupPool = new();
        private List<DamagePopupItem> _activePopups = new();
        private GameObject _poolContainer;
        private Canvas _canvas;
        private Camera _worldCamera;

        // 预设资源（如果不存在则动态创建）
        private GameObject _popupPrefab;

        public void Initialize(Canvas canvas, Camera worldCamera)
        {
            _canvas = canvas;
            _worldCamera = worldCamera ?? Camera.main;

            // 创建池容器
            var go = new GameObject("[DamagePopupPool]");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsFirstSibling(); // 放在最底层
            _poolContainer = go;

            // 初始化对象池
            CreatePool();

            Debug.Log("[DamagePopupManager] Initialized with pool size: " + POOL_SIZE);
        }

        private void CreatePool()
        {
            // 尝试从Resources加载预设
            _popupPrefab = Resources.Load<GameObject>(POPUP_PREFAB_PATH);

            if (_popupPrefab == null)
            {
                Debug.LogWarning("[DamagePopupManager] Popup prefab not found at " + POPUP_PREFAB_PATH + ", creating default");
                _popupPrefab = CreateDefaultPrefab();
            }

            // 创建池对象
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var item = CreatePopupItem();
                item.go.SetActive(false);
                _popupPool.Enqueue(item);
            }
        }

        private DamagePopupItem CreatePopupItem()
        {
            var go = UnityEngine.Object.Instantiate(_popupPrefab, _poolContainer.transform);
            go.name = "DamagePopup_" + _popupPool.Count;

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = go.AddComponent<TextMeshProUGUI>();
                text.fontSize = 24;
                text.alignment = TextAlignmentOptions.Center;
                // Note: Sorting for UI is handled at Canvas level, not individual text components
            }

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100, 40);
            }

            return new DamagePopupItem
            {
                go = go,
                text = text,
                lifetime = 0f,
                speed = 0f,
                direction = Vector3.up
            };
        }

        private GameObject CreateDefaultPrefab()
        {
            // 创建简单的默认预设
            var go = new GameObject("DamagePopupPrefab");
            var rect = go.AddComponent<RectTransform>();
            var text = go.AddComponent<TextMeshProUGUI>();

            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DamagePopupData.NormalColor;
            text.outlineColor = new Color(0, 0, 0, 0.5f);
            text.outlineWidth = 0.2f;
            text.fontStyle = FontStyles.Bold;

            // 添加Outline组件增强可见性
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.3f);
            outline.effectDistance = new Vector2(1, 1);

            return go;
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        public void ShowDamage(DamagePopupData data)
        {
            if (_canvas == null)
            {
                Debug.LogWarning("[DamagePopupManager] Canvas not initialized");
                return;
            }

            // 获取或创建popup
            DamagePopupItem popup;
            if (_popupPool.Count > 0)
            {
                popup = _popupPool.Dequeue();
            }
            else
            {
                // 池空，创建新的
                popup = CreatePopupItem();
            }

            // 设置位置（世界坐标转屏幕坐标，再转Canvas局部坐标）
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_worldCamera, data.position);
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                null,
                out localPos
            );

            popup.go.transform.localPosition = localPos;
            popup.go.SetActive(true);

            // 设置文本
            string displayText;
            Color textColor;
            if (data.isDodge)
            {
                displayText = "闪避";
                textColor = DamagePopupData.DodgeColor;
            }
            else if (data.isCritical)
            {
                displayText = $"暴击! {data.damage}";
                textColor = DamagePopupData.CritColor;
            }
            else
            {
                displayText = data.damage.ToString();
                textColor = DamagePopupData.NormalColor;
            }

            popup.text.text = displayText;
            popup.text.color = textColor;
            popup.text.alpha = 1f;

            // 设置动画参数
            popup.lifetime = data.lifetime > 0 ? data.lifetime : 1.5f;
            popup.speed = 50f; // 每秒上升50像素
            popup.direction = Vector3.up;

            // 添加到激活列表
            _activePopups.Add(popup);

            Debug.Log($"[DamagePopupManager] ShowDamage: {displayText} at {localPos}");
        }

        /// <summary>
        /// 显示战斗日志中的伤害
        /// </summary>
        public void ShowFromCombatLog(CombatLogEntry log, bool isPlayerAttacking, string playerName, string enemyName)
        {
            string targetName = isPlayerAttacking ? enemyName : playerName;

            var data = new DamagePopupData
            {
                targetName = targetName,
                damage = log.damageDealt,
                isCritical = log.wasCritical,
                isDodge = log.damageDealt == 0,
                position = Vector3.zero, // 将由调用方设置实际位置
                lifetime = 1.5f
            };

            ShowDamage(data);
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float deltaTime)
        {
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                var popup = _activePopups[i];
                popup.Update(deltaTime);

                if (!popup.IsAlive)
                {
                    _activePopups.RemoveAt(i);
                    popup.go.SetActive(false);
                    _popupPool.Enqueue(popup);
                }
            }
        }

        /// <summary>
        /// 获取当前活跃popup数量
        /// </summary>
        public int GetActiveCount() => _activePopups.Count;

        /// <summary>
        /// 清空所有活跃popup
        /// </summary>
        public void ClearAll()
        {
            foreach (var popup in _activePopups)
            {
                popup.go.SetActive(false);
                _popupPool.Enqueue(popup);
            }
            _activePopups.Clear();
        }
    }

    /// <summary>
    /// 战斗结果面板
    /// 显示战斗结果日志和奖励
    /// 继承BaseUIPanel，使用CombatAnimationDispatcher监听战斗动画事件
    /// </summary>
    public class UICombatResultPanel : CombatAnimationListenerBase
    {
        [Header("Combat Result Panel")]
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Button _closeButton;

        [Header("Combat Log")]
        [SerializeField] private Transform _logContainer;
        [SerializeField] private GameObject _logEntryPrefab;
        [SerializeField] private int _maxVisibleLogs = 10;

        [Header("Animation")]
        [SerializeField] private float _entryAnimationDelay = 0.3f;
        [SerializeField] private bool _autoCloseAfterVictory = true;
        [SerializeField] private float _autoCloseDelay = 3f;

        private int _currentLogIndex = 0;
        private float _logAnimationTimer = 0f;
        private bool _isAnimating = false;
        private bool _pendingClose = false;
        private float _pendingCloseTimer = 0f;

        private CombatResult _currentResult;
        private List<GameObject> _logEntries = new();

        protected void Awake()
        {
            // 默认初始状态下隐藏
            if (_contentPanel != null)
            {
                _contentPanel.SetActive(false);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // 注册关闭按钮
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        /// <summary>
        /// 显示战斗结果
        /// </summary>
        public void ShowCombatResult(CombatResult result)
        {
            if (result == null)
            {
                Debug.LogWarning("[UICombatResultPanel] ShowCombatResult: result is null");
                return;
            }

            _currentResult = result;
            _currentLogIndex = 0;
            _isAnimating = true;
            _pendingClose = false;

            // 清空旧日志
            ClearLogEntries();

            // 显示面板
            if (_contentPanel != null)
            {
                _contentPanel.SetActive(true);
            }

            // 设置标题
            if (_titleText != null)
            {
                _titleText.text = result.playerVictory ? "战斗胜利！" : "战斗失败...";
                _titleText.color = result.playerVictory ? new Color(1f, 0.8f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            }

            // 设置结果文本
            if (_resultText != null)
            {
                _resultText.text = result.endMessage;
            }

            // 设置奖励文本
            if (_rewardText != null && result.playerVictory)
            {
                _rewardText.text = $"获得: {result.goldReward} 金币";
                _rewardText.gameObject.SetActive(true);
            }
            else if (_rewardText != null)
            {
                _rewardText.gameObject.SetActive(false);
            }

            // 开始日志动画序列
            _logAnimationTimer = 0f;

            Debug.Log($"[UICombatResultPanel] ShowCombatResult: Victory={result.playerVictory}, LogCount={result.combatLog.Count}");
        }

        private void Update()
        {
            if (!_isAnimating) return;

            // 处理日志动画
            if (_currentResult != null && _currentLogIndex < _currentResult.combatLog.Count)
            {
                _logAnimationTimer += Time.deltaTime;

                // 每_entryAnimationDelay秒显示一条日志
                while (_logAnimationTimer >= _entryAnimationDelay && _currentLogIndex < _currentResult.combatLog.Count)
                {
                    ShowNextLogEntry();
                    _logAnimationTimer -= _entryAnimationDelay;
                }
            }

            // 处理自动关闭
            if (_pendingClose)
            {
                _pendingCloseTimer += Time.deltaTime;
                if (_pendingCloseTimer >= _autoCloseDelay)
                {
                    ClosePanel();
                }
            }
        }

        private void ShowNextLogEntry()
        {
            if (_currentResult == null || _currentLogIndex >= _currentResult.combatLog.Count) return;

            var log = _currentResult.combatLog[_currentLogIndex];

            // 创建日志条目
            GameObject entryObj;
            if (_logEntryPrefab != null && _logContainer != null)
            {
                entryObj = Instantiate(_logEntryPrefab, _logContainer);
            }
            else
            {
                entryObj = new GameObject("LogEntry_" + _currentLogIndex);
                entryObj.transform.SetParent(_logContainer);
            }

            // 设置文本
            var text = entryObj.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = entryObj.AddComponent<TextMeshProUGUI>();
                text.fontSize = 16;
                text.alignment = TextAlignmentOptions.Left;
            }

            // 格式化日志文本
            string logText = $"第{log.round}回合: ";
            if (log.damageDealt == 0)
            {
                logText += $"{log.defenderName}闪避了{log.attackerName}的攻击";
                text.color = new Color(0.4f, 0.8f, 1f); // 蓝色
            }
            else if (log.wasCritical)
            {
                logText += $"{log.attackerName}对{log.defenderName}造成暴击伤害 {log.damageDealt}! (HP: {log.defenderHpAfter})";
                text.color = new Color(1f, 0.2f, 0.2f); // 红色
            }
            else
            {
                logText += $"{log.attackerName}对{log.defenderName}造成 {log.damageDealt} 伤害 (HP: {log.defenderHpAfter})";
                text.color = new Color(1f, 0.95f, 0.8f); // 金色
            }

            text.text = logText;
            entryObj.SetActive(true);

            _logEntries.Add(entryObj);
            _currentLogIndex++;

            Debug.Log($"[UICombatResultPanel] ShowNextLogEntry: {logText}");
        }

        private void ClearLogEntries()
        {
            foreach (var entry in _logEntries)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            _logEntries.Clear();
        }

        /// <summary>
        /// 战斗动画事件处理
        /// </summary>
        public override void OnCombatAnimationEvent(CombatAnimationEvent e)
        {
            Debug.Log($"[UICombatResultPanel] OnCombatAnimationEvent: {e.eventType}, damage={e.damage}");

            // 如果需要显示伤害数字（由外部调用DamagePopupManager）
            // 这里主要用于UI状态同步
            switch (e.eventType)
            {
                case CombatAnimationEventType.PlayerCrit:
                case CombatAnimationEventType.EnemyCrit:
                    // 触发屏幕震动效果（预留）
                    break;

                case CombatAnimationEventType.PlayerDodge:
                case CombatAnimationEventType.EnemyDodge:
                    // 触发闪避特效（预留）
                    break;
            }
        }

        private void OnCloseButtonClicked()
        {
            ClosePanel();
        }

        private void ClosePanel()
        {
            _isAnimating = false;
            _pendingClose = false;
            _pendingCloseTimer = 0f;

            if (_contentPanel != null)
            {
                _contentPanel.SetActive(false);
            }

            // 通知战斗结束
            // EventBus可以在这里发布战斗结束事件

            Debug.Log("[UICombatResultPanel] ClosePanel");
        }

        /// <summary>
        /// 标记战斗结束，准备自动关闭
        /// </summary>
        public void PrepareAutoClose(bool autoClose)
        {
            if (autoClose && _autoCloseAfterVictory && _currentResult?.playerVictory == true)
            {
                _pendingClose = true;
                _pendingCloseTimer = 0f;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
            }
        }
    }
}