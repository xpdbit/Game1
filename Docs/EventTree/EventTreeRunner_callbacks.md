# EventTreeRunner 事件回调文档

## 概述

`EventTreeRunner` 是事件树运行器，负责执行事件树模板，支持分支叙事。它提供了完整的事件回调系统，UI层可以订阅这些回调来实现事件树的显示。

## 事件回调列表

| 回调 | 签名 | 触发时机 |
|------|------|----------|
| `onTreeStarted` | `Action<EventTreeTemplate>` | 事件树开始时 |
| `onNodeEntered` | `Action<EventTreeNode>` | 进入节点时 |
| `onWaitingForChoice` | `Action<List<EventTreeChoice>>` | 等待玩家选择时 |
| `onTreeCompleted` | `Action` | 事件树完成时 |
| `onTreeCancelled` | `Action` | 事件树取消时 |

## 状态机

```
EventTreeState:
  - Idle         // 空闲
  - Running      // 运行中
  - WaitingChoice // 等待选择
  - Completed    // 完成
  - Cancelled    // 取消
```

## 使用示例

### 订阅事件回调

```csharp
// 在UI初始化时订阅
EventTreeRunner.instance.onTreeStarted += OnTreeStarted;
EventTreeRunner.instance.onNodeEntered += OnNodeEntered;
EventTreeRunner.instance.onWaitingForChoice += OnWaitingForChoice;
EventTreeRunner.instance.onTreeCompleted += OnTreeCompleted;
EventTreeRunner.instance.onTreeCancelled += OnTreeCancelled;

// 事件处理方法
private void OnTreeStarted(EventTreeTemplate template)
{
    Debug.Log($"[UI] 事件树开始: {template.id}");
    // 显示事件树标题
}

private void OnNodeEntered(EventTreeNode node)
{
    Debug.Log($"[UI] 进入节点: {node.id}");
    // 显示节点内容
}

private void OnWaitingForChoice(List<EventTreeChoice> choices)
{
    Debug.Log($"[UI] 等待选择，共{choices.Count}个选项");
    // 显示选择对话框
}

private void OnTreeCompleted()
{
    Debug.Log("[UI] 事件树完成");
    // 关闭事件树面板
}

private void OnTreeCancelled()
{
    Debug.Log("[UI] 事件树取消");
    // 处理取消逻辑
}
```

### 启动事件树

```csharp
// 通过模板ID启动
bool success = EventTreeRunner.instance.StartTree("merchant_001");

// 或通过模板对象启动
var template = EventTreeManager.GetTemplate("merchant_001");
if (template != null)
{
    EventTreeRunner.instance.StartTree(template);
}
```

### 选择选项

```csharp
// 玩家选择后调用
EventTreeRunner.instance.SelectChoice(choiceId);
```

### 返回上一个节点

```csharp
// 支持历史返回
EventTreeRunner.instance.GoBack();
```

### 跳过当前节点

```csharp
// 跳过非选择节点
EventTreeRunner.instance.SkipNode();
```

### 取消事件树

```csharp
// 取消正在运行的事件树
EventTreeRunner.instance.Cancel();
```

## 节点类型处理

| 节点类型 | 自动行为 |
|----------|----------|
| `Root` | 自动推进到下一个节点 |
| `Story` | 自动推进到下一个节点 |
| `Combat` | 自动推进到下一个节点 |
| `Trade` | 自动推进到下一个节点 |
| `Reward` | 自动推进到下一个节点 |
| `Choice` | 等待玩家选择 |
| `End` | 直接结束事件树 |

## 状态查询

```csharp
// 当前状态
var state = EventTreeRunner.instance.state;

// 是否运行中
bool running = EventTreeRunner.instance.isRunning;

// 当前模板
var template = EventTreeRunner.instance.currentTemplate;

// 当前节点
var node = EventTreeRunner.instance.currentNode;

// 获取当前选项（等待选择时）
var choices = EventTreeRunner.instance.GetCurrentChoices();
```

## 与UI集成建议

### UISelectionDialog 集成

```csharp
public class EventTreeUI : MonoBehaviour
{
    public UISelectionDialog dialogPrefab;
    private UISelectionDialog _currentDialog;

    void Start()
    {
        EventTreeRunner.instance.onWaitingForChoice += ShowChoiceDialog;
        EventTreeRunner.instance.onTreeCompleted += OnEventTreeCompleted;
        EventTreeRunner.instance.onNodeEntered += OnNodeEntered;
    }

    void ShowChoiceDialog(List<EventTreeChoice> choices)
    {
        // 创建或显示对话框
        var dialog = Instantiate(dialogPrefab);
        dialog.Show(
            title: "选择",
            options: choices.Select(c => c.text).ToList(),
            onSelect: (index) => {
                EventTreeRunner.instance.SelectChoice(choices[index].id);
            }
        );
    }

    void OnNodeEntered(EventTreeNode node)
    {
        // 显示节点内容
        Debug.Log($"显示节点: {node.title}\n{node.description}");
    }

    void OnEventTreeCompleted()
    {
        // 清理UI
        if (_currentDialog != null)
        {
            Destroy(_currentDialog.gameObject);
        }
    }
}
```

## 注意事项

1. **订阅时机**：建议在UI初始化时订阅，在UI关闭时取消订阅
2. **生命周期**：EventTreeRunner 是单例，整个游戏生命周期存在
3. **多订阅者**：支持多个订阅者同时监听事件
4. **线程安全**：事件在主线程触发，无需担心线程安全问题