using System;
using System.Collections.Generic;
using UnityEngine;
using Game1.Modules.Combat;

namespace Game1.Modules.Travel
{
    /// <summary>
    /// 旅行管理器
    /// 管理旅行进度、路径选择、事件触发
    /// 实现ITravelManager接口以支持VContainer DI
    /// </summary>
    public class TravelManager : ITravelManager
    {
        #region Singleton
        private static TravelManager _instance;
        public static TravelManager instance => _instance ??= new TravelManager();
        #endregion

        private WorldMap _worldMap;
        private PlayerActor _player;
        private EventQueue _eventQueue;
        private float _travelSpeed = 1f;

        // 状态
        public enum TravelStatus
        {
            Idle,
            Traveling,
            AwaitingChoice,
            EventActive,
            ProgressMilestone  // 进度里程碑触发
        }

        public TravelStatus status { get; private set; } = TravelStatus.Idle;
        public float currentProgress => _player?.travelState.progress ?? 0f;
        public float progressToNextEvent => 1f - currentProgress;

        public event Action<TravelStatus> onStatusChanged;
        public event Action onTravelCompleted;
        public event Action<int> onMilestoneReached;  // 进度里程碑事件

        public TravelManager()
        {
            _worldMap = new WorldMap();
        }

        #region ITravelManager Implementation

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(PlayerActor player)
        {
            _player = player;
        }

        /// <summary>
        /// 设置事件队列引用
        /// </summary>
        public void SetEventQueue(EventQueue eventQueue)
        {
            _eventQueue = eventQueue;
        }

        /// <summary>
        /// 触发事件链
        /// </summary>
        public void TriggerEventChain(string eventChainId)
        {
            // TODO: 实现事件链触发
            Debug.Log($"[TravelManager] TriggerEventChain: {eventChainId}");
        }

        /// <summary>
        /// 玩家交互
        /// </summary>
        public void OnPlayerInteract()
        {
            // 允许在大多数状态下增加进度点，除了正在处理需要玩家专注选择的情况
            if (status == TravelStatus.AwaitingChoice)
            {
                // 等待选择时不增加进度点，需要玩家专注当前选择
                return;
            }
            ProgressManager.instance.AddPointsClick();
        }

        /// <summary>
        /// Tick - 更新旅行进度和进度点
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_player == null) return;

            switch (status)
            {
                case TravelStatus.Traveling:
                    TickTraveling(deltaTime);
                    break;
                case TravelStatus.ProgressMilestone:
                    // 处理里程碑事件
                    break;
            }

