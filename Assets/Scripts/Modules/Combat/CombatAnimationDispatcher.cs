using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 战斗动画事件类型
    /// </summary>
    public enum CombatAnimationEventType
    {
        None,
        PlayerAttack,
        PlayerHit,
        PlayerCrit,
        PlayerDodge,
        EnemyAttack,
        EnemyHit,
        EnemyCrit,
        EnemyDodge,
        PlayerVictory,
        PlayerDefeat,
        Death
    }

    /// <summary>
    /// 战斗动画事件数据
    /// </summary>
    [Serializable]
    public class CombatAnimationEvent
    {
        public CombatAnimationEventType eventType;
        public string attackerName;
        public string defenderName;
        public int damage;
        public int defenderHpAfter;
        public bool isCritical;
        public float timestamp;
        public Vector3 worldPosition;

        public CombatAnimationEvent() { }

        public CombatAnimationEvent(CombatAnimationEventType type, string attacker, string defender, int dmg, int hpAfter, bool crit, Vector3 worldPos = default)
        {
            eventType = type;
            attackerName = attacker;
            defenderName = defender;
            damage = dmg;
            defenderHpAfter = hpAfter;
            isCritical = crit;
            timestamp = Time.time;
            worldPosition = worldPos;
        }
    }

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

        public static Color NormalColor = new Color(1f, 0.8f, 0.2f);
        public static Color CritColor = new Color(1f, 0.2f, 0.2f);
        public static Color DodgeColor = new Color(0.4f, 0.8f, 1f);
    }

    /// <summary>
    /// 单个伤害数字弹出项
    /// </summary>
    public class DamagePopupItem
    {
        public GameObject go;
        public TextMeshProUGUI text;
        public float lifetime;
        public float maxLifetime;
        public float speed;
        public Vector3 direction;
        public float scaleMultiplier;

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
            float alpha = Mathf.Clamp01(lifetime / maxLifetime);
            text.alpha = alpha;

            // 缩放动画（暴击时放大效果）
            if (scaleMultiplier > 1f)
            {
                float scale = 1f + (scaleMultiplier - 1f) * alpha;
                go.transform.localScale = Vector3.one * scale;
            }
        }

        public bool IsAlive => go != null && go.activeSelf && lifetime > 0f;
    }

    /// <summary>
    /// 伤害数字弹出管理器
    /// </summary>
    public class DamagePopupManager
    {
        #region Singleton
        private static DamagePopupManager _instance;
        public static DamagePopupManager instance => _instance ??= new DamagePopupManager();
        #endregion

        private const int POOL_SIZE = 20;
        private const string POPUP_PREFAB_PATH = "UI/Prefabs/DamagePopup";

        private Queue<DamagePopupItem> _popupPool = new();
        private List<DamagePopupItem> _activePopups = new();
        private GameObject _poolContainer;
        private Canvas _canvas;
        private Camera _worldCamera;
        private GameObject _popupPrefab;

        public void Initialize(Canvas canvas, Camera worldCamera)
        {
            _canvas = canvas;
            _worldCamera = worldCamera ?? Camera.main;

            var go = new GameObject("[DamagePopupPool]");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsFirstSibling();
            _poolContainer = go;

            CreatePool();
            Debug.Log("[DamagePopupManager] Initialized with pool size: " + POOL_SIZE);
        }

        private void CreatePool()
        {
            _popupPrefab = Resources.Load<GameObject>(POPUP_PREFAB_PATH);

            if (_popupPrefab == null)
            {
                Debug.LogWarning("[DamagePopupManager] Popup prefab not found, creating default");
                _popupPrefab = CreateDefaultPrefab();
            }

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
                maxLifetime = 1.5f,
                speed = 50f,
                direction = Vector3.up,
                scaleMultiplier = 1f
            };
        }

        private GameObject CreateDefaultPrefab()
        {
            var go = new GameObject("DamagePopupPrefab");
            var rect = go.AddComponent<RectTransform>();
            var text = go.AddComponent<TextMeshProUGUI>();

            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DamagePopupData.NormalColor;
            text.outlineColor = new Color(0, 0, 0, 0.5f);
            text.outlineWidth = 0.2f;
            text.fontStyle = FontStyles.Bold;

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

            DamagePopupItem popup;
            if (_popupPool.Count > 0)
            {
                popup = _popupPool.Dequeue();
            }
            else
            {
                popup = CreatePopupItem();
            }

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

            string displayText;
            Color textColor;
            if (data.isDodge)
            {
                displayText = "闪避";
                textColor = DamagePopupData.DodgeColor;
                popup.scaleMultiplier = 1f;
            }
            else if (data.isCritical)
            {
                displayText = $"暴击! {data.damage}";
                textColor = DamagePopupData.CritColor;
                popup.scaleMultiplier = 1.5f;
            }
            else
            {
                displayText = data.damage.ToString();
                textColor = DamagePopupData.NormalColor;
                popup.scaleMultiplier = 1f;
            }

            popup.text.text = displayText;
            popup.text.color = textColor;
            popup.text.alpha = 1f;
            popup.maxLifetime = data.lifetime > 0 ? data.lifetime : 1.5f;
            popup.lifetime = popup.maxLifetime;
            popup.speed = 50f;
            popup.direction = Vector3.up;
            popup.go.transform.localScale = Vector3.one;

            _activePopups.Add(popup);
        }

        /// <summary>
        /// 显示战斗日志中的伤害
        /// </summary>
        public void ShowFromCombatLog(CombatLogEntry log, bool isPlayerAttacking, string playerName, string enemyName, Vector3 worldPos)
        {
            string targetName = isPlayerAttacking ? enemyName : playerName;

            var data = new DamagePopupData
            {
                targetName = targetName,
                damage = log.damageDealt,
                isCritical = log.wasCritical,
                isDodge = log.damageDealt == 0,
                position = worldPos,
                lifetime = 1.5f
            };

            ShowDamage(data);
        }

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

        public int GetActiveCount() => _activePopups.Count;

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
    /// 战斗动画中断状态
    /// </summary>
    public enum AnimationInterruptState
    {
        None,
        WaitingForResume,
        Interrupted
    }

    /// <summary>
    /// 动画中断上下文
    /// </summary>
    [Serializable]
    public class AnimationInterruptContext
    {
        public CombatAnimationEvent interruptedEvent;
        public AnimationInterruptState state;
        public float interruptedAt;
        public List<CombatAnimationEvent> pendingEvents = new();
        public int resumeIndex;

        public AnimationInterruptContext()
        {
            state = AnimationInterruptState.None;
            interruptedAt = 0f;
            resumeIndex = 0;
        }

        public void Clear()
        {
            interruptedEvent = null;
            state = AnimationInterruptState.None;
            interruptedAt = 0f;
            pendingEvents.Clear();
            resumeIndex = 0;
        }
    }

    /// <summary>
    /// 战斗动画事件监听器接口
    /// </summary>
    public interface ICombatAnimationListener
    {
        void OnCombatAnimationEvent(CombatAnimationEvent e);
    }

    /// <summary>
    /// 战斗动画事件调度器
    /// 管理战斗动画事件的派发和监听
    /// </summary>
    public class CombatAnimationDispatcher
    {
        #region Singleton
        private static CombatAnimationDispatcher _instance;
        public static CombatAnimationDispatcher instance => _instance ??= new CombatAnimationDispatcher();
        #endregion

        private List<ICombatAnimationListener> _listeners = new();
        private Queue<CombatAnimationEvent> _eventQueue = new();
        private bool _isDispatching = false;

        // 动画中断控制
        private AnimationInterruptContext _interruptContext = new();
        private bool _animationEnabled = true;

        // 回调委托
        public Action<CombatAnimationEvent> onDamagePopup;
        public Action<CombatAnimationEvent> onCritEffect;
        public Action<CombatAnimationEvent> onDeathAnimation;

        /// <summary>
        /// 添加监听器
        /// </summary>
        public void AddListener(ICombatAnimationListener listener)
        {
            if (listener == null) return;
            if (_listeners.Contains(listener))
            {
                Debug.LogWarning("[CombatAnimationDispatcher] Listener already registered");
                return;
            }
            _listeners.Add(listener);
            Debug.Log($"[CombatAnimationDispatcher] Added listener: {listener.GetType().Name}");
        }

        /// <summary>
        /// 移除监听器
        /// </summary>
        public void RemoveListener(ICombatAnimationListener listener)
        {
            if (listener == null) return;
            if (!_listeners.Contains(listener))
            {
                Debug.LogWarning("[CombatAnimationDispatcher] Listener not found");
                return;
            }
            _listeners.Remove(listener);
            Debug.Log($"[CombatAnimationDispatcher] Removed listener: {listener.GetType().Name}");
        }

        /// <summary>
        /// 清空所有监听器
        /// </summary>
        public void ClearListeners()
        {
            _listeners.Clear();
            Debug.Log("[CombatAnimationDispatcher] Cleared all listeners");
        }

        /// <summary>
        /// 派发战斗动画事件
        /// </summary>
        public void Dispatch(CombatAnimationEvent e)
        {
            if (e == null)
            {
                Debug.LogWarning("[CombatAnimationDispatcher] Cannot dispatch null event");
                return;
            }

            _eventQueue.Enqueue(e);
            ProcessQueue();
        }

        /// <summary>
        /// 从战斗日志派发事件
        /// </summary>
        public void DispatchFromCombatLog(CombatLogEntry log, string playerName, string enemyName, bool isPlayerTurn, Vector3 worldPosition = default)
        {
            if (log == null) return;

            CombatAnimationEventType type;
            string attacker, defender;

            if (isPlayerTurn)
            {
                attacker = log.attackerName;
                defender = log.defenderName;

                if (log.damageDealt == 0)
                {
                    type = CombatAnimationEventType.EnemyDodge;
                }
                else if (log.wasCritical)
                {
                    type = CombatAnimationEventType.PlayerCrit;
                }
                else
                {
                    type = CombatAnimationEventType.PlayerAttack;
                }
            }
            else
            {
                attacker = log.attackerName;
                defender = log.defenderName;

                if (log.damageDealt == 0)
                {
                    type = CombatAnimationEventType.PlayerDodge;
                }
                else if (log.wasCritical)
                {
                    type = CombatAnimationEventType.EnemyCrit;
                }
                else
                {
                    type = CombatAnimationEventType.EnemyAttack;
                }
            }

            var e = new CombatAnimationEvent(type, attacker, defender, log.damageDealt, log.defenderHpAfter, log.wasCritical, worldPosition);
            Dispatch(e);
        }

        /// <summary>
        /// 派发死亡动画事件
        /// </summary>
        public void DispatchDeath(string defenderName, Vector3 worldPosition)
        {
            var e = new CombatAnimationEvent(CombatAnimationEventType.Death, "", defenderName, 0, 0, false, worldPosition);
            Dispatch(e);
        }

        /// <summary>
        /// 派发战斗结果事件
        /// </summary>
        public void DispatchResult(bool playerVictory)
        {
            var type = playerVictory ? CombatAnimationEventType.PlayerVictory : CombatAnimationEventType.PlayerDefeat;
            var e = new CombatAnimationEvent(type, "", "", 0, 0, false);
            Dispatch(e);
        }

        /// <summary>
        /// 处理事件队列
        /// </summary>
        private void ProcessQueue()
        {
            if (_isDispatching) return;

            // 检查动画是否被中断
            if (_interruptContext.state == AnimationInterruptState.Interrupted)
            {
                // 保存待处理事件
                while (_eventQueue.Count > 0)
                {
                    _interruptContext.pendingEvents.Add(_eventQueue.Dequeue());
                }
                return;
            }

            _isDispatching = true;

            while (_eventQueue.Count > 0)
            {
                // 检查是否需要中断
                while (_interruptContext.state == AnimationInterruptState.WaitingForResume)
                {
                    _interruptContext.pendingEvents.Add(_eventQueue.Dequeue());
                    return;
                }

                var e = _eventQueue.Dequeue();

                // 触发回调（用于伤害数字和特效）
                TriggerCallbacks(e);

                NotifyListeners(e);
            }

            _isDispatching = false;
        }

        private void TriggerCallbacks(CombatAnimationEvent e)
        {
            // 伤害数字飘字
            if (e.eventType != CombatAnimationEventType.PlayerVictory &&
                e.eventType != CombatAnimationEventType.PlayerDefeat &&
                e.eventType != CombatAnimationEventType.None)
            {
                onDamagePopup?.Invoke(e);
            }

            // 暴击特效
            if (e.eventType == CombatAnimationEventType.PlayerCrit ||
                e.eventType == CombatAnimationEventType.EnemyCrit)
            {
                onCritEffect?.Invoke(e);
            }

            // 死亡动画
            if (e.eventType == CombatAnimationEventType.Death)
            {
                onDeathAnimation?.Invoke(e);
            }
        }

        private void NotifyListeners(CombatAnimationEvent e)
        {
            var listenersCopy = _listeners.ToArray();

            foreach (var listener in listenersCopy)
            {
                try
                {
                    listener.OnCombatAnimationEvent(e);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CombatAnimationDispatcher] Exception in listener {listener.GetType().Name}: {ex.Message}");
                }
            }

            Debug.Log($"[CombatAnimationDispatcher] Dispatched: {e.eventType} - {e.attackerName} -> {e.defenderName} (dmg:{e.damage}, crit:{e.isCritical})");
        }

        /// <summary>
        /// 中断当前动画
        /// </summary>
        public void InterruptAnimation()
        {
            if (_interruptContext.state == AnimationInterruptState.Interrupted) return;

            _interruptContext.state = AnimationInterruptState.Interrupted;
            _interruptContext.interruptedAt = Time.time;
            Debug.Log("[CombatAnimationDispatcher] Animation interrupted");
        }

        /// <summary>
        /// 恢复中断的动画，播放pending事件
        /// </summary>
        public void ResumeAnimation()
        {
            if (_interruptContext.state != AnimationInterruptState.Interrupted &&
                _interruptContext.state != AnimationInterruptState.WaitingForResume) return;

            Debug.Log($"[CombatAnimationDispatcher] Resuming animation, pending count: {_interruptContext.pendingEvents.Count}");

            _interruptContext.state = AnimationInterruptState.None;
            _isDispatching = false;

            // 处理所有pending事件
            foreach (var e in _interruptContext.pendingEvents)
            {
                _eventQueue.Enqueue(e);
            }
            _interruptContext.pendingEvents.Clear();

            ProcessQueue();
        }

        /// <summary>
        /// 设置动画启用状态
        /// </summary>
        public void SetAnimationEnabled(bool enabled)
        {
            _animationEnabled = enabled;
            if (enabled && _interruptContext.state == AnimationInterruptState.Interrupted)
            {
                ResumeAnimation();
            }
            else if (!enabled && _interruptContext.state == AnimationInterruptState.None && _eventQueue.Count > 0)
            {
                InterruptAnimation();
            }
        }

        /// <summary>
        /// 获取当前中断状态
        /// </summary>
        public AnimationInterruptState GetInterruptState() => _interruptContext.state;

        /// <summary>
        /// 获取当前队列中的事件数量
        /// </summary>
        public int GetQueueCount() => _eventQueue.Count + _interruptContext.pendingEvents.Count;

        /// <summary>
        /// 获取已注册监听器数量
        /// </summary>
        public int GetListenerCount() => _listeners.Count;

        /// <summary>
        /// 初始化伤害数字系统
        /// </summary>
        public void InitializeDamagePopup(Canvas canvas, Camera worldCamera)
        {
            DamagePopupManager.instance.Initialize(canvas, worldCamera);
            onDamagePopup += OnDamagePopup;
        }

        private void OnDamagePopup(CombatAnimationEvent e)
        {
            if (!_animationEnabled) return;

            var data = new DamagePopupData
            {
                targetName = e.defenderName,
                damage = e.damage,
                isCritical = e.isCritical,
                isDodge = e.eventType == CombatAnimationEventType.PlayerDodge || e.eventType == CombatAnimationEventType.EnemyDodge,
                position = e.worldPosition != default ? e.worldPosition : Vector3.zero,
                lifetime = e.isCritical ? 2f : 1.5f
            };

            DamagePopupManager.instance.ShowDamage(data);
        }

        /// <summary>
        /// 每帧更新（需要在MonoBehaviour的Update中调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            DamagePopupManager.instance.Update(deltaTime);
        }
    }

    /// <summary>
    /// MonoBehaviour监听器基类
    /// </summary>
    public abstract class CombatAnimationListenerBase : MonoBehaviour, ICombatAnimationListener
    {
        [Header("Combat Animation Listener")]
        [SerializeField] private bool _autoRegisterOnEnable = true;
        [SerializeField] private bool _autoUnregisterOnDisable = false;

        protected virtual void OnEnable()
        {
            if (_autoRegisterOnEnable)
            {
                CombatAnimationDispatcher.instance.AddListener(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (_autoUnregisterOnDisable)
            {
                CombatAnimationDispatcher.instance.RemoveListener(this);
            }
        }

        protected virtual void OnDestroy()
        {
            CombatAnimationDispatcher.instance.RemoveListener(this);
        }

        public abstract void OnCombatAnimationEvent(CombatAnimationEvent e);
    }
}