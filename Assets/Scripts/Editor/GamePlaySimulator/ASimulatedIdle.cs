using System;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Simulated idle reward module (mirrors IdleRewardModule)
    /// </summary>
    [Serializable]
    public class ASimulatedIdleModule : ASimulatedModule
    {
        #region IModule Implementation
        public string moduleId => "idle";
        public string moduleName => "挂机收益";
        #endregion

        #region Configuration
        private float _baseRewardPerSecond = 1f;
        private float _offlineRewardRate = 0.5f;
        private float _offlineCapHours = 24f;
        private float _offlineFullRateHours = 6f;
        #endregion

        #region State
        private ASimulatedPlayer _player;
        private bool _isActive = true;
        private float _accumulatedTime;
        private float _totalEarned;
        private float _sessionStartTime;
        #endregion

        #region Events
        public event System.Action<int> onGoldEarned;
        #endregion

        public ASimulatedIdleModule() { }

        public void Initialize(ASimulatedPlayer player)
        {
            _player = player;
            _accumulatedTime = 0f;
            _totalEarned = 0f;
            _sessionStartTime = Time.time;
        }

        #region IModule Members
        public string GetBonus(string bonusType)
        {
            switch (bonusType)
            {
                case "idle_rate":
                    return _baseRewardPerSecond.ToString();
                case "offline_rate":
                    return _offlineRewardRate.ToString();
                default:
                    return "0";
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive || _player == null) return;

            _accumulatedTime += deltaTime;

            // Calculate reward: base * (1 + player bonus) * deltaTime
            float rewardRate = GetCurrentRewardRate();
            float reward = _baseRewardPerSecond * rewardRate * deltaTime;

            int goldEarned = Mathf.FloorToInt(reward);
            if (goldEarned > 0)
            {
                _player.AddGold(goldEarned);
                _totalEarned += goldEarned;
                onGoldEarned?.Invoke(goldEarned);
            }
        }

        public void OnActivate() => _isActive = true;
        public void OnDeactivate() => _isActive = false;
        #endregion

        #region Reward Calculation
        /// <summary>
        /// Get current reward rate multiplier from player bonuses
        /// </summary>
        public float GetCurrentRewardRate()
        {
            return 1f + _player.GetTotalBonus("idle_rate");
        }

        /// <summary>
        /// Calculate offline reward (mirrors IdleRewardModule.CalculateOfflineReward)
        /// </summary>
        public int CalculateOfflineReward(float offlineSeconds)
        {
            var config = AConfig.Active;

            // Cap offline time
            float cappedSeconds = Mathf.Min(offlineSeconds, config.offlineCapHours * 3600f);

            // First X hours at 100%, rest at 50%
            float fullRateSeconds = Mathf.Min(cappedSeconds, config.offlineFullRateHours * 3600f);
            float reducedRateSeconds = cappedSeconds - fullRateSeconds;

            float rewardRate = GetCurrentRewardRate();

            float fullRateReward = fullRateSeconds * _baseRewardPerSecond * _offlineRewardRate * rewardRate;
            float reducedRateReward = reducedRateSeconds * _baseRewardPerSecond * _offlineRewardRate * rewardRate * 0.5f;

            return Mathf.FloorToInt(fullRateReward + reducedRateReward);
        }

        /// <summary>
        /// Apply offline reward to player
        /// </summary>
        public void ApplyOfflineReward(float offlineSeconds)
        {
            int reward = CalculateOfflineReward(offlineSeconds);
            if (reward > 0)
            {
                _player.AddGold(reward);
                _totalEarned += reward;
                Debug.Log($"[ASimulatedIdle] Applied offline reward: {reward} gold for {offlineSeconds / 3600f:F1} hours");
            }
        }
        #endregion

        #region Statistics
        public float AccumulatedTime => _accumulatedTime;
        public float TotalEarned => _totalEarned;
        public float AverageGoldPerSecond => _accumulatedTime > 0 ? _totalEarned / _accumulatedTime : 0f;

        public void Reset()
        {
            _accumulatedTime = 0f;
            _totalEarned = 0f;
            _sessionStartTime = Time.time;
        }
        #endregion
    }

    /// <summary>
    /// Idle simulation orchestrator
    /// </summary>
    public class ASimulatedIdle
    {
        private ASimulatedPlayer _player;
        private ASimulatedIdleModule _idleModule;
        private float _simulationTime;

        public ASimulatedIdle(ASimulatedPlayer player)
        {
            _player = player;
            _idleModule = new ASimulatedIdleModule();
            _idleModule.Initialize(player);
            _player.AddModule(_idleModule);
        }

        /// <summary>
        /// Simulate idle for specified seconds
        /// </summary>
        public void Simulate(float seconds)
        {
            _simulationTime = 0f;
            float tickInterval = 0.1f; // 100ms ticks

            while (_simulationTime < seconds)
            {
                float delta = Mathf.Min(tickInterval, seconds - _simulationTime);
                _idleModule.Tick(delta);
                _simulationTime += delta;
            }
        }

        /// <summary>
        /// Simulate offline with return
        /// </summary>
        public int SimulateOffline(float hours)
        {
            float seconds = hours * 3600f;
            int goldBefore = _player.carryItems.gold;
            _idleModule.ApplyOfflineReward(seconds);
            return _player.carryItems.gold - goldBefore;
        }

        public ASimulatedIdleModule Module => _idleModule;
        public float SimulationTime => _simulationTime;
        public float GoldEarned => _idleModule.TotalEarned;
    }
}