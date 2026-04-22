using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// NPC态度类型
    /// </summary>
    public enum NPCType
    {
        Friendly,   // 友善 - 会帮助玩家
        Allied,     // 友方 - 同盟关系
        Hostile,    // 敌方 - 敌对关系
        Neutral     // 中立 - 无特殊关系
    }

    /// <summary>
    /// NPC模板数据（配置）
    /// </summary>
    [Serializable]
    public class NPCTemplate
    {
        public string id;                 // 唯一ID
        public string nameId;            // 名称文本ID
        public NPCType npcType;          // NPC态度类型
        public Sprite portrait;          // 头像
        public string defaultDialogue;   // 默认对话
        public int level;                 // NPC等级
        public float hostileChance = 0.3f;  // 变为敌对的概率（Neutral类型）

        // 战斗属性
        public int baseHp = 20;
        public int baseArmor = 5;
        public int baseDamage = 3;

        // 交互配置
        public bool canTrade = true;         // 可以交易
        public bool canCombat = true;        // 可以战斗
        public bool canRecruit = false;       // 可以招募
        public List<string> questIds = new(); // 关联任务ID

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(nameId)) return id;
            var parts = nameId.Split('.');
            return parts.Length > 0 ? parts[parts.Length - 1] : nameId;
        }
    }

    /// <summary>
    /// NPC实例数据（运行时）
    /// </summary>
    [Serializable]
    public class NPCInstance
    {
        public NPCTemplate template;
        public string instanceId;
        public NPCType currentType;           // 当前态度（可能变化）
        public int currentHp;
        public int maxHp;
        public int armor;
        public int damage;
        public string currentDialogue;
        public bool isDefeated;

        public NPCInstance(NPCTemplate template)
        {
            this.template = template;
            this.instanceId = Guid.NewGuid().ToString();
            this.currentType = template.npcType;
            this.maxHp = template.baseHp;
            this.currentHp = template.baseHp;
            this.armor = template.baseArmor;
            this.damage = template.baseDamage;
            this.currentDialogue = template.defaultDialogue;
        }

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead => currentHp <= 0;

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            int actualDamage = Mathf.Max(1, damage - armor);
            currentHp -= actualDamage;
            if (currentHp < 0) currentHp = 0;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            currentHp += amount;
            if (currentHp > maxHp) currentHp = maxHp;
        }

        /// <summary>
        /// 获取态度颜色（用于UI）
        /// </summary>
        public Color GetTypeColor()
        {
            return currentType switch
            {
                NPCType.Friendly => new Color(0.2f, 0.8f, 0.2f),   // 绿色
                NPCType.Allied => new Color(0.2f, 0.5f, 0.9f),   // 蓝色
                NPCType.Hostile => new Color(0.9f, 0.2f, 0.2f),   // 红色
                NPCType.Neutral => new Color(0.7f, 0.7f, 0.7f),   // 灰色
                _ => Color.white
            };
        }

        /// <summary>
        /// 获取态度文本
        /// </summary>
        public string GetTypeText()
        {
            return currentType switch
            {
                NPCType.Friendly => "友善",
                NPCType.Allied => "友方",
                NPCType.Hostile => "敌对",
                NPCType.Neutral => "中立",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// NPC管理器
    /// </summary>
    public class NPCManager
    {
        #region Singleton
        private static NPCManager _instance;
        public static NPCManager instance => _instance ??= new NPCManager();
        #endregion

        private readonly Dictionary<string, NPCTemplate> _templates = new();
        private readonly List<NPCInstance> _activeNPCs = new();

        public IReadOnlyList<NPCInstance> activeNPCs => _activeNPCs;

        /// <summary>
        /// 注册NPC模板
        /// </summary>
        public void RegisterTemplate(NPCTemplate template)
        {
            if (template == null || string.IsNullOrEmpty(template.id)) return;
            _templates[template.id] = template;
        }

        /// <summary>
        /// 创建NPC实例
        /// </summary>
        public NPCInstance CreateNPC(string templateId)
        {
            if (!_templates.TryGetValue(templateId, out var template))
            {
                Debug.LogWarning($"[NPCManager] Template not found: {templateId}");
                return null;
            }

            var npc = new NPCInstance(template);
            _activeNPCs.Add(npc);
            return npc;
        }

        /// <summary>
        /// 移除NPC实例
        /// </summary>
        public void RemoveNPC(NPCInstance npc)
        {
            if (npc == null) return;
            _activeNPCs.Remove(npc);
        }

        /// <summary>
        /// 获取所有模板ID
        /// </summary>
        public List<string> GetAllTemplateIds()
        {
            return new List<string>(_templates.Keys);
        }

        /// <summary>
        /// 根据类型获取所有NPC
        /// </summary>
        public List<NPCInstance> GetNPCsByType(NPCType type)
        {
            var result = new List<NPCInstance>();
            foreach (var npc in _activeNPCs)
            {
                if (npc.currentType == type)
                    result.Add(npc);
            }
            return result;
        }

        /// <summary>
        /// 清除所有活跃NPC
        /// </summary>
        public void Clear()
        {
            _activeNPCs.Clear();
        }
    }
}