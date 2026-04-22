using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 战斗结果
    /// </summary>
    [Serializable]
    public class CombatResult
    {
        public bool playerVictory;
        public int playerDamageTaken;
        public int enemyDamageDealt;
        public int goldReward;
        public int expReward;
        public List<CombatLogEntry> combatLog = new();
        public string endMessage;
    }

    /// <summary>
    /// 战斗日志条目
    /// </summary>
    [Serializable]
    public class CombatLogEntry
    {
        public int round;
        public string attackerName;
        public string defenderName;
        public int damageDealt;
        public int defenderHpAfter;
        public bool wasCritical;
    }

    /// <summary>
    /// 战斗者接口
    /// </summary>
    public interface ICombatant
    {
        string combatantName { get; }
        int currentHp { get; }
        int maxHp { get; }
        int armor { get; }
        int damage { get; }
        bool IsDead { get; }
        void TakeDamage(int damage);
        void Heal(int amount);
    }

    /// <summary>
    /// 战斗系统
    /// 处理回合制战斗计算
    /// </summary>
    public class CombatSystem
    {
        #region Singleton
        private static CombatSystem _instance;
        public static CombatSystem instance => _instance ??= new CombatSystem();
        #endregion

        /// <summary>
        /// 执行战斗
        /// </summary>
        /// <param name="player">玩家属性</param>
        /// <param name="enemy">敌人属性</param>
        /// <param name="enemyName">敌人名称</param>
        /// <returns>战斗结果</returns>
        public CombatResult ExecuteCombat(PlayerActor player, int enemyHp, int enemyArmor, int enemyDamage, string enemyName = "敌人")
        {
            var result = new CombatResult();
            int playerHp = player.stats.currentHp;
            int playerMaxHp = player.stats.maxHp;
            int playerArmor = player.stats.defense;
            int playerDamage = player.stats.attack;

            int currentEnemyHp = enemyHp;
            int round = 0;
            int maxRounds = 20;  // 防止无限循环

            result.combatLog = new List<CombatLogEntry>();

            // 玩家先攻
            bool isPlayerTurn = true;

            while (playerHp > 0 && currentEnemyHp > 0 && round < maxRounds)
            {
                if (isPlayerTurn)
                {
                    // 玩家攻击敌人
                    int damageToEnemy = CalculateDamage(playerDamage, enemyArmor);
                    currentEnemyHp -= damageToEnemy;
                    if (currentEnemyHp < 0) currentEnemyHp = 0;

                    result.combatLog.Add(new CombatLogEntry
                    {
                        round = round + 1,
                        attackerName = player.actorName,
                        defenderName = enemyName,
                        damageDealt = damageToEnemy,
                        defenderHpAfter = currentEnemyHp,
                        wasCritical = false
                    });
                }
                else
                {
                    // 敌人攻击玩家
                    int damageToPlayer = CalculateDamage(enemyDamage, playerArmor);
                    playerHp -= damageToPlayer;
                    if (playerHp < 0) playerHp = 0;

                    result.playerDamageTaken += damageToPlayer;
                    
                    result.combatLog.Add(new CombatLogEntry
                    {
                        round = round + 1,
                        attackerName = enemyName,
                        defenderName = player.actorName,
                        damageDealt = damageToPlayer,
                        defenderHpAfter = playerHp,
                        wasCritical = false
                    });
                }

                isPlayerTurn = !isPlayerTurn;
                round++;
            }

            // 判断胜负
            result.playerVictory = currentEnemyHp <= 0 && playerHp > 0;

            // 更新玩家HP
            player.stats.currentHp = playerHp;

            // 计算奖励
            if (result.playerVictory)
            {
                result.goldReward = CalculateGoldReward(enemyHp, enemyArmor, enemyDamage);
                result.endMessage = $"击败了{enemyName}！获得{result.goldReward}金币。";
            }
            else if (playerHp <= 0)
            {
                result.endMessage = $"被{enemyName}击败了...";
            }
            else
            {
                result.endMessage = "战斗陷入僵局...";
            }

            return result;
        }

        /// <summary>
        /// 计算伤害（伤害 = 攻击方攻击力 - 防御方护甲，最小为1）
        /// </summary>
        public int CalculateDamage(int attack, int defense)
        {
            return Mathf.Max(1, attack - defense);
        }

        /// <summary>
        /// 计算金币奖励
        /// </summary>
        private int CalculateGoldReward(int hp, int armor, int damage)
        {
            // 基础奖励 + 属性加成
            int baseReward = 10;
            int hpBonus = hp / 5;
            int armorBonus = armor * 2;
            int damageBonus = damage * 3;

            return baseReward + hpBonus + armorBonus + damageBonus;
        }

        /// <summary>
        /// 快速计算能否战胜
        /// </summary>
        public bool CanVictory(int playerDamage, int playerArmor, int playerHp, int enemyDamage, int enemyArmor, int enemyHp)
        {
            // 粗略估算：比较DPS
            float playerDps = (float)playerDamage / Mathf.Max(1, enemyArmor);
            float enemyDps = (float)enemyDamage / Mathf.Max(1, playerArmor);

            // 考虑HP因素
            float playerEffectiveHp = playerHp / Mathf.Max(0.1f, enemyDps);
            float enemyEffectiveHp = enemyHp / Mathf.Max(0.1f, playerDps);

            return playerEffectiveHp > enemyEffectiveHp * 0.8f;  // 80%阈值
        }
    }

    /// <summary>
    /// 战斗事件扩展
    /// 用于在事件系统中触发战斗
    /// </summary>
    [Serializable]
    public class CombatEventEx : CombatEvent
    {
        public NPCTemplate npcTemplate;  // NPC模板（如果有）
        public string npcInstanceId;       // NPC实例ID

        /// <summary>
        /// 执行战斗（使用新战斗系统）
        /// </summary>
        public new EventResult Execute()
        {
            var result = new EventResult();

            // 获取玩家数据（这里需要从GameMain获取）
            var player = GameMain.instance?.playerActor;
            if (player == null)
            {
                result.success = false;
                result.message = "玩家数据不存在";
                return result;
            }

            // 执行战斗
            // 敌人属性：HP = enemyStrength, ARMOR = 5, DAMAGE = 3 (每角色20HP/5ARMOR/3DAMAGE)
            var combatResult = CombatSystem.instance.ExecuteCombat(
                player,
                enemyStrength,  // HP = enemyStrength
                5,              // 敌人护甲
                3,              // 敌人伤害
                $"敌人(enemyCount:{enemyCount})"
            );

            result.success = combatResult.playerVictory;
            result.goldReward = combatResult.goldReward;
            result.message = combatResult.endMessage;

            return result;
        }
    }
}