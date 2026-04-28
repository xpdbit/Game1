using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;
using Game1.Modules.Combat;
using Game1.Modules.PendingEvent;

namespace Game1
{
    /// <summary>
    /// 存档数据基类
    /// </summary>
    [Serializable]
    public abstract class SaveDataBase
    {
        public long timestamp;

        /// <summary>
        /// 序列化为XML字符串
        /// </summary>
        public abstract string ToXml();

        /// <summary>
        /// 从XML元素解析
        /// </summary>
        public abstract void ParseFromXml(XmlElement element);
    }

    /// <summary>
    /// 战斗存档数据
    /// </summary>
    [Serializable]
    public class CombatSaveData : SaveDataBase
    {
        public int totalBattles;
        public int victories;
        public int defeats;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalGoldEarned;

        /// <summary>
        /// 默认构造函数（用于反序列化）
        /// </summary>
        public CombatSaveData() { }

        /// <summary>
        /// 从战斗统计构造
        /// </summary>
        public CombatSaveData(CombatStatistics statistics)
        {
            if (statistics == null) return;
            totalBattles = statistics.totalBattles;
            victories = statistics.victories;
            defeats = statistics.defeats;
            totalDamageDealt = statistics.totalDamageDealt;
            totalDamageTaken = statistics.totalDamageTaken;
            totalGoldEarned = statistics.totalGoldEarned;
        }

        /// <summary>
        /// 转换为战斗统计
        /// </summary>
        public CombatStatistics ToStatistics()
        {
            return new CombatStatistics
            {
                totalBattles = totalBattles,
                victories = victories,
                defeats = defeats,
                totalDamageDealt = totalDamageDealt,
                totalDamageTaken = totalDamageTaken,
                totalGoldEarned = totalGoldEarned
            };
        }

        public override string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<CombatSaveData>");
            sb.Append($"<totalBattles>{totalBattles}</totalBattles>");
            sb.Append($"<victories>{victories}</victories>");
            sb.Append($"<defeats>{defeats}</defeats>");
            sb.Append($"<totalDamageDealt>{totalDamageDealt}</totalDamageDealt>");
            sb.Append($"<totalDamageTaken>{totalDamageTaken}</totalDamageTaken>");
            sb.Append($"<totalGoldEarned>{totalGoldEarned}</totalGoldEarned>");
            sb.Append("</CombatSaveData>");
            return sb.ToString();
        }

