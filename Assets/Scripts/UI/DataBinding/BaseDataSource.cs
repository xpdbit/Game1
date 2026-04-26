using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.UI.DataBinding
{
    /// <summary>
    /// 数据源抽象基类，实现值比较和通知功能
    /// </summary>
    public abstract class BaseDataSource<T> : IDataSource<T>, IDisposable
    {
        protected T _value;
        private bool _disposed;

        public T Value
        {
            get => _value;
            protected set
            {
                if (!EqualityCompare(_value, value))
                {
                    _value = value;
                    NotifyValueChanged();
                }
            }
        }

        public event Action<T> OnValueChanged;

        protected BaseDataSource(T initialValue = default)
        {
            _value = initialValue;
        }

        /// <summary>
        /// 比较两个值是否相等，默认使用标准 equality
        /// </summary>
        protected virtual bool EqualityCompare(T left, T right)
        {
            return EqualityComparer<T>.Default.Equals(left, right);
        }

        /// <summary>
        /// 通知值已变化
        /// </summary>
        protected void NotifyValueChanged()
        {
            OnValueChanged?.Invoke(_value);
        }

        /// <summary>
        /// 直接设置值而不触发比较，用于外部直接更新
        /// </summary>
        protected void SetValueDirect(T value)
        {
            _value = value;
            NotifyValueChanged();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            OnValueChanged = null;
            OnDispose();
        }

        /// <summary>
        /// 子类可重写的释放逻辑
        /// </summary>
        protected virtual void OnDispose() { }
    }
}