#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Game1.Events.Effect
{
    /// <summary>
    /// 统一效果数据模型，取代项目中碎片化的多种 Effect 表示方式。
    /// 支持类别区分（奖励/消耗/状态）、多种操作符、随机区间和属性缩放。
    /// </summary>
    [Serializable]
    public class UnifiedEffect
    {
        /// <summary>效果类别：奖励、消耗、状态。</summary>
        public EffectCategory Category { get; set; } = EffectCategory.Reward;

        /// <summary>效果类型：金币、生命、经验、物品、标志等。</summary>
        public EffectType Type { get; set; } = EffectType.Gold;

        /// <summary>操作符：增加、减少、乘法等。</summary>
        public EffectOperator Operator { get; set; } = EffectOperator.Add;

        /// <summary>基础数值。</summary>
        public float Value { get; set; }

        /// <summary>随机区间下限（启用随机时使用）。</summary>
        public float RandomMin { get; set; }

        /// <summary>随机区间上限（启用随机时使用）。</summary>
        public float RandomMax { get; set; }

        /// <summary>目标ID：物品ID、标志名称、Buff ID等。</summary>
        public string? TargetId { get; set; }

        /// <summary>数量（物品、模块等）。</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>缩放属性名（如 "Attack"、"Defense"）。</summary>
        public string? ScalingStat { get; set; }

        /// <summary>缩放系数。</summary>
        public float ScalingFactor { get; set; } = 1f;

        [ThreadStatic]
        private static System.Random? _rng;

        /// <summary>
        /// 获取最终效果值，考虑随机区间。
        /// </summary>
        public float GetFinalValue()
        {
            if (RandomMin != 0 || RandomMax != 0)
            {
                _rng ??= new System.Random();
                return (float)(RandomMin + _rng.NextDouble() * (RandomMax - RandomMin));
            }
            return Value;
        }

        /// <summary>
        /// 判断是否为随机效果。
        /// </summary>
        public bool IsRandom => RandomMin != 0 || RandomMax != 0;

        /// <summary>
        /// 判断是否需要属性缩放。
        /// </summary>
        public bool HasScaling => !string.IsNullOrEmpty(ScalingStat) && ScalingFactor != 0;

        /// <summary>
        /// 判断是否为简单的数值操作（加减乘除等）。
        /// </summary>
        public bool IsNumericOperation => Type switch
        {
            EffectType.Gold or EffectType.HP or EffectType.EXP or EffectType.Damage or EffectType.Heal => true,
            _ => false,
        };

        /// <summary>
        /// 判断是否为标志操作。
        /// </summary>
        public bool IsFlagOperation => Type == EffectType.Flag && Operator == EffectOperator.Set;

        /// <summary>
        /// 判断是否为物品操作。
        /// </summary>
        public bool IsItemOperation => Type == EffectType.Item;

        /// <summary>
        /// 判断是否为模块操作。
        /// </summary>
        public bool IsModuleOperation => Type == EffectType.Module;

        /// <summary>
        /// 返回效果的字符串表示，用于调试和日志。
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Category}] {Type} {Operator} {Value}");
            if (IsRandom)
                sb.Append($" (random {RandomMin}-{RandomMax})");
            if (!string.IsNullOrEmpty(TargetId))
                sb.Append($" target:{TargetId}");
            if (Quantity > 1)
                sb.Append($" x{Quantity}");
            if (HasScaling)
                sb.Append($" scale({ScalingStat}x{ScalingFactor})");
            return sb.ToString();
        }
    }
}
