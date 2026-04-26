using System;

namespace Game1.UI.DataBinding
{
    /// <summary>
    /// 数据源接口，用于UI数据绑定
    /// </summary>
    public interface IDataSource<T>
    {
        /// <summary>
        /// 当前数据值
        /// </summary>
        T Value { get; }

        /// <summary>
        /// 数据值变化时触发
        /// </summary>
        event Action<T> OnValueChanged;
    }
}