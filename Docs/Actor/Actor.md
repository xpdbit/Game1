# 角色系统 (Actor)

## 概述

角色系统包含**玩家角色（PlayerActor）**和**NPC系统（NPCSystem）**两大部分。玩家角色管理玩家的属性、背包、状态和模块加成；NPC系统管理游戏中的非玩家角色，包括其模板数据、运行时实例和交互能力。

## 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                        PlayerActor                          │
│  (玩家角色数据：属性/背包/旅行状态/模块集合)                    │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    TravelState                              │
│  (旅行状态：Idle/Traveling/Arrived/EventPending)            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    ModuleCollection                          │
│  (模块集合：管理IModule实现，支持加成计算)                     │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                      NPCSystem                               │
│  (NPC管理：模板/实例/态度/战斗属性)                            │
└─────────────────────┬───────────────────────────────────────┘
                      │
         ┌────────────┴────────────┐
         ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│    NPCTemplate      │   │   NPCInstance        │
│  (NPC配置模板)       │   │  (NPC运行时实例)     │
└─────────────────────┘   └─────────────────────┘
```

## 玩家角色 (PlayerActor)

定义于 `Assets/Scripts/Entities/Player/PlayerActor.cs`

### 数据结构

#### Identity（身份）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `id` | string | Guid.NewGuid() | 唯一标识 |
| `actorName` | string | "行者" | 角色名称 |
| `level` | int | 1 | 角色等级 |

#### Stats（属性）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxHp` | int | 20 | 最大生命值 |
| `currentHp` | int | 20 | 当前生命值 |
| `attack` | int | 3 | 攻击力 |
| `defense` | int | 5 | 护甲值 |
| `speed` | float | 1f | 速度 |

#### CarryItems（携带物品）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `gold` | int | 0 | 金币数量 |
| `ownedModuleIds` | List<string> | new() | 拥有的模块ID列表 |
| `maxCapacity` | int | 100 | 最大容量 |
| `currentLoad` | int | 0 | 当前负载 |

#### State（状态）

| 属性 | 类型 | 说明 |
|------|------|------|
| `travelState` | TravelState | 旅行状态 |
| `modules` | ModuleCollection | 模块集合 |

### 核心方法

```csharp
// 获取总收益加成
float GetTotalBonus(string bonusType)

// 添加模块
void AddModule(IModule module)

// 移除模块
void RemoveModule(string moduleId)

// 应用事件结果
void ApplyEventResult(EventResult result)
```

## 旅行状态 (TravelState)

定义于 `Assets/Scripts/Entities/Player/PlayerActor.cs`

### 状态枚举

| 枚举值 | 说明 |
|--------|------|
| `Idle` | 空闲 |
| `Traveling` | 旅行中 |
| `Arrived` | 已到达 |
| `EventPending` | 事件待处理 |

### 数据结构

| 属性 | 类型 | 说明 |
|------|------|------|
| `currentState` | State | 当前状态 |
| `progress` | float | 进度（0~1） |
| `realTimeRequired` | float | 所需真实时间（秒） |
| `currentLocationId` | string | 当前位置ID |
| `nextLocationId` | string | 下一位置ID |

### 核心方法

```csharp
// 开始旅行
void StartTravel(string from, string to, float requiredTime)

// 更新进度
void UpdateProgress(float deltaTime)

// 完成事件后重置
void Complete()
```

### 进度更新逻辑

```csharp
public void UpdateProgress(float deltaTime)
{
    if (currentState != State.Traveling) return;

    progress += deltaTime / realTimeRequired;

    if (progress >= 1f)
    {
        progress = 1f;
        currentState = State.Arrived;
    }
}
```

## 模块系统 (ModuleCollection)

定义于 `Assets/Scripts/Entities/Player/PlayerActor.cs`

### IModule接口

所有模块必须实现此接口：

```csharp
public interface IModule
{
    string moduleId { get; }        // 模块ID
    string moduleName { get; }      // 模块名称
    string GetBonus(string bonusType);  // 获取加成
    void Tick(float deltaTime);         // _tick更新
    void OnActivate();                   // 激活时调用
    void OnDeactivate();                 // 停用时调用
}
```

### 模块集合管理

| 方法 | 说明 |
|------|------|
| `AddModule(IModule module)` | 添加模块（不重复） |
| `RemoveModule(string moduleId)` | 移除模块 |
| `GetTotalBonus(string bonusType)` | 获取指定类型的总加成 |

### 加成计算

```csharp
public float GetTotalBonus(string bonusType)
{
    float total = 0f;
    foreach (var module in _moduleDict.Values)
    {
        var bonus = module.GetBonus(bonusType);
        if (float.TryParse(bonus, out float value))
        {
            total += value;
        }
    }
    return total;
}
```

### 已知模块实现

| 模块 | 来源 | 说明 |
|------|------|------|
| `IdleRewardModule` | `Assets/Scripts/Modules/Idle/IdleRewardModule.cs` | 挂机收益模块 |
| `BonusMultiplierModule` | `Assets/Scripts/Modules/Idle/` | 加成倍率模块 |

## NPC系统 (NPCSystem)

定义于 `Assets/Scripts/Entities/NPC/NPCSystem.cs`

### NPC态度类型 (NPCType)

