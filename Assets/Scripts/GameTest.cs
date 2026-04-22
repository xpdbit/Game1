using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 游戏功能测试类
    /// 用于测试库存、事件、挂机等核心系统
    /// </summary>
    public class GameTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _runInventoryTest = false;
        [SerializeField] private bool _runEventTest = false;
        [SerializeField] private bool _runIdleTest = false;
        [SerializeField] private bool _runEventBusTest = false;
        [SerializeField] private bool _runTravelPointTest = true;

        [Header("Travel Point 测试配置")]
        [SerializeField] private bool _useBackgroundInput = true;
        [SerializeField] private float _testDuration = 30f;

        private PlayerActor _testPlayer;
        private IdleRewardModule _idleModule;
        private EventQueue _eventQueue;
        private float _testTimer;
        private bool _travelPointTestStarted;

        // Travel Point 测试统计
        private int _normalEventsTriggered;
        private int _eventTreesTriggered;
        private int _backgroundInputCount;

        private void Start()
        {
            ItemManager.Initialize();

            _testPlayer = new PlayerActor();
            _idleModule = new IdleRewardModule
            {
                baseRewardPerSecond = 10f,
                offlineRewardRate = 0.5f
            };
            _idleModule.Initialize(_testPlayer);
            _testPlayer.AddModule(_idleModule);

            _eventQueue = new EventQueue();

            if (_runInventoryTest) RunInventoryTests();
            if (_runEventBusTest) RunEventBusTests();
            if (_runEventTest) RunEventTests();
            if (_runIdleTest) RunIdleTests();

            // 启动 Travel Point 测试
            if (_runTravelPointTest)
            {
                StartTravelPointTest();
            }
        }

        private void Update()
        {
            // Travel Point 测试 - 每帧更新
            if (_runTravelPointTest && _travelPointTestStarted)
            {
                UpdateTravelPointTest();
            }
        }

        #region Travel Point Test

        /// <summary>
        /// 启动 Travel Point 测试
        /// </summary>
        private void StartTravelPointTest()
        {
            Debug.Log("[GameTest] Starting Travel Point Test...");
            Debug.Log($"[GameTest] Test Duration: {_testDuration}s");
            Debug.Log($"[GameTest] Background Input: {(_useBackgroundInput ? "Enabled" : "Disabled")}");

            // 重置进度管理器
            ProgressManager.instance.Reset();

            // 初始化后台输入管理器
            if (_useBackgroundInput)
            {
                BackgroundInputManager.instance.Initialize();
                // 使用 onMouseButtonPressed 检测后台鼠标输入
                // 注意：这里不直接调用 AddPointsClick，因为前台输入会通过 GameMain -> TravelManager 处理
                BackgroundInputManager.instance.onMouseButtonPressed += OnBackgroundInputDetected;
                Debug.Log("[GameTest] Background input listener started (mouse only - UniWinCore limitation)");
            }

            // 订阅 ProgressManager 事件
            ProgressManager.instance.onNormalEventTriggered += OnNormalEventTriggered;
            ProgressManager.instance.onEventTreeTriggered += OnEventTreeTriggered;

            // 订阅 GameMain 的前台输入事件（如果存在）
            var gameMain = GameMain.instance;
            if (gameMain != null)
            {
                gameMain.onPlayerInput += OnForegroundInput;
            }

            // 重置统计
            _normalEventsTriggered = 0;
            _eventTreesTriggered = 0;
            _backgroundInputCount = 0;
            _testTimer = 0f;
            _travelPointTestStarted = true;

            Debug.Log("[GameTest] Travel Point test initialized. Waiting for point accumulation...");
        }

        /// <summary>
        /// 每帧更新 Travel Point 测试
        /// </summary>
        private void UpdateTravelPointTest()
        {
            _testTimer += Time.deltaTime;

            // 被动收入：每秒获得1点 Travel Point
            ProgressManager.instance.AddPoints(Time.deltaTime);

            // 更新后台输入管理器
            if (_useBackgroundInput)
            {
                BackgroundInputManager.instance.Update();
            }

            // 检查测试时间
            if (_testTimer >= _testDuration)
            {
                EndTravelPointTest();
                return;
            }

            // 每秒输出一次进度
            if (Mathf.FloorToInt(_testTimer) != Mathf.FloorToInt(_testTimer - Time.deltaTime))
            {
                int currentPoints = ProgressManager.instance.currentPoints;
                Debug.Log($"[GameTest] Time: {_testTimer:F1}s | Points: {currentPoints} | Normal Events: {_normalEventsTriggered} | EventTrees: {_eventTreesTriggered}");
            }
        }

        /// <summary>
        /// 结束 Travel Point 测试
        /// </summary>
        private void EndTravelPointTest()
        {
            _travelPointTestStarted = false;

            // 取消订阅
            if (_useBackgroundInput)
            {
                BackgroundInputManager.instance.onMouseButtonPressed -= OnBackgroundInputDetected;
            }
            ProgressManager.instance.onNormalEventTriggered -= OnNormalEventTriggered;
            ProgressManager.instance.onEventTreeTriggered -= OnEventTreeTriggered;

            var gameMain = GameMain.instance;
            if (gameMain != null)
            {
                gameMain.onPlayerInput -= OnForegroundInput;
            }

            // 输出测试结果
            Debug.Log("========================================");
            Debug.Log("[GameTest] Travel Point Test Completed!");
            Debug.Log($"[GameTest] Duration: {_testDuration}s");
            Debug.Log($"[GameTest] Total Points Earned: {ProgressManager.instance.totalEarnedPoints}");
            Debug.Log($"[GameTest] Current Points: {ProgressManager.instance.currentPoints}");
            Debug.Log($"[GameTest] Normal Events Triggered (200 pts): {_normalEventsTriggered}");
            Debug.Log($"[GameTest] Event Trees Triggered (1000 pts): {_eventTreesTriggered}");
            Debug.Log($"[GameTest] Background Input Count: {_backgroundInputCount}");
            Debug.Log("========================================");

            _runTravelPointTest = false;
        }

        /// <summary>
        /// 后台输入检测回调
        /// 后台鼠标输入也触发加分（+10点）
        /// 注意：由于UniWinCore限制，后台只能检测鼠标，不能检测普通键盘按键
        /// </summary>
        private void OnBackgroundInputDetected()
        {
            _backgroundInputCount++;
            // 后台鼠标输入也触发加分（每次+10点）
            ProgressManager.instance.AddPointsClick();
            Debug.Log($"[GameTest] Background input detected via UniWinCore (Count: {_backgroundInputCount}, Points now: {ProgressManager.instance.currentPoints})");
        }

        /// <summary>
        /// 前台输入回调
        /// 注意：实际分数由 GameMain.Update() -> TravelManager.OnPlayerInteract() -> ProgressManager.AddPointsClick() 处理
        /// </summary>
        private void OnForegroundInput()
        {
            Debug.Log("[GameTest] Foreground input detected (scoring handled by TravelManager)");
        }

        /// <summary>
        /// 普通事件触发（每200点）
        /// </summary>
        private void OnNormalEventTriggered(int eventNumber)
        {
            _normalEventsTriggered++;

            // 从 EventQueue 获取一个随机事件并执行
            var randomEvent = CreateRandomEvent();
            if (randomEvent != null)
            {
                _eventQueue.Enqueue(randomEvent);
                var result = _eventQueue.ProcessNext();
                if (result != null)
                {
                    _testPlayer.ApplyEventResult(result);
                    Debug.Log($"[GameTest] Normal Event #{eventNumber}: {result.message}");
                }
            }

            Debug.Log($"[GameTest] Normal event triggered! Count: {_normalEventsTriggered}");
        }

        /// <summary>
        /// 事件树触发（每1000点）
        /// </summary>
        private void OnEventTreeTriggered(int milestoneNumber)
        {
            _eventTreesTriggered++;

            // 尝试启动一个事件树
            bool started = EventTreeRunner.instance.StartRandomTree();
            if (started)
            {
                Debug.Log($"[GameTest] Event Tree #{milestoneNumber} started: {EventTreeRunner.instance.currentTemplate?.name ?? "Unknown"}");
            }
            else
            {
                Debug.Log($"[GameTest] Event Tree #{milestoneNumber} triggered but no template available (using fallback event)");
                // 如果没有事件树模板，使用普通事件作为后备
                var fallbackEvent = CreateRandomEvent();
                if (fallbackEvent != null)
                {
                    _eventQueue.Enqueue(fallbackEvent);
                    var result = _eventQueue.ProcessNext();
                    if (result != null)
                    {
                        _testPlayer.ApplyEventResult(result);
                    }
                }
            }

            Debug.Log($"[GameTest] Event tree triggered! Count: {_eventTreesTriggered}");
        }

        /// <summary>
        /// 创建随机事件
        /// </summary>
        private IGameEvent CreateRandomEvent()
        {
            int eventType = Random.Range(0, 3);
            return eventType switch
            {
                0 => new CombatEvent
                {
                    enemyCount = Random.Range(1, 4),
                    enemyStrength = Random.Range(10, 50)
                },
                1 => new TradeEvent(),
                _ => new CombatEvent
                {
                    enemyCount = 1,
                    enemyStrength = Random.Range(15, 30)
                }
            };
        }

        #endregion

        #region Inventory Tests

        private void RunInventoryTests()
        {
            TestInventoryAdd();
            TestInventoryCapacity();
            TestInventoryBatchOperations();
        }

        private void TestInventoryAdd()
        {
            ItemManager.AddItem("Core.Item.GoldCoin", 100);
            ItemManager.AddItem("Core.Item.Bacon", 5);
            ItemManager.AddItem("Core.Item.Bacon", 2);
            ItemManager.AddItem("Non.Existent.Item", 1);
            ItemManager.AddItem("Core.Item.ShortBlade", 0);

            RefreshInventoryUI();
        }

        private void TestInventoryRemove()
        {
            var items = ItemManager.GetInventory();
            if (items.Count > 0)
            {
                var firstItem = items[0];
                ItemManager.RemoveItem(firstItem.instanceId, 1);
                ItemManager.RemoveItem(firstItem.instanceId, 0);
            }
            ItemManager.RemoveItem(99999, 1);

            RefreshInventoryUI();
        }

        private void TestInventoryCapacity()
        {
            ItemManager.SetInventoryCapacity(20, 50f);
            ItemManager.SetInventoryCapacity(100, 500f);
        }

        private void TestInventoryBatchOperations()
        {
            ItemManager.ClearInventory();
            ItemManager.AddItem("Core.Item.GoldCoin", 50);
            ItemManager.AddItem("Core.Item.Bacon", 10);
        }

        private void RefreshInventoryUI()
        {
            if (UIManager.instance?.inventory != null)
            {
                UIManager.instance.inventory.Refresh();
            }
        }

        #endregion

        #region EventBus Tests

        private void RunEventBusTests()
        {
            var subscriber = new TestInventorySubscriber();
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemAdded, subscriber);
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemRemoved, subscriber);

            ItemManager.AddItem("Core.Item.Money", 25);
            ItemManager.AddItem("Core.Item.Bacon", 1);

            InventoryEventBus.instance.UnsubscribeAll(subscriber);
            InventoryEventBus.instance.Clear();
        }

        private class TestInventorySubscriber : IInventoryEventSubscriber
        {
            public void OnInventoryEvent(InventoryEventData data)
            {
                Debug.Log($"[Event] {data.eventType}: {data.templateId} x{data.amount}");
            }
        }

        #endregion

        #region Event Tests

        private void RunEventTests()
        {
            var queue = new EventQueue();
            var chainMgr = EventChainManager.instance;
        }

        #endregion

        #region Idle Tests

        private void RunIdleTests()
        {
            _idleModule.OnActivate();
            _idleModule.Tick(1f);
            _idleModule.OnDeactivate();

            float offlineReward = _idleModule.CalculateOfflineReward(3600f);
        }

        #endregion
    }
}