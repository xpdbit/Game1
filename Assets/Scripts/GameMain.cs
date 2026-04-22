using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1
{
    /// <summary>
    /// 游戏主入口
    /// 单例模式，管理所有子系统
    /// </summary>
    public class GameMain : MonoBehaviour
    {
        #region Singleton
        public static GameMain instance { get; private set; }
        #endregion

        [Header("配置")]
        public GameConfig config = new();

        [Header("UI引用")]
        public UIManager uIManager;

        [Header("玩家数据")]
        public PlayerActor playerActor;

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

        private void Awake()
        {
            instance = this;

            // 初始化管理器（按依赖顺序）
            InitializeManagers();

            // 初始化物品系统
            ItemManager.Initialize();

            // 初始化玩家
            InitializePlayer();
        }

        private void Start()
        {
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

        /// <summary>
        /// 初始化所有管理器
        /// </summary>
        private void InitializeManagers()
        {
            // 旅行管理器
            if (travelManager == null)
                travelManager = TravelManager.instance;

            // 进度管理器
            if (progressManager == null)
                progressManager = ProgressManager.instance;

            // 事件队列
            if (eventQueue == null)
                eventQueue = new EventQueue();

            // 事件链管理器
            if (eventChainManager == null)
                eventChainManager = EventChainManager.instance;

            // NPC管理器
            if (npcManager == null)
                npcManager = NPCManager.instance;

            // 战斗系统
            if (combatSystem == null)
                combatSystem = CombatSystem.instance;

            // 设置旅行管理器的事件队列引用
            travelManager?.SetEventQueue(eventQueue);
        }

        /// <summary>
        /// 初始化玩家
        /// </summary>
        private void InitializePlayer()
        {
            if (playerActor == null)
            {
                playerActor = new PlayerActor();
            }

            // 初始化旅行管理器
            travelManager?.Initialize(playerActor);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        private void StartGame()
        {
            // 开始新旅程
            string seed = System.DateTime.Now.Ticks.ToString();
            travelManager?.StartNewJourney(seed);

            Debug.Log("[GameMain] Game started");
        }

        /// <summary>
        /// 获取玩家Actor
        /// </summary>
        public PlayerActor GetPlayerActor()
        {
            return playerActor;
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
            // TODO: 实现存档逻辑
            Debug.Log("[GameMain] SaveGame called");
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame()
        {
            // TODO: 实现读档逻辑
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

        [Header("背包配置")]
        public int inventoryMaxSlots = 50;
        public float inventoryMaxWeight = 100f;
    }
}
