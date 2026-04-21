using UnityEngine;
using UnityEngine.UI;
using Game1.UI.Utils;

namespace Game1
{
    public class UIProgressBar : MonoBehaviour
    {
        public RectTransform progressBar;
        public Direction direction;

        [Range(0, 1)]
        public float onDrawGizmosSelectedRatio = 0.4f;

        [Space]

        public RectTransform indicator;
        public UIText indicatorPercentText;

        public float ratio { get; private set; }
        public float targetRatio;

        public RectTransform rectTransform => _rectTransform ??= this.GetComponent<RectTransform>();
        private RectTransform _rectTransform;

        private void OnDrawGizmosSelected()
        {
            this.UpdateBarImmediate(onDrawGizmosSelectedRatio);
        }

        private void Awake()
        {
            this.ratio = onDrawGizmosSelectedRatio;
            this.targetRatio = this.ratio;
        }

        private void Update()
        {
            if (this.ratio != targetRatio)
            {
                this.ratio = Mathf.Lerp(this.ratio, targetRatio, Time.deltaTime * 8f);
                this.UpdateBar();
            }
        }

        public void UpdateBar(float ratio)
        {
            this.targetRatio = ratio;
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
                indicatorPercentText.text = $"{(this.ratio * 100f).ToString("0")}%";
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
