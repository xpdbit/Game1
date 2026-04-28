#nullable enable
using System.Collections.Generic;
using System.Xml;

namespace Game1
{
    public sealed class SkillSaveFile : ISaveFile
    {
        public string FileName => "skill.xml";
        public int Version => 1;

        public List<MemberSkillGroup> skillGroups = new List<MemberSkillGroup>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<SkillSaveFile>");
            sb.Append("<skillGroups>");

            foreach (var group in skillGroups)
            {
                sb.Append("<MemberSkillGroup>");
                sb.Append($"<memberId>{group.memberId}</memberId>");
                sb.Append("<skills>");

                foreach (var skill in group.skills)
                {
                    sb.Append("<SkillEntry>");
                    sb.Append($"<skillId>{XmlEscape.EscapeXml(skill.skillId)}</skillId>");
                    sb.Append($"<currentLevel>{skill.currentLevel}</currentLevel>");
                    sb.Append("</SkillEntry>");
                }

                sb.Append("</skills>");
                sb.Append("</MemberSkillGroup>");
            }

            sb.Append("</skillGroups>");
            sb.Append("</SkillSaveFile>");

            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            skillGroups.Clear();

            var skillGroupsNode = element["skillGroups"];
            if (skillGroupsNode == null)
                return;

            foreach (XmlNode node in skillGroupsNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element || node.Name != "MemberSkillGroup")
                    continue;

                var memberGroupNode = (XmlElement)node;
                var group = new MemberSkillGroup
                {
                    memberId = int.Parse(memberGroupNode["memberId"]?.InnerText ?? "0")
                };

                group.skills = new List<SkillEntry>();
                var skillsNode = memberGroupNode["skills"];
                if (skillsNode != null)
                {
                    foreach (XmlNode skillNode in skillsNode.ChildNodes)
                    {
                        if (skillNode.NodeType != XmlNodeType.Element || skillNode.Name != "SkillEntry")
                            continue;

                        var skillEntryNode = (XmlElement)skillNode;
                        var entry = new SkillEntry
                        {
                            skillId = skillEntryNode["skillId"]?.InnerText ?? "",
                            currentLevel = int.Parse(skillEntryNode["currentLevel"]?.InnerText ?? "0")
                        };
                        group.skills.Add(entry);
                    }
                }

                skillGroups.Add(group);
            }
        }

        public class SkillEntry
        {
            public string skillId = "";
            public int currentLevel;
        }

        public class MemberSkillGroup
        {
            public int memberId;
            public List<SkillEntry> skills = new List<SkillEntry>();
        }
    }
}