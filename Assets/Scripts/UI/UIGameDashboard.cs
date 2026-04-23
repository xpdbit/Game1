using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    public class UIGameDashboard : MonoBehaviour
    {
        public UIProgressBar travelProgressBar;
        public UIProgressBar xpProgressBar;
        public UIProgressBar foodProgressBar;

        [Header("侧边栏")]
        public Button inventoryButton;

        [Header("挂机信息")]
        public UIText travelRateText;
        public UIText goldText;

        [Header("调试信息")]
        public UIText debugText;

        private IdleRewardModule _idleModule;

        public void Initialize()
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(() =>
            {
                UIManager.instance.inventory.Open();
                UIManager.instance.inventory.Refresh();
            });

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
            ProgressManager.instance.onProgressChanged -= OnProgressChanged;
        }

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