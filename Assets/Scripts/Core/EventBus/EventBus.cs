using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
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
    /// 事件订阅者
    /// </summary>
    public interface IEventSubscriber
    {
        void OnEvent(GameEvent e);
    }

    /// <summary>
    /// 事件总线 - 发布订阅模式
    /// 用于模块间解耦通信
    /// </summary>
    public class EventBus
    {
        private static EventBus _instance;
        public static EventBus instance => _instance ??= new EventBus();

        private readonly Dictionary<EventType, List<IEventSubscriber>> _subscribers = new();

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(EventType eventType, IEventSubscriber subscriber)
        {
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<IEventSubscriber>();
            }
            if (!_subscribers[eventType].Contains(subscriber))
            {
                _subscribers[eventType].Add(subscriber);
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe(EventType eventType, IEventSubscriber subscriber)
        {
            if (_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType].Remove(subscriber);
            }
        }

        /// <summary>
        /// 发布事件 (同步)
        /// </summary>
        public void Publish(GameEvent e)
        {
            if (_subscribers.ContainsKey(e.type))
            {
                foreach (var subscriber in _subscribers[e.type])
                {
                    try
                    {
                        subscriber.OnEvent(e);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Event handler error for {e.type}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 发布事件 (泛型便捷方法)
        /// </summary>
        public void Publish<T>(EventType eventType, T sender, object data = null)
        {
            Publish(new GameEvent(eventType, sender, data));
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }

        private static void Debug_Log(object message)
        {
            UnityEngine.Debug.Log($"[EventBus] {message}");
        }
    }

    /// <summary>
    /// 事件订阅者的抽象基类 (可选)
    /// </summary>
    public abstract class EventSubscriberBase : IEventSubscriber
    {
        protected readonly List<EventType> _subscribedEvents = new();

        public virtual void OnEvent(GameEvent e)
        {
            // 子类重写处理具体事件
        }

        public void RegisterSelf()
        {
            foreach (var eventType in _subscribedEvents)
            {
                EventBus.instance.Subscribe(eventType, this);
            }
        }

        public void UnregisterSelf()
        {
            foreach (var eventType in _subscribedEvents)
            {
                EventBus.instance.Unsubscribe(eventType, this);
            }
        }
    }
}
