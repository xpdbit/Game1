#nullable enable

namespace Game1.Modules.Achievement
{
    public enum AchievementEventType
    {
        AchievementUnlocked,
        AchievementProgressUpdated,
        TaskCompleted,
        TaskProgressUpdated,
        DailyTasksRefreshed,
        WeeklyTasksRefreshed,
    }

    [System.Serializable]
    public class AchievementEventData
    {
        public AchievementEventType eventType;
        public string achievementId;
        public string achievementName;
        public float progress;
        public float targetProgress;
    }

    // 成就数据绑定模型(给UI用)
    [System.Serializable]
    public class AchievementUIData
    {
        public string templateId;
        public string displayName;
        public string description;
        public bool isUnlocked;
        public float progress;       // 0.0 - 1.0
        public int currentValue;
        public int targetValue;
        public AchievementCategory category;
        public bool isHidden;
        public string iconPath;
    }
}