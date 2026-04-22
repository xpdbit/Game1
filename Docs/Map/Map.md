# 世界地图系统 (Map)

## 概述

世界地图系统管理游戏中的旅行路径和地点节点。玩家通过地图系统从起点前往终点，途中触发各种事件和遭遇。系统采用**节点图**结构，每个节点代表一个可探索的地点。

## 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      TravelManager                          │
│  (单例，旅行核心枢纽，管理 WorldMap + EventQueue 交互)       │
└─────────────────────┬───────────────────────────────────────┘
                      │
         ┌────────────┴────────────┐
         ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│      WorldMap       │   │     EventQueue       │
│  (节点图结构管理)    │   │   (事件队列管理)     │
└─────────┬───────────┘   └─────────┬───────────┘
          │                         │
          ▼                         │
┌─────────────────────┐             │
│     Location        │             │
│  (地点节点数据)       │             │
└─────────────────────┘             │
                                    ▼
                          ┌─────────────────────┐
                          │    IGameEvent       │
                          │ (战斗/交易/发现事件) │
                          └─────────────────────┘
```

## 核心组件

### WorldMap（世界地图）

定义于 `Assets/Scripts/Entities/World/WorldMap.cs`

WorldMap 负责管理整个地图的节点结构和旅行状态。

| 属性 | 类型 | 说明 |
|------|------|------|
| `seed` | string | 地图生成种子 |
| `currentNodeIndex` | int | 当前节点索引 |
| `maxNodeIndex` | int | 最大到达节点（限制回头路） |
| `currentLocation` | Location | 当前地点 |
| `nextLocation` | Location | 下一个地点 |
| `totalNodes` | int | 总节点数 |

### Location（地点节点）

定义于 `Assets/Scripts/Entities/World/WorldMap.cs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `id` | string | 唯一标识，格式 `loc_{index}_{type}` |
| `locationName` | string | 地点名称文本ID |
| `type` | LocationType | 地点类型 |
| `connections` | List<string> | 连接的节点ID列表 |
| `nodeIndex` | int | 在路径上的顺序索引 |
| `hasEvent` | bool | 是否有事件触发 |
| `eventId` | string | 事件ID（如 `combat_001`） |
| `eventChance` | float | 事件触发概率（0~1） |
| `baseReward` | int | 基础奖励（金币） |
| `travelTime` | float | 基础旅行时间（秒） |
| `explorationTime` | float | 探索时间（秒） |
| `discoveredItems` | List<string> | 可发现物品ID列表 |

### LocationType（地点类型枚举）

| 枚举值 | 说明 | 事件倾向 |
|--------|------|----------|
| `Start` | 起点 | 无 |
| `City` | 古城 | NPC遭遇（友好） |
| `Wilderness` | 荒野 | 随机战斗/交易 |
| `Market` | 集市 | 交易事件 |
| `Dungeon` | 副本 | 战斗事件 |
| `Boss` | BOSS | 高难度战斗 |
| `Goal` | 终点 | 无 |

## 地图生成算法

### 生成流程

```
StartNewJourney(seed)
    │
    └─► WorldMap.Generate(seed)
            │
            ├─► 初始化随机数生成器（seed.GetHashCode()）
            │
            ├─► 确定节点数量（10~15个）
            │
            └─► 循环生成节点：
                    │
                    ├─► DetermineNodeType() - 确定类型权重
                    │     - 15% City
                    │     - 15% Market
                    │     - 20% Wilderness
                    │     - 25% Dungeon
                    │     - 25% Boss
                    │
                    ├─► GenerateLocation() - 创建节点
                    │     - ID: loc_{index}_{type}
                    │     - 40%概率有事件
                    │     - 事件ID根据类型确定
                    │
                    └─► 设置连接关系（previous <-> current）
```

### 事件ID映射

| 地点类型 | 默认事件ID | 说明 |
|----------|-----------|------|
| `City` | `npc_001` | NPC友好遭遇 |
| `Market` | `trade_001` | 商队交易 |
| `Dungeon` | `combat_001` | 副本战斗 |
| `Boss` | `combat_003` | BOSS战斗 |
| `Wilderness` | `combat_002` 或 `trade_001` | 50/50随机 |

### 节点分布示例（10节点）

```
[Start] ── [City] ── [Dungeon] ── [Market] ── [Wilderness] ── [Boss] ── [Goal]
   │         │          │           │            │            │
   └─────────┴──────────┴───────────┴────────────┴────────────┘
   (线性路径，每个节点只连接前后节点)
```

## 路径管理

### 移动规则

1. **线性前进**：默认只能前进到下一个节点
2. **回头路限制**：`maxNodeIndex` 限制只能前往已探索节点
3. **分支选择**：特定节点提供多个选择（AwaitingChoice状态）

### 移动方法

```csharp
// 前进到下一个节点
bool MoveToNext()

// 移动到指定位置（用于分支选择）
bool MoveToLocation(string locationId)

// 获取指定地点
Location GetLocation(string id)

// 获取当前节点的连接节点
List<Location> GetCurrentConnections()

// 重置地图（回到起点）
void Reset()
```

