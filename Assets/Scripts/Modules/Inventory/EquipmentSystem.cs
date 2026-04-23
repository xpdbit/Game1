using System;
using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 装备槽位枚举
    /// </summary>
    public enum EquipmentSlot
    {
        /// <summary>武器</summary>
        Weapon,

        /// <summary>护甲</summary>
        Armor,

        /// <summary>饰品1</summary>
        Accessory1,

        /// <summary>饰品2</summary>
        Accessory2,

        /// <summary>坐骑</summary>
        Mount
    }

    /// <summary>
    /// 装备操作结果
    /// </summary>
    public struct EquipmentOperationResult
    {
        public bool success;
        public string message;
        public ItemInstance previousItem;  // 替换前的物品
        public ItemInstance newItem;       // 新装备的物品

        public static EquipmentOperationResult Success(string message, ItemInstance previousItem = null, ItemInstance newItem = null)
        {
            return new EquipmentOperationResult
            {
                success = true,
                message = message,
                previousItem = previousItem,
                newItem = newItem
            };
        }

        public static EquipmentOperationResult Failure(string message)
        {
            return new EquipmentOperationResult
            {
                success = false,
                message = message
            };
        }
    }

    /// <summary>
    /// 装备系统
    /// 管理装备的装备、强化和属性计算
    /// 采用与InventoryDesign相同的单例非MonoBehaviour模式
    /// </summary>
    public class EquipmentSystem
    {
        #region Singleton
        private static EquipmentSystem _instance;
        public static EquipmentSystem instance => _instance ??= new EquipmentSystem();
        #endregion

        #region Configuration
        // 强化费用倍率（每级）
        private const float ENHANCE_COST_MULTIPLIER = 1.5f;

        // 强化基础费用
        private const int BASE_ENHANCE_COST = 100;

        // 速度惩罚上限
        private const float MAX_SPEED_PENALTY = 0.5f;
        #endregion

        #region Public Methods

        /// <summary>
        /// 装备物品到指定槽位
        /// </summary>
        /// <param name="member">队伍成员</param>
        /// <param name="item">物品实例</param>
        /// <param name="slot">装备槽位</param>
        /// <returns>操作结果</returns>
        public EquipmentOperationResult Equip(TeamMemberData member, ItemInstance item, EquipmentSlot slot)
        {
            if (member == null)
                return EquipmentOperationResult.Failure("队伍成员不能为空");

            if (item == null)
                return EquipmentOperationResult.Failure("物品不能为空");

            var template = ItemManager.instance.GetTemplate(item.templateId);
            if (template == null)
                return EquipmentOperationResult.Failure($"物品模板不存在: {item.templateId}");

            // 检查物品类型是否匹配槽位
            if (!IsItemTypeMatchSlot(template.type, slot))
                return EquipmentOperationResult.Failure($"物品类型 {template.type} 不能装备到槽位 {slot}");

            // 获取当前已装备的物品
            ItemInstance previousItem = GetEquippedItem(member, slot);

            // 卸下原装备
            if (previousItem != null)
            {
                // 这里需要调用Inventory来添加回物品
                // 先暂时简化处理
            }

            // 装备新物品
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    member.weaponTemplateId = item.templateId;
                    break;
                case EquipmentSlot.Armor:
                    member.armorTemplateId = item.templateId;
                    break;
                case EquipmentSlot.Accessory1:
                    member.accessory1TemplateId = item.templateId;
                    break;
                case EquipmentSlot.Accessory2:
                    member.accessory2TemplateId = item.templateId;
                    break;
                case EquipmentSlot.Mount:
                    member.mountTemplateId = item.templateId;
                    break;
            }

            string itemName = template.nameTextId;
            return EquipmentOperationResult.Success(
                $"成功装备 {itemName}",
                previousItem,
                item
            );
        }

        /// <summary>
        /// 从指定槽位卸下装备
        /// </summary>
        /// <param name="member">队伍成员</param>
        /// <param name="slot">装备槽位</param>
        /// <returns>操作结果</returns>
        public ItemInstance Unequip(TeamMemberData member, EquipmentSlot slot)
        {
            if (member == null)
                return null;

            string templateId = "";

            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    templateId = member.weaponTemplateId;
                    member.weaponTemplateId = "";
                    break;
                case EquipmentSlot.Armor:
                    templateId = member.armorTemplateId;
                    member.armorTemplateId = "";
                    break;
                case EquipmentSlot.Accessory1:
                    templateId = member.accessory1TemplateId;
                    member.accessory1TemplateId = "";
                    break;
                case EquipmentSlot.Accessory2:
                    templateId = member.accessory2TemplateId;
                    member.accessory2TemplateId = "";
                    break;
                case EquipmentSlot.Mount:
                    templateId = member.mountTemplateId;
                    member.mountTemplateId = "";
                    break;
            }

            if (string.IsNullOrEmpty(templateId))
                return null;

            // 创建物品实例并返回
            return new ItemInstance
            {
                instanceId = ItemManager.instance.GenerateInstanceId(),
                templateId = templateId,
                amount = 1
            };
        }

        /// <summary>
        /// 强化装备
        /// </summary>
        /// <param name="equipment">装备物品实例</param>
        /// <param name="level">目标等级</param>
        /// <returns>操作结果</returns>
        public InventoryOperationResult EnhanceEquipment(ItemInstance equipment, int level)
        {
            if (equipment == null)
                return InventoryOperationResult.Failure("装备不能为空");

            if (level <= 0)
                return InventoryOperationResult.Failure("强化等级必须大于0");

            var template = ItemManager.instance.GetTemplate(equipment.templateId);
            if (template == null)
                return InventoryOperationResult.Failure($"物品模板不存在: {equipment.templateId}");

            // 检查是否为可强化物品（武器和防具）
            if (template.type != ItemType.Weapon && template.type != ItemType.Armor)
                return InventoryOperationResult.Failure("只有武器和防具可以强化");

            // 计算强化费用
            int cost = GetEnhanceCost(template, level);

            // 检查玩家金币是否足够
            // TODO: 需要从PlayerActor获取金币
            // 这里暂时简化处理

            // 执行强化
            // equipment.enhancedLevel = level; // 需要在ItemInstance中添加增强等级字段

            return InventoryOperationResult.Success($"强化成功，等级提升到 {level}");
        }

        /// <summary>
        /// 获取强化费用
        /// </summary>
        /// <param name="template">物品模板</param>
        /// <param name="currentLevel">当前等级</param>
        /// <returns>强化费用</returns>
        public int GetEnhanceCost(ItemTemplate template, int currentLevel)
        {
            if (template == null)
                return 0;

            // 基础费用 * 等级倍率
            int baseCost = template.type == ItemType.Weapon ? BASE_ENHANCE_COST * 2 : BASE_ENHANCE_COST;
            return (int)(baseCost * Math.Pow(ENHANCE_COST_MULTIPLIER, currentLevel));
        }

        /// <summary>
        /// 获取装备伤害值
        /// </summary>
        public int GetEquipmentDamage(ItemInstance equipment)
        {
            if (equipment == null)
                return 0;

            var template = ItemManager.instance.GetTemplate(equipment.templateId);
            if (template == null || template.type != ItemType.Weapon)
                return 0;

            int baseDamage = template.damage;

            // 如果有强化等级，计算加成
            // int enhancedLevel = equipment.enhancedLevel; // 待实现
            // baseDamage += enhancedLevel * 2;

            return baseDamage;
        }

        /// <summary>
        /// 获取装备防御值
        /// </summary>
        public int GetEquipmentDefense(ItemInstance equipment)
        {
            if (equipment == null)
                return 0;

            var template = ItemManager.instance.GetTemplate(equipment.templateId);
            if (template == null || template.type != ItemType.Armor)
                return 0;

            int baseDefense = template.armor;

            return baseDefense;
        }

        /// <summary>
        /// 获取装备速度惩罚
        /// </summary>
        public float GetEquipmentSpeedPenalty(ItemTemplate template)
        {
            if (template == null || template.type != ItemType.Armor)
                return 0f;

            // 返回防具的速度惩罚（如果有的话，配置在moveSpeedOffset中）
            float penalty = template.moveSpeedOffset;

            // 确保惩罚不超过50%
            return Math.Min(MAX_SPEED_PENALTY, Math.Abs(penalty));
        }

        /// <summary>
        /// 获取指定槽位已装备的物品
        /// </summary>
        public ItemInstance GetEquippedItem(TeamMemberData member, EquipmentSlot slot)
        {
            if (member == null)
                return null;

            string templateId = slot switch
            {
                EquipmentSlot.Weapon => member.weaponTemplateId,
                EquipmentSlot.Armor => member.armorTemplateId,
                EquipmentSlot.Accessory1 => member.accessory1TemplateId,
                EquipmentSlot.Accessory2 => member.accessory2TemplateId,
                EquipmentSlot.Mount => member.mountTemplateId,
                _ => ""
            };

            if (string.IsNullOrEmpty(templateId))
                return null;

            // 需要从InventoryManager获取物品实例
            // 这里暂时返回null
            return null;
        }

        /// <summary>
        /// 获取成员所有装备的总加成
        /// </summary>
        public EquipmentBonus GetTotalEquipmentBonus(TeamMemberData member)
        {
            var bonus = new EquipmentBonus();

            if (member == null)
                return bonus;

            // 遍历所有槽位累加属性
            var slots = new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Accessory1, EquipmentSlot.Accessory2, EquipmentSlot.Mount };

            foreach (var slot in slots)
            {
                var templateId = slot switch
                {
                    EquipmentSlot.Weapon => member.weaponTemplateId,
                    EquipmentSlot.Armor => member.armorTemplateId,
                    EquipmentSlot.Accessory1 => member.accessory1TemplateId,
                    EquipmentSlot.Accessory2 => member.accessory2TemplateId,
                    EquipmentSlot.Mount => member.mountTemplateId,
                    _ => ""
                };

                if (string.IsNullOrEmpty(templateId))
                    continue;

                var template = ItemManager.instance.GetTemplate(templateId);
                if (template == null)
                    continue;

                // 累加属性
                bonus.totalDamage += template.damage;
                bonus.totalDefense += template.armor;
                bonus.speedBonus += template.moveSpeedFactor;
                bonus.speedPenalty += GetEquipmentSpeedPenalty(template);
            }

            return bonus;
        }

        /// <summary>
        /// 检查物品类型是否匹配槽位
        /// </summary>
        public bool IsItemTypeMatchSlot(ItemType itemType, EquipmentSlot slot)
        {
            return (itemType, slot) switch
            {
                (ItemType.Weapon, EquipmentSlot.Weapon) => true,
                (ItemType.Armor, EquipmentSlot.Armor) => true,
                (ItemType.Accessory, EquipmentSlot.Accessory1) => true,
                (ItemType.Accessory, EquipmentSlot.Accessory2) => true,
                (ItemType.Mount, EquipmentSlot.Mount) => true,
                _ => false
            };
        }

        /// <summary>
        /// 获取槽位名称
        /// </summary>
        public string GetSlotName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Weapon => "武器",
                EquipmentSlot.Armor => "护甲",
                EquipmentSlot.Accessory1 => "饰品1",
                EquipmentSlot.Accessory2 => "饰品2",
                EquipmentSlot.Mount => "坐骑",
                _ => "未知"
            };
        }

        #endregion
    }

    /// <summary>
    /// 装备加成数据结构
    /// </summary>
    public struct EquipmentBonus
    {
        public int totalDamage;
        public int totalDefense;
        public float speedBonus;      // 速度加成（正值）
        public float speedPenalty;    // 速度惩罚（正值）

        /// <summary>
        /// 最终速度倍率
        /// </summary>
        public float speedMultiplier => 1f + speedBonus - speedPenalty;

        /// <summary>
        /// 是否为净加成
        /// </summary>
        public bool isPositive => speedMultiplier > 1f;

        public override string ToString()
        {
            return $"[EquipmentBonus: DMG={totalDamage}, DEF={totalDefense}, SPD={speedMultiplier:P0}]";
        }
    }
}