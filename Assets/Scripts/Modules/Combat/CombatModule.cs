using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 战斗模块接口
    /// 用于将战斗系统集成到PlayerActor的模块系统中
    /// </summary>
    public interface ICombatModule : IModule
    {
        /// <summary>
        /// 执行单目标战斗
        /// </summary>
        CombatResult ExecuteCombat(PlayerActor player, int enemyHp, int enemyArmor, int enemyDamage, string enemyName = "敌人");

        /// <summary>
        /// 执行多目标战斗
        /// </summary>
        MultiEnemyCombatResult ExecuteMultiEnemyCombat(PlayerActor player, List<EnemyCombatantData> enemies, List<TeamMemberData> playerTeam = null);

        /// <summary>
        /// 快速计算能否战胜
        /// </summary>
        bool CanVictory(int playerDamage, int playerArmor, int playerHp, int enemyDamage, int enemyArmor, int enemyHp);

        /// <summary>
        /// 获取当前战斗统计
        /// </summary>
        CombatStatistics GetStatistics();
    }

    /// <summary>
    /// 战斗统计
    /// </summary>
    [Serializable]
    public class CombatStatistics
    {
        public int totalBattles;
        public int victories;
        public int defeats;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalGoldEarned;

        public float winRate => totalBattles > 0 ? (float)victories / totalBattles : 0f;
    }

    /// <summary>
    /// 战斗模块
    /// 实现IModule接口，将CombatSystem封装为可热插拔的模块
    /// </summary>
    [Serializable]
    public class CombatModule : ICombatModule
    {
        #region IModule Implementation
        public string moduleId => "combat";
        public string moduleName => "战斗系统";
        #endregion

        #region Configuration
        [SerializeField] private float _critBonusMultiplier = 1.0f;   // 暴击加成倍率
        [SerializeField] private float _damageBonusMultiplier = 1.0f; // 伤害加成倍率
        [SerializeField] private float _defenseBonusMultiplier = 1.0f; // 防御加成倍率
        #endregion

        #region Private Fields
        private PlayerActor _player;
        private CombatStatistics _statistics = new();
        private bool _isActive = true;
        #endregion

        #region Properties
        public CombatStatistics statistics => _statistics;
        public bool isActive => _isActive;
        #endregion

        /// <summary>
        /// 初始化模块
        /// </summary>
        public void Initialize(PlayerActor player)
        {
            _player = player;
            ResetStatistics();
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public void ResetStatistics()
        {
            _statistics = new CombatStatistics();
        }

        #region IModule Members

        /// <summary>
        /// 获取战斗相关加成
        /// </summary>
        public string GetBonus(string bonusType)
        {
            switch (bonusType)
            {
                case "combat_crit":
                    return _critBonusMultiplier.ToString();
                case "combat_damage":
                    return _damageBonusMultiplier.ToString();
                case "combat_defense":
                    return _defenseBonusMultiplier.ToString();
                case "combat_rate":
                    // 返回战斗相关的基础值，不包含加成（避免递归）
                    return "1.0";
                default:
                    return "0";
            }
        }

        /// <summary>
        /// Tick - 战斗系统不需要每帧Tick，但实现接口
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 战斗系统不需要每帧更新，主要在事件触发时工作
        }

        /// <summary>
        /// 激活模块
        /// </summary>
        public void OnActivate()
        {
            _isActive = true;
            Debug.Log("[CombatModule] Activated");
        }

        /// <summary>
        /// 停用模块
        /// </summary>
        public void OnDeactivate()
        {
            _isActive = false;
            Debug.Log("[CombatModule] Deactivated");
        }

        #endregion

        #region Combat API

        /// <summary>
        /// 执行单目标战斗（应用加成）
        /// </summary>
        public CombatResult ExecuteCombat(PlayerActor player, int enemyHp, int enemyArmor, int enemyDamage, string enemyName = "敌人")
        {
            if (!_isActive || player == null)
            {
                return new CombatResult
                {
                    endMessage = "战斗系统未激活或玩家数据无效"
                };
            }

            // 应用玩家加成
            float damageBonus = 1f + player.GetTotalBonus("combat_damage");
            float defenseBonus = 1f + player.GetTotalBonus("combat_defense");
            float critBonus = 1f + player.GetTotalBonus("combat_crit");

            // 应用卡牌加成
            var cardBonus = CardDesign.instance.GetCombatBonus();
            damageBonus += cardBonus.damageMultiplier;
            defenseBonus += cardBonus.defenseMultiplier;
            critBonus += cardBonus.critMultiplier;

            // 调整玩家属性（战斗系统内部会进一步处理暴击）
            int adjustedDamage = Mathf.RoundToInt(player.stats.attack * damageBonus);
            int adjustedArmor = Mathf.RoundToInt(player.stats.defense * defenseBonus);

            // 临时调整玩家属性以应用加成
            var originalAttack = player.stats.attack;
            var originalDefense = player.stats.defense;
            var originalCrit = player.stats.critChance;
            var originalCritMult = player.stats.critDamageMultiplier;

            player.stats.attack = adjustedDamage;
            player.stats.defense = adjustedArmor;
            player.stats.critChance *= critBonus;

            // 执行战斗
            var result = CombatSystem.instance.ExecuteCombat(
                player, enemyHp, enemyArmor, enemyDamage, enemyName);

            // 恢复原始属性
            player.stats.attack = originalAttack;
            player.stats.defense = originalDefense;
            player.stats.critChance = originalCrit;
            player.stats.critDamageMultiplier = originalCritMult;

            // 更新统计
            UpdateStatistics(result);

            return result;
        }

        /// <summary>
        /// 执行多目标战斗（应用加成）
        /// </summary>
        public MultiEnemyCombatResult ExecuteMultiEnemyCombat(
            PlayerActor player,
            List<EnemyCombatantData> enemies,
            List<TeamMemberData> playerTeam = null)
        {
            if (!_isActive || player == null)
            {
                return new MultiEnemyCombatResult
                {
                    endMessage = "战斗系统未激活或玩家数据无效"
                };
            }

            // 应用玩家加成
            float damageBonus = 1f + player.GetTotalBonus("combat_damage");
            float defenseBonus = 1f + player.GetTotalBonus("combat_defense");
            float critBonus = 1f + player.GetTotalBonus("combat_crit");

            // 临时调整玩家属性以应用加成
            var originalAttack = player.stats.attack;
            var originalDefense = player.stats.defense;
            var originalCrit = player.stats.critChance;
            var originalCritMult = player.stats.critDamageMultiplier;

            player.stats.attack = Mathf.RoundToInt(player.stats.attack * damageBonus);
            player.stats.defense = Mathf.RoundToInt(player.stats.defense * defenseBonus);
            player.stats.critChance *= critBonus;

            // 执行多目标战斗
            var result = CombatSystem.instance.ExecuteMultiEnemyCombat(player, enemies, playerTeam);

            // 恢复原始属性
            player.stats.attack = originalAttack;
            player.stats.defense = originalDefense;
            player.stats.critChance = originalCrit;
            player.stats.critDamageMultiplier = originalCritMult;

            // 更新统计
            UpdateMultiEnemyStatistics(result);

            return result;
        }

        /// <summary>
        /// 快速计算能否战胜（考虑加成）
        /// </summary>
        public bool CanVictory(int playerDamage, int playerArmor, int playerHp, int enemyDamage, int enemyArmor, int enemyHp)
        {
            if (_player == null) return false;

            float damageBonus = 1f + _player.GetTotalBonus("combat_damage");
            float defenseBonus = 1f + _player.GetTotalBonus("combat_defense");

            int adjustedPlayerDamage = Mathf.RoundToInt(playerDamage * damageBonus);
            int adjustedPlayerArmor = Mathf.RoundToInt(playerArmor * defenseBonus);

            return CombatSystem.instance.CanVictory(
                adjustedPlayerDamage, adjustedPlayerArmor, playerHp,
                enemyDamage, enemyArmor, enemyHp);
        }

        /// <summary>
        /// 获取战斗统计
        /// </summary>
        public CombatStatistics GetStatistics()
        {
            return _statistics;
        }

        #endregion

        #region Statistics

        private void UpdateStatistics(CombatResult result)
        {
            _statistics.totalBattles++;
            _statistics.totalDamageDealt += result.enemyDamageDealt;
            _statistics.totalDamageTaken += result.playerDamageTaken;
            _statistics.totalGoldEarned += result.goldReward;

            if (result.playerVictory)
            {
                _statistics.victories++;
            }
            else
            {
                _statistics.defeats++;
            }
        }

        private void UpdateMultiEnemyStatistics(MultiEnemyCombatResult result)
        {
            _statistics.totalBattles++;
            _statistics.totalDamageDealt += result.totalDamageDealt;
            _statistics.totalDamageTaken += result.playerDamageTaken;
            _statistics.totalGoldEarned += result.goldReward;

            if (result.playerVictory)
            {
                _statistics.victories++;
            }
            else
            {
                _statistics.defeats++;
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// 设置加成倍率（用于技能/装备效果）
        /// </summary>
        public void SetBonusMultipliers(float critBonus, float damageBonus, float defenseBonus)
        {
            _critBonusMultiplier = critBonus;
            _damageBonusMultiplier = damageBonus;
            _defenseBonusMultiplier = defenseBonus;
        }

        /// <summary>
        /// 应用临时加成（如buff效果）
        /// </summary>
        public void ApplyTemporaryBonus(float duration, float critBonus, float damageBonus, float defenseBonus)
        {
            // TODO: 实现临时加成逻辑，可以使用计时器在Tick中检查
            Debug.Log($"[CombatModule] Applied temporary bonus for {duration}s");
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 导出战斗统计到存档数据
        /// </summary>
        /// <returns>战斗存档数据</returns>
        public CombatSaveData Export()
        {
            return new CombatSaveData(_statistics);
        }

        /// <summary>
        /// 从存档数据导入战斗统计
        /// </summary>
        /// <param name="saveData">战斗存档数据</param>
        public void Import(CombatSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[CombatModule] Import: saveData is null, resetting statistics");
                ResetStatistics();
                return;
            }

            _statistics = saveData.ToStatistics();
            Debug.Log($"[CombatModule] Imported combat statistics: battles={_statistics.totalBattles}, victories={_statistics.victories}, winRate={_statistics.winRate:P1}");
        }

        #endregion
    }
}