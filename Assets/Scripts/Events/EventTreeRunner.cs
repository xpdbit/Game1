using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 事件树运行状态
    /// </summary>
    public enum EventTreeState
    {
        Idle,       // 空闲
        Running,    // 运行中
        WaitingChoice, // 等待选择
        Completed,  // 完成
        Cancelled   // 取消
    }

    /// <summary>
    /// 事件树运行器
    /// 负责执行事件树模板，支持分支叙事
    /// </summary>
    public class EventTreeRunner
    {
        #region Singleton
        private static EventTreeRunner _instance;
        public static EventTreeRunner instance => _instance ??= new EventTreeRunner();
        #endregion

        private EventTreeTemplate _currentTemplate;
        private EventTreeNode _currentNode;
        private EventTreeState _state = EventTreeState.Idle;
        private readonly Stack<string> _history = new();

        // 事件
        public event Action<EventTreeTemplate> onTreeStarted;
        public event Action<EventTreeNode> onNodeEntered;
        public event Action<List<EventTreeChoice>> onWaitingForChoice;
        public event Action onTreeCompleted;
        public event Action onTreeCancelled;

        // 属性
        public EventTreeState state => _state;
        public EventTreeTemplate currentTemplate => _currentTemplate;
        public EventTreeNode currentNode => _currentNode;
        public bool isRunning => _state == EventTreeState.Running || _state == EventTreeState.WaitingChoice;

        #region Public API

        /// <summary>
        /// 开始事件树
        /// </summary>
        /// <param name="templateId">事件树模板ID</param>
        /// <returns>是否成功开始</returns>
        public bool StartTree(string templateId)
        {
            var template = EventTreeManager.GetTemplate(templateId);
            if (template == null)
            {
                Debug.LogWarning($"[EventTreeRunner] Template not found: {templateId}");
                return false;
            }

            return StartTree(template);
        }

        /// <summary>
        /// 开始事件树（从模板）
        /// </summary>
        public bool StartTree(EventTreeTemplate template)
        {
            if (template == null || template.nodes.Count == 0)
            {
                Debug.LogWarning("[EventTreeRunner] Invalid template");
                return false;
            }

            _currentTemplate = template;
            _history.Clear();

            var startNode = template.GetStartNode();
            if (startNode == null)
            {
                Debug.LogWarning("[EventTreeRunner] No start node found");
                return false;
            }

            _state = EventTreeState.Running;
            EnterNode(startNode.id);

            onTreeStarted?.Invoke(template);
            return true;
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        /// <param name="choiceId">选项ID</param>
        public void SelectChoice(string choiceId)
        {
            if (_state != EventTreeState.WaitingChoice || _currentNode == null)
            {
                Debug.LogWarning("[EventTreeRunner] Not waiting for choice");
                return;
            }

            var choice = FindChoice(_currentNode, choiceId);
            if (choice == null)
            {
                Debug.LogWarning($"[EventTreeRunner] Choice not found: {choiceId}");
                return;
            }

            // 记录历史
            _history.Push(_currentNode.id);

            // 进入下一个节点
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                EnterNode(choice.nextNodeId);
            }
            else
            {
                // 没有下一个节点，结束
                CompleteTree();
            }
        }

        /// <summary>
        /// 跳过当前节点（如果不是等待选择）
        /// </summary>
        public void SkipNode()
        {
            if (_state == EventTreeState.WaitingChoice)
            {
                Debug.LogWarning("[EventTreeRunner] Cannot skip while waiting for choice");
                return;
            }

            if (_currentNode == null || string.IsNullOrEmpty(_currentNode.nextNodeId))
            {
                CompleteTree();
                return;
            }

            _history.Push(_currentNode.id);
            EnterNode(_currentNode.nextNodeId);
        }

        /// <summary>
        /// 取消当前事件树
        /// </summary>
        public void Cancel()
        {
            if (!isRunning) return;

            _state = EventTreeState.Cancelled;
            var template = _currentTemplate;
            _currentTemplate = null;
            _currentNode = null;

            onTreeCancelled?.Invoke();
        }

        /// <summary>
        /// 返回上一个节点（如果可能）
        /// </summary>
        public void GoBack()
        {
            if (_history.Count == 0)
            {
                Debug.Log("[EventTreeRunner] No history to go back to");
                return;
            }

            var previousNodeId = _history.Pop();
            EnterNode(previousNodeId, false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 进入节点
        /// </summary>
        private void EnterNode(string nodeId, bool pushHistory = true)
        {
            if (_currentTemplate == null) return;

            _currentNode = _currentTemplate.GetNode(nodeId);
            if (_currentNode == null)
            {
                Debug.LogWarning($"[EventTreeRunner] Node not found: {nodeId}");
                CompleteTree();
                return;
            }

            _state = EventTreeState.Running;
            onNodeEntered?.Invoke(_currentNode);

            // 根据节点类型处理
            switch (_currentNode.type)
            {
                case EventTreeNodeType.Choice:
                    // 等待玩家选择
                    _state = EventTreeState.WaitingChoice;
                    onWaitingForChoice?.Invoke(_currentNode.choices);
                    break;

                case EventTreeNodeType.End:
                    // 直接结束
                    CompleteTree();
                    break;

                case EventTreeNodeType.Story:
                case EventTreeNodeType.Combat:
                case EventTreeNodeType.Trade:
                case EventTreeNodeType.Reward:
                    // 自动推进到下一个节点
                    if (!string.IsNullOrEmpty(_currentNode.nextNodeId))
                    {
                        if (pushHistory) _history.Push(_currentNode.id);
                        EnterNode(_currentNode.nextNodeId);
                    }
                    else
                    {
                        CompleteTree();
                    }
                    break;

                case EventTreeNodeType.Root:
                default:
                    // 自动推进
                    if (!string.IsNullOrEmpty(_currentNode.nextNodeId))
                    {
                        if (pushHistory) _history.Push(_currentNode.id);
                        EnterNode(_currentNode.nextNodeId);
                    }
                    else
                    {
                        CompleteTree();
                    }
                    break;
            }
        }

        /// <summary>
        /// 完成事件树
        /// </summary>
        private void CompleteTree()
        {
            _state = EventTreeState.Completed;
            var template = _currentTemplate;

            onTreeCompleted?.Invoke();

            _currentTemplate = null;
            _currentNode = null;
            _history.Clear();
        }

        /// <summary>
        /// 查找选项
        /// </summary>
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

        #endregion

        #region Utility

        /// <summary>
        /// 随机开始一个事件树
        /// </summary>
        public bool StartRandomTree()
        {
            var template = EventTreeManager.GetRandomTemplate();
            if (template == null)
            {
                Debug.LogWarning("[EventTreeRunner] No random template available");
                return false;
            }

            return StartTree(template);
        }

        /// <summary>
        /// 获取当前可用的选项
        /// </summary>
        public List<EventTreeChoice> GetCurrentChoices()
        {
            if (_state != EventTreeState.WaitingChoice || _currentNode == null)
                return new List<EventTreeChoice>();

            return _currentNode.choices ?? new List<EventTreeChoice>();
        }

        #endregion
    }
}