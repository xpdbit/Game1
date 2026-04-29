using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Game1.Events.Effect;

namespace Game1
{
    /// <summary>
    /// 事件树节点类型
    /// </summary>
    public enum EventTreeNodeType
    {
        Story,      // 剧情节点
        Choice,     // 选择节点
        Combat,     // 战斗节点
        Trade,      // 交易节点
        Reward,     // 奖励节点
        Root,       // 根节点
        End         // 结束节点
    }

    /// <summary>
    /// 事件树节点条件
    /// </summary>
    [Serializable]
    public class EventTreeCondition
    {
        public string type;      // 条件类型：flag, item, gold, module
        public string key;      // 条件键
        public string value;    // 条件值
        public string comparison; // 比较方式：equals, greater, less

        public static EventTreeCondition ParseFromXml(XmlElement element)
        {
            return new EventTreeCondition
            {
                type = element.GetAttribute("type") ?? "flag",
                key = element.GetAttribute("key") ?? string.Empty,
                value = element.GetAttribute("value") ?? string.Empty,
                comparison = element.GetAttribute("comparison") ?? "equals"
            };
        }
    }

    /// <summary>
    /// 事件树选择项
    /// </summary>
    [Serializable]
    public class EventTreeChoice
    {
        public string id;               // 选择ID
        public string text;            // 显示文本
        public string nextNodeId;      // 下一个节点ID
        public List<Effect> effects;   // 选择效果
        public List<EventTreeCondition> conditions; // 选择条件

        public List<UnifiedEffect> GetUnifiedEffects()
        {
            // Convert old effects lazily
            if (_unifiedEffects == null)
            {
                _unifiedEffects = new List<UnifiedEffect>();
                foreach (var oldEffect in effects)
                {
                    var legacyStr = $"{oldEffect.type.ToString().ToLowerInvariant()}:{oldEffect.value}";
                    if (oldEffect.quantity > 1)
                        legacyStr += $":{oldEffect.quantity}";
                    var unified = EffectParser.ParseLegacyString(legacyStr);
                    _unifiedEffects.Add(unified);
                }
                // Also parse the raw XML if available (for EventTree attributes like goldCost, setFlag)
                if (_choiceNode != null)
                {
                    var attrEffects = EffectParser.ParseEventTreeChoiceEffects(_choiceNode);
                    _unifiedEffects.AddRange(attrEffects);
                }
            }
            return _unifiedEffects;
        }
        private List<UnifiedEffect> _unifiedEffects;
        private XmlNode _choiceNode; // store the original XML node if available

        public static EventTreeChoice ParseFromXml(XmlElement element)
        {
            var choice = new EventTreeChoice
            {
                id = element.GetAttribute("id") ?? Guid.NewGuid().ToString(),
                text = element.SelectSingleNode("text")?.InnerText ?? string.Empty,
                nextNodeId = element.SelectSingleNode("nextNodeId")?.InnerText ?? string.Empty,
                effects = new List<Effect>(),
                conditions = new List<EventTreeCondition>()
            };

            // 解析平铺字段格式的effects (goldCost, setFlag, addModuleIds)
            // goldCost -> gold effect
            var goldCostNode = element.SelectSingleNode("goldCost");
            if (goldCostNode != null && int.TryParse(goldCostNode.InnerText, out var goldCost) && goldCost > 0)
            {
                choice.effects.Add(new Effect
                {
                    type = EffectType.Gold,
                    value = "-" + goldCost,
                    target = "player",
                    quantity = 1
                });
            }

            // setFlag -> flag effect
            var setFlagNode = element.SelectSingleNode("setFlag");
            if (setFlagNode != null && !string.IsNullOrEmpty(setFlagNode.InnerText))
            {
                choice.effects.Add(new Effect
                {
                    type = EffectType.Flag,
                    value = "set:" + setFlagNode.InnerText.Trim(),
                    target = "player",
                    quantity = 1
                });
            }

            // addModuleIds/moduleId -> module effect
            var moduleIdNodes = element.SelectNodes("addModuleIds/moduleId");
            if (moduleIdNodes != null)
            {
                foreach (XmlNode moduleIdNode in moduleIdNodes)
                {
                    if (!string.IsNullOrEmpty(moduleIdNode.InnerText))
                    {
                        choice.effects.Add(new Effect
                        {
                            type = EffectType.Module,
                            value = moduleIdNode.InnerText.Trim(),
                            target = "player",
                            quantity = 1
                        });
                    }
                }
            }

            // 解析嵌套effects/Effect结构 (向后兼容)
            var effectsNodes = element.SelectNodes("effects/Effect");
            if (effectsNodes != null)
            {
                foreach (XmlNode effectNode in effectsNodes)
                {
                    if (effectNode is XmlElement effectElement)
                    {
                        choice.effects.Add(Effect.ParseFromXml(effectElement));
                    }
                }
            }

            // 解析conditions
            var conditionsNodes = element.SelectNodes("conditions/Condition");
            if (conditionsNodes != null)
            {
                foreach (XmlNode condNode in conditionsNodes)
                {
                    if (condNode is XmlElement condElement)
                    {
                        choice.conditions.Add(EventTreeCondition.ParseFromXml(condElement));
                    }
                }
            }

            return choice;
        }
    }

