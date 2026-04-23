# 事件树系统 (EventTree)

## 概述

事件树系统管理**分支对话和选择序列**，允许玩家在事件中做出多分支选择，影响后续发展和奖励。与 `EventQueue` 的即时事件不同，事件树是**有状态的连续事件链**。

## 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    EventTreeRunner                           │
│  (单例，事件树运行器，管理分支叙事状态机)                        │
└─────────────────────┬───────────────────────────────────────┘
                      │
          ┌───────────┴───────────┐
          ▼                       ▼
┌─────────────────────┐   ┌─────────────────────┐
│   EventTreeManager  │   │     PlayerActor    │
│  (事件树模板加载)    │   │   (应用选择效果)    │
└─────────┬───────────┘   └─────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│                 EventTreeTemplate                            │
│  (事件树模板：节点列表+起始节点)                               │
└─────────────────────┬───────────────────────────────────────┘
                      │
          ┌───────────┴───────────┐
          ▼                       ▼
┌─────────────────────┐   ┌─────────────────────┐
│   EventTreeNode     │   │   EventTreeChoice   │
│  (节点：类型+内容)   │   │  (选项：条件+效果)    │
└─────────────────────┘   └─────────────────────┘
```

## EventTree vs EventChain

| 特性 | EventTree | EventChain |
|------|-----------|------------|
| 状态机 | EventTreeRunner 管理 | EventChainManager 管理 |
| 历史支持 | Stack<string> 历史栈 | Dictionary<string, string> flags |
| 节点类型 | Root/Story/Choice/Combat/Trade/Reward/End | 简单节点+选项 |
| 配置 | XML模板 | 代码创建或XML |
| 适用场景 | 复杂分支叙事、战斗、交易流程 | 简单线性选择 |

## 核心组件

### EventTreeRunner（事件树运行器）

定义于 `Assets/Scripts/Events/EventTreeRunner.cs`

事件树运行器负责执行事件树模板，支持分支叙事和历史返回。

| 属性 | 类型 | 说明 |
|------|------|------|
| `state` | EventTreeState | 当前状态 |
| `currentTemplate` | EventTreeTemplate | 当前模板 |
| `currentNode` | EventTreeNode | 当前节点 |
| `isRunning` | bool | 是否运行中 |

| 事件回调 | 签名 | 说明 |
|----------|------|------|
| `onTreeStarted` | `Action<EventTreeTemplate>` | 事件树开始时 |
| `onNodeEntered` | `Action<EventTreeNode>` | 进入节点时 |
| `onWaitingForChoice` | `Action<List<EventTreeChoice>>` | 等待玩家选择时 |
| `onTreeCompleted` | `Action` | 事件树完成时 |
| `onTreeCancelled` | `Action` | 事件树取消时 |

### 核心API

```csharp
// 开始事件树
bool StartTree(string templateId)
bool StartTree(EventTreeTemplate template)

// 选择选项
void SelectChoice(string choiceId)

// 跳过当前节点
void SkipNode()

// 返回上一个节点
void GoBack()

// 取消事件树
void Cancel()

// 查询状态
EventTreeState state { get; }
bool isRunning { get; }
EventTreeTemplate currentTemplate { get; }
EventTreeNode currentNode { get; }
List<EventTreeChoice> GetCurrentChoices()
```

### 状态机

```
EventTreeState:
  - Idle         // 空闲
  - Running      // 运行中（自动节点）
  - WaitingChoice // 等待选择
  - Completed   // 完成
  - Cancelled   // 取消
