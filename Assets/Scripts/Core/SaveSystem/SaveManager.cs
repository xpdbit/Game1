using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using Game1.Modules.Combat;

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
    /// 存档版本迁移处理器接口
    /// </summary>
    public interface IMigrationHandler
    {
        /// <summary>
        /// 处理迁移的目标版本
        /// </summary>
        int TargetVersion { get; }

        /// <summary>
        /// 从哪个版本迁移过来
        /// </summary>
        int SourceVersion { get; }

        /// <summary>
        /// 执行迁移
        /// </summary>
        /// <param name="saveData">要迁移的存档数据</param>
        /// <returns>迁移后的存档数据</returns>
        GameSaveData Migrate(GameSaveData saveData);
    }

    /// <summary>
    /// 存档版本迁移管理器
    /// </summary>
    public class MigrationManager
    {
        private readonly List<IMigrationHandler> _handlers = new();

        /// <summary>
        /// 当前存档版本（最新版本号）
        /// </summary>
        public int CurrentVersion { get; private set; } = 1;

        /// <summary>
        /// 注册迁移处理器
        /// </summary>
        public void RegisterHandler(IMigrationHandler handler)
        {
            if (handler == null) return;

            // 按源版本排序，确保按顺序执行
            var insertIndex = _handlers.FindIndex(h => h.SourceVersion >= handler.SourceVersion);
            if (insertIndex < 0)
                _handlers.Add(handler);
            else
                _handlers.Insert(insertIndex, handler);

            // 更新当前版本为最大目标版本
            if (handler.TargetVersion > CurrentVersion)
                CurrentVersion = handler.TargetVersion;
        }

        /// <summary>
        /// 迁移存档数据到最新版本
        /// </summary>
        /// <param name="saveData">原始存档数据</param>
        /// <returns>迁移后的存档数据</returns>
        public GameSaveData MigrateToLatest(GameSaveData saveData)
        {
            if (saveData == null) return null;

            var currentVersion = saveData.version;
            if (currentVersion >= CurrentVersion)
            {
                // 无需迁移或已是最新版本
                return saveData;
            }

            var result = saveData;
            foreach (var handler in _handlers)
            {
                if (handler.SourceVersion == currentVersion)
                {
                    result = handler.Migrate(result);
                    currentVersion = handler.TargetVersion;
                    Debug.Log($"[MigrationManager] Migrated from v{handler.SourceVersion} to v{handler.TargetVersion}");

                    // 如果已达到目标版本，停止迁移
                    if (currentVersion >= CurrentVersion)
                        break;
                }
            }

            // 确保版本号正确
            result.version = CurrentVersion;
            return result;
        }

        /// <summary>
        /// 检测存档版本
        /// </summary>
        /// <param name="saveData">存档数据</param>
        /// <returns>存档版本，如果为0或不存在则返回0（未知版本）</returns>
        public int DetectVersion(GameSaveData saveData)
        {
            return saveData?.version ?? 0;
        }
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
        public List<SkillSaveData> skills = new List<SkillSaveData>();

        public string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<MemberSkillSaveData>");
            sb.Append($"<memberId>{memberId}</memberId>");
            sb.Append("<skills>");
            foreach (var skill in skills)
            {
                sb.Append(skill.ToXml());
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
                foreach (XmlNode childNode in skillsNode.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        var skill = new SkillSaveData();
                        skill.ParseFromXml((XmlElement)childNode);
                        skills.Add(skill);
                    }
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
            sb.Append($"<skillId>{EscapeXml(skillId)}</skillId>");
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
            sb.Append($"<templateId>{EscapeXml(templateId)}</templateId>");
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
    public class TeamMemberData
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
            sb.Append("<TeamMemberData>");
            sb.Append($"<memberId>{memberId}</memberId>");
            sb.Append($"<actorId>{EscapeXml(actorId)}</actorId>");
            sb.Append($"<name>{EscapeXml(name)}</name>");
            sb.Append($"<level>{level}</level>");
            sb.Append($"<currentHp>{currentHp}</currentHp>");
            sb.Append($"<maxHp>{maxHp}</maxHp>");
            sb.Append($"<attack>{attack}</attack>");
            sb.Append($"<defense>{defense}</defense>");
            sb.Append($"<speed>{speed}</speed>");
            sb.Append($"<jobType>{EscapeXml(jobType)}</jobType>");
            sb.Append("</TeamMemberData>");
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
        public List<TeamMemberData> teamMembers = new List<TeamMemberData>();
        public List<MemberSkillSaveData> skillsByMemberId = new List<MemberSkillSaveData>();
        public CombatSaveData combatData = new CombatSaveData();

        public PlayerSaveData()
        {
            inventoryItems = new List<InventorySaveData>();
            teamMembers = new List<TeamMemberData>();
            skillsByMemberId = new List<MemberSkillSaveData>();
            combatData = new CombatSaveData();
        }

        public override string ToXml()
        {
            var sb = new StringBuilder();
            sb.Append("<PlayerSaveData>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<actorId>{EscapeXml(actorId)}</actorId>");
            sb.Append($"<actorName>{EscapeXml(actorName)}</actorName>");
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

            sb.Append("</PlayerSaveData>");
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
                        var member = new TeamMemberData();
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
            sb.Append("<WorldSaveData>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<currentNodeIndex>{currentNodeIndex}</currentNodeIndex>");
            sb.Append($"<currentMapSeed>{EscapeXml(currentMapSeed)}</currentMapSeed>");
            sb.Append($"<travelProgress>{travelProgress}</travelProgress>");
            sb.Append("</WorldSaveData>");
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
            sb.Append("<EventTreeRunSaveData>");
            sb.Append($"<timestamp>{timestamp}</timestamp>");
            sb.Append($"<templateId>{EscapeXml(templateId)}</templateId>");
            sb.Append($"<currentNodeId>{EscapeXml(currentNodeId)}</currentNodeId>");
            sb.Append($"<isRunning>{isRunning}</isRunning>");
            sb.Append("<history>");
            foreach (var h in history)
            {
                sb.Append($"<item>{EscapeXml(h)}</item>");
            }
            sb.Append("</history>");
            sb.Append("</EventTreeRunSaveData>");
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
    /// 完整存档数据
    /// </summary>
    [Serializable]
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
    /// 增量存档数据（差异包）
    /// </summary>
    [Serializable]
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
                sb.Append($"<slot>{EscapeXml(slot)}</slot>");
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
    /// 存档槽位枚举
    /// </summary>
    [Flags]
    public enum SaveSlot
    {
        None = 0,
        Player = 1 << 0,
        World = 1 << 1,
        EventTree = 1 << 2,
        PlayTime = 1 << 3,
        All = Player | World | EventTree | PlayTime
    }

    /// <summary>
    /// 云存档后端接口
    /// </summary>
    public interface ISaveBackend
    {
        /// <summary>
        /// 异步保存存档
        /// </summary>
        System.Threading.Tasks.Task<bool> SaveAsync(string slotId, byte[] data);

        /// <summary>
        /// 异步加载存档
        /// </summary>
        System.Threading.Tasks.Task<byte[]> LoadAsync(string slotId);

        /// <summary>
        /// 检查云端存档是否存在
        /// </summary>
        System.Threading.Tasks.Task<bool> ExistsAsync(string slotId);

        /// <summary>
        /// 删除云端存档
        /// </summary>
        System.Threading.Tasks.Task<bool> DeleteAsync(string slotId);
    }

    /// <summary>
    /// 本地存档后端（默认实现）
    /// </summary>
    public class LocalSaveBackend : ISaveBackend
    {
        private readonly string _basePath;

        public LocalSaveBackend(string basePath)
        {
            _basePath = basePath;
        }

        public async System.Threading.Tasks.Task<bool> SaveAsync(string slotId, byte[] data)
        {
            try
            {
                var path = Path.Combine(_basePath, slotId);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(path, data));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveBackend] Save failed: {ex.Message}");
                return false;
            }
        }

        public async System.Threading.Tasks.Task<byte[]> LoadAsync(string slotId)
        {
            try
            {
                var path = Path.Combine(_basePath, slotId);
                if (!File.Exists(path))
                    return null;

                return await System.Threading.Tasks.Task.Run(() => File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveBackend] Load failed: {ex.Message}");
                return null;
            }
        }

        public async System.Threading.Tasks.Task<bool> ExistsAsync(string slotId)
        {
            var path = Path.Combine(_basePath, slotId);
            return await System.Threading.Tasks.Task.Run(() => File.Exists(path));
        }

        public async System.Threading.Tasks.Task<bool> DeleteAsync(string slotId)
        {
            try
            {
                var path = Path.Combine(_basePath, slotId);
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveBackend] Delete failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 存档管理器
    /// </summary>
    public class SaveManager
    {
        private const string SAVE_FILE = "GameSave.xml";
        private const string BASE_SAVE_FILE = "GameSave.base.xml";
        private const string INCREMENTAL_FILE = "GameSave.inc.xml";

        private GameSaveData _currentSave;
        private GameSaveData _baseSave;
        private bool _isDirty = false;
        private bool _isIncrementalDirty = false;
        private float _autoSaveTimer = 0f;
        private float _autoSaveInterval = 1f;

        private ISaveBackend _backend;
        private SaveSlot _changedSlots = SaveSlot.None;
        private readonly MigrationManager _migrationManager;

        public GameSaveData currentSave => _currentSave;
        public ISaveBackend backend => _backend;
        public MigrationManager migrationManager => _migrationManager;

        /// <summary>
        /// 设置存档后端（默认使用本地存储）
        /// </summary>
        public void SetBackend(ISaveBackend backend)
        {
            _backend = backend ?? new LocalSaveBackend(Path.Combine(Application.persistentDataPath, "Saves"));
        }

        public SaveManager()
        {
            _migrationManager = new MigrationManager();
            _currentSave = new GameSaveData();
            _currentSave.player = new PlayerSaveData();
            _currentSave.world = new WorldSaveData();
            SetBackend(null);
        }

        /// <summary>
        /// 注册存档迁移处理器
        /// </summary>
        public void RegisterMigrationHandler(IMigrationHandler handler)
        {
            _migrationManager.RegisterHandler(handler);
        }

        /// <summary>
        /// 标记指定槽位已修改
        /// </summary>
        public void MarkSlotDirty(SaveSlot slot)
        {
            _changedSlots |= slot;
            _isDirty = true;
            _isIncrementalDirty = true;
        }

        /// <summary>
        /// 标记数据已修改，需要存档
        /// </summary>
        public void MarkDirty()
        {
            _changedSlots = SaveSlot.All;
            _isDirty = true;
            _isIncrementalDirty = true;
        }

        /// <summary>
        /// 主动保存（逐一转换XML）
        /// </summary>
        public void Save()
        {
            if (_currentSave == null)
            {
                Debug.LogError("[SaveManager] Save called but _currentSave is null");
                return;
            }

            try
            {
                _currentSave.timestamp = DateTime.Now.Ticks;

                // 填充世界存档数据
                if (string.IsNullOrEmpty(_currentSave.world?.currentMapSeed)
                    && Game1.Modules.Travel.TravelManager.instance != null)
                {
                    var worldData = Game1.Modules.Travel.TravelManager.instance.ExportToSaveData();
                    if (worldData != null)
                    {
                        _currentSave.world = worldData;
                        _changedSlots |= SaveSlot.World;
                    }
                }

                // 检查并修复潜在的问题
                if (_currentSave.player == null)
                {
                    Debug.LogWarning("[SaveManager] _currentSave.player is null, creating new PlayerSaveData");
                    _currentSave.player = new PlayerSaveData();
                }

                // 确保所有列表初始化
                if (_currentSave.player.inventoryItems == null)
                    _currentSave.player.inventoryItems = new List<InventorySaveData>();
                if (_currentSave.player.teamMembers == null)
                    _currentSave.player.teamMembers = new List<TeamMemberData>();
                if (_currentSave.player.skillsByMemberId == null)
                    _currentSave.player.skillsByMemberId = new List<MemberSkillSaveData>();
                if (_currentSave.player.combatData == null)
                    _currentSave.player.combatData = new CombatSaveData();

                var path = GetSavePath();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 检查是否需要全量保存
                bool needsFullSave = _baseSave == null || _changedSlots == SaveSlot.All;

                if (needsFullSave)
                {
                    // 全量保存 - 使用逐一转换的ToXml方法
                    string xmlContent = _currentSave.ToXml();
                    File.WriteAllText(path, xmlContent, Encoding.UTF8);

                    // 更新基准存档
                    _baseSave = CloneSaveData(_currentSave);
                    SaveBase();
                }
                else
                {
                    // 增量保存
                    var incData = CreateIncrementalSave();
                    if (incData != null)
                    {
                        File.WriteAllText(GetIncrementalPath(), incData.ToXml(), Encoding.UTF8);
                    }
                    else
                    {
                        Debug.Log("[SaveManager] No changes detected, skipping save");
                    }
                }

                _isDirty = false;
                _isIncrementalDirty = false;
                _changedSlots = SaveSlot.None;
            }
            catch (Exception ex)
            {
                var msg = $"[SaveManager] Failed to save: {ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                    msg += $"\n  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n  Inner StackTrace: {ex.InnerException?.StackTrace}";
                Debug.LogError(msg);
            }
        }

        /// <summary>
        /// 异步保存到云端
        /// </summary>
        public async System.Threading.Tasks.Task<bool> SaveToCloudAsync()
        {
            if (_currentSave == null) return false;

            try
            {
                _currentSave.timestamp = DateTime.Now.Ticks;
                var xmlBytes = Encoding.UTF8.GetBytes(_currentSave.ToXml());
                return await _backend.SaveAsync(SAVE_FILE, xmlBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Cloud save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步从云端加载
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LoadFromCloudAsync()
        {
            try
            {
                var data = await _backend.LoadAsync(SAVE_FILE);
                if (data == null || data.Length == 0)
                {
                    Debug.Log("[SaveManager] No cloud save found");
                    return false;
                }

                var xmlString = Encoding.UTF8.GetString(data);
                _currentSave = GameSaveData.ParseFromXmlString(xmlString);

                if (_currentSave == null)
                {
                    Debug.LogWarning("[SaveManager] Failed to parse cloud save");
                    return false;
                }

                _baseSave = CloneSaveData(_currentSave);
                Debug.Log("[SaveManager] Cloud save loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Cloud load failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载存档
        /// </summary>
        public void Load()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[SaveManager] No save file found, creating new save");
                    CreateNewSave();
                    Save();
                    return;
                }

                // 使用逐一转换的ParseFromXmlString方法
                string xmlContent = File.ReadAllText(path, Encoding.UTF8);
                _currentSave = GameSaveData.ParseFromXmlString(xmlContent);

                if (_currentSave == null)
                {
                    Debug.LogWarning("[SaveManager] Failed to parse save file, creating new save");
                    CreateNewSave();
                    return;
                }

                // 加载基准存档
                LoadBase();

                // 检查并应用增量存档
                var incPath = GetIncrementalPath();
                if (File.Exists(incPath))
                {
                    try
                    {
                        var incXmlContent = File.ReadAllText(incPath, Encoding.UTF8);
                        var incData = IncrementalSaveData.ParseFromXmlString(incXmlContent);
                        if (incData != null)
                        {
                            ApplyIncrementalSave(incData);
                            Debug.Log($"[SaveManager] Applied incremental save with {incData.changedSlots.Count} changes");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveManager] Failed to apply incremental save: {ex.Message}");
                    }
                }

                // 检测并执行版本迁移
                var loadedVersion = _migrationManager.DetectVersion(_currentSave);
                if (loadedVersion < _migrationManager.CurrentVersion)
                {
                    Debug.Log($"[SaveManager] Save version {loadedVersion} < current {_migrationManager.CurrentVersion}, migrating...");
                    _currentSave = _migrationManager.MigrateToLatest(_currentSave);
                    if (_currentSave == null)
                    {
                        Debug.LogWarning("[SaveManager] Migration failed, creating new save");
                        CreateNewSave();
                        return;
                    }
                    Debug.Log($"[SaveManager] Migration completed, now at version {_currentSave.version}");
                }

                // 更新基准存档
                _baseSave = CloneSaveData(_currentSave);

                Debug.Log($"[SaveManager] Game loaded from {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load: {ex.Message}");
                CreateNewSave();
            }
        }

        /// <summary>
        /// 加载基准存档
        /// </summary>
        private void LoadBase()
        {
            try
            {
                var path = GetBasePath();
                if (!File.Exists(path))
                {
                    _baseSave = CloneSaveData(_currentSave);
                    return;
                }

                var xmlContent = File.ReadAllText(path, Encoding.UTF8);
                _baseSave = GameSaveData.ParseFromXmlString(xmlContent);

                if (_baseSave == null)
                    _baseSave = CloneSaveData(_currentSave);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to load base save: {ex.Message}");
                _baseSave = CloneSaveData(_currentSave);
            }
        }

        /// <summary>
        /// 创建新游戏存档
        /// </summary>
        public void CreateNewSave()
        {
            _currentSave = new GameSaveData();
            _currentSave.player = new PlayerSaveData();
            _currentSave.world = new WorldSaveData();
            _currentSave.timestamp = DateTime.Now.Ticks;
            _currentSave.version = _migrationManager.CurrentVersion;
        }

        /// <summary>
        /// Tick (自动存档检测)
        /// </summary>
        public void Tick(float deltaTime)
        {
            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                Save();
                _autoSaveTimer = 0f;
            }
        }

        /// <summary>
        /// 使用PlayerActor数据保存存档
        /// </summary>
        public void SaveWithPlayer(PlayerActor player, float offlineTime)
        {
            if (player == null) return;

            if (_currentSave == null)
            {
                _currentSave = new GameSaveData();
                _currentSave.player = new PlayerSaveData();
                _currentSave.world = new WorldSaveData();
            }

            // 导出玩家数据
            _currentSave.player = player.ExportToSaveData();
            _currentSave.player.offlineAccumulatedTime = offlineTime;
            _currentSave.timestamp = DateTime.Now.Ticks;

            // 填充世界存档数据
            var worldData = Game1.Modules.Travel.TravelManager.instance.ExportToSaveData();
            if (worldData != null)
            {
                _currentSave.world = worldData;
                _changedSlots |= SaveSlot.World;
            }

            MarkSlotDirty(SaveSlot.Player | SaveSlot.PlayTime);
            Save();
        }

        /// <summary>
        /// 保存事件树运行数据
        /// </summary>
        public void SetEventTreeRunData(EventTreeRunSaveData data)
        {
            if (_currentSave == null)
            {
                _currentSave = new GameSaveData();
                _currentSave.player = new PlayerSaveData();
                _currentSave.world = new WorldSaveData();
            }

            _currentSave.eventTreeRun = data;
            MarkSlotDirty(SaveSlot.EventTree);
        }

        /// <summary>
        /// 获取事件树运行数据
        /// </summary>
        public EventTreeRunSaveData GetEventTreeRunData()
        {
            return _currentSave?.eventTreeRun;
        }

        /// <summary>
        /// 带PlayerActor数据的Tick
        /// </summary>
        public void TickWithPlayer(float deltaTime, PlayerActor player, float offlineTime)
        {
            if (player != null && _currentSave != null)
            {
                _currentSave.player.offlineAccumulatedTime = offlineTime;
            }
            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                Save();
                _autoSaveTimer = 0f;
            }
        }

        private string GetSavePath()
        {
            string exeDir = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(exeDir, "Save", SAVE_FILE);
        }

        private string GetBasePath()
        {
            string exeDir = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(exeDir, "Save", BASE_SAVE_FILE);
        }

        private string GetIncrementalPath()
        {
            string exeDir = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(exeDir, "Save", INCREMENTAL_FILE);
        }

        /// <summary>
        /// 创建增量存档
        /// </summary>
        private IncrementalSaveData CreateIncrementalSave()
        {
            if (_baseSave == null || _changedSlots == SaveSlot.None)
                return null;

            var incData = new IncrementalSaveData
            {
                baseTimestamp = _baseSave.timestamp,
                timestamp = _currentSave.timestamp,
                changedSlots = new List<string>()
            };

            var slotDataBuilder = new StringBuilder();

            if ((_changedSlots & SaveSlot.Player) != 0)
            {
                incData.changedSlots.Add("player");
                slotDataBuilder.Append(_currentSave.player.ToXml());
            }

            if ((_changedSlots & SaveSlot.World) != 0)
            {
                incData.changedSlots.Add("world");
                slotDataBuilder.Append(_currentSave.world.ToXml());
            }

            if ((_changedSlots & SaveSlot.EventTree) != 0)
            {
                incData.changedSlots.Add("eventTreeRun");
                slotDataBuilder.Append(_currentSave.eventTreeRun.ToXml());
            }

            if ((_changedSlots & SaveSlot.PlayTime) != 0)
            {
                incData.changedSlots.Add("playTime");
                slotDataBuilder.Append($"<playTime>{_currentSave.playTime}</playTime>");
            }

            if (incData.changedSlots.Count == 0)
                return null;

            incData.slotData = slotDataBuilder.ToString();
            return incData;
        }

        /// <summary>
        /// 从增量存档恢复
        /// </summary>
        private bool ApplyIncrementalSave(IncrementalSaveData incData)
        {
            if (_baseSave == null || string.IsNullOrEmpty(incData.slotData))
                return false;

            try
            {
                // 解析增量数据中的XML片段
                var doc = new XmlDocument();
                doc.LoadXml(incData.slotData);

                foreach (var slot in incData.changedSlots)
                {
                    var node = doc.SelectSingleNode($"//{slot}");
                    if (node == null) continue;

                    switch (slot)
                    {
                        case "player":
                            _currentSave.player = new PlayerSaveData();
                            _currentSave.player.ParseFromXml((XmlElement)node);
                            break;
                        case "world":
                            _currentSave.world = new WorldSaveData();
                            _currentSave.world.ParseFromXml((XmlElement)node);
                            break;
                        case "eventTreeRun":
                            _currentSave.eventTreeRun = new EventTreeRunSaveData();
                            _currentSave.eventTreeRun.ParseFromXml((XmlElement)node);
                            break;
                        case "playTime":
                            _currentSave.playTime = long.Parse(node.InnerText);
                            break;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Apply incremental save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存基准存档
        /// </summary>
        private void SaveBase()
        {
            try
            {
                var path = GetBasePath();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(path, _baseSave.ToXml(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save base: {ex.Message}");
            }
        }

        /// <summary>
        /// 深度克隆存档数据
        /// </summary>
        private GameSaveData CloneSaveData(GameSaveData source)
        {
            if (source == null) return null;
            var xmlString = source.ToXml();
            return GameSaveData.ParseFromXmlString(xmlString);
        }

        /// <summary>
        /// XML特殊字符转义
        /// </summary>
        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
