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
        Accessory,
        Mount,
        Consumable,
        Material,
        QuestItem,
        Money
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
        public int maxStack = 99;       // 最大堆叠数量，默认99

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
                type = (ItemType)Enum.Parse(typeof(ItemType), element.SelectSingleNode("type")?.InnerText ?? "Food"),
                weight = float.Parse(element.SelectSingleNode("weight")?.InnerText ?? "0"),
                foodCalorific = float.Parse(element.SelectSingleNode("foodCalorific")?.InnerText ?? "0"),
                fuelCalorific = float.Parse(element.SelectSingleNode("fuelCalorific")?.InnerText ?? "0"),
                damage = int.Parse(element.SelectSingleNode("damage")?.InnerText ?? "0"),
                armor = int.Parse(element.SelectSingleNode("armor")?.InnerText ?? "0"),
                moveSpeedOffset = float.Parse(element.SelectSingleNode("moveSpeedOffset")?.InnerText ?? "0"),
                moveSpeedFactor = float.Parse(element.SelectSingleNode("moveSpeedFactor")?.InnerText ?? "1"),
                maxStack = int.Parse(element.SelectSingleNode("maxStack")?.InnerText ?? "99")
            };
        }
    }

    /// <summary>
    /// 物品实例数据（运行时）
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public ItemTemplate itemTemplate;      // 模板
        public int amount;             // 数量（堆叠）
        public int instanceId;         // 实例唯一ID

        public ItemInstance(ItemTemplate template, int amount = 1)
        {
            Init(template, amount);
        }

        public ItemInstance(string templateId, int amount = 1)
        {
            var template = ItemManager.GetTemplate(templateId);
            if (template == null)
            {
                Debug.LogError($"[ItemInstance] Invalid templateId: {templateId}");
                template = new ItemTemplate { id = templateId, nameTextId = "Unknown" };
            }
            Init(template, amount);
        }

        private void Init(ItemTemplate template, int amount)
        {
            this.itemTemplate = template;
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
    /// 委托给 InventoryDesign 处理背包逻辑
    /// </summary>
    public static class ItemManager
    {
        /// <summary>
        /// 物品模板字典（只读配置）
        /// </summary>
        private static readonly Dictionary<string, ItemTemplate> _templates = new();

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

        #region Inventory Delegation (委托给 InventoryDesign)

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public static InventoryOperationResult AddItem(string templateId, int amount = 1)
        {
            return InventoryDesign.instance.AddItem(templateId, amount);
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public static InventoryOperationResult RemoveItem(int instanceId, int amount = 0)
        {
            return InventoryDesign.instance.RemoveItem(instanceId, amount);
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public static void ClearInventory()
        {
            InventoryDesign.instance.Clear();
        }

        /// <summary>
        /// 获取背包所有物品
        /// </summary>
        public static IReadOnlyList<ItemInstance> GetInventory()
        {
            return InventoryDesign.instance.GetAllItems();
        }

        /// <summary>
        /// 获取背包物品数量
        /// </summary>
        public static int GetInventoryCount()
        {
            return InventoryDesign.instance.slotCount;
        }

        /// <summary>
        /// 获取物品实例
        /// </summary>
        public static ItemInstance GetItem(int instanceId)
        {
            return InventoryDesign.instance.GetItem(instanceId);
        }

        /// <summary>
        /// 按模板ID获取所有实例
        /// </summary>
        public static List<ItemInstance> GetItemsByTemplateId(string templateId)
        {
            return InventoryDesign.instance.GetItemsByTemplateId(templateId);
        }

        /// <summary>
        /// 按类型获取所有物品
        /// </summary>
        public static List<ItemInstance> GetItemsByType(ItemType type)
        {
            return InventoryDesign.instance.GetItemsByType(type);
        }

        /// <summary>
        /// 检查是否可以添加物品
        /// </summary>
        public static bool CanAddItem(string templateId, int amount = 1)
        {
            return InventoryDesign.instance.CanAddItem(templateId, amount);
        }

        /// <summary>
        /// 获取总重量
        /// </summary>
        public static float GetTotalWeight()
        {
            return InventoryDesign.instance.GetTotalWeight();
        }

        /// <summary>
        /// 获取剩余槽位数
        /// </summary>
        public static int RemainingSlotCount()
        {
            return InventoryDesign.instance.RemainingSlotCount();
        }

        /// <summary>
        /// 获取剩余重量
        /// </summary>
        public static float RemainingWeight()
        {
            return InventoryDesign.instance.RemainingWeight();
        }

        /// <summary>
        /// 批量添加物品
        /// </summary>
        public static List<InventoryOperationResult> AddItems(IEnumerable<(string templateId, int amount)> items)
        {
            return InventoryDesign.instance.AddItems(items);
        }

        /// <summary>
        /// 导出背包数据用于存档
        /// </summary>
        public static List<InventorySaveData> ExportInventory()
        {
            return InventoryDesign.instance.Export();
        }

        /// <summary>
        /// 从存档恢复背包数据
        /// </summary>
        public static void ImportInventory(List<InventorySaveData> saveData)
        {
            InventoryDesign.instance.Import(saveData);
        }

        /// <summary>
        /// 设置背包容量
        /// </summary>
        public static void SetInventoryCapacity(int maxSlotCount, float maxWeight)
        {
            InventoryDesign.instance.capacity = new InventoryCapacity(maxSlotCount, maxWeight);
        }

        /// <summary>
        /// 订阅背包变化事件
        /// </summary>
        public static void SubscribeInventoryChanged(Action<InventoryEventData> callback)
        {
            InventoryDesign.instance.onInventoryChanged += callback;
        }

        /// <summary>
        /// 取消订阅背包变化事件
        /// </summary>
        public static void UnsubscribeInventoryChanged(Action<InventoryEventData> callback)
        {
            InventoryDesign.instance.onInventoryChanged -= callback;
        }

        #endregion
    }
}