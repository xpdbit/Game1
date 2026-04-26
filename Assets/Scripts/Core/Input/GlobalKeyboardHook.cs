#if UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 全局键盘钩子管理器
    /// 使用 Windows API SetWindowsHookEx 实现全局键盘监听
    /// 可以在透明悬浮窗非焦点状态下捕获键盘输入
    ///
    /// 注意：需要Windows平台，需要在初始化时设置DLL导入路径
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        #region Windows API
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion

        #region Singleton
        private static GlobalKeyboardHook _instance;
        public static GlobalKeyboardHook instance => GetInstanceSafe();
        #endregion

        #region Events
        public event Action<int> onKeyDown;           // 按键按下（virtual key code）
        public event Action<int> onKeyUp;             // 按键释放（virtual key code）
        public event Action onAnyKeyPressed;           // 任意键按下
        #endregion

        #region Private Fields
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _hookProc;
        private bool _isInitialized;
        private bool _isDisposed;

        // 记录上一帧按下的键，用于检测按键释放
        private int _lastPressedKey = -1;

        // 键事件队列 - 避免在HookCallback中阻塞消息泵
        private readonly ConcurrentQueue<int> _keyDownQueue = new ConcurrentQueue<int>();
        private readonly ConcurrentQueue<int> _keyUpQueue = new ConcurrentQueue<int>();
        #endregion

        #region Public Properties
        /// <summary>
        /// 钩子是否已安装
        /// </summary>
        public bool isHookInstalled => _hookId != IntPtr.Zero;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool isInitialized => _isInitialized;
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化全局键盘钩子
        /// </summary>
        /// <returns>是否成功初始化</returns>
        public bool Initialize()
        {
            if (_isInitialized) return true;
            if (_isDisposed)
            {
                Debug.LogError("[GlobalKeyboardHook] Cannot initialize - already disposed");
                return false;
            }

            try
            {
                _hookProc = HookCallback;
                _hookId = SetHook(_hookProc);

                if (_hookId != IntPtr.Zero)
                {
                    _isInitialized = true;
                    Debug.Log("[GlobalKeyboardHook] Initialized successfully - global keyboard hook installed");
                    return true;
                }
                else
                {
                    // 安装失败时清理已分配的委托，防止内存泄漏
                    _hookProc = null;
                    Debug.LogError("[GlobalKeyboardHook] Failed to install keyboard hook");
                    return false;
                }
            }
            catch (Exception e)
            {
                // 异常时清理已分配的委托，防止内存泄漏
                _hookProc = null;
                Debug.LogError($"[GlobalKeyboardHook] Initialize failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 每帧更新（需要在游戏循环中调用）
        /// 处理队列中的键事件，避免在HookCallback中阻塞消息泵
        /// </summary>
        public void Update()
        {
            if (!_isInitialized || _isDisposed) return;

            // 处理按键按下队列
            while (_keyDownQueue.TryDequeue(out int vkCode))
            {
                // 避免重复触发
                if (_lastPressedKey != vkCode)
                {
                    _lastPressedKey = vkCode;
                    try
                    {
                        onKeyDown?.Invoke(vkCode);
                        onAnyKeyPressed?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[GlobalKeyboardHook] onKeyDown handler threw: {e.Message}");
                    }
                }
            }

            // 检测上一帧按下的键是否已释放
            if (_lastPressedKey != -1)
            {
                short state = GetAsyncKeyState(_lastPressedKey);
                if ((state & 0x8000) == 0) // 最高位为0表示键已释放
                {
                    try
                    {
                        onKeyUp?.Invoke(_lastPressedKey);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[GlobalKeyboardHook] onKeyUp handler threw: {e.Message}");
                    }
                    _lastPressedKey = -1;
                }
            }
        }

        /// <summary>
        /// 卸载钩子并释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                Debug.Log("[GlobalKeyboardHook] Keyboard hook uninstalled");
            }

            _hookProc = null;
            _isInitialized = false;
            _isDisposed = true;
            // 注意：不设置 _instance = null，因为静态实例在域重新加载时会自动重置
            // 如果需要强制重置实例，请使用 ForceReset() 方法
        }

        /// <summary>
        /// 重置实例状态（用于在Dispose后重新初始化）
        /// 解决了_isDisposed=true导致无法重新Initialize的问题
        /// </summary>
        public void Reset()
        {
            _isDisposed = false;
            _isInitialized = false;
            _hookId = IntPtr.Zero;
            _hookProc = null;
            _lastPressedKey = -1;
            // 清空队列
            while (_keyDownQueue.TryDequeue(out _)) { }
            while (_keyUpQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 强制重置单例（用于域重新加载后）
        /// </summary>
        public static void ForceReset()
        {
            if (_instance != null)
            {
                _instance.Dispose();
            }
            _instance = null;
        }

        /// <summary>
        /// 安全获取实例（自动重置已销毁的实例）
        /// </summary>
        private static GlobalKeyboardHook GetInstanceSafe()
        {
            if (_instance != null && _instance._isDisposed)
            {
                // 实例已被销毁，重置它以便重新初始化
                _instance.Reset();
            }
            return _instance ??= new GlobalKeyboardHook();
        }

        /// <summary>
        /// 检测指定虚拟键是否正在按下
        /// </summary>
        public bool IsKeyPressed(int vkCode)
        {
            short state = GetAsyncKeyState(vkCode);
            return (state & 0x8000) != 0;
        }

        /// <summary>
        /// 获取键盘上所有正在按下的键
        /// </summary>
        public int[] GetPressedKeys()
        {
            var pressedKeys = new System.Collections.Generic.List<int>();

            // 检查常用键范围
            for (int i = 0x08; i <= 0xFE; i++)
            {
                if (IsKeyPressed(i))
                {
                    pressedKeys.Add(i);
                }
            }

            return pressedKeys.ToArray();
        }

        #endregion

        #region Private Methods

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                try
                {
                    // 直接读取 vkCode（偏移量0），避免 Marshal.PtrToStructure 分配内存
                    // KBDLLHOOKSTRUCT: vkCode 是第一个 uint32 字段
                    int vkCode = Marshal.ReadInt32(lParam, 0);

                    // 仅将按键码入队列，不做任何耗时操作
                    // 这样可以避免阻塞消息泵导致输入延迟
                    _keyDownQueue.Enqueue(vkCode);
                }
                catch
                {
                    // 静默处理异常 - 在钩子回调中不允许抛出异常，否则会导致输入延迟
                    // 不要使用Debug.LogError，因为这会在主线程上造成阻塞
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #endregion

        #region Virtual Key Codes (常用按键码)
        /// <summary>
        /// 常用虚拟键码
        /// </summary>
        public static class KeyCode
        {
            public const int VK_BACK = 0x08;
            public const int VK_TAB = 0x09;
            public const int VK_RETURN = 0x0D;
            public const int VK_SHIFT = 0x10;
            public const int VK_CONTROL = 0x11;
            public const int VK_MENU = 0x12; // Alt
            public const int VK_ESCAPE = 0x1B;
            public const int VK_SPACE = 0x20;
            public const int VK_PAGE_UP = 0x21;
            public const int VK_PAGE_DOWN = 0x22;
            public const int VK_END = 0x23;
            public const int VK_HOME = 0x24;
            public const int VK_LEFT = 0x25;
            public const int VK_UP = 0x26;
            public const int VK_RIGHT = 0x27;
            public const int VK_DOWN = 0x28;
            public const int VK_DELETE = 0x2E;

            // 字母键 A-Z (0x41-0x5A)
            public const int VK_A = 0x41;
            public const int VK_B = 0x42;
            public const int VK_C = 0x43;
            public const int VK_D = 0x44;
            public const int VK_E = 0x45;
            public const int VK_F = 0x46;
            public const int VK_G = 0x47;
            public const int VK_H = 0x48;
            public const int VK_I = 0x49;
            public const int VK_J = 0x4A;
            public const int VK_K = 0x4B;
            public const int VK_L = 0x4C;
            public const int VK_M = 0x4D;
            public const int VK_N = 0x4E;
            public const int VK_O = 0x4F;
            public const int VK_P = 0x50;
            public const int VK_Q = 0x51;
            public const int VK_R = 0x52;
            public const int VK_S = 0x53;
            public const int VK_T = 0x54;
            public const int VK_U = 0x55;
            public const int VK_V = 0x56;
            public const int VK_W = 0x57;
            public const int VK_X = 0x58;
            public const int VK_Y = 0x59;
            public const int VK_Z = 0x5A;

            // 数字键 0-9 (0x30-0x39)
            public const int VK_0 = 0x30;
            public const int VK_1 = 0x31;
            public const int VK_2 = 0x32;
            public const int VK_3 = 0x33;
            public const int VK_4 = 0x34;
            public const int VK_5 = 0x35;
            public const int VK_6 = 0x36;
            public const int VK_7 = 0x37;
            public const int VK_8 = 0x38;
            public const int VK_9 = 0x39;

            // 数字小键盘 (0x60-0x69)
            public const int VK_NUMPAD0 = 0x60;
            public const int VK_NUMPAD1 = 0x61;
            public const int VK_NUMPAD2 = 0x62;
            public const int VK_NUMPAD3 = 0x63;
            public const int VK_NUMPAD4 = 0x64;
            public const int VK_NUMPAD5 = 0x65;
            public const int VK_NUMPAD6 = 0x66;
            public const int VK_NUMPAD7 = 0x67;
            public const int VK_NUMPAD8 = 0x68;
            public const int VK_NUMPAD9 = 0x69;
            public const int VK_MULTIPLY = 0x6A;
            public const int VK_ADD = 0x6B;
            public const int VK_SUBTRACT = 0x6D;
            public const int VK_DECIMAL = 0x6E;
            public const int VK_DIVIDE = 0x6F;

            // 功能键 F1-F12
            public const int VK_F1 = 0x70;
            public const int VK_F2 = 0x71;
            public const int VK_F3 = 0x72;
            public const int VK_F4 = 0x73;
            public const int VK_F5 = 0x74;
            public const int VK_F6 = 0x75;
            public const int VK_F7 = 0x76;
            public const int VK_F8 = 0x77;
            public const int VK_F9 = 0x78;
            public const int VK_F10 = 0x79;
            public const int VK_F11 = 0x7A;
            public const int VK_F12 = 0x7B;

            /// <summary>
            /// 检查是否为有效按键码
            /// </summary>
            public static bool IsValid(int vkCode) => vkCode >= 0x08 && vkCode <= 0xFE;

            /// <summary>
            /// 检查是否为字母键
            /// </summary>
            public static bool IsLetter(int vkCode) => vkCode >= VK_A && vkCode <= VK_Z;

            /// <summary>
            /// 检查是否为数字键
            /// </summary>
            public static bool IsDigit(int vkCode) => vkCode >= VK_0 && vkCode <= VK_9;

            /// <summary>
            /// 检查是否为方向键
            /// </summary>
            public static bool IsArrow(int vkCode) => vkCode >= VK_LEFT && vkCode <= VK_DOWN;

            /// <summary>
            /// 将虚拟键码转换为可读字符串
            /// </summary>
            public static string ToString(int vkCode)
            {
                if (IsLetter(vkCode))
                    return ((char)vkCode).ToString();
                if (IsDigit(vkCode))
                    return ((char)vkCode).ToString();

                return vkCode switch
                {
                    VK_BACK => "Back",
                    VK_TAB => "Tab",
                    VK_RETURN => "Enter",
                    VK_SHIFT => "Shift",
                    VK_CONTROL => "Ctrl",
                    VK_MENU => "Alt",
                    VK_ESCAPE => "Esc",
                    VK_SPACE => "Space",
                    VK_PAGE_UP => "PageUp",
                    VK_PAGE_DOWN => "PageDown",
                    VK_END => "End",
                    VK_HOME => "Home",
                    VK_LEFT => "Left",
                    VK_UP => "Up",
                    VK_RIGHT => "Right",
                    VK_DOWN => "Down",
                    VK_DELETE => "Delete",
                    VK_F1 => "F1",
                    VK_F2 => "F2",
                    VK_F3 => "F3",
                    VK_F4 => "F4",
                    VK_F5 => "F5",
                    VK_F6 => "F6",
                    VK_F7 => "F7",
                    VK_F8 => "F8",
                    VK_F9 => "F9",
                    VK_F10 => "F10",
                    VK_F11 => "F11",
                    VK_F12 => "F12",
                    _ => $"0x{vkCode:X2}"
                };
            }
        }
        #endregion
    }
}
#endif // UNITY_STANDALONE_WIN