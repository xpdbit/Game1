# 必须遵守

1. 对话、思考、GITHUB的提交描述 均必须保持使用中文！
2. 维持同样的代码风格！
   - C#: Assets/Scripts/命名空间/

# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-23
**Project:** Game1 - Unity 6 游戏开发项目
**Commit:** e9be3b5
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
│   │   │   ├── Input/      # 输入管理
│   │   │   │   ├── BackgroundInputManager.cs  # 后台输入管理（UniWindowController）
│   │   │   │   ├── GlobalKeyboardHook.cs      # 全局键盘钩子（Windows API）
│   │   │   │   └── InputConverter.cs           # 虚实交互输入转换
│   │   │   ├── Debug/      # GameDebug调试信息
│   │   │   └── Utils/      # 工具类 (ResourceManager, Utils, UniTaskProgress)
│   │   ├── Combat/         # 战斗系统 (已废弃，请使用 Modules/Combat/)
│   │   ├── Entities/       # 实体
│   │   │   ├── Player/     # PlayerActor
│   │   │   ├── World/      # WorldMap
│   │   │   └── NPC/        # NPCSystem
│   │   ├── Inventory/      # 背包系统 (已废弃，请使用 Modules/Inventory/)
│   │   ├── Modules/        # 游戏模块
│   │   │   ├── Idle/       # IdleRewardModule
│   │   │   ├── Travel/     # TravelManager, ProgressManager
│   │   │   ├── Combat/     # CombatSystem (移动自根目录 Combat/)
│   │   │   ├── Team/       # TeamDesign, TeamManager, TeamMemberData, JobSystem
│   │   │   ├── Inventory/  # InventoryDesign, ItemManager, InventoryEvents, InventoryItemData, EquipmentSystem
│   │   │   ├── Skill/      # SkillDesign, SkillManager, SkillData (NEW)
│   │   │   ├── Card/       # CardDesign, CardManager, CardData (NEW)
│   │   │   ├── Prestige/   # PrestigeManager (NEW)
│   │   │   └── PVP/        # PVPMatchManager, PVPArenaManager (NEW)
│   │   ├── Events/         # 事件系统
│   │   │   ├── EventQueue.cs
│   │   │   ├── EventChain.cs
│   │   │   ├── EventManager.cs    # 事件管理器(模板加载)
│   │   │   ├── EventTreeManager.cs # 事件树管理器(配置加载)
│   │   │   └── EventTreeRunner.cs  # 事件树运行器(分支叙事)
│   │   ├── Roguelike/      # MapGenerator
│   │   ├── UI/             # UI系统
│   │   │   ├── Dialog/     # UISelectionDialog
│   │   │   ├── Map/        # UIMapPath
│   │   │   ├── Editor/     # Unity编辑器扩展
│   │   │   ├── UITeam.cs   # 队伍UI
│   │   │   ├── UIInventory.cs
│   │   │   ├── UIManager.cs
│   │   │   ├── UIProgressBar.cs
│   │   │   ├── UIText.cs
│   │   │   ├── UILayout.cs
│   │   │   ├── UIListItems.cs
│   │   │   └── Utils/      # UI工具
│   │   ├── Managers/       # 管理器 (已废弃，请使用 Modules/Inventory/)
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
| 调试信息 | Assets/Scripts/Core/Debug/ | GameDebug运行时显示 |
| 事件系统 | Assets/Scripts/Core/EventBus/ | 发布-订阅 |
| 存档系统 | Assets/Scripts/Core/SaveSystem/ | JSON持久化 |
| 玩家数据 | Assets/Scripts/Entities/Player/ | PlayerActor |
| 旅行系统 | Assets/Scripts/Modules/Travel/ | 节点地图 |
| 挂机收益 | Assets/Scripts/Modules/Idle/ | IdleRewardModule |
| 透明窗口 | Packages/UniWindowController | 悬浮窗支持 |
| 场景 | Assets/Scenes/SampleScene.unity | 测试场景 |
| 背包系统 | Assets/Scripts/Modules/Inventory/ | InventoryDesign核心逻辑 |
| 战斗系统 | Assets/Scripts/Modules/Combat/ | CombatSystem |
| NPC系统 | Assets/Scripts/Entities/NPC/ | NPCSystem |
| 事件系统 | Assets/Scripts/Events/ | EventManager, EventChain, EventTreeManager, EventTreeRunner |
| 事件树 | Assets/Scripts/Events/EventTreeRunner.cs | 分支叙事运行器 |
| 事件链 | Assets/Scripts/Events/EventChain.cs | 事件链系统 |
| 选择对话框 | Assets/Scripts/UI/Dialog/ | UISelectionDialog |
| 地图路径 | Assets/Scripts/UI/Map/ | UIMapPath |
| 队伍系统 | Assets/Scripts/Modules/Team/ | TeamDesign, TeamManager |
| 队伍UI | Assets/Scripts/UI/UITeam.cs | 队伍面板 |

