using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    public class UIGameDashboard : MonoBehaviour
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
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (ProgressManager.instance != null)
            {
                ProgressManager.instance.onProgressChanged -= OnProgressChanged;
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
            // 更新调试信息
            GameDebug.instance?.Update();

            // 更新TravelPoint平均速率（过去60秒）
            if (travelRateText != null)
            {
                float travelPointRate = ProgressManager.instance.travelRate;
                travelRateText.text = $"{travelPointRate:F1} TP/s";
            }

            // 更新当前金币显示（背包中GoldCoin数目）
            if (goldText != null)
            {
                int goldCount = InventoryDesign.instance.GetTotalAmountByTemplateId("Core.Item.GoldCoin");
                goldText.text = $"Gold {goldCount}";
            }
        }
    }
}