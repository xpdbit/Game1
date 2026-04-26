using System;

namespace Game1.Core.EventBus
{
    /// <summary>
    /// 事件类型枚举
    /// </summary>
    public enum EventType
    {
        None,
        GoldChanged,
        LevelUp,
        TravelStarted,
        TravelCompleted,
        ModuleActivated,
        ModuleDeactivated,
        EventTriggered,
        SaveCompleted,
        LoadCompleted,
    }

    /// <summary>
    /// 事件数据
    /// </summary>
    [Serializable]
    public class GameEvent
    {
        public EventType type;
        public object sender;
        public object data;

        public GameEvent(EventType type, object sender = null, object data = null)
        {
            this.type = type;
            this.sender = sender;
            this.data = data;
        }
    }

    /// <summary>
    /// 事件订阅者接口
    /// </summary>
    public interface IEventSubscriber
    {
        void OnEvent(GameEvent e);
    }

    /// <summary>
    /// 事件总线接口 (用于VContainer DI)
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe(EventType eventType, IEventSubscriber subscriber);

        /// <summary>
        /// 取消订阅
        /// </summary>
        void Unsubscribe(EventType eventType, IEventSubscriber subscriber);

        /// <summary>
        /// 发布事件 (同步)
        /// </summary>
        void Publish(GameEvent e);

        /// <summary>
        /// 发布事件 (异步)
        /// </summary>
        void PublishAsync(GameEvent e);
    }
}