    /// <summary>
    /// 事件树节点
    /// </summary>
    [Serializable]
    public class EventTreeNode
    {
        public string id;                       // 节点ID
        public EventTreeNodeType type;          // 节点类型
        public string title;                   // 节点标题
        public string description;              // 节点描述
        public List<EventTreeChoice> choices;   // 选择列表
        public string nextNodeId;              // 默认下一个节点（用于非选择节点）
        public List<EventTreeCondition> conditions; // 进入条件
        public List<Effect> rewards;           // 节点奖励
        public bool isStartNode;               // 是否为起始节点

        public List<UnifiedEffect> GetUnifiedRewards()
        {
            if (_unifiedRewards == null)
            {
                _unifiedRewards = new List<UnifiedEffect>();
                foreach (var oldEffect in rewards)
                {
                    var legacyStr = $"{oldEffect.type.ToString().ToLowerInvariant()}:{oldEffect.value}";
                    var unified = EffectParser.ParseLegacyString(legacyStr);
                    _unifiedRewards.Add(unified);
                }
            }
            return _unifiedRewards;
        }
        private List<UnifiedEffect> _unifiedRewards;

        public static EventTreeNode ParseFromXml(XmlElement element)
        {
            var node = new EventTreeNode
            {
                id = element.GetAttribute("id") ?? string.Empty,
                title = element.SelectSingleNode("title")?.InnerText ?? string.Empty,
                description = element.SelectSingleNode("description")?.InnerText ?? string.Empty,
                nextNodeId = element.SelectSingleNode("nextNodeId")?.InnerText ?? string.Empty,
                choices = new List<EventTreeChoice>(),
                conditions = new List<EventTreeCondition>(),
                rewards = new List<Effect>(),
                isStartNode = false
            };

            // 解析type
            var typeNode = element.SelectSingleNode("type");
            if (typeNode != null)
            {
                if (Enum.TryParse<EventTreeNodeType>(typeNode.InnerText, true, out var nodeType))
                {
                    node.type = nodeType;
                }
            }

            // 解析isStartNode
            var startNode = element.SelectSingleNode("isStartNode");
            if (startNode != null)
            {
                node.isStartNode = startNode.InnerText.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // 解析choices
            var choiceNodes = element.SelectNodes("choices/Choice");
            if (choiceNodes != null)
            {
                foreach (XmlNode choiceNode in choiceNodes)
                {
                    if (choiceNode is XmlElement choiceElement)
                    {
                        node.choices.Add(EventTreeChoice.ParseFromXml(choiceElement));
                    }
                }
            }

            // 解析conditions
            var condNodes = element.SelectNodes("conditions/Condition");
            if (condNodes != null)
            {
                foreach (XmlNode condNode in condNodes)
                {
                    if (condNode is XmlElement condElement)
                    {
                        node.conditions.Add(EventTreeCondition.ParseFromXml(condElement));
                    }
                }
            }

            // 解析rewards
            var rewardNodes = element.SelectNodes("rewards/Effect");
            if (rewardNodes != null)
            {
                foreach (XmlNode rewardNode in rewardNodes)
                {
                    if (rewardNode is XmlElement rewardElement)
                    {
                        node.rewards.Add(Effect.ParseFromXml(rewardElement));
                    }
                }
            }

            return node;
        }
    }

    /// <summary>
    /// 事件树模板
    /// </summary>
    [Serializable]
    public class EventTreeTemplate
    {
        public string id;                              // 唯一标识
        public string name;                            // 事件树名称
        public string description;                     // 描述
        public string rootNodeId;                      // 根节点ID
        public Dictionary<string, EventTreeNode> nodes; // 节点字典

        public EventTreeTemplate()
        {
            nodes = new Dictionary<string, EventTreeNode>();
        }

