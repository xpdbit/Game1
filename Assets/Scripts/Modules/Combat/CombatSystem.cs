using System;
using System.Collections.Generic;
using UnityEngine;
using Game1;
using Game1.Core.EventBus;
using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat
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
        public System.Collections.Generic.List<CombatLogEntry> combatLog = new();
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
    /// 敌人生存数据
    /// </summary>
    [Serializable]
    public class EnemyCombatantData
    {
        public string name;
        public int hp;
        public int maxHp;
        public int armor;
        public int damage;
        public float critChance;
        public float critMultiplier;
        public float dodgeChance;

        public bool IsDead => hp <= 0;

        public void TakeDamage(int damage)
        {
            hp -= damage;
            if (hp < 0) hp = 0;
        }
    }

    /// <summary>
    /// 多敌战斗结果
    /// </summary>
    [Serializable]
    public class MultiEnemyCombatResult
    {
        public bool playerVictory;
        public int totalDamageDealt;
        public int playerDamageTaken;
        public int goldReward;
        public int expReward;
        public List<CombatLogEntry> combatLog = new();
        public List<EnemyKillInfo> kills = new();
        public List<MemberParticipation> memberParticipations = new();
        public string endMessage;
    }

    /// <summary>
    /// 敌人击杀信息
    /// </summary>
    [Serializable]
    public class EnemyKillInfo
    {
        public string name;
        public int roundKilled;
    }

    /// <summary>
    /// 战斗系统
    /// 处理回合制战斗计算
    /// 实现ICombatSystem接口以支持VContainer DI
    /// </summary>
    public class CombatSystem : ICombatSystem
    {
        #region Singleton
        private static CombatSystem _instance;
        public static CombatSystem instance => _instance ??= new CombatSystem();
        #endregion

        #region Multi-Enemy Combat

        /// <summary>
        /// 执行多敌战斗（使用状态机驱动）
        /// </summary>
        /// <param name="player">玩家数据</param>
        /// <param name="enemies">敌人列表</param>
        /// <param name="playerTeam">玩家队伍（可选，用于多目标战斗）</param>
        /// <returns>多敌战斗结果</returns>
        public MultiEnemyCombatResult ExecuteMultiEnemyCombat(
            PlayerActor player,
            List<EnemyCombatantData> enemies,
            List<TeamMemberData> playerTeam = null)
        {
            if (enemies == null || enemies.Count == 0)
            {
                return new MultiEnemyCombatResult { endMessage = "没有敌人" };
            }

            var aliveEnemies = enemies.FindAll(e => !e.IsDead);
            if (aliveEnemies.Count == 0)
            {
                return new MultiEnemyCombatResult { endMessage = "所有敌人都已死亡" };
            }

            // 创建状态机集成器
            var integration = new CombatStateMachineIntegration();

            // 订阅动画派发事件
            integration.OnAnimationDispatch += (log, isPlayerTurn) =>
            {
                CombatAnimationDispatcher.instance?.DispatchFromCombatLog(
                    log, player.actorName, log.defenderName, isPlayerTurn);
            };

            // 初始化战斗
            integration.Initialize(player, enemies, playerTeam);

            // 设置职业映射（用于计算职业加成）
            if (playerTeam != null && playerTeam.Count > 0)
            {
                var jobMapping = new Dictionary<string, JobType>();
                foreach (var member in playerTeam)
                {
                    jobMapping[member.name] = member.job;
                }
                integration.SetMemberJobMapping(jobMapping);
            }

            // 运行状态机直到战斗结束
            while (integration.Tick())
            {
                // 继续直到战斗结束
            }

            // 获取结果
            var result = integration.result;

            // 更新玩家HP
            player.stats.currentHp = integration.GetPlayerHp();

            // 发布金币变化事件
            if (result.playerVictory)
            {
                EventBus.instance?.Publish(Game1.Core.EventBus.EventType.GoldChanged, this, result.goldReward);
            }

            return result;
        }

        /// <summary>
        /// 获取下一个存活敌人索引
        /// </summary>
        private int GetNextAliveEnemyIndex(List<EnemyCombatantData> enemies, int currentIndex)
        {
            if (enemies.Count == 0) return -1;

            int nextIndex = (currentIndex + 1) % enemies.Count;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[nextIndex].IsDead)
                    return nextIndex;
                nextIndex = (nextIndex + 1) % enemies.Count;
            }

            return -1; // 没有存活敌人
        }

        /// <summary>
        /// 计算多敌战斗金币奖励
        /// </summary>
        private int CalculateMultiEnemyGoldReward(List<EnemyCombatantData> enemies)
        {
            int totalReward = 0;
            foreach (var enemy in enemies)
            {
                int baseReward = 10;
                int hpBonus = enemy.maxHp / 5;
                int armorBonus = enemy.armor * 2;
                int damageBonus = enemy.damage * 3;
                totalReward += baseReward + hpBonus + armorBonus + damageBonus;
            }
            return totalReward;
        }

        /// <summary>
        /// 从ActorTemplate创建敌人生存数据
        /// </summary>
        public EnemyCombatantData CreateEnemyFromTemplate(ActorTemplate template, int difficultyBonus = 0)
        {
            return new EnemyCombatantData
            {
                name = template.nameTextId,
                hp = template.maxHp + difficultyBonus,
                maxHp = template.maxHp + difficultyBonus,
                armor = template.defense,
                damage = template.attack,
                critChance = 0.05f,
                critMultiplier = 1.5f,
                dodgeChance = 0.05f
            };
        }

        #endregion

        /// <summary>
        /// 执行战斗
        /// </summary>
        /// <param name="player">玩家属性</param>
        /// <param name="enemyHp">敌人HP</param>
        /// <param name="enemyArmor">敌人护甲</param>
        /// <param name="enemyDamage">敌人攻击力</param>
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

            // 获取玩家暴击/闪避属性
            float playerCritChance = player.stats.critChance;
            float playerCritMultiplier = player.stats.critDamageMultiplier;
            float playerDodgeChance = player.stats.dodgeChance;

            // 获取敌人暴击/闪避属性（从敌人属性推算）
            float enemyCritChance = 0.05f;  // 敌人默认暴击率5%
            float enemyCritMultiplier = 1.5f; // 敌人暴击伤害150%
            float enemyDodgeChance = 0.05f; // 敌人默认闪避率5%

            while (playerHp > 0 && currentEnemyHp > 0 && round < maxRounds)
            {
                if (isPlayerTurn)
                {
                    // 检查敌人是否闪避
                    if (UnityEngine.Random.value < enemyDodgeChance)
                    {
                        result.combatLog.Add(new CombatLogEntry
                        {
                            round = round + 1,
                            attackerName = player.actorName,
                            defenderName = enemyName,
                            damageDealt = 0,
                            defenderHpAfter = currentEnemyHp,
                            wasCritical = false
                        });
                    }
                    else
                    {
                        // 玩家攻击敌人
                        int damageToEnemy = CalculateDamage(playerDamage, enemyArmor, playerCritChance, playerCritMultiplier, out bool isCrit);

                        if (isCrit)
                        {
                            damageToEnemy = Mathf.FloorToInt(damageToEnemy * playerCritMultiplier);
                        }

                        currentEnemyHp -= damageToEnemy;
                        if (currentEnemyHp < 0) currentEnemyHp = 0;

                        result.combatLog.Add(new CombatLogEntry
                        {
                            round = round + 1,
                            attackerName = player.actorName,
                            defenderName = enemyName,
                            damageDealt = damageToEnemy,
                            defenderHpAfter = currentEnemyHp,
                            wasCritical = isCrit
                        });
                    }
                }
                else
                {
                    // 检查玩家是否闪避
                    if (UnityEngine.Random.value < playerDodgeChance)
                    {
                        result.combatLog.Add(new CombatLogEntry
                        {
                            round = round + 1,
                            attackerName = enemyName,
                            defenderName = player.actorName,
                            damageDealt = 0,
                            defenderHpAfter = playerHp,
                            wasCritical = false
                        });
                    }
                    else
                    {
                        // 敌人攻击玩家
                        int damageToPlayer = CalculateDamage(enemyDamage, playerArmor, enemyCritChance, enemyCritMultiplier, out bool isCrit);

                        if (isCrit)
                        {
                            damageToPlayer = Mathf.FloorToInt(damageToPlayer * enemyCritMultiplier);
                        }

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
                            wasCritical = isCrit
                        });
                    }
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
                EventBus.instance?.Publish(Game1.Core.EventBus.EventType.GoldChanged, this, result.goldReward);
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
        /// 计算伤害（委派到共享的DamageCalculator）
        /// 公式：damage = max(1, floor(attack * (1 - defense / (defense + 100))))
        /// </summary>
        public int CalculateDamage(int attack, int defense, float critChance, float critMultiplier, out bool isCrit)
        {
            return DamageCalculator.CalculateDamage(attack, defense, critChance, critMultiplier, out isCrit);
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

        // ICombatSystem explicit implementation for interface compliance
        int ICombatSystem.CalculateDamage(int attack, int defense)
        {
            return CalculateDamage(attack, defense, 0.1f, 1.5f, out _);
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

            // 考虑暴击率加成（暴击伤害*暴击率）
            float playerCritBonus = 1f + (0.5f * 0.1f); // 假设10%暴击率，150%伤害
            float enemyCritBonus = 1f + (0.5f * 0.05f);  // 假设5%暴击率

            return playerEffectiveHp * playerCritBonus > enemyEffectiveHp * enemyCritBonus * 0.8f;  // 80%阈值
        }
    }

    /// <summary>
    /// 战斗事件扩展
    /// 用于在事件系统中触发战斗
    /// 使用virtual/override体系保证多态正确工作
    /// </summary>
    [Serializable]
    public class CombatEventEx : CombatEvent
    {
        public NPCTemplate npcTemplate;  // NPC模板（如果有）
        public string npcInstanceId;       // NPC实例ID

        /// <summary>
        /// 执行战斗（使用新战斗系统）
        /// </summary>
        public override EventResult Execute()
        {
            // 获取玩家数据
            var player = GameMain.instance?.GetPlayerActor();
            if (player == null)
            {
                return new EventResult
                {
                    success = false,
                    message = "玩家数据不存在"
                };
            }

            // 从 ActorManager 获取敌人属性
            int enemyHp = enemyStrength;
            int enemyArmor = 5;
            int enemyDamage = 3;
            string enemyName = $"敌人(enemyCount:{enemyCount})";

            // 如果有NPC模板，使用NPC属性
            if (npcTemplate != null)
            {
                enemyHp = npcTemplate.baseHp;
                enemyArmor = npcTemplate.baseArmor;
                enemyDamage = npcTemplate.baseDamage;
                enemyName = npcTemplate.nameId ?? enemyName;
            }
            else
            {
                // 通过 ActorManager 查找敌对阵营模板
                var hostileTemplate = ActorManager.GetHostileTemplate(enemyCount);
                if (hostileTemplate != null)
                {
                    enemyHp = hostileTemplate.maxHp + enemyStrength / 10;
                    enemyArmor = hostileTemplate.defense;
                    enemyDamage = hostileTemplate.attack;
                    enemyName = hostileTemplate.nameTextId ?? enemyName;
                }
            }

            var combatResult = CombatSystem.instance.ExecuteCombat(
                player,
                enemyHp,
                enemyArmor,
                enemyDamage,
                enemyName
            );

            var result = new EventResult
            {
                success = combatResult.playerVictory,
                playerVictory = combatResult.playerVictory,
                goldReward = combatResult.goldReward,
                message = combatResult.endMessage,
                combatLog = combatResult.combatLog
            };

            // 从战斗日志派发动画事件
            if (combatResult.combatLog != null)
            {
                foreach (var log in combatResult.combatLog)
                {
                    bool isPlayerTurn = (log.round % 2) == 1;
                    CombatAnimationDispatcher.instance?.DispatchFromCombatLog(log, player.actorName, enemyName, isPlayerTurn);
                }
            }

            // 派发战斗结果事件
            CombatAnimationDispatcher.instance?.DispatchResult(combatResult.playerVictory);

            return result;
        }
    }
}