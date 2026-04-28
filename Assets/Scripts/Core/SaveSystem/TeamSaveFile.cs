#nullable enable
using System.Collections.Generic;
using System.Xml;

namespace Game1
{
    public sealed class TeamSaveFile : ISaveFile
    {
        public string FileName => "team.xml";
        public int Version => 1;
        public List<MemberEntry> members = new List<MemberEntry>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<TeamSaveFile>");
            sb.Append("<members>");
            foreach (var member in members)
            {
                sb.Append("<MemberEntry>");
                sb.Append($"<memberId>{member.memberId}</memberId>");
                sb.Append($"<actorId>{XmlEscape.EscapeXml(member.actorId)}</actorId>");
                sb.Append($"<name>{XmlEscape.EscapeXml(member.name)}</name>");
                sb.Append($"<level>{member.level}</level>");
                sb.Append($"<currentHp>{member.currentHp}</currentHp>");
                sb.Append($"<maxHp>{member.maxHp}</maxHp>");
                sb.Append($"<attack>{member.attack}</attack>");
                sb.Append($"<defense>{member.defense}</defense>");
                sb.Append($"<speed>{member.speed}</speed>");
                sb.Append($"<jobType>{XmlEscape.EscapeXml(member.jobType)}</jobType>");
                sb.Append("</MemberEntry>");
            }
            sb.Append("</members>");
            sb.Append("</TeamSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            members.Clear();
            var membersNode = element["members"];
            if (membersNode == null) return;

            foreach (XmlNode node in membersNode.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element || node.Name != "MemberEntry") continue;
                var memberElem = (XmlElement)node;
                var entry = new MemberEntry
                {
                    memberId = int.Parse(memberElem["memberId"]?.InnerText ?? "0"),
                    actorId = memberElem["actorId"]?.InnerText ?? "",
                    name = memberElem["name"]?.InnerText ?? "",
                    level = int.Parse(memberElem["level"]?.InnerText ?? "1"),
                    currentHp = int.Parse(memberElem["currentHp"]?.InnerText ?? "0"),
                    maxHp = int.Parse(memberElem["maxHp"]?.InnerText ?? "0"),
                    attack = int.Parse(memberElem["attack"]?.InnerText ?? "0"),
                    defense = int.Parse(memberElem["defense"]?.InnerText ?? "0"),
                    speed = float.Parse(memberElem["speed"]?.InnerText ?? "0"),
                    jobType = memberElem["jobType"]?.InnerText ?? ""
                };
                members.Add(entry);
            }
        }

        public class MemberEntry
        {
            public int memberId;
            public string actorId = "";
            public string name = "";
            public int level;
            public int currentHp;
            public int maxHp;
            public int attack;
            public int defense;
            public float speed;
            public string jobType = "";
        }
    }
}