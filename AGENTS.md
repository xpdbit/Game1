# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-29
**Project:** Game1 - Unity 6 游戏开发项目
**Commit:** 5e78657
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
│   │   │   ├── Audio/      # 音频系统 (AudioManager, AudioPresets)
│   │   │   └── Utils/      # 工具类 (ResourceManager, Utils, UniTaskProgress)
│   │   ├── Combat/         # 战斗系统 (已废弃，请使用 Modules/Combat/)
│   │   ├── Entities/       # 实体
│   │   │   ├── Player/     # PlayerActor
│   │   │   ├── World/      # WorldMap
│   │   │   ├── NPC/        # NPCSystem
│   │   │   └── Actor/      # ActorManager, ActorTemplate
│   │   ├── Inventory/      # 背包系统 (已废弃，请使用 Modules/Inventory/)
│   │   ├── Modules/        # 游戏模块
│   │   │   ├── Idle/       # IdleRewardModule
│   │   │   ├── Travel/     # TravelManager, ProgressManager
│   │   │   ├── Combat/     # CombatSystem (移动自根目录 Combat/)
│   │   │   │   ├── Commands/ # 战斗命令系统 (ICombatCommand, AttackCommand, 等)
│   │   │   │   └── State/   # 战斗状态机 (CombatContext, CombatPhase, CombatStateMachine)
│   │   │   ├── Team/       # TeamDesign, TeamManager, TeamMemberData, JobSystem
│   │   │   ├── Inventory/  # InventoryDesign, ItemManager, InventoryEvents, InventoryItemData, EquipmentSystem
│   │   │   ├── Skill/      # SkillDesign, SkillManager, SkillData
│   │   │   ├── Card/       # CardDesign, CardManager, CardData
│   │   │   ├── Prestige/   # PrestigeManager
│   │   │   ├── PVP/        # PVPMatchManager, PVPArenaManager
│   │   │   ├── Pet/         # PetCompanionModule, PetCompanionPanel (随队宠物)
│   │   │   ├── Activity/   # ActivityMonitorModule (活跃度监测)
│   │   │   ├── Achievement/ # AchievementDesign, AchievementManager (成就系统)
│   │   │   └── Task/        # TaskDesign, TaskManager (任务系统)
│   │   ├── Events/         # 事件系统
│   │   │   ├── EventQueue.cs
│   │   │   ├── EventChain.cs
│   │   │   ├── EventManager.cs    # 事件管理器(模板加载)
│   │   │   ├── EventTreeManager.cs # 事件树管理器(配置加载)
│   │   │   ├── EventTreeRunner.cs  # 事件树运行器(分支叙事)
│   │   │   ├── EventTreeDialogRunner.cs # 事件树对话框运行器
│   │   │   ├── Effect/     # EffectSystem (效果系统：伤害计算、状态效果)
│   │   │   └── Editor/xNode_Legacy/ # xNode事件树编辑器 (EventTreeEditorWindow, EventTreeGraph, 节点类型)
│   │   ├── Roguelike/      # MapGenerator
│   │   ├── UI/             # UI系统
│   │   │   ├── Dialog/     # UISelectionDialog
│   │   │   ├── Map/        # UIMapPath
│   │   │   ├── Editor/     # Unity编辑器扩展
│   │   │   ├── DataBinding/ # 数据绑定系统 (BaseDataSource, EventDrivenProgressBar, 等)
│   │   │   ├── UITeam.cs   # 队伍UI
│   │   │   ├── UIInventory.cs
│   │   │   ├── UIManager.cs
│   │   │   ├── UIProgressBar.cs
│   │   │   ├── UIText.cs
│   │   │   ├── UILayout.cs
│   │   │   ├── UIListItems.cs
│   │   │   └── Utils/      # UI工具
│   │   ├── Managers/       # 管理器 (已废弃，请使用 Modules/Inventory/)
│   │   ├── Editor/GamePlaySimulator/ # 游戏模拟器 (AAGENTTestRunner, AGameSimulator, ASimulated*)
│   │   ├── GameMain.cs     # 游戏入口
│   │   └── GameTest.cs     # 测试类
│   ├── Scenes/            # Unity场景
│   ├── Settings/          # URP设置
│   ├── Shaders/          # 着色器
│   └── Resources/         # 资源
        └── Data/
            ├── Items/         # 物品配置 (Items.xml)
            ├── Actors/       # 角色配置 (Actors.xml) - 统一Actor设计
            ├── Events/       # 事件配置 (Events.xml)
            ├── EventTrees/   # 事件树配置 (EventTrees.xml)
            ├── Achievements/ # 成就配置 (Achievements.xml)
            ├── Tasks/       # 任务配置 (Tasks.xml)
            └── Prestige/    # 轮回配置 (Prestige.xml)
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
| RawInputManager | Assets/Scripts/Core/Input/ | Windows Raw Input API键盘输入 |
| AudioManager | Assets/Scripts/Core/Audio/ | 音频管理器 |
| 战斗命令系统 | Assets/Scripts/Modules/Combat/Commands/ | ICombatCommand命令队列 |
| 战斗状态机 | Assets/Scripts/Modules/Combat/State/ | CombatStateMachine |
| UI数据绑定 | Assets/Scripts/UI/DataBinding/ | EventDrivenProgressBar |
| UIMapPathV2 | Assets/Scripts/UI/Map/ | 新版地图路径UI |
| UICanvasManager | Assets/Scripts/UI/ | Canvas管理 |
| UIGameDashboard | Assets/Scripts/UI/ | 游戏仪表盘 |
| UISelectionDialogEx | Assets/Scripts/UI/Dialog/ | 扩展选择对话框 |
| PetCompanionPanel | Assets/Scripts/Modules/Pet/ | 宠物面板 |
| 成就系统 | Assets/Scripts/Modules/Achievement/ | AchievementDesign, AchievementManager |
| 任务系统 | Assets/Scripts/Modules/Task/ | TaskDesign, TaskManager |
| 游戏模拟器 | Assets/Scripts/Editor/GamePlaySimulator/ | 游戏模拟器测试工具 |
| EventTreeDialogRunner | Assets/Scripts/Events/ | 事件树对话框运行器 |
| 伤害计算器 | Assets/Scripts/Modules/Combat/DamageCalculator.cs | 战斗伤害计算 |