        public override void ParseFromXml(XmlElement element)
        {
            totalBattles = int.Parse(element.SelectSingleNode("totalBattles")?.InnerText ?? "0");
            victories = int.Parse(element.SelectSingleNode("victories")?.InnerText ?? "0");
            defeats = int.Parse(element.SelectSingleNode("defeats")?.InnerText ?? "0");
            totalDamageDealt = int.Parse(element.SelectSingleNode("totalDamageDealt")?.InnerText ?? "0");
            totalDamageTaken = int.Parse(element.SelectSingleNode("totalDamageTaken")?.InnerText ?? "0");
            totalGoldEarned = int.Parse(element.SelectSingleNode("totalGoldEarned")?.InnerText ?? "0");
        }
    }

    /// <summary>
    /// 角色技能存档数据
    /// </summary>
    [Serializable]
    public class MemberSkillSaveData
    {
        public int memberId;
        public List<SkillSaveDataLite> skills = new List<SkillSaveDataLite>();

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<MemberSkillSaveData>");
            sb.Append($"<memberId>{memberId}</memberId>");
            sb.Append("<skills>");
            foreach (var skill in skills)
            {
                sb.Append("<SkillSaveDataLite>");
                sb.Append($"<skillId>{XmlEscape.EscapeXml(skill.skillId)}</skillId>");
                sb.Append($"<currentLevel>{skill.currentLevel}</currentLevel>");
                sb.Append("</SkillSaveDataLite>");
            }
            sb.Append("</skills>");
            sb.Append("</MemberSkillSaveData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            memberId = int.Parse(element.SelectSingleNode("memberId")?.InnerText ?? "0");
            var skillsNode = element.SelectSingleNode("skills");
            if (skillsNode != null)
            {
                skills.Clear();
                foreach (XmlElement skillElement in skillsNode.ChildNodes)
                {
                    var skill = new SkillSaveDataLite
                    {
                        skillId = skillElement.SelectSingleNode("skillId")?.InnerText ?? string.Empty,
                        currentLevel = int.Parse(skillElement.SelectSingleNode("currentLevel")?.InnerText ?? "0")
                    };
                    skills.Add(skill);
                }
            }
        }
    }

    /// <summary>
    /// 技能存档数据
    /// </summary>
    [Serializable]
    public class SkillSaveData
    {
        public string skillId;
        public int level;
        public int exp;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<SkillSaveData>");
            sb.Append($"<skillId>{XmlEscape.EscapeXml(skillId)}</skillId>");
            sb.Append($"<level>{level}</level>");
            sb.Append($"<exp>{exp}</exp>");
            sb.Append("</SkillSaveData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            skillId = element.SelectSingleNode("skillId")?.InnerText ?? string.Empty;
            level = int.Parse(element.SelectSingleNode("level")?.InnerText ?? "0");
            exp = int.Parse(element.SelectSingleNode("exp")?.InnerText ?? "0");
        }
    }

    /// <summary>
    /// 背包物品存档数据
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public string templateId;
        public int instanceId;
        public int amount;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<InventorySaveData>");
            sb.Append($"<templateId>{XmlEscape.EscapeXml(templateId)}</templateId>");
            sb.Append($"<instanceId>{instanceId}</instanceId>");
            sb.Append($"<amount>{amount}</amount>");
            sb.Append("</InventorySaveData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            templateId = element.SelectSingleNode("templateId")?.InnerText ?? string.Empty;
            instanceId = int.Parse(element.SelectSingleNode("instanceId")?.InnerText ?? "0");
            amount = int.Parse(element.SelectSingleNode("amount")?.InnerText ?? "0");
        }
    }

    /// <summary>
    /// 队伍成员存档数据
    /// </summary>
    [Serializable]
    public class TeamMemberSaveData
    {
        public int memberId;
        public string actorId;
        public string name;
        public int level;
        public int currentHp;
        public int maxHp;
        public int attack;
        public int defense;
        public float speed;
        public string jobType;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<TeamMemberSaveData>");
            sb.Append($"<memberId>{memberId}</memberId>");
            sb.Append($"<actorId>{XmlEscape.EscapeXml(actorId)}</actorId>");
            sb.Append($"<name>{XmlEscape.EscapeXml(name)}</name>");
            sb.Append($"<level>{level}</level>");
            sb.Append($"<currentHp>{currentHp}</currentHp>");
            sb.Append($"<maxHp>{maxHp}</maxHp>");
            sb.Append($"<attack>{attack}</attack>");
            sb.Append($"<defense>{defense}</defense>");
            sb.Append($"<speed>{speed}</speed>");
            sb.Append($"<jobType>{XmlEscape.EscapeXml(jobType)}</jobType>");
            sb.Append("</TeamMemberSaveData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            memberId = int.Parse(element.SelectSingleNode("memberId")?.InnerText ?? "0");
            actorId = element.SelectSingleNode("actorId")?.InnerText ?? string.Empty;
            name = element.SelectSingleNode("name")?.InnerText ?? string.Empty;
            level = int.Parse(element.SelectSingleNode("level")?.InnerText ?? "0");
            currentHp = int.Parse(element.SelectSingleNode("currentHp")?.InnerText ?? "0");
            maxHp = int.Parse(element.SelectSingleNode("maxHp")?.InnerText ?? "0");
            attack = int.Parse(element.SelectSingleNode("attack")?.InnerText ?? "0");
            defense = int.Parse(element.SelectSingleNode("defense")?.InnerText ?? "0");
            speed = float.Parse(element.SelectSingleNode("speed")?.InnerText ?? "0");
            jobType = element.SelectSingleNode("jobType")?.InnerText ?? string.Empty;
        }
    }

    /// <summary>
    /// 玩家存档数据
    /// </summary>
    [Serializable]
    public class PlayerSaveData : SaveDataBase
    {
        public string actorId;
        public string actorName;
        public int level;
        public int gold;
        public float offlineAccumulatedTime;

        // 模块数据
        public List<InventorySaveData> inventoryItems = new List<InventorySaveData>();
        public List<TeamMemberSaveData> teamMembers = new List<TeamMemberSaveData>();
        public List<MemberSkillSaveData> skillsByMemberId = new List<MemberSkillSaveData>();
        public CombatSaveData combatData = new CombatSaveData();
        public PendingEventSaveData pendingEventData = new PendingEventSaveData();

        public PlayerSaveData()
        {
            inventoryItems = new List<InventorySaveData>();
            teamMembers = new List<TeamMemberSaveData>();
            skillsByMemberId = new List<MemberSkillSaveData>();
            combatData = new CombatSaveData();
        }

        public override string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<player>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<actorId>{XmlEscape.EscapeXml(actorId)}</actorId>");
            sb.Append($"<actorName>{XmlEscape.EscapeXml(actorName)}</actorName>");
            sb.Append($"<level>{level}</level>");
            sb.Append($"<gold>{gold}</gold>");
            sb.Append($"<offlineAccumulatedTime>{offlineAccumulatedTime}</offlineAccumulatedTime>");

            // 背包物品
            sb.Append("<inventoryItems>");
            foreach (var item in inventoryItems)
            {
                sb.Append(item.ToXml());
            }
            sb.Append("</inventoryItems>");

            // 队伍成员
            sb.Append("<teamMembers>");
            foreach (var member in teamMembers)
            {
                sb.Append(member.ToXml());
            }
            sb.Append("</teamMembers>");

            // 技能数据
            sb.Append("<skillsByMemberId>");
            foreach (var skill in skillsByMemberId)
            {
                sb.Append(skill.ToXml());
            }
            sb.Append("</skillsByMemberId>");

            // 战斗数据
            sb.Append("<combatData>");
            sb.Append(combatData.ToXml());
            sb.Append("</combatData>");

            // 积压事件数据
            sb.Append("<pendingEventData>");
            sb.Append(pendingEventData.ToXml());
            sb.Append("</pendingEventData>");

            sb.Append("</player>");
            return sb.ToString();
        }

        public override void ParseFromXml(XmlElement element)
        {
            timestamp = long.Parse(element.SelectSingleNode("timestamp")?.InnerText ?? "0");
            actorId = element.SelectSingleNode("actorId")?.InnerText ?? string.Empty;
            actorName = element.SelectSingleNode("actorName")?.InnerText ?? string.Empty;
            level = int.Parse(element.SelectSingleNode("level")?.InnerText ?? "0");
            gold = int.Parse(element.SelectSingleNode("gold")?.InnerText ?? "0");
            offlineAccumulatedTime = float.Parse(element.SelectSingleNode("offlineAccumulatedTime")?.InnerText ?? "0");

            // 背包物品
            var inventoryNode = element.SelectSingleNode("inventoryItems");
            inventoryItems.Clear();
            if (inventoryNode != null)
            {
                foreach (XmlNode childNode in inventoryNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        var item = new InventorySaveData();
                        item.ParseFromXml((XmlElement)childNode);
                        inventoryItems.Add(item);
                    }
                }
            }

            // 队伍成员
            var teamNode = element.SelectSingleNode("teamMembers");
            teamMembers.Clear();
            if (teamNode != null)
            {
                foreach (XmlNode childNode in teamNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        var member = new TeamMemberSaveData();
                        member.ParseFromXml((XmlElement)childNode);
                        teamMembers.Add(member);
                    }
                }
            }

            // 技能数据
            var skillsNode = element.SelectSingleNode("skillsByMemberId");
            skillsByMemberId.Clear();
            if (skillsNode != null)
            {
                foreach (XmlNode childNode in skillsNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        var skillData = new MemberSkillSaveData();
                        skillData.ParseFromXml((XmlElement)childNode);
                        skillsByMemberId.Add(skillData);
                    }
                }
            }

            // 战斗数据
            var combatNode = element.SelectSingleNode("combatData");
            if (combatNode != null)
            {
                combatData = new CombatSaveData();
                combatData.ParseFromXml((XmlElement)combatNode);
            }

            // 积压事件数据（向后兼容：旧存档可能没有此节点）
            var pendingNode = element.SelectSingleNode("pendingEventData");
            if (pendingNode != null)
            {
                pendingEventData = new PendingEventSaveData();
                pendingEventData.ParseFromXml((XmlElement)pendingNode);
            }
        }
    }

    /// <summary>
    /// 世界存档数据
    /// </summary>
    [Serializable]
    public class WorldSaveData : SaveDataBase
    {
        public int currentNodeIndex;
        public string currentMapSeed;
        public float travelProgress;

        public override string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<world>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<currentNodeIndex>{currentNodeIndex}</currentNodeIndex>");
            sb.Append($"<currentMapSeed>{XmlEscape.EscapeXml(currentMapSeed)}</currentMapSeed>");
            sb.Append($"<travelProgress>{travelProgress}</travelProgress>");
            sb.Append("</world>");
            return sb.ToString();
        }

        public override void ParseFromXml(XmlElement element)
        {
            timestamp = long.Parse(element.SelectSingleNode("timestamp")?.InnerText ?? "0");
            currentNodeIndex = int.Parse(element.SelectSingleNode("currentNodeIndex")?.InnerText ?? "0");
            currentMapSeed = element.SelectSingleNode("currentMapSeed")?.InnerText ?? string.Empty;
            travelProgress = float.Parse(element.SelectSingleNode("travelProgress")?.InnerText ?? "0");
        }
    }

    /// <summary>
    /// 事件树运行存档数据
    /// </summary>
    [Serializable]
    public class EventTreeRunSaveData : SaveDataBase
    {
        public string templateId;
        public string currentNodeId;
        public List<string> history = new List<string>();
        public bool isRunning;

        public EventTreeRunSaveData()
        {
            history = new List<string>();
        }

        public override string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<eventTreeRun>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<templateId>{XmlEscape.EscapeXml(templateId)}</templateId>");
            sb.Append($"<currentNodeId>{XmlEscape.EscapeXml(currentNodeId)}</currentNodeId>");
            sb.Append($"<isRunning>{isRunning}</isRunning>");
            sb.Append("<history>");
            foreach (var h in history)
            {
                sb.Append($"<item>{XmlEscape.EscapeXml(h)}</item>");
            }
            sb.Append("</history>");
            sb.Append("</eventTreeRun>");
            return sb.ToString();
        }

        public override void ParseFromXml(XmlElement element)
        {
            timestamp = long.Parse(element.SelectSingleNode("timestamp")?.InnerText ?? "0");
            templateId = element.SelectSingleNode("templateId")?.InnerText ?? string.Empty;
            currentNodeId = element.SelectSingleNode("currentNodeId")?.InnerText ?? string.Empty;
            isRunning = bool.Parse(element.SelectSingleNode("isRunning")?.InnerText ?? "false");

            var historyNode = element.SelectSingleNode("history");
            history.Clear();
            if (historyNode != null)
            {
                foreach (XmlNode childNode in historyNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element && childNode.Name == "item")
                    {
                        history.Add(childNode.InnerText);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 完整存档数据（已废弃，请使用按职能拆分的 ISaveFile 各实现类）
    /// </summary>
    [Serializable]
    [Obsolete("Use ISaveFile implementations instead (PlayerSaveFile, WorldSaveFile, etc.)")]
    public class GameSaveData : SaveDataBase
    {
        public int version = 1;
        public PlayerSaveData player = new PlayerSaveData();
        public WorldSaveData world = new WorldSaveData();
        public long playTime;
        public int totalInputCount;
        public EventTreeRunSaveData eventTreeRun = new EventTreeRunSaveData();

        public override string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<GameSaveData>");
            sb.Append($"<version>{version}</version>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<playTime>{playTime}</playTime>");
            sb.Append($"<totalInputCount>{totalInputCount}</totalInputCount>");

            // 玩家数据
            sb.Append("<player>");
            sb.Append(player.ToXml());
            sb.Append("</player>");

            // 世界数据
            sb.Append("<world>");
            sb.Append(world.ToXml());
            sb.Append("</world>");

            // 事件树运行数据
            sb.Append("<eventTreeRun>");
            sb.Append(eventTreeRun.ToXml());
            sb.Append("</eventTreeRun>");

            sb.Append("</GameSaveData>");
            return sb.ToString();
        }

        public override void ParseFromXml(XmlElement element)
        {
            version = int.Parse(element.SelectSingleNode("version")?.InnerText ?? "1");
            timestamp = long.Parse(element.SelectSingleNode("timestamp")?.InnerText ?? "0");
            playTime = long.Parse(element.SelectSingleNode("playTime")?.InnerText ?? "0");
            totalInputCount = int.Parse(element.SelectSingleNode("totalInputCount")?.InnerText ?? "0");

            // 玩家数据
            var playerNode = element.SelectSingleNode("player");
            if (playerNode != null)
            {
                player = new PlayerSaveData();
                player.ParseFromXml((XmlElement)playerNode);
            }

            // 世界数据
            var worldNode = element.SelectSingleNode("world");
            if (worldNode != null)
            {
                world = new WorldSaveData();
                world.ParseFromXml((XmlElement)worldNode);
            }

            // 事件树运行数据
            var eventTreeNode = element.SelectSingleNode("eventTreeRun");
            if (eventTreeNode != null)
            {
                eventTreeRun = new EventTreeRunSaveData();
                eventTreeRun.ParseFromXml((XmlElement)eventTreeNode);
            }
        }

        /// <summary>
        /// 解析XML字符串创建GameSaveData
        /// </summary>
        public static GameSaveData ParseFromXmlString(string xmlString)
        {
            if (string.IsNullOrEmpty(xmlString))
                return null;

            var doc = new XmlDocument();
            doc.LoadXml(xmlString);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "GameSaveData")
                return null;

            var saveData = new GameSaveData();
            saveData.ParseFromXml(root);
            return saveData;
        }
    }

    /// <summary>
    /// 增量存档数据（差异包，已废弃）
    /// </summary>
    [Serializable]
    [Obsolete("Incremental save is no longer used. Each ISaveFile is saved independently.")]
    public class IncrementalSaveData
    {
        public long baseTimestamp;
        public long timestamp;
        public List<string> changedSlots = new List<string>();
        public string slotData;

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<IncrementalSaveData>");
            sb.Append($"<baseTimestamp>{baseTimestamp}</baseTimestamp>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append("<changedSlots>");
            foreach (var slot in changedSlots)
            {
                sb.Append($"<slot>{XmlEscape.EscapeXml(slot)}</slot>");
            }
            sb.Append("</changedSlots>");
            sb.Append($"<slotData><![CDATA[{slotData}]]></slotData>");
            sb.Append("</IncrementalSaveData>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            baseTimestamp = long.Parse(element.SelectSingleNode("baseTimestamp")?.InnerText ?? "0");
            timestamp = long.Parse(element.SelectSingleNode("timestamp")?.InnerText ?? "0");

            var slotsNode = element.SelectSingleNode("changedSlots");
            changedSlots.Clear();
            if (slotsNode != null)
            {
                foreach (XmlNode childNode in slotsNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element && childNode.Name == "slot")
                    {
                        changedSlots.Add(childNode.InnerText);
                    }
                }
            }

            slotData = element.SelectSingleNode("slotData")?.InnerText ?? string.Empty;
        }

        public static IncrementalSaveData ParseFromXmlString(string xmlString)
        {
            if (string.IsNullOrEmpty(xmlString))
                return null;

            var doc = new XmlDocument();
            doc.LoadXml(xmlString);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "IncrementalSaveData")
                return null;

            var data = new IncrementalSaveData();
            data.ParseFromXml(root);
            return data;
        }
    }

    /// <summary>
    /// 存档槽位枚举（已废弃，不再使用槽位标记模式）
    /// </summary>
    [Flags]
    [Obsolete("Slot-based dirty tracking is removed. Each ISaveFile is saved independently.")]
    public enum SaveSlot
    {
        None = 0,
        Player = 1 << 0,
        World = 1 << 1,
        EventTree = 1 << 2,
        PlayTime = 1 << 3,
        InputCount = 1 << 4,
        All = Player | World | EventTree | PlayTime | InputCount
    }
}