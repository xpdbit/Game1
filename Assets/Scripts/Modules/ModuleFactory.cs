using System;
using System.Collections.Generic;
using Game1.Modules.Activity;
using Game1.Modules.Combat;
using Game1;

namespace Game1.Modules
{
    /// <summary>
    /// 模块工厂 - 将模块ID字符串映射到IModule实例
    /// 集中管理所有模块的创建逻辑，供EffectExecutor和EventChain使用
    /// </summary>
    public static class ModuleFactory
    {
        private static readonly Dictionary<string, Func<IModule>> _creators;

        static ModuleFactory()
        {
            _creators = new Dictionary<string, Func<IModule>>
            {
                { "idle_reward",       () => new IdleRewardModule() },
                { "bonus_multiplier",   () => new BonusMultiplierModule() },
                { "combat",             () => new CombatModule() },
                { "activity_monitor",   () => new ActivityMonitorModule() },
                { "pet_companion",      () => new PetCompanionModule() },
            };
        }

        /// <summary>
        /// 创建模块实例。返回null表示未知moduleId。
        /// </summary>
        public static IModule Create(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId))
                return null;

            if (_creators.TryGetValue(moduleId, out var factory))
            {
                var module = factory();
                UnityEngine.Debug.Log($"[ModuleFactory] Created module: {moduleId} ({module.GetType().Name})");
                return module;
            }

            UnityEngine.Debug.LogWarning($"[ModuleFactory] Unknown module ID: {moduleId}");
            return null;
        }

        /// <summary>
        /// 创建并初始化模块，然后添加到玩家
        /// </summary>
        public static bool CreateAndAddToPlayer(string moduleId, PlayerActor player)
        {
            var module = Create(moduleId);
            if (module == null) return false;

            // 尝试初始化（某些模块有Initialize方法）
            if (module is IIdleRewardModule idleModule)
                idleModule.Initialize(player);
            else if (module is ICombatModule combatModule)
                combatModule.Initialize(player);
            else if (module is IPetCompanionModule petModule)
                petModule.Initialize(player);
            else if (module is ActivityMonitorModule activityModule)
                activityModule.Initialize(player);

            player.AddModule(module);
            return true;
        }

        /// <summary>
        /// 注册自定义模块创建器（供扩展用）
        /// </summary>
        public static void Register(string moduleId, Func<IModule> creator)
        {
            if (!string.IsNullOrEmpty(moduleId) && creator != null)
                _creators[moduleId] = creator;
        }
    }
}
