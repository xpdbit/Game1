using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 战斗HP条组件
    /// 使用UIProgressBar实现，支持玩家和敌人HP显示
    /// </summary>
    public class UICombatHPBar : MonoBehaviour
    {
        [Header("HP条组件")]
        public UIProgressBar hpBar;
        public UIText hpText;
        public UIText nameText;

        [Header("HP条颜色配置")]
        public Color normalHPColor = new Color(0.2f, 0.8f, 0.2f);  // 绿色
        public Color lowHPColor = new Color(0.8f, 0.2f, 0.2f);      // 红色
        public Color emptyHPColor = new Color(0.3f, 0.3f, 0.3f);     // 灰色

        [Header("低HP阈值")]
        [Range(0f, 1f)]
        public float lowHPThreshold = 0.25f;

        // 当前显示的数据
        private int _currentHp;
        private int _maxHp;
        private string _displayName;

        #region Unity Lifecycle

        private void Awake()
        {
            if (hpBar == null)
                hpBar = GetComponentInChildren<UIProgressBar>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 初始化HP条
        /// </summary>
        /// <param name="name">显示名称</param>
        /// <param name="currentHp">当前HP</param>
        /// <param name="maxHp">最大HP</param>
        public void Initialize(string name, int currentHp, int maxHp)
        {
            _displayName = name;
            _currentHp = currentHp;
            _maxHp = maxHp;

            if (nameText != null)
                nameText.text = name;

            UpdateHPDisplay();
        }

        /// <summary>
        /// 更新HP显示
        /// </summary>
        /// <param name="currentHp">当前HP</param>
        /// <param name="animate">是否动画过渡</param>
        public void UpdateHP(int currentHp, bool animate = true)
        {
            _currentHp = Mathf.Clamp(currentHp, 0, _maxHp);
            UpdateHPDisplay(animate);
        }

        /// <summary>
        /// 立即更新HP（无动画）
        /// </summary>
        public void UpdateHPImmediate(int currentHp)
        {
            _currentHp = Mathf.Clamp(currentHp, 0, _maxHp);
            UpdateHPDisplay(animate: false);
        }

        /// <summary>
        /// 设置最大HP（用于升级等情况）
        /// </summary>
        public void SetMaxHP(int maxHp)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _currentHp = Mathf.Min(_currentHp, _maxHp);
            UpdateHPDisplay();
        }

        #endregion

        #region Private Methods

        private void UpdateHPDisplay(bool animate = true)
        {
            if (_maxHp <= 0) return;

            float ratio = (float)_currentHp / _maxHp;

            // 更新进度条
            if (hpBar != null)
            {
                if (animate)
                    hpBar.UpdateBar(ratio);
                else
                    hpBar.UpdateBarImmediate(ratio);
            }

            // 更新HP文本
            if (hpText != null)
            {
                hpText.text = $"{_currentHp}/{_maxHp}";
            }

            // 更新颜色（根据HP比例）
            UpdateHPColor(ratio);
        }

        private void UpdateHPColor(float ratio)
        {
            if (hpBar == null || hpBar.progressBar == null) return;

            var image = hpBar.progressBar.GetComponent<Image>();
            if (image == null) return;

            Color targetColor;
            if (ratio <= 0)
                targetColor = emptyHPColor;
            else if (ratio <= lowHPThreshold)
                targetColor = lowHPColor;
            else
                targetColor = normalHPColor;

            image.color = targetColor;
        }

        #endregion

        #region Editor Helper
#if UNITY_EDITOR
        [ContextMenu("Test HP Display")]
        private void TestHPDisplay()
        {
            Initialize("Test Enemy", 75, 100);
            Debug.Log($"[UICombatHPBar] Test: {gameObject.name}");
        }

        [ContextMenu("Test Low HP")]
        private void TestLowHP()
        {
            UpdateHP(20);
        }

        [ContextMenu("Test Empty HP")]
        private void TestEmptyHP()
        {
            UpdateHP(0);
        }
#endif
        #endregion
    }
}