## CODE MAP

### Core Classes

| Class | Location | Role |
|-------|----------|------|
| GameMain | GameMain.cs | Unity入口，单例 |
| GameConfig | GameMain.cs | 游戏配置（占位） |
| GameLoopManager | Core/GameLoop/ | 主循环Tick协调 |
| SaveManager | Core/SaveSystem/ | 存档管理，手动XML序列化(ToXml/ParseFromXml) |
| EventBus | Core/EventBus/ | 事件发布-订阅 |
| BackgroundInputManager | Core/Input/ | 后台输入管理（UniWindowController） |
| RawInputManager | Core/Input/ | Windows Raw Input API键盘输入 |
| GameDebug | Core/Debug/ | 调试信息管理器（运行时显示） |
| AudioManager | Core/Audio/ | 音频管理器 |
| AudioPresets | Core/Audio/ | 音频预设配置 |
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
| ActorManager | Entities/Actor/ | 角色模板管理器（统一Actor设计） |
| CombatSystem | Modules/Combat/ | 战斗系统 |
| CombatModule | Modules/Combat/ | 战斗模块 |
| CombatEventEx | Modules/Combat/CombatSystem.cs | 战斗事件扩展（virtual/override多态） |
| ICombatCommand | Modules/Combat/Commands/ | 战斗命令接口 |
| AttackCommand | Modules/Combat/Commands/ | 攻击命令 |
| DefendCommand | Modules/Combat/Commands/ | 防御命令 |
| HealCommand | Modules/Combat/Commands/ | 治疗命令 |
| UseSkillCommand | Modules/Combat/Commands/ | 使用技能命令 |
| CombatCommandQueue | Modules/Combat/Commands/ | 命令队列 |
| CombatContext | Modules/Combat/State/ | 战斗上下文 |
| CombatPhase | Modules/Combat/State/ | 战斗阶段枚举 |
| CombatStateMachine | Modules/Combat/State/ | 战斗状态机 |
| CombatEffects | Modules/Combat/ | 战斗特效 |
| CombatAnimationDispatcher | Modules/Combat/ | 战斗动画调度 |
| DeathAnimationHandler | Modules/Combat/ | 死亡动画处理 |
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
| ActivityMonitorModule | Modules/Activity/ | 活跃度监测模块 |
| PetCompanionModule | Modules/Pet/ | 随队宠物模块 |
| PetCompanionPanel | Modules/Pet/ | 宠物面板 |
| AchievementDesign | Modules/Achievement/ | 成就核心逻辑（单例） |
| AchievementManager | Modules/Achievement/ | 成就管理器（静态API） |
| TaskDesign | Modules/Task/ | 任务核心逻辑（单例） |
| TaskManager | Modules/Task/ | 任务管理器（静态API） |
| PendingEventManager | Modules/Idle/ | 积压事件管理器 |
| BatchProcessor | Modules/Idle/ | 批量处理系统 |
| DamageCalculator | Modules/Combat/ | 战斗伤害计算器 |
| CombatStateMachineIntegration | Modules/Combat/State/ | 状态机集成 |
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
| EventTreeDialogRunner | Events/ | 事件树对话框运行器 |
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
| UISelectionDialogEx | UI/Dialog/ | 扩展选择对话框 |
| DialogAnimationComponents | UI/Dialog/ | 对话框动画组件 |
| UIMapPath | UI/Map/ | 地图路径UI |
| UIMapPathV2 | UI/Map/ | 新版地图路径UI |
| UICanvasManager | UI/ | Canvas管理 |
| UIGameDashboard | UI/ | 游戏仪表盘 |
| EventDrivenProgressBar | UI/DataBinding/ | 事件驱动进度条 |
| BaseDataSource | UI/DataBinding/ | 数据源基类 |
| IDataSource | UI/DataBinding/ | 数据源接口 |
| ProgressDataSource | UI/DataBinding/ | 进度数据源 |
| IProgressBarOwner | UI/DataBinding/ | 进度条所有者接口 |
| ProgressBarConfig | UI/DataBinding/ | 进度条配置 |
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
| CardAnimationController | UI/ | 卡牌动画控制器 |
| UIAchievementPanel | UI/ | 成就面板 |

