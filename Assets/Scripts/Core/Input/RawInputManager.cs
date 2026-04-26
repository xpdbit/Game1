#if UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// Raw Input 管理器
    /// 使用 Windows Raw Input API 替代 WH_KEYBOARD_LL 钩子
    ///
    /// 优势：
    /// - 消息驱动的 WM_INPUT，非回调式
    /// - 性能更好，无每次按键的线程切换
    /// - 支持前后台输入
    ///
    /// 实现：需要窗口子类化来处理 WM_INPUT 消息
    /// </summary>
    public class RawInputManager : IDisposable
    {
        #region Windows API
        private const int WM_INPUT = 0x00FF;
        private const int RIM_TYPEKEYBOARD = 1;
        private const int RID_INPUT = 0x10000003;

        // Raw Input Device Flags
        private const uint RIDEV_INPUTSINK = 0x00000100;  // 接收后台输入

        [DllImport("user32.dll")]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, out RAWINPUTDATA pData, ref int pcbSize, int cbSizeHeader);

        [DllImport("user32.dll")]
        private static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref int pcbSize, int cbSizeHeader);

        [DllImport("user32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pDevices, int uiDevices, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr DefRawInputProc(RAWINPUTDATA[] pRawInput, int nInput, int cbSizeHeader);

        // 窗口子类化
        private const int GWLP_WNDPROC = -4;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDATA
        {
            public RAWINPUTHEADER header;
            public RAWINPUTKEYBOARD keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public IntPtr ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion

        #region Singleton
        private static RawInputManager _instance;
        public static RawInputManager instance => _instance ??= new RawInputManager();
        #endregion

        #region Events
        public event Action<int> onKeyDown;           // 按键按下（virtual key code）
        public event Action<int> onKeyUp;             // 按键释放（virtual key code）
        public event Action onAnyKeyPressed;           // 任意键按下
        #endregion

        #region Private Fields
        private IntPtr _windowHandle = IntPtr.Zero;
        private IntPtr _originalWndProc = IntPtr.Zero;
        private IntPtr _ourWndProcPtr = IntPtr.Zero;
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isWindowSubclassed;

        // 用于检测按键释放 - Raw Input 不提供 WM_KEYUP，需要轮询
        private int _lastPressedKey = -1;
        private readonly ConcurrentQueue<int> _keyDownQueue = new ConcurrentQueue<int>();

        // WndProc 委托 - 需要保持引用防止 GC
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate _wndProcDelegate;

        // 统计
        private int _totalKeyDowns;
        #endregion

        #region Public Properties
        public bool isInitialized => _isInitialized;
        public int totalKeyDowns => _totalKeyDowns;
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化 Raw Input 管理器
        /// </summary>
        /// <returns>是否成功初始化</returns>
        public bool Initialize()
        {
            if (_isInitialized) return true;
            if (_isDisposed)
            {
                Debug.LogError("[RawInputManager] Cannot initialize - already disposed");
                return false;
            }

            try
            {
                // 1. 获取 Unity 窗口句柄
                _windowHandle = GetUnityWindowHandle();
                if (_windowHandle == IntPtr.Zero)
                {
                    Debug.LogError("[RawInputManager] Failed to get Unity window handle");
                    return false;
                }
                Debug.Log($"[RawInputManager] Got window handle: {_windowHandle}");

                // 2. 注册原始窗口过程
                if (!SubclassWindow())
                {
                    Debug.LogError("[RawInputManager] Failed to subclass window");
                    return false;
                }

                // 3. 注册原始输入设备
                if (!RegisterDevices())
                {
                    Debug.LogError("[RawInputManager] Failed to register raw input devices");
                    return false;
                }

                _isInitialized = true;
                Debug.Log("[RawInputManager] Initialized successfully - Raw Input API installed");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RawInputManager] Initialize failed: {e.Message}\n{e.StackTrace}");
                Cleanup();
                return false;
            }
        }

        /// <summary>
        /// 每帧更新（需要在游戏循环中调用）
        /// </summary>
        public void Update()
        {
            if (!_isInitialized || _isDisposed) return;

            // 处理按键按下队列
            while (_keyDownQueue.TryDequeue(out int vkCode))
            {
                if (_lastPressedKey != vkCode)
                {
                    _lastPressedKey = vkCode;
                    try
                    {
                        onKeyDown?.Invoke(vkCode);
                        onAnyKeyPressed?.Invoke();
                        _totalKeyDowns++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[RawInputManager] onKeyDown handler threw: {e.Message}");
                    }
                }
            }

            // 检测上一帧按下的键是否已释放（轮询方式）
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
                        Debug.LogWarning($"[RawInputManager] onKeyUp handler threw: {e.Message}");
                    }
                    _lastPressedKey = -1;
                }
            }
        }

        /// <summary>
        /// 卸载并释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            Cleanup();

            _isInitialized = false;
            _isDisposed = true;
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
        /// 重置实例状态
        /// </summary>
        public static void ForceReset()
        {
            if (_instance != null)
            {
                _instance.Dispose();
            }
            _instance = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取 Unity 窗口句柄
        /// </summary>
        private IntPtr GetUnityWindowHandle()
        {
            // 方法1: 通过 UnityPlayer.dll 获取主窗口
            IntPtr unityPlayer = GetModuleHandle("UnityPlayer.dll");
            if (unityPlayer != IntPtr.Zero)
            {
                Debug.Log($"[RawInputManager] Found UnityPlayer.dll at {unityPlayer}");
            }

            // 方法2: 查找 Unity 窗口类名
            // Unity 窗口类名通常是 "UnityWndClass" 或类似
            IntPtr hwnd = FindWindow("UnityWndClass", null);
            if (hwnd != IntPtr.Zero)
            {
                Debug.Log($"[RawInputManager] Found UnityWndClass window: {hwnd}");
                return hwnd;
            }

            // 方法3: 尝试 UnityPlayer.dll 内的窗口
            // 使用 Unity 的方式来获取窗口句柄
            hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                // 检查是否是我们的进程
                uint processId;
                GetWindowThreadProcessId(hwnd, IntPtr.Zero);
                return hwnd;
            }

            // 方法4: 枚举窗口找 Unity 窗口
            hwnd = FindWindow(null, "Game1");
            if (hwnd != IntPtr.Zero)
            {
                Debug.Log($"[RawInputManager] Found game window 'Game1': {hwnd}");
                return hwnd;
            }

            // 回退：使用 Unity 的主窗口
            return GetActiveWindowHandle();
        }

        /// <summary>
        /// 获取活动窗口句柄（Unity 主窗口）
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        private IntPtr GetActiveWindowHandle()
        {
            IntPtr hwnd = GetActiveWindow();
            Debug.Log($"[RawInputManager] GetActiveWindow returned: {hwnd}");
            return hwnd;
        }

        /// <summary>
        /// 子类化窗口以添加 WndProc
        /// </summary>
        private bool SubclassWindow()
        {
            if (_windowHandle == IntPtr.Zero) return false;

            // 保存原始窗口过程
            _originalWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, IntPtr.Zero);
            if (_originalWndProc == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Debug.LogError($"[RawInputManager] SetWindowLongPtr failed with error {error}");
                return false;
            }

            // 创建我们的 WndProc 委托
            _wndProcDelegate = MyWndProc;

            // 使用 GCHandle 防止委托被 GC
            GCHandle.Alloc(_wndProcDelegate, GCHandleType.Pinned);
            _ourWndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

            // 设置新的窗口过程
            IntPtr result = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _ourWndProcPtr);
            if (result == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Debug.LogError($"[RawInputManager] Failed to set WndProc, error {error}");
                return false;
            }

            _isWindowSubclassed = true;
            Debug.Log("[RawInputManager] Window subclassed successfully");
            return true;
        }

        /// <summary>
        /// 我们的 WndProc 处理 WM_INPUT
        /// </summary>
        private IntPtr MyWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                ProcessRawInput(lParam);
            }

            // 调用原始窗口过程
            return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// 处理原始输入数据
        /// </summary>
        private void ProcessRawInput(IntPtr lParam)
        {
            try
            {
                // 获取原始输入数据大小
                int size = 0;
                GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size, Marshal.SizeOf<RAWINPUTHEADER>());
                if (size <= 0) return;

                // 分配内存并获取数据
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    int ret = GetRawInputData(lParam, RID_INPUT, buffer, ref size, Marshal.SizeOf<RAWINPUTHEADER>());
                    if (ret < 0) return;

                    // 解析 RAWKEYBOARD 结构
                    // keyboard 字段在 header 之后，偏移量为 sizeof(RAWINPUTHEADER)
                    int keyboardOffset = Marshal.SizeOf<RAWINPUTHEADER>();
                    ushort makeCode = (ushort)Marshal.ReadInt16(buffer, keyboardOffset);
                    ushort flags = (ushort)Marshal.ReadInt16(buffer, keyboardOffset + 2);
                    ushort reserved = (ushort)Marshal.ReadInt16(buffer, keyboardOffset + 4);
                    ushort vKey = (ushort)Marshal.ReadInt16(buffer, keyboardOffset + 6);
                    uint message = (uint)Marshal.ReadInt32(buffer, keyboardOffset + 8);

                    // 过滤重复的断点检测
                    bool isKeyDown = (flags & 0x0001) == 0; // bit 0: key was pressed (0) or released (1)

                    if (!isKeyDown)
                    {
                        // 按键释放 - 入队让 Update 处理
                        // Raw Input 的 WM_KEYDOWN 对应按下，WM_KEYUP 对应释放
                        // 但我们可以直接处理
                    }

                    // 仅处理按键按下事件
                    if (message == 0x0100 || message == 0x0104) // WM_KEYDOWN or WM_SYSKEYDOWN
                    {
                        int vkCode = vKey;
                        if (vkCode != 0)
                        {
                            _keyDownQueue.Enqueue(vkCode);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception e)
            {
                // 静默处理，不在回调中抛出异常
                Debug.LogWarning($"[RawInputManager] ProcessRawInput error: {e.Message}");
            }
        }

        /// <summary>
        /// 注册原始输入设备
        /// </summary>
        private bool RegisterDevices()
        {
            var keyboardDevice = new RAWINPUTDEVICE
            {
                usUsagePage = 0x01,                    // Generic Desktop
                usUsage = 0x06,                        // Keyboard
                dwFlags = RIDEV_INPUTSINK,             // 接收后台输入 + 移除默认处理
                hwndTarget = _windowHandle              // 接收窗口
            };

            var devices = new RAWINPUTDEVICE[] { keyboardDevice };
            bool success = RegisterRawInputDevices(devices, devices.Length, Marshal.SizeOf<RAWINPUTDEVICE>());

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                Debug.LogError($"[RawInputManager] RegisterRawInputDevices failed with error {error}");
                return false;
            }

            Debug.Log("[RawInputManager] Raw input devices registered");
            return true;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            // 恢复原始窗口过程
            if (_isWindowSubclassed && _windowHandle != IntPtr.Zero && _originalWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _originalWndProc);
                _isWindowSubclassed = false;
                Debug.Log("[RawInputManager] Window subclassing removed");
            }

            // 释放委托
            if (_wndProcDelegate != null)
            {
                try
                {
                    GCHandle handle = GCHandle.Alloc(_wndProcDelegate, GCHandleType.Pinned);
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
                catch { }
                _wndProcDelegate = null;
            }

            _ourWndProcPtr = IntPtr.Zero;
            _originalWndProc = IntPtr.Zero;
            _windowHandle = IntPtr.Zero;
            _isInitialized = false;

            Debug.Log("[RawInputManager] Cleanup completed");
        }

        #endregion

        #region Virtual Key Codes (与 GlobalKeyboardHook 兼容)
        public static class KeyCode
        {
            public const int VK_BACK = 0x08;
            public const int VK_TAB = 0x09;
            public const int VK_RETURN = 0x0D;
            public const int VK_SHIFT = 0x10;
            public const int VK_CONTROL = 0x11;
            public const int VK_MENU = 0x12;
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

            // 字母键 A-Z
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

            // 数字键 0-9
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

            // 数字小键盘
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
        }
        #endregion
    }
}
#endif // UNITY_STANDALONE_WIN
