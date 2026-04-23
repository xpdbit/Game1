using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 事件链节点
    /// 表示事件序列中的一个事件
    /// </summary>
    [Serializable]
    public class EventChainNode
    {
        public string nodeId;                  // 节点ID
        public string eventId;                // 关联的事件ID
        public string title;                   // 显示标题
        public string description;            // 描述
        public List<EventChoice> choices;    // 选项列表
        public bool isOptional = true;        // 是否可选（跳过）
        public float skipDelay = 0f;          // 跳过延迟（秒），0表示立即
    }

    /// <summary>
    /// 事件选项
    /// </summary>
    [Serializable]
    public class EventChoice
    {
        public string choiceId;               // 选项ID
        public string text;                   // 显示文本
        public ChoiceType choiceType;        // 选项类型
        public string nextNodeId;            // 下一个节点ID（null表示结束）
        public int goldCost;                 // 金币消耗
        public int requiredItemId;           // 需要物品
        public string requiredFlag;           // 需要标志
        public string setFlag;               // 选择后设置的标志
        public List<string> addModuleIds;    // 添加的模块ID
        public List<string> removeModuleIds; // 移除的模块ID
    }

    /// <summary>
    /// 事件链
    /// 管理连续事件的执行
    /// </summary>
    [Serializable]
    public class EventChain
    {
        public string chainId;               // 事件链ID
        public string title;                  // 事件链标题
        public List<EventChainNode> nodes;    // 节点列表
        public string startNodeId;            // 起始节点ID
        public string currentNodeId;           // 当前节点ID
        public Dictionary<string, string> flags = new();  // 事件标志
        public bool isCompleted;

        public EventChain()
        {
            nodes = new List<EventChainNode>();
            flags = new Dictionary<string, string>();
        }

        /// <summary>
        /// 获取当前节点
        /// </summary>
        public EventChainNode GetCurrentNode()
        {
            return nodes.Find(n => n.nodeId == currentNodeId);
        }

        /// <summary>
        /// 获取下一个节点
        /// </summary>
        public EventChainNode GetNextNode(string nextNodeId)
        {
            return nodes.Find(n => n.nodeId == nextNodeId);
        }

        /// <summary>
        /// 选择选项并前进
        /// </summary>
        public EventChoice SelectChoice(string choiceId)
        {
            var currentNode = GetCurrentNode();
            if (currentNode == null) return null;

            var choice = currentNode.choices.Find(c => c.choiceId == choiceId);
            if (choice == null) return null;

            // 设置标志
            if (!string.IsNullOrEmpty(choice.setFlag))
            {
                flags[choice.setFlag] = choiceId;
            }

            // 移动到下一个节点
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                currentNodeId = choice.nextNodeId;
            }
            else
            {
                // 没有下一个节点，事件链结束
                isCompleted = true;
            }

            return choice;
        }

        /// <summary>
        /// 检查选项是否可用
        /// </summary>
        public bool IsChoiceAvailable(EventChoice choice, PlayerActor player)
        {
            // 检查金币
            if (choice.goldCost > 0 && player.carryItems.gold < choice.goldCost)
                return false;

            // 检查标志
            if (!string.IsNullOrEmpty(choice.requiredFlag))
            {
                if (!flags.ContainsKey(choice.requiredFlag))
                    return false;
            }

            // 检查物品
            if (choice.requiredItemId > 0)
            {
                // 通过 ItemManager 检查背包中是否有指定物品
                var items = ItemManager.GetItemsByTemplateId(choice.requiredItemId.ToString());
                if (items == null || items.Count == 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 重置事件链
        /// </summary>
        public void Reset()
        {
            currentNodeId = startNodeId;
            flags.Clear();
            isCompleted = false;
        }
    }

    /// <summary>
    /// 事件链管理器
    /// </summary>
    public class EventChainManager
    {
        #region Singleton
        private static EventChainManager _instance;
        public static EventChainManager instance => _instance ??= new EventChainManager();
        #endregion

        private readonly List<EventChain> _activeChains = new();
        private EventChain _currentChain;

        public EventChain currentChain => _currentChain;
        public bool hasActiveChain => _currentChain != null && !_currentChain.isCompleted;

        // 事件
        public event Action<EventChain> onChainStarted;
        public event Action<EventChainNode> onNodeEntered;
        public event Action<EventChoice> onChoiceSelected;
        public event Action<EventChain> onChainCompleted;

        /// <summary>
        /// 开始事件链
        /// </summary>
        public void StartChain(EventChain chain)
        {
            if (chain == null || chain.nodes.Count == 0) return;

            _currentChain = chain;
            chain.Reset();
            _activeChains.Add(chain);

            onChainStarted?.Invoke(chain);
            EnterNode(chain.startNodeId);
        }

        /// <summary>
        /// 进入节点
        /// </summary>
        private void EnterNode(string nodeId)
        {
            if (_currentChain == null) return;

            _currentChain.currentNodeId = nodeId;
            var node = _currentChain.GetCurrentNode();

            if (node != null)
            {
                onNodeEntered?.Invoke(node);
            }
        }

        /// <summary>
        /// 选择选项
        /// </summary>
        public void SelectChoice(string choiceId)
        {
            if (_currentChain == null) return;

            var choice = _currentChain.SelectChoice(choiceId);
            if (choice == null) return;

            onChoiceSelected?.Invoke(choice);

            // 应用选择效果
            ApplyChoiceEffects(choice);

            // 进入下一个节点或结束
            if (!_currentChain.isCompleted)
            {
                EnterNode(_currentChain.currentNodeId);
            }
            else
            {
                CompleteChain();
            }
        }

        /// <summary>
        /// 跳过当前节点（如果允许）
        /// </summary>
        public void SkipCurrentNode()
        {
            if (_currentChain == null) return;

            var currentNode = _currentChain.GetCurrentNode();
            if (currentNode == null || !currentNode.isOptional) return;

            // 查找默认选项
            var defaultChoice = currentNode.choices.Count > 0 ? currentNode.choices[0] : null;
            if (defaultChoice != null && !string.IsNullOrEmpty(defaultChoice.nextNodeId))
            {
                _currentChain.currentNodeId = defaultChoice.nextNodeId;
                EnterNode(_currentChain.currentNodeId);
            }
            else
            {
                CompleteChain();
            }
        }

        /// <summary>
        /// 应用选择效果
        /// </summary>
        private void ApplyChoiceEffects(EventChoice choice)
        {
            var player = GameMain.instance?.playerActor;
            if (player == null) return;

            // 消耗金币
            if (choice.goldCost > 0)
            {
                player.carryItems.gold -= choice.goldCost;
            }

            // 添加模块
            if (choice.addModuleIds != null)
            {
                foreach (var moduleId in choice.addModuleIds)
                {
                    // 根据moduleId创建模块并添加
                    // moduleId格式与IModule.moduleId一致
                    IModule module = moduleId switch
                    {
                        "idle_reward" => new IdleRewardModule(),
                        _ => null
                    };

                    if (module != null)
                    {
                        // 先初始化模块（如果需要）
                        if (module is IdleRewardModule idleModule)
                        {
                            idleModule.Initialize(player);
                        }
                        // 再添加到玩家
                        player.AddModule(module);
                    }
                    else
                    {
                        Debug.LogWarning($"[EventChain] Unknown moduleId: {moduleId}");
                    }
                }
            }

            // 移除模块
            if (choice.removeModuleIds != null)
            {
                foreach (var moduleId in choice.removeModuleIds)
                {
                    player.RemoveModule(moduleId);
                }
            }
        }

        /// <summary>
        /// 完成事件链
        /// </summary>
        private void CompleteChain()
        {
            if (_currentChain == null) return;

            _currentChain.isCompleted = true;
            onChainCompleted?.Invoke(_currentChain);

            // 从活动列表移除
            _activeChains.Remove(_currentChain);
            _currentChain = null;
        }

        /// <summary>
        /// 取消当前事件链
        /// </summary>
        public void CancelChain()
        {
            if (_currentChain != null)
            {
                _activeChains.Remove(_currentChain);
                _currentChain = null;
            }
        }

        /// <summary>
        /// 清空所有事件链
        /// </summary>
        public void ClearAll()
        {
            _activeChains.Clear();
            _currentChain = null;
        }
    }
}