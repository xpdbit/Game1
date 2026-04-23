using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 虚实交互输入转换器
    /// 将键盘敲击转换为脚程，将鼠标移动转换为校准判定，计算连击加成
    ///
    /// 算法设计：
    /// - 每10次键盘敲击 = 1秒脚程
    /// - 每100px鼠标移动 = 0.1秒脚程
    /// - 连击窗口1秒，每10次连击+0.1，最高1.5
    /// - 精准校准：静止>2秒后移动 = 2x加成
    /// </summary>
    public class InputConverter
    {
        #region Singleton
        private static InputConverter _instance;
        public static InputConverter instance => _instance ??= new InputConverter();
        #endregion

        #region Configuration
        // 配置常量
        private const int KEYSTROKES_PER_STEP = 10;       // 每10次敲击 = 1秒脚程
        private const float MOUSE_PIXELS_PER_STEP = 100f; // 每100px = 0.1秒脚程
        private const float COMBO_WINDOW = 1.0f;          // 连击窗口1秒
        private const float MAX_COMBO_MULTIPLIER = 1.5f;  // 最大连击加成
        private const float CALIBRATION_THRESHOLD = 0.8f; // 校准阈值
        private const float PRECISION_STILL_TIME = 2.0f;  // 精准校准静止时间
        #endregion

        #region State
        private int _keystrokeCount;
        private float _lastKeystrokeTime;
        private float _comboMultiplier = 1.0f;
        private Vector2 _lastMousePosition;
        private float _lastMouseMoveTime;
        private float _lastInputTime;

        // 统计
        private int _totalKeystrokes;
        private int _totalComboCount;
        private float _maxComboMultiplierAchieved = 1.0f;
        #endregion

        #region Events
        public event System.Action<float> onStepsConverted;     // 脚程转换时触发（参数：脚程值）
        public event System.Action<float> onComboUpdated;        // 连击更新时触发（参数：当前加成）
        public event System.Action onPrecisionCalibration;      // 精准校准触发
        public event System.Action<int> onKeystrokeCountChanged; // 敲击计数变化
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前连击加成
        /// </summary>
        public float comboMultiplier => _comboMultiplier;

        /// <summary>
        /// 当前连击数
        /// </summary>
        public int comboCount => _keystrokeCount;

        /// <summary>
        /// 最大连击加成（历史记录）
        /// </summary>
        public float maxComboMultiplierAchieved => _maxComboMultiplierAchieved;

        /// <summary>
        /// 总敲击次数
        /// </summary>
        public int totalKeystrokes => _totalKeystrokes;

        /// <summary>
        /// 总连击次数
        /// </summary>
        public int totalComboCount => _totalComboCount;

        /// <summary>
        /// 是否处于精准校准状态（静止>2秒后首次移动）
        /// </summary>
        public bool isPrecisionCalibration { get; private set; }

        /// <summary>
        /// 距离上次输入的时间
        /// </summary>
        public float timeSinceLastInput => Time.time - _lastInputTime;
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            _keystrokeCount = 0;
            _lastKeystrokeTime = 0;
            _comboMultiplier = 1.0f;
            _lastMousePosition = Input.mousePosition;
            _lastMouseMoveTime = Time.time;
            _lastInputTime = Time.time;
            isPrecisionCalibration = false;

            Debug.Log("[InputConverter] Initialized");
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update()
        {
            _lastMousePosition = Input.mousePosition;
        }

        /// <summary>
        /// 处理键盘敲击
        /// </summary>
        /// <param name="vkCode">虚拟键码</param>
        /// <returns>获得的脚程（秒）</returns>
        public float OnKeystroke(int vkCode)
        {
            // 过滤修饰键和特殊键
            if (!IsCountableKey(vkCode))
                return 0;

            float now = Time.time;

            // 更新连击
            UpdateCombo(now);

            // 计算基础脚程
            float baseSteps = 1f / KEYSTROKES_PER_STEP;

            // 应用连击加成
            float finalSteps = baseSteps * _comboMultiplier;

            _totalKeystrokes++;
            _lastInputTime = now;

            // 触发事件
            onKeystrokeCountChanged?.Invoke(_keystrokeCount);
            onStepsConverted?.Invoke(finalSteps);

            return finalSteps;
        }

        /// <summary>
        /// 处理鼠标移动
        /// </summary>
        /// <param name="delta">鼠标移动向量（屏幕坐标）</param>
        /// <returns>获得的脚程（秒）</returns>
        public float OnMouseMove(Vector2 delta)
        {
            if (delta.magnitude < 0.1f)
                return 0;

            float now = Time.time;

            // 计算移动距离
            float distance = delta.magnitude;

            // 每100px = 0.1秒脚程
            float baseSteps = distance / MOUSE_PIXELS_PER_STEP * 0.1f;

            // 精准校准判定：静止>2秒后移动 = 2x加成
            float multiplier = 1.0f;
            if (now - _lastMouseMoveTime > PRECISION_STILL_TIME)
            {
                multiplier = 2.0f;
                isPrecisionCalibration = true;
                onPrecisionCalibration?.Invoke();
            }
            else
            {
                isPrecisionCalibration = false;
            }

            float finalSteps = baseSteps * multiplier;

            _lastMousePosition += delta;
            _lastMouseMoveTime = now;
            _lastInputTime = now;

            // 触发事件
            onStepsConverted?.Invoke(finalSteps);

            return finalSteps;
        }

        /// <summary>
        /// 处理鼠标滚轮
        /// </summary>
        /// <param name="scrollDelta">滚轮滚动值</param>
        /// <returns>获得的脚程（秒）</returns>
        public float OnMouseScroll(float scrollDelta)
        {
            if (Mathf.Abs(scrollDelta) < 0.1f)
                return 0;

            // 每次滚轮 = 0.5秒脚程
            float steps = Mathf.Abs(scrollDelta) * 0.5f;
            _lastInputTime = Time.time;

            onStepsConverted?.Invoke(steps);
            return steps;
        }

        /// <summary>
        /// 将键盘敲击转换为脚程
        /// </summary>
        /// <param name="keystrokeCount">敲击次数</param>
        /// <returns>获得的脚程（秒）</returns>
        public float ConvertKeystrokesToSteps(int keystrokeCount)
        {
            if (keystrokeCount <= 0)
                return 0;

            float baseSteps = keystrokeCount / (float)KEYSTROKES_PER_STEP;

            // 更新连击
            float now = Time.time;
            UpdateCombo(now);

            float finalSteps = baseSteps * _comboMultiplier;

            _totalKeystrokes += keystrokeCount;
            _lastInputTime = now;

            onStepsConverted?.Invoke(finalSteps);
            return finalSteps;
        }

        /// <summary>
        /// 将鼠标移动转换为脚程
        /// </summary>
        /// <param name="delta">鼠标移动向量</param>
        /// <returns>获得的脚程（秒）</returns>
        public float ConvertMouseMovementToSteps(Vector2 delta)
        {
            if (delta.magnitude < 0.1f)
                return 0;

            float distance = delta.magnitude;
            float steps = distance / MOUSE_PIXELS_PER_STEP * 0.1f;

            // 精准校准判定
            if (IsPrecisionCalibrationTime())
            {
                steps *= 2.0f;
                isPrecisionCalibration = true;
                onPrecisionCalibration?.Invoke();
            }
            else
            {
                isPrecisionCalibration = false;
            }

            _lastInputTime = Time.time;
            onStepsConverted?.Invoke(steps);
            return steps;
        }

        /// <summary>
        /// 计算校准加成
        /// </summary>
        /// <param name="currentDirection">当前移动方向</param>
        /// <param name="targetDirection">目标方向</param>
        /// <returns>校准加成（成功返回>1.0，失败返回<1.0）</returns>
        public float CalculateCalibrationBonus(Vector2 currentDirection, Vector2 targetDirection)
        {
            if (currentDirection.magnitude < 0.01f || targetDirection.magnitude < 0.01f)
                return 1.0f;

            // 计算方向点积（-1到1）
            float dot = Vector2.Dot(currentDirection.normalized, targetDirection.normalized);

            // 点积>阈值时成功
            if (dot > CALIBRATION_THRESHOLD)
            {
                // 成功：1.0 ~ 1.5 加成
                return 1.0f + (dot - CALIBRATION_THRESHOLD) / (1 - CALIBRATION_THRESHOLD) * 0.5f;
            }

            // 失败：方向偏移惩罚（最多-30%）
            return 1.0f + dot * 0.3f;
        }

        /// <summary>
        /// 获取当前连击信息
        /// </summary>
        public (int count, float multiplier) GetComboInfo()
        {
            return (_keystrokeCount, _comboMultiplier);
        }

        /// <summary>
        /// 重置连击状态
        /// </summary>
        public void ResetCombo()
        {
            _keystrokeCount = 0;
            _comboMultiplier = 1.0f;
            onComboUpdated?.Invoke(_comboMultiplier);
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public (int totalKeystrokes, int totalCombo, float maxMultiplier) GetStatistics()
        {
            return (_totalKeystrokes, _totalComboCount, _maxComboMultiplierAchieved);
        }

        #endregion

        #region Private Methods

        private void UpdateCombo(float now)
        {
            if (now - _lastKeystrokeTime < COMBO_WINDOW)
            {
                _keystrokeCount++;

                // 连击加成：每10次连击+0.1，最高1.5
                float newMultiplier = Mathf.Min(MAX_COMBO_MULTIPLIER, 1.0f + _keystrokeCount / 10f);

                if (newMultiplier > _comboMultiplier)
                {
                    _comboMultiplier = newMultiplier;
                    onComboUpdated?.Invoke(_comboMultiplier);

                    if (_comboMultiplier > _maxComboMultiplierAchieved)
                    {
                        _maxComboMultiplierAchieved = _comboMultiplier;
                    }
                }

                if (_keystrokeCount > 0 && _keystrokeCount % 10 == 0)
                {
                    _totalComboCount++;
                }
            }
            else
            {
                _keystrokeCount = 1;
                _comboMultiplier = 1.0f;
            }

            _lastKeystrokeTime = now;
        }

        private bool IsPrecisionCalibrationTime()
        {
            return (Time.time - _lastMouseMoveTime) > PRECISION_STILL_TIME;
        }

        /// <summary>
        /// 判断是否为可计数的按键（排除修饰键和特殊键）
        /// </summary>
        private bool IsCountableKey(int vkCode)
        {
            // 排除修饰键
            if (vkCode == 0x10 || vkCode == 0x11 || vkCode == 0x12) // Shift, Ctrl, Alt
                return false;

            // 排除功能键 F1-F12（这些通常有其他用途）
            if (vkCode >= 0x70 && vkCode <= 0x7B)
                return false;

            // 排除方向键（可能用于其他导航功能）
            if (vkCode >= 0x25 && vkCode <= 0x28)
                return false;

            // 排除特殊键
            if (vkCode == 0x1B) // Escape
                return false;

            return true;
        }

        #endregion
    }
}