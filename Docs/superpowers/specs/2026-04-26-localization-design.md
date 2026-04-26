# Game1 本地化方案设计

**版本**: 1.0
**日期**: 2026-04-26
**状态**: 已设计，待实施

---

## 1. 概述

### 1.1 目标
为Game1项目实现完整的本地化方案，支持中文/英文双语，使用Unity Localization Package + TextMeshPro。

### 1.2 约束
- **不修改**任何业务逻辑
- **不删除**任何已有功能
- **不修改**UI显示逻辑

### 1.3 验收标准
- ✅ 使用Unity Localization Package
- ✅ 所有静态文本使用StringTable而非硬编码
- ✅ 支持中文/英文两种语言
- ✅ 语言首选项写入存档

---

## 2. 现有文本分析

### 2.1 文本来源统计

| 来源 | 类型 | 本地化状态 | 示例 |
|------|------|-----------|------|
| Items.xml | 配置ID | ✅ 已使用TextId引用 | `<nameTextId>Core.Item.GoldCoin.NameText</nameTextId>` |
| Events.xml | 硬编码文本 | ❌ 需迁移 | `<name>遭遇战斗</name>` |
| EventTrees.xml | 硬编码文本 | ❌ 需迁移 | `<title>商队领队</title>` |
| UISelectionDialog.cs | 代码硬编码 | ❌ 需迁移 | `"确认"`, `"取消"` |
| IdleRewardModule.cs | Debug日志 | ❌ 忽略 | Debug.Log文本 |

### 2.2 需要本地化的文本范围

| 类别 | 数量(估算) | 示例 |
|------|-----------|------|
| UI文本 | ~50处 | "确认"、"取消"、"设置" |
| 事件文本 | ~30处 | Events.xml中的name/description |
| 事件树文本 | ~100处 | EventTrees.xml中的title/text |

---

## 3. 架构设计

### 3.1 组件关系图

```
┌─────────────────────────────────────────────────────────────┐
│                    LocalizationManager                       │
│  (核心单例，管理语言切换、文本查询、事件通知)                  │
├─────────────────────────────────────────────────────────────┤
│  + instance: LocalizationManager                            │
│  + currentLanguage: SystemLanguage                          │
│  + availableLanguages: List<SystemLanguage>                │
│  + Initialize(): void                                       │
│  + SetLanguage(lang): void                                  │
│  + GetString(tableId, key): string                         │
│  + OnLanguageChanged: Action<SystemLanguage>               │
└─────────────────────────────────────────────────────────────┘
           │                                    │
           ▼                                    ▼
┌─────────────────────┐          ┌─────────────────────────────────────┐
│    GameSaveData      │          │           UIText                      │
│  + language: lang   │          │  + localizationKey: string           │
│  (存档存储语言偏好)   │          │  + Reload(): 调用LocalizationManager │
└─────────────────────┘          └─────────────────────────────────────┘
           │                                    │
           ▼                                    ▼
┌─────────────────────┐          ┌─────────────────────────────────────┐
│    SaveManager      │          │      StringTable (SO资产)             │
│  Load/Save时会自动   │          │  + UI Table (UI文本)                 │
│  读取/写入language   │          │  + EventTable (事件文本)             │
└─────────────────────┘          │  + GameTable (游戏文本)               │
                                  └─────────────────────────────────────┘
```

### 3.2 核心类设计

#### LocalizationManager

| 属性/方法 | 类型 | 说明 |
|-----------|------|------|
| `instance` | static | 单例访问点 |
| `currentLanguage` | SystemLanguage | 当前语言 |
| `availableLanguages` | List<SystemLanguage> | 可用语言列表 |
| `Initialize()` | void | 初始化（从存档加载语言设置） |
| `SetLanguage(lang)` | void | 切换语言并通知所有监听者 |
| `GetString(tableId, key)` | string | 获取本地化文本 |
| `onLanguageChanged` | Action<SystemLanguage> | 语言变更事件 |

---

## 4. StringTable 配置方案

### 4.1 表结构

| TableId | 用途 | 语言 |
|---------|------|------|
| `UI` | 所有UI相关文本 | en-US, zh-CN |
| `Events` | 事件名称和描述 | en-US, zh-CN |
| `Game` | 通用游戏文本 | en-US, zh-CN |

### 4.2 Key命名规范

遵循项目现有ID规则：

| 类型 | Key格式 | 示例 |
|------|--------|------|
| UI按钮 | `UI.Button.{Name}` | `UI.Button.Confirm`, `UI.Button.Cancel` |
| UI标签 | `UI.Label.{Name}` | `UI.Label.Settings`, `UI.Label.Language` |
| 事件名称 | `Event.Name.{Id}` | `Event.Name.EncounterBandit` |
| 事件描述 | `Event.Desc.{Id}` | `Event.Desc.EncounterBandit` |

### 4.3 Unity编辑器配置

1. **安装Package**: `com.unity.localization`
2. **创建Locale**: en-US (English), zh-CN (Simplified Chinese)
3. **创建StringTableCollection**: UI, Events, Game
4. **配置Inspector Settings**:
   - Behavior > Change Unity Player Settings: ❌ 不勾选
   - 手动通过LocalizationManager控制语言切换

