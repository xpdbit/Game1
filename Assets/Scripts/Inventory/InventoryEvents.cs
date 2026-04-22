using System;
using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 背包事件类型
    /// </summary>
    public enum InventoryEventType
    {
        ItemAdded,       // 物品添加成功
        ItemRemoved,     // 物品移除成功
        ItemUpdated,     // 物品数量更新
        InventoryCleared, // 背包清空
        CapacityChanged   // 容量变化
    }

    /// <summary>
    /// 背包事件数据
    /// </summary>
    [Serializable]
    public class InventoryEventData
    {
        public InventoryEventType eventType;
        public string templateId;
        public int instanceId;
        public int amount;
        public object extraData;

        public InventoryEventData() { }

        public InventoryEventData(InventoryEventType eventType, string templateId, int instanceId, int amount, object extraData = null)
        {
            this.eventType = eventType;
            this.templateId = templateId;
            this.instanceId = instanceId;
            this.amount = amount;
            this.extraData = extraData;
        }

        public static InventoryEventData ItemAdded(string templateId, int instanceId, int amount)
            => new InventoryEventData(InventoryEventType.ItemAdded, templateId, instanceId, amount);

        public static InventoryEventData ItemRemoved(string templateId, int instanceId, int amount)
            => new InventoryEventData(InventoryEventType.ItemRemoved, templateId, instanceId, amount);

        public static InventoryEventData ItemUpdated(string templateId, int instanceId, int amount)
            => new InventoryEventData(InventoryEventType.ItemUpdated, templateId, instanceId, amount);

        public static InventoryEventData InventoryCleared()
            => new InventoryEventData(InventoryEventType.InventoryCleared, null, 0, 0);

        public static InventoryEventData CapacityChanged()
            => new InventoryEventData(InventoryEventType.CapacityChanged, null, 0, 0);
    }

    /// <summary>
    /// 背包事件订阅者接口
    /// </summary>
    public interface IInventoryEventSubscriber
    {
        void OnInventoryEvent(InventoryEventData data);
    }

    /// <summary>
    /// 背包事件总线
    /// 负责管理事件订阅和发布
    /// </summary>
    public class InventoryEventBus
    {
        private static InventoryEventBus _instance;
        public static InventoryEventBus instance => _instance ??= new InventoryEventBus();

        private readonly Dictionary<InventoryEventType, List<IInventoryEventSubscriber>> _subscribers = new();
        private readonly object _lock = new();

        private InventoryEventBus() { }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(InventoryEventType eventType, IInventoryEventSubscriber subscriber)
        {
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType] = new List<IInventoryEventSubscriber>();
                }
                if (!_subscribers[eventType].Contains(subscriber))
                {
                    _subscribers[eventType].Add(subscriber);
                }
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe(InventoryEventType eventType, IInventoryEventSubscriber subscriber)
        {
            lock (_lock)
            {
                if (_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType].Remove(subscriber);
                }
            }
        }

        /// <summary>
        /// 取消订阅所有事件
        /// </summary>
        public void UnsubscribeAll(IInventoryEventSubscriber subscriber)
        {
            lock (_lock)
            {
                foreach (var kvp in _subscribers)
                {
                    kvp.Value.Remove(subscriber);
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish(InventoryEventData data)
        {
            List<IInventoryEventSubscriber> subscribersCopy;
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(data.eventType))
                    return;
                subscribersCopy = new List<IInventoryEventSubscriber>(_subscribers[data.eventType]);
            }

            foreach (var subscriber in subscribersCopy)
            {
                try
                {
                    subscriber.OnInventoryEvent(data);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }
}