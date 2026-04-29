# 周期1：Effect系统完成 + 测试全覆盖（v2）

## 背景

根据10个探索/搜索agent + Momus审查分析，项目最大的架构缺口是 **统一Effect系统未完成**：
- `EffectExecutor.cs` 有TODO方法（HP/Item/Module Add）
- `EventTreeDialogRunner.cs` 自己实现了效果执行逻辑（与EffectExecutor重复）
- 0个Effect系统单元测试

**Momus审查发现的关键约束**：
- PlayerActor无EXP系统 → ApplyEXP移出此周期
- PlayerActor无flags系统 → ApplyFlag移出此周期  
- ModuleCollection.AddModule需IModule实例而非stringID → ApplyModule仅保留Remove

**调整后范围**：只对接已有API的EffectType，移除需要新建系统的工作。

---

## 子任务分解

### Wave 1: EffectExecutor 方法补全（对接已有API）
- [ ] **ApplyGold()** — 使用PlayerActor.AddGold() (API已有)
  - 支持 Add/Subtract/Multiply/Divide/Set/Percent 操作符
  - 支持随机范围
  - 支持属性缩放
- [ ] **ApplyHP() / ApplyDamage() / ApplyHeal()** — 使用PlayerActor.stats (currentHp/maxHp)
  - HP: 批量修改（Add/Subtract操作符 → Heal/Damage）
  - Damage: 连接PlayerActor.TakeDamage()
  - Heal: 连接PlayerActor.Heal()（上限maxHp）
  - 属性缩放: 如 ScalingStat="attack" 时增加伤害
- [ ] **ApplyItem()** — 使用InventoryDesign.AddItem()
  - 解析 TargetId 作为物品ID，Quantity 作为数量
  - 返回操作结果（成功/失败）
- [ ] **ApplyModule()** — 仅保留Remove（移除指定模块）
  - Add 逻辑移出此周期（需要ModuleFactory）
- [ ] 补充 `ToEventResult()` 完整转换

### Wave 2: 消除EventTreeDialogRunner重复
- [ ] EventTreeDialogRunner.SelectChoice() 效果逻辑 → 改用EffectExecutor.Execute()
- [ ] 保留描述生成逻辑（ExecuteSingleUnifiedEffect 中的 switch 描述部分）
- [ ] 移除重复的执行代码
- [ ] 验证gold/hp/item效果执行正确性

### Wave 3: EffectExecutor测试
- **文件**: Assets/Tests/EditMode/Effect/EffectExecutorTests.cs
- **测试用例**:
  1. Gold_Add_IncreaseGold正确
  2. Gold_Subtract_DecreaseGold正确  
  3. Gold_Set_OverrideGold正确
  4. Gold_Multiply_MultiplyGold正确
  5. Gold_Divide_DivideGold正确
  6. Gold_Percent_ApplyPercent正确
  7. Gold_RandomRange_ValueInRange正确
  8. HP_Heal_IncreaseHP不超过maxHp
  9. HP_Damage_DecreaseHP
  10. HP_ScalingStat_AffectsValue
  11. Item_Add_ValidItem_CallsInventory
  12. Item_Add_InvalidId_ReturnsFailure
  13. Module_Remove_RemovesModule
  14. UnlockEffect_LogsOnly (验证不抛异常)
  15. EmptyEffects_NoChanges
  16. NegativeValues_HandledCorrectly
  17. ToEventResult_ConvertsCorrectly

### Wave 4: EffectParser测试
- **文件**: Assets/Tests/EditMode/Effect/EffectParserTests.cs
- **测试用例**:
  1. StructuredXml_ParsesCorrectly
  2. LegacyString_ParsesCorrectly
  3. EventTreeChoiceEffects_ParsesCorrectly
  4. InvalidXml_ReturnsEmpty
  5. EmptyInput_ReturnsEmpty

### Wave 5: 集成验证与四维审查
- [ ] 运行所有现有测试确认回归通过
- [ ] LSP诊断0错误
- [ ] 四维审查：
  - **正确性**: 每个EffectType的数值变化符合预期
  - **工程质量**: 无硬编码Magic Number、null检查完备、命名规范
  - **性能**: EffectExecutor无GC分配、无Update轮询
  - **健壮性**: 边界值（0值、负值、null、空列表）不抛异常

---

## 验收标准

1. ✅ EffectExecutor：Gold/HP/Item正常执行，Module支持Remove
2. ✅ EventTreeDialogRunner改用EffectExecutor，无重复代码
3. ✅ 新增 ≥22个Effect系统单元测试
4. ✅ 所有测试通过
5. ✅ LSP诊断0错误
6. ✅ 四维审查零改进项
