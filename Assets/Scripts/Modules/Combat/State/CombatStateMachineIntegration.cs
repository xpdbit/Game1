using System;
using System.Collections.Generic;
using UnityEngine;
using Game1.Modules.Combat;
using Game1.Modules.Combat.Commands;

namespace Game1.Modules.Combat.State
{
    /// <summary>
    /// 战斗状态机集成器
    /// 桥接CombatStateMachine和CombatSystem，使用状态机驱动战斗流程
    /// 支持队伍战斗，多个队友参与
    /// </summary>
    public class CombatStateMachineIntegration
    {
        private const string LOG_PREFIX = "[CombatStateMachineIntegration]";
        private const float HEALER_HEAL_RATIO = 0.2f;
        private const int HEALER_HEAL_INTERVAL = 3;

        private readonly CombatStateMachine _stateMachine;
        private readonly CombatCommandQueue _commandQueue;
        private CombatContext _context;

        // 战斗结果收集
        private MultiEnemyCombatResult _result;
        private List<EnemyCombatantData> _aliveEnemies;
        private int _currentTargetIndex;
        private int _maxRounds = 50;

        // 队伍战斗支持
        private List<CombatantData> _allyCombatants;
        private List<CombatantData> _cachedAliveAllies;
        private List<MemberParticipation> _memberParticipations;
        private int _currentAllyIndex;
        private int _roundCounter;

        // 职业映射（memberName -> JobType）
        private Dictionary<string, JobType> _memberJobMapping = new Dictionary<string, JobType>();

        // 动画派发回调
        public event Action<CombatLogEntry, bool> OnAnimationDispatch;

        /// <summary>
        /// 当前阶段
        /// </summary>
        public CombatPhase currentPhase => _stateMachine.currentPhase;

        /// <summary>
        /// 当前回合
        /// </summary>
        public int currentRound => _context?.round ?? 0;

        /// <summary>
        /// 战斗是否结束
        /// </summary>
        public bool IsCombatEnded => _stateMachine.currentPhase == CombatPhase.Victory ||
                                     _stateMachine.currentPhase == CombatPhase.Defeat;

        /// <summary>
        /// 是否为胜利
        /// </summary>
        public bool IsVictory => _stateMachine.currentPhase == CombatPhase.Victory;

        /// <summary>
        /// 战斗结果
        /// </summary>
        public MultiEnemyCombatResult result => _result;

        /// <summary>
        /// 创建战斗状态机集成器
        /// </summary>
        public CombatStateMachineIntegration()
        {
            _stateMachine = new CombatStateMachine();
            _commandQueue = new CombatCommandQueue();
            _allyCombatants = new List<CombatantData>();
            _cachedAliveAllies = new List<CombatantData>();
            _memberParticipations = new List<MemberParticipation>();

            // 订阅状态机事件
            // 注意：这些事件处理器在战斗结束后通过垃圾回收自动清理
            // 因为_integration是ExecuteMultiEnemyCombat中的局部变量，战斗结束后无引用会被GC
            // _stateMachine也由_integration拥有，随_integration一起被GC时事件处理器变为不可达
            _stateMachine.OnPhaseChanged += HandlePhaseChanged;
            _stateMachine.OnCombatStart += HandleCombatStart;
            _stateMachine.OnCombatEnd += HandleCombatEnd;
        }

