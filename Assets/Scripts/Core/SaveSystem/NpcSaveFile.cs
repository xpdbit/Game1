#nullable enable
using System;
using System.Collections.Generic;
using System.Xml;


namespace Game1
{
    public sealed class NpcSaveFile : ISaveFile
    {
        public string FileName => "npc.xml";
        public int Version => 1;

        public List<NpcEntry> npcs = new List<NpcEntry>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<NpcSaveFile>");
            sb.Append("<npcs>");
            foreach (var npc in npcs)
            {
                sb.Append("<NpcEntry>");
                sb.Append($"<instanceId>{XmlEscape.EscapeXml(npc.instanceId)}</instanceId>");
                sb.Append($"<templateId>{XmlEscape.EscapeXml(npc.templateId)}</templateId>");
                sb.Append($"<currentType>{XmlEscape.EscapeXml(npc.currentType)}</currentType>");
                sb.Append($"<currentHp>{npc.currentHp}</currentHp>");
                sb.Append($"<isDefeated>{npc.isDefeated.ToString().ToLower()}</isDefeated>");
                sb.Append("</NpcEntry>");
            }
            sb.Append("</npcs>");
            sb.Append("</NpcSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            npcs.Clear();
            var npcsNode = element.SelectSingleNode("npcs");
            if (npcsNode == null)
                return;

            var nodeList = npcsNode.SelectNodes("NpcEntry");
            if (nodeList == null)
                return;

            foreach (XmlNode node in nodeList)
            {
                var entry = new NpcEntry();
                entry.instanceId = node.SelectSingleNode("instanceId")?.InnerText ?? string.Empty;
                entry.templateId = node.SelectSingleNode("templateId")?.InnerText ?? string.Empty;
                entry.currentType = node.SelectSingleNode("currentType")?.InnerText ?? string.Empty;
                entry.currentHp = int.TryParse(node.SelectSingleNode("currentHp")?.InnerText, out var hp) ? hp : 0;
                entry.isDefeated = bool.TryParse(node.SelectSingleNode("isDefeated")?.InnerText, out var defeated) && defeated;
                npcs.Add(entry);
            }
        }

        public sealed class NpcEntry
        {
            public string instanceId = string.Empty;
            public string templateId = string.Empty;
            public string currentType = string.Empty;
            public int currentHp;
            public bool isDefeated;
        }
    }
}
