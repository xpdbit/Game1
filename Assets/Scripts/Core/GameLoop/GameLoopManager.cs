using UnityEngine;
using VContainer;
using Game1.Core.GameLoop;
using Game1.Modules.Travel;
using Game1.Modules.Combat;
using Game1.Modules.Activity;

namespace Game1
{
    /// <summary>
    /// 游戏主循环协调器
    /// 主动协调各系统的Tick顺序
    /// 实现IGameRunner接口以支持VContainer DI
    /// </summary>
    public class GameLoopManager : MonoBehaviour, IGameRunner
    {
        public static GameLoopManager instance { get; private set; }

        // 系统引用
        private PlayerActor _player;
        private IdleRewardModule _idleModule;
        private TravelManager _travelModule;
        private EventQueue _eventQueue;
        private SaveManager _saveManager;
        private BackgroundInputManager _backgroundInput;
        private CombatModule _combatModule;
        private ActivityMonitorModule _activityModule;
        private float _totalGameTime;

        // 更新频率
        [SerializeField] private float _tickInterval = 0.1f;
        private float _tickTimer = 0f;

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        public PlayerActor player => _player;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 实现IGameRunner接口的Initialize方法
        /// </summary>
        public void Initialize()
        {
            InitializeSystems();
            IsInitialized = true;
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

        private void OnDisable()
        {
            // 当组件被禁用或场景改变时也要清理，防止残留钩子
            Debug.Log("[GameLoopManager] OnDisable - disposing BackgroundInputManager");
            _backgroundInput?.Dispose();
            GlobalKeyboardHook.ForceReset();
        }

        private void OnDestroy()
        {
            Debug.Log("[GameLoopManager] Saving on quit");
            // 保存时传入离线时间用于下次加载计算离线收益
            float offlineTime = _idleModule?.accumulatedTime ?? 0f;
            _saveManager?.SaveWithPlayer(_player, offlineTime);

            // 清理后台输入系统（关键：确保Windows键盘钩子被正确卸载）
            Debug.Log("[GameLoopManager] Disposing BackgroundInputManager");
            _backgroundInput?.Dispose();

            // 强制重置GlobalKeyboardHook单例（确保域重新加载后能正常初始化）
            GlobalKeyboardHook.ForceReset();
            Debug.Log("[GameLoopManager] Force reset GlobalKeyboardHook singleton");
        }

        private void OnApplicationQuit()
        {
            Debug.Log("[GameLoopManager] Application quitting - ensuring cleanup");
            // 确保在应用程序退出时也进行清理
            _backgroundInput?.Dispose();
            GlobalKeyboardHook.ForceReset();
        }

        /// <summary>
        /// 初始化所有系统
        /// </summary>
        private void InitializeSystems()
        {
            // 0. 初始化后台输入系统（必须在其他系统之前，以便接收输入事件）
            _backgroundInput = BackgroundInputManager.instance;
            _backgroundInput.Initialize();

            // 1. 创建玩家数据
            _player = new PlayerActor();

            // 2. 初始化各模块并传入PlayerActor引用
            _idleModule = new IdleRewardModule();
            _idleModule.Initialize(_player);

            // 2.1 初始化活跃度监控模块
            _activityModule = ActivityMonitorModule.instance;
            _activityModule.Initialize(_player);
            _player.AddModule(_activityModule);

            // 3. 使用共享的TravelManager单例
            _travelModule = TravelManager.instance;
            _travelModule.Initialize(_player);

            // 3.1 初始化PrestigeManager并传入PlayerActor引用
            PrestigeManager.instance.SetPlayerActor(_player);
            PrestigeManager.instance.Initialize();

            _eventQueue = new EventQueue();
            _travelModule.SetEventQueue(_eventQueue);

            _saveManager = GameMain.instance.Container.Resolve<SaveManager>();

            // 4. 尝试加载存档（如果没有则创建新存档）
            _saveManager.Load();

            // 5. 将存档数据应用到PlayerActor（关键：修复双实例不同步问题）
            if (_saveManager.currentSave != null && _saveManager.currentSave.player != null)
            {
                _player.ApplyFromSaveData(_saveManager.currentSave.player);
                Debug.Log($"[GameLoopManager] Applied save data to PlayerActor: {_player.id}");
            }
            else
            {
                Debug.Log("[GameLoopManager] No save data to apply, using default PlayerActor");
            }

            // 恢复游戏时间
            _totalGameTime = _saveManager.currentSave?.playTime ?? 0f;

            // 6. 注册加成模块到PlayerActor
            var bonusModule = new BonusMultiplierModule
            {
                multiplierType = "idle",
                multiplierValue = 0.5f  // 默认提供50%额外挂机加成
            };
            _player.AddModule(bonusModule);

            // 7. 注册战斗模块到PlayerActor
            _combatModule = new CombatModule();
            _combatModule.Initialize(_player);
            _player.AddModule(_combatModule);

            // 7. 计算并应用离线收益（如果有存档且非首次新建）
            if (_saveManager.currentSave != null && _saveManager.currentSave.player != null)
            {
                float offlineTime = _saveManager.currentSave.player.offlineAccumulatedTime;
                if (offlineTime > 0)
                {
                    _idleModule.ApplyOfflineReward(offlineTime);
                    Debug.Log($"[GameLoopManager] Applied offline reward for {offlineTime / 3600f:F1} hours");
                }
            }
        }

        /// <summary>
        /// 主循环Tick (无参数版本，由IGameRunner接口使用)
        /// </summary>
        void IGameRunner.Tick()
        {
            Tick(_tickInterval);
        }

        /// <summary>
        /// 主循环Tick
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 0. 更新后台输入系统（必须首先更新以确保输入事件及时处理）
            _backgroundInput?.Update();

            // 1. 挂机收益
            _idleModule.Tick(deltaTime);

            // 1.1 活跃度监控
            _activityModule.Tick(deltaTime);

            // 2. 旅行进度
            _travelModule.Tick(deltaTime);

            // 3. 事件处理
            _eventQueue.Tick(deltaTime);

            // 4. 更新游戏时间和输入次数
            _totalGameTime += deltaTime;
            _saveManager.currentSave.playTime = (long)_totalGameTime;
            var (totalKeystrokes, _, _) = _backgroundInput?.GetInputStatistics() ?? (0, 0, 1f);
            _saveManager.currentSave.totalInputCount = totalKeystrokes;

            // 5. 自动存档（同步PlayerActor数据，offlineTime只在退出时保存）
            _saveManager.TickWithPlayer(deltaTime, _player, 0f);

            // 6. 更新调试信息显示
            GameDebug.instance?.Update();
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
            if (typeof(T) == typeof(CombatModule)) return _combatModule as T;
            if (typeof(T) == typeof(ActivityMonitorModule)) return _activityModule as T;
            return null;
        }

        /// <summary>
        /// 获取PlayerActor (实现IGameRunner接口)
        /// </summary>
        PlayerActor IGameRunner.GetPlayerActor()
        {
            return _player;
        }
    }
}
