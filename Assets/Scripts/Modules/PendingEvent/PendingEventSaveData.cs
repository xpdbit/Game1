using System.Collections.Generic;
using System.Xml;

namespace Game1.Modules.PendingEvent
{
    /// <summary>
    /// 积压事件存档数据
    /// </summary>
    [System.Serializable]
    public class PendingEventSaveData
    {
        public List<PendingEventData> pendingEvents = new();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<PendingEventSaveData>");
            sb.Append("<pendingEvents>");
            foreach (var e in pendingEvents)
                sb.Append(e.ToXml());
            sb.Append("</pendingEvents>");
            sb.Append("</PendingEventSaveData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            var node = element.SelectSingleNode("pendingEvents");
            pendingEvents.Clear();
            if (node == null) return;

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child is XmlElement childElem && childElem.Name == "PendingEventData")
                {
                    var data = new PendingEventData();
                    data.ParseFromXml(childElem);
                    pendingEvents.Add(data);
                }
            }
        }
    }
}