## CODE MAP

### Core Classes

| Class | Location | Role |
|-------|----------|------|
| GameMain | GameMain.cs | Unity入口，单例 |
| GameConfig | GameMain.cs | 游戏配置（占位） |
| GameLoopManager | Core/GameLoop/ | 主循环Tick协调 |
| SaveManager | Core/SaveSystem/ | 存档管理 |
| EventBus | Core/EventBus/ | 事件发布-订阅 |
| BackgroundInputManager | Core/Input/ | 后台输入管理（UniWindowController） |
| GameDebug | Core/Debug/ | 调试信息管理器（运行时显示） |
| ResourceManager | Core/Utils/ | 资源加载（JSON/XML/手动解析） |
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
| CombatSystem | Modules/Combat/ | 战斗系统 |
| CombatEventEx | Modules/Combat/CombatSystem.cs | 战斗事件扩展（virtual/override多态） |
| TeamDesign | Modules/Team/ | 队伍核心逻辑（单例） |
| TeamManager | Modules/Team/ | 队伍管理器（静态API） |
| TeamMemberData | Modules/Team/ | 队伍成员数据结构 |
| JobSystem | Modules/Team/ | 职业系统（商贾/镖师/学者/医者） |
| JobType | Modules/Team/ | 职业类型枚举 |
| JobBonus | Modules/Team/ | 职业加成数据结构 |
| EquipmentSystem | Modules/Inventory/ | 装备系统（装备/强化/属性计算） |
| EquipmentSlot | Modules/Inventory/ | 装备槽位枚举 |
| SkillDesign | Modules/Skill/ | 技能核心逻辑（单例） |
| SkillManager | Modules/Skill/ | 技能管理器（静态API） |
| SkillData | Modules/Skill/ | 技能数据（运行时） |
| SkillTemplate | Modules/Skill/ | 技能模板（配置） |
| SkillType | Modules/Skill/ | 技能类型枚举（Passive/Active/Ultimate） |
| CardDesign | Modules/Card/ | 卡牌核心逻辑（单例） |
| CardManager | Modules/Card/ | 卡牌管理器（静态API） |
| CardData | Modules/Card/ | 卡牌数据 |
| CardType | Modules/Card/ | 卡牌类型枚举 |
| CardRarity | Modules/Card/ | 卡牌稀有度（N/R/SR/SSR/UR/GR） |
| GachaType | Modules/Card/ | 抽卡类型枚举 |
| PrestigeManager | Modules/Prestige/ | 轮回系统（点数/商店/资源保留） |
| PrestigeUpgrade | Modules/Prestige/ | 轮回升级数据 |
| PVPMatchManager | Modules/PVP/ | PVP匹配管理器 |
| PVPArenaManager | Modules/PVP/ | PVP竞技场管理器 |
| GlobalKeyboardHook | Core/Input/ | Windows全局键盘钩子 |
| InputConverter | Core/Input/ | 虚实交互输入转换 |
| EventQueue | Events/ | 事件队列 |
| IGameEvent | Events/EventQueue.cs | 游戏事件接口 |
| EventResult | Events/EventQueue.cs | 事件结果 |
| CombatEvent | Events/EventQueue.cs | 战斗事件示例 |
| TradeEvent | Events/EventQueue.cs | 交易事件示例 |
| EventChainNode | Events/EventChain.cs | 事件链节点 |
| EventChoice | Events/EventChain.cs | 事件选项 |
| EventManager | Events/EventManager.cs | 事件模板管理器（XML加载） |
| EventTreeManager | Events/EventTreeManager.cs | 事件树模板管理器（XML加载） |
| EventTreeRunner | Events/EventTreeRunner.cs | 事件树运行器（分支叙事执行） |
| EventTreeState | Events/EventTreeRunner.cs | 事件树运行状态枚举 |
| EventTemplate | Events/EventManager.cs | 事件模板数据结构 |
| EventTreeTemplate | Events/EventTreeManager.cs | 事件树模板数据结构 |
| MapGenerator | Roguelike/ | 随机地图 |
| UIState | UI/UIManager.cs | UI状态枚举 |
| IUIPanel | UI/UIManager.cs | 面板接口 |
| BaseUIPanel | UI/UIManager.cs | 面板基类 |
| GameHUDPanel | UI/UIManager.cs | HUD面板 |
| UIManager | UI/UIManager.cs | UI状态机 |
| UIInventory | UI/UIInventory.cs | 背包主组件 |
| UIInventoryItem | UI/UIInventory.cs | 物品行 |
| UITeam | UI/UITeam.cs | 队伍UI主组件 |
| UITeamMember | UI/UITeam.cs | 队伍成员行 |
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
| InventoryItemData | Modules/Inventory/ | 物品数据+状态 |
| InventoryDesign | Modules/Inventory/ | 背包核心逻辑（非MonoBehaviour） |
| InventoryOperationResult | Modules/Inventory/InventoryDesign.cs | 操作结果 |
| InventoryCapacity | Modules/Inventory/InventoryDesign.cs | 容量配置 |
| ItemTemplate | Modules/Inventory/ItemManager.cs | 物品模板，含ParseFromXml |
| ItemInstance | Modules/Inventory/ItemManager.cs | 物品实例 |
| ItemType | Modules/Inventory/ItemManager.cs | 物品类型枚举 |
| ActorManager | Entities/Actor/ActorManager.cs | 角色模板管理器 |
| ActorTemplate | Entities/Actor/ActorManager.cs | 角色模板，含ParseFromXml |
| TeamOperationResult | Modules/Team/TeamDesign.cs | 队伍操作结果 |
| TeamCapacity | Modules/Team/TeamDesign.cs | 队伍容量配置 |
| TeamEventData | Modules/Team/TeamDesign.cs | 队伍事件数据 |

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

