using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        // TODO: 添加更多玩家数据
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
        public PlayerSaveData player;
        public WorldSaveData world;
        public long playTime; // 总游玩时间(秒)
        public EventTreeRunSaveData eventTreeRun; // 事件树运行状态
    }

    /// <summary>
    /// 存档管理器
    /// </summary>
    public class SaveManager
    {
        private const string SAVE_FOLDER = "Saves";
        private const string SAVE_FILE = "gamesave.json";

        private GameSaveData _currentSave;
        private bool _isDirty = false;
        private float _autoSaveTimer = 0f;
        private float _autoSaveInterval = 300f; // 5分钟

        public GameSaveData currentSave => _currentSave;

        public SaveManager()
        {
            _currentSave = new GameSaveData();
            _currentSave.player = new PlayerSaveData();
            _currentSave.world = new WorldSaveData();
        }

        /// <summary>
        /// 标记数据已修改，需要存档
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 主动保存
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

                var json = JsonUtility.ToJson(_currentSave, true);
                File.WriteAllText(path, json);

                _isDirty = false;
                Debug.Log($"[SaveManager] Game saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save: {ex.Message}");
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
                    return;
                }

                var json = File.ReadAllText(path);
                _currentSave = JsonUtility.FromJson<GameSaveData>(json);

                if (_currentSave == null)
                {
                    Debug.LogWarning("[SaveManager] Failed to parse save file, creating new save");
                    CreateNewSave();
                    return;
                }

                Debug.Log($"[SaveManager] Game loaded from {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load: {ex.Message}");
                CreateNewSave();
            }
        }

        /// <summary>
        /// 创建新游戏存档
        /// </summary>
        public void CreateNewSave()
        {
            // TODO: 初始化新游戏数据
            _currentSave = new GameSaveData();
            _currentSave.player = new PlayerSaveData();
            _currentSave.world = new WorldSaveData();
            _currentSave.timestamp = DateTime.Now.Ticks;
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

            _isDirty = true;
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
            _isDirty = true;
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
    }
}
