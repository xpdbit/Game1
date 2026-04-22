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
        /// 触发事件的进度阈值
        /// </summary>
        public int pointsPerEvent = 1000;

        /// <summary>
        /// 最大进度点上限
        /// </summary>
        public int maxPoints = 9999;
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

        // 事件
        public event Action<ProgressEventData> onProgressChanged;
        public event Action<int> onMilestoneReached;  // 触发事件时
        public event Action onPointsOverflow;          // 进度溢出时

        // 属性
        public int currentPoints => _currentPoints;
        public int totalEarnedPoints => _totalEarnedPoints;
        public int milestoneCount => _milestoneCount;
        public float progressPercent => (float)_currentPoints / _config.pointsPerEvent;

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
            _currentPoints = Mathf.Clamp(points, 0, _config.maxPoints);
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

            // 检查溢出
            if (_currentPoints > _config.maxPoints)
            {
                _currentPoints = _config.maxPoints;
                onPointsOverflow?.Invoke();
            }

            // 检查里程碑
            int oldMilestone = oldPoints / _config.pointsPerEvent;
            int newMilestone = _currentPoints / _config.pointsPerEvent;

            if (newMilestone > oldMilestone)
            {
                _milestoneCount += (newMilestone - oldMilestone);
                for (int i = oldMilestone + 1; i <= newMilestone; i++)
                {
                    onMilestoneReached?.Invoke(i);
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

        private void PublishProgressChange()
        {
            var data = new ProgressEventData
            {
                currentPoints = _currentPoints,
                milestoneReached = _milestoneCount,
                isMilestone = _currentPoints >= _config.pointsPerEvent
            };
            onProgressChanged?.Invoke(data);
        }

        #endregion
    }
}