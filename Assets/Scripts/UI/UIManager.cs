using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game1.Core.EventBus;

namespace Game1
{
    /// <summary>
    /// UI状态
    /// </summary>
    public enum UIState
    {
        Loading,     // 加载
        MainMenu,   // 主菜单
        Playing,    // 游戏中
        Paused,     // 暂停
        Event,      // 事件处理中
    }

    /// <summary>
    /// UI状态配置 - 数据驱动核心
    /// </summary>
    [Serializable]
    public class UIStateConfig
    {
        public UIState state;
        [Tooltip("进入状态时打开的面板ID列表")]
        public string[] openPanelsOnEnter = Array.Empty<string>();
        [Tooltip("退出状态时关闭的面板ID列表")]
        public string[] closePanelsOnExit = Array.Empty<string>();
    }

    /// <summary>
    /// 状态表 - 替代switch硬编码
    /// </summary>
    [Serializable]
    public class UIStateTable
    {
        [Tooltip("状态配置列表")]
        public UIStateConfig[] configs = Array.Empty<UIStateConfig>();

        private Dictionary<UIState, UIStateConfig> _configMap;

        /// <summary>
        /// 获取默认状态表配置（用于编辑器配置或回退）
        /// </summary>
        public static UIStateConfig[] GetDefaultConfigs()
        {
            return new UIStateConfig[]
            {
                new UIStateConfig { state = UIState.Loading, openPanelsOnEnter = new[] { "LoadingPanel" }, closePanelsOnExit = new[] { "LoadingPanel" } },
                new UIStateConfig { state = UIState.MainMenu, openPanelsOnEnter = new[] { "MainMenuPanel" } },
                new UIStateConfig { state = UIState.Playing, openPanelsOnEnter = new[] { "GameHUDPanel" } },
                new UIStateConfig { state = UIState.Paused, openPanelsOnEnter = new[] { "PausePanel" } },
                new UIStateConfig { state = UIState.Event, openPanelsOnEnter = new[] { "EventPanel" } },
            };
        }

        /// <summary>
        /// 初始化状态表（若序列化为空，使用默认配置）
        /// </summary>
        public void Initialize()
        {
            _configMap = new Dictionary<UIState, UIStateConfig>();

            // 使用序列化的configs，如果为空则使用默认配置保持向后兼容
            var configsToUse = configs.Length > 0 ? configs : GetDefaultConfigs();

            foreach (var config in configsToUse)
            {
                if (config != null)
                    _configMap[config.state] = config;
            }
        }

        /// <summary>
        /// 获取状态配置
        /// </summary>
        public bool TryGetConfig(UIState state, out UIStateConfig config)
        {
            if (_configMap == null)
                Initialize();
            return _configMap.TryGetValue(state, out config);
        }

        /// <summary>
        /// 获取进入状态时需要打开的面板
        /// </summary>
        public string[] GetOpenPanelsOnEnter(UIState state)
        {
            if (TryGetConfig(state, out var config))
                return config.openPanelsOnEnter;
            return Array.Empty<string>();
        }

        /// <summary>
        /// 获取退出状态时需要关闭的面板
        /// </summary>
        public string[] GetClosePanelsOnExit(UIState state)
        {
            if (TryGetConfig(state, out var config))
                return config.closePanelsOnExit;
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// UI面板接口
    /// </summary>
    public interface IUIPanel
    {
        string panelId { get; }
        bool isOpen { get; }
        void OnOpen();
        void OnClose();
        void OnUpdate(float deltaTime);
    }

    /// <summary>
    /// UI管理器 - 协调所有UI面板和交互
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager instance { get; private set; }
        #endregion

        #region State
        [SerializeField] private UIState _currentState = UIState.Loading;
        public UIState currentState => _currentState;

        // 面板字典
        private Dictionary<string, IUIPanel> _panels = new();
        private Stack<string> _panelStack = new(); // 用于管理面板堆栈（返回）

        // 状态表 - 数据驱动替代switch硬编码
        [Header("状态配置")]
        [Tooltip("状态表配置 - 新增状态只需在此配置，无需修改源码")]
        public UIStateTable stateTable = new();
        #endregion

        #region UI Components
        [Header("模块")]
        public UIGameDashboard gameDashboard;
        public UIInventory inventory;
        public UITeam team;

        #endregion

        #region Events
        public event Action<UIState> onStateChanged;
        public event Action<string> onPanelOpened;
        public event Action<string> onPanelClosed;
        public event Action onLoadingComplete;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            Initialize();
            SubscribeEvents();
        }

        private void Start()
        {
            // 初始状态
            ChangeState(UIState.Loading);

            // 模拟加载完成
            Invoke(nameof(OnLoadingComplete), 1f);
        }

        private void Update()
        {
            // 处理返回键 (使用新Input System)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnEscapePressed();
            }

            // 更新当前面板
            UpdateCurrentPanel(Time.deltaTime);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化所有面板
        /// </summary>
        private void Initialize()
        {
            // 初始化状态表
            stateTable.Initialize();

            gameDashboard.Initialize();

            // 初始关闭所有面板
            CloseAllPanels();
        }

