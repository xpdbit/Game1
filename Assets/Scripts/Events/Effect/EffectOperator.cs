#nullable enable
using System.ComponentModel;

namespace Game1.Events.Effect
{
    /// <summary>
    /// 效果操作符，定义如何将效果值应用到目标上。
    /// </summary>
    public enum EffectOperator
    {
        /// <summary>增加（默认操作）。</summary>
        [Description("增加")]
        Add,

        /// <summary>减少。</summary>
        [Description("减少")]
        Subtract,

        /// <summary>乘法运算。</summary>
        [Description("乘法")]
        Multiply,

        /// <summary>除法运算。</summary>
        [Description("除法")]
        Divide,

        /// <summary>直接设置为指定值。</summary>
        [Description("设为")]
        Set,

        /// <summary>百分比运算（如 -0.15 表示减少15%）。</summary>
        [Description("百分比")]
        Percent,
    }
}
