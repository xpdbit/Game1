# 游戏事件系统 (Event)

## 概述

游戏事件系统处理旅行过程中的各种随机事件和遭遇。系统采用**事件队列**机制，事件被添加到队列并逐个执行，每个事件产生结果影响玩家状态。

## 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      EventQueue                             │
│  (事件队列管理：待处理事件/历史记录/当前事件)                 │
└─────────────────────┬───────────────────────────────────────┘
                      │
         ┌────────────┴────────────┐
         ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│    IGameEvent        │   │     EventResult      │
│  (事件接口)           │   │   (事件结果数据)     │
└─────────┬───────────┘   └─────────────────────┘
          │
          │ 实现
          ▼
┌─────────────────────────────────────────────────────────────┐
│                   具体事件实现                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ CombatEvent │  │ TradeEvent  │  │ Discovery   │          │
│  │  (战斗事件)  │  │  (交易事件)  │  │  (发现事件)  │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└─────────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    EventResult                               │
│  (success / goldReward / goldCost / unlockedModuleIds / ...) │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   PlayerActor                                │
│              ApplyEventResult() → 更新玩家状态                 │
└─────────────────────────────────────────────────────────────┘
```

## 核心接口

### IGameEvent（游戏事件接口）

定义于 `Assets/Scripts/Events/EventQueue.cs`

所有游戏事件必须实现此接口。

```csharp
public interface IGameEvent
{
    string eventId { get; }           // 事件ID
    string title { get; }             // 事件标题
    string description { get; }       // 事件描述
    GameEventType eventType { get; }  // 事件类型
    bool CanTrigger();                // 是否可以触发
    EventResult Execute();            // 执行事件并返回结果
}
```

### EventResult（事件结果）

定义于 `Assets/Scripts/Events/EventQueue.cs`

事件执行后返回的结果数据。

| 属性 | 类型 | 说明 |
|------|------|------|
| `success` | bool | 是否成功 |
| `goldReward` | int | 金币奖励 |
| `goldCost` | int | 金币消耗 |
| `unlockedModuleIds` | List<string> | 解锁的模块ID |
| `removedModuleIds` | List<string> | 移除的模块ID |
| `message` | string | 结果消息 |
| `isGameOver` | bool | 是否游戏结束 |

### GameEventType（事件类型枚举）

| 枚举值 | 说明 |
|--------|------|
| `Random` | 随机事件 |
| `Trade` | 交易事件 |
| `Combat` | 战斗事件 |
| `Discovery` | 发现事件 |
| `Mystery` | 神秘事件 |

## 事件队列 (EventQueue)

EventQueue 管理事件的排队和处理。

### 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `pendingCount` | int | 待处理事件数量 |
| `hasCurrentEvent` | bool | 是否有当前事件 |
| `currentEvent` | IGameEvent | 当前事件 |

### 核心API

```csharp
// 添加事件到队列
void Enqueue(IGameEvent gameEvent)

// 处理下一个事件
EventResult ProcessNext()

// 查看下一个事件（不处理）
IGameEvent PeekNext()

// 清空所有待处理事件
void Clear()

// 生成积压事件（上线时）
void GeneratePendingEvents(float offlineTime)

// Tick - 检查是否需要生成新随机事件
void Tick(float deltaTime)

// 获取事件历史
List<IGameEvent> GetHistory()
```

### 事件回调

```csharp
event Action<IGameEvent> onEventTriggered;   // 事件触发
event Action<EventResult> onEventCompleted;  // 事件完成
```

## 具体事件实现

### CombatEvent（战斗事件）

定义于 `Assets/Scripts/Events/EventQueue.cs`

战斗事件是最常见的事件类型。

| 属性 | 类型 | 说明 |
|------|------|------|
| `enemyCount` | int | 敌人数量 |
| `enemyStrength` | int | 敌人强度 |

#### 战斗逻辑

```csharp
// 计算奖励公式
int baseReward = 20;
int rewardPerEnemy = 15;
float strengthMultiplier = 1f + (enemyStrength / 50f);

int totalReward = (int)((baseReward + enemyCount * rewardPerEnemy) * strengthMultiplier);

result.success = true;
result.goldReward = totalReward;
result.message = $"击败了{enemyCount}个敌人（强度{enemyStrength}），获得了{totalReward}金币！";
```

### TradeEvent（交易事件）

定义于 `Assets/Scripts/Events/EventQueue.cs`

交易事件提供玩家与商队的交互。

#### 交易逻辑

```csharp
int tradeCost = 30;       // 基础交易成本
float tradeBonus = 0.2f;  // 交易提供20%额外收益加成

