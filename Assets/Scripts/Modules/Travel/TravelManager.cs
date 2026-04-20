using System;

namespace Game1
{
    /// <summary>
    /// 旅行管理器
    /// </summary>
    public class TravelManager
    {
        private WorldMap _worldMap;
        private PlayerActor _player;
        private float _travelSpeed = 1f; // 旅行速度倍率

        // 状态
        public enum TravelStatus
        {
            Idle,
            Traveling,
            AwaitingChoice, // 等待玩家选择路径
            EventActive,
        }

        public TravelStatus status { get; private set; } = TravelStatus.Idle;
        public float currentProgress => _player?.travelState.progress ?? 0f;

        public event Action<TravelStatus> onStatusChanged;
        public event Action onTravelCompleted;

        public TravelManager()
        {
            _worldMap = new WorldMap();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(PlayerActor player)
        {
            _player = player;
        }

        /// <summary>
        /// 开始新旅程
        /// </summary>
        public void StartNewJourney(string seed)
        {
            _worldMap.Generate(seed);
            _player.travelState.StartTravel(
                _worldMap.currentLocation?.id ?? "",
                _worldMap.nextLocation?.id ?? "",
                _worldMap.nextLocation?.travelTime ?? 10f
            );
            SetStatus(TravelStatus.Traveling);
        }

        /// <summary>
        /// Tick - 更新旅行进度
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (status != TravelStatus.Traveling) return;
            if (_player == null) return;

            // 更新进度
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
            if (currentLoc?.hasEvent == true)
            {
                SetStatus(TravelStatus.EventActive);
                // TODO: 触发事件系统
            }
            else
            {
                // 自动前进到下一个节点
                AdvanceToNextNode();
            }

            onTravelCompleted?.Invoke();
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

        /// <summary>
        /// 获取当前可选择的路径
        /// </summary>
        public System.Collections.Generic.List<Location> GetCurrentChoices()
        {
            if (status == TravelStatus.AwaitingChoice)
            {
                return _worldMap.GetCurrentConnections();
            }
            return new System.Collections.Generic.List<Location>();
        }
    }
}