## 代码规范

- **命名空间**: `Game1` (根)
- **命名**: PascalCase (MonoBehaviour方法: UpdateScore)
- **私有字段**: `_camelCase` 或 `m_CamelCase` (来自.editorconfig)
- **SerializeField**: private字段需序列化时显式标记
- **MonoBehaviour**: 避免空脚本
- **Input**: 使用 `UnityEngine.InputSystem` (Keyboard.current)
- **Editorconfig**: `E:\UnityProgram\Game1\.editorconfig` (核心规则：space缩进4, utf-8, CRLF)

## ANTI-PATTERNS (THIS PROJECT)

- **禁止**: FindObjectOfType in Update (Awake/Start缓存)
- **禁止**: GetComponent in Update (缓存引用)
- **禁止**: public字段无[SerializeField]暴露实现
- **禁止**: 空MonoBehaviour
- **避免**: 同步加载大资源 (用Addressables/async)
- **避免**: UnityEngine.Input (用InputSystem)

### 非标准组织模式

| 模式 | 描述 | 位置 |
|------|------|------|
| **废弃目录残留** | Combat/, Inventory/, Managers/ 标注废弃但目录仍存在 | Assets/Scripts/根目录 |
| **测试代码混杂** | GamePlay目录包含AAGENTTestRunner和ASimulated*测试类 | Assets/Scripts/GamePlay/ |
| **自实现数据绑定** | UI/DataBinding目录实现自定义数据绑定系统 | Assets/Scripts/UI/DataBinding/ |

### TODO分布 (26个)

高发区优先修复：
- `Roguelike/MapGenerator.cs` - 4处
- `UI/UICardPanel.cs` - 4处
- `Modules/Skill/SkillDesign.cs` - 3处
- `Modules/Travel/TravelManager.cs` - 3处

### 测试规范

测试目录：`Assets/Tests/EditMode/` (NUnit框架)
测试类命名：`[ClassName]Tests` (如 `InventoryDesignTests`)
测试方法：`Method_Scenario_ExpectedResult` (如 `AddItem_Success_SingleItem`)

运行命令：
```bash
unity -batchmode -projectPath . -runTests -testMode EditMode
```

### CI/CD

3个GitHub Workflows：
- `build.yml`: game-ci官方action，多平台矩阵(Windows64/OSX/Linux64/WebGL)
- `ci.yml`: 自定义Unity容器，PR触发
- `release.yml`: Release发布流水线

必需Secrets: UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD, SLACK_WEBHOOK_URL

## UNIQUE STYLES

- **透明悬浮窗**: UniWindowController + D3D11 + FlipModel禁用
- **前台悬浮**: 无边框、置顶、点击穿透
- **输入系统**: New Input System (com.unity.inputsystem) + RawInputManager (Windows Raw Input API后端)
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
- **存档序列化**: 所有存档数据类(SaveDataBase, PlayerSaveData, CombatSaveData等)使用手动ToXml/ParseFromXml方法替代XmlSerializer，实现逐一转换模式
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

## 积压事件系统（新）

积压事件是游戏离线/后台时累积的事件，等待玩家返回后决策：

