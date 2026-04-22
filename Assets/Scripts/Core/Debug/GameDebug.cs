using System;
using System.Text;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 游戏调试信息管理器
    /// 提供运行时调试信息显示接口
    /// </summary>
    public class GameDebug
    {
        #region Singleton
        private static GameDebug _instance;
        public static GameDebug instance => _instance ??= new GameDebug();
        #endregion

        private UIText _debugText;
        private readonly StringBuilder _sb = new();

        // 调试信息分类
        private bool _showTravelPoints = true;
        private bool _showIdleInfo = true;
        private bool _showEventInfo = false;
        private bool _showPerformanceInfo = false;

        // 缓存
        private float _lastUpdateTime;
        private float _fps;
        private int _frameCount;

        /// <summary>
        /// 设置调试文本组件
        /// </summary>
        public UIText debugText
        {
            get => _debugText;
            set => _debugText = value;
        }

        /// <summary>
        /// 是否显示旅行进度
        /// </summary>
        public bool showTravelPoints
        {
            get => _showTravelPoints;
            set => _showTravelPoints = value;
        }

        /// <summary>
        /// 是否显示挂机信息
        /// </summary>
        public bool showIdleInfo
        {
            get => _showIdleInfo;
            set => _showIdleInfo = value;
        }

        /// <summary>
        /// 是否显示事件信息
        /// </summary>
        public bool showEventInfo
        {
            get => _showEventInfo;
            set => _showEventInfo = value;
        }

        /// <summary>
        /// 是否显示性能信息
        /// </summary>
        public bool showPerformanceInfo
        {
            get => _showPerformanceInfo;
            set => _showPerformanceInfo = value;
        }

        /// <summary>
        /// 更新调试信息显示
        /// </summary>
        public void Update()
        {
            if (_debugText == null) return;

            _sb.Clear();

            // 性能信息
            if (_showPerformanceInfo)
            {
                UpdateFPS();
                _sb.AppendLine($"FPS: {_fps:F0}");
            }

            // 旅行进度
            if (_showTravelPoints)
            {
                var pm = ProgressManager.instance;
                if (pm != null)
                {
                    _sb.AppendLine($"TravelPoint: {pm.currentPoints}/{pm.config.travelPointSize}");
                    _sb.AppendLine($"TravelRate: {pm.travelRate:F1} TP/s (60s平均)");
                    _sb.AppendLine($"Milestone: {pm.milestoneCount}");
                }
            }

            // 挂机信息
            if (_showIdleInfo)
            {
                var player = GameMain.instance?.GetPlayerActor();
                if (player != null)
                {
                    var idleModule = player.modules.GetModule<IdleRewardModule>();
                    if (idleModule != null)
                    {
                        _sb.AppendLine($"金币IdleRate: {idleModule.GetCurrentRewardRate():F1} 金币/秒");
                    }
                    _sb.AppendLine($"Gold: {player.carryItems.gold}");
                }
            }

            // 事件信息
            if (_showEventInfo)
            {
                var runner = EventTreeRunner.instance;
                if (runner != null && runner.isRunning)
                {
                    _sb.AppendLine($"EventTree: {runner.currentTemplate?.name ?? "Unknown"}");
                    _sb.AppendLine($"State: {runner.state}");
                }
            }

            _debugText.text = _sb.ToString();
        }

        /// <summary>
        /// 添加自定义调试行
        /// </summary>
        public void AddLine(string line)
        {
            _sb.AppendLine(line);
        }

        /// <summary>
        /// 清空调试信息
        /// </summary>
        public void Clear()
        {
            if (_debugText != null)
            {
                _debugText.text = string.Empty;
            }
        }

        private void UpdateFPS()
        {
            _frameCount++;
            float time = Time.time;
            if (time - _lastUpdateTime >= 1f)
            {
                _fps = _frameCount / (time - _lastUpdateTime);
                _frameCount = 0;
                _lastUpdateTime = time;
            }
        }
    }
}
