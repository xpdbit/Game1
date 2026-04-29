using System;
using System.Text;
using System.Xml;

namespace Game1
{
    public class PlayerSaveFile : ISaveFile
    {
        public string FileName => "player.xml";
        public int Version => 1;

        public string actorId;
        public string actorName;
        public int level;
        public int exp;
        public int gold;
        public long totalInputCount;
        public long playTime;
        public float offlineAccumulatedTime;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<PlayerSaveFile>");
            sb.Append("<Version>").Append(Version).Append("</Version>");
            sb.Append("<actorId>").Append(XmlEscape.EscapeXml(actorId ?? string.Empty)).Append("</actorId>");
            sb.Append("<actorName>").Append(XmlEscape.EscapeXml(actorName ?? string.Empty)).Append("</actorName>");
            sb.Append("<level>").Append(level).Append("</level>");
            sb.Append("<exp>").Append(exp).Append("</exp>");
            sb.Append("<gold>").Append(gold).Append("</gold>");
            sb.Append("<totalInputCount>").Append(totalInputCount).Append("</totalInputCount>");
            sb.Append("<playTime>").Append(playTime).Append("</playTime>");
            sb.Append("<offlineAccumulatedTime>").Append(offlineAccumulatedTime).Append("</offlineAccumulatedTime>");
            sb.Append("</PlayerSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            actorId = element.SelectSingleNode("actorId")?.InnerText ?? string.Empty;
            actorName = element.SelectSingleNode("actorName")?.InnerText ?? string.Empty;
            level = int.Parse(element.SelectSingleNode("level")?.InnerText ?? "0");
            exp = int.Parse(element.SelectSingleNode("exp")?.InnerText ?? "0");
            gold = int.Parse(element.SelectSingleNode("gold")?.InnerText ?? "0");
            totalInputCount = long.Parse(element.SelectSingleNode("totalInputCount")?.InnerText ?? "0");
            playTime = long.Parse(element.SelectSingleNode("playTime")?.InnerText ?? "0");
            offlineAccumulatedTime = float.Parse(element.SelectSingleNode("offlineAccumulatedTime")?.InnerText ?? "0");
        }
    }
}