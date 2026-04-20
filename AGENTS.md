# 必须遵守

1. 对话、思考、GITHUB的提交描述 均必须保持使用中文！
2. 维持同样的代码风格！
   - C#: Assets/Scripts/命名空间/

# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-20
**Project:** Game1 - Unity 6 游戏开发项目
**Commit:** (see git)
**Branch:** main

## OVERVIEW

挂机放置类游戏，包含旅行、事件、Roguelike元素。使用Unity 6.1.5 + URP渲染管线，C# 9.0开发，目标平台Windows64透明悬浮窗。

## STRUCTURE

```
Game1/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/           # 核心系统
│   │   │   ├── GameLoop/   # GameLoopManager
│   │   │   ├── SaveSystem/ # SaveManager
│   │   │   └── EventBus/   # 事件总线
│   │   ├── Entities/       # 实体
│   │   │   ├── Player/     # PlayerActor
│   │   │   └── World/      # WorldMap
│   │   ├── Modules/        # 游戏模块
│   │   │   ├── Idle/      # IdleRewardModule
│   │   │   └── Travel/    # TravelManager
│   │   ├── Events/        # EventQueue
│   │   ├── Roguelike/     # MapGenerator
│   │   ├── UI/            # UI系统
│   │   │   ├── Editor/    # Unity编辑器扩展
│   │   │   ├── UIInventory/  # 背包系统
│   │   │   │   ├── UIInventory.cs    # 背包主组件(List式,勾选/多选)
│   │   │   │   ├── UIInventoryItem.cs # 物品行(Image+Name左, Amount右, 三状态)
│   │   │   │   └── InventoryItemData.cs # 物品数据+状态
│   │   │   ├── UIManager.cs
│   │   │   ├── UIProgressBar.cs
│   │   │   ├── UIText.cs
│   │   │   ├── UILayout.cs
│   │   │   ├── UIListItems.cs
│   │   │   └── XUtilities.cs
│   │   ├── GamePlay/      # GamePlay
│   │   └── Managers/      # 管理器
│   ├── Scenes/            # Unity场景
│   ├── Settings/          # URP设置
│   ├── Shaders/          # 着色器
│   └── Resources/         # 资源
├── Packages/             # Unity包 (含UniWindowController)
├── ProjectSettings/      # 项目配置
└── Game1.sln            # VS解决方案
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| 游戏入口 | Assets/Scripts/GameMain.cs | 单例模式 |
| UI管理器 | Assets/Scripts/UI/UIManager.cs | 面板状态管理 |
| UI组件 | Assets/Scripts/UI/*.cs | ProgressBar, Text, Layout等 |
| 游戏循环 | Assets/Scripts/Core/GameLoop/ | Tick协调 |
| 事件系统 | Assets/Scripts/Core/EventBus/ | 发布-订阅 |
| 存档系统 | Assets/Scripts/Core/SaveSystem/ | JSON持久化 |
| 玩家数据 | Assets/Scripts/Entities/Player/ | PlayerActor |
| 旅行系统 | Assets/Scripts/Modules/Travel/ | 节点地图 |
| 挂机收益 | Assets/Scripts/Modules/Idle/ | IdleRewardModule |
| 透明窗口 | Packages/UniWindowController | 悬浮窗支持 |
| 场景 | Assets/Scenes/SampleScene.unity | 测试场景 |

## CODE MAP

### Core Classes

| Class | Location | Role |
|-------|----------|------|
| GameMain | GameMain.cs | Unity入口，单例 |
| GameLoopManager | Core/GameLoop/ | 主循环Tick协调 |
| SaveManager | Core/SaveSystem/ | 存档管理 |
| EventBus | Core/EventBus/ | 事件发布-订阅 |
| PlayerActor | Entities/Player/ | 玩家数据+模块 |
| TravelManager | Modules/Travel/ | 旅行进度 |
| IdleRewardModule | Modules/Idle/ | 挂机收益 |
| WorldMap | Entities/World/ | 节点地图 |
| EventQueue | Events/ | 事件队列 |
| MapGenerator | Roguelike/ | 随机地图 |
| UIManager | UI/UIManager.cs | UI状态机 |
| UIInventory | UI/UIInventory/UIInventory.cs | 背包主组件(列表/勾选/多选) |
| UIInventoryItem | UI/UIInventory/UIInventoryItem.cs | 物品行(高亮/默认/禁用) |
| InventoryItemData | UI/UIInventory/InventoryItemData.cs | 物品数据+状态 |
| UIProgressBar | UI/UIProgressBar.cs | 进度条 |
| UIText | UI/UIText.cs | TextMeshPro封装 |
| UILayout | UI/UILayout.cs | 布局系统 |
| UIListItems | UI/UIListItems.cs | 列表管理 |
| XUniTaskProgress | UI/XUtilities.cs | 任务进度 |
| XObjectPool | UI/XUtilities.cs | 对象池 |

## CONVENTIONS (C#)

- **命名空间**: `Game1` (根)
- **命名**: PascalCase (MonoBehaviour方法: UpdateScore)
- **私有字段**: `_camelCase` 或 `m_CamelCase`
- **SerializeField**: private字段需序列化时显式标记
- **MonoBehaviour**: 避免空脚本
- **Input**: 使用 `UnityEngine.InputSystem` (Keyboard.current)

## ANTI-PATTERNS (THIS PROJECT)

- **禁止**: FindObjectOfType in Update (Awake/Start缓存)
- **禁止**: GetComponent in Update (缓存引用)
- **禁止**: public字段无[SerializeField]暴露实现
- **禁止**: 空MonoBehaviour
- **避免**: 同步加载大资源 (用Addressables/async)
- **避免**: UnityEngine.Input (用InputSystem)

## UNIQUE STYLES

- **透明悬浮窗**: UniWindowController + D3D11 + FlipModel禁用
- **前台悬浮**: 无边框、置顶、点击穿透
- **输入系统**: New Input System (com.unity.inputsystem)
- **UI架构**: UIManager状态机 + EventBus解耦

## TECHNICAL STACK

| 组件 | 版本 |
|------|------|
| Unity | 6000.1.5f1 |
| URP | 17.1.0 |
| C# | 9.0 |
| .NET | 4.7.1 |
| Input System | 1.14.0 |
| UniWindowController | 0.9.8 |
| TextMeshPro | (TMP bundled) |

## COMMANDS

```bash
# Unity Editor
unity -openProject Game1

# Headless Build
unity -batchmode -buildTarget StandaloneWindows64 -quit

# Play Mode测试
unity -batchmode -runTests -testPlatform playmode
```

## NOTES

- 透明窗口要求: D3D11 + FlipModel禁用 + HDR关闭
- PlayerSettings: useFlipModelSwapchain=0, preserveFramebufferAlpha=1
- Camera: HDR=Off, PostProcessing=Off, Background=(0,0,0,0)
- 默认分辨率: 1024x768
- UniWindowController Prefab: Packages/UniWindowController/Runtime/Prefabs/
- Editor脚本放 Assets/Scripts/UI/Editor/ (自动排除构建)
- XUtilities为UI系统存根实现，游戏逻辑需补充

## GIT WORKFLOW

```bash
git add -A
git commit -m "描述"
git push origin main
```