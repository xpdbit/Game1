using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Game1.Core.GameLoop;
using Game1.Modules.Travel;
using Game1.Modules.Combat;
using Game1.Modules.Activity;
using Game1.Modules.PendingEvent;
using Game1.Modules.Achievement;

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
            _backgroundInput?.Dispose();
            GlobalKeyboardHook.ForceReset();
        }

        private void OnDestroy()
        {
            // 退出时先标记所有文件为脏，再保存
            _saveManager?.MarkAllDirty();
            _saveManager?.SaveAll();

            _backgroundInput?.Dispose();
            GlobalKeyboardHook.ForceReset();
        }

        private void OnApplicationQuit()
        {
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

            // 注册保存前同步回调 - SaveManager在写入磁盘前触发，确保存档数据最新
            _saveManager.OnBeforeSave += SyncAllSaveData;

            // 3.2 初始化战斗模块（需要在加载存档数据之前，以便ApplyLoadedSaveData可以恢复战斗数据）
            _combatModule = new CombatModule();
            _combatModule.Initialize(_player);
            _player.AddModule(_combatModule);

            // 3.3 注册所有职能存档文件到SaveManager
            RegisterSaveFiles();

            // 3.4 初始化成就系统（在注册存档文件之后、加载存档之前）
            AchievementManager.Initialize();

            // 4. 加载所有职能存档文件
            _saveManager.LoadAll();

            // 4.1 应用加载的数据到各模块
            ApplyLoadedSaveData();

            // 7. 计算并应用离线收益（如果有存档且非首次新建）
            var playerFile = _saveManager.GetFile<PlayerSaveFile>();
            if (playerFile != null && (playerFile.level > 1 || playerFile.playTime > 0))
            {
                float offlineTime = playerFile.offlineAccumulatedTime;
                if (offlineTime > 0)
                {
                    _idleModule.ApplyOfflineReward(offlineTime);
                    Debug.Log($"[GameLoopManager] Applied offline reward for {offlineTime / 3600f:F1} hours");
                }
            }
        }

        /// <summary>
        /// 注册所有职能存档文件
        /// </summary>
        private void RegisterSaveFiles()
        {
            _saveManager.RegisterFiles(
                new PlayerSaveFile(),
                new WorldSaveFile(),
                new InventorySaveFile(),
                new TeamSaveFile(),
                new SkillSaveFile(),
                new CombatSaveFile(),
                new EventTreeSaveFile(),
                new NpcSaveFile(),
                new PendingEventSaveFile(),
                new PrestigeSaveFile(),
                new ActivitySaveFile(),
                new PetSaveFile(),
                new AchievementSaveFile()
            );
        }

        /// <summary>
        /// 将加载的存档数据应用到各模块（所有12个职能文件）
        /// </summary>
        private void ApplyLoadedSaveData()
        {
            // 1. 应用玩家数据
            var playerFile = _saveManager.GetFile<PlayerSaveFile>();
            if (playerFile != null)
            {
                _player.ApplyFromSaveData(playerFile);
            }
            else
            {
                Debug.Log("[GameLoopManager] No player save data to apply, using default PlayerActor");
            }

            // 2. 恢复输入统计
            if (playerFile != null)
            {
                Debug.Log($"[GameLoopManager] Restoring input count: {playerFile.totalInputCount}");
                _backgroundInput.RestoreInputCount((int)playerFile.totalInputCount);
                Debug.Log("[GameLoopManager] Input count restored successfully");
            }

            // 3. 恢复世界/旅行状态
            var worldFile = _saveManager.GetFile<WorldSaveFile>();
            if (worldFile != null && !string.IsNullOrEmpty(worldFile.currentMapSeed))
            {
                _travelModule.ImportFromSaveData(worldFile);
                Debug.Log($"[GameLoopManager] Restored world state: seed={worldFile.currentMapSeed}, nodeIndex={worldFile.currentMapIndex}");
            }
            else
            {
                Debug.Log("[GameLoopManager] No world save data to restore, will start new journey in GameMain");
            }

            // 4. 恢复游戏时间
            _totalGameTime = playerFile?.playTime ?? 0f;

            // 5. 恢复背包数据（桥接旧InventorySaveData到新InventorySaveFile）
            var inventoryFile = _saveManager.GetFile<InventorySaveFile>();
            if (inventoryFile != null && inventoryFile.items.Count > 0)
            {
                var oldItems = new System.Collections.Generic.List<InventorySaveData>();
                foreach (var item in inventoryFile.items)
                {
                    oldItems.Add(new InventorySaveData
                    {
                        templateId = item.templateId,
                        instanceId = item.instanceId,
                        amount = item.amount
                    });
                }
                InventoryDesign.instance.Import(oldItems);
                Debug.Log($"[GameLoopManager] Restored {oldItems.Count} inventory items");
            }

            // 6. 恢复队伍数据（桥接旧TeamMemberSaveData到新TeamSaveFile）
            var teamFile = _saveManager.GetFile<TeamSaveFile>();
            if (teamFile != null && teamFile.members.Count > 0)
            {
                var oldMembers = new System.Collections.Generic.List<TeamMemberSaveData>();
                foreach (var m in teamFile.members)
                {
                    oldMembers.Add(new TeamMemberSaveData
                    {
                        memberId = m.memberId,
                        actorId = m.actorId,
                        name = m.name,
                        level = m.level,
                        currentHp = m.currentHp,
                        maxHp = m.maxHp,
                        attack = m.attack,
                        defense = m.defense,
                        speed = m.speed,
                        jobType = m.jobType
                    });
                }
                TeamDesign.instance.Import(oldMembers);
                Debug.Log($"[GameLoopManager] Restored {oldMembers.Count} team members");
            }

            // 7. 恢复技能数据（桥接旧MemberSkillSaveData到新SkillSaveFile）
            var skillFile = _saveManager.GetFile<SkillSaveFile>();
            if (skillFile != null && skillFile.skillGroups.Count > 0)
            {
                var oldSkillGroups = new System.Collections.Generic.List<MemberSkillSaveData>();
                foreach (var g in skillFile.skillGroups)
                {
                    var group = new MemberSkillSaveData { memberId = g.memberId };
                    foreach (var s in g.skills)
                    {
                        group.skills.Add(new SkillSaveDataLite
                        {
                            skillId = s.skillId,
                            currentLevel = s.currentLevel
                        });
                    }
                    oldSkillGroups.Add(group);
                }
                SkillDesign.instance.Import(oldSkillGroups);
                Debug.Log($"[GameLoopManager] Restored {oldSkillGroups.Count} skill groups");
            }

            // 8. 恢复战斗数据（桥接旧CombatSaveData到新CombatSaveFile）
            var combatFile = _saveManager.GetFile<CombatSaveFile>();
            if (combatFile != null && _combatModule != null)
            {
                var oldCombat = new CombatSaveData
                {
                    totalBattles = combatFile.totalBattles,
                    victories = combatFile.victories,
                    defeats = combatFile.defeats,
                    totalDamageDealt = combatFile.totalDamageDealt,
                    totalDamageTaken = combatFile.totalDamageTaken,
                    totalGoldEarned = combatFile.totalGoldEarned
                };
                _combatModule.Import(oldCombat);
                Debug.Log("[GameLoopManager] Restored combat data");
            }

            // 9. 恢复NPC数据
            var npcFile = _saveManager.GetFile<NpcSaveFile>();
            if (npcFile != null)
            {
                NPCManager.instance.ImportFromNpcSaveFile(npcFile);
                Debug.Log($"[GameLoopManager] Restored {npcFile.npcs?.Count ?? 0} NPCs");
            }

            // 10. 恢复积压事件数据（桥接旧PendingEventSaveData到新PendingEventSaveFile）
            var pendingFile = _saveManager.GetFile<PendingEventSaveFile>();
            if (pendingFile != null && pendingFile.pendingEvents.Count > 0)
            {
                var oldPending = new PendingEventSaveData();
                foreach (var e in pendingFile.pendingEvents)
                {
                    oldPending.pendingEvents.Add(new PendingEventData
                    {
                        eventId = e.eventId,
                        templateId = e.templateId,
                        rarity = (Game1.Modules.PendingEvent.PendingEventRarity)e.rarity,
                        timestamp = e.timestamp,
                        offlineSeconds = e.offlineSeconds,
                        isProcessed = e.isProcessed,
                        goldReward = e.goldReward
                    });
                }
                Game1.Modules.PendingEvent.PendingEventDesign.instance.Import(oldPending);
                Debug.Log($"[GameLoopManager] Restored {oldPending.pendingEvents.Count} pending events");
            }

            // 11. 恢复轮回数据
            var prestigeFile = _saveManager.GetFile<PrestigeSaveFile>();
            if (prestigeFile != null)
            {
                PrestigeManager.instance.ImportFromPrestigeSaveFile(prestigeFile);
                Debug.Log("[GameLoopManager] Restored prestige data");
            }

            // 12. 恢复活跃度数据
            var activityFile = _saveManager.GetFile<ActivitySaveFile>();
            if (activityFile != null)
            {
                _activityModule?.ImportFromActivitySaveFile(activityFile);
                Debug.Log("[GameLoopManager] Restored activity data");
            }

            // 13. 恢复宠物数据
            var petFile = _saveManager.GetFile<PetSaveFile>();
            if (petFile != null)
            {
                var petModule = _player?.modules.GetModule<PetCompanionModule>();
                petModule?.ImportFromPetSaveFile(petFile);
                Debug.Log("[GameLoopManager] Restored pet data");
            }

            // 14. 恢复事件树运行状态
            var eventTreeFile = _saveManager.GetFile<EventTreeSaveFile>();
            if (eventTreeFile != null && eventTreeFile.isRunning)
            {
                var eventTreeData = new EventTreeRunSaveData
                {
                    templateId = eventTreeFile.templateId,
                    currentNodeId = eventTreeFile.currentNodeId,
                    isRunning = eventTreeFile.isRunning,
                    history = eventTreeFile.history ?? new List<string>()
                };
                EventTreeRunner.instance.RestoreState(eventTreeData);
                Debug.Log("[GameLoopManager] Restored EventTreeRunner state");
            }

            // 15. 成就数据
            var achievementFile = _saveManager.GetFile<AchievementSaveFile>();
            if (achievementFile != null && achievementFile.records.Count > 0)
            {
                var saveData = new AchievementSaveData
                {
                    version = achievementFile.Version,
                    records = achievementFile.records
                };
                AchievementManager.Import(saveData);
                Debug.Log($"[GameLoopManager] Restored {achievementFile.records.Count} achievement records");
            }
        }

        /// <summary>
        /// 将所有模块的当前数据同步到各自的ISaveFile（保存前调用）
        /// </summary>
        private void SyncAllSaveData()
        {
            // 1. 玩家数据
            var playerFile = _saveManager.GetFile<PlayerSaveFile>();
            if (playerFile != null && _player != null)
            {
                var exported = _player.ExportToPlayerSaveFile();
                playerFile.actorId = exported.actorId;
                playerFile.actorName = exported.actorName;
                playerFile.level = exported.level;
                playerFile.gold = exported.gold;
                // playTime 和 totalInputCount 已在 Tick 中同步
            }

            // 2. 世界数据
            var worldFile = _saveManager.GetFile<WorldSaveFile>();
            if (worldFile != null && _travelModule != null)
            {
                var currentWorld = _travelModule.ExportToWorldSaveFile();
                worldFile.currentMapSeed = currentWorld.currentMapSeed;
                worldFile.currentMapIndex = currentWorld.currentMapIndex;
                worldFile.travelProgress = currentWorld.travelProgress;
            }

            // 3. 背包数据（桥接InventorySaveData到InventorySaveFile）
            var inventoryFile = _saveManager.GetFile<InventorySaveFile>();
            if (inventoryFile != null)
            {
                inventoryFile.items.Clear();
                var oldItems = InventoryDesign.instance.Export();
                foreach (var item in oldItems)
                {
                    inventoryFile.items.Add(new InventorySaveFile.ItemEntry
                    {
                        templateId = item.templateId,
                        instanceId = item.instanceId,
                        amount = item.amount
                    });
                }
            }

            // 4. 队伍数据（桥接TeamMemberSaveData到TeamSaveFile）
            var teamFile = _saveManager.GetFile<TeamSaveFile>();
            if (teamFile != null)
            {
                teamFile.members.Clear();
                var oldMembers = TeamDesign.instance.Export();
                foreach (var m in oldMembers)
                {
                    teamFile.members.Add(new TeamSaveFile.MemberEntry
                    {
                        memberId = m.memberId,
                        actorId = m.actorId,
                        name = m.name,
                        level = m.level,
                        currentHp = m.currentHp,
                        maxHp = m.maxHp,
                        attack = m.attack,
                        defense = m.defense,
                        speed = m.speed,
                        jobType = m.jobType
                    });
                }
            }

            // 5. 技能数据（桥接MemberSkillSaveData到SkillSaveFile）
            var skillFile = _saveManager.GetFile<SkillSaveFile>();
            if (skillFile != null)
            {
                skillFile.skillGroups.Clear();
                var oldSkillGroups = SkillDesign.instance.Export();
                foreach (var g in oldSkillGroups)
                {
                    var group = new SkillSaveFile.MemberSkillGroup { memberId = g.memberId };
                    foreach (var s in g.skills)
                    {
                        group.skills.Add(new SkillSaveFile.SkillEntry
                        {
                            skillId = s.skillId,
                            currentLevel = s.currentLevel
                        });
                    }
                    skillFile.skillGroups.Add(group);
                }
            }

            // 6. 战斗数据（桥接CombatSaveData到CombatSaveFile）
            var combatFile = _saveManager.GetFile<CombatSaveFile>();
            if (combatFile != null && _combatModule != null)
            {
                var oldCombat = _combatModule.Export();
                combatFile.totalBattles = oldCombat.totalBattles;
                combatFile.victories = oldCombat.victories;
                combatFile.defeats = oldCombat.defeats;
                combatFile.totalDamageDealt = oldCombat.totalDamageDealt;
                combatFile.totalDamageTaken = oldCombat.totalDamageTaken;
                combatFile.totalGoldEarned = oldCombat.totalGoldEarned;
            }

            // 7. NPC数据
            var npcFile = _saveManager.GetFile<NpcSaveFile>();
            if (npcFile != null)
            {
                var exported = NPCManager.instance.ExportToNpcSaveFile();
                npcFile.npcs = exported.npcs;
            }

            // 8. 积压事件数据（桥接PendingEventSaveData到PendingEventSaveFile）
            var pendingFile = _saveManager.GetFile<PendingEventSaveFile>();
            if (pendingFile != null)
            {
                pendingFile.pendingEvents.Clear();
                var oldPending = Game1.Modules.PendingEvent.PendingEventDesign.instance.Export();
                foreach (var e in oldPending.pendingEvents)
                {
                    pendingFile.pendingEvents.Add(new PendingEventSaveFile.PendingEventEntry
                    {
                        eventId = e.eventId,
                        templateId = e.templateId,
                        rarity = (int)e.rarity,
                        timestamp = e.timestamp,
                        offlineSeconds = e.offlineSeconds,
                        isProcessed = e.isProcessed,
                        goldReward = e.goldReward
                    });
                }
            }

            // 9. 轮回数据
            var prestigeFile = _saveManager.GetFile<PrestigeSaveFile>();
            if (prestigeFile != null)
            {
                var exported = PrestigeManager.instance.ExportToPrestigeSaveFile();
                prestigeFile.prestigeCount = exported.prestigeCount;
                prestigeFile.prestigePoints = exported.prestigePoints;
                prestigeFile.goldRetentionRate = exported.goldRetentionRate;
                prestigeFile.expRetentionRate = exported.expRetentionRate;
                prestigeFile.purchasedUpgrades = exported.purchasedUpgrades;
                prestigeFile.retainedSkills = exported.retainedSkills;
            }

            // 10. 活跃度数据
            var activityFile = _saveManager.GetFile<ActivitySaveFile>();
            if (activityFile != null && _activityModule != null)
            {
                var exported = _activityModule.ExportToActivitySaveFile();
                activityFile.accumulatedActivity = exported.accumulatedActivity;
                activityFile.displayedActivity = exported.displayedActivity;
                activityFile.peakActivity = exported.peakActivity;
            }

            // 11. 宠物数据
            var petFile = _saveManager.GetFile<PetSaveFile>();
            if (petFile != null && _player != null)
            {
                var petModule = _player.modules.GetModule<PetCompanionModule>();
                if (petModule != null)
                {
                    var exported = petModule.ExportToPetSaveFile();
                    petFile.happiness = exported.happiness;
                    petFile.excitement = exported.excitement;
                    petFile.sadness = exported.sadness;
                    petFile.currentState = exported.currentState;
                    petFile.isUnlocked = exported.isUnlocked;
                }
            }

            // 12. 事件树数据（同步运行中的事件树状态到EventTreeSaveFile）
            var eventTreeFile = _saveManager.GetFile<EventTreeSaveFile>();
            if (eventTreeFile != null)
            {
                var runner = EventTreeRunner.instance;
                if (runner.isRunning)
                {
                    var exportData = runner.ExportSaveData();
                    if (exportData != null)
                    {
                        eventTreeFile.templateId = exportData.templateId;
                        eventTreeFile.currentNodeId = exportData.currentNodeId;
                        eventTreeFile.isRunning = exportData.isRunning;
                        eventTreeFile.history = exportData.history;
                    }
                }
                else
                {
                    eventTreeFile.isRunning = false;
                }
            }

            // 13. 成就数据
            var achievementFile = _saveManager.GetFile<AchievementSaveFile>();
            if (achievementFile != null)
            {
                var exported = AchievementManager.Export();
                achievementFile.records = exported.records;
            }

            // 标记所有已同步的文件为脏（由OnBeforeSave触发，仅存盘前执行）
            _saveManager.MarkAllDirty();
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

            // 4. 更新游戏时间和输入次数（直接写入PlayerSaveFile并标记脏）
            _totalGameTime += deltaTime;
            var playerFile = _saveManager.GetFile<PlayerSaveFile>();
            if (playerFile != null)
            {
                playerFile.playTime = (long)_totalGameTime;
                var (totalKeystrokes, _, _) = _backgroundInput?.GetInputStatistics() ?? (0, 0, 1f);
                playerFile.totalInputCount = totalKeystrokes;
                _saveManager.MarkDirty<PlayerSaveFile>();
            }

            // 5. 自动存档（SaveManager.Tick内部调用OnBeforeSave→SyncAllSaveData + SaveDirtyFiles）
            _saveManager.Tick(deltaTime);

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
