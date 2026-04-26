using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 里程碑事件触发器
    /// 连接ProgressManager与EventTreeRunner
    /// 当进度达到里程碑时自动触发事件树
    /// </summary>
    public class MilestoneEventTrigger : MonoBehaviour
    {
        [Header("配置")]
        public bool autoTriggerOnMilestone = true;
        public List<string> eventTreePool = new();

        [Header("调试")]
        public bool logTriggers = true;

        private void OnEnable()
        {
            if (ProgressManager.instance != null)
            {
                ProgressManager.instance.onEventTreeTriggered += OnMilestoneReached;
                ProgressManager.instance.onNormalEventTriggered += OnNormalEventTriggered;
            }

            if (logTriggers)
                Debug.Log("[MilestoneEventTrigger] Subscribed to ProgressManager events");
        }

        private void OnDisable()
        {
            if (ProgressManager.instance != null)
            {
                ProgressManager.instance.onEventTreeTriggered -= OnMilestoneReached;
                ProgressManager.instance.onNormalEventTriggered -= OnNormalEventTriggered;
            }
        }

        private void OnMilestoneReached(int milestoneNumber)
        {
            if (!autoTriggerOnMilestone) return;

            if (logTriggers)
                Debug.Log($"[MilestoneEventTrigger] Milestone #{milestoneNumber} reached!");

            string templateId = SelectEventTreeForMilestone(milestoneNumber);

            if (!string.IsNullOrEmpty(templateId))
            {
                EnsureEventTreePanelReady();
                bool success = EventTreeRunner.instance.StartTree(templateId);

                if (logTriggers)
                    Debug.Log($"[MilestoneEventTrigger] Starting event tree: {templateId}, success={success}");
            }
            else
            {
                Debug.LogWarning($"[MilestoneEventTrigger] No event tree template found for milestone #{milestoneNumber}");
            }
        }

        private void OnNormalEventTriggered(int eventNumber)
        {
            if (!autoTriggerOnMilestone) return;

            if (logTriggers)
                Debug.Log($"[MilestoneEventTrigger] Normal event #{eventNumber} triggered");

            TriggerNormalEvent(eventNumber);
        }

        private string SelectEventTreeForMilestone(int milestoneNumber)
        {
            if (eventTreePool.Count == 0)
            {
                var templates = EventTreeManager.GetAllTemplates();
                if (templates.Count > 0)
                {
                    int index = milestoneNumber % templates.Count;
                    return templates[index].id;
                }
                return null;
            }

            int poolIndex = milestoneNumber % eventTreePool.Count;
            return eventTreePool[poolIndex];
        }

        private void TriggerNormalEvent(int eventNumber)
        {
            var events = EventManager.GetAllEventTemplates();
            if (events.Count > 0)
            {
                int index = eventNumber % events.Count;
                var eventTemplate = events[index];

                if (logTriggers)
                    Debug.Log($"[MilestoneEventTrigger] Triggering normal event: {eventTemplate.id}");
            }
        }

        private void EnsureEventTreePanelReady()
        {
            if (UIManager.instance != null)
            {
                // 可以打开事件面板
            }
        }

        public void TriggerEventTree(string templateId)
        {
            if (logTriggers)
                Debug.Log($"[MilestoneEventTrigger] Manually triggering event tree: {templateId}");

            bool success = EventTreeRunner.instance.StartTree(templateId);

            if (!success)
            {
                Debug.LogWarning($"[MilestoneEventTrigger] Failed to trigger event tree: {templateId}");
            }
        }

        public (int currentPoints, int milestone, float progress) GetProgressInfo()
        {
            if (ProgressManager.instance != null)
            {
                return (
                    ProgressManager.instance.currentPoints,
                    ProgressManager.instance.milestoneCount,
                    ProgressManager.instance.progressPercent
                );
            }
            return (0, 0, 0f);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Milestone Trigger")]
        private void TestMilestoneTrigger()
        {
            OnMilestoneReached(1);
        }

        [ContextMenu("Test Normal Event")]
        private void TestNormalEvent()
        {
            OnNormalEventTriggered(1);
        }
#endif
    }
}