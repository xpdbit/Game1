using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Game1.Modules.Travel;
using Game1.Modules.Combat;
using Game1.Core.EventBus;

namespace Game1
{
    /// <summary>
    /// 游戏主入口
    /// 单例模式，管理所有子系统
    /// 同时作为VContainer的LifetimeScope
    /// </summary>
    public class GameMain : LifetimeScope
    {
        #region Singleton
        public static GameMain instance { get; private set; }
        #endregion

        [Header("配置")]
        public GameConfig config = new();

        [Header("UI引用")]
        public UIManager uIManager;

        [Header("游戏循环")]
        public GameLoopManager gameLoopManager;

        [Header("旅行系统")]
        public TravelManager travelManager;
        public ProgressManager progressManager;

        [Header("事件系统")]
        public EventQueue eventQueue;
        public EventChainManager eventChainManager;

        [Header("NPC系统")]
        public NPCManager npcManager;

        [Header("战斗系统")]
        public CombatSystem combatSystem;

        // 事件
        public event System.Action onPlayerInput;

        protected override void Awake()
        {
            instance = this;

            // 初始化管理器（按依赖顺序）- 必须在base.Awake()之前
            InitializeManagers();

            // 初始化物品系统
            ItemManager.Initialize();

            // VContainer的Awake - 创建容器并调用Configure()
            base.Awake();
        }

        private void Start()
        {
            // 初始化玩家（等待GameLoopManager完成初始化）
            InitializePlayer();

            // 开始游戏
            StartGame();
        }

        private void Update()
        {
            // 处理输入（增加进度点）- 使用Input System
            if (Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            {
                travelManager?.OnPlayerInteract();
                onPlayerInput?.Invoke();
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Register existing singleton managers via RegisterInstance
            // InitializeManagers() runs before this, so singletons are already initialized
            builder.RegisterInstance(gameLoopManager).As<GameLoopManager>();
            builder.RegisterInstance(TravelManager.instance).As<ITravelManager>();
            builder.RegisterInstance(CombatSystem.instance).As<ICombatSystem>();
            builder.RegisterInstance(EventBus.instance).As<IEventBus>();
            builder.RegisterInstance(ProgressManager.instance);
            builder.Register<SaveManager>(Lifetime.Singleton);
        }

        /// <summary>
        /// 初始化所有管理器
        /// </summary>
        private void InitializeManagers()
        {
            // GameLoopManager必须首先创建，确保PlayerActor唯一实例
            if (gameLoopManager == null)
                gameLoopManager = gameObject.AddComponent<GameLoopManager>();

            // 旅行管理器
            if (travelManager == null)
                travelManager = TravelManager.instance;

            // 进度管理器
            if (progressManager == null)
                progressManager = ProgressManager.instance;

            // 事件队列 - 使用GameLoopManager已创建的实例
            // 注意：EventQueue在GameLoopManager.InitializeSystems()中创建
            // GameLoopManager先于TravelManager初始化，确保一致性
            eventQueue ??= new EventQueue();

            // 事件链管理器
            if (eventChainManager == null)
                eventChainManager = EventChainManager.instance;

            // NPC管理器
            if (npcManager == null)
                npcManager = NPCManager.instance;

            // 战斗系统
            if (combatSystem == null)
                combatSystem = CombatSystem.instance;

            // 卡牌系统（CardDesign在Game1命名空间）
            CardDesign.instance.Initialize();

            // 技能系统（SkillDesign在Game1命名空间）
            SkillDesign.instance.Initialize();

            // 设置旅行管理器的事件队列引用
            travelManager?.SetEventQueue(eventQueue);

            // 订阅事件树存档请求
            EventTreeRunner.instance.onTreeSaveRequested += OnEventTreeSaveRequested;
        }

        /// <summary>
        /// 事件树存档请求处理
        /// </summary>
        private void OnEventTreeSaveRequested(EventTreeRunSaveData data)
        {
            var saveManager = GetService<SaveManager>();
            saveManager?.SetEventTreeRunData(data);
        }

        /// <summary>
        /// 初始化玩家
        /// </summary>
        private void InitializePlayer()
        {
            // PlayerActor由GameLoopManager创建并拥有，这里获取引用确保一致
            if (gameLoopManager != null)
            {
                // 等待GameLoopManager初始化完成
            }

            // 初始化旅行管理器（使用GameLoopManager中的PlayerActor）
            if (gameLoopManager?.player != null)
            {
                travelManager?.Initialize(gameLoopManager.player);
            }
            else
            {
                Debug.LogWarning("[GameMain] GameLoopManager.player is null during InitializePlayer");
            }
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        private void StartGame()
        {
            // 获取存档管理器
            var saveManager = Container.Resolve<SaveManager>();

            // 检查是否有有效世界存档
            if (saveManager?.currentSave?.world != null && !string.IsNullOrEmpty(saveManager.currentSave.world.currentMapSeed))
            {
                // 恢复旅行
                travelManager?.ImportFromSaveData(saveManager.currentSave.world);
                Debug.Log("[GameMain] Restored journey from save");
            }
            else
            {
                // 开始新旅程
                string seed = System.DateTime.Now.Ticks.ToString();
                travelManager?.StartNewJourney(seed);
                Debug.Log("[GameMain] Started new journey");
            }

            Debug.Log("[GameMain] Game started");
        }

        /// <summary>
        /// 获取玩家Actor
        /// </summary>
        public PlayerActor GetPlayerActor()
        {
            return gameLoopManager?.player;
        }

        /// <summary>
        /// 获取DI容器中的服务
        /// </summary>
        public T GetService<T>() where T : class
        {
            return Container.Resolve<T>();
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        public void GameOver()
        {
            Debug.Log("[GameMain] Game Over!");
            // TODO: 显示游戏结束界面
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame()
        {
            var saveManager = Container.Resolve<SaveManager>();

            // 同步世界数据
            var worldData = travelManager?.ExportToSaveData();
            if (worldData != null)
            {
                saveManager.currentSave.world = worldData;
            }

            // 保存事件树状态
            EventTreeRunner.instance.SaveState();

            // 标记世界槽位脏
            saveManager?.MarkSlotDirty(SaveSlot.World);

            Debug.Log("[GameMain] SaveGame called");
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame()
        {
            // 获取SaveManager中的事件树数据并恢复
            var saveManager = GetService<SaveManager>();
            var eventTreeData = saveManager?.GetEventTreeRunData();
            if (eventTreeData != null)
            {
                EventTreeRunner.instance.RestoreState(eventTreeData);
            }
            Debug.Log("[GameMain] LoadGame called");
        }
    }

    /// <summary>
    /// 游戏配置
    /// </summary>
    [System.Serializable]
    public class GameConfig
    {
        [Header("进度配置")]
        public int pointsPerSecond = 1;
        public int pointsPerClick = 10;
        public int pointsPerEvent = 1000;

        [Header("战斗配置")]
        public int playerHp = 20;
        public int playerArmor = 5;
        public int playerDamage = 3;
    }
}