        /// <summary>
        /// 初始化战斗
        /// </summary>
        /// <param name="player">玩家数据</param>
        /// <param name="enemies">敌人列表</param>
        /// <param name="playerTeam">玩家队伍（可选，用于多目标战斗）</param>
        public void Initialize(PlayerActor player, List<EnemyCombatantData> enemies, List<TeamMemberData> playerTeam = null)
        {
            if (enemies == null || enemies.Count == 0)
            {
                throw new ArgumentException("敌人列表不能为空", nameof(enemies));
            }

            // 过滤活着的敌人
            _aliveEnemies = enemies.FindAll(e => !e.IsDead);
            if (_aliveEnemies.Count == 0)
            {
                throw new ArgumentException("所有敌人都已死亡");
            }

            // 初始化敌人属性
            foreach (var enemy in _aliveEnemies)
            {
                enemy.maxHp = enemy.hp;
                enemy.critChance = 0.05f;
                enemy.critMultiplier = 1.5f;
                if (enemy.dodgeChance <= 0f) enemy.dodgeChance = 0.05f;
            }

            // 创建战斗上下文
            var playerData = new CombatantData
            {
                name = player.actorName,
                hp = player.stats.currentHp,
                maxHp = player.stats.maxHp,
                armor = player.stats.defense,
                damage = player.stats.attack,
                attack = player.stats.attack,
                defense = player.stats.defense,
                critChance = player.stats.critChance,
                critDamageMultiplier = player.stats.critDamageMultiplier,
                isDefending = false,
                activeBuffs = new List<string>(),
                activeDebuffs = new List<string>()
            };

            // 创建敌人战斗数据（使用第一个存活敌人作为主敌人）
            var enemyData = new CombatantData
            {
                name = _aliveEnemies[0].name,
                hp = _aliveEnemies[0].hp,
                maxHp = _aliveEnemies[0].maxHp,
                armor = _aliveEnemies[0].armor,
                damage = _aliveEnemies[0].damage,
                attack = _aliveEnemies[0].damage,
                defense = _aliveEnemies[0].armor,
                critChance = _aliveEnemies[0].critChance,
                critDamageMultiplier = _aliveEnemies[0].critMultiplier,
                isDefending = false,
                activeBuffs = new List<string>(),
                activeDebuffs = new List<string>()
            };

            _context = new CombatContext(playerData, enemyData);
            _context.currentPhase = CombatPhase.Idle;

            // 初始化命令队列
            _commandQueue.Initialize(_context);

            // 初始化结果
            _result = new MultiEnemyCombatResult();
            _currentTargetIndex = 0;
            _maxRounds = 50;
            _currentAllyIndex = 0;
            _roundCounter = 1;

            // 初始化队伍战斗支持
            _allyCombatants.Clear();
            _memberParticipations.Clear();

            // 如果有队伍，转换为战斗者数据
            if (playerTeam != null && playerTeam.Count > 0)
            {
                foreach (var member in playerTeam)
                {
                    if (member.IsAlive)
                    {
                        var combatant = CombatantData.FromTeamMember(member);
                        _allyCombatants.Add(combatant);

                        var participation = new MemberParticipation(member.id, member.name);
                        participation.isDead = false;
                        _memberParticipations.Add(participation);

                        // 存储职业映射
                        _memberJobMapping[member.name] = member.job;
                    }
                }
            }

            // 初始化状态机
            _stateMachine.Initialize(_context);
        }

        /// <summary>
        /// 初始化战斗（简化版本，用于单敌战斗）
        /// </summary>
        public void Initialize(CombatantData player, CombatantData enemy)
        {
            _context = new CombatContext(player, enemy);
            _context.currentPhase = CombatPhase.Idle;

            _commandQueue.Initialize(_context);

            _result = new MultiEnemyCombatResult();
            _aliveEnemies = new List<EnemyCombatantData>();
            _currentTargetIndex = 0;
            _maxRounds = 20;
            _allyCombatants.Clear();
            _memberParticipations.Clear();
            _currentAllyIndex = 0;
            _roundCounter = 1;
            _memberJobMapping.Clear();

            _stateMachine.Initialize(_context);
        }

        /// <summary>
        /// 执行一个Tick，推进战斗状态机
        /// 返回是否需要继续Tick（战斗未结束）
        /// </summary>
        public bool Tick()
        {
            if (IsCombatEnded)
            {
                return false;
            }

            switch (_stateMachine.currentPhase)
            {
                case CombatPhase.Idle:
                    // 开始战斗
                    _stateMachine.StartCombat();
                    break;

                case CombatPhase.Preparing:
                    // 进入玩家回合
                    _stateMachine.StartPlayerTurn();
                    break;

                case CombatPhase.PlayerTurn:
                    ExecutePlayerTurn();
                    break;

                case CombatPhase.EnemyTurn:
                    ExecuteEnemyTurn();
                    break;

                case CombatPhase.Animating:
                    // 动画阶段完成，进入下一回合
                    TransitionToNextPhase();
                    break;

                case CombatPhase.Victory:
                case CombatPhase.Defeat:
                    return false;
            }

            return !IsCombatEnded;
        }

