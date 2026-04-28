using System;
using UnityEngine;
using Game1.Modules.PendingEvent;

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
        /// 计算离线收益（带护肝机制）
        /// </summary>
        public float CalculateOfflineReward(float offlineSeconds)
        {
            // 护肝机制：
            // 1. 上限24小时
            // 2. 前6小时正常收益，6小时外收益减半
            float cappedSeconds = Mathf.Min(offlineSeconds, 24f * 3600f);

            // 分段计算：前6小时100%，之后50%
            float fullRateSeconds = Mathf.Min(cappedSeconds, 6f * 3600f);
            float reducedRateSeconds = cappedSeconds - fullRateSeconds;

            float fullRateReward = fullRateSeconds * baseRewardPerSecond * offlineRewardRate * GetCurrentRewardRate();
            float reducedRateReward = reducedRateSeconds * baseRewardPerSecond * offlineRewardRate * GetCurrentRewardRate() * 0.5f;

            return fullRateReward + reducedRateReward;
        }

        /// <summary>
        /// 应用离线收益到玩家
        /// </summary>
        public void ApplyOfflineReward(float offlineSeconds)
        {
            if (_player == null)
            {
                Debug.LogWarning("[IdleRewardModule] ApplyOfflineReward: _player is null");
                return;
            }

            float reward = CalculateOfflineReward(offlineSeconds);

            // 使用float累加，最后再取整，避免精度丢失
            float newGold = _player.carryItems.gold + reward;
            _player.carryItems.gold = Mathf.FloorToInt(newGold);
            _totalEarned += reward;

            Debug.Log($"[IdleRewardModule] Offline reward applied: +{Mathf.FloorToInt(reward)} gold for {offlineSeconds / 3600f:F1} hours");

            // 生成积压事件
            if (offlineSeconds > 60f) // 超过1分钟离线才生成
            {
                PendingEventManager.GeneratePendingEvents(offlineSeconds);
            }
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
            // 使用float累加，最后再取整，避免精度丢失
            float reward = baseRewardPerSecond * GetCurrentRewardRate() * deltaTime;
            float newGold = _player.carryItems.gold + reward;
            _player.carryItems.gold = Mathf.FloorToInt(newGold);
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
