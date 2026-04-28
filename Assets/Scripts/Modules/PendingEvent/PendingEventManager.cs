using System.Collections.Generic;
using Game1.Events;

namespace Game1.Modules.PendingEvent
{
    /// <summary>
    /// 积压事件管理器——静态API代理，委托给PendingEventDesign
    /// </summary>
    public static class PendingEventManager
    {
        #region Generation
        /// <summary>
        /// 生成离线积压事件
        /// </summary>
        public static void GeneratePendingEvents(float offlineSeconds)
        {
            PendingEventDesign.instance.GeneratePendingEvents(offlineSeconds);
        }
        #endregion

        #region Processing
        /// <summary>
        /// 处理单个积压事件
        /// </summary>
        public static EventResult ProcessEvent(string eventId)
        {
            return PendingEventDesign.instance.ProcessEvent(eventId);
        }

        /// <summary>
        /// 批量处理指定事件
        /// </summary>
        public static List<EventResult> ProcessBatch(List<string> eventIds)
        {
            return PendingEventDesign.instance.ProcessBatch(eventIds);
        }

        /// <summary>
        /// 处理所有未处理事件
        /// </summary>
        public static List<EventResult> ProcessAllPending()
        {
            return PendingEventDesign.instance.ProcessAllPending();
        }
        #endregion

        #region Queries
        /// <summary>
        /// 是否有未处理的积压事件
        /// </summary>
        public static bool HasPendingEvents()
        {
            return PendingEventDesign.instance.hasPendingEvents;
        }

        /// <summary>
        /// 获取所有未处理事件
        /// </summary>
        public static List<PendingEventData> GetPendingEvents()
        {
            return PendingEventDesign.instance.GetPendingEvents();
        }

        /// <summary>
        /// 按稀有度筛选事件
        /// </summary>
        public static List<PendingEventData> GetPendingByRarity(PendingEventRarity rarity)
        {
            return PendingEventDesign.instance.GetPendingByRarity(rarity);
        }

        /// <summary>
        /// 获取事件时间线
        /// </summary>
        public static List<PendingEventData> GetTimeline()
        {
            return PendingEventDesign.instance.GetTimeline();
        }

        /// <summary>
        /// 获取稀有度分布
        /// </summary>
        public static RarityDistribution GetRarityDistribution()
        {
            return PendingEventDesign.instance.GetRarityDistribution();
        }

        /// <summary>
        /// 获取积压事件简报
        /// </summary>
        public static PendingEventBrief GetBrief()
        {
            var events = PendingEventDesign.instance.GetPendingEvents();
            return PendingEventBrief.GenerateFromEvents(events);
        }
        #endregion

        #region Management
        /// <summary>
        /// 订阅事件变更通知
        /// </summary>
        public static void Subscribe(System.Action callback)
        {
            PendingEventDesign.instance.onPendingEventChanged += callback;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public static void Unsubscribe(System.Action callback)
        {
            PendingEventDesign.instance.onPendingEventChanged -= callback;
        }

        /// <summary>
        /// 获取积压事件存档数据
        /// </summary>
        public static PendingEventSaveData Export()
        {
            return PendingEventDesign.instance.Export();
        }

        /// <summary>
        /// 加载积压事件存档数据
        /// </summary>
        public static void Import(PendingEventSaveData saveData)
        {
            PendingEventDesign.instance.Import(saveData);
        }

        /// <summary>
        /// 清空所有积压事件
        /// </summary>
        public static void Clear()
        {
            PendingEventDesign.instance.Clear();
        }
        #endregion
    }
}
