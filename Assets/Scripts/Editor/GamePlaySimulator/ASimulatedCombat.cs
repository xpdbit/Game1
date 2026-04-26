using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Enemy combatant data for simulation
    /// </summary>
    [Serializable]
    public class AEnemyData
    {
        public string id;
        public string name;
        public int hp;
        public int maxHp;
        public int armor;
        public int damage;
        public float critChance;
        public float dodgeChance;
        public float critMultiplier = 1.5f;
        public int goldReward;
        public int expReward;
        public bool isBoss;

        public AEnemyData()
        {
            id = Guid.NewGuid().ToString();
            name = "敌人";
        }

        public bool IsDead => hp <= 0;

        public void TakeDamage(int damage)
        {
            hp = Mathf.Max(0, hp - damage);
        }

        public void Reset()
        {
            hp = maxHp;
        }
    }

    /// <summary>
    /// Combat result data
    /// </summary>
    [Serializable]
    public class ACombatResult
    {
        public bool playerVictory;
        public int playerDamageDealt;
        public int playerDamageTaken;
        public int goldReward;
        public int expReward;
        public int playerRemainingHp;
        public int enemyRemainingHp;
        public List<string> combatLog = new();
        public string endMessage;
        public List<bool> playerCrits = new();
        public List<bool> enemyCrits = new();
        public List<int> playerDamages = new();
        public List<int> enemyDamages = new();
    }

    /// <summary>
    /// Multi-enemy combat result
    /// </summary>
    [Serializable]
    public class AMultiEnemyCombatResult
    {
        public bool playerVictory;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int goldReward;
        public int expReward;
        public int playerRemainingHp;
        public List<int> enemyRemainingHps = new();
        public List<string> combatLog = new();
        public string endMessage;
    }

    /// <summary>
    /// Combat statistics for simulation
    /// </summary>
    [Serializable]
    public class ACombatStatistics
    {
        public int totalBattles;
        public int victories;
        public int defeats;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalGoldEarned;
        public int totalExpEarned;

        public float winRate => totalBattles > 0 ? (float)victories / totalBattles : 0f;
    }

    /// <summary>
    /// Combat simulation system (mirrors CombatSystem logic)
    /// </summary>
    public class ASimulatedCombat
    {
        private ACombatStatistics _statistics = new();
        private System.Random _rng;
        private int _seed;

        public ASimulatedCombat() : this(Environment.TickCount) { }

        public ASimulatedCombat(int seed)
        {
            _seed = seed;
            _rng = new System.Random(seed);
        }

        #region Single Target Combat
        /// <summary>
        /// Execute single target combat (mirrors CombatSystem.ExecuteCombat)
        /// </summary>
        public ACombatResult Execute(ASimulatedPlayer player, AEnemyData enemy)
        {
            var result = new ACombatResult();

            // Initialize RNG with seed for determinism
            _rng = new System.Random(_seed);

            enemy.Reset();
            result.playerRemainingHp = player.stats.currentHp;
            result.enemyRemainingHp = enemy.hp;

            result.combatLog.Add($"=== 战斗开始: {player.actorName} vs {enemy.name} ===");
            result.combatLog.Add($"{enemy.name}: HP={enemy.hp}, Armor={enemy.armor}, Damage={enemy.damage}");

            int turn = 1;
            bool playerTurn = true;

            while (!player.IsDead && !enemy.IsDead && turn <= 100)
            {
                if (playerTurn)
                {
                    // Player attacks
                    int damage = CalculateDamage(player, enemy, result);
                    enemy.TakeDamage(damage);
                    result.playerDamageDealt += damage;
                    result.playerDamages.Add(damage);
                    result.enemyRemainingHp = enemy.hp;

                    result.combatLog.Add($"[{turn}] {player.actorName} 攻击! 造成 {damage} 伤害 ({(result.playerCrits[^1] ? "暴击!" : "普通")})");
                }
                else
                {
                    // Enemy attacks
                    int damage = CalculateEnemyDamage(enemy, player, result);
                    player.TakeDamage(damage);
                    result.playerDamageTaken += damage;
                    result.enemyDamages.Add(damage);
                    result.playerRemainingHp = player.stats.currentHp;

                    if (!player.IsDead)
                        result.combatLog.Add($"[{turn}] {enemy.name} 攻击! 造成 {damage} 伤害 ({(result.enemyCrits[^1] ? "暴击!" : "普通")})");
                    else
                        result.combatLog.Add($"[{turn}] {enemy.name} 攻击! {player.actorName} 被击败!");
                }

                playerTurn = !playerTurn;
                turn++;
            }

            // Determine winner
            if (enemy.IsDead)
            {
                result.playerVictory = true;
                result.goldReward = CalculateGoldReward(enemy);
                result.expReward = enemy.expReward;
                player.AddGold(result.goldReward);
                player.AddExp(result.expReward);
                result.endMessage = $"{enemy.name} 被击败! 获得 {result.goldReward} 金币, {result.expReward} 经验";
            }
            else
            {
                result.playerVictory = false;
                result.endMessage = $"{player.actorName} 被打败了...";
            }

            result.combatLog.Add($"=== 战斗结束 ===");
            result.combatLog.Add(result.endMessage);

            UpdateStatistics(result);
            return result;
        }

        private int CalculateDamage(ASimulatedPlayer player, AEnemyData enemy, ACombatResult result)
        {
            bool isCrit = _rng.NextDouble() < player.stats.critChance;
            result.playerCrits.Add(isCrit);

            int baseDamage = player.stats.attack;
            int damage = isCrit
                ? Mathf.RoundToInt(baseDamage * player.stats.critDamageMultiplier)
                : baseDamage;

            // Armor reduction
            damage = Mathf.Max(1, damage - enemy.armor);
            return damage;
        }

        private int CalculateEnemyDamage(AEnemyData enemy, ASimulatedPlayer player, ACombatResult result)
        {
            bool isCrit = _rng.NextDouble() < enemy.critChance;
            result.enemyCrits.Add(isCrit);

            int baseDamage = enemy.damage;
            int damage = isCrit
                ? Mathf.RoundToInt(baseDamage * enemy.critMultiplier)
                : baseDamage;

            // Player armor reduction
            damage = Mathf.Max(1, damage - player.stats.defense);
            return damage;
        }

        private int CalculateGoldReward(AEnemyData enemy)
        {
            var config = AConfig.Active;
            return config.baseGoldReward
                + Mathf.RoundToInt(enemy.maxHp * config.goldRewardPerEnemyHp)
                + Mathf.RoundToInt(enemy.armor * config.goldRewardPerEnemyArmor)
                + Mathf.RoundToInt(enemy.damage * config.goldRewardPerEnemyDamage);
        }
        #endregion

        #region Multi-Enemy Combat
        /// <summary>
        /// Execute multi-enemy combat (mirrors CombatSystem.ExecuteMultiEnemyCombat)
        /// </summary>
        public AMultiEnemyCombatResult ExecuteMulti(ASimulatedPlayer player, List<AEnemyData> enemies)
        {
            var result = new AMultiEnemyCombatResult();

            _rng = new System.Random(_seed);

            foreach (var enemy in enemies)
                enemy.Reset();

            result.playerRemainingHp = player.stats.currentHp;
            result.combatLog.Add($"=== 多目标战斗开始: {player.actorName} vs {enemies.Count} 敌人 ===");

            int turn = 1;
            bool playerTurn = true;
            int activeEnemyIndex = 0;

            while (!player.IsDead && enemies.Exists(e => !e.IsDead) && turn <= 200)
            {
                if (playerTurn)
                {
                    // Player attacks one enemy
                    var target = enemies[activeEnemyIndex];
                    if (!target.IsDead)
                    {
                        bool isCrit;
                        int damage = player.CalculateDamage(out isCrit);
                        damage = Mathf.Max(1, damage - target.armor);
                        target.TakeDamage(damage);
                        result.totalDamageDealt += damage;

                        result.combatLog.Add($"[{turn}] {player.actorName} 攻击 {target.name}! 造成 {damage} 伤害");
                    }

                    // Find next alive enemy
                    activeEnemyIndex = (activeEnemyIndex + 1) % enemies.Count;
                    while (enemies[activeEnemyIndex].IsDead && enemies.Exists(e => !e.IsDead))
                        activeEnemyIndex = (activeEnemyIndex + 1) % enemies.Count;
                }
                else
                {
                    // Each alive enemy attacks
                    foreach (var enemy in enemies)
                    {
                        if (!enemy.IsDead)
                        {
                            bool isCrit = _rng.NextDouble() < enemy.critChance;
                            int damage = isCrit
                                ? Mathf.RoundToInt(enemy.damage * enemy.critMultiplier)
                                : enemy.damage;
                            damage = Mathf.Max(1, damage - player.stats.defense);

                            player.TakeDamage(damage);
                            result.totalDamageTaken += damage;

                            result.combatLog.Add($"[{turn}] {enemy.name} 攻击 {player.actorName}! 造成 {damage} 伤害");
                        }
                    }
                    result.playerRemainingHp = player.stats.currentHp;
                }

                playerTurn = !playerTurn;
                turn++;
            }

            // Determine result
            if (player.IsDead)
            {
                result.playerVictory = false;
                result.endMessage = $"{player.actorName} 被击败了...";
            }
            else
            {
                result.playerVictory = true;
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsDead)
                        result.goldReward += CalculateGoldReward(enemy);
                }
                player.AddGold(result.goldReward);
                result.endMessage = $"胜利! 获得 {result.goldReward} 金币";
            }

            foreach (var enemy in enemies)
                result.enemyRemainingHps.Add(enemy.hp);

            result.combatLog.Add(result.endMessage);
            return result;
        }
        #endregion

        #region Quick Calculations
        /// <summary>
        /// Quick victory check (mirrors CombatSystem.CanVictory)
        /// </summary>
        public bool CanVictory(int playerDamage, int playerArmor, int playerHp,
                                int enemyDamage, int enemyArmor, int enemyHp)
        {
            // Simplified calculation - estimate turns to kill
            int playerDps = Mathf.Max(1, playerDamage - enemyArmor);
            int enemyDps = Mathf.Max(1, enemyDamage - playerArmor);

            int turnsToKillEnemy = Mathf.CeilToInt((float)enemyHp / playerDps);
            int turnsToKillPlayer = Mathf.CeilToInt((float)playerHp / enemyDps);

            return turnsToKillEnemy <= turnsToKillPlayer;
        }

        /// <summary>
        /// Estimate win rate based on stats
        /// </summary>
        public float EstimateWinRate(ASimulatedPlayer player, AEnemyData enemy)
        {
            int playerDps = Mathf.Max(1, player.stats.attack - enemy.armor);
            int enemyDps = Mathf.Max(1, enemy.damage - player.stats.defense);

            int turnsToKillEnemy = Mathf.CeilToInt((float)enemy.hp / playerDps);
            int turnsToKillPlayer = Mathf.CeilToInt((float)player.stats.currentHp / enemyDps);

            // Factor in crit/dodge
            float playerEffectiveDps = playerDps * (1 + player.stats.critChance * (player.stats.critDamageMultiplier - 1));
            float enemyEffectiveDps = enemyDps * (1 + enemy.critChance * (enemy.critMultiplier - 1));

            turnsToKillEnemy = Mathf.CeilToInt((float)enemy.hp / playerEffectiveDps);
            turnsToKillPlayer = Mathf.CeilToInt((float)player.stats.currentHp / enemyEffectiveDps);

            // Calculate probability
            if (turnsToKillEnemy <= turnsToKillPlayer)
                return 0.5f + 0.05f * (turnsToKillPlayer - turnsToKillEnemy);
            else
                return 0.5f - 0.05f * (turnsToKillEnemy - turnsToKillPlayer);
        }
        #endregion

        #region Enemy Generation
        /// <summary>
        /// Generate random enemy based on player level
        /// </summary>
        public AEnemyData GenerateEnemy(int playerLevel, int seed)
        {
            _rng = new System.Random(seed);
            var config = AConfig.Active;

            float levelScale = 1f + (playerLevel - 1) * 0.15f;
            bool isBoss = _rng.NextDouble() < 0.1f;

            var enemy = new AEnemyData
            {
                name = isBoss ? $"Boss_{seed}" : $"敌人_{seed}",
                maxHp = Mathf.RoundToInt((15 + _rng.Next(0, 10)) * levelScale * (isBoss ? 2f : 1f)),
                hp = 0, // Will be reset
                armor = Mathf.RoundToInt((3 + _rng.Next(0, 5)) * levelScale * (isBoss ? 1.5f : 1f)),
                damage = Mathf.RoundToInt((2 + _rng.Next(0, 4)) * levelScale * (isBoss ? 1.5f : 1f)),
                critChance = 0.05f + _rng.Next(0, 5) * 0.02f,
                dodgeChance = 0.03f + _rng.Next(0, 3) * 0.02f,
                goldReward = Mathf.RoundToInt((5 + playerLevel * 2) * (isBoss ? 5f : 1f)),
                expReward = Mathf.RoundToInt((3 + playerLevel) * (isBoss ? 3f : 1f)),
                isBoss = isBoss
            };
            enemy.hp = enemy.maxHp;

            return enemy;
        }
        #endregion

        #region Statistics
        private void UpdateStatistics(ACombatResult result)
        {
            _statistics.totalBattles++;
            _statistics.totalDamageDealt += result.playerDamageDealt;
            _statistics.totalDamageTaken += result.playerDamageTaken;
            _statistics.totalGoldEarned += result.goldReward;
            _statistics.totalExpEarned += result.expReward;

            if (result.playerVictory)
                _statistics.victories++;
            else
                _statistics.defeats++;
        }

        public ACombatStatistics GetStatistics() => _statistics;

        public void ResetStatistics()
        {
            _statistics = new ACombatStatistics();
        }
        #endregion
    }
}