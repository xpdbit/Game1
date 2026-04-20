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
            public int maxHp = 100;
            public int currentHp = 100;
            public int attack = 10;
            public int defense = 5;
            public float speed = 1f;
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
        void OnTick(float deltaTime);
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
    }
}
