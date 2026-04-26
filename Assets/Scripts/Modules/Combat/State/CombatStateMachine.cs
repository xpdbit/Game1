using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Modules.Combat.State
{
    /// <summary>
    /// 战斗状态机
    /// 管理战斗阶段转换和事件通知
    /// </summary>
    public class CombatStateMachine
    {
        private const string LOG_PREFIX = "[CombatStateMachine]";

        private CombatPhase _currentPhase;
        private CombatPhase _previousPhase;
        private CombatContext _context;
        private readonly Dictionary<CombatPhase, HashSet<CombatPhase>> _validTransitions;

        /// <summary>
        /// 阶段变化时触发
        /// </summary>
        public event Action<CombatPhase, CombatPhase> OnPhaseChanged;

        /// <summary>
        /// 战斗开始时触发
        /// </summary>
        public event Action OnCombatStart;

        /// <summary>
        /// 战斗结束时触发
        /// </summary>
        public event Action<bool> OnCombatEnd; // bool: 是否为胜利

        /// <summary>
        /// 当前阶段
        /// </summary>
        public CombatPhase currentPhase => _currentPhase;

        /// <summary>
        /// 上一个阶段
        /// </summary>
        public CombatPhase previousPhase => _previousPhase;

        /// <summary>
        /// 创建战斗状态机
        /// </summary>
        public CombatStateMachine()
        {
            _currentPhase = CombatPhase.Idle;
            _previousPhase = CombatPhase.Idle;
            _validTransitions = new Dictionary<CombatPhase, HashSet<CombatPhase>>();

            // 初始化有效的阶段转换
            InitializeTransitions();
        }

        /// <summary>
        /// 初始化状态转换规则
        /// </summary>
        private void InitializeTransitions()
        {
            // Idle可以转到Preparing
            _validTransitions[CombatPhase.Idle] = new HashSet<CombatPhase> { CombatPhase.Preparing };

            // Preparing可以转到PlayerTurn或EnemyTurn
            _validTransitions[CombatPhase.Preparing] = new HashSet<CombatPhase>
            {
                CombatPhase.PlayerTurn,
                CombatPhase.EnemyTurn
            };

            // PlayerTurn可以转到Animating、Victory、Defeat
            _validTransitions[CombatPhase.PlayerTurn] = new HashSet<CombatPhase>
            {
                CombatPhase.Animating,
                CombatPhase.Victory,
                CombatPhase.Defeat
            };

            // EnemyTurn可以转到Animating、Victory、Defeat
            _validTransitions[CombatPhase.EnemyTurn] = new HashSet<CombatPhase>
            {
                CombatPhase.Animating,
                CombatPhase.Victory,
                CombatPhase.Defeat
            };

            // Animating可以转到PlayerTurn、EnemyTurn、Victory、Defeat
            _validTransitions[CombatPhase.Animating] = new HashSet<CombatPhase>
            {
                CombatPhase.PlayerTurn,
                CombatPhase.EnemyTurn,
                CombatPhase.Victory,
                CombatPhase.Defeat
            };

            // Victory和Defeat只能转回Idle
            _validTransitions[CombatPhase.Victory] = new HashSet<CombatPhase> { CombatPhase.Idle };
            _validTransitions[CombatPhase.Defeat] = new HashSet<CombatPhase> { CombatPhase.Idle };
        }

        /// <summary>
        /// 初始化状态机
        /// </summary>
        public void Initialize(CombatContext context)
        {
            _context = context;
            _currentPhase = CombatPhase.Idle;
            _previousPhase = CombatPhase.Idle;
        }

        /// <summary>
        /// 检查是否可以转换到指定阶段
        /// </summary>
        public bool CanTransition(CombatPhase targetPhase)
        {
            if (_currentPhase == targetPhase)
            {
                return false;
            }

            if (!_validTransitions.TryGetValue(_currentPhase, out var validTargets))
            {
                return false;
            }

            return validTargets.Contains(targetPhase);
        }

        /// <summary>
        /// 转换到指定阶段
        /// </summary>
        /// <param name="targetPhase">目标阶段</param>
        /// <returns>转换是否成功</returns>
        public bool TransitionTo(CombatPhase targetPhase)
        {
            if (!CanTransition(targetPhase))
            {
                Debug.LogWarning($"{LOG_PREFIX} Invalid transition from {_currentPhase} to {targetPhase}");
                return false;
            }

            _previousPhase = _currentPhase;
            _currentPhase = targetPhase;

            // 更新上下文中的阶段
            if (_context != null)
            {
                _context.currentPhase = _currentPhase;
            }

            Debug.Log($"{LOG_PREFIX} Transitioned from {_previousPhase} to {_currentPhase}");

            // 触发事件
            OnPhaseChanged?.Invoke(_previousPhase, _currentPhase);

            // 检查是否触发战斗开始/结束事件
            CheckSpecialPhaseEvents();

            return true;
        }

        /// <summary>
        /// 检查特殊阶段事件
        /// </summary>
        private void CheckSpecialPhaseEvents()
        {
            if (_previousPhase == CombatPhase.Idle && _currentPhase == CombatPhase.Preparing)
            {
                OnCombatStart?.Invoke();
            }

            if (_currentPhase == CombatPhase.Victory)
            {
                OnCombatEnd?.Invoke(true);
            }
            else if (_currentPhase == CombatPhase.Defeat)
            {
                OnCombatEnd?.Invoke(false);
            }
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat()
        {
            if (_currentPhase != CombatPhase.Idle)
            {
                Debug.LogWarning($"{LOG_PREFIX} Cannot start combat from phase {_currentPhase}");
                return;
            }

            TransitionTo(CombatPhase.Preparing);
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        /// <param name="victory">是否为胜利</param>
        public void EndCombat(bool victory)
        {
            TransitionTo(victory ? CombatPhase.Victory : CombatPhase.Defeat);
        }

        /// <summary>
        /// 进入玩家回合
        /// </summary>
        public void StartPlayerTurn()
        {
            if (_context != null)
            {
                _context.round++;
            }

            TransitionTo(CombatPhase.PlayerTurn);
        }

        /// <summary>
        /// 进入敌人回合
        /// </summary>
        public void StartEnemyTurn()
        {
            TransitionTo(CombatPhase.EnemyTurn);
        }

        /// <summary>
        /// 进入动画阶段
        /// </summary>
        public void EnterAnimating()
        {
            TransitionTo(CombatPhase.Animating);
        }

        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        public bool CheckCombatEnd()
        {
            if (_context == null)
            {
                return false;
            }

            if (!(_currentPhase == CombatPhase.PlayerTurn ||
                  _currentPhase == CombatPhase.EnemyTurn ||
                  _currentPhase == CombatPhase.Animating))
            {
                return false;
            }

            bool playerAlive = _context.playerCombatant.IsAlive();
            bool enemyAlive = _context.enemyCombatant.IsAlive();

            if (!playerAlive)
            {
                EndCombat(false);
                return true;
            }

            if (!enemyAlive)
            {
                EndCombat(true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 重置状态机
        /// </summary>
        public void Reset()
        {
            _previousPhase = _currentPhase;
            _currentPhase = CombatPhase.Idle;

            if (_context != null)
            {
                _context.currentPhase = _currentPhase;
            }

            OnPhaseChanged?.Invoke(_previousPhase, _currentPhase);
        }

        /// <summary>
        /// 获取阶段描述
        /// </summary>
        public string GetPhaseDescription()
        {
            return _currentPhase switch
            {
                CombatPhase.Idle => "空闲",
                CombatPhase.Preparing => "准备阶段",
                CombatPhase.PlayerTurn => $"玩家回合 ({_context?.round ?? 0})",
                CombatPhase.EnemyTurn => $"敌人回合 ({_context?.round ?? 0})",
                CombatPhase.Animating => "动画播放中",
                CombatPhase.Victory => "胜利！",
                CombatPhase.Defeat => "失败",
                _ => "未知"
            };
        }
    }
}