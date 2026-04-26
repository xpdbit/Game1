using System;
using UnityEngine;
using UnityEngine.UI;
using Game1.UI.Utils;
using Game1.UI.DataBinding;

namespace Game1
{
    public class UIProgressBar : MonoBehaviour
    {
        public RectTransform progressBar;
        public Direction direction;

        [Range(0, 1)]
        public float onDrawGizmosSelectedRatio = 0.4f;

        [Space]
        [Header("优化选项")]
        public bool enableOptimization = true;  // 启用优化：避免每帧检测

        public RectTransform indicator;
        public UIText indicatorPercentText;

        public float ratio { get; protected set; }
        public float targetRatio;

        public RectTransform rectTransform => _rectTransform ??= this.GetComponent<RectTransform>();
        private RectTransform _rectTransform;

        // 优化：缓存上次更新的值，减少不必要的Canvas rebuild
        private float _lastReportedRatio;
        private bool _isAnimating;

        /// <summary>
        /// ProgressBar配置引用（可选）
        /// </summary>
        protected ProgressBarConfig config;

        /// <summary>
        /// ProgressBar更新事件，当值变化时触发（仅EventDriven模式）
        /// 参数：currentRatio, targetRatio
        /// </summary>
        public event Action<float, float> OnProgressUpdate;

        /// <summary>
        /// 检查事件是否有订阅者（供派生类使用）
        /// </summary>
        protected bool HasProgressUpdateSubscribers()
        {
            return OnProgressUpdate != null;
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public UIProgressBar()
        {
        }

        /// <summary>
        /// 带配置的构造函数
        /// </summary>
        /// <param name="config">ProgressBar配置数据</param>
        public UIProgressBar(ProgressBarConfig config)
        {
            this.config = config;
        }

        private void OnDrawGizmosSelected()
        {
            this.UpdateBarImmediate(onDrawGizmosSelectedRatio);
        }

        protected virtual void Awake()
        {
            this.ratio = onDrawGizmosSelectedRatio;
            this.targetRatio = this.ratio;
            _lastReportedRatio = ratio;
        }

        protected virtual void Update()
        {
            if (!enableOptimization)
            {
                // 未启用优化时使用原有逻辑
                if (this.ratio != targetRatio)
                {
                    this.ratio = Mathf.Lerp(this.ratio, targetRatio, Time.deltaTime * (config?.lerpSpeed ?? 8f));
                    this.UpdateBar();
                }
                return;
            }

            // 优化版本：只在值真正变化或正在动画时才更新
            if (_isAnimating || Mathf.Abs(this.ratio - targetRatio) > 0.0001f)
            {
                _isAnimating = Mathf.Abs(this.ratio - targetRatio) > 0.0001f;

                if (_isAnimating)
                {
                    this.ratio = Mathf.Lerp(this.ratio, targetRatio, Time.deltaTime * (config?.lerpSpeed ?? 8f));

                    // 接近目标时直接跳到目标值
                    if (Mathf.Abs(this.ratio - targetRatio) < 0.001f)
                    {
                        this.ratio = targetRatio;
                        _isAnimating = false;
                    }
                }

                // 只有值真正变化时才更新UI
                if (Mathf.Abs(_lastReportedRatio - this.ratio) > 0.0001f)
                {
                    this.UpdateBar();
                    _lastReportedRatio = this.ratio;
                }
            }
        }

        public void UpdateBar(float ratio)
        {
            this.targetRatio = ratio;

            // EventDriven模式下，值变化时触发事件
            if (config != null && config.updateMode == UpdateMode.EventDriven)
            {
                if (Mathf.Abs(this.ratio - ratio) > 0.0001f)
                {
                    OnProgressUpdate?.Invoke(this.ratio, ratio);
                }
            }
        }

        public void UpdateBarImmediate(float ratio)
        {
            this.ratio = ratio;
            this.targetRatio = ratio;
            this.UpdateBar();
        }

        public void UpdateBar()
        {
            if (this.progressBar == null)
                return;

            switch (this.direction)
            {
                case Direction.LeftToRight:
                    this.SetHorizontalBar(0f, this.ratio);
                    this.SetIndicatorHorizontal(this.ratio);
                    break;
                case Direction.RightToLeft:
                    this.SetHorizontalBar(1 - this.ratio, 1f);
                    this.SetIndicatorHorizontal(1 - this.ratio);
                    break;
                case Direction.TopToBottom:
                    this.SetVerticalBar(1 - this.ratio, 1f);
                    this.SetIndicatorVertical(1 - this.ratio);
                    break;
                case Direction.BottomToTop:
                    this.SetVerticalBar(0f, this.ratio);
                    this.SetIndicatorVertical(this.ratio);
                    break;
            }

            if (indicatorPercentText != null)
                indicatorPercentText.SetNumberWithSuffix(this.ratio * 100f, "0", "%");
        }

        private void SetHorizontalBar(float minX, float maxX)
        {
            this.progressBar.anchorMin = new Vector2(minX, 0f);
            this.progressBar.anchorMax = new Vector2(maxX, 1f);
            this.progressBar.offsetMin = Vector2.zero;
            this.progressBar.offsetMax = Vector2.zero;
        }

        private void SetVerticalBar(float minY, float maxY)
        {
            this.progressBar.anchorMin = new Vector2(0f, minY);
            this.progressBar.anchorMax = new Vector2(1f, maxY);
            this.progressBar.offsetMin = Vector2.zero;
            this.progressBar.offsetMax = Vector2.zero;
        }

        private void SetIndicatorHorizontal(float position)
        {
            if (this.indicator == null) return;

            this.indicator.anchorMin = new Vector2(position, 0.5f);
            this.indicator.anchorMax = new Vector2(position, 0.5f);
            this.indicator.offsetMin = Vector2.zero;
            this.indicator.offsetMax = Vector2.zero;
        }

        private void SetIndicatorVertical(float position)
        {
            if (this.indicator == null) return;

            this.indicator.anchorMin = new Vector2(0.5f, position);
            this.indicator.anchorMax = new Vector2(0.5f, position);
            this.indicator.offsetMin = Vector2.zero;
            this.indicator.offsetMax = Vector2.zero;
        }

        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop,
        }
    }
}
