using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 玩家角色数据
    /// </summary>
    [Serializable]
    public class PlayerActor
    {
        #region Identity
        public string id;
        public string actorName;
        public int level;
        #endregion

        #region Stats
        [Serializable]
        public class Stats
        {
            public int maxHp = 20;      // 最大生命值
            public int currentHp = 20;  // 当前生命值
            public int attack = 3;      // 攻击力
            public int defense = 5;     // 护甲值
            public float speed = 1f;    // 速度
        }
        public Stats stats = new();
        #endregion

        #region Inventory
        [Serializable]
        public class CarryItems
        {
            public int gold;
            public List<string> ownedModuleIds = new();
            public int maxCapacity = 100;
            public int currentLoad;
        }
        public CarryItems carryItems = new();
        #endregion

        #region State
        /// <summary>
        /// 旅行状态
        /// </summary>
        public TravelState travelState = new();

        /// <summary>
        /// 模块集合
        /// </summary>
        public ModuleCollection modules = new();
        #endregion

        public PlayerActor()
        {
            id = Guid.NewGuid().ToString();
            actorName = "行者";
            level = 1;
        }

        #region Methods
        /// <summary>
        /// 获取总收益加成
        /// </summary>
        public float GetTotalBonus(string bonusType)
        {
            return modules.GetTotalBonus(bonusType);
        }

        /// <summary>
        /// 添加模块
        /// </summary>
        public void AddModule(IModule module)
        {
            modules.AddModule(module);
        }

        /// <summary>
        /// 移除模块
        /// </summary>
        public void RemoveModule(string moduleId)
        {
            modules.RemoveModule(moduleId);
        }

        /// <summary>
        /// 应用事件结果到玩家
        /// </summary>
        public void ApplyEventResult(EventResult result)
        {
            if (result == null) return;

            if (result.isGameOver)
            {
                // 处理游戏结束
                Debug.Log("[PlayerActor] Game Over!");
                return;
            }

            // 应用金币变化
            carryItems.gold += result.goldReward;
            carryItems.gold -= result.goldCost;

            // 确保金币不为负
            if (carryItems.gold < 0) carryItems.gold = 0;

            // 处理模块解锁/移除（预留接口）
            // foreach (var moduleId in result.unlockedModuleIds) { ... }
            // foreach (var moduleId in result.removedModuleIds) { ... }

            Debug.Log($"[PlayerActor] Applied event result: +{result.goldReward}g -{result.goldCost}g, Message: {result.message}");
        }
        #endregion
    }

    /// <summary>
    /// 旅行状态
    /// </summary>
    [Serializable]
    public class TravelState
    {
        public enum State
        {
            Idle,       // 空闲
            Traveling,  // 旅行中
            Arrived,    // 已到达
            EventPending // 事件待处理
        }

        public State currentState = State.Idle;
        public float progress;           // 0~1 进度
        public float realTimeRequired;   // 所需真实时间(秒)
        public string currentLocationId;
        public string nextLocationId;

        /// <summary>
        /// 开始旅行
        /// </summary>
        public void StartTravel(string from, string to, float requiredTime)
        {
            currentState = State.Traveling;
            progress = 0f;
            realTimeRequired = requiredTime;
            currentLocationId = from;
            nextLocationId = to;
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public void UpdateProgress(float deltaTime)
        {
            if (currentState != State.Traveling) return;
            progress += deltaTime / realTimeRequired;
            if (progress >= 1f)
            {
                progress = 1f;
                currentState = State.Arrived;
            }
        }

        /// <summary>
        /// 完成事件后重置
        /// </summary>
        public void Complete()
        {
            currentState = State.Idle;
            progress = 0f;
        }
    }

    /// <summary>
    /// 模块接口
    /// </summary>
    public interface IModule
    {
        string moduleId { get; }
        string moduleName { get; }
        string GetBonus(string bonusType);
        void Tick(float deltaTime);
        void OnActivate();
        void OnDeactivate();
    }

    /// <summary>
    /// 模块集合
    /// </summary>
    [Serializable]
    public class ModuleCollection
    {
        [SerializeField]
        private List<ModuleWrapper> _modules = new();

        [Serializable]
        private class ModuleWrapper
        {
            public string moduleId;
            public IModule module;
        }

        private Dictionary<string, IModule> _moduleDict = new();

        public void AddModule(IModule module)
        {
            if (_moduleDict.ContainsKey(module.moduleId)) return;
            _moduleDict[module.moduleId] = module;
            _modules.Add(new ModuleWrapper { moduleId = module.moduleId, module = module });
            module.OnActivate();
        }

        public void RemoveModule(string moduleId)
        {
            if (!_moduleDict.TryGetValue(moduleId, out var module)) return;
            module.OnDeactivate();
            _moduleDict.Remove(moduleId);
            _modules.RemoveAll(w => w.moduleId == moduleId);
        }

        public float GetTotalBonus(string bonusType)
        {
            float total = 0f;
            foreach (var module in _moduleDict.Values)
            {
                var bonus = module.GetBonus(bonusType);
                if (float.TryParse(bonus, out float value))
                {
                    total += value;
                }
            }
            return total;
        }

        public int Count => _moduleDict.Count;

        /// <summary>
        /// 获取指定类型的模块
        /// </summary>
        public T GetModule<T>() where T : class, IModule
        {
            foreach (var module in _moduleDict.Values)
            {
                if (module is T t)
                    return t;
            }
            return null;
        }
    }
}