        /// <summary>
        /// 执行玩家回合（包括队友回合）
        /// </summary>
        private void ExecutePlayerTurn()
        {
            if (_aliveEnemies == null || _aliveEnemies.Count == 0)
            {
                _stateMachine.EndCombat(true);
                return;
            }

            // 检查是否有队伍成员可以攻击
            bool hasAliveAllies = _allyCombatants.Count > 0 && _memberParticipations.Exists(p => !p.isDead);

            if (!hasAliveAllies)
            {
                // 只有玩家一人，执行单次攻击
                ExecuteSingleAttackerTurn(_context.playerCombatant, null, null);
            }
            else
            {
                // 玩家 + 队友依次攻击
                // 玩家攻击
                var playerTarget = _aliveEnemies[_currentTargetIndex];
                bool playerDodged = UnityEngine.Random.value < playerTarget.dodgeChance;

                if (!playerDodged)
                {
                    int damageToEnemy = CalculateDamage(
                        _context.playerCombatant.damage,
                        playerTarget.armor,
                        _context.playerCombatant.critChance,
                        _context.playerCombatant.critDamageMultiplier,
                        out bool isCrit);

                    if (isCrit)
                    {
                        damageToEnemy = Mathf.FloorToInt(damageToEnemy * _context.playerCombatant.critDamageMultiplier);
                    }

                    playerTarget.TakeDamage(damageToEnemy);
                    _result.totalDamageDealt += damageToEnemy;

                    var logEntry = new CombatLogEntry
                    {
                        round = _roundCounter,
                        attackerName = _context.playerCombatant.name,
                        defenderName = playerTarget.name,
                        damageDealt = damageToEnemy,
                        defenderHpAfter = playerTarget.hp,
                        wasCritical = isCrit
                    };
                    _result.combatLog.Add(logEntry);
                    OnAnimationDispatch?.Invoke(logEntry, true);

                    if (playerTarget.IsDead)
                    {
                        _result.kills.Add(new EnemyKillInfo
                        {
                            name = playerTarget.name,
                            roundKilled = _roundCounter
                        });
                    }
                }
                else
                {
                    var logEntry = new CombatLogEntry
                    {
                        round = _roundCounter,
                        attackerName = _context.playerCombatant.name,
                        defenderName = playerTarget.name,
                        damageDealt = 0,
                        defenderHpAfter = playerTarget.hp,
                        wasCritical = false
                    };
                    _result.combatLog.Add(logEntry);
                    OnAnimationDispatch?.Invoke(logEntry, true);
                }

                // 移动到下一个存活敌人
                _currentTargetIndex = GetNextAliveEnemyIndex(_aliveEnemies, _currentTargetIndex);
                if (_currentTargetIndex < 0)
                {
                    // 所有敌人死亡，胜利
                    _stateMachine.EndCombat(true);
                    return;
                }

                // 队友攻击（镖师有+15%伤害加成）
                foreach (var ally in _allyCombatants)
                {
                    if (!ally.IsAlive()) continue;

                    var participation = _memberParticipations.Find(p => p.name == ally.name);

                    // 获取镖师战斗加成
                    float combatBonus = 0f;
                    if (_memberJobMapping.TryGetValue(ally.name, out var job))
                    {
                        combatBonus = JobSystem.instance.GetCombatBonus(job);
                    }

                    var allyTarget = _aliveEnemies[_currentTargetIndex];
                    bool allyDodged = UnityEngine.Random.value < allyTarget.dodgeChance;

                    if (!allyDodged)
                    {
                        int baseDamage = CalculateDamage(
                            ally.damage,
                            allyTarget.armor,
                            ally.critChance,
                            ally.critDamageMultiplier,
                            out bool isCrit);

                        // 应用镖师加成
                        if (combatBonus > 0f)
                        {
                            baseDamage = Mathf.FloorToInt(baseDamage * (1f + combatBonus));
                        }

                        if (isCrit)
                        {
                            baseDamage = Mathf.FloorToInt(baseDamage * ally.critDamageMultiplier);
                        }

                        allyTarget.TakeDamage(baseDamage);
                        _result.totalDamageDealt += baseDamage;

                        if (participation != null)
                        {
                            participation.damageDealt += baseDamage;
                        }

                        var allyLogEntry = new CombatLogEntry
                        {
                            round = _roundCounter,
                            attackerName = ally.name,
                            defenderName = allyTarget.name,
                            damageDealt = baseDamage,
                            defenderHpAfter = allyTarget.hp,
                            wasCritical = isCrit
                        };
                        _result.combatLog.Add(allyLogEntry);
                        OnAnimationDispatch?.Invoke(allyLogEntry, true);

                        if (allyTarget.IsDead)
                        {
                            _result.kills.Add(new EnemyKillInfo
                            {
                                name = allyTarget.name,
                                roundKilled = _roundCounter
                            });

                            if (participation != null)
                            {
                                participation.kills++;
                            }
                        }
                    }
                    else
                    {
                        var allyLogEntry = new CombatLogEntry
                        {
                            round = _roundCounter,
                            attackerName = ally.name,
                            defenderName = allyTarget.name,
                            damageDealt = 0,
                            defenderHpAfter = allyTarget.hp,
                            wasCritical = false
                        };
                        _result.combatLog.Add(allyLogEntry);
                        OnAnimationDispatch?.Invoke(allyLogEntry, true);
                    }

                    // 移动到下一个存活敌人
                    _currentTargetIndex = GetNextAliveEnemyIndex(_aliveEnemies, _currentTargetIndex);
                    if (_currentTargetIndex < 0)
                    {
                        // 所有敌人死亡，胜利
                        _stateMachine.EndCombat(true);
                        return;
                    }
                }

                // 医者治疗 - 每3回合治疗最低HP队友
                if (_roundCounter % HEALER_HEAL_INTERVAL == 0)
                {
                    PerformHealerHeal();
                }
            }

            // 进入动画阶段
            _stateMachine.EnterAnimating();
        }

