namespace Game1.Modules.Travel
{
    /// <summary>
    /// 旅行系统接口 (用于VContainer DI)
    /// </summary>
    public interface ITravelManager
    {
        /// <summary>
        /// 当前旅行状态
        /// </summary>
        TravelManager.TravelStatus status { get; }

        /// <summary>
        /// 当前进度 (0-1)
        /// </summary>
        float currentProgress { get; }

        /// <summary>
        /// 距离下一事件的进度点
        /// </summary>
        float progressToNextEvent { get; }

        /// <summary>
        /// 初始化旅行系统
        /// </summary>
        void Initialize(PlayerActor player);

        /// <summary>
        /// 主循环Tick
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 玩家交互（触发选择等）
        /// </summary>
        void OnPlayerInteract();

        /// <summary>
        /// 触发事件链
        /// </summary>
        void TriggerEventChain(string eventChainId);

        /// <summary>
        /// 设置事件队列引用
        /// </summary>
        void SetEventQueue(EventQueue eventQueue);
    }
}
