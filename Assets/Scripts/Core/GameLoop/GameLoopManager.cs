using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 游戏主循环协调器
    /// 主动协调各系统的Tick顺序
    /// </summary>
    public class GameLoopManager : MonoBehaviour
    {
        public static GameLoopManager instance { get; private set; }

        // 系统引用
        private PlayerActor _player;
        private IdleRewardModule _idleModule;
        private TravelManager _travelModule;
        private EventQueue _eventQueue;
        private SaveManager _saveManager;

        // 更新频率
        [SerializeField] private float _tickInterval = 0.1f;
        private float _tickTimer = 0f;

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        public PlayerActor player => _player;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            InitializeSystems();
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer = 0f;
                Tick(_tickInterval);
            }
        }

        /// <summary>
        /// 初始化所有系统
        /// </summary>
        private void InitializeSystems()
        {
            // 1. 创建玩家数据
            _player = new PlayerActor();

            // 2. 初始化各模块并传入PlayerActor引用
            _idleModule = new IdleRewardModule();
            _idleModule.Initialize(_player);

            _travelModule = new TravelManager();
            _travelModule.Initialize(_player);

            _eventQueue = new EventQueue();
            _travelModule.SetEventQueue(_eventQueue);

            _saveManager = new SaveManager();

            // 3. 注册加成模块到PlayerActor
            var bonusModule = new BonusMultiplierModule
            {
                multiplierType = "idle",
                multiplierValue = 0.5f  // 默认提供50%额外挂机加成
            };
            _player.AddModule(bonusModule);
        }

        /// <summary>
        /// 主循环Tick
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 1. 挂机收益
            _idleModule.Tick(deltaTime);

            // 2. 旅行进度
            _travelModule.Tick(deltaTime);

            // 3. 事件处理
            _eventQueue.Tick(deltaTime);

            // 4. 自动存档
            _saveManager.Tick(deltaTime);
        }

        /// <summary>
        /// 获取指定系统
        /// </summary>
        public T GetSystem<T>() where T : class
        {
            if (typeof(T) == typeof(PlayerActor)) return _player as T;
            if (typeof(T) == typeof(IdleRewardModule)) return _idleModule as T;
            if (typeof(T) == typeof(TravelManager)) return _travelModule as T;
            if (typeof(T) == typeof(EventQueue)) return _eventQueue as T;
            if (typeof(T) == typeof(SaveManager)) return _saveManager as T;
            return null;
        }
    }
}