| 枚举值 | 说明 | 颜色标识 |
|--------|------|----------|
| `Friendly` | 友善 - 会帮助玩家 | 绿色 (0.2f, 0.8f, 0.2f) |
| `Allied` | 友方 - 同盟关系 | 蓝色 (0.2f, 0.5f, 0.9f) |
| `Hostile` | 敌方 - 敌对关系 | 红色 (0.9f, 0.2f, 0.2f) |
| `Neutral` | 中立 - 无特殊关系 | 灰色 (0.7f, 0.7f, 0.7f) |

### NPCTemplate（NPC模板）

NPC的配置数据，定义NPC的基本属性。

| 属性 | 类型 | 说明 |
|------|------|------|
| `id` | string | 唯一ID |
| `nameId` | string | 名称文本ID |
| `npcType` | NPCType | NPC态度类型 |
| `portrait` | Sprite | 头像 |
| `defaultDialogue` | string | 默认对话 |
| `level` | int | NPC等级 |
| `hostileChance` | float | 变为敌对的概率（Neutral类型，默认0.3） |
| `baseHp` | int | 基础生命值（默认20） |
| `baseArmor` | int | 基础护甲（默认5） |
| `baseDamage` | int | 基础伤害（默认3） |
| `canTrade` | bool | 可以交易（默认true） |
| `canCombat` | bool | 可以战斗（默认true） |
| `canRecruit` | bool | 可以招募（默认false） |
| `questIds` | List<string> | 关联任务ID |

### NPCInstance（NPC实例）

NPC运行时实例，存储当前状态。

| 属性 | 类型 | 说明 |
|------|------|------|
| `template` | NPCTemplate | 关联的模板 |
| `instanceId` | string | 实例唯一ID |
| `currentType` | NPCType | 当前态度（可能变化） |
| `currentHp` | int | 当前生命值 |
| `maxHp` | int | 最大生命值 |
| `armor` | int | 护甲值 |
| `damage` | int | 伤害值 |
| `currentDialogue` | string | 当前对话 |
| `isDefeated` | bool | 是否被击败 |

### 实例方法

```csharp
// 是否死亡
bool IsDead => currentHp <= 0

// 受到伤害
void TakeDamage(int damage)
    // actualDamage = max(1, damage - armor)

// 治疗
void Heal(int amount)

// 获取态度颜色
Color GetTypeColor()

// 获取态度文本
string GetTypeText()
```

## NPC管理器 (NPCManager)

单例类，管理所有NPC模板和活跃实例。

### 核心API

```csharp
// 获取所有活跃NPC实例
IReadOnlyList<NPCInstance> activeNPCs  // 只读属性

// 注册NPC模板
void RegisterTemplate(NPCTemplate template)

// 创建NPC实例
NPCInstance CreateNPC(string templateId)

// 移除NPC实例
void RemoveNPC(NPCInstance npc)

// 获取所有模板ID
List<string> GetAllTemplateIds()

// 根据类型获取所有NPC
List<NPCInstance> GetNPCsByType(NPCType type)

// 清除所有活跃NPC
void Clear()
```

### 使用示例

```csharp
// 注册模板
var template = new NPCTemplate
{
    id = "merchant_001",
    nameId = "Map.NPC.Merchant",
    npcType = NPCType.Friendly,
    canTrade = true,
    canRecruit = false
};
NPCManager.instance.RegisterTemplate(template);

// 创建NPC
var merchant = NPCManager.instance.CreateNPC("merchant_001");
if (merchant != null)
{
    Debug.Log($"创建NPC: {merchant.template.GetDisplayName()}");
}

// 根据类型查询
var friendlyNPCs = NPCManager.instance.GetNPCsByType(NPCType.Friendly);
```

## 与其他系统的交互

```
TravelManager
    │
    ├─► PlayerActor.travelState.StartTravel()
    │       │
    │       └─► TravelState.UpdateProgress(deltaTime)
    │
    └─► PlayerActor.modules.GetTotalBonus("travel_speed")
            │
            └─► ModuleCollection.GetTotalBonus()
                    │
                    └─► IModule.GetBonus()

EventChainManager
    │
    └─► PlayerActor.carryItems.gold -= choice.goldCost
    │
    └─► PlayerActor.AddModule() / RemoveModule()

EventQueue
    │
    └─► PlayerActor.ApplyEventResult(result)
            │
            ├─► goldReward / goldCost
            │
            └─► unlockedModuleIds / removedModuleIds
```

## 扩展方向

### 待实现功能

1. **玩家属性升级** - 等级提升，增加属性点
2. **属性加点系统** - 自由分配属性点
3. **装备系统** - 武器/护甲装备槽位
4. **NPC商店** - 交易界面集成
5. **NPC招募** - 招募NPC作为伙伴
6. **任务系统** - NPC关联任务 (questIds)

### 配置化NPC

```xml
<!-- 潜在的NPC XML配置 -->
<NPC>
    <id>merchant_001</id>
    <nameId>Map.NPC.Merchant</nameId>
    <type>Friendly</type>
    <level>5</level>
    <hostileChance>0.2</hostileChance>
    <baseHp>30</baseHp>
    <baseArmor>8</baseArmor>
    <baseDamage>5</baseDamage>
    <canTrade>true</canTrade>
    <canRecruit>false</canRecruit>
</NPC>
```

### AI行为扩展

```csharp
// NPC AI行为接口（待实现）
public interface INPCAI
{
    void Think(NPCInstance npc);
    float CalculateHostileChance();
    bool ShouldInteract();
}
```