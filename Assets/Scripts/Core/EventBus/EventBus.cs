using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game1.Core.EventBus
{
    /// <summary>
    /// 事件总线 - 发布订阅模式
    /// 用于模块间解耦通信
    /// 实现IEventBus接口以支持VContainer DI
    /// </summary>
    public class EventBus : IEventBus
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
        /// 发布事件 (异步泛型便捷方法)
        /// </summary>
        public void PublishAsync<T>(EventType eventType, T sender, object data = null)
        {
            PublishAsync(new GameEvent(eventType, sender, data));
        }

        /// <summary>
        /// 发布事件 (异步可等待泛型便捷方法)
        /// </summary>
        public async UniTask PublishAsyncAwaitable<T>(EventType eventType, T sender, object data = null, CancellationToken cancellationToken = default)
        {
            await PublishAsyncAwaitable(new GameEvent(eventType, sender, data), cancellationToken);
        }

        /// <summary>
        /// 发布事件 (异步) - 真正异步实现，不阻塞主线程
        /// 使用UniTask.Void实现fire-and-forget异步发布
        /// </summary>
        public void PublishAsync(GameEvent e)
        {
            // 先获取订阅者列表的快照，避免异步执行时集合被修改
            if (!_subscribers.TryGetValue(e.type, out var subscriberList) || subscriberList.Count == 0)
            {
                return;
            }

            // 复制列表以确保线程安全和迭代安全
            var subscribersSnapshot = new List<IEventSubscriber>(subscriberList);

            // Fire-and-forget异步执行，不阻塞主线程
            UniTask.Void(async () =>
            {
                // 切换到主线程，确保Unity API调用安全
                await UniTask.SwitchToMainThread();

                // 使用PlayerLoopTiming.Update确保在下一帧执行
                // 这样不会阻塞当前的PublishAsync调用
                await UniTask.Yield(PlayerLoopTiming.Update, CancellationToken.None);

                try
                {
                    foreach (var subscriber in subscribersSnapshot)
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
                catch (Exception ex)
                {
                    Debug.LogError($"EventBus async publish error for {e.type}: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 发布事件 (异步) - 异步发布并等待所有订阅者处理完成
        /// 注意：这是等待所有处理器完成，而非立即返回
        /// </summary>
        public async UniTask PublishAsyncAwaitable(GameEvent e, CancellationToken cancellationToken = default)
        {
            if (!_subscribers.TryGetValue(e.type, out var subscriberList) || subscriberList.Count == 0)
            {
                return;
            }

            // 复制列表以确保线程安全和迭代安全
            var subscribersSnapshot = new List<IEventSubscriber>(subscriberList);

            // 切换到主线程
            await UniTask.SwitchToMainThread();

            // 等待下一帧
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            foreach (var subscriber in subscribersSnapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