## INITIALIZATION ORDER

正确的初始化顺序：

```
1. ResourceManager.Initialize()
   └─► 加载配置（Items.xml, Events.xml等）

2. ItemManager.Initialize() [RuntimeInitializeOnLoadMethod]
   └─► 加载物品模板

3. ActorManager.Initialize() [RuntimeInitializeOnLoadMethod]
   └─► 加载角色模板

4. GameMain.instance.Initialize()
   │
   ├─► PlayerActor.Initialize()
   │
   ├─► TravelManager.Initialize(player)
   │
   ├─► EventQueue.Initialize()
   │
   └─► SaveManager.LoadSaveData() → 应用存档
```

依赖关系：ResourceManager → ItemManager/ActorManager → GameMain → SaveManager

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
- 后台输入使用 UniWindowController 静态API（UniWinCore为internal不可直接访问）
- Editor脚本放 Assets/Scripts/UI/Editor/ (自动排除构建)
- XUtilities已重构为UI/Utils/UIUtils.cs，命名空间Game1.UI.Utils
- IModule接口定义在 PlayerActor.cs 中
- 物品配置使用XML: Resources/Data/Items/Items.xml
- ItemTemplate.ParseFromXml使用SelectSingleNode路径解析
- ResourceManager.Load<T>(path)提供资源查找入口
- ItemManager.Initialize使用[RuntimeInitializeOnLoadMethod]在场景加载前初始化
- InventoryDesign是纯逻辑类（非MonoBehaviour），提供背包核心操作
- UIListItems使用对象池(XObjectPool)管理列表项实例
- EventChain提供事件链/分支叙事功能
- 事件配置使用XML: Resources/Data/Events/Events.xml
- 事件树配置使用XML: Resources/Data/EventTrees/EventTrees.xml
- EventManager管理事件模板和事件链配置加载
- EventTreeManager管理事件树模板加载，支持分支叙事
- EventTreeRunner执行事件树，支持分支选择、历史返回、随机开始
- 事件类型枚举: Random, Choice, Combat, Trade, Discovery, Story
- 事件树节点类型: Root, Choice, Random, End
- ProgressManager提供进度点系统，每200点触发普通事件，每1000点触发事件树
- ProgressManager.travelRate使用滑动窗口计算过去60秒的平均TP/s
- TravelPoint超出travelPointSize(默认1000)时归零重新累计
- Team模块采用与Inventory模块相同的设计模式:
  - TeamDesign: 单例非MonoBehaviour，核心逻辑
  - TeamManager: 静态API，委托给TeamDesign
  - TeamMemberData: 成员数据结构
  - UITeam: UI面板，继承BaseUIPanel
- 全局输入系统（NEW）:
  - GlobalKeyboardHook: 使用Windows API SetWindowsHookEx实现全局键盘监听
  - InputConverter: 键盘敲击转脚程、鼠标移动转校准、连击加成计算
  - BackgroundInputManager: 集成UniWindowController鼠标 + GlobalKeyboardHook键盘
- 虚实交互输入转换算法:
  - 每10次键盘敲击 = 1秒脚程
  - 每100px鼠标移动 = 0.1秒脚程
  - 连击窗口1秒，每10次连击+0.1，最高1.5
  - 精准校准：静止>2秒后移动 = 2x加成
- InputConverter使用UnityEngine.InputSystem.Mouse.current.position.ReadValue()获取鼠标位置，兼容New Input System

## GIT WORKFLOW

```bash
git add -A
git commit -m "描述"
git push origin main
```