        /// <summary>
        /// 执行单个攻击者的回合（用于无队伍时的玩家攻击）
        /// </summary>
        private void ExecuteSingleAttackerTurn(CombatantData attacker, CombatantData ally, MemberParticipation participation)
        {
            if (_aliveEnemies == null || _aliveEnemies.Count == 0)
            {
                _stateMachine.EndCombat(true);
                return;
            }

            var currentTarget = _aliveEnemies[_currentTargetIndex];

            // 检查敌人是否闪避
            bool dodged = UnityEngine.Random.value < currentTarget.dodgeChance;

            if (!dodged)
            {
                // 计算伤害
                int damageToEnemy = CalculateDamage(
                    attacker.damage,
                    currentTarget.armor,
                    attacker.critChance,
                    attacker.critDamageMultiplier,
                    out bool isCrit);

                if (isCrit)
                {
                    damageToEnemy = Mathf.FloorToInt(damageToEnemy * attacker.critDamageMultiplier);
                }

                currentTarget.TakeDamage(damageToEnemy);
                _result.totalDamageDealt += damageToEnemy;

                if (participation != null)
                {
                    participation.damageDealt += damageToEnemy;
                }

                var logEntry = new CombatLogEntry
                {
                    round = _context.round,
                    attackerName = attacker.name,
                    defenderName = currentTarget.name,
                    damageDealt = damageToEnemy,
                    defenderHpAfter = currentTarget.hp,
                    wasCritical = isCrit
                };
                _result.combatLog.Add(logEntry);

                // 派发动画事件
                OnAnimationDispatch?.Invoke(logEntry, true);

                // 检查敌人是否死亡
                if (currentTarget.IsDead)
                {
                    _result.kills.Add(new EnemyKillInfo
                    {
                        name = currentTarget.name,
                        roundKilled = _context.round
                    });

                    if (participation != null)
                    {
                        participation.kills++;
                    }

                    Debug.Log($"{LOG_PREFIX} {currentTarget.name} 被击败于第 {_context.round} 回合");
                }
            }
            else
            {
                // 闪避
                var logEntry = new CombatLogEntry
                {
                    round = _context.round,
                    attackerName = attacker.name,
                    defenderName = currentTarget.name,
                    damageDealt = 0,
                    defenderHpAfter = currentTarget.hp,
                    wasCritical = false
                };
                _result.combatLog.Add(logEntry);

                // 派发动画事件
                OnAnimationDispatch?.Invoke(logEntry, true);
            }

            // 移动到下一个存活敌人
            _currentTargetIndex = GetNextAliveEnemyIndex(_aliveEnemies, _currentTargetIndex);

            // 如果所有敌人都死了，胜利
            if (_currentTargetIndex < 0)
            {
                _stateMachine.EndCombat(true);
                return;
            }

            // 进入动画阶段
            _stateMachine.EnterAnimating();
        }

