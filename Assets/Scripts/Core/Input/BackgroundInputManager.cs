using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 后台输入管理器
    /// 使用 UniWindowController 的静态 API 实现后台输入监听
    /// 支持在透明悬浮窗模式下，即使窗口不是前台焦点也能监听鼠标和键盘
    ///
    /// 功能：
    /// - 鼠标按钮/移动检测（通过UniWindowController）
    /// - 键盘全局检测（通过GlobalKeyboardHook - Windows only）
    /// - 虚实交互输入转换（通过InputConverter）
    /// </summary>
    public class BackgroundInputManager
    {
        #region Singleton
        private static BackgroundInputManager _instance;
        public static BackgroundInputManager instance => _instance ??= new BackgroundInputManager();
        #endregion

        // 鼠标按钮位标志（与 UniWindowController.MouseButton 对应）
        private const int LEFT_BUTTON = 1;
        private const int RIGHT_BUTTON = 2;
        private const int MIDDLE_BUTTON = 4;

        // 修饰键位标志（与 UniWindowController.ModifierKey 对应）
        private const int KEY_SHIFT = 4;
        private const int KEY_CONTROL = 2;
        private const int KEY_ALT = 1;

        // 状态
        private int _lastMouseButtons;
        private int _lastModifierKeys;
        private Vector2 _lastCursorPosition;
        private bool _isInitialized;

        // 组件引用
        private GlobalKeyboardHook _keyboardHook;     // 后备键盘钩子
        private RawInputManager _rawInputManager;    // Raw Input 管理器（优先使用）
        private InputConverter _inputConverter;
        private bool _useRawInput = true;            // 是否使用 Raw Input

        // 事件
        public event Action onAnyKeyPressed;
        public event Action<int> onKeyDown;             // 按键按下（参数：vkCode）
        public event Action<int> onKeyUp;             // 按键释放（参数：vkCode）
        public event Action onLeftMouseClicked;
        public event Action onRightMouseClicked;
        public event Action onMiddleMouseClicked;
        public event Action<Vector2> onMouseMoved;
        public event Action onShiftPressed;
        public event Action onCtrlPressed;
        public event Action onAltPressed;
        public event Action onMouseButtonPressed;      // 鼠标按钮按下事件
        public event Action<float> onStepsConverted;  // 脚程转换事件（参数：脚程值）
        public event Action<float> onComboUpdated;     // 连击更新事件（参数：当前加成）
        public event Action onPrecisionCalibration;     // 精准校准触发

        // 属性 - 使用 UniWindowController 的静态方法
        public Vector2 cursorPosition => Kirurobo.UniWindowController.GetCursorPosition();
        public bool isLeftButtonPressed => ((Kirurobo.UniWindowController.GetMouseButtons()) & Kirurobo.UniWindowController.MouseButton.Left) != 0;
        public bool isRightButtonPressed => ((Kirurobo.UniWindowController.GetMouseButtons()) & Kirurobo.UniWindowController.MouseButton.Right) != 0;
        public bool isMiddleButtonPressed => ((Kirurobo.UniWindowController.GetMouseButtons()) & Kirurobo.UniWindowController.MouseButton.Middle) != 0;
        public bool isShiftPressed => ((Kirurobo.UniWindowController.GetModifierKeys()) & Kirurobo.UniWindowController.ModifierKey.Shift) != 0;
        public bool isCtrlPressed => ((Kirurobo.UniWindowController.GetModifierKeys()) & Kirurobo.UniWindowController.ModifierKey.Control) != 0;
        public bool isAltPressed => ((Kirurobo.UniWindowController.GetModifierKeys()) & Kirurobo.UniWindowController.ModifierKey.Alt) != 0;

        // InputConverter 相关属性
        public float comboMultiplier => _inputConverter?.comboMultiplier ?? 1f;
        public int comboCount => _inputConverter?.comboCount ?? 0;
        public bool isPrecisionCalibration => _inputConverter?.isPrecisionCalibration ?? false;
        public InputConverter inputConverter => _inputConverter;

        #region Public API

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // 关键修复：确保所有钩子处于干净状态
            // 防止上次Dispose后无法重新初始化的问题
            GlobalKeyboardHook.ForceReset();
            RawInputManager.ForceReset();

            _lastMouseButtons = (int)Kirurobo.UniWindowController.GetMouseButtons();
            _lastModifierKeys = (int)Kirurobo.UniWindowController.GetModifierKeys();
            _lastCursorPosition = Kirurobo.UniWindowController.GetCursorPosition();

            // 初始化InputConverter
            _inputConverter = InputConverter.instance;
            _inputConverter.Initialize();

            // 初始化 Raw Input 管理器（Windows only，优先使用）
#if UNITY_STANDALONE_WIN
            if (_useRawInput)
            {
                _rawInputManager = RawInputManager.instance;
                if (_rawInputManager.Initialize())
                {
                    _rawInputManager.onKeyDown += OnRawInputKeyDown;
                    _rawInputManager.onKeyUp += OnRawInputKeyUp;
                    _rawInputManager.onAnyKeyPressed += OnRawInputAnyKeyPressed;

                    // 转发InputConverter事件
                    _inputConverter.onStepsConverted += (steps) => onStepsConverted?.Invoke(steps);
                    _inputConverter.onComboUpdated += (multiplier) => onComboUpdated?.Invoke(multiplier);
                    _inputConverter.onPrecisionCalibration += () => onPrecisionCalibration?.Invoke();

                    Debug.Log("[BackgroundInputManager] Raw Input manager installed - no hook delay");
                }
                else
                {
                    Debug.LogWarning("[BackgroundInputManager] Failed to initialize Raw Input - falling back to keyboard hook");
                    _useRawInput = false;
                }
            }

            // 如果 Raw Input 失败，使用 GlobalKeyboardHook 作为后备
            if (!_useRawInput || !_rawInputManager.isInitialized)
            {
                _keyboardHook = GlobalKeyboardHook.instance;
                if (_keyboardHook.Initialize())
                {
                    _keyboardHook.onKeyDown += OnGlobalKeyDown;
                    _keyboardHook.onKeyUp += OnGlobalKeyUp;
                    _keyboardHook.onAnyKeyPressed += OnGlobalAnyKeyPressed;

                    // 转发InputConverter事件
                    _inputConverter.onStepsConverted += (steps) => onStepsConverted?.Invoke(steps);
                    _inputConverter.onComboUpdated += (multiplier) => onComboUpdated?.Invoke(multiplier);
                    _inputConverter.onPrecisionCalibration += () => onPrecisionCalibration?.Invoke();

                    Debug.Log("[BackgroundInputManager] Global keyboard hook installed (fallback mode)");
                }
                else
                {
                    Debug.LogWarning("[BackgroundInputManager] Failed to install global keyboard hook - keyboard input will be limited when window is not focused");
                }
            }
#else
            Debug.Log("[BackgroundInputManager] Non-Windows platform - using fallback input method");
#endif

            _isInitialized = true;
            Debug.Log("[BackgroundInputManager] Initialized with UniWindowController backend");
        }

        /// <summary>
        /// 每帧更新（需要在游戏循环中调用）
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) Initialize();

            // 更新鼠标状态
            UpdateMouseState();

            // 更新修饰键状态
            UpdateModifierKeys();

            // 更新InputConverter
            _inputConverter?.Update();

            // 更新 Raw Input 管理器（优先）
