using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 进度系统配置
    /// </summary>
    [Serializable]
    public class ProgressConfig
    {
        /// <summary>
        /// 每秒获得的进度点
        /// </summary>
        public float pointsPerSecond = 1f;

        /// <summary>
        /// 每次点击获得的进度点
        /// </summary>
        public int pointsPerClick = 10;

        /// <summary>
        /// 触发普通事件的进度阈值（每200点触发一次）
        /// </summary>
        public int pointsPerNormalEvent = 200;

        /// <summary>
        /// 触发事件树的进度阈值（每1000点触发一次）
        /// </summary>
        public int pointsPerEventTree = 1000;

        /// <summary>
        /// 进度点尺寸上限，超出时归零重新累计
        /// </summary>
        public int travelPointSize = 1000;
    }

    /// <summary>
    /// 进度事件数据
    /// </summary>
    public class ProgressEventData
    {
        public int currentPoints;
        public int milestoneReached;
        public bool isMilestone;
    }

    /// <summary>
    /// 进度管理器
    /// 管理旅行进度点，每1000点触发事件
    /// </summary>
    public class ProgressManager
    {
        #region Singleton
        private static ProgressManager _instance;
        public static ProgressManager instance => _instance ??= new ProgressManager();
        #endregion

        private ProgressConfig _config = new();
        private int _currentPoints = 0;
        private int _totalEarnedPoints = 0;
        private int _milestoneCount = 0;
        private float _accumulatedTime = 0f;  // 累积时间，用于精确计算

        // TravelRate计算：使用滑动窗口记录过去60秒的每秒点数
        private const int TRAVEL_RATE_SECONDS = 60;
        private readonly float[] _pointsPerSecond = new float[TRAVEL_RATE_SECONDS];  // 每秒的点数
        private int _currentSecondIndex = 0;  // 当前秒对应的索引
        private float _lastRecordTime = 0f;  // 上次记录的时间（秒）
        private float _travelRate = 0f;  // 过去60秒平均每秒获得的TravelPoint
        private float _accumulatedPointsThisSecond = 0f;  // 当前秒累积的点数

        // 事件
        public event Action<ProgressEventData> onProgressChanged;
        public event Action<int> onNormalEventTriggered;  // 触发普通事件时（每200点）
        public event Action<int> onEventTreeTriggered;     // 触发事件树时（每1000点）
        public event Action onPointsOverflow;              // 进度溢出时

        // 属性
        public int currentPoints => _currentPoints;
        public int totalEarnedPoints => _totalEarnedPoints;
        public int milestoneCount => _milestoneCount;
        public float progressPercent => (float)_currentPoints / _config.travelPointSize;
        public float travelRate => _travelRate;  // 过去60秒平均TravelPoint/秒

        public ProgressConfig config
        {
            get => _config;
            set => _config = value ?? new ProgressConfig();
        }

        #region Public API

        /// <summary>
        /// 增加进度点（挂机时每秒调用）
        /// </summary>
        public void AddPoints(float deltaTime)
        {
            // 累积时间，每秒获得1点
            _accumulatedTime += deltaTime;
            if (_accumulatedTime >= 1f)
            {
                int pointsToAdd = Mathf.FloorToInt(_accumulatedTime);
                _accumulatedTime -= pointsToAdd;
                AddPointsInternal(pointsToAdd);
            }
        }

        /// <summary>
        /// 增加进度点（点击时调用）
        /// </summary>
        public void AddPointsClick()
        {
            AddPointsInternal(_config.pointsPerClick);
        }

        /// <summary>
        /// 直接设置进度点（用于测试）
        /// </summary>
        public void SetPoints(int points)
        {
            _currentPoints = Mathf.Clamp(points, 0, _config.travelPointSize);
            PublishProgressChange();
        }

        /// <summary>
        /// 增加指定数量进度点
        /// </summary>
        public void AddPointsInternal(int amount)
        {
            if (amount <= 0) return;

            int oldPoints = _currentPoints;
            _currentPoints += amount;
            _totalEarnedPoints += amount;

            // 记录每秒点数用于IdleRate计算
            RecordPointsPerSecond(amount);

            // 检查溢出，归零重新累计
            if (_currentPoints >= _config.travelPointSize)
            {
                _currentPoints = _currentPoints % _config.travelPointSize;
                onPointsOverflow?.Invoke();
            }

            // 检查普通事件阈值（每200点触发一次）
            int oldNormalEvent = oldPoints / _config.pointsPerNormalEvent;
            int newNormalEvent = _currentPoints / _config.pointsPerNormalEvent;

            if (newNormalEvent > oldNormalEvent)
            {
                int eventsTriggered = newNormalEvent - oldNormalEvent;
                for (int i = 0; i < eventsTriggered; i++)
                {
                    onNormalEventTriggered?.Invoke(oldNormalEvent + i + 1);
                }
            }

            // 检查事件树阈值（每1000点触发一次）
            int oldEventTree = oldPoints / _config.pointsPerEventTree;
            int newEventTree = _currentPoints / _config.pointsPerEventTree;

            if (newEventTree > oldEventTree)
            {
                _milestoneCount += (newEventTree - oldEventTree);
                for (int i = oldEventTree + 1; i <= newEventTree; i++)
                {
                    onEventTreeTriggered?.Invoke(i);
                }
            }

            PublishProgressChange();
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void Reset()
        {
            _currentPoints = 0;
            _totalEarnedPoints = 0;
            _milestoneCount = 0;
            _accumulatedTime = 0f;

            // 重置TravelRate计算
            for (int i = 0; i < TRAVEL_RATE_SECONDS; i++)
            {
                _pointsPerSecond[i] = 0f;
            }
            _currentSecondIndex = 0;
            _lastRecordTime = 0f;
            _accumulatedPointsThisSecond = 0f;
            _travelRate = 0f;

            PublishProgressChange();
        }

        /// <summary>
        /// 消耗进度点
        /// </summary>
        public bool ConsumePoints(int amount)
        {
            if (_currentPoints < amount) return false;
            _currentPoints -= amount;
            PublishProgressChange();
            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 记录点数（按秒累加）
        /// </summary>
        private void RecordPointsPerSecond(int points)
        {
            float currentTime = Time.time;
            int currentSecond = Mathf.FloorToInt(currentTime);
            int lastSecond = Mathf.FloorToInt(_lastRecordTime);

            // 检查是否进入新的一秒
            if (currentSecond != lastSecond)
            {
                // 新的一秒开始，将上一秒的点数存入数组
                _pointsPerSecond[_currentSecondIndex] = _accumulatedPointsThisSecond;

                // 移动到下一个槽位
                _currentSecondIndex = (_currentSecondIndex + 1) % TRAVEL_RATE_SECONDS;
                _accumulatedPointsThisSecond = 0f;
                _lastRecordTime = currentTime;

                // 更新TravelRate
                UpdateTravelRate();
            }

            // 累加当前秒的点数
            _accumulatedPointsThisSecond += points;
        }

        /// <summary>
        /// 更新TravelRate（过去60秒平均每秒点数）
        /// </summary>
        private void UpdateTravelRate()
        {
            float total = 0f;
            for (int i = 0; i < TRAVEL_RATE_SECONDS; i++)
            {
                total += _pointsPerSecond[i];
            }
            _travelRate = total / TRAVEL_RATE_SECONDS;
        }

        private void PublishProgressChange()
        {
            var data = new ProgressEventData
            {
                currentPoints = _currentPoints,
                milestoneReached = _milestoneCount,
                isMilestone = _currentPoints >= _config.pointsPerEventTree
            };
            onProgressChanged?.Invoke(data);
        }

        #endregion
    }
}