            // 更新挂机进度点
            if (status == TravelStatus.Traveling || status == TravelStatus.Idle)
            {
                ProgressManager.instance.AddPoints(deltaTime);
            }
        }

        #endregion

        #region Journey Control

        /// <summary>
        /// 开始新旅程
        /// </summary>
        public void StartNewJourney(string seed)
        {
            if (_player == null)
            {
                Debug.LogWarning("[TravelManager] StartNewJourney called but _player is null, skipping");
                return;
            }

            _worldMap.Generate(seed);
            ProgressManager.instance.Reset();
            _player.travelState.StartTravel(
                _worldMap.currentLocation?.id ?? "",
                _worldMap.nextLocation?.id ?? "",
                _worldMap.nextLocation?.travelTime ?? 10f
            );
            SetStatus(TravelStatus.Traveling);
        }

        /// <summary>
        /// 前进到下一个节点
        /// </summary>
        public void AdvanceToNextNode()
        {
            if (_worldMap.MoveToNext())
            {
                var nextLoc = _worldMap.nextLocation;
                _player.travelState.StartTravel(
                    _worldMap.currentLocation?.id ?? "",
                    nextLoc?.id ?? "",
                    nextLoc?.travelTime ?? 10f
                );
                SetStatus(TravelStatus.Traveling);
            }
            else
            {
                // 已到达终点
                SetStatus(TravelStatus.Idle);
                // TODO: 触发终点结算
            }
        }

        /// <summary>
        /// 完成事件后继续旅行
        /// </summary>
        public void CompleteEventAndContinue()
        {
            _player.travelState.Complete();
            AdvanceToNextNode();
        }

        /// <summary>
        /// 设置速度倍率
        /// </summary>
        public void SetSpeedMultiplier(float speed)
        {
            _travelSpeed = speed;
        }

        #endregion

        #region Journey Control (continued)>
        /// 处理旅行中的Tick
        /// </summary>
        private void TickTraveling(float deltaTime)
        {
            // 更新旅行进度
            float speedBonus = _player.GetTotalBonus("travel_speed");
            float adjustedTime = deltaTime * _travelSpeed * (1f + speedBonus);
            _player.travelState.UpdateProgress(adjustedTime);

            // 检查是否到达
            if (_player.travelState.currentState == TravelState.State.Arrived)
            {
                OnTravelCompleted();
            }
        }

        /// <summary>
        /// 旅行完成处理
        /// </summary>
        private void OnTravelCompleted()
        {
            var currentLoc = _worldMap.currentLocation;

            // 检查是否有事件
            if (currentLoc?.hasEvent == true && _eventQueue != null)
            {
                SetStatus(TravelStatus.EventActive);
                var gameEvent = CreateEvent(currentLoc.eventId, currentLoc);
                if (gameEvent != null)
                {
                    _eventQueue.Enqueue(gameEvent);
                }
                var result = _eventQueue.ProcessNext();
                if (result != null)
                {
                    _player.ApplyEventResult(result);
                }
                CompleteEventAndContinue();
            }
            else
            {
                // 检查进度里程碑
                CheckProgressMilestone();

                // 自动前进到下一个节点
                AdvanceToNextNode();
            }

            onTravelCompleted?.Invoke();
        }

        /// <summary>
        /// 检查进度里程碑
        /// </summary>
        private void CheckProgressMilestone()
        {
            if (ProgressManager.instance.milestoneCount > 0)
            {
                // 有新的里程碑触发事件
                int milestone = ProgressManager.instance.milestoneCount;
                onMilestoneReached?.Invoke(milestone);

                // 触发随机事件
                TriggerMilestoneEvent();
            }
        }

        /// <summary>
        /// 触发里程碑事件
        /// </summary>
        private void TriggerMilestoneEvent()
        {
            if (_eventQueue == null) return;

            // 创建随机事件
            var randomEvent = CreateRandomEvent();
            if (randomEvent != null)
            {
                _eventQueue.Enqueue(randomEvent);
                var result = _eventQueue.ProcessNext();
                if (result != null)
                {
                    _player.ApplyEventResult(result);
                }
            }
        }

        #endregion

        #region Event Creation

        /// <summary>
        /// 根据eventId创建游戏事件
        /// </summary>
        private IGameEvent CreateEvent(string eventId, Location location)
        {
            if (string.IsNullOrEmpty(eventId)) return null;

            switch (eventId)
            {
                case "combat_001":
                case "combat_002":
                case "combat_003":
                    var combatEventEx = new CombatEventEx
                    {
                        enemyCount = Math.Max(1, location.baseReward / 20),
                        enemyStrength = Math.Max(10, location.baseReward / 10)
                    };
                    return combatEventEx;

                case "trade_001":
                case "trade_002":
                    return new TradeEvent();

                case "npc_001":
                    // NPC遭遇事件
                    return CreateNPCEvent(location);

                default:
                    return CreateRandomEvent();
            }
        }

        /// <summary>
        /// 创建随机事件
        /// </summary>
        private IGameEvent CreateRandomEvent()
        {
            int eventType = UnityEngine.Random.Range(0, 3);
            return eventType switch
            {
                0 => new CombatEventEx
                {
                    enemyCount = UnityEngine.Random.Range(1, 4),
                    enemyStrength = UnityEngine.Random.Range(10, 50)
                },
                1 => new TradeEvent(),
                _ => CreateRandomNPCEvent()
            };
        }

        /// <summary>
        /// 创建NPC事件
        /// </summary>
        private IGameEvent CreateNPCEvent(Location location)
        {
            // 根据位置类型决定NPC态度
            var npcType = location.type switch
            {
                LocationType.City => NPCType.Friendly,
                LocationType.Market => NPCType.Neutral,
                LocationType.Dungeon => NPCType.Hostile,
                LocationType.Boss => NPCType.Hostile,
                _ => NPCType.Neutral
            };

            // TODO: 创建NPC遭遇事件
            return new CombatEventEx
            {
                enemyCount = 1,
                enemyStrength = 20
            };
        }

        /// <summary>
        /// 创建随机NPC事件
        /// </summary>
        private IGameEvent CreateRandomNPCEvent()
        {
            int typeRoll = UnityEngine.Random.Range(0, 100);
            var npcType = typeRoll switch
            {
                < 20 => NPCType.Friendly,
                < 40 => NPCType.Allied,
                < 70 => NPCType.Neutral,
                _ => NPCType.Hostile
            };

            return new CombatEventEx
            {
                enemyCount = 1,
                enemyStrength = npcType == NPCType.Hostile ? 25 : 15
            };
        }

        #endregion

        #region Path Selection

        /// <summary>
        /// 请求选择路径（进入AwaitingChoice状态）
        /// </summary>
        public void RequestPathSelection()
        {
            var choices = _worldMap.GetCurrentConnections();
            if (choices.Count > 0)
            {
                SetStatus(TravelStatus.AwaitingChoice);
            }
            else
            {
                // 没有选择，直接前进
                AdvanceToNextNode();
            }
        }

        /// <summary>
        /// 选择路径
        /// </summary>
        public void SelectPath(Location chosenLocation)
        {
            if (status != TravelStatus.AwaitingChoice) return;

            // 更新世界地图到选定位置
            _worldMap.MoveToLocation(chosenLocation.id);

            // 开始新的旅行段
            var nextLoc = _worldMap.nextLocation;
            _player.travelState.StartTravel(
                _worldMap.currentLocation?.id ?? "",
                nextLoc?.id ?? "",
                nextLoc?.travelTime ?? 10f
            );

            SetStatus(TravelStatus.Traveling);
        }

        /// <summary>
        /// 获取当前可选择的路径
        /// </summary>
        public List<Location> GetCurrentChoices()
        {
            if (status == TravelStatus.AwaitingChoice)
            {
                return _worldMap.GetCurrentConnections();
            }
            return new List<Location>();
        }

        #endregion

        #region Status Management

        private void SetStatus(TravelStatus newStatus)
        {
            if (status != newStatus)
            {
                status = newStatus;
                onStatusChanged?.Invoke(status);
            }
        }

        /// <summary>
        /// 获取当前世界地图
        /// </summary>
        public WorldMap GetWorldMap() => _worldMap;

        #endregion
    }
}