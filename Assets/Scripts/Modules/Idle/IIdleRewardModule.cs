namespace Game1.Modules.Idle
{
    /// <summary>
    /// 挂机收益模块接口 (用于VContainer DI)
    /// </summary>
    public interface IIdleRewardModule
    {
        /// <summary>
        /// 初始化挂机收益模块
        /// </summary>
        void Initialize(PlayerActor player);

        /// <summary>
        /// 主循环Tick
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 计算离线收益
        /// </summary>
        void CalculateOfflineEarnings(float offlineSeconds);

        /// <summary>
        /// 获取当前挂机收益速率 (金币/秒)
        /// </summary>
        float GetCurrentRewardRate();
    }
}
