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
        public float baseRewardPerSecond = 1f; // 每秒基础收益
        public float offlineRewardRate = 0.5f; // 离线收益比例

        // 状态
        private float _accumulatedTime;
        private float _totalEarned;
        private bool _isActive = true;
        private PlayerActor _player;

        /// <summary>
        /// 累计在线时间（秒）
        /// </summary>
        public float accumulatedTime => _accumulatedTime;

        /// <summary>
        /// 累计获得金币
        /// </summary>
        public float totalEarned => _totalEarned;

        /// <summary>
        /// 初始化模块
        /// </summary>
        public void Initialize(PlayerActor player)
        {
            _player = player;
        }

        /// <summary>
        /// 获取当前收益倍率（基于加成系统）
        /// </summary>
        public float GetCurrentRewardRate()
        {
            if (_player == null) return 1f;
            // 基础1.0 + 加成系统返回的idle_rate加成
            return 1f + _player.GetTotalBonus("idle_rate");
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
                    // 返回基础收益值，不包含加成（避免递归）
                    return baseRewardPerSecond.ToString();
                case "offline_rate":
                    return offlineRewardRate.ToString();
                default:
                    return "0";
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive || _player == null) return;
            _accumulatedTime += deltaTime;

            // 计算本tick的收益并添加到玩家金币
            float reward = baseRewardPerSecond * GetCurrentRewardRate() * deltaTime;
            _player.carryItems.gold += (int)reward;
            _totalEarned += reward;
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

        public void Tick(float deltaTime) { }
        public void OnActivate() { }
        public void OnDeactivate() { }
    }
}
