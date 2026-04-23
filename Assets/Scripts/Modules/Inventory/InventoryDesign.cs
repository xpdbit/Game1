using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 背包操作结果
    /// </summary>
    public class InventoryOperationResult
    {
        public bool success;
        public string message;
        public int instanceId;
        public int actualAmount;

        public static InventoryOperationResult Ok(int instanceId = 0, int actualAmount = 0)
            => new InventoryOperationResult { success = true, message = "Success", instanceId = instanceId, actualAmount = actualAmount };

        public static InventoryOperationResult Fail(string message)
            => new InventoryOperationResult { success = false, message = message };

        public static InventoryOperationResult Full(string reason = "Inventory is full")
            => new InventoryOperationResult { success = false, message = reason };

        public static InventoryOperationResult WeightExceeded(string reason = "Weight limit exceeded")
            => new InventoryOperationResult { success = false, message = reason };

        public static InventoryOperationResult NotFound(string reason = "Item not found")
            => new InventoryOperationResult { success = false, message = reason };
    }

    /// <summary>
    /// 背包容量配置
    /// </summary>
    [Serializable]
    public class InventoryCapacity
    {
        /// <summary>
        /// 最大物品种类数
        /// </summary>
        public int maxSlotCount = 50;

        /// <summary>
        /// 最大总重量
        /// </summary>
        public float maxWeight = 100f;

        /// <summary>
        /// 默认堆叠上限
        /// </summary>
        public int defaultMaxStack = 99;

        public InventoryCapacity() { }

        public InventoryCapacity(int maxSlotCount, float maxWeight, int defaultMaxStack = 99)
        {
            this.maxSlotCount = maxSlotCount;
            this.maxWeight = maxWeight;
            this.defaultMaxStack = defaultMaxStack;
        }
    }

    /// <summary>
    /// 背包设计类（核心逻辑）
    /// 非MonoBehaviour，纯数据+逻辑
    /// </summary>
    public class InventoryDesign
    {
        #region Singleton
        private static InventoryDesign _instance;
        public static InventoryDesign instance => _instance ??= new InventoryDesign();
        #endregion

        #region Events
        public event Action<InventoryEventData> onInventoryChanged;
        #endregion

        #region Private Fields
        private readonly Dictionary<int, ItemInstance> _itemsByInstanceId = new();  // instanceId -> ItemInstance
        private readonly Dictionary<string, List<int>> _itemsByTemplateId = new(); // templateId -> List<instanceId>
        private readonly List<ItemInstance> _items = new();                         // 保持插入顺序

        private InventoryCapacity _capacity = new();
        private float _totalWeight;
        private int _instanceIdCounter = 0;
        #endregion

        #region Properties
        /// <summary>
        /// 当前物品数量（种类数）
        /// </summary>
        public int slotCount => _items.Count;

        /// <summary>
        /// 当前总重量
        /// </summary>
        public float totalWeight => _totalWeight;

        /// <summary>
        /// 容量配置
        /// </summary>
        public InventoryCapacity capacity
        {
            get => _capacity;
            set => _capacity = value ?? new InventoryCapacity();
        }

        /// <summary>
        /// 所有物品实例（只读）
        /// </summary>
        public IReadOnlyList<ItemInstance> items => _items;
        #endregion

        #region Core API - Add/Remove

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="amount">数量</param>
        /// <returns>操作结果</returns>
        public InventoryOperationResult AddItem(string templateId, int amount = 1)
        {
            if (string.IsNullOrEmpty(templateId))
                return InventoryOperationResult.Fail("TemplateId is null or empty");

            if (amount <= 0)
                return InventoryOperationResult.Fail("Amount must be positive");

            var template = ItemManager.GetTemplate(templateId);
            if (template == null)
                return InventoryOperationResult.Fail($"Template not found: {templateId}");

            int maxStack = template.maxStack > 0 ? template.maxStack : _capacity.defaultMaxStack;
            int remaining = amount;

            // 先尝试堆叠到现有物品
            if (_itemsByTemplateId.TryGetValue(templateId, out var instanceIds))
            {
                foreach (var instanceId in instanceIds)
                {
                    if (_itemsByInstanceId.TryGetValue(instanceId, out var existing))
                    {
                        int canAdd = maxStack - existing.amount;
                        if (canAdd > 0)
                        {
                            int toAdd = Mathf.Min(canAdd, remaining);
                            existing.amount += toAdd;
                            remaining -= toAdd;
                            _totalWeight += template.weight * toAdd;

                            PublishEvent(InventoryEventData.ItemUpdated(templateId, instanceId, existing.amount));
                            PublishEvent(InventoryEventData.CapacityChanged());

                            if (remaining <= 0)
                                return InventoryOperationResult.Ok(instanceId, amount);
                        }
                    }
                }
            }

            // 创建新堆叠
            while (remaining > 0)
            {
                // 检查容量
                if (!CanAddSlot())
                    return InventoryOperationResult.Full($"No empty slot available (max: {_capacity.maxSlotCount})");

                if (!CanAddWeight(template.weight * remaining))
                    return InventoryOperationResult.WeightExceeded($"Weight would exceed limit ({_totalWeight}/{_capacity.maxWeight})");

                int instanceId = GenerateInstanceId();
                int stackAmount = Mathf.Min(remaining, maxStack);

                var newItem = new ItemInstance(template, stackAmount);
                // 手动设置instanceId因为构造函数会生成一个
                newItem.instanceId = instanceId;

                _items.Add(newItem);
                _itemsByInstanceId[instanceId] = newItem;

                if (!_itemsByTemplateId.ContainsKey(templateId))
                    _itemsByTemplateId[templateId] = new List<int>();
                _itemsByTemplateId[templateId].Add(instanceId);

                _totalWeight += template.weight * stackAmount;
                remaining -= stackAmount;

                PublishEvent(InventoryEventData.ItemAdded(templateId, instanceId, stackAmount));
                PublishEvent(InventoryEventData.CapacityChanged());
            }

            return InventoryOperationResult.Ok();
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <param name="amount">移除数量，0表示全部</param>
        /// <returns>操作结果</returns>
        public InventoryOperationResult RemoveItem(int instanceId, int amount = 0)
        {
            if (!_itemsByInstanceId.TryGetValue(instanceId, out var item))
                return InventoryOperationResult.NotFound($"Item instance not found: {instanceId}");

            int removeAmount = amount > 0 ? amount : item.amount;
            if (removeAmount > item.amount)
                removeAmount = item.amount;

            item.amount -= removeAmount;
            _totalWeight -= item.itemTemplate.weight * removeAmount;

            if (item.amount <= 0)
            {
                // 完全移除
                _items.Remove(item);
                _itemsByInstanceId.Remove(instanceId);

                if (_itemsByTemplateId.TryGetValue(item.itemTemplate.id, out var ids))
                {
                    ids.Remove(instanceId);
                    if (ids.Count == 0)
                        _itemsByTemplateId.Remove(item.itemTemplate.id);
                }

                PublishEvent(InventoryEventData.ItemRemoved(item.itemTemplate.id, instanceId, removeAmount));
            }
            else
            {
                PublishEvent(InventoryEventData.ItemUpdated(item.itemTemplate.id, instanceId, item.amount));
            }

            PublishEvent(InventoryEventData.CapacityChanged());
            return InventoryOperationResult.Ok(instanceId, removeAmount);
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _itemsByInstanceId.Clear();
            _itemsByTemplateId.Clear();
            _totalWeight = 0;

            PublishEvent(InventoryEventData.InventoryCleared());
            PublishEvent(InventoryEventData.CapacityChanged());
        }
        #endregion

        #region Core API - Query

        /// <summary>
        /// 获取物品实例
        /// </summary>
        public ItemInstance GetItem(int instanceId)
        {
            return _itemsByInstanceId.TryGetValue(instanceId, out var item) ? item : null;
        }

        /// <summary>
        /// 获取所有物品实例
        /// </summary>
        public IReadOnlyList<ItemInstance> GetAllItems()
        {
            return _items;
        }

        /// <summary>
        /// 按模板ID获取所有实例
        /// </summary>
        public List<ItemInstance> GetItemsByTemplateId(string templateId)
        {
            if (!_itemsByTemplateId.TryGetValue(templateId, out var ids))
                return new List<ItemInstance>();

            var result = new List<ItemInstance>();
            foreach (var id in ids)
            {
                if (_itemsByInstanceId.TryGetValue(id, out var item))
                    result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// 按类型获取所有物品
        /// </summary>
        public List<ItemInstance> GetItemsByType(ItemType type)
        {
            var result = new List<ItemInstance>();
            foreach (var item in _items)
            {
                if (item.itemTemplate.type == type)
                    result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// 获取某模板的总量
        /// </summary>
        public int GetTotalAmountByTemplateId(string templateId)
        {
            if (!_itemsByTemplateId.TryGetValue(templateId, out var ids))
                return 0;

            int total = 0;
            foreach (var id in ids)
            {
                if (_itemsByInstanceId.TryGetValue(id, out var item))
                    total += item.amount;
            }
            return total;
        }

        /// <summary>
        /// 计算总重量
        /// </summary>
        public float GetTotalWeight()
        {
            return _totalWeight;
        }

        /// <summary>
        /// 获取指定模板的总重量
        /// </summary>
        public float GetWeightByTemplateId(string templateId)
        {
            if (!_itemsByTemplateId.TryGetValue(templateId, out var ids))
                return 0f;

            float weight = 0f;
            var template = ItemManager.GetTemplate(templateId);
            if (template == null) return 0f;

            foreach (var id in ids)
            {
                if (_itemsByInstanceId.TryGetValue(id, out var item))
                    weight += template.weight * item.amount;
            }
            return weight;
        }
        #endregion

        #region Capacity Checks

        /// <summary>
        /// 检查是否可以添加指定数量的物品
        /// </summary>
        public bool CanAddItem(string templateId, int amount = 1)
        {
            var template = ItemManager.GetTemplate(templateId);
            if (template == null) return false;

            int maxStack = template.maxStack > 0 ? template.maxStack : _capacity.defaultMaxStack;
            int remaining = amount;

            // 检查现有堆叠
            if (_itemsByTemplateId.TryGetValue(templateId, out var instanceIds))
            {
                foreach (var instanceId in instanceIds)
                {
                    if (_itemsByInstanceId.TryGetValue(instanceId, out var existing))
                    {
                        int canAdd = maxStack - existing.amount;
                        remaining -= canAdd;
                        if (remaining <= 0) break;
                    }
                }
            }

            // 需要新堆叠的情况
            while (remaining > 0)
            {
                if (!CanAddSlot()) return false;
                if (!CanAddWeight(template.weight * remaining)) return false;
                remaining -= maxStack;
            }

            return true;
        }

        /// <summary>
        /// 是否有空槽位
        /// </summary>
        public bool CanAddSlot()
        {
            return _items.Count < _capacity.maxSlotCount;
        }

        /// <summary>
        /// 是否可以添加指定重量
        /// </summary>
        public bool CanAddWeight(float weight)
        {
            return _totalWeight + weight <= _capacity.maxWeight;
        }

        /// <summary>
        /// 获取剩余槽位数
        /// </summary>
        public int RemainingSlotCount()
        {
            return Mathf.Max(0, _capacity.maxSlotCount - _items.Count);
        }

        /// <summary>
        /// 获取剩余重量
        /// </summary>
        public float RemainingWeight()
        {
            return Mathf.Max(0f, _capacity.maxWeight - _totalWeight);
        }
        #endregion

        #region Batch Operations

        /// <summary>
        /// 批量添加物品
        /// </summary>
        public List<InventoryOperationResult> AddItems(IEnumerable<(string templateId, int amount)> items)
        {
            var results = new List<InventoryOperationResult>();
            foreach (var (templateId, amount) in items)
            {
                results.Add(AddItem(templateId, amount));
            }
            return results;
        }

        /// <summary>
        /// 批量移除物品
        /// </summary>
        public List<InventoryOperationResult> RemoveItems(IEnumerable<(int instanceId, int amount)> items)
        {
            var results = new List<InventoryOperationResult>();
            foreach (var (instanceId, amount) in items)
            {
                results.Add(RemoveItem(instanceId, amount));
            }
            return results;
        }
        #endregion

        #region Serialization

        /// <summary>
        /// 导出数据用于存档
        /// </summary>
        public List<InventorySaveData> Export()
        {
            var saveData = new List<InventorySaveData>();
            foreach (var item in _items)
            {
                saveData.Add(new InventorySaveData
                {
                    templateId = item.itemTemplate.id,
                    instanceId = item.instanceId,
                    amount = item.amount
                });
            }
            return saveData;
        }

        /// <summary>
        /// 从存档恢复
        /// </summary>
        public void Import(List<InventorySaveData> saveData)
        {
            Clear();
            foreach (var data in saveData)
            {
                var template = ItemManager.GetTemplate(data.templateId);
                if (template == null) continue;

                var instance = new ItemInstance(template, data.amount);
                instance.instanceId = data.instanceId;

                _items.Add(instance);
                _itemsByInstanceId[data.instanceId] = instance;

                if (!_itemsByTemplateId.ContainsKey(data.templateId))
                    _itemsByTemplateId[data.templateId] = new List<int>();
                _itemsByTemplateId[data.templateId].Add(data.instanceId);

                _totalWeight += template.weight * data.amount;

                if (data.instanceId > _instanceIdCounter)
                    _instanceIdCounter = data.instanceId;
            }

            PublishEvent(InventoryEventData.CapacityChanged());
        }
        #endregion

        #region Private Methods
        private int GenerateInstanceId()
        {
            return ++_instanceIdCounter;
        }

        private void PublishEvent(InventoryEventData data)
        {
            onInventoryChanged?.Invoke(data);
            InventoryEventBus.instance.Publish(data);
        }
        #endregion
    }

    /// <summary>
    /// 背包存档数据结构
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public string templateId;
        public int instanceId;
        public int amount;
    }
}