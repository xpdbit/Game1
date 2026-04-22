# 事件树系统 (EventTree)

## 概述

事件树系统管理**分支对话和选择序列**，允许玩家在事件中做出多分支选择，影响后续发展和奖励。与 `EventQueue` 的即时事件不同，事件树是**有状态的连续事件链**。

## 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    EventChainManager                        │
│  (单例，管理事件链生命周期)                                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
         ┌────────────┴────────────┐
         ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│     EventChain       │   │     PlayerActor      │
│  (单次事件链实例)     │   │   (应用选择效果)      │
└─────────┬───────────┘   └─────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│                    EventChainNode                            │
│  (事件节点：标题/描述/选项列表)                               │
└─────────────────────┬───────────────────────────────────────┘
                      │
         ┌────────────┴────────────┐
         ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│    EventChoice       │   │      Flags           │
│  (选项：消耗/前置/效果) │   │   (状态标志字典)      │
└─────────────────────┘   └─────────────────────┘
```

## 核心组件

### EventChain（事件链）

定义于 `Assets/Scripts/Events/EventChain.cs`

事件链表示一次完整的分支事件会话。

| 属性 | 类型 | 说明 |
|------|------|------|
| `chainId` | string | 事件链唯一ID |
| `title` | string | 事件链标题 |
| `nodes` | List<EventChainNode> | 节点列表 |
| `startNodeId` | string | 起始节点ID |
| `currentNodeId` | string | 当前节点ID |
| `flags` | Dictionary<string, string> | 状态标志（用于条件判断） |
| `isCompleted` | bool | 是否完成 |

### EventChain 方法

```csharp
// 获取当前节点
EventChainNode GetCurrentNode()

// 获取指定节点
EventChainNode GetNextNode(string nextNodeId)

// 选择选项并前进
EventChoice SelectChoice(string choiceId)

// 检查选项是否可用
bool IsChoiceAvailable(EventChoice choice, PlayerActor player)

// 重置事件链
void Reset()
```

### EventChainNode（事件节点）

表示事件序列中的一个节点。

| 属性 | 类型 | 说明 |
|------|------|------|
| `nodeId` | string | 节点ID |
| `eventId` | string | 关联的事件ID |
| `title` | string | 显示标题 |
| `description` | string | 描述文本 |
| `choices` | List<EventChoice> | 选项列表 |
| `isOptional` | bool | 是否可跳过 |
| `skipDelay` | float | 跳过延迟（秒） |

### EventChoice（事件选项）

玩家可以选择的选项。

| 属性 | 类型 | 说明 |
|------|------|------|
| `choiceId` | string | 选项ID |
| `text` | string | 显示文本 |
| `choiceType` | ChoiceType | 选项类型 |
| `nextNodeId` | string | 下一个节点ID（null表示结束） |
| `goldCost` | int | 金币消耗 |
| `requiredItemId` | int | 需要物品ID |
| `requiredFlag` | string | 需要标志 |
| `setFlag` | string | 选择后设置的标志 |
| `addModuleIds` | List<string> | 添加的模块ID |
| `removeModuleIds` | List<string> | 移除的模块ID |

### ChoiceType（选项类型枚举）

| 枚举值 | 说明 |
|--------|------|
| `Normal` | 普通选项 |
| `Risky` | 风险选项（可能失败） |
| `Premium` | 高级选项（需要资源） |
| `Story` | 剧情选项（影响故事） |

## 事件链管理器 (EventChainManager)

EventChainManager 是单例，负责：

1. 启动和结束事件链
2. 管理当前事件链状态
3. 处理玩家选择
4. 应用选择效果到玩家

### 核心API

```csharp
// 开始事件链
void StartChain(EventChain chain)

// 选择选项
void SelectChoice(string choiceId)

// 跳过当前节点（如果允许）
void SkipCurrentNode()

// 取消事件链
void CancelChain()

// 清空所有事件链
void ClearAll()
```

### 事件回调

```csharp
event Action<EventChain> onChainStarted;       // 事件链开始
event Action<EventChainNode> onNodeEntered;    // 进入节点
event Action<EventChoice> onChoiceSelected;    // 选择选项
event Action<EventChain> onChainCompleted;     // 事件链完成
```

## 选择效果系统

### 效果类型

| 效果 | 触发条件 | 作用目标 |
|------|----------|----------|
| `goldCost` | 金币消耗 | player.carryItems.gold |
| `requiredItemId` | 物品检查 | 背包验证 |
| `requiredFlag` | 标志检查 | flags字典 |
| `setFlag` | 标志设置 | flags字典 |
| `addModuleIds` | 模块添加 | PlayerActor.modules |
| `removeModuleIds` | 模块移除 | PlayerActor.modules |

### 标志系统

标志（Flags）用于追踪玩家选择历史，实现条件分支：

```csharp
// 设置标志 - 注意：存储的值是 choiceId，不是"true"
// 例如：choice.setFlag = "talked_to_merchant", choice.choiceId = "buy"
// 则 flags["talked_to_merchant"] = "buy"
flags[choice.setFlag] = choiceId;

