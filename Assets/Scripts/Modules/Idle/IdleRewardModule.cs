using System;

namespace Game1
{
    /// <summary>
    /// 挂机收益模块接口
    /// </summary>
    public interface IIdleRewardModule : IModule
    {
        float GetCurrentRewardRate();
    }

    /// <summary>
    /// 挂机收益模块
    /// </summary>
    [Serializable]
    public class IdleRewardModule : IIdleRewardModule
    {
        public string moduleId => "idle_reward";
        public string moduleName => "挂机收益";

        // 配置
        [UnityEngine.SerializeField] private float _baseRewardPerSecond = 1f; // 每秒基础旅费
        public float baseRewardPerSecond => _baseRewardPerSecond;
        public float offlineRewardRate = 0.5f; // 离线收益比例

        // 状态
        private float _accumulatedTime;
        private float _totalEarned;
        private bool _isActive = true;

        /// <summary>
        /// 获取当前收益倍率
        /// </summary>
        public float GetCurrentRewardRate()
        {
            // TODO: 从PlayerActor获取加成
            return 1f;
        }

        /// <summary>
        /// 计算离线收益
        /// </summary>
        public float CalculateOfflineReward(float offlineSeconds)
        {
            return offlineSeconds * baseRewardPerSecond * offlineRewardRate * GetCurrentRewardRate();
        }

        #region IModule Members
        public string GetBonus(string bonusType)
        {
            switch (bonusType)
            {
                case "idle_rate":
                    return (GetCurrentRewardRate() * baseRewardPerSecond).ToString();
                case "offline_rate":
                    return offlineRewardRate.ToString();
                default:
                    return "0";
            }
        }

        public void OnTick(float deltaTime)
        {
            if (!_isActive) return;
            _accumulatedTime += deltaTime;
            // TODO: 实际添加收益到玩家金币
        }

        public void OnActivate()
        {
            _isActive = true;
        }

        public void OnDeactivate()
        {
            _isActive = false;
        }
        #endregion
    }

    /// <summary>
    /// 加成倍率模块
    /// </summary>
    [Serializable]
    public class BonusMultiplierModule : IModule
    {
        public string moduleId => "bonus_multiplier";
        public string moduleName => "加成倍率";

        public string multiplierType; // "idle", "trade", "combat"
        public float multiplierValue = 1f;

        public string GetBonus(string bonusType)
        {
            if (bonusType == multiplierType)
                return multiplierValue.ToString();
            return "0";
        }

        public void OnTick(float deltaTime) { }
        public void OnActivate() { }
        public void OnDeactivate() { }
    }
}
