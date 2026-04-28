using System;
using System.Collections.Generic;
using System.Xml;


namespace Game1
{
    public sealed class PrestigeSaveFile : ISaveFile
    {
        public string FileName => "prestige.xml";
        public int Version => 1;

        public int prestigeCount;
        public int prestigePoints;
        public float goldRetentionRate;
        public float expRetentionRate;
        public List<UpgradeEntry> purchasedUpgrades = new List<UpgradeEntry>();
        public List<string> retainedSkills = new List<string>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<PrestigeSaveFile>");
            sb.Append($"<prestigeCount>{prestigeCount}</prestigeCount>");
            sb.Append($"<prestigePoints>{prestigePoints}</prestigePoints>");
            sb.Append($"<goldRetentionRate>{goldRetentionRate}</goldRetentionRate>");
            sb.Append($"<expRetentionRate>{expRetentionRate}</expRetentionRate>");
            sb.Append("<purchasedUpgrades>");
            foreach (var upgrade in purchasedUpgrades)
            {
                sb.Append("<UpgradeEntry>");
                sb.Append($"<id>{XmlEscape.EscapeXml(upgrade.id)}</id>");
                sb.Append($"<isPurchased>{upgrade.isPurchased}</isPurchased>");
                sb.Append($"<currentLevel>{upgrade.currentLevel}</currentLevel>");
                sb.Append("</UpgradeEntry>");
            }
            sb.Append("</purchasedUpgrades>");
            sb.Append("<retainedSkills>");
            foreach (var skill in retainedSkills)
            {
                sb.Append($"<skill>{XmlEscape.EscapeXml(skill)}</skill>");
            }
            sb.Append("</retainedSkills>");
            sb.Append("</PrestigeSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            prestigeCount = int.Parse(element.SelectSingleNode("prestigeCount")?.InnerText ?? "0");
            prestigePoints = int.Parse(element.SelectSingleNode("prestigePoints")?.InnerText ?? "0");
            goldRetentionRate = float.Parse(element.SelectSingleNode("goldRetentionRate")?.InnerText ?? "0");
            expRetentionRate = float.Parse(element.SelectSingleNode("expRetentionRate")?.InnerText ?? "0");

            var upgrades = element.SelectNodes("purchasedUpgrades/UpgradeEntry");
            foreach (XmlNode node in upgrades)
            {
                var upgrade = new UpgradeEntry
                {
                    id = node.SelectSingleNode("id")?.InnerText ?? "",
                    isPurchased = bool.Parse(node.SelectSingleNode("isPurchased")?.InnerText ?? "false"),
                    currentLevel = int.Parse(node.SelectSingleNode("currentLevel")?.InnerText ?? "0")
                };
                purchasedUpgrades.Add(upgrade);
            }

            var skills = element.SelectNodes("retainedSkills/skill");
            foreach (XmlNode node in skills)
            {
                retainedSkills.Add(node.InnerText);
            }
        }

        public class UpgradeEntry
        {
            public string id;
            public bool isPurchased;
            public int currentLevel;
        }
    }
}