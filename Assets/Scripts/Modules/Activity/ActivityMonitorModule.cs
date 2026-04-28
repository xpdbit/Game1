using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1.Modules.Activity
{
    /// <summary>
    /// 活跃度监控模块
    /// 使用衰减+增量累积活跃度系统
    /// 活跃度范围: 0-100
    /// </summary>
    [Serializable]
    public class ActivityMonitorModule : IModule
    {
        #region Singleton
        private static ActivityMonitorModule _instance;
        public static ActivityMonitorModule instance => _instance ??= new ActivityMonitorModule();
        #endregion

        #region Constants
        public const int MIN_ACTIVITY = 0;
        public const int MAX_ACTIVITY = 100;

        // 操作键位标识符
        private const int OP_MOUSE_LEFT = -1;
        private const int OP_MOUSE_RIGHT = -2;
        private const int OP_MOUSE_MIDDLE = -3;
        private const int OP_SCROLL = -4;
        #endregion

        #region Serializable Fields
        [SerializeField] private float _decayFactor = 0.85f;
        [SerializeField] private int _baseValuePerMinute = 2;
        [SerializeField] private int _operationIncrementCoefficient = 2;
        [SerializeField] private int _maxConsecutiveSameKey = 4;
        [SerializeField] private float _activityRateHalfSaturation = 20f;
        #endregion

        public string moduleId => "activity_monitor";
        public string moduleName => "活跃度监控";

        #region State
        private PlayerActor _player;
        private bool _isActive;

        // 上一次操作键位 (-1 = 无上一次操作)
        private int _lastOperationKey = -1;

        // 键位连续计数: vkCode -> 连续计数
        private Dictionary<int, int> _keyConsecutiveCount = new();

        // 结算计时器 (1秒)
        private float _settlementTimer = 0f;

        // 操作时间戳队列 (用于5分钟滑动窗口)
        private Queue<float> _operationTimestamps = new();

        // 活跃度数据
        private float _accumulatedActivity;     // 累积活跃度(浮点数内部计算)
        private int _displayedActivity;          // 显示用活跃度(整数)
        private int _peakActivity;                // 历史峰值

        // 滚轮累积值追踪（Input System返回累积值，需手动算增量）
        private Vector2 _lastScrollValue;
        #endregion

        #region Events
        public event Action<int> onActivityChanged;
        public event Action<int> onDecayApplied;
        public event Action onPrecisionCalibration;
        #endregion

        #region Properties
        /// <summary>
        /// 当前活跃度
        /// </summary>
        public int currentActivity => _displayedActivity;

        /// <summary>
        /// 衰减后活跃度(与displayedActivity相同,兼容旧接口)
        /// </summary>
        public int decayedActivity => _displayedActivity;

        /// <summary>
        /// 历史峰值
        /// </summary>
        public int peakActivity => _peakActivity;

        /// <summary>
        /// 活跃点数 (显示用活跃度的别名，支持从"活跃度"到"活跃点数"的命名迁移)
        /// </summary>
        public int activityPoints => _displayedActivity;

        /// <summary>
        /// 活跃度比率 (非线性的0-1值，使用公式 y = x / (x + C) 计算)
        /// C = _activityRateHalfSaturation (默认20f)
        /// </summary>
        public float activityRate => _accumulatedActivity / (_accumulatedActivity + _activityRateHalfSaturation);
        #endregion

        /// <summary>
        /// 计算活跃度比率
        /// 公式: y = x / (x + C)，其中 C = _activityRateHalfSaturation
        /// - x=0 时 y=0
        /// - x=C 时 y=0.5
        /// - x→∞ 时 y→1
        /// </summary>
        /// <param name="x">累积活跃度值</param>
        /// <returns>活跃度比率 (0-1之间)</returns>
        public static float CalculateActivityRate(float x)
        {
            return x / (x + 20f);
        }

        /// <summary>
        /// 活跃度等级
        /// </summary>
        public enum ActivityTier
        {
            Low = 0,      // < 30
            Normal = 1,   // >= 30 && < 70
            High = 2      // >= 70
        }

        /// <summary>
        /// 初始化模块
        /// </summary>
        public void Initialize(PlayerActor player)
        {
            _player = player;
            ResetState();
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        private void ResetState()
        {
            _accumulatedActivity = 0f;
            _displayedActivity = 0;
            _peakActivity = 0;
            _settlementTimer = 0f;
            _operationTimestamps.Clear();
            _lastOperationKey = -1;
            _keyConsecutiveCount.Clear();
        }

        /// <summary>
        /// 获取当前活跃度
        /// </summary>
        public int GetCurrentActivity()
        {
            return _displayedActivity;
        }

        /// <summary>
        /// 导出活跃度数据到存档文件
        /// </summary>
        public ActivitySaveFile ExportToActivitySaveFile()
        {
            return new ActivitySaveFile
            {
                accumulatedActivity = _accumulatedActivity,
                displayedActivity = _displayedActivity,
                peakActivity = _peakActivity
            };
        }

        /// <summary>
        /// 从存档文件恢复活跃度数据
        /// </summary>
        public void ImportFromActivitySaveFile(ActivitySaveFile saveFile)
        {
            if (saveFile == null) return;
            _accumulatedActivity = saveFile.accumulatedActivity;
            _displayedActivity = saveFile.displayedActivity;
            _peakActivity = saveFile.peakActivity;
        }

        /// <summary>
        /// 获取衰减后活跃度
        /// </summary>
        public int GetDecayedActivity()
        {
            return _displayedActivity;
        }

        /// <summary>
        /// 获取历史峰值
        /// </summary>
        public int GetPeakActivity()
        {
            return _peakActivity;
        }

        /// <summary>
        /// 获取当前活跃度等级
        /// </summary>
        public ActivityTier GetCurrentTier()
        {
            if (_displayedActivity >= 70)
                return ActivityTier.High;
            if (_displayedActivity >= 30)
                return ActivityTier.Normal;
            return ActivityTier.Low;
        }

        /// <summary>
        /// 判断操作是否有效
        /// - 同一键位连续输入超过_maxConsecutiveSameKey次视为无效
        /// - 不同键位重置前一个键位的计数
        /// </summary>
        private bool IsValidOperation(int operationKey)
        {
            if (_lastOperationKey == -1)
            {
                // 第一次操作
                _lastOperationKey = operationKey;
                _keyConsecutiveCount[operationKey] = 1;
                return true;
            }

            if (operationKey == _lastOperationKey)
            {
                // 与上一次相同键位
                if (!_keyConsecutiveCount.ContainsKey(operationKey))
                    _keyConsecutiveCount[operationKey] = 0;

                _keyConsecutiveCount[operationKey]++;

                if (_keyConsecutiveCount[operationKey] > _maxConsecutiveSameKey)
                {
                    // 超过最大连续次数,无效
                    return false;
                }
                return true;
            }
            else
            {
                // 不同键位,重置前一个键位的计数
                if (_keyConsecutiveCount.ContainsKey(_lastOperationKey))
                    _keyConsecutiveCount[_lastOperationKey] = 0;

                // 设置新的键位计数
                _lastOperationKey = operationKey;
                _keyConsecutiveCount[operationKey] = 1;
                return true;
            }
        }

        /// <summary>
        /// 处理有效操作
        /// </summary>
        private void ProcessValidOperation()
        {
            _operationTimestamps.Enqueue(Time.time);

            // 立即增加0.5f活跃度，让操作有即时反馈
            _accumulatedActivity += 0.5f;
            _accumulatedActivity = Mathf.Clamp(_accumulatedActivity, MIN_ACTIVITY, MAX_ACTIVITY);
            _displayedActivity = (int)_accumulatedActivity;

            if (_displayedActivity > _peakActivity)
                _peakActivity = _displayedActivity;

            onActivityChanged?.Invoke(_displayedActivity);
        }

        /// <summary>
        /// 订阅输入事件
        /// </summary>
        private void SubscribeToInputEvents()
        {
            BackgroundInputManager.instance.onKeyDown += OnKeyDown;
            BackgroundInputManager.instance.onLeftMouseClicked += OnLeftMouseClicked;
            BackgroundInputManager.instance.onRightMouseClicked += OnRightMouseClicked;
            BackgroundInputManager.instance.onMiddleMouseClicked += OnMiddleMouseClicked;
        }

        /// <summary>
        /// 取消订阅输入事件
        /// </summary>
        private void UnsubscribeFromInputEvents()
        {
            BackgroundInputManager.instance.onKeyDown -= OnKeyDown;
            BackgroundInputManager.instance.onLeftMouseClicked -= OnLeftMouseClicked;
            BackgroundInputManager.instance.onRightMouseClicked -= OnRightMouseClicked;
            BackgroundInputManager.instance.onMiddleMouseClicked -= OnMiddleMouseClicked;
        }

        /// <summary>
        /// 键盘事件处理
        /// </summary>
        private void OnKeyDown(int vkCode)
        {
            if (!_isActive) return;

            // vkCode >= 0 为键盘按键
            if (!IsValidOperation(vkCode)) return;

            ProcessValidOperation();
        }

        /// <summary>
        /// 鼠标左键点击处理
        /// </summary>
        private void OnLeftMouseClicked()
        {
            if (!_isActive) return;

            if (!IsValidOperation(OP_MOUSE_LEFT)) return;

            ProcessValidOperation();
        }

        /// <summary>
        /// 鼠标右键点击处理
        /// </summary>
        private void OnRightMouseClicked()
        {
            if (!_isActive) return;

            if (!IsValidOperation(OP_MOUSE_RIGHT)) return;

            ProcessValidOperation();
        }

        /// <summary>
        /// 鼠标中键点击处理
        /// </summary>
        private void OnMiddleMouseClicked()
        {
            if (!_isActive) return;

            if (!IsValidOperation(OP_MOUSE_MIDDLE)) return;

            ProcessValidOperation();
        }

        /// <summary>
        /// 精准校准触发
        /// </summary>
        private void OnPrecisionCalibration()
        {
            if (!_isActive) return;
            onPrecisionCalibration?.Invoke();
        }

        #region IModule Members

        public string GetBonus(string bonusType)
        {
            if (bonusType == "activity_rate")
                return _displayedActivity.ToString();
            return "0";
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive) return;

            // 检测鼠标滚轮滚动
            // 使用 Input System 读取鼠标滚轮（累积值），计算帧间增量
            Vector2 currentScroll = Mouse.current.scroll.ReadValue();
            float scrollDelta = currentScroll.y - _lastScrollValue.y;
            _lastScrollValue = currentScroll;

            if (Mathf.Abs(scrollDelta) > 0.001f)
            if (scrollDelta != 0f)
            {
                if (IsValidOperation(OP_SCROLL))
                {
                    ProcessValidOperation();
                }
            }

            // 1秒结算 (滑动窗口)
            _settlementTimer += deltaTime;
            if (_settlementTimer >= 1f)
            {
                _settlementTimer -= 1f;

                // 移除5分钟前的操作记录
                float cutoffTime = Time.time - 300f;
                while (_operationTimestamps.Count > 0 && _operationTimestamps.Peek() < cutoffTime)
                {
                    _operationTimestamps.Dequeue();
                }

                // 计算近5分钟的有效操作次数
                int recentOpsCount = _operationTimestamps.Count;

                // 每秒衰减: 0.85^(1/60) ≈ 0.9974
                float decayedValue = _accumulatedActivity * Mathf.Pow(_decayFactor, 1f / 60f);

                // 增量: 将5分钟操作数归一化到每分钟再除以60得到每秒
                float incrementValue = (recentOpsCount / 5f) * _operationIncrementCoefficient / 60f;

                // 基础值: 每秒基础值
                float baseValue = _baseValuePerMinute / 60f;

                // 计算新活跃度
                float newActivity = decayedValue + incrementValue + baseValue;

                // 限制在有效范围内
                _accumulatedActivity = Mathf.Clamp(newActivity, MIN_ACTIVITY, MAX_ACTIVITY);
                _displayedActivity = (int)_accumulatedActivity;

                // 更新峰值
                if (_displayedActivity > _peakActivity)
                    _peakActivity = _displayedActivity;

                // 触发衰减事件
                onDecayApplied?.Invoke(_displayedActivity);
            }
        }

        public void OnActivate()
        {
            _isActive = true;
            SubscribeToInputEvents();
        }

        public void OnDeactivate()
        {
            _isActive = false;
            UnsubscribeFromInputEvents();
        }

        #endregion

        #if UNITY_EDITOR
        /// <summary>
        /// 测试用：模拟输入操作（仅编辑器测试使用）
        /// </summary>
        public void SimulateInputForTest(int operationKey)
        {
            if (!_isActive) return;

            if (IsValidOperation(operationKey))
            {
                ProcessValidOperation();
            }
        }
        #endif
    }
}