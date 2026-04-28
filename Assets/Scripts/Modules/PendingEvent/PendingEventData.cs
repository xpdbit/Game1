using System;
using System.Xml;

namespace Game1.Modules.PendingEvent
{
    /// <summary>
    /// 单个积压事件数据
    /// </summary>
    [Serializable]
    public class PendingEventData
    {
        public string eventId;
        public string templateId;
        public PendingEventRarity rarity;
        public long timestamp;
        public float offlineSeconds;
        public bool isProcessed;
        public int goldReward;

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<PendingEventData>");
            sb.Append($"<eventId>{System.Security.SecurityElement.Escape(eventId ?? "")}</eventId>");
            sb.Append($"<templateId>{System.Security.SecurityElement.Escape(templateId ?? "")}</templateId>");
            sb.Append($"<rarity>{(int)rarity}</rarity>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<offlineSeconds>{offlineSeconds}</offlineSeconds>");
            sb.Append($"<isProcessed>{(isProcessed ? 1 : 0)}</isProcessed>");
            sb.Append($"<goldReward>{goldReward}</goldReward>");
            sb.Append("</PendingEventData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            eventId = element.SelectSingleNode("eventId")?.InnerText ?? string.Empty;
            templateId = element.SelectSingleNode("templateId")?.InnerText ?? string.Empty;
            rarity = (PendingEventRarity)int.Parse(element.SelectSingleNode("rarity")?.InnerText ?? "0");
            timestamp = long.Parse(element.SelectSingleNode("timestamp")?.InnerText ?? "0");
            offlineSeconds = float.Parse(element.SelectSingleNode("offlineSeconds")?.InnerText ?? "0");
            isProcessed = int.Parse(element.SelectSingleNode("isProcessed")?.InnerText ?? "0") == 1;
            goldReward = int.Parse(element.SelectSingleNode("goldReward")?.InnerText ?? "0");
        }
    }
}
