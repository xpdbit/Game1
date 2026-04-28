using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 宠物状态枚举
    /// </summary>
    public enum PetState
    {
        Idle,       // 空闲状态 - 正常挂机
        Happy,      // 开心状态 - 获得奖励/成就
        Sad,        // 悲伤状态 - 血量低/失败
        Excited,    // 兴奋状态 - 进度达成/升级
    }

    /// <summary>
    /// 宠物心情数据
    /// </summary>
    [Serializable]
    public class PetMood
    {
        public float happiness = 1f;      // 0~1 开心值
        public float excitement = 0f;      // 0~1 兴奋值
        public float sadness = 0f;         // 0~1 悲伤值

        /// <summary>
        /// 根据心情值获取宠物状态
        /// </summary>
        public PetState GetState()
        {
            if (sadness > 0.6f)
                return PetState.Sad;
            if (excitement > 0.7f)
                return PetState.Excited;
            if (happiness > 0.7f)
                return PetState.Happy;
            return PetState.Idle;
        }

        /// <summary>
        /// 更新心情衰减（随时间自然衰减）
        /// </summary>
        public void Decay(float deltaTime, float decayRate = 0.1f)
        {
            excitement = Mathf.Max(0f, excitement - decayRate * deltaTime);
            sadness = Mathf.Max(0f, sadness - decayRate * 0.5f * deltaTime);
            happiness = Mathf.Clamp01(happiness + decayRate * 0.3f * deltaTime);
        }
    }

    /// <summary>
    /// 随队宠物模块接口
    /// </summary>
    public interface IPetCompanionModule : IModule
    {
        PetMood GetMood();
        PetState GetCurrentState();
        void TriggerHappy(float intensity = 1f);
        void TriggerSad(float intensity = 1f);
        void TriggerExcited(float intensity = 1f);
        void TriggerIdle();
    }

    /// <summary>
    /// 随队宠物模块 - 情感陪伴元素
    /// 作为"桌宠"存在，显示玩家状态，偶尔互动不影响核心玩法
    /// </summary>
    [Serializable]
    public class PetCompanionModule : IPetCompanionModule
    {
        public string moduleId => "pet_companion";
        public string moduleName => "随队宠物";

        // 配置
        [SerializeField] private float _moodDecayRate = 0.1f;      // 心情衰减速率
        [SerializeField] private float _stateChangeThreshold = 0.3f; // 状态切换阈值
        [SerializeField] private float _hpLowThreshold = 0.3f;       // 血量低阈值(比例)

        // 状态
        private PetMood _mood = new();
        private PetState _currentState = PetState.Idle;
        private PlayerActor _player;
        private float _stateTimer = 0f;
        private float _lastHPRatio = 1f;
        private bool _wasInCombat = false;

        // 事件
        public event Action<PetState> onStateChanged;
        public event Action<PetMood> onMoodChanged;

        /// <summary>
        /// 当前宠物状态
        /// </summary>
        public PetState currentState => _currentState;

        /// <summary>
        /// 当前心情数据
        /// </summary>
        public PetMood mood => _mood;

        #region IModule Members
        public string GetBonus(string bonusType)
        {
            // 宠物模块不提供数值加成，保持纯情感陪伴定位
            return "0";
        }

        public void Tick(float deltaTime)
        {
            if (_player == null) return;

            // 1. 更新心情衰减
            _mood.Decay(deltaTime, _moodDecayRate);

            // 2. 检测玩家状态变化
            DetectPlayerStateChange();

            // 3. 状态计时器
            _stateTimer += deltaTime;

            // 4. 强制切换到Idle状态（防止状态卡住）
            if (_stateTimer > 10f && _currentState != PetState.Idle)
            {
                TriggerIdle();
            }

            // 5. 触发心情变化事件（节流）
            if (_stateTimer > 0.5f)
            {
                onMoodChanged?.Invoke(_mood);
            }
        }

        public void OnActivate()
        {
            Debug.Log("[PetCompanionModule] 随队宠物已激活");
        }

        public void OnDeactivate()
        {
            Debug.Log("[PetCompanionModule] 随队宠物已停用");
        }
        #endregion

        /// <summary>
        /// 初始化模块
        /// </summary>
        public void Initialize(PlayerActor player)
        {
            _player = player;
            _mood = new PetMood();
            _currentState = PetState.Idle;
            _stateTimer = 0f;
            _lastHPRatio = 1f;

            Debug.Log("[PetCompanionModule] 随队宠物初始化完成");
        }

        /// <summary>
        /// 获取心情数据
        /// </summary>
        public PetMood GetMood()
        {
            return _mood;
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public PetState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// 导出宠物数据到存档文件
        /// </summary>
        public PetSaveFile ExportToPetSaveFile()
        {
            return new PetSaveFile
            {
                happiness = _mood.happiness,
                excitement = _mood.excitement,
                sadness = _mood.sadness,
                currentState = _currentState.ToString(),
                isUnlocked = true
            };
        }

        /// <summary>
        /// 从存档文件恢复宠物数据
        /// </summary>
        public void ImportFromPetSaveFile(PetSaveFile saveFile)
        {
            if (saveFile == null) return;
            _mood.happiness = saveFile.happiness;
            _mood.excitement = saveFile.excitement;
            _mood.sadness = saveFile.sadness;

            if (System.Enum.TryParse<PetState>(saveFile.currentState, out var state))
            {
                _currentState = state;
            }
        }

        /// <summary>
        /// 触发开心状态
        /// </summary>
        public void TriggerHappy(float intensity = 1f)
        {
            _mood.happiness = Mathf.Clamp01(_mood.happiness + 0.3f * intensity);
            _mood.sadness = Mathf.Max(0f, _mood.sadness - 0.2f * intensity);
            _mood.excitement = Mathf.Min(1f, _mood.excitement + 0.2f * intensity);
            ChangeState(PetState.Happy);
        }

        /// <summary>
        /// 触发悲伤状态
        /// </summary>
        public void TriggerSad(float intensity = 1f)
        {
            _mood.sadness = Mathf.Clamp01(_mood.sadness + 0.4f * intensity);
            _mood.happiness = Mathf.Max(0f, _mood.happiness - 0.2f * intensity);
            _mood.excitement = Mathf.Max(0f, _mood.excitement - 0.1f * intensity);
            ChangeState(PetState.Sad);
        }

        /// <summary>
        /// 触发兴奋状态
        /// </summary>
        public void TriggerExcited(float intensity = 1f)
        {
            _mood.excitement = Mathf.Clamp01(_mood.excitement + 0.5f * intensity);
            _mood.happiness = Mathf.Clamp01(_mood.happiness + 0.2f * intensity);
            ChangeState(PetState.Excited);
        }

        /// <summary>
        /// 触发空闲状态
        /// </summary>
        public void TriggerIdle()
        {
            ChangeState(PetState.Idle);
        }

        /// <summary>
        /// 检测玩家状态变化并触发相应宠物状态
        /// </summary>
        private void DetectPlayerStateChange()
        {
            if (_player == null) return;

            // 检测血量变化
            float currentHpRatio = (float)_player.stats.currentHp / _player.stats.maxHp;

            // 血量突然降低 -> 悲伤
            if (currentHpRatio < _hpLowThreshold && _lastHPRatio >= _hpLowThreshold)
            {
                TriggerSad(0.8f);
            }

            // 血量恢复 -> 开心
            if (currentHpRatio > 0.8f && _lastHPRatio <= _hpLowThreshold)
            {
                TriggerHappy(0.5f);
            }

            _lastHPRatio = currentHpRatio;

            // 检测等级变化（进度达成）
            // 这个会在外部调用 TriggerExcited
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        private void ChangeState(PetState newState)
        {
            if (_currentState == newState) return;

            PetState oldState = _currentState;
            _currentState = newState;
            _stateTimer = 0f;

            Debug.Log($"[PetCompanionModule] 状态切换: {oldState} -> {newState}");
            onStateChanged?.Invoke(newState);
        }

        #region 外部触发接口（供其他模块调用）

        /// <summary>
        /// 获得奖励时调用
        /// </summary>
        public void OnRewardReceived(float rewardAmount)
        {
            float intensity = Mathf.Clamp01(rewardAmount / 100f);
            TriggerHappy(intensity + 0.3f);
        }

        /// <summary>
        /// 升级时调用
        /// </summary>
        public void OnLevelUp(int newLevel)
        {
            TriggerExcited(1f);
        }

        /// <summary>
        /// 旅行进度达成时调用
        /// </summary>
        public void OnTravelProgress(float progress)
        {
            if (progress >= 1f)
            {
                TriggerExcited(0.7f);
            }
        }

        /// <summary>
        /// 战斗胜利时调用
        /// </summary>
        public void OnCombatVictory()
        {
            TriggerHappy(0.6f);
        }

        /// <summary>
        /// 战斗失败时调用
        /// </summary>
        public void OnCombatDefeat()
        {
            TriggerSad(0.7f);
        }

        #endregion
    }
}