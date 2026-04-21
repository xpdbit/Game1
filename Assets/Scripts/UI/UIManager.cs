using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        #endregion

        #region UI Components
        [Header("核心UI引用")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gameHUDPanel;
        [SerializeField] private GameObject _eventPanel;
        [SerializeField] private GameObject _pausePanel;

        [Header("游戏信息")]
        [SerializeField] private UIProgressBar _travelProgressBar;
        [SerializeField] private UIText _goldText;
        [SerializeField] private UIText _levelText;
        [SerializeField] private UIText _stateText;

        [Header("模块")]
        public UIInventory inventory;

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

            InitializePanels();
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
        private void InitializePanels()
        {
            // 注册所有面板
            if (_loadingPanel != null)
                RegisterPanel(_loadingPanel.GetComponent<IUIPanel>());

            if (_mainMenuPanel != null)
                RegisterPanel(_mainMenuPanel.GetComponent<IUIPanel>());

            if (_gameHUDPanel != null)
                RegisterPanel(_gameHUDPanel.GetComponent<IUIPanel>());

            if (_eventPanel != null)
                RegisterPanel(_eventPanel.GetComponent<IUIPanel>());

            if (_pausePanel != null)
                RegisterPanel(_pausePanel.GetComponent<IUIPanel>());

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
            EventBus.instance.Subscribe(EventType.GoldChanged, new GoldChangedHandler(this));
            EventBus.instance.Subscribe(EventType.LevelUp, new LevelUpHandler(this));
            EventBus.instance.Subscribe(EventType.TravelStarted, new TravelStartedHandler(this));
            EventBus.instance.Subscribe(EventType.TravelCompleted, new TravelCompletedHandler(this));
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
            switch (state)
            {
                case UIState.Loading:
                    ClosePanel("LoadingPanel");
                    break;
            }
        }

        private void OnStateEnter(UIState state)
        {
            switch (state)
            {
                case UIState.Loading:
                    OpenPanel("LoadingPanel");
                    break;
                case UIState.MainMenu:
                    OpenPanel("MainMenuPanel");
                    break;
                case UIState.Playing:
                    OpenPanel("GameHUDPanel");
                    break;
                case UIState.Paused:
                    OpenPanel("PausePanel");
                    break;
                case UIState.Event:
                    OpenPanel("EventPanel");
                    break;
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

        /// <summary>
        /// 更新旅行进度
        /// </summary>
        public void UpdateTravelProgress(float progress)
        {
            if (_travelProgressBar != null)
            {
                _travelProgressBar.UpdateBarImmediate(progress);
            }
        }

        /// <summary>
        /// 更新金币显示
        /// </summary>
        public void UpdateGold(int gold)
        {
            if (_goldText != null)
            {
                _goldText.text = $"金币: {gold}";
            }
        }

        /// <summary>
        /// 更新等级显示
        /// </summary>
        public void UpdateLevel(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"等级: {level}";
            }
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        public void UpdateStateText(string text)
        {
            if (_stateText != null)
            {
                _stateText.text = text;
            }
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

        // 金币变化处理
        private class GoldChangedHandler : IEventSubscriber
        {
            private readonly UIManager _ui;
            public GoldChangedHandler(UIManager ui) { _ui = ui; }
            public void OnEvent(GameEvent e)
            {
                if (e.data is int gold)
                    _ui.UpdateGold(gold);
            }
        }

        // 升级处理
        private class LevelUpHandler : IEventSubscriber
        {
            private readonly UIManager _ui;
            public LevelUpHandler(UIManager ui) { _ui = ui; }
            public void OnEvent(GameEvent e)
            {
                if (e.data is int level)
                    _ui.UpdateLevel(level);
            }
        }

        // 旅行开始处理
        private class TravelStartedHandler : IEventSubscriber
        {
            private readonly UIManager _ui;
            public TravelStartedHandler(UIManager ui) { _ui = ui; }
            public void OnEvent(GameEvent e)
            {
                _ui.UpdateStateText("旅行中...");
            }
        }

        // 旅行完成处理
        private class TravelCompletedHandler : IEventSubscriber
        {
            private readonly UIManager _ui;
            public TravelCompletedHandler(UIManager ui) { _ui = ui; }
            public void OnEvent(GameEvent e)
            {
                _ui.UpdateStateText("已到达");
            }
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

    /// <summary>
    /// 游戏HUD面板
    /// </summary>
    public class GameHUDPanel : BaseUIPanel
    {
        public override string panelId => "GameHUDPanel";

        [SerializeField] private UIProgressBar _progressBar;
        [SerializeField] private UIText _infoText;

        public override void OnUpdate(float deltaTime)
        {
            // 更新HUD信息
        }
    }
    #endregion
}