### 状态查询

```csharp
// 检查是否已探索到指定节点
bool IsExplored(int nodeIndex)

// 获取已探索的节点列表
List<Location> GetExploredLocations()
```

## 旅行时间系统

### 时间计算

```
实际旅行时间 = 基础旅行时间 × 速度倍率 × (1 + 旅行速度加成)
```

- **基础旅行时间**：由节点类型决定（5~60秒）
- **速度倍率**：TravelManager._travelSpeed（默认1.0）
- **加成**：PlayerActor.modules.GetTotalBonus("travel_speed")

### 时间影响因素

| 因素 | 来源 | 效果 |
|------|------|------|
| 基础时间 | Location.travelTime | 5f + random.Next(10) |
| 速度倍率 | TravelManager.SetSpeedMultiplier() | 乘数 |
| 模块加成 | PlayerActor.modules | 百分比加成 |

## 地图生成器 (MapGenerator)

定义于 `Assets/Scripts/Roguelike/MapGenerator.cs`

MapGenerator 提供更高级的地图配置生成能力。

### MapGeneratorConfig

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `nodeCount` | 10 | 节点数量 |
| `branchFactor` | 2 | 分支系数 |
| `specialNodeChance` | 0.3f | 特殊节点概率 |
| `bossNodeChance` | 0.1f | BOSS节点概率 |
| `marketNodeChance` | 0.2f | 市集节点概率 |
| `seed` | 0 | 随机种子 |

### 使用方式

```csharp
var config = new MapGeneratorConfig
{
    nodeCount = 15,
    bossNodeChance = 0.15f,
    seed = 12345
};

var generator = new MapGenerator(config);
var worldMap = generator.Generate();

// 或使用种子生成
var worldMap2 = MapGenerator.Generate("mySeed");
```

## 与TravelManager的交互

```
TravelManager.StartNewJourney(seed)
    │
    └─► WorldMap.Generate(seed)
    │
    └─► PlayerActor.travelState.StartTravel(from, to, time)
    │
    └─► SetStatus(Traveling)

TravelManager.Tick(deltaTime)
    │
    └─► TickTraveling(deltaTime)
    │       │
    │       └─► PlayerActor.travelState.UpdateProgress(adjustedTime)
    │
    └─► ProgressManager.instance.AddPoints(deltaTime)

TravelManager.OnTravelCompleted()
    │
    ├─► 检查 currentLocation.hasEvent
    │       │
    │       └─► EventQueue.Enqueue(CreateEvent())
    │               │
    │               └─► ProcessNext() → ApplyEventResult()
    │
    └─► AdvanceToNextNode()
```

## 进度里程碑

地图探索与进度系统（ProgressManager）结合：

- 每到达一个节点，里程碑计数 +1
- 里程碑触发随机事件（TriggerMilestoneEvent）
- 里程碑影响挂机收益加成

```csharp
// 检查进度里程碑
private void CheckProgressMilestone()
{
    if (ProgressManager.instance.milestoneCount > 0)
    {
        int milestone = ProgressManager.instance.milestoneCount;
        onMilestoneReached?.Invoke(milestone);
        TriggerMilestoneEvent();
    }
}
```

## 系统交互总览

```
┌──────────────────────────────────────────────────────────────────┐
│                         GameMain                                 │
│  (启动时初始化)                                                    │
└──────────────────────────┬───────────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                      TravelManager                               │
│  singleton                                                             │
└──────────┬─────────────────────┬──────────────────────┬───────────┘
           │                     │                      │
           ▼                     ▼                      ▼
┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐
│      WorldMap      │  │    EventQueue      │  │   PlayerActor      │
│  管理节点图结构     │  │    管理事件队列     │  │   管理玩家数据      │
└─────────┬──────────┘  └──────────┬─────────┘  └─────────┬──────────┘
          │                        │                     │
          │                        │                     │
          ▼                        ▼                     │
┌────────────────────┐  ┌────────────────────┐           │
│     Location       │  │   IGameEvent       │           │
│    (地点节点)       │  │   (事件接口)        │           │
└────────────────────┘  └──────────┬─────────┘           │
                                   │                     │
                                   ▼                     │
                          ┌────────────────────┐         │
                          │  CombatEvent       │         │
                          │  TradeEvent        │         │
                          │  (具体事件实现)      │         │
                          └────────────────────┘         │
                                                         │
                              PlayerActor.ApplyEventResult◄┘
```

## 扩展方向

### 待实现功能

1. **分支路径** - 让某些节点可以有多个前进方向
2. **节点特殊效果** - 不同类型节点提供不同加成
3. **地图变体** - 根据玩家状态生成不同地图
4. **回头路惩罚** - 返回已探索节点减少奖励

### 配置扩展

```csharp
// 潜在的扩展字段
Location {
    // 商店配置
    bool hasShop;
    string shopId;

    // 支线任务
    bool hasSideQuest;
    string questId;

    // 特殊资源
    List<string> gatherResources;  // 可采集资源
    float gatherEfficiency;        // 采集效率
}
```