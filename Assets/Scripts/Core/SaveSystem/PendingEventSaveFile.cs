using System;
using System.Collections.Generic;
using System.Xml;

namespace Game1
{
    public sealed class PendingEventSaveFile : ISaveFile
    {
        public string FileName => "pending.xml";
        public int Version => 1;

        public List<PendingEventEntry> pendingEvents = new List<PendingEventEntry>();

        public string ToXml()
        {
            var xml = new System.Text.StringBuilder();
            xml.Append("<PendingEventSaveFile>");
            xml.Append("<pendingEvents>");
            foreach (var entry in pendingEvents)
            {
                xml.Append("<PendingEventEntry>");
                xml.Append($"<eventId>{XmlEscape.EscapeXml(entry.eventId)}</eventId>");
                xml.Append($"<templateId>{XmlEscape.EscapeXml(entry.templateId)}</templateId>");
                xml.Append($"<rarity>{entry.rarity}</rarity>");
                xml.Append($"<timestamp>{entry.timestamp}</timestamp>");
                xml.Append($"<offlineSeconds>{entry.offlineSeconds}</offlineSeconds>");
                xml.Append($"<isProcessed>{entry.isProcessed}</isProcessed>");
                xml.Append($"<goldReward>{entry.goldReward}</goldReward>");
                xml.Append("</PendingEventEntry>");
            }
            xml.Append("</pendingEvents>");
            xml.Append("</PendingEventSaveFile>");
            return xml.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            pendingEvents.Clear();
            var pendingEventsNode = element.SelectSingleNode("pendingEvents");
            if (pendingEventsNode == null)
                return;

            foreach (XmlNode node in pendingEventsNode.ChildNodes)
            {
                if (node.Name != "PendingEventEntry")
                    continue;

                var entry = new PendingEventEntry();
                entry.eventId = node.SelectSingleNode("eventId")?.InnerText ?? "";
                entry.templateId = node.SelectSingleNode("templateId")?.InnerText ?? "";
                entry.rarity = int.TryParse(node.SelectSingleNode("rarity")?.InnerText, out var r) ? r : 0;
                entry.timestamp = long.TryParse(node.SelectSingleNode("timestamp")?.InnerText, out var ts) ? ts : 0;
                entry.offlineSeconds = float.TryParse(node.SelectSingleNode("offlineSeconds")?.InnerText, out var fs) ? fs : 0f;
                entry.isProcessed = bool.TryParse(node.SelectSingleNode("isProcessed")?.InnerText, out var ip) && ip;
                entry.goldReward = int.TryParse(node.SelectSingleNode("goldReward")?.InnerText, out var gr) ? gr : 0;
                pendingEvents.Add(entry);
            }
        }

        public class PendingEventEntry
        {
            public string eventId;
            public string templateId;
            public int rarity;
            public long timestamp;
            public float offlineSeconds;
            public bool isProcessed;
            public int goldReward;
        }
    }
}
