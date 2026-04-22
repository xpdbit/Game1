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
        public UIText idleRateText;
        public UIText goldText;

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
            if (_idleModule == null) return;

            // 更新挂机金币显示
            float rate = _idleModule.GetCurrentRewardRate();
            if (idleRateText != null)
            {
                idleRateText.text = $"{rate:F1} 金币/秒";
            }

            // 更新当前金币显示
            if (goldText != null)
            {
                var player = GameMain.instance?.GetPlayerActor();
                if (player != null)
                {
                    goldText.text = $"{player.carryItems.gold}";
                }
            }
        }
    }
}