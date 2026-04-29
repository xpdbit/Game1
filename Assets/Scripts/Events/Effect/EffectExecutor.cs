#nullable enable
using System;
using System.Collections.Generic;
using Game1;
using UnityEngine;

namespace Game1.Events.Effect
{
    /// <summary>
    /// 效果执行器。
    /// 将 UnifiedEffect 列表应用到 PlayerActor，并返回人类可读的描述文本。
    /// 每个效果类型对应不同的处理逻辑。
    /// </summary>
    public static class EffectExecutor
    {
        /// <summary>
        /// 执行一组效果，应用到玩家。
        /// </summary>
        /// <param name="effects">要执行的效果列表。</param>
        /// <param name="player">玩家实例。</param>
        /// <returns>每条效果的人类可读描述。</returns>
        public static List<string> Execute(List<UnifiedEffect> effects, PlayerActor player)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var descriptions = new List<string>(effects.Count);

            foreach (var effect in effects)
            {
                try
                {
                    var description = ExecuteSingle(effect, player);
                    if (!string.IsNullOrEmpty(description))
                        descriptions.Add(description);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EffectExecutor] Failed to execute effect {effect}: {ex.Message}");
                    descriptions.Add($"执行失败: {effect.Type} ({ex.Message})");
                }
            }

            return descriptions;
        }

        /// <summary>
        /// 执行单个效果。
        /// </summary>
        private static string ExecuteSingle(UnifiedEffect effect, PlayerActor player)
        {
            var finalValue = effect.GetFinalValue();

            switch (effect.Type)
            {
                case EffectType.Gold:
                    return ApplyGold(effect, player, finalValue);

                case EffectType.HP:
                    return ApplyHP(effect, player, finalValue);

                case EffectType.EXP:
                    return ApplyEXP(effect, player, finalValue);

                case EffectType.Item:
                    return ApplyItem(effect, player);

                case EffectType.Flag:
                    return ApplyFlag(effect, player);

                case EffectType.Module:
                    return ApplyModule(effect, player);

                case EffectType.Heal:
                    return ApplyHeal(effect, player, finalValue);

                case EffectType.Damage:
                    return ApplyDamage(effect, player, finalValue);

                case EffectType.Unlock:
                    return ApplyUnlock(effect, player);

                case EffectType.Combat:
                    return "[战斗触发]";

                case EffectType.Buff:
                    return $"[增益] {effect.TargetId ?? "未知"} ({finalValue})";

                default:
                    return $"[未知效果: {effect.Type}]";
            }
        }

        /// <summary>
        /// 将效果列表转换为兼容旧系统的 EventResult。
        /// </summary>
        public static EventResult ToEventResult(List<UnifiedEffect> effects)
        {
            var result = new EventResult { success = true };
            int goldReward = 0;
            int goldCost = 0;
            var unlockedModules = new List<string>();

            foreach (var effect in effects)
            {
                var val = effect.GetFinalValue();

                switch (effect.Type)
                {
                    case EffectType.Gold:
                        if (effect.Category == EffectCategory.Cost || effect.Operator == EffectOperator.Subtract || val < 0)
                            goldCost += (int)Math.Abs(val);
                        else
                            goldReward += (int)val;
                        break;

                    case EffectType.Module when effect.Operator == EffectOperator.Add:
                        if (!string.IsNullOrEmpty(effect.TargetId))
                        {
                            unlockedModules.Add(effect.TargetId);
                        }
                        break;
                }
            }

            result.goldReward = goldReward;
            result.goldCost = goldCost;
            result.unlockedModuleIds = unlockedModules;

            return result;
        }

        #region Effect Apply Methods

        private static string ApplyGold(UnifiedEffect effect, PlayerActor player, float value)
        {
            var intValue = (int)Math.Round(value);

            switch (effect.Operator)
            {
                case EffectOperator.Add:
                    player.carryItems.gold += intValue;
                    return intValue >= 0
                        ? $"获得 {intValue} 金币"
                        : $"消耗 {Math.Abs(intValue)} 金币";
                case EffectOperator.Subtract:
                    player.carryItems.gold -= intValue;
                    return $"消耗 {intValue} 金币";
                case EffectOperator.Set:
                    player.carryItems.gold = Math.Max(0, intValue);
                    return $"金币设为 {intValue}";
                case EffectOperator.Multiply:
                    player.carryItems.gold = (int)(player.carryItems.gold * value);
                    return $"金币乘以 {value:F1}";
                case EffectOperator.Percent:
                    var delta = (int)(player.carryItems.gold * value);
                    player.carryItems.gold += delta;
                    return delta >= 0
                        ? $"金币 +{delta} ({value * 100:F0}%)"
                        : $"金币 {delta} ({value * 100:F0}%)";
                default:
                    return $"[金币操作: {effect.Operator} {intValue}]";
            }
        }

        private static string ApplyHP(UnifiedEffect effect, PlayerActor player, float value)
        {
            var intValue = (int)Math.Round(value);
            var healAmount = 0;
            var dmgAmount = 0;

            switch (effect.Operator)
            {
                case EffectOperator.Add:
                    if (intValue >= 0)
                        healAmount = intValue;
                    else
                        dmgAmount = Math.Abs(intValue);
                    break;
                case EffectOperator.Subtract:
                    dmgAmount = intValue >= 0 ? intValue : Math.Abs(intValue);
                    break;
                case EffectOperator.Set:
                    player.stats.currentHp = Mathf.Clamp(intValue, 0, player.stats.maxHp);
                    return $"生命值设为 {intValue}";
                case EffectOperator.Multiply:
                    var newHp = (int)(player.stats.currentHp * value);
                    player.stats.currentHp = Mathf.Clamp(newHp, 0, player.stats.maxHp);
                    return $"生命值乘以 {value:F1}";
                case EffectOperator.Percent:
                    var delta = (int)(player.stats.currentHp * value);
                    player.stats.currentHp = Mathf.Clamp(player.stats.currentHp + delta, 0, player.stats.maxHp);
                    return delta >= 0
                        ? $"生命值 +{delta} ({value * 100:F0}%)"
                        : $"生命值 {delta} ({value * 100:F0}%)";
            }

            if (healAmount > 0)
            {
                player.stats.currentHp = Mathf.Min(player.stats.currentHp + healAmount, player.stats.maxHp);
                return $"恢复 {healAmount} 生命值";
            }
            if (dmgAmount > 0)
            {
                player.stats.currentHp = Mathf.Max(0, player.stats.currentHp - dmgAmount);
                return $"受到 {dmgAmount} 点伤害";
            }

            return $"生命值: {effect.Operator} {intValue}";
        }

        private static string ApplyEXP(UnifiedEffect effect, PlayerActor player, float value)
        {
            var intValue = (int)Math.Round(Math.Abs(value));
            if (intValue <= 0)
                return "[经验效果: 数值为0]";

            int levelsGained;

            switch (effect.Operator)
            {
                case EffectOperator.Add:
                case EffectOperator.Multiply:
                    levelsGained = player.AddExp(intValue);
                    break;
                case EffectOperator.Set:
                    player.stats.exp = intValue;
                    UnityEngine.Debug.Log($"[EffectExecutor] Set EXP to {intValue}");
                    return $"经验值设为 {intValue}";
                case EffectOperator.Percent:
                    var percentExp = (int)(player.stats.exp * intValue);
                    levelsGained = player.AddExp(Math.Max(1, percentExp));
                    break;
                default:
                    levelsGained = player.AddExp(intValue);
                    break;
            }

            if (levelsGained > 0)
            {
                UnityEngine.Debug.Log($"[EffectExecutor] EXP: +{intValue}, Leveled up {levelsGained} time(s)! Now level {player.level}");
                return $"获得 {intValue} 经验值，提升了 {levelsGained} 级！当前等级 {player.level}";
            }
            else
            {
                UnityEngine.Debug.Log($"[EffectExecutor] EXP: +{intValue}, Progress: {player.stats.exp}/{player.stats.expToNextLevel}");
                return $"获得 {intValue} 经验值 ({player.stats.exp}/{player.stats.expToNextLevel})";
            }
        }

        private static string ApplyItem(UnifiedEffect effect, PlayerActor player)
        {
            if (string.IsNullOrEmpty(effect.TargetId))
                return "[物品效果: 缺少TargetId]";

            var itemName = effect.TargetId;
            // 尝试从ItemManager获取物品名称
            try
            {
                var template = ItemManager.GetTemplate(effect.TargetId);
                if (template != null && !string.IsNullOrEmpty(template.nameTextId))
                    itemName = template.nameTextId;
            }
            catch { /* 使用ID作为名称 */ }

            if (effect.Operator == EffectOperator.Add || effect.Category == EffectCategory.Reward)
            {
                var result = InventoryDesign.instance.AddItem(effect.TargetId, effect.Quantity);
                if (result.success)
                    UnityEngine.Debug.Log($"[EffectExecutor] Add Item: {effect.TargetId} x{effect.Quantity} - Success");
                else
                    UnityEngine.Debug.LogWarning($"[EffectExecutor] Add Item: {effect.TargetId} x{effect.Quantity} - Failed: {result.message}");
                return $"获得 {itemName} x{effect.Quantity}";
            }
            else
            {
                // RemoveItem 需要 instanceId int，而 effect.TargetId 是 templateId string
                // 暂时记录尝试，实际移除需要通过物品实例进行
                UnityEngine.Debug.Log($"[EffectExecutor] Remove Item (TODO - needs instanceId): {effect.TargetId} x{effect.Quantity}");
                return $"失去 {itemName} x{effect.Quantity}";
            }
        }

        private static string ApplyFlag(UnifiedEffect effect, PlayerActor player)
        {
            if (string.IsNullOrEmpty(effect.TargetId))
                return "[标志效果: 缺少TargetId]";

            var value = effect.Operator switch
            {
                EffectOperator.Set => effect.Value > 0 ? "true" : "false",
                EffectOperator.Add => "true",
                _ => "true"
            };

            player.SetFlag(effect.TargetId, value);
            UnityEngine.Debug.Log($"[EffectExecutor] Flag: {effect.TargetId} = {value}");
            return $"标志: {effect.TargetId} = {value}";
        }

        private static string ApplyModule(UnifiedEffect effect, PlayerActor player)
        {
            if (string.IsNullOrEmpty(effect.TargetId))
                return "[模块效果: 缺少TargetId]";

            if (effect.Operator == EffectOperator.Add || effect.Category != EffectCategory.Cost)
            {
                var success = Game1.Modules.ModuleFactory.CreateAndAddToPlayer(effect.TargetId, player);
                if (success)
                {
                    UnityEngine.Debug.Log($"[EffectExecutor] Added Module: {effect.TargetId}");
                    return $"解锁模块: {effect.TargetId}";
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[EffectExecutor] Failed to add module: {effect.TargetId} (unknown ID)");
                    return $"模块解锁失败: {effect.TargetId}";
                }
            }
            else
            {
                player.modules.RemoveModule(effect.TargetId);
                UnityEngine.Debug.Log($"[EffectExecutor] Remove Module: {effect.TargetId}");
                return $"移除: {effect.TargetId}";
            }
        }

        private static string ApplyHeal(UnifiedEffect effect, PlayerActor player, float value)
        {
            var healAmount = (int)Math.Round(Mathf.Abs(value));
            player.stats.currentHp = Mathf.Min(player.stats.currentHp + healAmount, player.stats.maxHp);
            UnityEngine.Debug.Log($"[EffectExecutor] Heal: {healAmount}, CurrentHP: {player.stats.currentHp}/{player.stats.maxHp}");
            return $"恢复 {healAmount} 生命值";
        }

        private static string ApplyDamage(UnifiedEffect effect, PlayerActor player, float value)
        {
            var scaledValue = value;
            if (effect.HasScaling && !string.IsNullOrEmpty(effect.ScalingStat))
            {
                // 根据 ScalingStat 缩放伤害
                var statValue = GetPlayerStat(player, effect.ScalingStat);
                scaledValue = value * statValue * effect.ScalingFactor;
            }
            var dmgAmount = (int)Math.Round(Mathf.Abs(scaledValue));
            player.stats.currentHp = Mathf.Max(0, player.stats.currentHp - dmgAmount);
            UnityEngine.Debug.Log($"[EffectExecutor] Damage: {dmgAmount} (base:{value}, scale:{effect.ScalingStat}x{effect.ScalingFactor}), CurrentHP: {player.stats.currentHp}/{player.stats.maxHp}");
            return $"受到 {dmgAmount} 点伤害";
        }

        private static float GetPlayerStat(PlayerActor player, string statName)
        {
            return statName switch
            {
                "attack" => player.stats.attack,
                "defense" => player.stats.defense,
                "speed" => player.stats.speed,
                "level" => player.level,
                _ => 1f
            };
        }

        private static string ApplyUnlock(UnifiedEffect effect, PlayerActor player)
        {
            UnityEngine.Debug.Log($"[EffectExecutor] Unlock: {effect.TargetId ?? "unknown"}");
            return $"解锁: {effect.TargetId ?? "新功能"}";
        }

        #endregion
    }
}
