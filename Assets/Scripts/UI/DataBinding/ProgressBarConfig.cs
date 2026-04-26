using UnityEngine;

namespace Game1.UI.DataBinding
{
    /// <summary>
    /// ProgressBar更新模式枚举
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// 事件驱动模式：依赖事件触发更新，不使用Update轮询
        /// </summary>
        EventDriven,

        /// <summary>
        /// 插值模式：使用Lerp进行平滑过渡
        /// </summary>
        Lerp
    }

    /// <summary>
    /// ProgressBar配置数据ScriptableObject
    /// 用于定义ProgressBar的行为模式和动画参数
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressBarConfig", menuName = "Game1/UI/ProgressBarConfig")]
    public class ProgressBarConfig : ScriptableObject
    {
        /// <summary>
        /// 更新模式：事件驱动或插值
        /// </summary>
        [Tooltip("更新模式：EventDriven使用事件触发，Lerp使用插值平滑")]
        public UpdateMode updateMode = UpdateMode.Lerp;

        /// <summary>
        /// 插值速度，仅在Lerp模式下生效
        /// </summary>
        [Tooltip("插值速度，仅在Lerp模式下生效")]
        public float lerpSpeed = 8f;

        /// <summary>
        /// 自定义动画曲线，用于更精细的速度控制
        /// </summary>
        [Tooltip("自定义动画曲线，用于更精细的速度控制")]
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}