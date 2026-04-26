using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
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
    public class CombatSaveData
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
        public List<InventorySaveData> inventoryItems;   // 背包物品
        public List<TeamMemberData> teamMembers;        // 队伍成员
        public SerializableDictionary<int, List<SkillSaveData>> skillsByMemberId; // 角色技能列表
        public CombatSaveData combatData;               // 战斗统计

        public PlayerSaveData()
        {
            inventoryItems = new List<InventorySaveData>();
            teamMembers = new List<TeamMemberData>();
            skillsByMemberId = new SerializableDictionary<int, List<SkillSaveData>>();
            combatData = new CombatSaveData();
        }
    }

    /// <summary>
    /// 可序列化的Dictionary（用于JsonUtility）
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new();

        [SerializeField]
        private List<TValue> values = new();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                Add(keys[i], values[i]);
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
        // TODO: 添加更多世界数据
    }

    /// <summary>
    /// 事件树运行存档数据
    /// </summary>
    [Serializable]
    public class EventTreeRunSaveData : SaveDataBase
    {
        public string templateId;          // 当前事件树模板ID
        public string currentNodeId;       // 当前节点ID
        public List<string> history;      // 历史记录栈（先进后出）
        public bool isRunning;            // 是否正在运行

        public EventTreeRunSaveData()
        {
            history = new List<string>();
        }
    }

    /// <summary>
    /// 完整存档数据
    /// </summary>
    [Serializable]
    public class GameSaveData : SaveDataBase
    {
        public int version = 1; // 存档版本，用于版本迁移
        public PlayerSaveData player;
        public WorldSaveData world;
        public long playTime; // 总游玩时间(秒)
        public EventTreeRunSaveData eventTreeRun; // 事件树运行状态
    }

    /// <summary>
    /// 增量存档数据（差异包）
    /// </summary>
    [Serializable]
    public class IncrementalSaveData
    {
        public long baseTimestamp;          // 基准时间戳
        public long timestamp;              // 当前时间戳
        public List<string> changedSlots;   // 已变更的槽位列表
        public byte[] compressedData;       // 压缩后的变更数据
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
        private const string SAVE_FOLDER = "Saves";
        private const string SAVE_FILE = "gamesave.json";
        private const string BASE_SAVE_FILE = "gamesave.base.json";
        private const string INCREMENTAL_FILE = "gamesave.inc.json";
        private const int COMPRESSION_LEVEL = 6;

        private GameSaveData _currentSave;
        private GameSaveData _baseSave;           // 基准存档（用于增量计算）
        private bool _isDirty = false;
        private bool _isIncrementalDirty = false;  // 增量存档标记
        private float _autoSaveTimer = 0f;
        private float _autoSaveInterval = 300f; // 5分钟

        private ISaveBackend _backend;
        private SaveSlot _changedSlots = SaveSlot.None; // 变更槽位追踪
        private readonly MigrationManager _migrationManager;

        public GameSaveData currentSave => _currentSave;
        public ISaveBackend backend => _backend;
        public MigrationManager migrationManager => _migrationManager;

        /// <summary>
        /// 设置存档后端（默认使用本地存储）
        /// </summary>
        public void SetBackend(ISaveBackend backend)
        {
            _backend = backend ?? new LocalSaveBackend(Path.Combine(Application.persistentDataPath, SAVE_FOLDER));
        }

        public SaveManager()
        {
            _migrationManager = new MigrationManager();
            _currentSave = new GameSaveData();
            _currentSave.player = new PlayerSaveData();
            _currentSave.world = new WorldSaveData();
            SetBackend(null); // 使用默认本地后端
        }

        /// <summary>
        /// 注册存档迁移处理器
        /// </summary>
        /// <param name="handler">迁移处理器</param>
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
        /// 主动保存（压缩+增量）
        /// </summary>
        public void Save()
        {
            if (_currentSave == null) return;

            try
            {
                _currentSave.timestamp = DateTime.Now.Ticks;

                var path = GetSavePath();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 检查是否需要全量保存（首次或重大变更）
                bool needsFullSave = _baseSave == null || _changedSlots == SaveSlot.All;

                if (needsFullSave)
                {
                    // 全量保存
                    var json = JsonUtility.ToJson(_currentSave, true);
                    var compressed = Compress(Encoding.UTF8.GetBytes(json));
                    File.WriteAllBytes(path, compressed);

                    // 更新基准存档
                    _baseSave = CloneSaveData(_currentSave);
                    SaveBase(); // 保存基准文件

                    Debug.Log($"[SaveManager] Full save compressed to {compressed.Length} bytes");
                }
                else
                {
                    // 增量保存
                    var incData = CreateIncrementalSave();
                    if (incData != null)
                    {
                        var json = JsonUtility.ToJson(incData, true);
                        var compressed = Compress(Encoding.UTF8.GetBytes(json));
                        File.WriteAllBytes(GetIncrementalPath(), compressed);

                        Debug.Log($"[SaveManager] Incremental save compressed to {compressed.Length} bytes");
                    }
                    else
                    {
                        // 无变更，跳过保存
                        Debug.Log("[SaveManager] No changes detected, skipping save");
                    }
                }

                _isDirty = false;
                _isIncrementalDirty = false;
                _changedSlots = SaveSlot.None;

                Debug.Log($"[SaveManager] Game saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save: {ex.Message}");
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
                var json = JsonUtility.ToJson(_currentSave, true);
                var compressed = Compress(Encoding.UTF8.GetBytes(json));

                return await _backend.SaveAsync(SAVE_FILE, compressed);
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

                var json = Decompress(data);
                if (json == null) return false;

                _currentSave = JsonUtility.FromJson<GameSaveData>(Encoding.UTF8.GetString(json));
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
        /// 加载存档（支持压缩+增量）
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
                    return;
                }

                var bytes = File.ReadAllBytes(path);
                var json = Decompress(bytes);

                if (json == null || json.Length == 0)
                {
                    // 尝试直接读取（兼容旧格式）
                    var text = System.Text.Encoding.UTF8.GetString(bytes);
                    _currentSave = JsonUtility.FromJson<GameSaveData>(text);
                }
                else
                {
                    var text = System.Text.Encoding.UTF8.GetString(json);
                    _currentSave = JsonUtility.FromJson<GameSaveData>(text);
                }

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
                        var incBytes = File.ReadAllBytes(incPath);
                        var incJson = Decompress(incBytes);
                        if (incJson != null)
                        {
                            var incData = JsonUtility.FromJson<IncrementalSaveData>(System.Text.Encoding.UTF8.GetString(incJson));
                            if (incData != null)
                            {
                                ApplyIncrementalSave(incData);
                                Debug.Log($"[SaveManager] Applied incremental save with {incData.changedSlots.Count} changes");
                            }
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

                var bytes = File.ReadAllBytes(path);
                var json = Decompress(bytes);

                if (json != null && json.Length > 0)
                {
                    var text = System.Text.Encoding.UTF8.GetString(json);
                    _baseSave = JsonUtility.FromJson<GameSaveData>(text);
                }

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
            if (_isDirty)
            {
                _autoSaveTimer += deltaTime;
                if (_autoSaveTimer >= _autoSaveInterval)
                {
                    Save();
                    _autoSaveTimer = 0f;
                }
            }
        }

        /// <summary>
        /// 使用PlayerActor数据保存存档
        /// </summary>
        /// <param name="player">玩家数据</param>
        /// <param name="offlineTime">离线时间（秒）</param>
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
        /// <param name="deltaTime">时间增量</param>
        /// <param name="player">玩家数据（用于实时同步）</param>
        /// <param name="offlineTime">离线时间</param>
        public void TickWithPlayer(float deltaTime, PlayerActor player, float offlineTime)
        {
            // 实时同步玩家数据到存档（不频繁保存，只更新状态）
            if (player != null && _currentSave != null)
            {
                // 每帧更新offlineAccumulatedTime
                _currentSave.player.offlineAccumulatedTime = offlineTime;
            }

            // 自动存档检测
            if (_isDirty)
            {
                _autoSaveTimer += deltaTime;
                if (_autoSaveTimer >= _autoSaveInterval)
                {
                    Save();
                    _autoSaveTimer = 0f;
                }
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, SAVE_FILE);
        }

        private string GetBasePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, BASE_SAVE_FILE);
        }

        private string GetIncrementalPath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, INCREMENTAL_FILE);
        }

        /// <summary>
        /// 创建增量存档
        /// </summary>
        private IncrementalSaveData CreateIncrementalSave()
        {
            if (_baseSave == null || _changedSlots == SaveSlot.None)
                return null;

            // 构建变更数据字典
            var changes = new Dictionary<string, object>();

            if ((_changedSlots & SaveSlot.Player) != 0)
                changes["player"] = _currentSave.player;

            if ((_changedSlots & SaveSlot.World) != 0)
                changes["world"] = _currentSave.world;

            if ((_changedSlots & SaveSlot.EventTree) != 0)
                changes["eventTreeRun"] = _currentSave.eventTreeRun;

            if ((_changedSlots & SaveSlot.PlayTime) != 0)
                changes["playTime"] = _currentSave.playTime;

            if (changes.Count == 0)
                return null;

            var changeJson = JsonUtility.ToJson(new SerializationProxy(changes), false);
            var compressed = Compress(Encoding.UTF8.GetBytes(changeJson));

            var incData = new IncrementalSaveData
            {
                baseTimestamp = _baseSave.timestamp,
                timestamp = _currentSave.timestamp,
                changedSlots = new List<string>(),
                compressedData = compressed
            };

            if ((_changedSlots & SaveSlot.Player) != 0) incData.changedSlots.Add("player");
            if ((_changedSlots & SaveSlot.World) != 0) incData.changedSlots.Add("world");
            if ((_changedSlots & SaveSlot.EventTree) != 0) incData.changedSlots.Add("eventTreeRun");
            if ((_changedSlots & SaveSlot.PlayTime) != 0) incData.changedSlots.Add("playTime");

            return incData;
        }

        /// <summary>
        /// 从增量存档恢复
        /// </summary>
        private bool ApplyIncrementalSave(IncrementalSaveData incData)
        {
            if (_baseSave == null || incData.compressedData == null)
                return false;

            try
            {
                var json = Decompress(incData.compressedData);
                if (json == null) return false;

                var proxy = JsonUtility.FromJson<SerializationProxy>(Encoding.UTF8.GetString(json));
                if (proxy == null || proxy.data == null) return false;

                // 应用变更
                foreach (var slot in incData.changedSlots)
                {
                    if (proxy.data.TryGetValue(slot, out var value))
                    {
                        switch (slot)
                        {
                            case "player":
                                _currentSave.player = value as PlayerSaveData;
                                break;
                            case "world":
                                _currentSave.world = value as WorldSaveData;
                                break;
                            case "eventTreeRun":
                                _currentSave.eventTreeRun = value as EventTreeRunSaveData;
                                break;
                            case "playTime":
                                if (value is long pt) _currentSave.playTime = pt;
                                else if (value is int pti) _currentSave.playTime = pti;
                                break;
                        }
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
                var json = JsonUtility.ToJson(_baseSave, true);
                var compressed = Compress(Encoding.UTF8.GetBytes(json));
                File.WriteAllBytes(path, compressed);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save base: {ex.Message}");
            }
        }

        /// <summary>
        /// 压缩数据
        /// </summary>
        private byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, System.IO.Compression.CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// 解压数据
        /// </summary>
        private byte[] Decompress(byte[] data)
        {
            try
            {
                using (var input = new MemoryStream(data))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Decompress failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 深度克隆存档数据
        /// </summary>
        private GameSaveData CloneSaveData(GameSaveData source)
        {
            var json = JsonUtility.ToJson(source, false);
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        /// <summary>
        /// 序列化代理类（用于Dictionary转Json）
        /// </summary>
        [Serializable]
        private class SerializationProxy
        {
            public Dictionary<string, object> data;

            public SerializationProxy(Dictionary<string, object> data)
            {
                this.data = data;
            }
        }
    }
}
