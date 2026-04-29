#nullable enable

using System.Collections.Generic;
using Game1.Core.EventBus;

namespace Game1.Modules.Achievement
{
    /// <summary>
    /// EventBus订阅器 - 将游戏事件映射到成就进度更新
    /// </summary>
    public class AchievementEventBusSubscriber : EventSubscriberBase
    {
        public AchievementEventBusSubscriber()
        {
            _subscribedEvents.Add(EventType.GoldChanged);
            _subscribedEvents.Add(EventType.EnemyDefeated);
            _subscribedEvents.Add(EventType.LevelUp);
            _subscribedEvents.Add(EventType.TravelCompleted);
            _subscribedEvents.Add(EventType.ItemsCollected);
            _subscribedEvents.Add(EventType.TeamMembersChanged);
            _subscribedEvents.Add(EventType.EventsCompleted);
            _subscribedEvents.Add(EventType.PrestigesPerformed);
            _subscribedEvents.Add(EventType.CombatWon);
            _subscribedEvents.Add(EventType.BossDefeated);
            _subscribedEvents.Add(EventType.LocationsDiscovered);
            _subscribedEvents.Add(EventType.PetsMaxHappiness);
        }

        public override void OnEvent(GameEvent e)
        {
            switch (e.type)
            {
                case EventType.GoldChanged:
                    float goldDelta = ExtractFloat(e.data, 1f);
                    AchievementManager.ReportProgress(AchievementConditionType.GoldEarned, goldDelta);
                    break;

                case EventType.EnemyDefeated:
                    AchievementManager.ReportProgress(AchievementConditionType.EnemiesDefeated, 1);
                    break;

                case EventType.LevelUp:
                    AchievementManager.ReportProgress(AchievementConditionType.LevelsGained, 1);
                    break;

                case EventType.TravelCompleted:
                    float distance = ExtractFloat(e.data, 1f);
                    AchievementManager.ReportProgress(AchievementConditionType.DistanceTraveled, distance);
                    break;

                case EventType.ItemsCollected:
                    AchievementManager.ReportProgress(AchievementConditionType.ItemsCollected, 1);
                    break;

                case EventType.TeamMembersChanged:
                    AchievementManager.ReportProgress(AchievementConditionType.TeamMembers, 1);
                    break;

                case EventType.EventsCompleted:
                    AchievementManager.ReportProgress(AchievementConditionType.EventsCompleted, 1);
                    break;

                case EventType.PrestigesPerformed:
                    AchievementManager.ReportProgress(AchievementConditionType.PrestigesPerformed, 1);
                    break;

                case EventType.CombatWon:
                    AchievementManager.ReportProgress(AchievementConditionType.CombatWon, 1);
                    break;

                case EventType.BossDefeated:
                    AchievementManager.ReportProgress(AchievementConditionType.BossesDefeated, 1);
                    break;

                case EventType.LocationsDiscovered:
                    AchievementManager.ReportProgress(AchievementConditionType.LocationsDiscovered, 1);
                    break;

                case EventType.PetsMaxHappiness:
                    AchievementManager.ReportProgress(AchievementConditionType.PetsMaxHappiness, 1);
                    break;
            }
        }

        private static float ExtractFloat(object? data, float defaultValue)
        {
            if (data == null) return defaultValue;
            if (data is float f) return f;
            if (data is int i) return i;
            if (data is double d) return (float)d;
            if (data is long l) return l;
            return defaultValue;
        }
    }
}
