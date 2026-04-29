# 周期2：成就/任务系统 EventBus 集成

## 背景

周期1完成了 Effect 系统（EffectExecutor 所有方法补全、EventTreeDialogRunner 去重、32个测试用例）。

根据周期1阶段2的研究，Achievement 和 Task 系统已创建数据结构和基本逻辑，但**缺少与 EventBus 的连接**——这意味着成就和任务目前只存在于数据层面，无法响应游戏事件（获得金币、击败敌人等）。

## 目标

连接 AchievementDesign/TaskDesign 到 EventBus，使成就/任务能真实追踪游戏进度。

## 子任务分解

### Wave 1: 分析已有代码
- [ ] 读取 AchievementDesign.cs 的 UpdateConditionProgress() 方法
- [ ] 读取 TaskDesign.cs 的 ReportProgress() 方法
- [ ] 读取 EventBus.cs 的事件类型
- [ ] 确定需要订阅哪些 EventBus 事件

### Wave 2: EventBus 事件类型定义
- [ ] 定义/确认成就相关事件类型（GoldEarned, EnemiesDefeated, DistanceTraveled, ItemsCollected, etc.）
- [ ] 确保对应模块发布这些事件（IdleRewardModule发布金币事件、CombatSystem发布战斗事件等）

### Wave 3: Achievement EventBus 订阅
- [ ] 在 AchievementDesign 中订阅 EventBus 事件
- [ ] 事件触发时调用 UpdateConditionProgress()
- [ ] 实现成就解锁通知

### Wave 4: Task EventBus 订阅
- [ ] 在 TaskDesign 中订阅 EventBus 事件
- [ ] 每日/每周任务进度更新

### Wave 5: 测试
- [ ] 创建 AchievementDesign 单元测试
- [ ] 验证事件触发 → 成就进度更新 → 解锁流程
- [ ] 回归测试（确保 Effect 系统不受影响）

### Wave 6: 四维审查
- [ ] 正确性：成就/任务进度正确更新
- [ ] 工程质量：EventBus 订阅/取消订阅正确管理
- [ ] 性能：无内存泄漏（EventBus 订阅者正确释放）
- [ ] 健壮性：边界处理
