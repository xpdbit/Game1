#nullable enable

using System;
using System.Collections.Generic;

namespace Game1.Modules.Achievement
{
    public static class AchievementManager
    {
        private static bool _isLoaded = false;
        private static AchievementEventBusSubscriber _subscriber;

        public static void Initialize()
        {
            if (_isLoaded) return;
            AchievementDesign.instance.Initialize();
            _subscriber = new AchievementEventBusSubscriber();
            _subscriber.RegisterSelf();
            _isLoaded = true;
        }

        public static void ReportProgress(AchievementConditionType type, float delta)
            => AchievementDesign.instance.UpdateConditionProgress(type, delta);

        public static void UnlockAchievement(string templateId)
        {
            // by direct trigger - 直接解锁，不检查条件
            var instance = AchievementDesign.instance.GetInstance(templateId);
            if (instance != null && !instance.isUnlocked)
            {
                // 强制解锁逻辑可通过AchievementDesign内部方法实现
                // 此处作为占位符
            }
        }

        public static AchievementTemplate? GetTemplate(string templateId)
            => AchievementDesign.instance.GetTemplate(templateId);

        public static List<AchievementUIData> GetAllAchievements()
            => AchievementDesign.instance.GetAllAchievements();

        public static List<AchievementUIData> GetByCategory(AchievementCategory category)
            => AchievementDesign.instance.GetByCategory(category);

        public static int GetTotalUnlockedCount()
            => AchievementDesign.instance.GetTotalUnlockedCount();

        public static AchievementSaveData Export()
            => AchievementDesign.instance.Export();

        public static void Import(AchievementSaveData data)
            => AchievementDesign.instance.Import(data);

        // 事件订阅
        public static void SubscribeUnlocked(Action<AchievementEventData> handler)
            => AchievementDesign.instance.onAchievementUnlocked += handler;
        public static void UnsubscribeUnlocked(Action<AchievementEventData> handler)
            => AchievementDesign.instance.onAchievementUnlocked -= handler;

        public static void SubscribeProgressUpdated(Action<AchievementEventData> handler)
            => AchievementDesign.instance.onProgressUpdated += handler;
        public static void UnsubscribeProgressUpdated(Action<AchievementEventData> handler)
            => AchievementDesign.instance.onProgressUpdated -= handler;
    }
}