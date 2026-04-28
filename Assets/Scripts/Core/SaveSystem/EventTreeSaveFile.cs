using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Game1
{
    public sealed class EventTreeSaveFile : ISaveFile
    {
        public string FileName => "events.xml";
        public int Version => 1;

        public string templateId;
        public string currentNodeId;
        public bool isRunning;
        public List<string> history;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<EventTreeSaveFile>");
            sb.Append("<templateId>").Append(XmlEscape.EscapeXml(templateId)).Append("</templateId>");
            sb.Append("<currentNodeId>").Append(XmlEscape.EscapeXml(currentNodeId)).Append("</currentNodeId>");
            sb.Append("<isRunning>").Append(isRunning.ToString().ToLower()).Append("</isRunning>");
            sb.Append("<history>");
            if (history != null)
            {
                foreach (var item in history)
                {
                    sb.Append("<item>").Append(XmlEscape.EscapeXml(item)).Append("</item>");
                }
            }
            sb.Append("</history>");
            sb.Append("</EventTreeSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            if (element == null)
                return;

            var templateIdNode = element.SelectSingleNode("templateId");
            if (templateIdNode != null)
                templateId = templateIdNode.InnerText;

            var currentNodeIdNode = element.SelectSingleNode("currentNodeId");
            if (currentNodeIdNode != null)
                currentNodeId = currentNodeIdNode.InnerText;

            var isRunningNode = element.SelectSingleNode("isRunning");
            if (isRunningNode != null)
                bool.TryParse(isRunningNode.InnerText, out isRunning);

            history = new List<string>();
            var historyNode = element.SelectSingleNode("history");
            if (historyNode != null)
            {
                var itemNodes = historyNode.SelectNodes("item");
                foreach (XmlNode itemNode in itemNodes)
                {
                    history.Add(itemNode.InnerText);
                }
            }
        }
    }
}