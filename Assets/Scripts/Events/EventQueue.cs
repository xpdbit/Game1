using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 游戏事件接口
    /// </summary>
    public interface IGameEvent
    {
        string eventId { get; }
        string title { get; }
        string description { get; }
        GameEventType eventType { get; }
        bool CanTrigger();
        EventResult Execute();
    }

    /// <summary>
    /// 事件结果
    /// </summary>
    [Serializable]
    public class EventResult
    {
        public bool success;
        public int goldReward;
        public int goldCost;
        public List<string> unlockedModuleIds = new();
        public List<string> removedModuleIds = new();
        public string message;
        public bool isGameOver;
    }

    /// <summary>
    /// 事件类型
    /// </summary>
    public enum GameEventType
    {
        Random,      // 随机事件
        Trade,       // 交易事件
        Combat,      // 战斗事件
        Discovery,   // 发现事件
        Mystery,     // 神秘事件
    }

    /// <summary>
    /// 事件队列 - 处理积压事件
    /// </summary>
    public class EventQueue
    {
        private Queue<IGameEvent> _pendingEvents = new();
        private List<IGameEvent> _eventHistory = new();
        private IGameEvent _currentEvent;

        public int pendingCount => _pendingEvents.Count;
        public bool hasCurrentEvent => _currentEvent != null;
        public IGameEvent currentEvent => _currentEvent;

        public event Action<IGameEvent> onEventTriggered;
        public event Action<EventResult> onEventCompleted;

        /// <summary>
        /// 添加事件到队列
        /// </summary>
        public void Enqueue(IGameEvent gameEvent)
        {
            _pendingEvents.Enqueue(gameEvent);
        }

        /// <summary>
        /// 处理下一个事件
        /// </summary>
        public EventResult ProcessNext()
        {
            if (_pendingEvents.Count == 0)
                return null;

            _currentEvent = _pendingEvents.Dequeue();
            _eventHistory.Add(_currentEvent);

            var result = _currentEvent.Execute();
            onEventTriggered?.Invoke(_currentEvent);
            onEventCompleted?.Invoke(result);

            return result;
        }

        /// <summary>
        /// 查看下一个事件（不处理）
        /// </summary>
        public IGameEvent PeekNext()
        {
            return _pendingEvents.Count > 0 ? _pendingEvents.Peek() : null;
        }

        /// <summary>
        /// 清空所有待处理事件
        /// </summary>
        public void Clear()
        {
            _pendingEvents.Clear();
            _currentEvent = null;
        }

/// <summary>
        /// 生成积压事件（上线时）
        /// </summary>
        public void GeneratePendingEvents(float offlineTime)
        {
            // 根据离线时间生成积压事件
            // 假设每60秒可能触发一个事件
            float eventInterval = 60f;
            int eventCount = Mathf.FloorToInt(offlineTime / eventInterval);

            for (int i = 0; i < eventCount; i++)
            {
                // 随机生成战斗事件
                var combatEvent = new CombatEvent
                {
                    enemyCount = UnityEngine.Random.Range(1, 4),
                    enemyStrength = UnityEngine.Random.Range(10, 50)
                };
                _pendingEvents.Enqueue(combatEvent);
            }

            Debug.Log($"[EventQueue] Generated {eventCount} pending events for {offlineTime}s offline time");
        }

        /// <summary>
        /// Tick - 检查是否需要生成新随机事件
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 1%概率生成随机事件（每tick约100ms，所以大约每10秒可能触发一次）
            float eventChance = 0.01f;
            if (UnityEngine.Random.value < eventChance)
            {
                var randomEvent = new CombatEvent
                {
                    enemyCount = UnityEngine.Random.Range(1, 3),
                    enemyStrength = UnityEngine.Random.Range(5, 30)
                };
                _pendingEvents.Enqueue(randomEvent);
                Debug.Log("[EventQueue] Random combat event triggered");
            }
        }

        /// <summary>
        /// 获取事件历史
        /// </summary>
        public List<IGameEvent> GetHistory()
        {
            return new List<IGameEvent>(_eventHistory);
        }
    }

    #region 具体事件实现示例

/// <summary>
        /// 战斗事件示例
        /// </summary>
        [Serializable]
        public class CombatEvent : IGameEvent
        {
            public string eventId => "combat_001";
            public string title => "遭遇战斗";
            public string description => "路上遇到了强盗！";
            public GameEventType eventType => GameEventType.Combat;

            public int enemyCount;
            public int enemyStrength;

            public virtual bool CanTrigger()
        {
            // TODO: 根据条件判断
            return true;
        }

        public virtual EventResult Execute()
        {
            var result = new EventResult();

            // 战斗逻辑：根据敌人数量和强度计算奖励
            int baseReward = 20;
            int rewardPerEnemy = 15;
            float strengthMultiplier = 1f + (enemyStrength / 50f);

            // 计算总奖励
            int totalReward = (int)((baseReward + enemyCount * rewardPerEnemy) * strengthMultiplier);

            result.success = true;
            result.goldReward = totalReward;
            result.message = $"击败了{enemyCount}个敌人（强度{enemyStrength}），获得了{totalReward}金币！";

            return result;
        }
    }

    /// <summary>
    /// 交易事件示例
    /// </summary>
    [Serializable]
    public class TradeEvent : IGameEvent
    {
        public string eventId => "trade_001";
        public string title => "路遇商队";
        public string description => "遇到了一支商队，可以进行交易。";
        public GameEventType eventType => GameEventType.Trade;

        public bool CanTrigger()
        {
            return true;
        }

        public EventResult Execute()
        {
            var result = new EventResult();

            // 交易逻辑：玩家消耗金币换取增益
            int tradeCost = 30;  // 基础交易成本
            float tradeBonus = 0.2f;  // 交易提供20%额外收益加成

            result.success = true;
            result.goldCost = tradeCost;
            result.message = $"与商队交易，消耗{tradeCost}金币，获得{tradeBonus * 100}%收益加成！";

            // 注意：实际的加成效果需要在事件完成后通过其他机制应用
            // 这里只返回结果，TravelManager或UI层负责应用具体效果

            return result;
        }
    }

    #endregion
}
