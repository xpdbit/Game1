# 必须遵守

1. 对话、思考、GITHUB的提交描述 均必须保持使用中文！
2. 维持同样的代码风格！
   - C#: Assets/Scripts/命名空间/

# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-22
**Project:** Game1 - Unity 6 游戏开发项目
**Commit:** d387289
**Branch:** main

## OVERVIEW

挂机放置类游戏，包含旅行、事件、Roguelike、战斗元素。使用Unity 6.1.5 + URP渲染管线，C# 9.0开发，目标平台Windows64透明悬浮窗。

## STRUCTURE

```
Game1/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/           # 核心系统
│   │   │   ├── GameLoop/   # GameLoopManager
│   │   │   ├── SaveSystem/ # SaveManager
│   │   │   ├── EventBus/   # 事件总线
│   │   │   └── Utils/      # 工具类
│   │   ├── Combat/         # 战斗系统
│   │   │   └── CombatSystem.cs
│   │   ├── Entities/       # 实体
│   │   │   ├── Player/     # PlayerActor
│   │   │   ├── World/      # WorldMap
│   │   │   └── NPC/        # NPCSystem
│   │   ├── Inventory/      # 背包系统
│   │   │   └── InventoryDesign.cs
│   │   ├── Modules/        # 游戏模块
│   │   │   ├── Idle/       # IdleRewardModule
│   │   │   └── Travel/     # TravelManager, ProgressManager
│   │   ├── Events/         # 事件系统
    │   │   │   ├── EventQueue.cs
    │   │   │   ├── EventChain.cs
    │   │   │   ├── EventManager.cs    # 事件管理器(模板加载)
    │   │   │   └── EventTreeManager.cs # 事件树管理器(配置加载)
│   │   ├── Roguelike/      # MapGenerator
│   │   ├── UI/             # UI系统
│   │   │   ├── Dialog/     # UISelectionDialog
│   │   │   ├── Map/        # UIMapPath
│   │   │   ├── Editor/     # Unity编辑器扩展
│   │   │   ├── UIInventory/
│   │   │   ├── UIManager.cs
│   │   │   ├── UIProgressBar.cs
│   │   │   ├── UIText.cs
│   │   │   ├── UILayout.cs
│   │   │   ├── UIListItems.cs
│   │   │   └── Utils/      # UI工具
│   │   ├── Managers/       # 管理器
│   │   │   ├── ItemManager.cs
│   │   │   └── ResourceManager.cs
│   │   ├── GameMain.cs     # 游戏入口
│   │   └── GameTest.cs     # 测试类
│   ├── Scenes/            # Unity场景
│   ├── Settings/          # URP设置
│   ├── Shaders/          # 着色器
│   └── Resources/         # 资源
        └── Data/
            ├── Items/         # 物品配置 (Items.xml)
            ├── Events/       # 事件配置 (Events.xml)
            └── EventTrees/   # 事件树配置 (EventTrees.xml)
├── Packages/             # Unity包 (含UniWindowController)
├── ProjectSettings/      # 项目配置
├── Docs/                 # 文档
│   ├── Item/            # 物品文档 (ItemList.md)
│   ├── Event/           # 事件文档 (EventList.md)
│   └── EventTree/        # 事件树文档 (EventTreeList.md)
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
| 背包系统 | Assets/Scripts/Inventory/ | InventoryDesign核心逻辑 |
| 战斗系统 | Assets/Scripts/Combat/ | CombatSystem |
| NPC系统 | Assets/Scripts/Entities/NPC/ | NPCSystem |
| 事件系统 | Assets/Scripts/Events/ | EventManager, EventChain |
| 事件链 | Assets/Scripts/Events/EventChain.cs | 事件链系统 |
| 选择对话框 | Assets/Scripts/UI/Dialog/ | UISelectionDialog |
| 地图路径 | Assets/Scripts/UI/Map/ | UIMapPath |

## CODE MAP

### Core Classes

| Class | Location | Role |
|-------|----------|------|
| GameMain | GameMain.cs | Unity入口，单例 |
| GameConfig | GameMain.cs | 游戏配置（占位） |
| GameLoopManager | Core/GameLoop/ | 主循环Tick协调 |
| SaveManager | Core/SaveSystem/ | 存档管理 |
| EventBus | Core/EventBus/ | 事件发布-订阅 |
| PlayerActor | Entities/Player/ | 玩家数据+模块 |
| IModule | Entities/Player/PlayerActor.cs | 模块接口 |
| TravelState | Entities/Player/PlayerActor.cs | 旅行状态 |
| ModuleCollection | Entities/Player/PlayerActor.cs | 模块集合管理 |
| TravelManager | Modules/Travel/ | 旅行进度 |
| IdleRewardModule | Modules/Idle/ | 挂机收益 |
| BonusMultiplierModule | Modules/Idle/ | 加成倍率 |
| WorldMap | Entities/World/ | 节点地图 |
| Location | Entities/World/ | 地点节点 |
| NPCSystem | Entities/NPC/ | NPC系统 |
| CombatSystem | Combat/ | 战斗系统 |
| EventQueue | Events/ | 事件队列 |
| IGameEvent | Events/EventQueue.cs | 游戏事件接口 |
| EventResult | Events/EventQueue.cs | 事件结果 |
| CombatEvent | Events/EventQueue.cs | 战斗事件示例 |
| TradeEvent | Events/EventQueue.cs | 交易事件示例 |
| EventChainNode | Events/EventChain.cs | 事件链节点 |
| EventChoice | Events/EventChain.cs | 事件选项 |
| EventManager | Events/EventManager.cs | 事件模板管理器（XML加载） |
| EventTreeManager | Events/EventTreeManager.cs | 事件树模板管理器（XML加载） |
| EventTemplate | Events/EventManager.cs | 事件模板数据结构 |
| EventTreeTemplate | Events/EventTreeManager.cs | 事件树模板数据结构 |
| MapGenerator | Roguelike/ | 随机地图 |
| UIState | UI/UIManager.cs | UI状态枚举 |
| IUIPanel | UI/UIManager.cs | 面板接口 |
| BaseUIPanel | UI/UIManager.cs | 面板基类 |
| GameHUDPanel | UI/UIManager.cs | HUD面板 |
| UIManager | UI/UIManager.cs | UI状态机 |
| UIInventory | UI/UIInventory/UIInventory.cs | 背包主组件 |
| UIInventoryItem | UI/UIInventory/UIInventoryItem.cs | 物品行 |
| UIProgressBar | UI/UIProgressBar.cs | 进度条（支持四方向） |
| UIText | UI/UIText.cs | TextMeshPro封装 |
| UITextLinkHandler | UI/UIText.cs | 文本链接处理（存根） |
| UILayout | UI/UILayout.cs | 布局系统（拉伸/平铺） |
| LayoutSender | UI/UILayout.cs | 布局参数传递 |
| UIListItems | UI/UIListItems.cs | 列表管理 |
| UISelectionDialog | UI/Dialog/ | 选择对话框 |
| UIMapPath | UI/Map/ | 地图路径UI |
| XUniTaskProgress | UI/Utils/UniTaskProgress.cs | 任务进度 |
| XObjectPool | UI/Utils/UIUtils.cs | 对象池 |
| InventoryItemData | UI/UIInventory/InventoryItemData.cs | 物品数据+状态 |
| InventoryDesign | Inventory/ | 背包核心逻辑（非MonoBehaviour） |
| InventoryOperationResult | Inventory/InventoryDesign.cs | 操作结果 |
| InventoryCapacity | Inventory/InventoryDesign.cs | 容量配置 |
| ResourceManager | Managers/ResourceManager.cs | 资源加载（JSON/XML/手动解析） |
| ItemTemplate | Managers/ItemManager.cs | 物品模板，含ParseFromXml |
| ItemInstance | Managers/ItemManager.cs | 物品实例 |
| ItemType | Managers/ItemManager.cs | 物品类型枚举 |

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
- XUtilities已重构为UI/Utils/UIUtils.cs，命名空间Game1.UI.Utils
- IModule接口定义在 PlayerActor.cs 中
- 物品配置使用XML: Resources/Data/Items/Items.xml
- ItemTemplate.ParseFromXml使用SelectSingleNode路径解析
- ResourceManager.Get<T>(id)提供资源查找入口
- ItemManager.Initialize使用[RuntimeInitializeOnLoadMethod]在场景加载前初始化
- InventoryDesign是纯逻辑类（非MonoBehaviour），提供背包核心操作
- UIListItems使用对象池(XObjectPool)管理列表项实例
- EventChain提供事件链/分支叙事功能
- 事件配置使用XML: Resources/Data/Events/Events.xml
- 事件树配置使用XML: Resources/Data/EventTrees/EventTrees.xml
- EventManager管理事件模板和事件链配置加载
- EventTreeManager管理事件树模板加载，支持分支叙事
- 事件类型枚举: Random, Choice, Combat, Trade, Discovery, Story
- 事件树节点类型: Root, Choice, Random, End

## GIT WORKFLOW

```bash
git add -A
git commit -m "描述"
git push origin main
```