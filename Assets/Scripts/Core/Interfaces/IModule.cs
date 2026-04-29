namespace Game1
{
    /// <summary>
    /// 模块接口
    /// 所有模块类实现的通用接口
    /// </summary>
    public interface IModule
    {
        string moduleId { get; }
        string moduleName { get; }
        string GetBonus(string bonusType);
        void Tick(float deltaTime);
        void OnActivate();
        void OnDeactivate();
    }
}
