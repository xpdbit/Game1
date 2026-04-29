using System.Collections.Generic;

namespace Game1.Modules.Achievement
{
    [System.Serializable]
    public class TaskTemplate
    {
        public string id;
        public string nameTextId;
        public string descriptionTextId;
        public TaskCategory category;
        public AchievementConditionType conditionType;
        public float targetValue;
        public AchievementRewardData reward;
        public bool isActive = true;
    }

    [System.Serializable]
    public class TaskInstance
    {
        public string templateId;
        public TaskCategory category;
        public float currentValue;
        public float targetValue;
        public bool isCompleted;
        public bool isClaimed;
        public long refreshTimestamp;
    }
}