```

## EventTreeNode（事件树节点）

| 属性 | 类型 | 说明 |
|------|------|------|
| `id` | string | 节点ID |
| `type` | EventTreeNodeType | 节点类型 |
| `title` | string | 标题 |
| `content` | string | 内容/描述 |
| `choices` | List<EventTreeChoice> | 选项列表（仅Choice类型） |
| `nextNodeId` | string | 下一个节点ID |
| `effects` | List<string> | 效果ID列表 |

### EventTreeNodeType（节点类型枚举）

| 枚举值 | 说明 | 自动行为 |
|--------|------|----------|
| `Root` | 根节点 | 自动推进 |
| `Story` | 剧情节点 | 自动推进 |
| `Choice` | 选择节点 | 等待玩家选择 |
| `Combat` | 战斗节点 | 自动推进 |
| `Trade` | 交易节点 | 自动推进 |
| `Reward` | 奖励节点 | 自动推进 |
| `End` | 结束节点 | 直接结束 |

### EventTreeChoice（事件树选项）

| 属性 | 类型 | 说明 |
|------|------|------|
| `id` | string | 选项ID |
| `text` | string | 选项文本 |
| `nextNodeId` | string | 下一个节点ID |
| `conditions` | List<string> | 前置条件 |
| `effects` | List<string> | 效果 |

### EventTreeCondition（条件）

| 属性 | 类型 | 说明 |
|------|------|------|
| `type` | string | 条件类型 |
| `value` | string | 条件值 |

## 事件树管理器 (EventTreeManager)

EventTreeManager 是单例，负责：

1. 从XML加载事件树模板
2. 提供模板查询API
3. 支持随机模板获取

```csharp
// 获取模板
EventTreeTemplate GetTemplate(string templateId)

// 获取随机模板
EventTreeTemplate GetRandomTemplate()

// 获取所有模板ID
IReadOnlyCollection<string> GetAllTemplateIds()
```

## 使用示例

```csharp
// 启动事件树
EventTreeRunner.instance.StartTree("merchant_001");

// 监听事件
EventTreeRunner.instance.onTreeStarted += template => {
    Debug.Log($"事件树开始: {template.id}");
};

EventTreeRunner.instance.onNodeEntered += node => {
    Debug.Log($"进入节点: {node.title}");
    // UI显示节点内容
};

EventTreeRunner.instance.onWaitingForChoice += choices => {
    Debug.Log($"等待选择，共{choices.Count}个选项");
    // UI显示选项
};

EventTreeRunner.instance.onTreeCompleted += () => {
    Debug.Log("事件树完成");
    // UI清理
};

// 选择
EventTreeRunner.instance.SelectChoice(choiceId);

// 返回
EventTreeRunner.instance.GoBack();
```

## 与其他系统的交互

```
TravelManager.OnTravelCompleted() → CreateEvent()
        │
        │ (触发事件树)
        ▼
EventTreeRunner.StartTree(templateId)
        │
        ├─► onTreeStarted → UI显示开始
        │
        ├─► EnterNode(nodeId)
        │       │
        │       ├─► onNodeEntered → UI显示内容
        │       │
        │       └─► 节点类型处理:
        │           - Story/Combat/Trade/Reward: 自动推进
        │           - Choice: 等待选择
        │
        ├─► SelectChoice(choiceId) → 更新历史栈
        │
        └─► onTreeCompleted → 继续旅行
```

## 扩展方向

### 待实现功能

1. **效果系统** - EventTreeChoice.effects 列表未被实际执行
2. **条件系统** - EventTreeCondition 解析但未验证
3. **UI集成** - 监听回调更新UI显示
4. **存档支持** - 事件树进度保存/恢复
5. **更多事件树** - 当前只有3个示例，需要扩展内容

### 配置扩展

```xml
<!-- 事件树XML配置 -->
<EventTree id="merchant_001">
    <Nodes>
        <Node id="root" type="Root" title="商队" content="..." nextNode="choice_1"/>
        <Node id="choice_1" type="Choice" title="选择" content="...">
            <Choices>
                <Choice id="buy" text="购买" nextNode="end"/>
                <Choice id="talk" text="交谈" nextNode="end"/>
            </Choices>
        </Node>
        <Node id="end" type="End"/>
    </Nodes>
</EventTree>
```