using System;
using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 匹配状态
    /// </summary>
    public enum MatchStatus
    {
        /// <summary>空闲</summary>
        Idle,

        /// <summary>搜索中</summary>
        Searching,

        /// <summary>已找到对手</summary>
        Found,

        /// <summary>即将开始</summary>
        Starting
    }

    /// <summary>
    /// 对战结果
    /// </summary>
    public struct MatchResult
    {
        public bool isVictory;
        public int goldReward;
        public int honorPoints;
        public int expReward;
    }

    /// <summary>
    /// PVP匹配管理器
    /// 管理延迟PVP的匹配流程
    /// </summary>
    public class PVPMatchManager
    {
        #region Singleton
        private static PVPMatchManager _instance;
        public static PVPMatchManager instance => _instance ??= new PVPMatchManager();
        #endregion

        #region Configuration
        // 匹配等待时间估算（秒）
        private const float MIN_WAIT_TIME = 30f;
        private const float MAX_WAIT_TIME = 300f; // 5分钟

        // 匹配队列间隔（秒）
        private const float MATCH_CHECK_INTERVAL = 5f;
        #endregion

        #region Private Fields
        private MatchStatus _status = MatchStatus.Idle;
        private float _searchStartTime;
        private float _estimatedWaitTime;
        private string _opponentId;
        private int _playerRank;
        private float _matchProgress;

        // 模拟对手池
        private readonly List<string> _fakeOpponents = new() { "OPP_001", "OPP_002", "OPP_003" };
        #endregion

        #region Events
        public event Action<MatchResult> onMatchCompleted;
        public event Action onMatchFound;
        public event Action onSearchStarted;
        public event Action onSearchCancelled;
        #endregion

        #region Public Properties
        /// <summary>
        /// 当前匹配状态
        /// </summary>
        public MatchStatus status => _status;

        /// <summary>
        /// 估计等待时间（秒）
        /// </summary>
        public float estimatedWaitTime => _estimatedWaitTime;

        /// <summary>
        /// 匹配进度（0-1）
        /// </summary>
        public float matchProgress => _matchProgress;

        /// <summary>
        /// 对手ID
        /// </summary>
        public string opponentId => _opponentId;

        /// <summary>
        /// 是否正在匹配
        /// </summary>
        public bool isSearching => _status == MatchStatus.Searching;
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            _status = MatchStatus.Idle;
            Debug.Log("[PVPMatchManager] Initialized");
        }

        /// <summary>
        /// 开始匹配
        /// </summary>
        public void StartMatch()
        {
            if (_status != MatchStatus.Idle && _status != MatchStatus.Found)
            {
                Debug.LogWarning("[PVPMatchManager] Already searching or in match");
                return;
            }

            _status = MatchStatus.Searching;
            _searchStartTime = UnityEngine.Time.time;
            _estimatedWaitTime = UnityEngine.Random.Range(MIN_WAIT_TIME, MAX_WAIT_TIME);
            _matchProgress = 0;

            onSearchStarted?.Invoke();

            Debug.Log("[PVPMatchManager] Started searching for match");
        }

        /// <summary>
        /// 取消匹配
        /// </summary>
        public void CancelMatch()
        {
            if (_status != MatchStatus.Searching)
                return;

            _status = MatchStatus.Idle;
            _matchProgress = 0;

            onSearchCancelled?.Invoke();

            Debug.Log("[PVPMatchManager] Cancelled match search");
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update()
        {
            if (_status != MatchStatus.Searching)
                return;

            float elapsed = UnityEngine.Time.time - _searchStartTime;
            _matchProgress = UnityEngine.Mathf.Clamp01(elapsed / _estimatedWaitTime);

            // 模拟匹配成功
            if (elapsed >= _estimatedWaitTime)
            {
                // 随机选择对手
                _opponentId = _fakeOpponents[UnityEngine.Random.Range(0, _fakeOpponents.Count)];
                _status = MatchStatus.Found;

                onMatchFound?.Invoke();

                Debug.Log($"[PVPMatchManager] Match found! Opponent: {_opponentId}");
            }
        }

        /// <summary>
        /// 开始PVP对战
        /// </summary>
        public void StartPVP(string opponentId)
        {
            if (_status != MatchStatus.Found)
            {
                Debug.LogWarning("[PVPMatchManager] Match not found yet");
                return;
            }

            _opponentId = opponentId;
            _status = MatchStatus.Starting;

            Debug.Log($"[PVPMatchManager] Starting PVP with {opponentId}");
        }

        /// <summary>
        /// 完成对战
        /// </summary>
        public void CompleteMatch(bool isVictory, int goldReward, int honorPoints, int expReward)
        {
            var result = new MatchResult
            {
                isVictory = isVictory,
                goldReward = goldReward,
                honorPoints = honorPoints,
                expReward = expReward
            };

            _status = MatchStatus.Idle;
            _opponentId = null;
            _matchProgress = 0;

            onMatchCompleted?.Invoke(result);

            Debug.Log($"[PVPMatchManager] Match completed - Victory: {isVictory}, Gold: {goldReward}, Honor: {honorPoints}");
        }

        /// <summary>
        /// 获取匹配状态描述
        /// </summary>
        public string GetStatusDescription()
        {
            return _status switch
            {
                MatchStatus.Idle => "点击匹配开始对战",
                MatchStatus.Searching => $"匹配中... 预计 {_estimatedWaitTime - (UnityEngine.Time.time - _searchStartTime):F0} 秒",
                MatchStatus.Found => "找到对手！",
                MatchStatus.Starting => "对战中...",
                _ => ""
            };
        }

        #endregion
    }
}