using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 游戏状态接口 (UniState框架)
    /// </summary>
    public interface IGameState
    {
        string stateId { get; }
        void OnEnter();
        void OnUpdate(float deltaTime);
        void OnExit();
    }

    /// <summary>
    /// 游戏状态基类
    /// </summary>
    public abstract class GameStateBase : IGameState
    {
        public abstract string stateId { get; }

        public virtual void OnEnter() { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnExit() { }
    }

    /// <summary>
    /// 旅行状态
    /// </summary>
    public class TravelGameState : GameStateBase
    {
        public override string stateId => "Travel";

        public event Action onTravelStart;
        public event Action onTravelComplete;
        public event Action<float> onProgressChanged; // progress 0-1

        private float _progress = 0f;
        public float progress => _progress;

        public override void OnEnter()
        {
            Debug.Log("[TravelState] OnEnter");
            onTravelStart?.Invoke();
        }

        public override void OnUpdate(float deltaTime)
        {
            // 旅行进度更新逻辑由TravelManager驱动
        }

        public override void OnExit()
        {
            Debug.Log("[TravelState] OnExit");
            onTravelComplete?.Invoke();
        }

        public void SetProgress(float value)
        {
            _progress = Mathf.Clamp01(value);
            onProgressChanged?.Invoke(_progress);
        }
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public class CombatGameState : GameStateBase
    {
        public override string stateId => "Combat";

        public event Action onCombatStart;
        public event Action onCombatEnd;
        public event Action<bool> onCombatResult; // victory/defeat

        public CombatResult? lastResult { get; private set; }

        public override void OnEnter()
        {
            Debug.Log("[CombatState] OnEnter");
            onCombatStart?.Invoke();
        }

        public override void OnUpdate(float deltaTime)
        {
            // 战斗更新逻辑由CombatSystem驱动
        }

        public override void OnExit()
        {
            Debug.Log("[CombatState] OnExit");
            onCombatEnd?.Invoke();
        }

        public void SetResult(bool victory)
        {
            lastResult = victory ? CombatResult.Victory : CombatResult.Defeat;
            onCombatResult?.Invoke(victory);
        }
    }

    /// <summary>
    /// UI状态
    /// </summary>
    public class UIGameState : GameStateBase
    {
        public override string stateId => "UI";

        public string currentPanelId { get; private set; }

        public override void OnEnter()
        {
            Debug.Log("[UIState] OnEnter");
        }

        public override void OnUpdate(float deltaTime)
        {
            // UI更新逻辑由UIManager驱动
        }

        public override void OnExit()
        {
            Debug.Log("[UIState] OnExit");
            currentPanelId = null;
        }

        public void SetCurrentPanel(string panelId)
        {
            currentPanelId = panelId;
        }
    }

    /// <summary>
    /// 事件状态
    /// </summary>
    public class EventGameState : GameStateBase
    {
        public override string stateId => "Event";

        public string currentEventId { get; private set; }

        public override void OnEnter()
        {
            Debug.Log("[EventState] OnEnter");
        }

        public override void OnUpdate(float deltaTime)
        {
            // 事件更新逻辑由EventTreeRunner驱动
        }

        public override void OnExit()
        {
            Debug.Log("[EventState] OnExit");
            currentEventId = null;
        }

        public void SetCurrentEvent(string eventId)
        {
            currentEventId = eventId;
        }
    }

    /// <summary>
    /// 统一游戏状态机 (UniState)
    /// </summary>
    public class UniStateMachine
    {
        private IGameState _currentState;
        private readonly GameStateBase[] _states;
        private string _previousStateId;

        public string currentStateId => _currentState?.stateId;
        public string previousStateId => _previousStateId;

        public UniStateMachine(params GameStateBase[] states)
        {
            _states = states;
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        public void TransitionTo(string stateId)
        {
            var targetState = Array.Find(_states, s => s.stateId == stateId);
            if (targetState == null)
            {
                Debug.LogWarning($"[UniStateMachine] State not found: {stateId}");
                return;
            }

            if (_currentState == targetState) return;

            // Exit current state
            _previousStateId = _currentState?.stateId;
            _currentState?.OnExit();

            // Enter new state
            _currentState = targetState;
            _currentState.OnEnter();

            Debug.Log($"[UniStateMachine] Transition: {_previousStateId} -> {stateId}");
        }

        /// <summary>
        /// 更新当前状态
        /// </summary>
        public void Update(float deltaTime)
        {
            _currentState?.OnUpdate(deltaTime);
        }

        /// <summary>
        /// 获取指定状态
        /// </summary>
        public T GetState<T>() where T : GameStateBase
        {
            return Array.Find(_states, s => s is T) as T;
        }
    }

    /// <summary>
    /// 战斗结果枚举
    /// </summary>
    public enum CombatResult
    {
        None,
        Victory,
        Defeat
    }
}