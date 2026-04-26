using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game1.UI.Utils;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 战斗结果面板
    /// 显示战斗结果日志和奖励
    /// 继承BaseUIPanel，使用CombatAnimationDispatcher监听战斗动画事件
    /// 注意：伤害数字和暴击特效已移至CombatAnimationDispatcher整合的DamagePopupManager和CombatEffects
    /// </summary>

    /// <summary>
    /// 战斗结果面板
    /// 显示战斗结果日志和奖励
    /// 继承BaseUIPanel，使用CombatAnimationDispatcher监听战斗动画事件
    /// </summary>
    public class UICombatResultPanel : CombatAnimationListenerBase
    {
        [Header("Combat Result Panel")]
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Button _closeButton;

        [Header("Combat Log")]
        [SerializeField] private Transform _logContainer;
        [SerializeField] private GameObject _logEntryPrefab;
        [SerializeField] private int _maxVisibleLogs = 10;

        [Header("Animation")]
        [SerializeField] private float _entryAnimationDelay = 0.3f;
        [SerializeField] private bool _autoCloseAfterVictory = true;
        [SerializeField] private float _autoCloseDelay = 3f;

        private int _currentLogIndex = 0;
        private float _logAnimationTimer = 0f;
        private bool _isAnimating = false;
        private bool _pendingClose = false;
        private float _pendingCloseTimer = 0f;

        private CombatResult _currentResult;
        private List<GameObject> _logEntries = new();

        protected void Awake()
        {
            // 默认初始状态下隐藏
            if (_contentPanel != null)
            {
                _contentPanel.SetActive(false);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // 注册关闭按钮
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        /// <summary>
        /// 显示战斗结果
        /// </summary>
        public void ShowCombatResult(CombatResult result)
        {
            if (result == null)
            {
                Debug.LogWarning("[UICombatResultPanel] ShowCombatResult: result is null");
                return;
            }

            _currentResult = result;
            _currentLogIndex = 0;
            _isAnimating = true;
            _pendingClose = false;

            // 清空旧日志
            ClearLogEntries();

            // 显示面板
            if (_contentPanel != null)
            {
                _contentPanel.SetActive(true);
            }

            // 设置标题
            if (_titleText != null)
            {
                _titleText.text = result.playerVictory ? "战斗胜利！" : "战斗失败...";
                _titleText.color = result.playerVictory ? new Color(1f, 0.8f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            }

            // 设置结果文本
            if (_resultText != null)
            {
                _resultText.text = result.endMessage;
            }

            // 设置奖励文本
            if (_rewardText != null && result.playerVictory)
            {
                _rewardText.text = $"获得: {result.goldReward} 金币";
                _rewardText.gameObject.SetActive(true);
            }
            else if (_rewardText != null)
            {
                _rewardText.gameObject.SetActive(false);
            }

            // 开始日志动画序列
            _logAnimationTimer = 0f;

            Debug.Log($"[UICombatResultPanel] ShowCombatResult: Victory={result.playerVictory}, LogCount={result.combatLog.Count}");
        }

        private void Update()
        {
            if (!_isAnimating) return;

            // 处理日志动画
            if (_currentResult != null && _currentLogIndex < _currentResult.combatLog.Count)
            {
                _logAnimationTimer += Time.deltaTime;

                // 每_entryAnimationDelay秒显示一条日志
                while (_logAnimationTimer >= _entryAnimationDelay && _currentLogIndex < _currentResult.combatLog.Count)
                {
                    ShowNextLogEntry();
                    _logAnimationTimer -= _entryAnimationDelay;
                }
            }

            // 处理自动关闭
            if (_pendingClose)
            {
                _pendingCloseTimer += Time.deltaTime;
                if (_pendingCloseTimer >= _autoCloseDelay)
                {
                    ClosePanel();
                }
            }
        }

        private void ShowNextLogEntry()
        {
            if (_currentResult == null || _currentLogIndex >= _currentResult.combatLog.Count) return;

            var log = _currentResult.combatLog[_currentLogIndex];

            // 创建日志条目
            GameObject entryObj;
            if (_logEntryPrefab != null && _logContainer != null)
            {
                entryObj = Instantiate(_logEntryPrefab, _logContainer);
            }
            else
            {
                entryObj = new GameObject("LogEntry_" + _currentLogIndex);
                entryObj.transform.SetParent(_logContainer);
            }

            // 设置文本
            var text = entryObj.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = entryObj.AddComponent<TextMeshProUGUI>();
                text.fontSize = 16;
                text.alignment = TextAlignmentOptions.Left;
            }

            // 格式化日志文本
            string logText = $"第{log.round}回合: ";
            if (log.damageDealt == 0)
            {
                logText += $"{log.defenderName}闪避了{log.attackerName}的攻击";
                text.color = new Color(0.4f, 0.8f, 1f); // 蓝色
            }
            else if (log.wasCritical)
            {
                logText += $"{log.attackerName}对{log.defenderName}造成暴击伤害 {log.damageDealt}! (HP: {log.defenderHpAfter})";
                text.color = new Color(1f, 0.2f, 0.2f); // 红色
            }
            else
            {
                logText += $"{log.attackerName}对{log.defenderName}造成 {log.damageDealt} 伤害 (HP: {log.defenderHpAfter})";
                text.color = new Color(1f, 0.95f, 0.8f); // 金色
            }

            text.text = logText;
            entryObj.SetActive(true);

            _logEntries.Add(entryObj);
            _currentLogIndex++;

            Debug.Log($"[UICombatResultPanel] ShowNextLogEntry: {logText}");
        }

        private void ClearLogEntries()
        {
            foreach (var entry in _logEntries)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            _logEntries.Clear();
        }

        /// <summary>
        /// 战斗动画事件处理
        /// </summary>
        public override void OnCombatAnimationEvent(CombatAnimationEvent e)
        {
            Debug.Log($"[UICombatResultPanel] OnCombatAnimationEvent: {e.eventType}, damage={e.damage}");

            // 如果需要显示伤害数字（由外部调用DamagePopupManager）
            // 这里主要用于UI状态同步
            switch (e.eventType)
            {
                case CombatAnimationEventType.PlayerCrit:
                case CombatAnimationEventType.EnemyCrit:
                    // 触发屏幕震动效果（预留）
                    break;

                case CombatAnimationEventType.PlayerDodge:
                case CombatAnimationEventType.EnemyDodge:
                    // 触发闪避特效（预留）
                    break;
            }
        }

        private void OnCloseButtonClicked()
        {
            ClosePanel();
        }

        private void ClosePanel()
        {
            _isAnimating = false;
            _pendingClose = false;
            _pendingCloseTimer = 0f;

            if (_contentPanel != null)
            {
                _contentPanel.SetActive(false);
            }

            // 通知战斗结束
            // EventBus可以在这里发布战斗结束事件

            Debug.Log("[UICombatResultPanel] ClosePanel");
        }

        /// <summary>
        /// 标记战斗结束，准备自动关闭
        /// </summary>
        public void PrepareAutoClose(bool autoClose)
        {
            if (autoClose && _autoCloseAfterVictory && _currentResult?.playerVictory == true)
            {
                _pendingClose = true;
                _pendingCloseTimer = 0f;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
            }
        }
    }
}