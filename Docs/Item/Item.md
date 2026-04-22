# 物品系统 (Items)

## 概述

物品系统是游戏的核心子系统之一，负责管理所有可收集、使用、交易的物品实体。系统采用**模板-实例分离**的设计模式：

- **ItemTemplate（物品模板）**：静态配置数据，定义物品的基础属性（如类型、重量、伤害值等）
- **ItemInstance（物品实例）**：运行时数据，关联模板并记录数量、唯一ID等运行时信息

## 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                        ItemManager                          │
│  (单例工具类，管理模板字典，委托背包操作给 InventoryDesign)   │
└─────────────────────┬───────────────────────────────────────┘
                      │ 模板查询
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    ItemTemplate (配置)                       │
│  id / nameTextId / descTextId / type / weight / damage ...  │
└─────────────────────┬───────────────────────────────────────┘
                      │ 实例化
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   ItemInstance (运行时)                      │
│  itemTemplate / amount / instanceId                         │
└─────────────────────┬───────────────────────────────────────┘
                      │ 存入背包
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    InventoryDesign                          │
│  (单例，背包核心逻辑：存储/容量管理/事件发布/存档序列化)     │
└─────────────────────────────────────────────────────────────┘
```

## 数据模型

### ItemTemplate（物品模板）

定义于 `Assets/Scripts/Managers/ItemManager.cs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `id` | string | 唯一标识符，格式如 `Core.Item.Bacon` |
| `nameTextId` | string | 名称文本ID（用于国际化） |
| `descTextId` | string | 描述文本ID（用于国际化） |
| `type` | ItemType | 物品类型枚举 |
| `weight` | float | 重量（kg），影响背包容量 |
| `foodCalorific` | float | 食物热量（千焦），用于恢复体力 |
| `fuelCalorific` | float | 燃料热值，用于生火烹饪 |
| `damage` | int | 武器伤害值 |
| `armor` | int | 护甲防御值 |
| `moveSpeedOffset` | float | 移动速度偏移值（绝对值） |
| `moveSpeedFactor` | float | 移动速度倍率（乘数） |
| `maxStack` | int | 最大堆叠数量，默认99 |

### ItemInstance（物品实例）

运行时实例，关联模板并记录数量。

| 属性 | 类型 | 说明 |
|------|------|------|
| `itemTemplate` | ItemTemplate | 关联的物品模板 |
| `amount` | int | 堆叠数量 |
| `instanceId` | int | 实例唯一ID（自增） |

### ItemType（物品类型枚举）

定义于 `ItemManager.cs`

| 枚举值 | 说明 |
|--------|------|
| `Food` | 食物，可食用恢复体力 |
| `Weapon` | 武器，装备后增加伤害 |
| `Armor` | 护甲，装备后增加防御 |
| `Consumable` | 消耗品，使用后消耗 |
| `Material` | 材料，用于制造和交易 |
| `QuestItem` | 任务物品，不可丢弃 |
| `Money` | 货币（金币等） |

## 物品配置 (Items.xml)

物品模板数据存储于 `Assets/Resources/Data/Items/Items.xml`，采用XML格式。

### 当前配置物品

| ID | 类型 | 重量 | 关键属性 |
|----|------|------|----------|
| `Core.Item.GoldCoin` | Money | 0kg | 货币，无重量 |
| `Core.Item.Bacon` | Food | 0.5kg | 热量1200kJ |
| `Core.Item.Cabbage` | Food | 1.0kg | 热量400kJ |
| `Core.Item.ShortBlade` | Weapon | 2.0kg | 伤害+5 |

### XML配置结构

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Items>
    <Item>
        <id>Core.Item.XXX</id>
        <nameTextId>Core.Item.XXX.NameText</nameTextId>
        <descTextId>Core.Item.XXX.DescriptionText</descTextId>
        <type>Food</type>           <!-- 物品类型 -->
        <weight>1.0</weight>        <!-- 重量(kg) -->
        <foodCalorific>400</foodCalorific>  <!-- 食物热量(kJ) -->
        <!-- 以下为可选字段 -->
        <damage>5</damage>         <!-- 武器伤害 -->
        <armor>3</armor>           <!-- 护甲防御 -->
        <fuelCalorific>1000</fuelCalorific>  <!-- 燃料热值 -->
        <moveSpeedOffset>0.5</moveSpeedOffset>  <!-- 速度偏移 -->
        <moveSpeedFactor>1.2</moveSpeedFactor>   <!-- 速度倍率 -->
        <maxStack>99</maxStack>    <!-- 最大堆叠 -->
    </Item>
