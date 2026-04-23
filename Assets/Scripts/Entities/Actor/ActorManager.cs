using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 角色类型枚举
    /// </summary>
    public enum ActorType
    {
        Player,
        Enemy,
        NPC,
        Boss
    }

    /// <summary>
    /// 角色交互类型
    /// </summary>
    public enum InteractionType
    {
        None,
        Trade,
        Story,
        Combat
    }

    /// <summary>
    /// 角色模板数据（配置）
    /// </summary>
    [Serializable]
    public class ActorTemplate
    {
        public string id;              // 唯一标识，如 Enemy.Bandit
        public string nameTextId;      // 名称文本ID
        public string descTextId;       // 描述文本ID
        public ActorType type;          // 角色类型
        public int maxHp;              // 最大生命值
        public int attack;             // 攻击力
        public int defense;            // 护甲值
        public float speed;            // 速度

        // 敌人特有属性
        public int goldReward;         // 金币奖励
        public int expReward;          // 经验奖励

        // NPC特有属性
        public InteractionType interactionType;  // 交互类型

        /// <summary>
        /// 从 XML 元素解析
        /// </summary>
        public static ActorTemplate ParseFromXml(XmlElement element)
        {
            var template = new ActorTemplate
            {
                id = element.GetAttribute("id"),
                nameTextId = element.SelectSingleNode("nameTextId")?.InnerText ?? string.Empty,
                descTextId = element.SelectSingleNode("descTextId")?.InnerText ?? string.Empty,
                type = ParseActorType(element.SelectSingleNode("type")?.InnerText),
                maxHp = ParseInt(element.SelectSingleNode("maxHp"), 20),
                attack = ParseInt(element.SelectSingleNode("attack"), 0),
                defense = ParseInt(element.SelectSingleNode("defense"), 0),
                speed = ParseFloat(element.SelectSingleNode("speed"), 1f),
                goldReward = ParseInt(element.SelectSingleNode("goldReward"), 0),
                expReward = ParseInt(element.SelectSingleNode("expReward"), 0),
                interactionType = ParseInteractionType(element.SelectSingleNode("interactionType")?.InnerText)
            };

            return template;
        }

        private static ActorType ParseActorType(string value)
        {
            if (string.IsNullOrEmpty(value)) return ActorType.Enemy;
            return value switch
            {
                "Player" => ActorType.Player,
                "Enemy" => ActorType.Enemy,
                "NPC" => ActorType.NPC,
                "Boss" => ActorType.Boss,
                _ => ActorType.Enemy
            };
        }

        private static InteractionType ParseInteractionType(string value)
        {
            if (string.IsNullOrEmpty(value)) return InteractionType.None;
            return value switch
            {
                "Trade" => InteractionType.Trade,
                "Story" => InteractionType.Story,
                "Combat" => InteractionType.Combat,
                _ => InteractionType.None
            };
        }

        private static int ParseInt(XmlNode node, int defaultValue)
        {
            if (node == null || string.IsNullOrEmpty(node.InnerText))
                return defaultValue;
            return int.TryParse(node.InnerText, out int result) ? result : defaultValue;
        }

        private static float ParseFloat(XmlNode node, float defaultValue)
        {
            if (node == null || string.IsNullOrEmpty(node.InnerText))
                return defaultValue;
            return float.TryParse(node.InnerText, out float result) ? result : defaultValue;
        }
    }

    /// <summary>
    /// 角色模板管理器
    /// 负责从XML加载角色配置
    /// </summary>
    public static class ActorManager
    {
        private static readonly Dictionary<string, ActorTemplate> _templates = new();
        private static bool _isLoaded = false;

        /// <summary>
        /// 模板是否已加载
        /// </summary>
        public static bool isLoaded => _isLoaded;

        /// <summary>
        /// 初始化（由 GameMain 在启动时调用）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_isLoaded) return;
            LoadTemplates();
            _isLoaded = true;
        }

        /// <summary>
        /// 加载所有角色模板
        /// </summary>
        private static void LoadTemplates()
        {
            var path = "Data/Actors/Actors";
            try
            {
                var loadedTemplates = ResourceManager.LoadXmlArray<ActorTemplate>(path);
                if (loadedTemplates == null || loadedTemplates.Length == 0)
                {
                    Debug.LogWarning("[ActorManager] No actors loaded from: " + path);
                    return;
                }

                foreach (var template in loadedTemplates)
                {
                    if (string.IsNullOrEmpty(template.id))
                    {
                        Debug.LogWarning($"[ActorManager] Skip template with empty id");
                        continue;
                    }

                    if (_templates.ContainsKey(template.id))
                    {
                        Debug.LogWarning($"[ActorManager] Duplicate template id: {template.id}");
                        continue;
                    }

                    _templates[template.id] = template;
                    Debug.Log($"[ActorManager] Loaded actor: {template.id} ({template.type})");
                }

                Debug.Log($"[ActorManager] Total actors loaded: {_templates.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActorManager] Failed to load actors: {e.Message}");
            }
        }

        /// <summary>
        /// 获取角色模板
        /// </summary>
        public static ActorTemplate GetTemplate(string templateId)
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
        /// 按类型获取模板
        /// </summary>
        public static List<ActorTemplate> GetTemplatesByType(ActorType type)
        {
            var result = new List<ActorTemplate>();
            foreach (var template in _templates.Values)
            {
                if (template.type == type)
                    result.Add(template);
            }
            return result;
        }

        /// <summary>
        /// 获取敌人模板（用于战斗）
        /// </summary>
        public static ActorTemplate GetEnemyTemplate(int difficulty)
        {
            var enemies = GetTemplatesByType(ActorType.Enemy);
            if (enemies.Count == 0) return null;

            // 根据难度选择
            int index = Mathf.Clamp(difficulty - 1, 0, enemies.Count - 1);
            return enemies[index];
        }

        /// <summary>
        /// 获取Boss模板
        /// </summary>
        public static ActorTemplate GetBossTemplate()
        {
            var bosses = GetTemplatesByType(ActorType.Boss);
            return bosses.Count > 0 ? bosses[0] : null;
        }
    }
}