        /// <summary>
        /// 从 XML 元素解析
        /// </summary>
        public static EventTreeTemplate ParseFromXml(XmlElement element)
        {
            var template = new EventTreeTemplate
            {
                id = element.SelectSingleNode("id")?.InnerText ?? string.Empty,
                name = element.SelectSingleNode("name")?.InnerText ?? string.Empty,
                description = element.SelectSingleNode("description")?.InnerText ?? string.Empty,
                rootNodeId = element.SelectSingleNode("rootNodeId")?.InnerText ?? string.Empty,
                nodes = new Dictionary<string, EventTreeNode>()
            };

            // 解析nodes
            var nodeNodes = element.SelectNodes("nodes/Node");
            if (nodeNodes != null)
            {
                foreach (XmlNode nodeNode in nodeNodes)
                {
                    if (nodeNode is XmlElement nodeElement)
                    {
                        var node = EventTreeNode.ParseFromXml(nodeElement);
                        if (!string.IsNullOrEmpty(node.id))
                        {
                            template.nodes[node.id] = node;
                        }
                    }
                }
            }

            // 如果没有指定rootNodeId，尝试找isStartNode为true的节点
            if (string.IsNullOrEmpty(template.rootNodeId))
            {
                foreach (var kvp in template.nodes)
                {
                    if (kvp.Value.isStartNode)
                    {
                        template.rootNodeId = kvp.Key;
                        break;
                    }
                }
            }

            return template;
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        public EventTreeNode GetNode(string nodeId)
        {
            return nodes.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>
        /// 获取起始节点
        /// </summary>
        public EventTreeNode GetStartNode()
        {
            return GetNode(rootNodeId);
        }
    }

    /// <summary>
    /// 事件树管理器
    /// 负责事件树模板加载
    /// </summary>
    public static class EventTreeManager
    {
        /// <summary>
        /// 事件树模板字典（只读配置）
        /// </summary>
        private static readonly Dictionary<string, EventTreeTemplate> _templates = new();

        /// <summary>
        /// 模板是否已加载
        /// </summary>
        private static bool _isLoaded = false;

        /// <summary>
        /// 初始化（由 RuntimeInitializeOnLoadMethod 在启动时调用）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_isLoaded) return;
            LoadTemplates();
            _isLoaded = true;
        }

        /// <summary>
        /// 加载所有事件树模板
        /// </summary>
        private static void LoadTemplates()
        {
            var xml = ResourceManager.LoadXml("Data/EventTrees/EventTrees");
            if (string.IsNullOrEmpty(xml))
            {
                Debug.LogWarning("[EventTreeManager] No EventTrees.xml found at Data/EventTrees/EventTrees");
                return;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var treeNodes = doc.SelectNodes("/EventTrees/EventTree");
                if (treeNodes == null || treeNodes.Count == 0)
                {
                    Debug.LogWarning("[EventTreeManager] No EventTree nodes found in EventTrees.xml");
                    return;
                }

                foreach (XmlNode treeNode in treeNodes)
                {
                    if (treeNode is XmlElement treeElement)
                    {
                        try
                        {
                            var template = EventTreeTemplate.ParseFromXml(treeElement);
                            if (string.IsNullOrEmpty(template.id))
                            {
                                Debug.LogWarning("[EventTreeManager] Skip template with empty id");
                                continue;
                            }

                            if (_templates.ContainsKey(template.id))
                            {
                                Debug.LogWarning($"[EventTreeManager] Duplicate template id: {template.id}");
                                continue;
                            }

                            _templates[template.id] = template;
                            Debug.Log($"[EventTreeManager] Loaded template: {template.id}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[EventTreeManager] Failed to parse EventTree node: {ex.Message}");
                            continue;
                        }
                    }
                }

                Debug.Log($"[EventTreeManager] Total templates loaded: {_templates.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventTreeManager] Failed to load event trees: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取事件树模板
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>模板数据，不存在返回null</returns>
        public static EventTreeTemplate GetTemplate(string templateId)
        {
            return _templates.TryGetValue(templateId, out var template) ? template : null;
        }

        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        public static bool HasTemplate(string templateId)
        {
            return _templates.ContainsKey(templateId);
        }

        /// <summary>
        /// 获取所有模板ID
        /// </summary>
        public static IReadOnlyCollection<string> GetAllTemplateIds()
        {
            return _templates.Keys;
        }

        /// <summary>
        /// 获取所有事件树模板
        /// </summary>
        public static List<EventTreeTemplate> GetAllTemplates()
        {
            return new List<EventTreeTemplate>(_templates.Values);
        }

        /// <summary>
        /// 随机获取一个事件树模板
        /// </summary>
        public static EventTreeTemplate GetRandomTemplate()
        {
            if (_templates.Count == 0) return null;
            var randomIndex = UnityEngine.Random.Range(0, _templates.Count);
            int i = 0;
            foreach (var template in _templates.Values)
            {
                if (i == randomIndex) return template;
                i++;
            }
            return null;
        }
    }
}