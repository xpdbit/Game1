using System;
using System.Collections.Generic;
using System.Xml;

namespace Game1
{
    public sealed class AchievementSaveFile : ISaveFile
    {
        public string FileName => "achievement.xml";
        public int Version => 1;
        public List<AchievementRecord> records = new List<AchievementRecord>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<AchievementSaveFile version=\"" + Version + "\"><records>");
            foreach (var record in records)
            {
                sb.Append("<record");
                sb.Append(" templateId=\"" + XmlEscape.EscapeXml(record.templateId) + "\"");
                sb.Append(" isUnlocked=\"" + record.isUnlocked + "\"");
                if (record.conditionProgress != null && record.conditionProgress.Length > 0)
                {
                    sb.Append(" progress=\"" + string.Join(",", record.conditionProgress) + "\"");
                }
                if (record.unlockedAtTimestamp > 0)
                    sb.Append(" unlockedAt=\"" + record.unlockedAtTimestamp + "\"");
                sb.Append("/>");
            }
            sb.Append("</records></AchievementSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            records.Clear();
            var recordsNode = element.SelectSingleNode("records");
            if (recordsNode == null) return;

            foreach (XmlElement recordEl in recordsNode.ChildNodes)
            {
                var record = new AchievementRecord
                {
                    templateId = recordEl.GetAttribute("templateId"),
                    isUnlocked = bool.Parse(recordEl.GetAttribute("isUnlocked") ?? "false"),
                    conditionProgress = ParseProgressArray(recordEl.GetAttribute("progress")),
                    unlockedAtTimestamp = long.Parse(recordEl.GetAttribute("unlockedAt") ?? "0")
                };
                records.Add(record);
            }
        }

        private float[] ParseProgressArray(string value)
        {
            if (string.IsNullOrEmpty(value)) return new float[0];
            var parts = value.Split(',');
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                float.TryParse(parts[i], out result[i]);
            return result;
        }
    }

    public class AchievementRecord
    {
        public string templateId;
        public bool isUnlocked;
        public float[] conditionProgress;
        public long unlockedAtTimestamp;
    }
}