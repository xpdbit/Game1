using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 物品状态
    /// </summary>
    public enum InventoryItemState
    {
        Normal,     // 默认状态
        Highlighted, // 高亮状态
        Disabled   // 禁用状态
    }

    /// <summary>
    /// 物品数据结构
    /// </summary>
    [Serializable]
    public class InventoryItemData
    {
        public string id;
        public Sprite image;
        public string name;
        public int amount;

        [NonSerialized] public InventoryItemState state = InventoryItemState.Normal;
        [NonSerialized] public bool isSelected = false;

        public InventoryItemData() { }

        public InventoryItemData(string id, Sprite image, string name, int amount)
        {
            this.id = id;
            this.image = image;
            this.name = name;
            this.amount = amount;
            this.state = InventoryItemState.Normal;
            this.isSelected = false;
        }
    }

    /// <summary>
    /// 背包数据管理器
    /// </summary>
    public class InventoryData
    {
        public System.Collections.Generic.List<InventoryItemData> items { get; private set; } = new();

        public event Action<InventoryItemData> onItemAdded;
        public event Action<string> onItemRemoved;
        public event Action onItemsCleared;

        public void AddItem(InventoryItemData item)
        {
            items.Add(item);
            onItemAdded?.Invoke(item);
        }

        public void RemoveItem(string id)
        {
            items.RemoveAll(i => i.id == id);
            onItemRemoved?.Invoke(id);
        }

        public void Clear()
        {
            items.Clear();
            onItemsCleared?.Invoke();
        }

        public InventoryItemData GetItem(string id)
        {
            return items.Find(i => i.id == id);
        }
    }
}