        /// <summary>
        /// 执行医者治疗
        /// </summary>
        private void PerformHealerHeal()
        {
            // 找到最低HP的存活队友
            CombatantData lowestHpAlly = null;
            int lowestHp = int.MaxValue;

            foreach (var ally in _allyCombatants)
            {
                if (ally.IsAlive() && ally.hp < lowestHp)
                {
                    lowestHp = ally.hp;
                    lowestHpAlly = ally;
                }
            }

            // 找到医者
            string healerName = null;
            foreach (var kvp in _memberJobMapping)
            {
                if (kvp.Value == JobType.Healer)
                {
                    healerName = kvp.Key;
                    break;
                }
            }

            if (healerName == null || lowestHpAlly == null || lowestHpAlly.hp >= lowestHpAlly.maxHp)
            {
                // 没有需要治疗的队友
                return;
            }

            // 计算治疗量：20% of maxHp
            int healAmount = Mathf.FloorToInt(lowestHpAlly.maxHp * HEALER_HEAL_RATIO);
            int actualHeal = Mathf.Min(healAmount, lowestHpAlly.maxHp - lowestHpAlly.hp);

            if (actualHeal > 0)
            {
                lowestHpAlly.Heal(actualHeal);

                var participation = _memberParticipations.Find(p => p.name == healerName);
                if (participation != null)
                {
                    participation.healingDone += actualHeal;
                }

                var healLogEntry = new CombatLogEntry
                {
                    round = _roundCounter,
                    attackerName = healerName,
                    defenderName = lowestHpAlly.name,
                    damageDealt = -actualHeal, // 负数表示治疗
                    defenderHpAfter = lowestHpAlly.hp,
                    wasCritical = false
                };
                _result.combatLog.Add(healLogEntry);
            }
        }

        /// <summary>
        /// 执行敌人回合
        /// </summary>
        private void ExecuteEnemyTurn()
        {
            if (_aliveEnemies == null || _aliveEnemies.Count == 0)
            {
                _stateMachine.EndCombat(true);
                return;
            }

            // 每个敌人依次攻击
            foreach (var enemy in _aliveEnemies)
            {
                if (enemy.IsDead) continue;

                // 确定攻击目标（优先攻击存活的队友，否则攻击玩家）
                CombatantData target = null;
                MemberParticipation targetParticipation = null;

                // 随机找一个存活的队友
                _cachedAliveAllies.Clear();
                foreach (var ally in _allyCombatants)
                {
                    if (ally.IsAlive())
                    {
                        _cachedAliveAllies.Add(ally);
                    }
                }

                if (_cachedAliveAllies.Count > 0)
                {
                    // 随机选择一个存活的队友
                    target = _cachedAliveAllies[UnityEngine.Random.Range(0, _cachedAliveAllies.Count)];
                    targetParticipation = _memberParticipations.Find(p => p.name == target.name);
                }

                // 如果没有存活的队友，攻击玩家
                if (target == null)
                {
                    target = _context.playerCombatant;
                }

                // 闪避判定
                float dodgeChance = enemy.dodgeChance > 0 ? enemy.dodgeChance : 0.05f;
                if (UnityEngine.Random.value < dodgeChance)
                {
                    var logEntry = new CombatLogEntry
                    {
                        round = _roundCounter,
                        attackerName = enemy.name,
                        defenderName = target.name,
                        damageDealt = 0,
                        defenderHpAfter = target.hp,
                        wasCritical = false
                    };
                    _result.combatLog.Add(logEntry);

                    // 派发动画事件
                    OnAnimationDispatch?.Invoke(logEntry, false);
                    continue;
                }

                // 计算敌人伤害
                int damageToTarget = CalculateDamage(
                    enemy.damage,
                    target.armor,
                    enemy.critChance,
                    enemy.critMultiplier,
                    out bool isCrit);

                if (isCrit)
                {
                    damageToTarget = Mathf.FloorToInt(damageToTarget * enemy.critMultiplier);
                }

                target.TakeDamage(damageToTarget);

                if (targetParticipation != null)
                {
                    targetParticipation.damageTaken += damageToTarget;
                }
                else
                {
                    _result.playerDamageTaken += damageToTarget;
                }

                var enemyLogEntry = new CombatLogEntry
                {
                    round = _roundCounter,
                    attackerName = enemy.name,
                    defenderName = target.name,
                    damageDealt = damageToTarget,
                    defenderHpAfter = target.hp,
                    wasCritical = isCrit
                };
                _result.combatLog.Add(enemyLogEntry);

                // 派发动画事件
                OnAnimationDispatch?.Invoke(enemyLogEntry, false);

                // 检查目标是否死亡
                if (!target.IsAlive())
                {
                    if (targetParticipation != null)
                    {
                        targetParticipation.isDead = true;
                    }
                    else if (target == _context.playerCombatant)
                    {
                        // 玩家死亡
                        _stateMachine.EndCombat(false);
                        return;
                    }
                }
            }

            // 移除死亡敌人
            _aliveEnemies.RemoveAll(e => e.IsDead);

            // 重新计算当前目标索引
            if (_aliveEnemies.Count > 0 && _currentTargetIndex >= _aliveEnemies.Count)
            {
                _currentTargetIndex = 0;
            }

            // 进入动画阶段
            _stateMachine.EnterAnimating();
        }