#if UNITY_STANDALONE_WIN
            _rawInputManager?.Update();

            // 如果 Raw Input 未初始化，使用 GlobalKeyboardHook 作为后备
            if (_rawInputManager == null || !_rawInputManager.isInitialized)
            {
                _keyboardHook?.Update();
            }
#endif
        }

        /// <summary>
        /// 处理全局按键按下（来自 Raw Input）
        /// </summary>
        private void OnRawInputKeyDown(int vkCode)
        {
            onKeyDown?.Invoke(vkCode);

            // 转换为脚程
            _inputConverter?.OnKeystroke(vkCode);
        }

        /// <summary>
        /// 处理全局按键释放（来自 Raw Input）
        /// </summary>
        private void OnRawInputKeyUp(int vkCode)
        {
            onKeyUp?.Invoke(vkCode);
        }

        /// <summary>
        /// 处理全局任意键按下（来自 Raw Input）
        /// </summary>
        private void OnRawInputAnyKeyPressed()
        {
            onAnyKeyPressed?.Invoke();
        }

        /// <summary>
        /// 处理全局按键按下（来自 GlobalKeyboardHook）
        /// </summary>
        private void OnGlobalKeyDown(int vkCode)
        {
            onKeyDown?.Invoke(vkCode);

            // 转换为脚程
            _inputConverter?.OnKeystroke(vkCode);
        }

        /// <summary>
        /// 处理全局按键释放（来自 GlobalKeyboardHook）
        /// </summary>
        private void OnGlobalKeyUp(int vkCode)
        {
            onKeyUp?.Invoke(vkCode);
        }

        /// <summary>
        /// 处理全局任意键按下（来自 GlobalKeyboardHook）
        /// </summary>
        private void OnGlobalAnyKeyPressed()
        {
            onAnyKeyPressed?.Invoke();
        }

        /// <summary>
        /// 更新鼠标状态
        /// </summary>
        private void UpdateMouseState()
        {
            int currentMouseButtons = (int)Kirurobo.UniWindowController.GetMouseButtons();
            Vector2 currentCursorPosition = Kirurobo.UniWindowController.GetCursorPosition();

            // 检测鼠标按钮变化
            int buttonChanged = _lastMouseButtons ^ currentMouseButtons;
            int buttonPressed = buttonChanged & currentMouseButtons;

            // 左键点击
            if ((buttonPressed & LEFT_BUTTON) != 0)
            {
                onLeftMouseClicked?.Invoke();
                onMouseButtonPressed?.Invoke();
                _inputConverter?.OnMouseClick();
                _inputConverter?.OnMouseMove(Vector2.zero);
            }

            // 右键点击
            if ((buttonPressed & RIGHT_BUTTON) != 0)
            {
                onRightMouseClicked?.Invoke();
                onMouseButtonPressed?.Invoke();
                _inputConverter?.OnMouseClick();
            }

            // 中键点击
            if ((buttonPressed & MIDDLE_BUTTON) != 0)
            {
                onMiddleMouseClicked?.Invoke();
                onMouseButtonPressed?.Invoke();
                _inputConverter?.OnMouseClick();
            }

            // 检测鼠标移动
            Vector2 delta = currentCursorPosition - _lastCursorPosition;
            if (delta.magnitude > 0.1f)
            {
                onMouseMoved?.Invoke(currentCursorPosition);

                // 鼠标移动转换为脚程
                _inputConverter?.OnMouseMove(delta);
            }

            _lastMouseButtons = currentMouseButtons;
            _lastCursorPosition = currentCursorPosition;
        }

        /// <summary>
        /// 更新修饰键状态
        /// </summary>
        private void UpdateModifierKeys()
        {
            int currentModifierKeys = (int)Kirurobo.UniWindowController.GetModifierKeys();

            int modifierChanged = _lastModifierKeys ^ currentModifierKeys;
            int modifierPressed = modifierChanged & currentModifierKeys;

            if ((modifierPressed & KEY_SHIFT) != 0 && (currentModifierKeys & KEY_SHIFT) != 0)
            {
                onShiftPressed?.Invoke();
            }

            if ((modifierPressed & KEY_CONTROL) != 0 && (currentModifierKeys & KEY_CONTROL) != 0)
            {
                onCtrlPressed?.Invoke();
            }

            if ((modifierPressed & KEY_ALT) != 0 && (currentModifierKeys & KEY_ALT) != 0)
            {
                onAltPressed?.Invoke();
            }

            _lastModifierKeys = currentModifierKeys;
        }

        /// <summary>
        /// 检测是否有任何输入（键盘或鼠标）
        /// </summary>
        public bool HasAnyInput()
        {
            if (!_isInitialized) Initialize();

            if (Kirurobo.UniWindowController.GetMouseButtons() != Kirurobo.UniWindowController.MouseButton.None ||
                Kirurobo.UniWindowController.GetModifierKeys() != Kirurobo.UniWindowController.ModifierKey.None)
            {
                return true;
            }

#if UNITY_STANDALONE_WIN
            if (_keyboardHook != null && _keyboardHook.isHookInstalled)
            {
                var pressedKeys = _keyboardHook.GetPressedKeys();
                return pressedKeys.Length > 0;
            }
#endif

            return false;
        }

        /// <summary>
        /// 获取鼠标按钮状态（位标志）
        /// </summary>
        public int GetMouseButtonState()
        {
            return (int)Kirurobo.UniWindowController.GetMouseButtons();
        }

        /// <summary>
        /// 获取修饰键状态
        /// </summary>
        public int GetModifierKeyState()
        {
            return (int)Kirurobo.UniWindowController.GetModifierKeys();
        }

        /// <summary>
        /// 获取连击信息
        /// </summary>
        public (int count, float multiplier) GetComboInfo()
        {
            return _inputConverter?.GetComboInfo() ?? (0, 1f);
        }

        /// <summary>
        /// 重置连击状态
        /// </summary>
        public void ResetCombo()
        {
            _inputConverter?.ResetCombo();
        }

        /// <summary>
        /// 从存档恢复输入统计（解决读档后_inputKeystrokes重置为0的问题）
        /// </summary>
        /// <param name="count">要恢复的总敲击次数</param>
        public void RestoreInputCount(int count)
        {
            if (_inputConverter == null)
            {
                Debug.LogError("[BackgroundInputManager] Cannot restore input count - InputConverter is null!");
                return;
            }
            _inputConverter.RestoreTotalKeystrokes(count);
            Debug.Log("[BackgroundInputManager] Input count restored successfully");
        }

        /// <summary>
        /// 获取输入转换统计
        /// </summary>
        public (int totalKeystrokes, int totalCombo, float maxMultiplier) GetInputStatistics()
        {
            return _inputConverter?.GetStatistics() ?? (0, 0, 1f);
        }

        /// <summary>
        /// 卸载钩子并释放资源
        /// </summary>
        public void Dispose()
        {
#if UNITY_STANDALONE_WIN
            // 清理 Raw Input 管理器
            if (_rawInputManager != null)
            {
                _rawInputManager.onKeyDown -= OnRawInputKeyDown;
                _rawInputManager.onKeyUp -= OnRawInputKeyUp;
                _rawInputManager.onAnyKeyPressed -= OnRawInputAnyKeyPressed;
                _rawInputManager.Dispose();
                _rawInputManager = null;
            }

            // 清理 GlobalKeyboardHook
            if (_keyboardHook != null)
            {
                // 移除事件订阅（防止内存泄漏和回调到已释放对象）
                _keyboardHook.onKeyDown -= OnGlobalKeyDown;
                _keyboardHook.onKeyUp -= OnGlobalKeyUp;
                _keyboardHook.onAnyKeyPressed -= OnGlobalAnyKeyPressed;
                _keyboardHook.Dispose();
                _keyboardHook = null;
            }
#endif
            _inputConverter = null;
            _isInitialized = false;
        }

        #endregion
    }
}