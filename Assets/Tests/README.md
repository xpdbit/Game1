# Unity 自动化测试框架

## 目录结构

```
Assets/Tests/
├── EditMode/                           # EditMode测试（纯C#逻辑，无需Unity运行时）
│   ├── InventoryDesignTests.cs         # 背包系统单元测试
│   ├── TeamDesignTests.cs              # 队伍系统单元测试
│   └── CombatModuleTests.cs            # 战斗模块单元测试
├── PlayMode/                           # PlayMode测试（需要Unity运行时）
├── testSettings.asset                  # 测试配置
└── README.md                           # 本文件
```

## 运行测试

### Unity编辑器内运行

1. 打开Unity编辑器
2. Window > General > Test Runner
3. 选择EditMode或PlayMode标签页
4. 点击"Run All"运行所有测试

### 命令行运行（Headless模式）

#### 运行所有EditMode测试
```bash
unity -batchmode -projectPath "E:\UnityProgram\Game1" -runTests -testMode EditMode
```

#### 运行所有PlayMode测试
```bash
unity -batchmode -projectPath "E:\UnityProgram\Game1" -runTests -testMode PlayMode
```

#### 生成XML报告
```bash
unity -batchmode -projectPath "E:\UnityProgram\Game1" -runTests -testMode EditMode -testResults "Assets\Tests\Results\editmode-results.xml"
```

## XML报告生成

Unity Test Framework支持生成NUnit兼容的XML报告格式。

### 报告输出路径
- 默认路径: `Assets\Tests\Results\`
- 建议使用`.xml`扩展名

### Jenkins/CI集成

Jenkins可以使用NUnit插件解析生成的XML报告：

```bash
# Windows批处理脚本示例
@echo off
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2023.1.0f1\Editor\Unity.exe"
set PROJECT_PATH="E:\UnityProgram\Game1"
set REPORT_PATH="%PROJECT_PATH%\Assets\Tests\Results\test-results.xml"

%UNITY_PATH% -batchmode -projectPath %PROJECT_PATH% -runTests -testMode EditMode -testResults %REPORT_PATH% -logFile

if %ERRORLEVEL% EQU 0 (
    echo Tests passed
) else (
    echo Tests failed
    exit /b %ERRORLEVEL%
)
```

## 测试覆盖范围

### InventoryDesignTests
- [x] AddItem - 添加物品（单件、堆叠、边界情况）
- [x] RemoveItem - 移除物品（部分、全部）
- [x] Clear - 清空背包
- [x] GetItem - 获取物品实例
- [x] GetItemsByTemplateId - 按模板ID查询
- [x] GetTotalAmountByTemplateId - 按模板ID统计总量
- [x] CanAddItem - 检查能否添加物品
- [x] CanAddSlot - 检查是否有空槽位
- [x] CanAddWeight - 检查重量限制
- [x] RemainingSlotCount - 剩余槽位数
- [x] RemainingWeight - 剩余重量
- [x] AddItems/RemoveItems - 批量操作
- [x] Export/Import - 序列化

### TeamDesignTests
- [x] AddMember - 添加成员（单人、多人、边界情况）
- [x] RemoveMember - 移除成员
- [x] UpdateMember - 更新成员数据
- [x] Clear - 清空队伍
- [x] GetMember - 获取成员
- [x] GetAllMembers - 获取所有成员
- [x] GetTotalCombatPower - 计算总战斗力
- [x] GetAverageLevel - 计算平均等级
- [x] CanAddMember - 检查能否添加成员
- [x] RemainingSlots - 剩余位置
- [x] Export/Import - 序列化
- [x] TeamMemberData - 数据结构方法测试

### CombatModuleTests
- [x] Module Interface - 模块接口实现
- [x] Bonus Getters - 加成获取
- [x] SetBonusMultipliers - 设置加成倍率
- [x] Statistics - 战斗统计
- [x] CanVictory - 胜利判断

### CombatSystemTests
- [x] CanVictory - 战斗力对比
- [x] CombatResult - 战斗结果数据结构
- [x] MultiEnemyCombatResult - 多敌战斗结果
- [x] EnemyCombatantData - 敌人生存数据

## 验收标准

- [x] EditMode测试覆盖核心工具类
- [ ] PlayMode测试覆盖核心模块API（待实现）
- [ ] 测试通过率100%（需CI验证）

## 注意事项

1. **EditMode vs PlayMode**: EditMode测试不需要Unity运行，适合纯逻辑测试；PlayMode测试需要完整Unity运行时环境。

2. **单例模式**: InventoryDesign、TeamDesign、CombatSystem使用单例模式。测试通过创建新实例或清理现有实例来隔离。

3. **依赖管理**: ItemManager需要物品模板配置，测试通过反射注册测试模板来隔离依赖。

4. **随机性**: 战斗系统包含随机性（暴击、闪避），测试应设计为验证逻辑而非具体数值。

## 扩展测试

### 添加新测试

1. 在`Assets\Tests\EditMode\`目录下创建新的`[ClassName]Tests.cs`文件
2. 使用 NUnit 框架编写测试
3. 每个测试方法使用 `[Test]` 属性标记
4. 使用 `[SetUp]` 和 `[TearDown]` 管理测试生命周期

### 示例

```csharp
using NUnit.Framework;

namespace Game1.Tests.EditMode
{
    public class NewClassTests
    {
        [SetUp]
        public void SetUp()
        {
            // 初始化测试数据
        }

        [TearDown]
        public void TearDown()
        {
            // 清理测试数据
        }

        [Test]
        public void TestMethod_Scenario_ExpectedResult()
        {
            // Arrange
            // Act
            // Assert
        }
    }
}
```