result.success = true;
result.goldCost = tradeCost;
result.message = $"与商队交易，消耗{tradeCost}金币，获得{tradeBonus * 100}%收益加成！";
```

注意：实际的加成效果需要在事件完成后通过其他机制应用。

## 事件生成机制

### 积压事件（离线收益）

上线时根据离线时间生成积压事件：

```csharp
void GeneratePendingEvents(float offlineTime)
{
    // 假设每60秒可能触发一个事件
    float eventInterval = 60f;
    int eventCount = Mathf.FloorToInt(offlineTime / eventInterval);

    for (int i = 0; i < eventCount; i++)
    {
        var combatEvent = new CombatEvent
        {
            enemyCount = UnityEngine.Random.Range(1, 4),
            enemyStrength = UnityEngine.Random.Range(10, 50)
        };
        _pendingEvents.Enqueue(combatEvent);
    }
}
```

### 运行时随机事件

Tick方法以1%概率生成随机战斗事件：

```csharp
void Tick(float deltaTime)
{
    float eventChance = 0.01f;  // 约每10秒可能触发一次
    if (UnityEngine.Random.value < eventChance)
    {
        var randomEvent = new CombatEvent
        {
            enemyCount = UnityEngine.Random.Range(1, 3),
            enemyStrength = UnityEngine.Random.Range(5, 30)
        };
        _pendingEvents.Enqueue(randomEvent);
    }
}
```

## 事件处理流程

```
TravelManager.OnTravelCompleted()
    │
    └─► 检查 currentLocation.hasEvent
            │
            └─► EventQueue.Enqueue(CreateEvent())
                    │
                    └─► EventQueue.ProcessNext()
                            │
                            ├─► Dequeue() → _currentEvent
                            │
                            ├─► _currentEvent.Execute() → EventResult
                            │
                            ├─► onEventTriggered?.Invoke(_currentEvent)
                            │
                            ├─► onEventCompleted?.Invoke(result)
                            │
                            └─► PlayerActor.ApplyEventResult(result)
```

## 事件创建工厂

TravelManager 根据 eventId 创建对应事件：

```csharp
private IGameEvent CreateEvent(string eventId, Location location)
{
    switch (eventId)
    {
        case "combat_001":
        case "combat_002":
        case "combat_003":
            return new CombatEvent
            {
                enemyCount = Math.Max(1, location.baseReward / 20),
                enemyStrength = Math.Max(10, location.baseReward / 10)
            };

        case "trade_001":
        case "trade_002":
            return new TradeEvent();

        case "npc_001":
            return CreateNPCEvent(location);

        default:
            return CreateRandomEvent();
    }
}
```

### EncounterGenerator（遭遇生成器）

定义于 `Assets/Scripts/Roguelike/MapGenerator.cs`

更高级的随机遭遇生成：

```csharp
public IGameEvent GenerateEncounter()
{
    int roll = _random.Next(100);

    if (roll < 40)  // 40% 战斗
    {
        return new CombatEvent { ... };
    }
    else if (roll < 70)  // 30% 交易
    {
        return new TradeEvent();
    }
    else  // 30% 随机
    {
        return new TradeEvent(); // TODO: 返回随机事件
    }
}
```

## 事件与玩家状态

EventResult 通过 PlayerActor.ApplyEventResult() 应用：

```csharp
public void ApplyEventResult(EventResult result)
{
    if (result == null) return;

    if (result.isGameOver)
    {
        Debug.Log("[PlayerActor] Game Over!");
        return;
    }

    // 应用金币变化
    carryItems.gold += result.goldReward;
    carryItems.gold -= result.goldCost;

    // 确保金币不为负
    if (carryItems.gold < 0)
        carryItems.gold = 0;

    // 处理模块解锁/移除（预留接口）
    // foreach (var moduleId in result.unlockedModuleIds) { ... }
    // foreach (var moduleId in result.removedModuleIds) { ... }
}
```

## 系统交互总览

```
┌──────────────────────────────────────────────────────────────────┐
│                       EventQueue                                 │
│  pendingEvents / currentEvent / eventHistory                     │
└──────────┬──────────────────┬───────────────────┬────────────────┘
           │                  │                   │
           │                  │                   │
           ▼                  ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  CombatEvent    │  │   TradeEvent     │  │  IGameEvent     │
│  enemyCount     │  │   tradeCost     │  │  (自定义事件)    │
│  enemyStrength  │  │   tradeBonus    │  └─────────────────┘
└────────┬────────┘  └────────┬────────┘
         │                    │
         │                    │
         └────────┬───────────┘
                  │
                  ▼
         ┌─────────────────┐
         │   EventResult   │
         │ goldReward      │
         │ goldCost        │
         │ message         │
         └────────┬────────┘
                  │
                  ▼
         ┌─────────────────┐
         │  PlayerActor    │
         │ ApplyEventResult│
         └─────────────────┘
```

## 扩展方向

### 待实现事件类型

| 事件类型 | 说明 | 优先级 |
|----------|------|--------|
| `DiscoveryEvent` | 发现宝箱/资源事件 | 高 |
| `MysteryEvent` | 神秘事件（随机奖励/惩罚） | 中 |
| `NPCEvent` | NPC交互事件 | 中 |
| `QuestEvent` | 任务触发事件 | 低 |

### 事件配置化

```csharp
// 潜在的事件XML配置
<Event>
    <id>combat_001</id>
    <type>Combat</type>
    <enemyCount base="1" variance="2"/>
    <enemyStrength base="20" variance="30"/>
    <rewards>
        <base>20</base>
        <perEnemy>15</perEnemy>
        <strengthMultiplier>true</strengthMultiplier>
    </rewards>
</Event>
```

### 事件链集成

事件结果可以触发 EventChain：

```csharp
// 在 ApplyEventResult 中扩展
if (result.triggerChainId != null)
{
    var chain = LoadEventChain(result.triggerChainId);
    EventChainManager.instance.StartChain(chain);
}
```

### 事件里程碑系统

事件触发可以累计里程碑：

```csharp
if (result.success)
{
    ProgressManager.instance.OnEventCompleted(result);
}
```