### 事件稀有度
- **普通(Normal)**：高频率，低价值，可批量处理
- **稀有(Rare)**：中等频率，中等价值，需要关注
- **传奇(Legendary)**：低频率，高价值，独特体验

### 批量处理系统
- 普通事件提供批量处理选项（自动战斗、自动收获）
- 批量处理提供简报：压缩文本、抽卡式结果展示
- 几乎自动的战斗流程，只需简单选择

### 时间线记录
- 记录每个积压事件的时间点
- 玩家可查看离线期间的事件编年史
- 支持按时间顺序或稀有度排序

## 活跃度系统（新）

### 输入活跃度监测
- 从"每次输入监测"改为"有效活跃度监测"
- 有效活跃度 = 操作混合度 × 间隔差系数
- 操作混合度：多种操作类型（点击、移动等）的组合
- 间隔差：操作时间间隔的变化程度

### 活跃/非活跃优势
- **活跃玩家**：更多脚程、更多互动事件
- **非活跃玩家**：自动战斗效率加成、离线收益提升

## 随队宠物系统（新）

参考桌宠设计，作为游戏封面展示：

### 功能定位
- 反馈玩家状态（血量、心情、活跃度）
- 实时展示游戏进度
- 增添情感陪伴元素

### 视觉设计
- 形象：纯色似狗宠物（类似蜡笔小新的小白）
- 实现方式：2D骨骼动画
- 状态动画：idle、happy、sad、excited等

### 技术实现
- 使用Unity 2D Animation或类似系统
- 骨骼绑定 + 关键帧动画
- 表情系统：眼睛、嘴巴等部件动画
- Team模块采用与Inventory模块相同的设计模式:
  - TeamDesign: 单例非MonoBehaviour，核心逻辑
  - TeamManager: 静态API，委托给TeamDesign
  - TeamMemberData: 成员数据结构
  - UITeam: UI面板，继承BaseUIPanel
- 全局输入系统（NEW）:
  - GlobalKeyboardHook: 使用Windows API SetWindowsHookEx实现全局键盘监听
  - RawInputManager: 使用Windows Raw Input API，消息驱动WM_INPUT，性能优于WH_KEYBOARD_LL钩子
  - InputConverter: 键盘敲击转脚程、鼠标移动转校准、连击加成计算
  - BackgroundInputManager: 集成UniWindowController鼠标 + GlobalKeyboardHook键盘
- 虚实交互输入转换算法:
  - 每10次键盘敲击 = 1秒脚程
  - 每100px鼠标移动 = 0.1秒脚程
  - 连击窗口1秒，每10次连击+0.1，最高1.5
  - 精准校准：静止>2秒后移动 = 2x加成
- InputConverter使用UnityEngine.InputSystem.Mouse.current.position.ReadValue()获取鼠标位置，兼容New Input System

## 统一ID规则（ID Rule）

所有资源配置的ID均遵循以下规则：

### 命名规范
- 采用大Pascal命名法
- 路径规则：`{Package}.{Category}.{Name}.{Extend}`

### ID格式示例
| 资源类型 | ID格式 | 示例 |
|---------|--------|------|
| 物品 | `Core.Item.{Name}` | `Core.Item.GoldCoin`, `Core.Item.ShortBlade` |
| 角色 | `Core.Actor.{Name}` | `Core.Actor.Player`, `Core.Actor.Bandit` |
| 事件 | `Core.Event.{Name}` | `Core.Event.EncounterBandit` |
| 事件树 | `Core.EventTree.{Name}` | `Core.EventTree.MerchantEncounter` |

### 扩展名（Extend）
| 扩展名 | 用途 |
|--------|------|
| `NameText` | 名称文本ID |
| `DescriptionText` | 描述文本ID |
| `Image` | 图片资源 |
| `SoundEffect` | 音效资源 |

## 统一Actor设计

Actor是统一的角色概念，不再区分敌人/玩家/NPC，通过Affiliation（阵营归属）来区分：

### Affiliation 枚举
| 阵营 | 说明 |
|------|------|
| `Player` | 玩家角色 |
| `Friendly` | 友好阵营 |
| `Neutral` | 中立阵营（商人、村民等） |
| `Hostile` | 敌对阵营（匪徒、野兽等） |
| `Authority` | 权威阵营（守卫、官员等） |

### Actor特性
- `isBoss`: 标记是否为Boss
- `goldReward`/`expReward`: 击杀奖励（Hostile阵营可能有）
- `interactionType`: 交互类型（Neutral/Authority阵营可能有）

## GIT WORKFLOW

```bash
git add -A
git commit -m "描述"
git push origin main
```