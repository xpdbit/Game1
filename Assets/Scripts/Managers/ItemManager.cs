using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 物品类型
    /// </summary>
    public enum ItemType
    {
        Food,
        Weapon,
        Armor,
        Consumable,
        Material,
        QuestItem
    }

/// <summary>
    /// 物品模板数据（配置）
    /// </summary>
    [Serializable]
    public class ItemTemplate
    {
        public string id;              // 唯一标识，如 Core.Item.Bacon
        public string nameTextId;      // 名称文本ID
        public string descTextId;       // 描述文本ID
        public ItemType type;          // 物品类型
        public float weight;            // 重量（kg）
        public float foodCalorific;     // 食物热量（千焦）
        public float fuelCalorific;      // 燃料热值
        public int damage;              // 伤害值
        public int armor;               // 护甲值
        public float moveSpeedOffset;   // 移动速度偏移
        public float moveSpeedFactor;   // 移动速度因子

        /// <summary>
        /// 从 XML 元素解析（使用路径解析）
        /// </summary>
        public static ItemTemplate ParseFromXml(XmlElement element)
        {
            return new ItemTemplate
            {
                id = element.SelectSingleNode("id")?.InnerText ?? string.Empty,
                nameTextId = element.SelectSingleNode("nameTextId")?.InnerText ?? string.Empty,
                descTextId = element.SelectSingleNode("descTextId")?.InnerText ?? string.Empty,
                type = (ItemType)int.Parse(element.SelectSingleNode("type")?.InnerText ?? "0"),
                weight = float.Parse(element.SelectSingleNode("weight")?.InnerText ?? "0"),
                foodCalorific = float.Parse(element.SelectSingleNode("foodCalorific")?.InnerText ?? "0"),
                fuelCalorific = float.Parse(element.SelectSingleNode("fuelCalorific")?.InnerText ?? "0"),
                damage = int.Parse(element.SelectSingleNode("damage")?.InnerText ?? "0"),
                armor = int.Parse(element.SelectSingleNode("armor")?.InnerText ?? "0"),
                moveSpeedOffset = float.Parse(element.SelectSingleNode("moveSpeedOffset")?.InnerText ?? "0"),
                moveSpeedFactor = float.Parse(element.SelectSingleNode("moveSpeedFactor")?.InnerText ?? "1")
            };
        }
    }

    /// <summary>
    /// 物品实例数据（运行时）
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string templateId;      // 模板ID
        public int amount;             // 数量（堆叠）
        public int instanceId;         // 实例唯一ID

        public ItemInstance(string templateId, int amount = 1)
        {
            this.templateId = templateId;
            this.amount = amount;
            this.instanceId = GenerateInstanceId();
        }

        private static int _instanceIdCounter = 0;
        private static int GenerateInstanceId()
        {
            return ++_instanceIdCounter;
        }
    }

    /// <summary>
    /// 物品管理器
    /// 负责物品模板加载和背包实例管理
    /// </summary>
    public static class ItemManager
    {
        /// <summary>
        /// 物品模板字典（只读配置）
        /// </summary>
        private static readonly Dictionary<string, ItemTemplate> _templates = new();

        /// <summary>
        /// 玩家背包（实例列表）
        /// </summary>
        private static readonly List<ItemInstance> _inventory = new();

        /// <summary>
        /// 模板是否已加载
        /// </summary>
        private static bool _isLoaded = false;

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
        /// 加载所有物品模板
        /// </summary>
        private static void LoadTemplates()
        {
            var itemsPath = "Data/Items/Items";
            var templates = ResourceManager.LoadXmlArray<ItemTemplate>(itemsPath);
            if (templates == null || templates.Length == 0)
            {
                Debug.LogError("[ItemManager] Failed to load items from: " + itemsPath);
                return;
            }

            foreach (var template in templates)
            {
                if (string.IsNullOrEmpty(template.id))
                {
                    Debug.LogWarning($"[ItemManager] Skip template with empty id: {template.nameTextId}");
                    continue;
                }

                if (_templates.ContainsKey(template.id))
                {
                    Debug.LogWarning($"[ItemManager] Duplicate template id: {template.id}");
                    continue;
                }

                _templates[template.id] = template;
                Debug.Log($"[ItemManager] Loaded template: {template.id}");
            }

            Debug.Log($"[ItemManager] Total templates loaded: {_templates.Count}");
        }

        /// <summary>
        /// 获取物品模板
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>模板数据，不存在返回null</returns>
        public static ItemTemplate GetTemplate(string templateId)
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
        /// 添加物品到背包
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="amount">数量</param>
        /// <returns>是否成功</returns>
        public static bool AddItem(string templateId, int amount = 1)
        {
            if (!_templates.ContainsKey(templateId))
            {
                Debug.LogWarning($"[ItemManager] Unknown template: {templateId}");
                return false;
            }

            if (amount <= 0) return false;

            _inventory.Add(new ItemInstance(templateId, amount));
            return true;
        }

        /// <summary>
        /// 从背包移除物品
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="amount">数量</param>
        /// <returns>是否成功</returns>
        public static bool RemoveItem(int instanceId, int amount = 1)
        {
            var item = _inventory.Find(i => i.instanceId == instanceId);
            if (item == null) return false;

            if (amount >= item.amount)
            {
                _inventory.Remove(item);
            }
            else
            {
                item.amount -= amount;
            }
            return true;
        }

        /// <summary>
        /// 获取背包所有物品
        /// </summary>
        public static IReadOnlyList<ItemInstance> GetInventory()
        {
            return _inventory;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public static void ClearInventory()
        {
            _inventory.Clear();
        }

        /// <summary>
        /// 获取背包物品数量
        /// </summary>
        public static int GetInventoryCount()
        {
            return _inventory.Count;
        }

        /// <summary>
        /// 获取指定模板的物品数量
        /// </summary>
        public static int GetItemCount(string templateId)
        {
            int count = 0;
            foreach (var item in _inventory)
            {
                if (item.templateId == templateId)
                {
                    count += item.amount;
                }
            }
            return count;
        }
    }
}