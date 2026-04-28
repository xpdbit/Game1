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
        private bool _showActivityInfo = true;
        private bool _showGameInfo = true;

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
        /// 是否显示活跃度信息
        /// </summary>
        public bool showActivityInfo
        {
            get => _showActivityInfo;
            set => _showActivityInfo = value;
        }

        /// <summary>
        /// 是否显示游戏信息
        /// </summary>
        public bool showGameInfo
        {
            get => _showGameInfo;
            set => _showGameInfo = value;
        }

        /// <summary>
        /// 更新调试信息显示
        /// </summary>
        public void Update()
        {
            if (_debugText == null) return;

            _sb.Clear();

            // 游戏信息
            if (_showGameInfo)
            {
                var gm = GameMain.instance;
                if (gm != null)
                {
                    var saveManager = gm.GetService<SaveManager>();
                    var playerFile = saveManager?.GetFile<PlayerSaveFile>();
                    if (playerFile != null)
                    {
                        long totalSeconds = playerFile.playTime;
                        var ts = System.TimeSpan.FromSeconds(totalSeconds);
                        _sb.AppendLine($"游戏时间: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}");
                        _sb.AppendLine($"输入次数: {playerFile.totalInputCount}");
                    }
                }
            }

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
                    _sb.AppendLine($"TravelRate: {pm.travelRate:F1} TP/s (5s平均)");
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

            // 积压事件
            int pendingCount = GameMain.instance?.eventQueue?.pendingCount ?? 0;
            if (pendingCount > 0)
                _sb.AppendLine($"积压事件: {pendingCount}");

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

            // 活跃度信息
            if (_showActivityInfo)
            {
                var activityModule = Modules.Activity.ActivityMonitorModule.instance;
                if (activityModule != null)
                {
                    _sb.AppendLine($"活跃点数: {activityModule.GetCurrentActivity()}");
                    _sb.AppendLine($"衰减活跃: {activityModule.GetDecayedActivity()}");
                    _sb.AppendLine($"历史峰值: {activityModule.peakActivity}");
                    _sb.AppendLine($"活跃率: {activityModule.activityRate:P1}");
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
