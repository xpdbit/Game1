using System.Text;
using System.Xml;

namespace Game1
{
    public sealed class WorldSaveFile : ISaveFile
    {
        public string FileName => "world.xml";
        public int Version => 1;

        public int currentMapIndex;
        public string currentMapSeed = null!;
        public float travelProgress;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<WorldSaveFile>");
            sb.Append("<Version>").Append(Version).Append("</Version>");
            sb.Append("<currentMapIndex>").Append(currentMapIndex).Append("</currentMapIndex>");
            sb.Append("<currentMapSeed>").Append(XmlEscape.EscapeXml(currentMapSeed ?? string.Empty)).Append("</currentMapSeed>");
            sb.Append("<travelProgress>").Append(travelProgress).Append("</travelProgress>");
            sb.Append("</WorldSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            currentMapIndex = int.Parse(element.SelectSingleNode("currentMapIndex")?.InnerText ?? "0");
            currentMapSeed = element.SelectSingleNode("currentMapSeed")?.InnerText ?? string.Empty;
            travelProgress = float.Parse(element.SelectSingleNode("travelProgress")?.InnerText ?? "0");
        }
    }
}