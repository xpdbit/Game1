using System;
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
        [SerializeField] private bool _runActorTest = true;
        [SerializeField] private bool _runTeamTest = true;
        [SerializeField] private bool _runTravelPointTest = false;

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
            ActorManager.Initialize();

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
            if (_runActorTest) RunActorTests();
            if (_runTeamTest) RunTeamTests();
            RunJobSystemTests(); // JobSystem测试总是运行
            
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
        /// 注意：由于UniWinCore限制，后台只能检测鼠标
        /// 这里只统计后台输入次数，不重复加分（加分由GameMain处理）
        /// </summary>
        private void OnBackgroundInputDetected()
        {
            _backgroundInputCount++;
            Debug.Log($"[GameTest] Background input detected via UniWinCore (Count: {_backgroundInputCount})");
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
            int eventType = UnityEngine.Random.Range(0, 3);
            return eventType switch
            {
                0 => new CombatEvent
                {
                    enemyCount = UnityEngine.Random.Range(1, 4),
                    enemyStrength = UnityEngine.Random.Range(10, 50)
                },
                1 => new TradeEvent(),
                _ => new CombatEvent
                {
                    enemyCount = 1,
                    enemyStrength = UnityEngine.Random.Range(15, 30)
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

        #region Actor Tests

        private void RunActorTests()
        {
            Debug.Log("========================================");
            Debug.Log("[GameTest] Starting Actor Tests...");
            Debug.Log("========================================");

            TestActorTemplateLoading();
            TestActorTemplateQuery();
            TestActorTemplateByAffiliation();
            TestActorSpecialQueries();

            Debug.Log("========================================");
            Debug.Log("[GameTest] Actor Tests Completed!");
            Debug.Log("========================================");
        }

        /// <summary>
        /// 测试角色模板加载
        /// </summary>
        private void TestActorTemplateLoading()
        {
            Debug.Log("[ActorTest] Testing template loading...");

            // 检查是否已加载
            Debug.Log($"[ActorTest] ActorManager initialized: {ActorManager.isLoaded}");

            // 获取总模板数量
            int totalCount = ActorManager.GetAllTemplateIds().Count;
            Debug.Log($"[ActorTest] Total templates loaded: {totalCount}");

            if (totalCount == 0)
            {
                Debug.LogWarning("[ActorTest] No templates loaded - check Data/Actors/Actors.xml");
            }
        }

        /// <summary>
        /// 测试角色模板查询
        /// </summary>
        private void TestActorTemplateQuery()
        {
            Debug.Log("[ActorTest] Testing template query...");

            // 测试获取玩家模板
            var playerTemplate = ActorManager.GetPlayerTemplate();
            if (playerTemplate != null)
            {
                Debug.Log($"[ActorTest] Player template: {playerTemplate.id}");
                Debug.Log($"[ActorTest]   - Affiliation: {playerTemplate.affiliation}");
                Debug.Log($"[ActorTest]   - HP: {playerTemplate.maxHp}, ATK: {playerTemplate.attack}, DEF: {playerTemplate.defense}");
            }
            else
            {
                Debug.LogWarning("[ActorTest] Player template not found: Core.Actor.Player");
            }

            // 测试获取不存在的模板
            var nonExistent = ActorManager.GetTemplate("Non.Existent.Actor");
            Debug.Log($"[ActorTest] Non-existent template query: {(nonExistent == null ? "Correctly returned null" : "ERROR - should be null")}");

            // 测试HasTemplate
            bool hasPlayer = ActorManager.HasTemplate("Core.Actor.Player");
            bool hasNonExistent = ActorManager.HasTemplate("Non.Existent.Actor");
            Debug.Log($"[ActorTest] HasTemplate 'Core.Actor.Player': {hasPlayer}");
            Debug.Log($"[ActorTest] HasTemplate 'Non.Existent.Actor': {hasNonExistent}");
        }

        /// <summary>
        /// 测试按阵营获取模板
        /// </summary>
        private void TestActorTemplateByAffiliation()
        {
            Debug.Log("[ActorTest] Testing affiliation-based query...");

            // 获取所有阵营
            var affiliations = new[] { Affiliation.Player, Affiliation.Hostile, Affiliation.Neutral, Affiliation.Friendly, Affiliation.Authority };

            foreach (var affiliation in affiliations)
            {
                var templates = ActorManager.GetTemplatesByAffiliation(affiliation);
                Debug.Log($"[ActorTest] {affiliation} templates: {templates.Count}");
                foreach (var template in templates)
                {
                    Debug.Log($"[ActorTest]   - {template.id} (Boss: {template.isBoss}, InteractType: {template.interactionType})");
                }
            }
        }

        /// <summary>
        /// 测试特殊查询（敌人、Boss、交互型NPC）
        /// </summary>
        private void TestActorSpecialQueries()
        {
            Debug.Log("[ActorTest] Testing special queries...");

            // 测试获取敌对模板（不同难度）
            for (int difficulty = 1; difficulty <= 5; difficulty++)
            {
                var hostile = ActorManager.GetHostileTemplate(difficulty);
                if (hostile != null)
                {
                    Debug.Log($"[ActorTest] Hostile template (difficulty {difficulty}): {hostile.id}");
                }
                else
                {
                    Debug.Log($"[ActorTest] Hostile template (difficulty {difficulty}): None available");
                }
            }

            // 测试获取Boss模板
            var boss = ActorManager.GetBossTemplate();
            if (boss != null)
            {
                Debug.Log($"[ActorTest] Boss template: {boss.id}");
                Debug.Log($"[ActorTest]   - HP: {boss.maxHp}, ATK: {boss.attack}, DEF: {boss.defense}");
                Debug.Log($"[ActorTest]   - Gold reward: {boss.goldReward}, EXP reward: {boss.expReward}");
            }
            else
            {
                Debug.Log("[ActorTest] Boss template: None available");
            }

            // 测试获取可交互NPC
            var interactables = ActorManager.GetInteractableTemplates();
            Debug.Log($"[ActorTest] Interactable NPCs: {interactables.Count}");
            foreach (var npc in interactables)
            {
                Debug.Log($"[ActorTest]   - {npc.id} (Type: {npc.interactionType})");
            }
        }

        #endregion

        #region Team Tests

        private void RunTeamTests()
        {
            Debug.Log("========================================");
            Debug.Log("[GameTest] Starting Team Tests...");
            Debug.Log("========================================");

            // 先清空队伍确保干净状态
            TeamManager.ClearTeam();

            TestTeamAddMember();
            TestTeamRemoveMember();
            TestTeamCapacity();
            TestTeamQuery();
            TestTeamCombatPower();
            TestTeamSerialization();
            TestTeamEvents();
            TestTeamOperations();

            Debug.Log("========================================");
            Debug.Log("[GameTest] Team Tests Completed!");
            Debug.Log("========================================");
        }

        /// <summary>
        /// 测试添加成员
        /// </summary>
        private void TestTeamAddMember()
        {
            Debug.Log("[TeamTest] Testing AddMember...");

            // 测试使用简单参数添加
            var result1 = TeamManager.AddMember("TestHero1", 5, 100, 20, 15);
            Debug.Log($"[TeamTest] AddMember 'TestHero1': success={result1.success}, id={result1.memberId}, msg={result1.message}");

            // 测试使用TeamMemberData添加
            var member2 = new TeamMemberData("TestHero2", 3, 80, 15, 10);
            member2.job = JobType.Merchant;
            var result2 = TeamManager.AddMember(member2);
            Debug.Log($"[TeamTest] AddMember 'TestHero2': success={result2.success}, id={result2.memberId}, msg={result2.message}");

            // 测试重复名称添加（应该失败）
            var result3 = TeamManager.AddMember("TestHero1", 1, 20, 5, 3);
            Debug.Log($"[TeamTest] AddMember duplicate name: success={result3.success}, msg={result3.message}");

            // 添加更多成员测试容量
            TeamManager.AddMember("TestHero3", 2, 60, 12, 8);
            TeamManager.AddMember("TestHero4", 4, 90, 18, 12);
            TeamManager.AddMember("TestHero5", 1, 50, 10, 6);
            TeamManager.AddMember("TestHero6", 6, 120, 25, 18);

            Debug.Log($"[TeamTest] Current team size: {TeamManager.GetMemberCount()}");
        }

        /// <summary>
        /// 测试移除成员
        /// </summary>
        private void TestTeamRemoveMember()
        {
            Debug.Log("[TeamTest] Testing RemoveMember...");

            int countBefore = TeamManager.GetMemberCount();
            Debug.Log($"[TeamTest] Team size before removal: {countBefore}");

            // 获取第一个成员
            var members = TeamManager.GetAllMembers();
            if (members.Count > 0)
            {
                int removeId = members[0].id;
                var result = TeamManager.RemoveMember(removeId);
                Debug.Log($"[TeamTest] RemoveMember id={removeId}: success={result.success}, msg={result.message}");
            }

            // 测试移除不存在的成员
            var resultNotFound = TeamManager.RemoveMember(99999);
            Debug.Log($"[TeamTest] RemoveMember non-existent: success={resultNotFound.success}, msg={resultNotFound.message}");

            Debug.Log($"[TeamTest] Team size after removal: {TeamManager.GetMemberCount()}");
        }

        /// <summary>
        /// 测试队伍容量
        /// </summary>
        private void TestTeamCapacity()
        {
            Debug.Log("[TeamTest] Testing team capacity...");

            Debug.Log($"[TeamTest] CanAddMember: {TeamManager.CanAddMember()}");
            Debug.Log($"[TeamTest] RemainingSlots: {TeamManager.RemainingSlots()}");

            // 填满队伍
            while (TeamManager.CanAddMember())
            {
                var result = TeamManager.AddMember($"Filler_{TeamManager.GetMemberCount()}", 1, 20, 5, 3);
                if (!result.success) break;
            }

            Debug.Log($"[TeamTest] After filling - team size: {TeamManager.GetMemberCount()}, remaining slots: {TeamManager.RemainingSlots()}");
            Debug.Log($"[TeamTest] CanAddMember (should be false): {TeamManager.CanAddMember()}");

            // 清空用于后续测试
            TeamManager.ClearTeam();
        }

        /// <summary>
        /// 测试队伍查询
        /// </summary>
        private void TestTeamQuery()
        {
            Debug.Log("[TeamTest] Testing team query...");

            // 添加测试成员
            TeamManager.AddMember("QueryTest1", 5, 100, 20, 15);
            TeamManager.AddMember("QueryTest2", 3, 80, 15, 10);

            // 测试GetMember
            var members = TeamManager.GetAllMembers();
            Debug.Log($"[TeamTest] Total members: {members.Count}");
            foreach (var member in members)
            {
                Debug.Log($"[TeamTest]   - {member.name}: Lv.{member.level}, HP={member.hp}/{member.maxHp}, ATK={member.attack}, DEF={member.defense}, Job={member.job}");
            }

            // 测试GetMember with ID
            if (members.Count > 0)
            {
                var fetched = TeamManager.GetMember(members[0].id);
                Debug.Log($"[TeamTest] GetMember id={members[0].id}: {(fetched != null ? fetched.name : "null")}");
            }

            // 测试平均等级
            float avgLevel = TeamManager.GetAverageLevel();
            Debug.Log($"[TeamTest] Average level: {avgLevel:F2}");
        }

        /// <summary>
        /// 测试队伍战斗力计算
        /// </summary>
        private void TestTeamCombatPower()
        {
            Debug.Log("[TeamTest] Testing combat power calculation...");

            TeamManager.ClearTeam();
            TeamManager.AddMember("PowerTest1", 10, 200, 50, 30);
            TeamManager.AddMember("PowerTest2", 5, 100, 25, 15);

            int totalPower = TeamManager.GetTotalCombatPower();
            Debug.Log($"[TeamTest] Total combat power: {totalPower}");

            // 验证计算：PowerTest1 = 25+15+100=140, PowerTest2 = 10+8+50=68, Total=208
            // 注意：实际计算可能包含装备和职业加成，这里仅测试基础
            Debug.Log($"[TeamTest] Expected approximate power: 200-300 (based on attack+defense+maxHp/2)");

            // 测试职业对属性的影响
            TeamManager.ClearTeam();
            var escortMember = new TeamMemberData("EscortTest", 5, 100, 20, 15);
            escortMember.job = JobType.Escort;
            TeamManager.AddMember(escortMember);

            var merchantMember = new TeamMemberData("MerchantTest", 5, 100, 20, 15);
            merchantMember.job = JobType.Merchant;
            TeamManager.AddMember(merchantMember);

            Debug.Log($"[TeamTest] Escort (JobType.Escort) total attack: {escortMember.GetTotalAttack()}");
            Debug.Log($"[TeamTest] Merchant (JobType.Merchant) total attack: {merchantMember.GetTotalAttack()}");
            Debug.Log($"[TeamTest] Escort should have higher attack due to Attack being primary attr for Escort");
        }

        /// <summary>
        /// 测试序列化（导出/导入）
        /// </summary>
        private void TestTeamSerialization()
        {
            Debug.Log("[TeamTest] Testing team serialization...");

            TeamManager.ClearTeam();
            TeamManager.AddMember("SerializeTest1", 7, 150, 30, 20);
            TeamManager.AddMember("SerializeTest2", 4, 90, 18, 12);

            Debug.Log($"[TeamTest] Team before export: {TeamManager.GetMemberCount()} members");

            // 导出
            var exported = TeamManager.ExportTeam();
            Debug.Log($"[TeamTest] Exported {exported.Count} members");
            foreach (var m in exported)
            {
                Debug.Log($"[TeamTest]   - {m.name}: Lv.{m.level}, ID={m.memberId}");
            }

            // 清空后导入
            TeamManager.ClearTeam();
            Debug.Log($"[TeamTest] Team after clear: {TeamManager.GetMemberCount()} members");

            TeamManager.ImportTeam(exported);
            Debug.Log($"[TeamTest] Team after import: {TeamManager.GetMemberCount()} members");

            // 验证ID保留
            var imported = TeamManager.GetAllMembers();
            if (imported.Count >= 2)
            {
                Debug.Log($"[TeamTest] First imported member ID: {imported[0].id} (should be same as exported)");
            }
        }

        /// <summary>
        /// 测试队伍事件
        /// </summary>
        private void TestTeamEvents()
        {
            Debug.Log("[TeamTest] Testing team events...");

            TeamManager.ClearTeam();

            int addEventCount = 0;
            int removeEventCount = 0;
            int updateEventCount = 0;

            void OnTeamChanged(TeamEventData data)
            {
                switch (data.eventType)
                {
                    case TeamEventData.TeamEventType.MemberAdded:
                        addEventCount++;
                        Debug.Log($"[TeamTest] Event: MemberAdded - {data.memberName} (id={data.memberId})");
                        break;
                    case TeamEventData.TeamEventType.MemberRemoved:
                        removeEventCount++;
                        Debug.Log($"[TeamTest] Event: MemberRemoved - {data.memberName} (id={data.memberId})");
                        break;
                    case TeamEventData.TeamEventType.MemberUpdated:
                        updateEventCount++;
                        Debug.Log($"[TeamTest] Event: MemberUpdated - {data.memberName} (id={data.memberId})");
                        break;
                    case TeamEventData.TeamEventType.TeamCleared:
                        Debug.Log("[TeamTest] Event: TeamCleared");
                        break;
                    case TeamEventData.TeamEventType.CapacityChanged:
                        Debug.Log($"[TeamTest] Event: CapacityChanged - slots remaining: {TeamManager.RemainingSlots()}");
                        break;
                }
            }

            TeamManager.SubscribeTeamChanged(OnTeamChanged);

            // 触发事件
            TeamManager.AddMember("EventTest1", 1, 20, 5, 3);
            TeamManager.AddMember("EventTest2", 1, 20, 5, 3);

            var member = TeamManager.GetAllMembers()[0];
            if (member != null)
            {
                member.level = 10;
                TeamManager.UpdateMember(member);
            }

            TeamManager.RemoveMember(TeamManager.GetAllMembers()[0].id);
            TeamManager.ClearTeam();

            Debug.Log($"[TeamTest] Events received - Added: {addEventCount}, Removed: {removeEventCount}, Updated: {updateEventCount}");

            TeamManager.UnsubscribeTeamChanged(OnTeamChanged);
        }

        /// <summary>
        /// 测试队伍操作（治疗、升级）
        /// </summary>
        private void TestTeamOperations()
        {
            Debug.Log("[TeamTest] Testing team operations...");

            TeamManager.ClearTeam();
            var member1 = new TeamMemberData("OpTest1", 3, 50, 10, 5);
            var member2 = new TeamMemberData("OpTest2", 2, 30, 8, 4);
            TeamManager.AddMember(member1);
            TeamManager.AddMember(member2);

            // 造成伤害
            foreach (var m in TeamManager.GetAllMembers())
            {
                m.TakeDamage(10);
            }
            Debug.Log("[TeamTest] After taking 10 damage:");
            foreach (var m in TeamManager.GetAllMembers())
            {
                Debug.Log($"[TeamTest]   - {m.name}: HP={m.hp}/{m.maxHp}");
            }

            // 治疗
            TeamManager.HealAll(20);
            Debug.Log("[TeamTest] After HealAll(20):");
            foreach (var m in TeamManager.GetAllMembers())
            {
                Debug.Log($"[TeamTest]   - {m.name}: HP={m.hp}/{m.maxHp}");
            }

            // 升级
            TeamManager.LevelUpAll();
            Debug.Log("[TeamTest] After LevelUpAll:");
            foreach (var m in TeamManager.GetAllMembers())
            {
                Debug.Log($"[TeamTest]   - {m.name}: Lv.{m.level}, HP={m.hp}/{m.maxHp}, ATK={m.attack}, DEF={m.defense}");
            }
        }

        #endregion

        #region JobSystem Tests

        /// <summary>
        /// 测试JobSystem（职业系统）
        /// </summary>
        private void RunJobSystemTests()
        {
            Debug.Log("========================================");
            Debug.Log("[GameTest] Starting JobSystem Tests...");
            Debug.Log("========================================");

            TestJobNames();
            TestJobAttributes();
            TestJobBonuses();
            TestJobDefaultSkills();

            Debug.Log("========================================");
            Debug.Log("[GameTest] JobSystem Tests Completed!");
            Debug.Log("========================================");
        }

        /// <summary>
        /// 测试职业名称
        /// </summary>
        private void TestJobNames()
        {
            Debug.Log("[JobTest] Testing job names...");

            foreach (JobType job in Enum.GetValues(typeof(JobType)))
            {
                string name = JobSystem.instance.GetJobName(job);
                Debug.Log($"[JobTest]   {job} -> {name}");
            }
        }

        /// <summary>
        /// 测试职业属性加成
        /// </summary>
        private void TestJobAttributes()
        {
            Debug.Log("[JobTest] Testing job attribute bonuses...");

            var testLevel = 10;

            foreach (JobType job in Enum.GetValues(typeof(JobType)))
            {
                Debug.Log($"[JobTest]   {job} at level {testLevel}:");

                foreach (AttributeType attr in Enum.GetValues(typeof(AttributeType)))
                {
                    int bonus = JobSystem.instance.GetJobAttributeBonus(job, attr, testLevel);
                    if (bonus > 0)
                    {
                        Debug.Log($"[JobTest]     {attr}: +{bonus}");
                    }
                }
            }
        }

        /// <summary>
        /// 测试职业特殊加成
        /// </summary>
        private void TestJobBonuses()
        {
            Debug.Log("[JobTest] Testing job special bonuses...");

            foreach (JobType job in Enum.GetValues(typeof(JobType)))
            {
                var bonus = JobSystem.instance.GetJobBonus(job);
                Debug.Log($"[JobTest]   {job}: Trade={bonus.tradeBonus:P0}, Combat={bonus.combatBonus:P0}, Discovery={bonus.discoveryBonus:P0}, Heal={bonus.healBonus:P0}");
            }
        }

        /// <summary>
        /// 测试职业默认技能
        /// </summary>
        private void TestJobDefaultSkills()
        {
            Debug.Log("[JobTest] Testing job default skills...");

            foreach (JobType job in Enum.GetValues(typeof(JobType)))
            {
                string passive = JobSystem.instance.GetJobDefaultPassiveSkill(job);
                string active = JobSystem.instance.GetJobDefaultActiveSkill(job);
                Debug.Log($"[JobTest]   {job}: Passive={passive}, Active={active}");
            }
        }

        #endregion
    }
}