</Items>
```

## 物品管理器 (ItemManager)

`ItemManager` 是静态工具类，职责：

1. **加载物品模板** - 启动时从XML加载所有模板到字典
2. **提供模板查询** - `GetTemplate()`、`HasTemplate()`、`GetAllTemplateIds()`
3. **委托背包操作** - 所有背包操作实际由 `InventoryDesign.instance` 处理

### 核心API

#### 模板查询
```csharp
ItemTemplate GetTemplate(string templateId)  // 获取模板，不存在返回null
bool HasTemplate(string templateId)         // 检查模板是否存在
IReadOnlyCollection<string> GetAllTemplateIds()  // 获取所有模板ID
```

#### 背包操作（委托给 InventoryDesign）
```csharp
InventoryOperationResult AddItem(string templateId, int amount = 1)
InventoryOperationResult RemoveItem(int instanceId, int amount = 0)
void ClearInventory()
IReadOnlyList<ItemInstance> GetInventory()
ItemInstance GetItem(int instanceId)
List<ItemInstance> GetItemsByTemplateId(string templateId)
List<ItemInstance> GetItemsByType(ItemType type)
bool CanAddItem(string templateId, int amount = 1)
float GetTotalWeight()
int RemainingSlotCount()
float RemainingWeight()
List<InventoryOperationResult> AddItems(IEnumerable<(string, int)> items)
```

#### 存档相关
```csharp
List<InventorySaveData> ExportInventory()   // 导出存档
void ImportInventory(List<InventorySaveData> saveData)  // 导入存档
void SetInventoryCapacity(int maxSlotCount, float maxWeight)  // 设置容量
```

#### 事件订阅
```csharp
void SubscribeInventoryChanged(Action<InventoryEventData> callback)
void UnsubscribeInventoryChanged(Action<InventoryEventData> callback)
```

## 背包系统 (InventoryDesign)

`InventoryDesign` 是背包单例核心类，负责：

- 物品存储（Dictionary<int, ItemInstance>）
- 容量管理（槽位数量、最大重量）
- 按模板分组索引（Dictionary<string, List<int>>）
- 事件发布（onInventoryChanged、InventoryEventBus）
- 存档序列化/反序列化

### 容量系统

背包有两条容量限制：

| 限制 | 默认值 | 说明 |
|------|--------|------|
| 槽位数量 | 50 | 物品种类上限（非堆叠数量） |
| 最大重量 | 100kg | 所有物品总重量上限 |

可通过 `SetInventoryCapacity(maxSlotCount, maxWeight)` 修改。

### 额外查询方法

```csharp
int GetInventoryCount()                    // 获取背包物品种类数
int GetTotalAmountByTemplateId(string templateId)  // 获取某模板的总数量
float GetWeightByTemplateId(string templateId)     // 获取某模板的总重量
bool CanAddSlot()                          // 是否有空槽位
bool CanAddWeight(float weight)            // 是否可以添加指定重量
float RemainingWeight()                    // 获取剩余重量
ItemInstance GetItem(int instanceId)        // 获取物品实例
```

### InventoryEventBus 事件系统

物品变化通过两条渠道通知UI（同时触发）：

1. **InventoryDesign.onInventoryChanged** - 直接事件回调
2. **InventoryEventBus** - 事件总线（发布-订阅模式）

```csharp
// 订阅示例
ItemManager.SubscribeInventoryChanged(data => {
    Debug.Log($"背包变化: 操作={data.operation}, 物品={data.templateId}");
});

// 或通过事件总线
InventoryEventBus.instance.Subscribe<InventoryEventData>(data => { ... });
```

## 系统交互

```
GameMain.Initialize()
    │
    └─► ItemManager.Initialize()
            │
            └─► LoadTemplates() → 从XML加载模板到字典
                        │
                        ▼
            ItemManager.GetTemplate("Core.Item.Bacon")  ← 查询接口
                        │
                        ▼
            InventoryDesign.instance.AddItem()  ← 委托背包操作
                        │
                        ▼
            onInventoryChanged?.Invoke()  ← 事件通知UI更新
```

## 未来扩展

### 待实现的物品（Items.md中已有条目）

| 物品 | 类型 | 说明 |
|------|------|------|
| 长刀 | Weapon | 待配置 |
| 木材 | Material | 待配置 |
| 铁锭 | Material | 待配置 |
| 碳 | Material | 待配置 |
| 木柴 | Material | 待配置 |

### 扩展方向

1. **新增物品类型** - 在 `ItemType` 枚举添加新类型
2. **扩展物品属性** - 在 `ItemTemplate` 添加新字段
3. **物品稀有度** - 添加 `rarity` 属性影响颜色显示
4. **装备槽位** - 支持武器/护甲/饰品装备系统
5. **物品制作** - 添加合成配方系统
6. **物品附魔** - 添加属性强化系统