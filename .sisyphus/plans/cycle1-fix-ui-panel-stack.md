# Cycle 1: 修复UI面板堆栈Bug

## 目标
修复 `UIManager.cs` 中面板堆栈管理Bug，使面板导航（打开/关闭/返回）正确工作。

## 背景
- 10代理分析确认 `PendingEventDesign.cs` 已存在，无需创建
- UI分析代理确认 `_panelStack.Clear()` 在 `ClosePanel()` 中清空整个堆栈，破坏返回导航
- 该Bug由两个问题构成

## Bug 分析

### Bug 1: ClosePanel 清空整个堆栈（307-313行）
```csharp
panel.OnClose();
_panelStack.Clear(); // ← 这行破坏了堆栈！
```

影响：当因状态切换（`OnStateExit`）或直接调用关闭面板时，整个浏览历史被清空，`GoBack()` 无法工作。

### Bug 2: GoBack 调用 CloseAllPanels（335-340行）
```csharp
_panelStack.Pop();
CloseAllPanels(); // ← 清空了堆栈，导致 OpenPanel(previousId) 在空堆栈上操作
OpenPanel(previousId);
```

影响：返回后堆栈状态不一致，再次返回可能出错。

## 修复方案

### ClosePanel（精确移除）
```csharp
panel.OnClose();
// 从堆栈中移除该面板（如果存在）
var tempStack = new Stack<string>();
while (_panelStack.Count > 0)
{
    var top = _panelStack.Pop();
    if (top != panelId)
        tempStack.Push(top);
}
while (tempStack.Count > 0)
    _panelStack.Push(tempStack.Pop());
```

### GoBack（只关闭当前面板）
```csharp
string currentId = _panelStack.Pop();
string previousId = _panelStack.Peek();

// 只关闭当前面板，而不是所有面板
ClosePanel(currentId);  // 使用修复后的 ClosePanel 精确移除

// 打开上一个面板（如果它被关闭了）
if (_panels.TryGetValue(previousId, out var previousPanel) && !previousPanel.isOpen)
    previousPanel.OnOpen();
if (!_panelStack.Contains(previousId))
    _panelStack.Push(previousId);
```

## 子任务

1. **修复 ClosePanel** - 精确移除面板（不清空堆栈）
2. **修复 GoBack** - 只处理当前面板（不清空堆栈）
3. **验证编译** - LSP diagnostics 通过
