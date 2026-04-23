using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 竞技场状态
    /// </summary>
    public enum PVPArenaState
    {
        /// <summary>布阵期</summary>
        Setup,

        /// <summary>对战期</summary>
        Battle,

        /// <summary>结算期</summary>
        Result
    }

    /// <summary>
    /// 位置类型
    /// </summary>
    public enum LanePosition
    {
        /// <summary>上路</summary>
        Top,

        /// <summary>中路</summary>
        Middle,

        /// <summary>下路</summary>
        Bottom
    }

    /// <summary>
    /// 放置的单位
    /// </summary>
    public struct PlacedUnit
    {
        public CardData card;
        public LanePosition position;
        public bool isLeft;  // 己方/对方
        public float moveSpeed;
        public float attackRange;
        public int damage;
    }

    /// <summary>
    /// 道路结果
    /// </summary>
    public struct LaneResult
    {
        public LanePosition lane;
        public bool playerWon;
        public int damageDealt;
        public int damageTaken;
    }

    /// <summary>
    /// PVP竞技场管理器
    /// 管理布阵、对战、结算
    /// </summary>
    public class PVPArenaManager
    {
        #region Singleton
        private static PVPArenaManager _instance;
        public static PVPArenaManager instance => _instance ??= new PVPArenaManager();
        #endregion

        #region Configuration
        // 布阵期时长（秒）
        private const float SETUP_DURATION = 30f;

        // 对战期时长（秒）
        private const float BATTLE_DURATION = 60f;

        // 结算期时长（秒）
        private const float RESULT_DURATION = 10f;
        #endregion

        #region Private Fields
        private PVPArenaState _state = PVPArenaState.Setup;
        private float _stateTimer;
        private string _opponentId;

        // 布阵
        private readonly List<PlacedUnit> _playerUnits = new();
        private readonly List<PlacedUnit> _enemyUnits = new();

        // 战斗结果
        private readonly List<LaneResult> _laneResults = new();
        private bool _playerVictory;
        private int _totalGoldReward;
        private int _totalHonorReward;
        #endregion

        #region Events
        public event Action<LaneResult> onLaneResolved;
        public event Action<PVPArenaState> onStateChanged;
        public event Action<MatchResult> onArenaCompleted;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前状态
        /// </summary>
        public PVPArenaState state => _state;

        /// <summary>
        /// 剩余时间
        /// </summary>
        public float remainingTime => _stateTimer;

        /// <summary>
        /// 对手ID
        /// </summary>
        public string opponentId => _opponentId;
        #endregion

        #region Public Methods

        /// <summary>
        /// 开始竞技（从布阵阶段）
        /// </summary>
        public void StartArena(string opponentId)
        {
            _opponentId = opponentId;
            _state = PVPArenaState.Setup;
            _stateTimer = SETUP_DURATION;

            _playerUnits.Clear();
            _enemyUnits.Clear();
            _laneResults.Clear();

            onStateChanged?.Invoke(_state);

            Debug.Log($"[PVPArenaManager] Arena started with {_opponentId} - Setup phase ({SETUP_DURATION}s)");
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update()
        {
            if (_state == PVPArenaState.Setup || _state == PVPArenaState.Battle)
            {
                _stateTimer -= UnityEngine.Time.deltaTime;

                if (_stateTimer <= 0)
                {
                    AdvanceState();
                }
            }
        }

        /// <summary>
        /// 放置单位
        /// </summary>
        public void PlaceUnit(CardData card, LanePosition position, bool isLeft)
        {
            if (_state != PVPArenaState.Setup)
            {
                Debug.LogWarning("[PVPArenaManager] Cannot place unit - not in setup phase");
                return;
            }

            var unit = new PlacedUnit
            {
                card = card,
                position = position,
                isLeft = isLeft,
                moveSpeed = 1f + card.attributeMultiplier * 0.1f,
                attackRange = 1f,
                damage = (int)(10 * card.attributeMultiplier)
            };

            if (isLeft)
                _playerUnits.Add(unit);
            else
                _enemyUnits.Add(unit);

            Debug.Log($"[PVPArenaManager] Placed unit {card.nameTextId} at {position}");
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public void RemoveUnit(LanePosition position, bool isLeft)
        {
            if (_state != PVPArenaState.Setup)
                return;

            if (isLeft)
                _playerUnits.RemoveAll(u => u.position == position);
            else
                _enemyUnits.RemoveAll(u => u.position == position);
        }

        /// <summary>
        /// 获取已放置的单位
        /// </summary>
        public List<PlacedUnit> GetPlacedUnits(bool isPlayer)
        {
            return isPlayer ? new List<PlacedUnit>(_playerUnits) : new List<PlacedUnit>(_enemyUnits);
        }

        /// <summary>
        /// 确认布阵
        /// </summary>
        public void ConfirmDeployment()
        {
            if (_state != PVPArenaState.Setup)
                return;

            // 如果玩家未放置任何单位，随机生成一些
            if (_playerUnits.Count == 0)
            {
                GenerateDefaultUnits(_playerUnits, true);
            }

            // 模拟敌人布阵
            GenerateDefaultUnits(_enemyUnits, false);

            // 进入战斗阶段
            _state = PVPArenaState.Battle;
            _stateTimer = BATTLE_DURATION;

            onStateChanged?.Invoke(_state);

            Debug.Log($"[PVPArenaManager] Deployment confirmed - Battle phase ({BATTLE_DURATION}s)");
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle()
        {
            if (_state != PVPArenaState.Battle)
                return;

            // 模拟战斗
            SimulateBattle();
        }

        /// <summary>
        /// 获取道路结果
        /// </summary>
        public LaneResult GetLaneResult(LanePosition position)
        {
            foreach (var result in _laneResults)
            {
                if (result.lane == position)
                    return result;
            }

            return default;
        }

        /// <summary>
        /// 获取比赛结果
        /// </summary>
        public MatchResult GetMatchResult()
        {
            int playerWins = 0;
            foreach (var result in _laneResults)
            {
                if (result.playerWon)
                    playerWins++;
            }

            bool victory = playerWins >= 2;

            return new MatchResult
            {
                isVictory = victory,
                goldReward = victory ? 500 : 200,
                honorPoints = victory ? 20 : 5,
                expReward = 100
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 进入下一个状态
        /// </summary>
        private void AdvanceState()
        {
            switch (_state)
            {
                case PVPArenaState.Setup:
                    // 自动确认布阵
                    ConfirmDeployment();
                    break;

                case PVPArenaState.Battle:
                    StartBattle();
                    _state = PVPArenaState.Result;
                    _stateTimer = RESULT_DURATION;
                    onStateChanged?.Invoke(_state);
                    break;

                case PVPArenaState.Result:
                    // 竞技场结束
                    var result = GetMatchResult();
                    _state = PVPArenaState.Setup; // 重置
                    onArenaCompleted?.Invoke(result);
                    break;
            }
        }

        /// <summary>
        /// 模拟战斗
        /// </summary>
        private void SimulateBattle()
        {
            _laneResults.Clear();

            // 三条路分别计算
            var positions = new[] { LanePosition.Top, LanePosition.Middle, LanePosition.Bottom };

            foreach (var pos in positions)
            {
                var playerUnit = _playerUnits.Find(u => u.position == pos);
                var enemyUnit = _enemyUnits.Find(u => u.position == pos);

                int playerDamage = playerUnit.damage;
                int enemyDamage = enemyUnit.damage;

                bool playerWon = playerDamage >= enemyDamage;

                var laneResult = new LaneResult
                {
                    lane = pos,
                    playerWon = playerWon,
                    damageDealt = playerDamage,
                    damageTaken = enemyDamage
                };

                _laneResults.Add(laneResult);
                onLaneResolved?.Invoke(laneResult);
            }
        }

        /// <summary>
        /// 生成默认单位（模拟AI）
        /// </summary>
        private void GenerateDefaultUnits(List<PlacedUnit> units, bool isPlayer)
        {
            // 简化：每路放置一个默认单位
            var positions = new[] { LanePosition.Top, LanePosition.Middle, LanePosition.Bottom };

            foreach (var pos in positions)
            {
                units.Add(new PlacedUnit
                {
                    card = null,
                    position = pos,
                    isLeft = isPlayer,
                    moveSpeed = 1f,
                    attackRange = 1f,
                    damage = 10
                });
            }
        }

        #endregion
    }
}