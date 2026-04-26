using System;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Simulation configuration constants
    /// </summary>
    [Serializable]
    public class ASimulatorConfig
    {
        #region Simulation Mode
        /// <summary>
        /// Run simulation in EditMode (fast) or PlayMode (visual)
        /// </summary>
        public bool runInEditMode = true;

        /// <summary>
        /// Simulation speed multiplier (1 = real-time, 10 = 10x faster)
        /// </summary>
        public float simulationSpeed = 100f;

        /// <summary>
        /// Use deterministic seed for reproducible results
        /// </summary>
        public bool useSeed = true;
        public int defaultSeed = 42;
        #endregion

        #region Player Defaults
        public int defaultPlayerLevel = 1;
        public int defaultMaxHp = 20;
        public int defaultAttack = 3;
        public int defaultDefense = 5;
        public float defaultCritChance = 0.1f;
        public float defaultDodgeChance = 0.05f;
        public float defaultCritMultiplier = 1.5f;
        #endregion

        #region Idle Reward Config
        public float baseRewardPerSecond = 1f;
        public float offlineRewardRate = 0.5f;
        public float offlineCapHours = 24f;
        public float offlineFullRateHours = 6f;
        #endregion

        #region Travel Config
        public float travelSpeed = 1f;
        public float travelSpeedBonusPerLevel = 0.05f;
        public int pointsPerSecond = 1;
        public int pointsPerClick = 10;
        public int pointsForNormalEvent = 200;
        public int pointsForEventTree = 1000;
        #endregion

        #region Combat Config
        public int baseGoldReward = 10;
        public float goldRewardPerEnemyHp = 0.2f;
        public float goldRewardPerEnemyArmor = 2f;
        public float goldRewardPerEnemyDamage = 3f;
        public int baseExpReward = 5;
        #endregion

        #region Prestige Config
        public int basePrestigePoints = 100;
        public int prestigePointsPerLevel = 50;
        public float defaultGoldRetention = 0.5f;
        public float defaultExpRetention = 0.1f;
        #endregion

        #region Visual Config
        public Color backgroundColor = Color.black;
        public Vector3 cameraPosition = new Vector3(0, 10, -10);
        public Vector3 cameraLookAt = Vector3.zero;
        public float lightIntensity = 1f;
        #endregion

        #region UI Config
        public int uiReferenceWidth = 1920;
        public int uiReferenceHeight = 1080;
        public Color progressBarBackground = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color progressBarFill = new Color(0f, 0.8f, 0.2f, 1f);
        public Color buttonBackground = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color buttonText = Color.white;
        #endregion

        #region Test Config
        public int testSessionCount = 100;
        public int testSessionDuration = 3600; // 1 hour in seconds
        public float winRateThreshold = 0.7f; // 70% win rate expected
        public float goldInflationThreshold = 2f; // Gold shouldn't grow more than 2x per hour
        #endregion
    }

    /// <summary>
    /// Singleton config holder
    /// </summary>
    public static class AConfig
    {
        public static ASimulatorConfig defaultConfig = new ASimulatorConfig();

        public static ASimulatorConfig Active { get; set; } = defaultConfig;
    }
}