using System;
using System.Collections.Generic;
using System.Xml;


namespace Game1
{
    public sealed class InventorySaveFile : ISaveFile
    {
        public string FileName => "inventory.xml";
        public int Version => 1;
        public List<ItemEntry> items = new List<ItemEntry>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<InventorySaveFile><items>");
            foreach (var item in items)
            {
                sb.Append($"<InventorySaveData templateId=\"{XmlEscape.EscapeXml(item.templateId)}\" instanceId=\"{item.instanceId}\" amount=\"{item.amount}\"/>");
            }
            sb.Append("</items></InventorySaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            items.Clear();
            var itemsNode = element.SelectSingleNode("items");
            if (itemsNode == null) return;

            foreach (XmlElement itemElement in itemsNode.ChildNodes)
            {
                var entry = new ItemEntry
                {
                    templateId = itemElement.GetAttribute("templateId"),
                    instanceId = int.Parse(itemElement.GetAttribute("instanceId")),
                    amount = int.Parse(itemElement.GetAttribute("amount"))
                };
                items.Add(entry);
            }
        }

        public class ItemEntry
        {
            public string templateId;
            public int instanceId;
            public int amount;
        }
    }
}