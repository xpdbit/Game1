using System.Collections.Generic;

namespace Game1.Modules.PendingEvent
{
    /// <summary>
    /// 积压事件简报——批量处理后展示给玩家的结果摘要
    /// </summary>
    [System.Serializable]
    public class PendingEventBrief
    {
        public int totalCount;
        public int normalCount;
        public int rareCount;
        public int legendaryCount;
        public string summaryText;
        public int totalGoldPreview;

        /// <summary>
        /// 从未处理的事件列表生成简报
        /// </summary>
        public static PendingEventBrief GenerateFromEvents(List<PendingEventData> events)
        {
            var brief = new PendingEventBrief();
            foreach (var e in events)
            {
                if (e.isProcessed) continue;
                brief.totalCount++;
                switch (e.rarity)
                {
                    case PendingEventRarity.Normal: brief.normalCount++; break;
                    case PendingEventRarity.Rare: brief.rareCount++; break;
                    case PendingEventRarity.Legendary: brief.legendaryCount++; break;
                }
                brief.totalGoldPreview += e.goldReward;
            }

            brief.summaryText = brief.totalCount > 0
                ? $"你离线期间共积累了 {brief.totalCount} 个事件。"
                  + $"包含 传奇({brief.legendaryCount})、稀有({brief.rareCount})、普通({brief.normalCount})。"
                  + $"预计可获得 {brief.totalGoldPreview} 金币。"
                : "离线期间没有特殊事件发生。";

            return brief;
        }

        public static int GetBaseGoldReward(PendingEventRarity rarity)
        {
            return rarity switch
            {
                PendingEventRarity.Normal => UnityEngine.Random.Range(10, 50),
                PendingEventRarity.Rare => UnityEngine.Random.Range(50, 200),
                PendingEventRarity.Legendary => UnityEngine.Random.Range(200, 1000),
                _ => 10
            };
        }
    }
}
