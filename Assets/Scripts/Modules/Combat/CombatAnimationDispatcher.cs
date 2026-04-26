using System;
using System.Collections.Generic;
using UnityEngine;

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
        PlayerDefeat
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

        public CombatAnimationEvent() { }

        public CombatAnimationEvent(CombatAnimationEventType type, string attacker, string defender, int dmg, int hpAfter, bool crit)
        {
            eventType = type;
            attackerName = attacker;
            defenderName = defender;
            damage = dmg;
            defenderHpAfter = hpAfter;
            isCritical = crit;
            timestamp = Time.time;
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
    /// 使用观察者模式，支持多个监听器
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
        public void DispatchFromCombatLog(CombatLogEntry log, string playerName, string enemyName, bool isPlayerTurn)
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
                    type = CombatAnimationEventType.EnemyDodge; // 敌人闪避了玩家攻击
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
                    type = CombatAnimationEventType.PlayerDodge; // 玩家闪避了敌人攻击
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

            var e = new CombatAnimationEvent(type, attacker, defender, log.damageDealt, log.defenderHpAfter, log.wasCritical);
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
        /// 处理事件队列（防止在监听器回调中再次派发导致的问题）
        /// </summary>
        private void ProcessQueue()
        {
            if (_isDispatching) return;
            _isDispatching = true;

            while (_eventQueue.Count > 0)
            {
                var e = _eventQueue.Dequeue();
                NotifyListeners(e);
            }

            _isDispatching = false;
        }

        private void NotifyListeners(CombatAnimationEvent e)
        {
            // 复制列表避免在迭代中修改
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

            // 记录事件日志
            Debug.Log($"[CombatAnimationDispatcher] Dispatched: {e.eventType} - {e.attackerName} -> {e.defenderName} (dmg:{e.damage}, crit:{e.isCritical})");
        }

        /// <summary>
        /// 获取当前队列中的事件数量
        /// </summary>
        public int GetQueueCount() => _eventQueue.Count;

        /// <summary>
        /// 获取已注册监听器数量
        /// </summary>
        public int GetListenerCount() => _listeners.Count;
    }

    /// <summary>
    /// MonoBehaviour监听器基类
    /// 方便Unity组件订阅战斗动画事件
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