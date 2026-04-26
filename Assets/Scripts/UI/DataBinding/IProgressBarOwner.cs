namespace Game1.UI.DataBinding
{
    /// <summary>
    /// ProgressBar所有者接口
    /// 实现此接口的UI组件负责更新ProgressBar的值
    /// 用于事件驱动模式的解耦
    /// </summary>
    public interface IProgressBarOwner
    {
        /// <summary>
        /// 更新ProgressBar的进度
        /// </summary>
        /// <param name="current">当前进度值</param>
        /// <param name="max">最大进度值</param>
        void UpdateProgress(float current, float max);
    }
}