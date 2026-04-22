using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 事件效果类型
    /// </summary>
    public enum EffectType
    {
        Gold,
        Item,
        HP,
        Flag,
        Module,
        Combat
    }

    /// <summary>
    /// 事件效果
    /// </summary>
    [Serializable]
    public class Effect
    {
        public EffectType type;
        public string value;      // 效果值（可以是数值、物品ID、标志名等）
        public string target;     // 目标（player, party, enemy等）
        public int quantity = 1; // 数量

        public static Effect ParseFromXml(XmlElement element)
        {
            return new Effect
            {
                type = (EffectType)Enum.Parse(typeof(EffectType), element.GetAttribute("type") ?? "Gold", true),
                value = element.GetAttribute("value") ?? string.Empty,
                target = element.GetAttribute("target") ?? "player",
                quantity = int.TryParse(element.GetAttribute("quantity"), out var q) ? q : 1
            };
        }
    }

    /// <summary>
    /// 事件模板数据（配置）
    /// </summary>
    [Serializable]
    public class EventTemplate
    {
        public string id;                      // 唯一标识
        public string name;                    // 事件名称
        public GameEventType type;            // 事件类型
        public string description;             // 事件描述
        public List<Effect> effects;           // 效果列表
        public float triggerChance = 1.0f;    // 触发概率
        public bool isRepeatable = true;      // 是否可重复触发

        /// <summary>
        /// 从 XML 元素解析
        /// </summary>
        public static EventTemplate ParseFromXml(XmlElement element)
        {
            var template = new EventTemplate
            {
                id = element.SelectSingleNode("id")?.InnerText ?? string.Empty,
                name = element.SelectSingleNode("name")?.InnerText ?? string.Empty,
                description = element.SelectSingleNode("description")?.InnerText ?? string.Empty,
                type = GameEventType.Random,
                effects = new List<Effect>()
            };

            // 解析type
            var typeNode = element.SelectSingleNode("type");
            if (typeNode != null)
            {
                if (Enum.TryParse<GameEventType>(typeNode.InnerText, true, out var eventType))
                {
                    template.type = eventType;
                }
            }

            // 解析triggerChance
            var chanceNode = element.SelectSingleNode("triggerChance");
            if (chanceNode != null && float.TryParse(chanceNode.InnerText, out var chance))
            {
                template.triggerChance = chance;
            }

            // 解析isRepeatable
            var repeatableNode = element.SelectSingleNode("isRepeatable");
            if (repeatableNode != null)
            {
                template.isRepeatable = repeatableNode.InnerText.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // 解析effects
            var effectsNodes = element.SelectNodes("effects/Effect");
            if (effectsNodes != null)
            {
                foreach (XmlNode effectNode in effectsNodes)
                {
                    if (effectNode is XmlElement effectElement)
                    {
                        template.effects.Add(Effect.ParseFromXml(effectElement));
                    }
                }
            }

            return template;
        }
    }

    /// <summary>
    /// 事件管理器
    /// 负责事件模板加载
    /// </summary>
    public static class EventManager
    {
        /// <summary>
        /// 事件模板字典（只读配置）
        /// </summary>
        private static readonly Dictionary<string, EventTemplate> _templates = new();

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
        /// 加载所有事件模板
        /// </summary>
        private static void LoadTemplates()
        {
            var xml = ResourceManager.LoadXml("Data/Events/Events");
            if (string.IsNullOrEmpty(xml))
            {
                Debug.LogWarning("[EventManager] No Events.xml found at Data/Events/Events");
                return;
            }

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var eventNodes = doc.SelectNodes("/Events/Event");
                if (eventNodes == null || eventNodes.Count == 0)
                {
                    Debug.LogWarning("[EventManager] No Event nodes found in Events.xml");
                    return;
                }

                foreach (XmlNode eventNode in eventNodes)
                {
                    if (eventNode is XmlElement eventElement)
                    {
                        try
                        {
                            var template = EventTemplate.ParseFromXml(eventElement);
                            if (string.IsNullOrEmpty(template.id))
                            {
                                Debug.LogWarning("[EventManager] Skip template with empty id");
                                continue;
                            }

                            if (_templates.ContainsKey(template.id))
                            {
                                Debug.LogWarning($"[EventManager] Duplicate template id: {template.id}");
                                continue;
                            }

                            _templates[template.id] = template;
                            Debug.Log($"[EventManager] Loaded template: {template.id}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[EventManager] Failed to parse Event node: {ex.Message}");
                            continue;
                        }
                    }
                }

                Debug.Log($"[EventManager] Total templates loaded: {_templates.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventManager] Failed to load events: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取事件模板
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>模板数据，不存在返回null</returns>
        public static EventTemplate GetTemplate(string templateId)
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
        /// 按类型获取所有事件模板
        /// </summary>
        public static List<EventTemplate> GetTemplatesByType(GameEventType type)
        {
            var result = new List<EventTemplate>();
            foreach (var template in _templates.Values)
            {
                if (template.type == type)
                {
                    result.Add(template);
                }
            }
            return result;
        }

        /// <summary>
        /// 随机获取一个指定类型的事件模板
        /// </summary>
        public static EventTemplate GetRandomTemplate(GameEventType type)
        {
            var templates = GetTemplatesByType(type);
            if (templates.Count == 0) return null;
            return templates[UnityEngine.Random.Range(0, templates.Count)];
        }

        /// <summary>
        /// 随机获取任意类型的事件模板
        /// </summary>
        public static EventTemplate GetRandomTemplate()
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