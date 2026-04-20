# 必须遵守

1. 对话和思考保持使用中文！
2. 维持同样的代码风格！
   - C#: Assets/Scripts/GameMain.cs

# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-17
**Project:** Game1 - Unity 6 游戏开发项目
**Commit:** N/A (git repo not initialized)
**Branch:** N/A

## OVERVIEW
挂机放置类游戏，包含旅行、事件、Roguelike元素。使用Unity 6.1.5 + URP渲染管线，C# 9.0开发，目标平台Windows64。

## STRUCTURE
```
Game1/
├── Assets/
│   ├── Scripts/           # 游戏代码 (当前为空)
│   ├── Scenes/            # Unity场景
│   ├── Shaders/           # 着色器 (待创建)
│   ├── TutorialInfo/      # 教程资源(默认)
│   └── Settings/         # 设置资产
├── Docs/                 # 游戏设计文档
├── Packages/             # Unity包
├── ProjectSettings/      # 项目配置
├── Library/              # Unity生成
├── Temp/                 # 临时文件
├── Logs/                 # 日志
└── Game1.sln             # VS解决方案
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| 游戏代码 | Assets/Scripts/ | 尚未创建 |
| 设计文档 | Docs/Main.md | 游戏方案 |
| 开发计划 | Docs/Plan1.md | 当前计划:星球生成 |
| 场景 | Assets/Scenes/ | SampleScene.unity |
| 着色器 | Assets/Shaders/ | 待创建 |
| 构建目标 | StandaloneWindows64 | PC独立游戏 |

## CODE ORGANIZATION (规划)
根据Docs/Main.md，项目应采用以下结构:
```
Assets/Scripts/
├── Core/                 # 核心系统
│   ├── GameLoop/         # 游戏循环
│   ├── SaveSystem/       # 存档
│   └── EventSystem/      # 事件系统
├── Entities/             # 实体
│   ├── Player/           # 玩家
│   ├── NPC/              # NPC
│   └── World/            # 世界
├── Modules/              # 模块
│   ├── Idle/             # 挂机收益
│   ├── Travel/           # 旅行
│   └── Skill/            # 技能
├── UI/                   # 界面
├── Graphics/             # 图形
│   ├── Planet/           # 星球生成
│   └── Terrain/          # 地形
└── Utils/                # 工具
```

## CONVENTIONS (Unity标准)
- **命名**: PascalCase (MonoBehaviour: MovePlayer, UpdateScore)
- **私有字段**: _camelCase 或 m_CamelCase
- **SerializeField**: private字段需要序列化时显式标记
- **文件夹**: 与代码组织结构对应
- **场景**: 一个主要功能一个场景

## ANTI-PATTERNS (THIS PROJECT)
- **禁止**: FindObjectOfType in Update (缓存引用)
- **禁止**: GetComponent in Update ( Awake/Start缓存)
- **禁止**: public字段无[SerializeField]暴露实现细节
- **禁止**: 空MonoBehaviour (只有using和class声明)
- **避免**: 同步加载大资源 (用Addressables/async)

## UNIQUE STYLES
- **URP双渲染器**: Mobile + PC (质量设置)
- **球形地形**: 六边形球形网格 + 噪声偏移
- **前台悬浮**: 类似输入法窗口的UI架构
- **输入系统**: 使用New Input System (com.unity.inputsystem)

## COMMANDS
```bash
# Unity Editor
unity -openProject Game1

# Headless Build (batchmode)
unity -batchmode -buildTarget StandaloneWindows64 -quit

# Play Mode测试
unity -batchmode -runTests -testPlatform playmode
```

## TECHNICAL STACK
| 组件 | 版本 |
|------|------|
| Unity | 6000.1.5f1 |
| URP | 17.1.0 |
| C# | 9.0 |
| .NET | 4.7.1 |
| Input System | 1.14.0 |
| Visual Scripting | 1.9.7 |
| Test Framework | 1.5.1 |

## NOTES
- Unity 6新特性: 源码生成器已启用, Source Generators active
- 默认程序集: Assembly-CSharp (运行时) + Assembly-CSharp-Editor (编辑器)
- 尚无.asmdef定义: 如代码量增长, 考虑拆分程序集
- Git未初始化: 建议尽早初始化以跟踪改动
