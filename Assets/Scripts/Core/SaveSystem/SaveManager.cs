using System;
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
    /// 完整存档数据
    /// </summary>
    [Serializable]
    public class GameSaveData : SaveDataBase
    {
        public PlayerSaveData player;
        public WorldSaveData world;
        public long playTime; // 总游玩时间(秒)
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
            // TODO: 实现存档逻辑
            _isDirty = false;
        }

        /// <summary>
        /// 加载存档
        /// </summary>
        public void Load()
        {
            // TODO: 实现读档逻辑
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

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FOLDER, SAVE_FILE);
        }
    }
}