---

## 5. 存档集成设计

### 5.1 GameSaveData扩展

```csharp
[Serializable]
public class GameSaveData : SaveDataBase
{
    // ... 现有字段 ...

    // 新增：语言偏好
    public SystemLanguage language = SystemLanguage.English;
}
```

### 5.2 语言加载流程

```
1. SaveManager.Load()
   └─► GameSaveData.language 被加载
            │
            ▼
2. LocalizationManager.Initialize()
   └─► 读取 SaveManager.currentSave.language
            │
            ▼
3. LocalizationSettings.SetLocale(currentLanguage)
```

### 5.3 语言切换流程

```
1. 玩家在设置界面选择语言
   └─► LocalizationManager.SetLanguage(newLang)
            │
            ▼
2. LocalizationManager.SetLanguage()
   ├─► this.currentLanguage = newLang
   ├─► SaveManager.currentSave.language = newLang
   ├─► SaveManager.MarkDirty()
   └─► onLanguageChanged?.Invoke(newLang)
            │
            ▼
3. UI组件监听 onLanguageChanged
   └─► UIText.Reload() 刷新文本
```

---

## 6. UIText增强设计

### 6.1 新增字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `localizationKey` | string | 本地化表Key，如"UI.Button.Confirm" |
| `tableId` | string | 表ID，默认为"UI" |

### 6.2 Reload() 修改

```csharp
public void Reload()
{
    if (!string.IsNullOrEmpty(localizationKey))
    {
        var tableId = string.IsNullOrEmpty(this.tableId) ? "UI" : this.tableId;
        this.text = LocalizationManager.instance.GetString(tableId, localizationKey);
    }
    else if (this.id.Length > 0)
    {
        // 向后兼容：使用id作为key
        this.text = LocalizationManager.instance.GetString("UI", this.id);
    }
}
```

---

## 7. 静态文本迁移计划

### 7.1 优先级

| 优先级 | 文本来源 | 数量 | 说明 |
|--------|---------|------|------|
| P0 | UISelectionDialog.cs | ~10 | 核心UI，"确认"、"取消"等 |
| P1 | Events.xml | ~30 | 事件名称和描述 |
| P2 | EventTrees.xml | ~100 | 事件树节点文本 |

### 7.2 迁移步骤

1. 创建StringTable资产和所有key
2. 修改UIText支持localizationKey
3. 在Inspector中设置每个UIText的localizationKey
4. 迁移UISelectionDialog中的硬编码文本
5. 迁移Events.xml（修改解析逻辑，支持从StringTable获取文本）
6. 迁移EventTrees.xml（修改解析逻辑，支持从StringTable获取文本）

---

## 8. 实施阶段

### Phase 1: 基础框架
- [ ] 安装 `com.unity.localization` Package
- [ ] 创建 Locale (en-US, zh-CN)
- [ ] 创建 LocalizationManager 单例
- [ ] 扩展 GameSaveData 添加 language 字段

### Phase 2: UIText对接
- [ ] 修改 UIText.cs 添加 localizationKey 和 tableId 字段
- [ ] 实现 Reload() 对接 LocalizationManager
- [ ] 创建监听 onLanguageChanged 的机制

### Phase 3: UI文本迁移
- [ ] 创建 UI StringTable
- [ ] 迁移 UISelectionDialog 硬编码文本
- [ ] 创建 Settings 面板语言切换UI

### Phase 4: 事件文本迁移
- [ ] 创建 Events StringTable
- [ ] 修改 Events.xml 解析逻辑
- [ ] 创建 EventTree StringTable
- [ ] 修改 EventTrees.xml 解析逻辑

---

## 9. 风险与注意事项

### 9.1 风险
| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| StringTable未加载完成 | 文本显示空 | 在LocalizationManager中确保初始化顺序 |
| Font缺失 | 中文显示方块 | 确保使用支持中文的Font资产 |

### 9.2 注意事项
- 所有静态文本必须使用StringTable，禁止硬编码
- UIText的id字段保留用于向后兼容
- 语言切换后需刷新所有UIText组件

---

## 10. 相关文件

### 新增文件
| 文件 | 路径 |
|------|------|
| LocalizationManager.cs | Assets/Scripts/Core/Localization/LocalizationManager.cs |

### 修改文件
| 文件 | 修改内容 |
|------|---------|
| GameSaveData.cs | 添加 language 字段 |
| SaveManager.cs | 无修改（语言字段自动序列化） |
| UIText.cs | 添加 localizationKey, tableId, Reload()对接 |

### 资源文件
| 文件 | 说明 |
|------|------|
| Assets/Localization/UI/UI_en-US.asset | UI英文表 |
| Assets/Localization/UI/UI_zh-CN.asset | UI中文表 |
| Assets/Localization/Events/Events_en-US.asset | 事件英文表 |
| Assets/Localization/Events/Events_zh-CN.asset | 事件中文表 |
