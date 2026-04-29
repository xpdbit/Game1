#nullable enable

namespace Game1.Modules.Achievement
{
    // 成就类别枚举
    public enum AchievementCategory
    {
        Exploration,   // 探索类: 到达X距离、发现X个地点
        Combat,        // 战斗类: 击败X个敌人、赢得X场战斗
        Collection,    // 收集类: 获得X金币、获得X个物品
        Team,          // 团队类: 招募X个成员、组合X个职业
        Special,       // 特殊类: 完成事件树、转生X次
        Hidden         // 隐藏类: 特殊条件触发
    }

    // 条件类型枚举
    public enum AchievementConditionType
    {
        GoldEarned,         // 累计获得金币
        EnemiesDefeated,    // 击败敌人数量
        DistanceTraveled,   // 旅行距离
        ItemsCollected,     // 收集物品数量
        TeamMembers,        // 队伍成员数量
        LevelsGained,       // 升级次数
        EventsCompleted,    // 完成事件数量
        PrestigesPerformed, // 转生次数
        CombatWon,          // 战斗胜利次数
        BossesDefeated,     // 击败Boss数量
        LocationsDiscovered, // 发现地点数量
        PetsMaxHappiness,   // 宠物满心情次数
    }

    // 奖励类型枚举
    public enum RewardType
    {
        Gold,
        Item,
        Experience,
        Title,
    }

    // 奖励数据
    [System.Serializable]
    public class AchievementRewardData
    {
        public RewardType type;
        public string configId;  // 物品ID等
        public int amount;
    }

    // 条件数据
    [System.Serializable]
    public class AchievementConditionData
    {
        public AchievementConditionType type;
        public float targetValue;
        public string extraParam;  // 额外参数(可选)
    }

    // 成就模板（配置数据）
    [System.Serializable]
    public class AchievementTemplate
    {
        public string id;                    // 唯一ID, 如 "Core.Achievement.FirstBlood"
        public string nameTextId;            // 名称文本ID
        public string descriptionTextId;     // 描述文本ID
        public string iconPath;              // 图标资源路径
        public AchievementCategory category;
        public bool isHidden;                // 隐藏成就
        public System.Collections.Generic.List<string> prerequisiteIds; // 前置成就ID列表
        public System.Collections.Generic.List<AchievementConditionData> conditions;
        public System.Collections.Generic.List<AchievementRewardData> rewards;
        public bool isIncremental;           // 是否增量追踪
    }

    // 成就运行时实例
    [System.Serializable]
    public class AchievementInstance
    {
        public string templateId;
        public bool isUnlocked;
        public float[] conditionProgress;    // 对应conditions的进度
        public long unlockedAtTimestamp;     // 解锁时间戳(Unix)
    }
}