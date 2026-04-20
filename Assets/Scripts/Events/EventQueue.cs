using System;
using System.Collections.Generic;

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
            // TODO: 根据离线时间生成积压事件
            // 例如：每X分钟可能触发一个事件
        }

        /// <summary>
        /// Tick - 检查是否需要生成新事件
        /// </summary>
        public void Tick(float deltaTime)
        {
            // TODO: 根据概率检测是否生成新随机事件
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

        public bool CanTrigger()
        {
            // TODO: 根据条件判断
            return true;
        }

        public EventResult Execute()
        {
            var result = new EventResult();

            // TODO: 战斗逻辑
            result.success = true;
            result.goldReward = 50;
            result.message = "击退了强盗，获得了50金币！";

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
            result.success = true;
            result.message = "交易完成！";
            return result;
        }
    }

    #endregion
}