        /// <summary>
        /// 注册面板
        /// </summary>
        public void RegisterPanel(IUIPanel panel)
        {
            if (panel == null) return;
            _panels[panel.panelId] = panel;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeEvents()
        {

        }

        private void UnsubscribeEvents()
        {
            EventBus.instance.Clear();
        }
        #endregion

        #region State Management
        /// <summary>
        /// 切换UI状态
        /// </summary>
        public void ChangeState(UIState newState)
        {
            if (_currentState == newState) return;

            UIState oldState = _currentState;
            _currentState = newState;

            OnStateExit(oldState);
            OnStateEnter(newState);

            onStateChanged?.Invoke(newState);
        }

        private void OnStateExit(UIState state)
        {
            // 数据驱动：使用状态表获取退出状态时需要关闭的面板
            var panelsToClose = stateTable.GetClosePanelsOnExit(state);
            foreach (var panelId in panelsToClose)
            {
                ClosePanel(panelId);
            }
        }

        private void OnStateEnter(UIState state)
        {
            // 数据驱动：使用状态表获取进入状态时需要打开的面板
            var panelsToOpen = stateTable.GetOpenPanelsOnEnter(state);
            foreach (var panelId in panelsToOpen)
            {
                OpenPanel(panelId);
            }
        }
        #endregion

        #region Panel Management
        /// <summary>
        /// 打开面板
        /// </summary>
        public void OpenPanel(string panelId)
        {
            if (!_panels.ContainsKey(panelId))
            {
                Debug.LogWarning($"[UIManager] Panel not found: {panelId}");
                return;
            }

            var panel = _panels[panelId];
            if (panel.isOpen) return;

            panel.OnOpen();
            _panelStack.Push(panelId);
            onPanelOpened?.Invoke(panelId);

            Debug.Log($"[UIManager] Opened: {panelId}");
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void ClosePanel(string panelId)
        {
            if (!_panels.ContainsKey(panelId)) return;

            var panel = _panels[panelId];
            if (!panel.isOpen) return;

            panel.OnClose();
            _panelStack.Clear(); // 简化：关闭面板时清空堆栈
            onPanelClosed?.Invoke(panelId);

            Debug.Log($"[UIManager] Closed: {panelId}");
        }

        /// <summary>
        /// 关闭所有面板
        /// </summary>
        public void CloseAllPanels()
        {
            foreach (var kvp in _panels)
            {
                if (kvp.Value.isOpen)
                    kvp.Value.OnClose();
            }
            _panelStack.Clear();
        }

        /// <summary>
        /// 返回上一个面板
        /// </summary>
        public void GoBack()
        {
            if (_panelStack.Count <= 1) return;

            _panelStack.Pop(); // 移除当前
            string previousId = _panelStack.Peek();

            // 关闭当前面板（简化实现）
            CloseAllPanels();
            OpenPanel(previousId);
        }

        /// <summary>
        /// 更新当前面板
        /// </summary>
        private void UpdateCurrentPanel(float deltaTime)
        {
            if (_panelStack.Count == 0) return;
            if (_panels.TryGetValue(_panelStack.Peek(), out var panel))
            {
                panel.OnUpdate(deltaTime);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            ChangeState(UIState.Playing);
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            ChangeState(UIState.Paused);
        }

        /// <summary>
        /// 继续游戏
        /// </summary>
        public void ResumeGame()
        {
            ChangeState(UIState.Playing);
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            ChangeState(UIState.MainMenu);
        }

        /// <summary>
        /// 打开事件面板
        /// </summary>
        public void ShowEvent(string eventId)
        {
            ChangeState(UIState.Event);
        }

        /// <summary>
        /// 关闭事件面板
        /// </summary>
        public void CloseEvent()
        {
            ChangeState(UIState.Playing);
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// 处理返回键
        /// </summary>
        private void OnEscapePressed()
        {
            switch (_currentState)
            {
                case UIState.Playing:
                    PauseGame();
                    break;
                case UIState.Paused:
                    ResumeGame();
                    break;
                case UIState.Event:
                    CloseEvent();
                    break;
                case UIState.MainMenu:
                    // 退出确认...
                    break;
            }
        }
        #endregion

        #region Event Handlers
        private void OnLoadingComplete()
        {
            ChangeState(UIState.MainMenu);
            onLoadingComplete?.Invoke();
        }
        #endregion
    }

    #region Base Panel Implementation
    /// <summary>
    /// 面板基类 - 简化实现
    /// </summary>
    public abstract class BaseUIPanel : MonoBehaviour, IUIPanel
    {
        public abstract string panelId { get; }
        public bool isOpen { get; private set; }

        /// <summary>
        /// 打开面板
        /// </summary>
        public void Open()
        {
            if (isOpen) return;
            OnOpen();
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;
            OnClose();
        }

        public virtual void OnOpen()
        {
            isOpen = true;
            gameObject.SetActive(true);
            
        }

        public virtual void OnClose()
        {
            isOpen = false;
            gameObject.SetActive(false);
        }

        public virtual void OnUpdate(float deltaTime) { }
    }
    #endregion
}
