using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game1.Events;

namespace Game1.Modules.PendingEvent
{
    /// <summary>
    /// 积压事件设计——积压事件系统核心逻辑（单例非MonoBehaviour）
    /// </summary>
    public class PendingEventDesign
    {
        #region Singleton
        private static PendingEventDesign _instance;
        public static PendingEventDesign instance => _instance ??= new PendingEventDesign();
        #endregion

        #region Events
        public event Action onPendingEventChanged;
        #endregion

        #region Private Fields
        private readonly Dictionary<string, PendingEventData> _eventsById = new();
        private readonly List<PendingEventData> _events = new();
        private readonly RarityWeightConfig _rarityConfig = new();
        private int _eventIdCounter;
        private bool _initialized;
        #endregion

        #region Properties
        public int pendingCount => _events.Count(e => !e.isProcessed);
        public int totalCount => _events.Count;
        public bool hasPendingEvents => _events.Any(e => !e.isProcessed);
        #endregion

        #region Initialization
        public void Initialize()
        {
            if (_initialized) return;
            _eventIdCounter = 0;
            _initialized = true;
            Debug.Log("[PendingEventDesign] Initialized");
        }
        #endregion

        #region Generation
        /// <summary>
        /// 根据离线时间生成积压事件
        /// </summary>
        public void GeneratePendingEvents(float offlineSeconds)
        {
            if (!_initialized) Initialize();
            int eventCount = Mathf.FloorToInt(offlineSeconds / 120f); // 每2分钟1个事件
            eventCount = Mathf.Clamp(eventCount, 0, 50); // 单次最多50个

            for (int i = 0; i < eventCount; i++)
            {
                var rarity = _rarityConfig.RollRarity();
                var template = GetRandomTemplateByRarity(rarity);
                if (template == null) continue;

                int reward = PendingEventBrief.GetBaseGoldReward(rarity);
                long eventTime = DateTime.Now.Ticks
                    - (long)(offlineSeconds * (eventCount - i) / eventCount * TimeSpan.TicksPerSecond);

                var pendingEvent = new PendingEventData
                {
                    eventId = $"pending_{++_eventIdCounter}",
                    templateId = template.id,
                    rarity = rarity,
                    timestamp = eventTime,
                    offlineSeconds = offlineSeconds,
                    isProcessed = false,
                    goldReward = reward
                };

                _events.Add(pendingEvent);
                _eventsById[pendingEvent.eventId] = pendingEvent;
            }

            Debug.Log($"[PendingEventDesign] Generated {eventCount} pending events for {offlineSeconds:F1}s offline");
            onPendingEventChanged?.Invoke();
        }

        /// <summary>
        /// 根据稀有度获取随机事件模板
        /// Normal→Random/Trade，Rare→Combat/Discovery，Legendary→Mystery
        /// </summary>
        private EventTemplate GetRandomTemplateByRarity(PendingEventRarity rarity)
        {
            var types = rarity switch
            {
                PendingEventRarity.Normal => new[] { GameEventType.Random, GameEventType.Trade },
                PendingEventRarity.Rare => new[] { GameEventType.Combat, GameEventType.Discovery },
                PendingEventRarity.Legendary => new[] { GameEventType.Mystery },
                _ => new[] { GameEventType.Random }
            };

            foreach (var type in types)
            {
                var template = EventManager.GetRandomTemplate(type);
                if (template != null) return template;
            }
            return null;
        }
        #endregion

        #region Processing
        /// <summary>
        /// 处理单个积压事件
        /// </summary>
        public EventResult ProcessEvent(string eventId)
        {
            if (!_eventsById.TryGetValue(eventId, out var pendingEvent))
            {
                Debug.LogWarning($"[PendingEventDesign] Event not found: {eventId}");
                return null;
            }

            if (pendingEvent.isProcessed) return null;

            var template = EventManager.GetTemplate(pendingEvent.templateId);
            var result = new EventResult
            {
                success = true,
                goldReward = pendingEvent.goldReward,
                message = $"处理了事件：{template?.name ?? pendingEvent.templateId}"
            };

            pendingEvent.isProcessed = true;
            onPendingEventChanged?.Invoke();
            return result;
        }

        /// <summary>
        /// 批量处理事件
        /// </summary>
        public List<EventResult> ProcessBatch(List<string> eventIds)
        {
            var results = new List<EventResult>();
            foreach (var id in eventIds)
            {
                var result = ProcessEvent(id);
                if (result != null) results.Add(result);
            }
            return results;
        }

        /// <summary>
        /// 批量处理所有未处理事件
        /// </summary>
        public List<EventResult> ProcessAllPending()
        {
            var ids = _events.Where(e => !e.isProcessed).Select(e => e.eventId).ToList();
            return ProcessBatch(ids);
        }
        #endregion

        #region Queries
        /// <summary>
        /// 获取所有未处理事件
        /// </summary>
        public List<PendingEventData> GetPendingEvents()
        {
            return _events.Where(e => !e.isProcessed).ToList();
        }

        /// <summary>
        /// 按稀有度筛选未处理事件
        /// </summary>
        public List<PendingEventData> GetPendingByRarity(PendingEventRarity rarity)
        {
            return _events.Where(e => !e.isProcessed && e.rarity == rarity).ToList();
        }

        /// <summary>
        /// 按时间倒序获取所有事件的时间线
        /// </summary>
        public List<PendingEventData> GetTimeline()
        {
            return _events.OrderByDescending(e => e.timestamp).ToList();
        }

        /// <summary>
        /// 获取稀有度分布
        /// </summary>
        public RarityDistribution GetRarityDistribution()
        {
            var dist = new RarityDistribution();
            foreach (var e in _events.Where(e => !e.isProcessed))
            {
                switch (e.rarity)
                {
                    case PendingEventRarity.Normal: dist.normalCount++; break;
                    case PendingEventRarity.Rare: dist.rareCount++; break;
                    case PendingEventRarity.Legendary: dist.legendaryCount++; break;
                }
            }
            return dist;
        }
        #endregion

        #region Serialization
        public PendingEventSaveData Export()
        {
            return new PendingEventSaveData { pendingEvents = new List<PendingEventData>(_events) };
        }

        public void Import(PendingEventSaveData saveData)
        {
            Clear();
            if (saveData?.pendingEvents == null) return;

            foreach (var e in saveData.pendingEvents)
            {
                _events.Add(e);
                _eventsById[e.eventId] = e;

                if (!string.IsNullOrEmpty(e.eventId) && e.eventId.StartsWith("pending_"))
                {
                    var parts = e.eventId.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int idNum))
                    {
                        if (idNum > _eventIdCounter) _eventIdCounter = idNum;
                    }
                }
            }
            Debug.Log($"[PendingEventDesign] Imported {saveData.pendingEvents.Count} pending events");
        }

        public void Clear()
        {
            _events.Clear();
            _eventsById.Clear();
            _eventIdCounter = 0;
        }
        #endregion
    }

    /// <summary>
    /// 稀有度分布数据
    /// </summary>
    public class RarityDistribution
    {
        public int normalCount;
        public int rareCount;
        public int legendaryCount;
    }
}
