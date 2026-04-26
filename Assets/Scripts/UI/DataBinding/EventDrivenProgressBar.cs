using UnityEngine;

namespace Game1.UI.DataBinding
{
    /// <summary>
    /// 事件驱动ProgressBar
    /// 继承自UIProgressBar，禁用Update轮询，依赖事件触发更新
    /// 仅当config.updateMode == EventDriven时激活
    /// </summary>
    public class EventDrivenProgressBar : UIProgressBar
    {
        /// <summary>
        /// 是否已启用事件驱动模式
        /// </summary>
        private bool _isEventDrivenActive;

        /// <summary>
        /// 带配置的构造函数
        /// </summary>
        /// <param name="config">ProgressBar配置数据</param>
        public EventDrivenProgressBar(ProgressBarConfig config) : base(config)
        {
            if (config != null && config.updateMode == UpdateMode.EventDriven)
            {
                _isEventDrivenActive = true;
            }
        }

        protected new void Awake()
        {
            base.Awake();

            // 检查配置是否为事件驱动模式
            if (config != null && config.updateMode == UpdateMode.EventDriven)
            {
                _isEventDrivenActive = true;
                // 禁用优化选项，因为事件驱动模式不需要Update轮询
                enableOptimization = false;
            }
        }

        /// <summary>
        /// 重写Update方法
        /// 事件驱动模式下禁用轮询，仅依赖事件更新
        /// </summary>
        protected new void Update()
        {
            // 非事件驱动模式，使用父类逻辑
            if (!_isEventDrivenActive)
            {
                base.Update();
                return;
            }

            // 事件驱动模式：不做任何轮询处理
            // 值的变化由外部事件驱动
        }

        /// <summary>
        /// 处理事件驱动更新
        /// 由外部Owner调用
        /// </summary>
        /// <param name="current">当前值</param>
        /// <param name="target">目标值</param>
        public void OnEventUpdate(float current, float target)
        {
            if (!_isEventDrivenActive)
                return;

            this.ratio = current;
            this.targetRatio = target;
            UpdateBar();
        }

        /// <summary>
        /// 注册进度更新事件
        /// </summary>
        public void SubscribeToProgressUpdate()
        {
            if (_isEventDrivenActive && HasProgressUpdateSubscribers())
            {
                // 外部可以通过订阅OnProgressUpdate来获取更新通知
            }
        }
    }
}