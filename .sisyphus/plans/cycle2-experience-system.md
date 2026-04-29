# Cycle 2: 实现PlayerActor经验值系统

## 目标
为PlayerActor添加经验值系统，并连接EffectExecutor.ApplyEXP，实现事件/战斗奖励经验、升级机制。

## 背景
- ActorTemplate有 `expReward` 字段（已设计）
- CombatResult有 `expReward` 字段（已设计）
- PlayerActor只有 `level` 字段，无 `exp` 和升级方法
- EffectExecutor.ApplyEXP 是空实现（TODO）
- 游戏无任何经验值积累或升级机制

## 修改文件

### 1. PlayerActor.cs
- Stats 中添加 `int exp = 0` 和 `int expToNextLevel = 100`
- 添加 `const int EXP_BASE = 100` 和 `const float EXP_SCALE = 1.5f`
- 添加 `AddExp(int amount)` → 累加exp，检查升级
- 添加 `LevelUp()` → level++，exp清零，重新计算expToNextLevel，可选的stats增长

### 2. EffectExecutor.cs
- ApplyEXP: 调用 `player.AddExp((int)Math.Round(value))`
- 返回描述文本

### 3. 影响范围
- 存档系统（PlayerSaveData/PlayerSaveFile需添加exp字段）
- 升级效果（stats自动增长）

## 子任务
1. **PlayerActor: 添加exp/expToNextLevel + AddExp方法**
2. **存档系统: PlayerSaveData/PlayerSaveFile添加exp字段**
3. **EffectExecutor: 实现ApplyEXP**
4. **验证 + 测试**
