using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 后台输入管理器
    /// 使用 UniWindowController 的静态 API 实现后台输入监听
    /// 支持在透明悬浮窗模式下，即使窗口不是前台焦点也能监听鼠标
    ///
    /// 注意：由于平台限制，只能检测鼠标按钮和修饰键（Shift/Ctrl/Alt），
    /// 不能检测普通键盘按键。当窗口无焦点时，普通键盘输入会发送到其他窗口。
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

        // 事件
        public event Action onAnyKeyPressed;
        public event Action onLeftMouseClicked;
        public event Action onRightMouseClicked;
        public event Action onMiddleMouseClicked;
        public event Action<Vector2> onMouseMoved;
        public event Action onShiftPressed;
        public event Action onCtrlPressed;
        public event Action onAltPressed;
        public event Action onMouseButtonPressed; // 鼠标按钮按下事件（用于后台检测）

        // 属性 - 使用 UniWindowController 的静态方法
        public Vector2 cursorPosition => Kirurobo.UniWindowController.GetCursorPosition();
        public bool isLeftButtonPressed => ((Kirurobo.UniWindowController.GetMouseButtons()) & Kirurobo.UniWindowController.MouseButton.Left) != 0;
        public bool isRightButtonPressed => ((Kirurobo.UniWindowController.GetMouseButtons()) & Kirurobo.UniWindowController.MouseButton.Right) != 0;
        public bool isMiddleButtonPressed => ((Kirurobo.UniWindowController.GetMouseButtons()) & Kirurobo.UniWindowController.MouseButton.Middle) != 0;
        public bool isShiftPressed => ((Kirurobo.UniWindowController.GetModifierKeys()) & Kirurobo.UniWindowController.ModifierKey.Shift) != 0;
        public bool isCtrlPressed => ((Kirurobo.UniWindowController.GetModifierKeys()) & Kirurobo.UniWindowController.ModifierKey.Control) != 0;
        public bool isAltPressed => ((Kirurobo.UniWindowController.GetModifierKeys()) & Kirurobo.UniWindowController.ModifierKey.Alt) != 0;

        #region Public API

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _lastMouseButtons = (int)Kirurobo.UniWindowController.GetMouseButtons();
            _lastModifierKeys = (int)Kirurobo.UniWindowController.GetModifierKeys();
            _lastCursorPosition = Kirurobo.UniWindowController.GetCursorPosition();

            _isInitialized = true;
            Debug.Log("[BackgroundInputManager] Initialized with UniWindowController backend");
        }

        /// <summary>
        /// 每帧更新（需要在游戏循环中调用）
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) Initialize();

            int currentMouseButtons = (int)Kirurobo.UniWindowController.GetMouseButtons();
            int currentModifierKeys = (int)Kirurobo.UniWindowController.GetModifierKeys();
            Vector2 currentCursorPosition = Kirurobo.UniWindowController.GetCursorPosition();

            // 检测鼠标按钮变化
            int buttonChanged = _lastMouseButtons ^ currentMouseButtons;
            int buttonPressed = buttonChanged & currentMouseButtons;

            // 左键点击
            if ((buttonPressed & LEFT_BUTTON) != 0)
            {
                onLeftMouseClicked?.Invoke();
                onMouseButtonPressed?.Invoke();
            }

            // 右键点击
            if ((buttonPressed & RIGHT_BUTTON) != 0)
            {
                onRightMouseClicked?.Invoke();
                onMouseButtonPressed?.Invoke();
            }

            // 中键点击
            if ((buttonPressed & MIDDLE_BUTTON) != 0)
            {
                onMiddleMouseClicked?.Invoke();
                onMouseButtonPressed?.Invoke();
            }

            // 检测鼠标移动
            if (currentCursorPosition != _lastCursorPosition)
            {
                onMouseMoved?.Invoke(currentCursorPosition);
            }

            // 检测修饰键变化
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

            _lastMouseButtons = currentMouseButtons;
            _lastModifierKeys = currentModifierKeys;
            _lastCursorPosition = currentCursorPosition;
        }

        /// <summary>
        /// 检测是否有任何输入（键盘或鼠标）
        /// 这是一个轮询方法，可用于检测玩家是否活跃
        /// </summary>
        public bool HasAnyInput()
        {
            if (!_isInitialized) Initialize();

            return Kirurobo.UniWindowController.GetMouseButtons() != Kirurobo.UniWindowController.MouseButton.None ||
                   Kirurobo.UniWindowController.GetModifierKeys() != Kirurobo.UniWindowController.ModifierKey.None;
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

        #endregion
    }
}