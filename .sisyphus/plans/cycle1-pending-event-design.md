# Cycle 1: 创建 PendingEventDesign 单例

## 目标
创建缺失的 `PendingEventDesign.cs` 单例，修复 `PendingEventManager` 引用的编译错误，遵循项目已有的 Manager-Design 模式。

## 背景
- `PendingEventManager.cs` 已存在（静态 API 层），但引用的 `PendingEventDesign.instance` 类缺失
- 其余模块（InventoryDesign/SkillDesign/CardDesign/TeamDesign）均遵循 Design 单例 + Manager 静态 API 模式
- `BonusMultiplierModule` 和 `BatchProcessor` 实际内联在 `IdleRewardModule.cs` 中，无需独立文件

## 子任务

### Wave 1（无依赖，可并行）
1. **创建 PendingEventDesign.cs** - 单例核心逻辑类
   - 文件: `Assets/Scripts/Modules/PendingEvent/PendingEventDesign.cs`
   - 遵循现有单例模式 (`instance ??= new()`)
   - 实现 `GeneratePendingEvents()`, `ProcessEvent()`, `GetPendingEvents()`, `Clear()` 等存根方法
   - 接受标准：编译通过，PendingEventManager 不再报错

2. **创建支持数据类型** - 数据模型定义
   - `PendingEventRarity.cs` (Normal/Rare/Legendary 枚举)
   - `PendingEventData.cs` (事件数据模型)
   - `PendingEventSaveData.cs` (存档数据)
   - `PendingEventBrief.cs` (UI简报)
   - `RarityDistribution.cs` (稀有度分布统计)
   - 接受标准：所有类型编译通过

### Wave 2（依赖 Wave 1）
3. **编写单元测试** - NUnit 测试
   - 测试单例行为
   - 测试空状态查询
   - 测试 Clear 功能
   - 接受标准：所有测试通过

### 最终验证
4. **编译+测试验证** - 全量验证
   - 运行 EditMode 测试套件
   - 确认无编译错误
