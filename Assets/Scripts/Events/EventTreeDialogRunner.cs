using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events
{
    /// <summary>
    /// 事件树对话事件数据
    /// </summary>
    [Serializable]
    public class EventTreeDialogData
    {
        public string speakerId;
        public string speakerName;
        public string portraitId;           // 头像资源ID
        public string expression;           // 表情
        public string title;
        public string description;
        public List<EventTreeChoice> choices;
        public Action<string> onChoiceSelected;
    }

    /// <summary>
    /// 事件树对话配置
    /// </summary>
    [Serializable]
    public class EventTreeDialogConfig
    {
        public bool enableTypewriter = true;
        public bool enablePortrait = true;
        public bool enableChoiceAnimation = true;
        public float typingSpeed = 50f;           // 每秒字符数
        public float choiceShowDelay = 0.1f;     // 选项显示延迟
        public float transitionDuration = 0.3f;   // 过渡动画时长
    }

    /// <summary>
    /// 事件树对话运行器
    /// 负责将 EventTreeRunner 的节点转换为对话UI显示
    /// </summary>
    public class EventTreeDialogRunner
    {
        #region Singleton
        private static EventTreeDialogRunner _instance;
        public static EventTreeDialogRunner instance => _instance ??= new EventTreeDialogRunner();
        #endregion

        [Header("运行时引用（由外部注入）")]
        private UI.Dialog.UISelectionDialogEx _dialog;
        private UI.Dialog.CharacterPortrait _characterPortrait;

        [Header("配置")]
        public EventTreeDialogConfig config = new();

        // 头像资源映射
        private Dictionary<string, UnityEngine.Sprite> _portraitCache = new();

        // 状态
        private bool _isDialogActive = false;
        private EventTreeTemplate _currentTemplate;
        private EventTreeNode _currentNode;

        // 事件
        public event Action<EventTreeTemplate> onTreeStarted;
        public event Action<EventTreeNode> onNodeEntered;
        public event Action onDialogOpened;
        public event Action onDialogClosed;
        public event Action<string> onChoiceSelected;

        #region Properties
        public bool isDialogActive => _isDialogActive;
        public EventTreeTemplate currentTemplate => _currentTemplate;
        public EventTreeNode currentNode => _currentNode;
        #endregion

        #region Public API

        /// <summary>
        /// 设置对话框引用
        /// </summary>
        public void SetDialogReference(UI.Dialog.UISelectionDialogEx dialog, UI.Dialog.CharacterPortrait characterPortrait = null)
        {
            _dialog = dialog;
            _characterPortrait = characterPortrait;

            if (_dialog != null)
            {
                _dialog.onOptionSelected += OnOptionSelected;
                _dialog.onDialogClosed += OnDialogClosed;
            }
        }

        /// <summary>
        /// 开始事件树对话
        /// </summary>
        public void StartDialog(string templateId)
        {
            var template = EventTreeManager.GetTemplate(templateId);
            if (template == null)
            {
                Debug.LogWarning($"[EventTreeDialogRunner] Template not found: {templateId}");
                return;
            }

            StartDialog(template);
        }

        /// <summary>
        /// 开始事件树对话（从模板）
        /// </summary>
        public void StartDialog(EventTreeTemplate template)
        {
            if (template == null || template.nodes.Count == 0)
            {
                Debug.LogWarning("[EventTreeDialogRunner] Invalid template");
                return;
            }

            _currentTemplate = template;
            var startNode = template.GetStartNode();

            if (startNode == null)
            {
                Debug.LogWarning("[EventTreeDialogRunner] No start node found");
                return;
            }

            _isDialogActive = true;
            onTreeStarted?.Invoke(template);

            EnterNode(startNode);
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        public void SelectChoice(string choiceId)
        {
            if (!_isDialogActive || _currentNode == null)
            {
                Debug.LogWarning("[EventTreeDialogRunner] No active dialog");
                return;
            }

            var choice = FindChoice(_currentNode, choiceId);
            if (choice == null)
            {
                Debug.LogWarning($"[EventTreeDialogRunner] Choice not found: {choiceId}");
                return;
            }

            onChoiceSelected?.Invoke(choiceId);

            // 记录历史
            EventTreeRunner.instance.SelectChoice(choiceId);

            // 获取下一个节点
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                var nextNode = _currentTemplate.GetNode(choice.nextNodeId);
                if (nextNode != null)
                {
                    // 延迟进入下一个节点
                    CoroutineHelper.StartCoroutine(DelayEnterNode(nextNode, config.transitionDuration));
                }
            }
            else
            {
                // 没有下一个节点，结束
                EndDialog();
            }
        }

        /// <summary>
        /// 跳过当前节点
        /// </summary>
        public void SkipNode()
        {
            if (!_isDialogActive) return;

            if (_currentNode.type == EventTreeNodeType.Choice)
            {
                Debug.LogWarning("[EventTreeDialogRunner] Cannot skip while waiting for choice");
                return;
            }

            if (string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                EndDialog();
                return;
            }

            var nextNode = _currentTemplate.GetNode(_currentNode.nextNodeId);
            if (nextNode != null)
            {
                CoroutineHelper.StartCoroutine(DelayEnterNode(nextNode, config.transitionDuration));
            }
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialog()
        {
            if (!_isDialogActive) return;

            _isDialogActive = false;
            _currentTemplate = null;
            _currentNode = null;

            if (_dialog != null)
            {
                _dialog.CloseDialog();
            }

            onDialogClosed?.Invoke();
        }

        /// <summary>
        /// 关闭对话框（对话仍保持状态）
        /// </summary>
        public void HideDialog()
        {
            if (_dialog != null && _dialog.isOpen)
            {
                _dialog.CloseDialog();
            }
        }

        /// <summary>
        /// 注册头像资源
        /// </summary>
        public void RegisterPortrait(string portraitId, UnityEngine.Sprite portrait)
        {
            _portraitCache[portraitId] = portrait;
        }

        /// <summary>
        /// 加载头像资源
        /// </summary>
        public UnityEngine.Sprite LoadPortrait(string portraitId)
        {
            if (_portraitCache.TryGetValue(portraitId, out var portrait))
            {
                return portrait;
            }

            // TODO: 从 Resources 或 Addressables 加载
            return null;
        }

        #endregion

        #region Private Methods

        private void EnterNode(EventTreeNode node)
        {
            if (node == null) return;

            _currentNode = node;
            onNodeEntered?.Invoke(node);

            // 根据节点类型处理
            switch (node.type)
            {
                case EventTreeNodeType.Choice:
                    ShowChoiceNode(node);
                    break;

                case EventTreeNodeType.Story:
                case EventTreeNodeType.Combat:
                case EventTreeNodeType.Trade:
                case EventTreeNodeType.Reward:
                    ShowStoryNode(node);
                    break;

                case EventTreeNodeType.Root:
                    ShowRootNode(node);
                    break;

                case EventTreeNodeType.End:
                    EndDialog();
                    break;
            }
        }

        private IEnumerator DelayEnterNode(EventTreeNode node, float delay)
        {
            // 等待过渡动画
            yield return new WaitForSeconds(delay);

            EnterNode(node);
        }

        private void ShowChoiceNode(EventTreeNode node)
        {
            if (_dialog == null) return;

            // 转换为 ChoiceOption
            var options = ConvertToChoiceOptions(node.choices);

            // 显示对话框
            var dialogData = new UI.Dialog.DialogEventData
            {
                title = node.title,
                description = node.description,
                options = options,
                animationType = UI.Dialog.DialogAnimationType.Typewriter
            };

            _dialog.ShowDialog(dialogData);
            onDialogOpened?.Invoke();
        }

        private void ShowStoryNode(EventTreeNode node)
        {
            if (_dialog == null)
            {
                // 没有对话框，自动跳转
                CoroutineHelper.StartCoroutine(DelayEnterNode(_currentTemplate.GetNode(node.nextNodeId), 1f));
                return;
            }

            // 转换为选项（只有一个"继续"选项）
            var options = new List<ChoiceOption>
            {
                ChoiceOption.Create("continue", "继续", ChoiceType.Normal)
            };

            var dialogData = new UI.Dialog.DialogEventData
            {
                title = node.title,
                description = node.description,
                options = options,
                animationType = UI.Dialog.DialogAnimationType.Typewriter
            };

            _dialog.ShowDialog(dialogData);
            onDialogOpened?.Invoke();
        }

        private void ShowRootNode(EventTreeNode node)
        {
            // 根节点直接进入下一个节点
            if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                var nextNode = _currentTemplate.GetNode(node.nextNodeId);
                if (nextNode != null)
                {
                    EnterNode(nextNode);
                }
            }
            else
            {
                EndDialog();
            }
        }

        private List<ChoiceOption> ConvertToChoiceOptions(List<EventTreeChoice> choices)
        {
            var options = new List<ChoiceOption>();

            if (choices == null) return options;

            foreach (var choice in choices)
            {
                var option = ChoiceOption.Create(choice.id, choice.text, ChoiceType.Normal);

                // 检查条件
                if (choice.conditions != null && choice.conditions.Count > 0)
                {
                    option.isEnabled = CheckConditions(choice.conditions);
                }

                options.Add(option);
            }

            return options;
        }

        private bool CheckConditions(List<EventTreeCondition> conditions)
        {
            // TODO: 实现条件检查逻辑
            return true;
        }

        private EventTreeChoice FindChoice(EventTreeNode node, string choiceId)
        {
            if (node?.choices == null) return null;

            foreach (var choice in node.choices)
            {
                if (choice.id == choiceId)
                    return choice;
            }

            return null;
        }

        private void OnOptionSelected(ChoiceOption option)
        {
            if (option == null) return;

            SelectChoice(option.optionId);
        }

        private void OnDialogClosed()
        {
            onDialogClosed?.Invoke();
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// 协程帮助类（简化版，不依赖 MonoBehaviour）
        /// </summary>
        private static class CoroutineHelper
        {
            private static List<CoroutineInfo> _coroutines = new();
            private static bool _isInitialized = false;

            public static CoroutineHandle StartCoroutine(IEnumerator enumerator)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    // 在游戏启动时自动初始化
                }

                var handle = new CoroutineHandle();
                _coroutines.Add(new CoroutineInfo { handle = handle, enumerator = enumerator });
                return handle;
            }

            public static void StopCoroutine(CoroutineHandle handle)
            {
                _coroutines.RemoveAll(c => c.handle.id == handle.id);
            }

            public static void Update()
            {
                for (int i = _coroutines.Count - 1; i >= 0; i--)
                {
                    if (!_coroutines[i].enumerator.MoveNext())
                    {
                        _coroutines.RemoveAt(i);
                    }
                }
            }

            private struct CoroutineInfo
            {
                public CoroutineHandle handle;
                public IEnumerator enumerator;
            }
        }

        /// <summary>
        /// 协程句柄
        /// </summary>
        public struct CoroutineHandle
        {
            internal int id;
        }

        #endregion
    }

    /// <summary>
    /// 事件树对话管理器
    /// 挂在场景中的 MonoBehaviour，负责协程更新
    /// </summary>
    public class EventTreeDialogManager : MonoBehaviour
    {
        [Header("对话框引用")]
        public UI.Dialog.UISelectionDialogEx dialog;
        public UI.Dialog.CharacterPortrait characterPortrait;

        [Header("配置")]
        public EventTreeDialogConfig config = new();

        private void Awake()
        {
            // 设置单例
            EventTreeDialogRunner.instance.SetDialogReference(dialog, characterPortrait);
            EventTreeRunner.instance.onTreeStarted += OnTreeStarted;
            EventTreeRunner.instance.onWaitingForChoice += OnWaitingForChoice;
            EventTreeRunner.instance.onTreeCompleted += OnTreeCompleted;
        }

        private void OnDestroy()
        {
            EventTreeRunner.instance.onTreeStarted -= OnTreeStarted;
            EventTreeRunner.instance.onWaitingForChoice -= OnWaitingForChoice;
            EventTreeRunner.instance.onTreeCompleted -= OnTreeCompleted;
        }

        private void Update()
        {
            // 更新协程
            EventTreeDialogRunner_CoroutineUpdater.Update();
        }

        #region Event Handlers

        private void OnTreeStarted(EventTreeTemplate template)
        {
            EventTreeDialogRunner.instance.StartDialog(template);
        }

        private void OnWaitingForChoice(List<EventTreeChoice> choices)
        {
            // 等待用户选择，由 UISelectionDialogEx 处理
        }

        private void OnTreeCompleted()
        {
            EventTreeDialogRunner.instance.EndDialog();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 开始事件树对话
        /// </summary>
        public void StartTreeDialog(string templateId)
        {
            EventTreeDialogRunner.instance.StartDialog(templateId);
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        public void SelectChoice(string choiceId)
        {
            EventTreeDialogRunner.instance.SelectChoice(choiceId);
        }

        /// <summary>
        /// 跳过当前节点
        /// </summary>
        public void SkipNode()
        {
            EventTreeDialogRunner.instance.SkipNode();
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialog()
        {
            EventTreeDialogRunner.instance.EndDialog();
        }

        #endregion
    }

    /// <summary>
    /// 协程更新器（需要挂载在场景中）
    /// </summary>
    public static class EventTreeDialogRunner_CoroutineUpdater
    {
        private static List<IEnumerator> _coroutines = new();

        public static void AddCoroutine(IEnumerator enumerator)
        {
            _coroutines.Add(enumerator);
        }

        public static void Update()
        {
            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                if (!_coroutines[i].MoveNext())
                {
                    _coroutines.RemoveAt(i);
                }
            }
        }

        public static void Clear()
        {
            _coroutines.Clear();
        }
    }
}