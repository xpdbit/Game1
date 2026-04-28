using UnityEngine;

namespace Game1.Modules.PendingEvent
{
    /// <summary>
    /// 积压事件稀有度
    /// Normal=普通（高频低值），Rare=稀有（中频中值），Legendary=传奇（低频高值）
    /// </summary>
    public enum PendingEventRarity
    {
        Normal = 0,
        Rare = 1,
        Legendary = 2
    }

    /// <summary>
    /// 稀有度权重配置（用于离线事件生成）
    /// </summary>
    [System.Serializable]
    public class RarityWeightConfig
    {
        public int normalWeight = 70;
        public int rareWeight = 25;
        public int legendaryWeight = 5;

        public PendingEventRarity RollRarity()
        {
            int roll = Random.Range(1, 101);
            if (roll <= legendaryWeight) return PendingEventRarity.Legendary;
            if (roll <= legendaryWeight + rareWeight) return PendingEventRarity.Rare;
            return PendingEventRarity.Normal;
        }
    }
}
