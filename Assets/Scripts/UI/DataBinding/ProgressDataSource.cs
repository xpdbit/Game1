using System;
using Game1;

namespace Game1.UI.DataBinding
{
    /// <summary>
    /// ProgressData数据类，用于UI绑定
    /// </summary>
    [Serializable]
    public class ProgressData
    {
        public int currentPoints;
        public int milestoneCount;
        public float progressPercent;
        public float travelRate;
        public bool isMilestone;
    }

    /// <summary>
    /// ProgressManager的数据源封装
    /// 封装ProgressManager的事件，转换为IDataSource<ProgressData>
    /// </summary>
    public class ProgressDataSource : BaseDataSource<ProgressData>
    {
        private ProgressManager _progressManager;

        public ProgressDataSource() : base(new ProgressData())
        {
            _progressManager = ProgressManager.instance;
            _progressManager.onProgressChanged += OnProgressChanged;
        }

        private void OnProgressChanged(ProgressEventData data)
        {
            var newData = new ProgressData
            {
                currentPoints = data.currentPoints,
                milestoneCount = data.milestoneReached,
                progressPercent = _progressManager.progressPercent,
                travelRate = _progressManager.travelRate,
                isMilestone = data.isMilestone
            };

            // 使用base的Value属性来触发比较和通知
            Value = newData;
        }

        protected override void OnDispose()
        {
            if (_progressManager != null)
            {
                _progressManager.onProgressChanged -= OnProgressChanged;
            }
            _progressManager = null;
        }
    }
}