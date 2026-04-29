using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    public class UIGameDashboard : MonoBehaviour, IInventoryEventSubscriber
    {
        public UIProgressBar travelProgressBar;
        public UIProgressBar xpProgressBar;
        public UIProgressBar foodProgressBar;

        [Header("侧边栏 - 静态元素")]
        public Button inventoryButton;
        public Button teamButton;

        [Header("挂机信息 - 动态元素")]
        public UIText travelRateText;
        public UIText goldText;

        [Header("调试信息 - HUD元素")]
        public UIText debugText;

        [Header("动静分离配置")]
        [Tooltip("是否启用Canvas动静分离优化")]
        public bool enableCanvasSeparation = true;

        // 动静分离后的父物体引用
        private Transform _staticRoot;
        private Transform _dynamicRoot;
        private Transform _hudRoot;

        private IdleRewardModule _idleModule;

        // 事件驱动缓存
        private int _lastGoldAmount = -1;          // -1 = 未初始化
        private float _travelRateUpdateTimer = 0f;
        private const float TRAVEL_RATE_UPDATE_INTERVAL = 0.5f; // 500ms

        public void Initialize()
        {
            // 动静分离初始化
            if (enableCanvasSeparation)
            {
                SetupCanvasSeparation();
            }

            if (inventoryButton != null && UIManager.instance?.inventory != null)
            {
                inventoryButton.onClick.RemoveAllListeners();
                inventoryButton.onClick.AddListener(() =>
                {
                    UIManager.instance.inventory.Open();
                    UIManager.instance.inventory.Refresh();
                });
            }

            if (teamButton != null && UIManager.instance?.team != null)
            {
                teamButton.onClick.RemoveAllListeners();
                teamButton.onClick.AddListener(() =>
                {
                    UIManager.instance.team.Open();
                    UIManager.instance.team.Refresh();
                });
            }

            // 获取挂机模块引用
            var player = GameMain.instance?.GetPlayerActor();
            _idleModule = player?.modules.GetModule<IdleRewardModule>();

            // 订阅进度变化事件，同步 TravelPoint 进度条
            ProgressManager.instance.onProgressChanged += OnProgressChanged;

            // 初始化进度条显示
            if (travelProgressBar != null)
            {
                travelProgressBar.UpdateBarImmediate(ProgressManager.instance.progressPercent);
            }

            // 初始化调试信息
            if (debugText != null)
            {
                GameDebug.instance.debugText = debugText;
            }

            // 订阅背包事件，替代每帧轮询
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemAdded, this);
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemRemoved, this);
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemUpdated, this);
            InventoryEventBus.instance.Subscribe(InventoryEventType.InventoryCleared, this);

            // 初始化金币显示
            RefreshGoldDisplay();
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (ProgressManager.instance != null)
            {
                ProgressManager.instance.onProgressChanged -= OnProgressChanged;
            }

            // 取消背包事件订阅
            if (InventoryEventBus.instance != null)
            {
                InventoryEventBus.instance.UnsubscribeAll(this);
            }
        }

        #region Canvas Separation
        /// <summary>
        /// 设置Canvas动静分离
        /// 将UI元素按更新频率分离到不同Canvas以优化Draw Call
        /// </summary>
        private void SetupCanvasSeparation()
        {
            // 如果UICanvasManager不存在，尝试获取
            if (UICanvasManager.instance == null)
            {
                Debug.LogWarning("[UIGameDashboard] UICanvasManager not found, skipping canvas separation");
                return;
            }

            var canvasManager = UICanvasManager.instance;

            // 创建动静分离的父物体
            _staticRoot = CreateRoot("StaticRoot", canvasManager.GetStaticCanvas());
            _dynamicRoot = CreateRoot("DynamicRoot", canvasManager.GetDynamicCanvas());
            _hudRoot = CreateRoot("HUDRoot", canvasManager.GetHUDCanvas());

            // 分离静态元素（按钮）
            MoveToRoot(inventoryButton?.transform, _staticRoot);
            MoveToRoot(teamButton?.transform, _staticRoot);

            // 分离动态元素（进度条、动态文本）
            MoveToRoot(travelProgressBar?.transform, _dynamicRoot);
            MoveToRoot(xpProgressBar?.transform, _dynamicRoot);
            MoveToRoot(foodProgressBar?.transform, _dynamicRoot);
            MoveToRoot(travelRateText?.transform, _dynamicRoot);
            MoveToRoot(goldText?.transform, _dynamicRoot);

            // 分离HUD元素（调试信息）
            MoveToRoot(debugText?.transform, _hudRoot);

            Debug.Log("[UIGameDashboard] Canvas separation completed");
        }

        private Transform CreateRoot(string name, Canvas parentCanvas)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parentCanvas?.transform, false);

            // 添加LayoutGroup便于子元素布局
            var vLayout = go.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 0;
            vLayout.padding = new RectOffset(0, 0, 0, 0);
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            // 标记为静态以优化
            go.SetActive(false);
            go.SetActive(true);

            return go.transform;
        }

        private void MoveToRoot(Transform target, Transform root)
        {
            if (target != null && root != null)
            {
                target.SetParent(root, false);
            }
        }
        #endregion

        private void OnProgressChanged(ProgressEventData data)
        {
            if (travelProgressBar != null)
            {
                // 使用 ProgressManager 的 progressPercent（当前点数 / 1000）
                travelProgressBar.UpdateBar(ProgressManager.instance.progressPercent);
            }
        }

        private void Update()
        {
            // 更新调试信息（调试信息需要每帧刷新）
            GameDebug.instance?.Update();

            // 节流更新TravelPoint平均速率（每500ms，替代每帧查询）
            UpdateTravelRateThrottled();
        }

        #region Event-Driven UI Updates

        /// <summary>
        /// IInventoryEventSubscriber 实现
        /// 背包变更时由InventoryEventBus调用，替代每帧轮询
        /// </summary>
        public void OnInventoryEvent(InventoryEventData data)
        {
            // 仅在 GoldCoin 相关变更时更新显示
            if (data.templateId == "Core.Item.GoldCoin" || data.eventType == InventoryEventType.InventoryCleared)
            {
                RefreshGoldDisplay();
            }
        }

        /// <summary>
        /// 刷新金币显示，仅在值变化时更新Text组件
        /// </summary>
        private void RefreshGoldDisplay()
        {
            if (goldText == null) return;

            int goldCount = InventoryDesign.instance.GetTotalAmountByTemplateId("Core.Item.GoldCoin");
            if (goldCount != _lastGoldAmount)
            {
                goldText.text = $"Gold {goldCount}";
                _lastGoldAmount = goldCount;
            }
        }

        /// <summary>
        /// 节流更新TravelPoint速率文本
        /// 每500ms更新一次，值无变化时跳过Text组件更新
        /// </summary>
        private void UpdateTravelRateThrottled()
        {
            if (travelRateText == null) return;

            _travelRateUpdateTimer += Time.deltaTime;
            if (_travelRateUpdateTimer < TRAVEL_RATE_UPDATE_INTERVAL)
                return;

            _travelRateUpdateTimer = 0f;

            float travelPointRate = ProgressManager.instance.travelRate;
            string newText = $"{travelPointRate:F1} TP/s";
            if (travelRateText.text != newText)
            {
                travelRateText.text = newText;
            }
        }

        #endregion
    }
}