        /// <summary>
        /// 从动画阶段转换到下一阶段
        /// </summary>
        private void TransitionToNextPhase()
        {
            // 检查战斗是否结束
            if (_stateMachine.CheckCombatEnd())
            {
                return;
            }

            // 检查回合数限制
            if (_roundCounter >= _maxRounds)
            {
                _stateMachine.EndCombat(false);
                return;
            }

            // 切换到敌人回合
            _stateMachine.StartEnemyTurn();
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

            return -1;
        }

        /// <summary>
        /// 计算伤害（委派到共享的DamageCalculator）
        /// </summary>
        private int CalculateDamage(int attack, int defense, float critChance, float critMultiplier, out bool isCrit)
        {
            return DamageCalculator.CalculateDamage(attack, defense, critChance, critMultiplier, out isCrit);
        }

        /// <summary>
        /// 处理阶段变化
        /// </summary>
        private void HandlePhaseChanged(CombatPhase from, CombatPhase to)
        {
            Debug.Log($"{LOG_PREFIX} Phase changed: {from} -> {to}");
        }

        /// <summary>
        /// 处理战斗开始
        /// </summary>
        private void HandleCombatStart()
        {
            Debug.Log($"{LOG_PREFIX} Combat started");
        }

        /// <summary>
        /// 处理战斗结束
        /// </summary>
        private void HandleCombatEnd(bool victory)
        {
            Debug.Log($"{LOG_PREFIX} Combat ended. Victory: {victory}");

            // 设置战斗结果
            _result.playerVictory = victory;

            // 更新成员参与数据
            _result.memberParticipations = new List<MemberParticipation>(_memberParticipations);

            if (victory && _aliveEnemies != null)
            {
                // 计算奖励
                int baseGold = CalculateMultiEnemyGoldReward(_aliveEnemies);
                int baseExp = _result.kills.Count * 10;

                // 应用职业加成
                float goldBonus = 0f;
                float expBonus = 0f;

                foreach (var kvp in _memberJobMapping)
                {
                    if (kvp.Value == JobType.Merchant)
                    {
                        goldBonus = Mathf.Max(goldBonus, 0.10f); // 商贾+10%金币
                    }
                    if (kvp.Value == JobType.Scholar)
                    {
                        expBonus = Mathf.Max(expBonus, 0.10f); // 学者+10%经验
                    }
                }

                _result.goldReward = Mathf.FloorToInt(baseGold * (1f + goldBonus));
                _result.expReward = Mathf.FloorToInt(baseExp * (1f + expBonus));
                _result.endMessage = $"击败了 {_result.kills.Count} 个敌人！获得 {_result.goldReward} 金币。";
            }
            else if (!victory)
            {
                _result.endMessage = "被敌人击败了...";
            }
            else
            {
                _result.endMessage = "战斗陷入僵局...";
            }
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
        /// 获取当前玩家HP
        /// </summary>
        public int GetPlayerHp() => _context?.playerCombatant.hp ?? 0;

        /// <summary>
        /// 更新玩家HP（用于外部同步）
        /// </summary>
        public void UpdatePlayerHp(int hp)
        {
            if (_context != null)
            {
                _context.playerCombatant.hp = hp;
            }
        }

        /// <summary>
        /// 获取命令队列（用于undo/redo）
        /// </summary>
        public CombatCommandQueue commandQueue => _commandQueue;

        /// <summary>
        /// 获取状态机
        /// </summary>
        public CombatStateMachine stateMachine => _stateMachine;

        /// <summary>
        /// 获取战斗上下文
        /// </summary>
        public CombatContext context => _context;

        /// <summary>
        /// 设置职业信息映射（memberName -> JobType）
        /// </summary>
        public void SetMemberJobMapping(Dictionary<string, JobType> jobMapping)
        {
            _memberJobMapping = jobMapping;
        }

        /// <summary>
        /// 获取成员的职业
        /// </summary>
        public JobType GetJobTypeForMember(string memberName)
        {
            return _memberJobMapping.TryGetValue(memberName, out var job) ? job : JobType.None;
        }
    }
}