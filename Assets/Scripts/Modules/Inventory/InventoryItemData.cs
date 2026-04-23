using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 背包物品数据（用于UI绑定）
    /// </summary>
    [Serializable]
    public class InventoryItemData
    {
        /// <summary>
        /// 物品模板ID
        /// </summary>
        public string templateId;

        /// <summary>
        /// 物品实例ID（唯一）
        /// </summary>
        public int instanceId;

        /// <summary>
        /// 当前数量
        /// </summary>
        public int amount;

        /// <summary>
        /// 最大堆叠数量
        /// </summary>
        public int maxStack;

        /// <summary>
        /// 单个物品重量
        /// </summary>
        public float weight;

        /// <summary>
        /// 物品类型
        /// </summary>
        public ItemType itemType;

        /// <summary>
        /// 名称文本ID
        /// </summary>
        public string nameTextId;

        /// <summary>
        /// 描述文本ID
        /// </summary>
        public string descTextId;

        /// <summary>
        /// 是否被选中（UI用）
        /// </summary>
        public bool isSelected;

        /// <summary>
        /// 物品状态（UI用）
        /// </summary>
        public ItemDataState state;

        public InventoryItemData() { }

        public InventoryItemData(ItemInstance instance)
        {
            if (instance?.itemTemplate == null) return;

            templateId = instance.itemTemplate.id;
            instanceId = instance.instanceId;
            amount = instance.amount;
            maxStack = instance.itemTemplate.maxStack;
            weight = instance.itemTemplate.weight;
            itemType = instance.itemTemplate.type;
            nameTextId = instance.itemTemplate.nameTextId;
            descTextId = instance.itemTemplate.descTextId;
            isSelected = false;
            state = ItemDataState.Normal;
        }

        /// <summary>
        /// 从模板创建新的物品数据
        /// </summary>
        public static InventoryItemData CreateFromTemplate(ItemTemplate template, int amount = 1)
        {
            return new InventoryItemData
            {
                templateId = template.id,
                instanceId = 0, // 运行时由InventoryDesign分配
                amount = amount,
                maxStack = template.maxStack,
                weight = template.weight,
                itemType = template.type,
                nameTextId = template.nameTextId,
                descTextId = template.descTextId,
                isSelected = false,
                state = ItemDataState.Normal
            };
        }

        /// <summary>
        /// 计算总重量
        /// </summary>
        public float TotalWeight => weight * amount;
    }

    /// <summary>
    /// 物品数据状态（用于UI显示）
    /// </summary>
    [Flags]
    public enum ItemDataState
    {
        Normal = 0,       // 正常
        Locked = 1,       // 锁定/不可用
        Checked = 2,      // 勾选
        Highlighted = 4   // 高亮
    }
}