// 检查标志示例
if (flags.ContainsKey("talked_to_merchant")) { ... }

// 获取标志值
if (flags.TryGetValue("talked_to_merchant", out string value))
{
    // value 是 choiceId，如 "buy"
}
```

### 效果应用流程

```
SelectChoice(choiceId)
    │
    ├─► EventChain.SelectChoice(choiceId)
    │       │
    │       └─► 设置 setFlag（如有）
    │
    ├─► onChoiceSelected?.Invoke(choice)
    │
    └─► ApplyChoiceEffects(choice)
            │
            ├─► goldCost > 0 → player.carryItems.gold -= amount
            │
            ├─► addModuleIds → player.AddModule(moduleId)
            │
            └─► removeModuleIds → player.RemoveModule(moduleId)
```

## 节点跳过机制

对于可跳过的节点（isOptional=true）：

```csharp
// SkipCurrentNode 逻辑
if (currentNode.isOptional)
{
    // 使用第一个选项作为默认
    // 或立即进入下一个节点
}
```

skipDelay 用于自动跳过延迟，增强沉浸感。

## 使用示例

```csharp
// 创建事件链
var chain = new EventChain
{
    chainId = "merchant_001",
    title = "路遇商队",
    startNodeId = "node_1",
    nodes = new List<EventChainNode>
    {
        new EventChainNode
        {
            nodeId = "node_1",
            title = "商队领队",
            description = "你好，旅行者！我这里有些稀奇的货物...",
            choices = new List<EventChoice>
            {
                new EventChoice
                {
                    choiceId = "buy",
                    text = "看看有什么货物",
                    nextNodeId = "node_2",
                    goldCost = 30
                },
                new EventChoice
                {
                    choiceId = "talk",
                    text = "聊聊旅途见闻",
                    nextNodeId = "node_3",
                    setFlag = "talked_to_merchant"
                },
                new EventChoice
                {
                    choiceId = "leave",
                    text = "婉言谢绝，继续赶路",
                    nextNodeId = null  // 结束事件链
                }
            }
        },
        // ... 更多节点
    }
};

// 启动事件链
EventChainManager.instance.StartChain(chain);

// 监听事件
EventChainManager.instance.onNodeEntered += node => {
    Debug.Log($"进入节点: {node.title}");
};

EventChainManager.instance.onChoiceSelected += choice => {
    Debug.Log($"选择了: {choice.text}");
};

EventChainManager.instance.onChainCompleted += chain => {
    Debug.Log($"事件链完成: {chain.chainId}");
};
```

## 与其他系统的交互

```
EventQueue.ProcessNext() → TravelManager.OnTravelCompleted()
        │
        │ (某些事件触发事件链)
        ▼
EventChainManager.StartChain(chain)
        │
        ├─► onChainStarted
        │
        ├─► EnterNode(startNodeId)
        │       │
        │       └─► onNodeEntered → UI显示对话
        │
        ├─► SelectChoice(choiceId)
        │       │
        │       ├─► ApplyChoiceEffects() → PlayerActor
        │       │
        │       └─► EnterNode(nextNodeId) / CompleteChain()
        │
        └─► onChainCompleted → 继续旅行
```

## 条件选项系统

### 选项可用性检查

```csharp
bool IsChoiceAvailable(EventChoice choice, PlayerActor player)
{
    // 金币检查
    if (choice.goldCost > player.carryItems.gold)
        return false;

    // 标志检查
    if (!string.IsNullOrEmpty(choice.requiredFlag))
        if (!flags.ContainsKey(choice.requiredFlag))
            return false;

    // 物品检查
    if (choice.requiredItemId > 0)
        // TODO: 检查背包

    return true;
}
```

### 前置条件组合

选项可以组合多个前置条件：
- requiredFlag + goldCost：需要标志且消耗金币
- requiredItemId + setFlag：需要物品且设置标志

## 扩展方向

### 待实现功能

1. **物品消耗检查** - 完善 requiredItemId 检查逻辑
2. **模块ID到模块对象转换** - 实现 addModuleIds 的模块创建
3. **风险选项判定** - 实现 Risky 类型选项的成功率判定
4. **剧情分支追踪** - 存档/读取事件链进度
5. **事件链模板** - 从XML/JSON加载事件链配置

### 配置化事件链

```xml
<!-- 潜在的事件链XML配置 -->
<EventChain chainId="merchant_001">
    <Title>路遇商队</Title>
    <StartNode>node_1</StartNode>
    <Nodes>
        <Node id="node_1">
            <Title>商队领队</Title>
            <Description>你好，旅行者！</Description>
            <Choices>
                <Choice id="buy" text="看看货物" nextNode="node_2" goldCost="30"/>
                <Choice id="talk" text="聊聊见闻" nextNode="node_3" setFlag="talked"/>
            </Choices>
        </Node>
    </Nodes>
</EventChain>
```

### UI集成

事件树需要与UI系统配合：
- `UISelectionDialog` - 显示对话和选项
- `ChoiceOption` - 选项数据类
- 需要监听 onNodeEntered 更新对话内容
- 需要监听 onChoiceSelected 触发选项显示