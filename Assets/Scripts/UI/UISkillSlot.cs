using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 技能槽位UI组件
    /// 显示技能图标、冷却、条件状态
    /// </summary>
    public class UISkillSlot : MonoBehaviour
    {
        [Header("UI组件")]
        public Image iconImage;
        public Image cooldownOverlay;     // 冷却遮罩（黑色半透明）
        public UIText cooldownText;      // 冷却倒计时文本
        public UIText levelText;          // 技能等级
        public Image conditionIndicator;  // 条件状态指示器

        [Header("状态颜色")]
        public Color readyColor = Color.white;
        public Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public Color conditionNotMetColor = new Color(1f, 0.5f, 0f, 1f); // 橙色

        [Header("冷却配置")]
        public float cooldownFillSpeed = 1f;  // 填充速度

        // 当前技能数据
        private SkillData _skillData;
        private float _remainingCooldown;

        // 回调
        public System.Action<SkillData> onSkillClicked;

        #region Unity Lifecycle

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnSlotClicked);
            }

            // 初始状态
            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = 0f;
        }

        private void Update()
        {
            if (_remainingCooldown > 0f)
            {
                _remainingCooldown -= Time.deltaTime;
                if (_remainingCooldown < 0f) _remainingCooldown = 0f;

                UpdateCooldownDisplay();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 设置技能数据
        /// </summary>
        public void SetSkill(SkillData skillData)
        {
            _skillData = skillData;
            _remainingCooldown = 0f;

            UpdateUI();
        }

        /// <summary>
        /// 开始冷却
        /// </summary>
        public void StartCooldown(float duration)
        {
            _remainingCooldown = duration;
            _skillData.isReady = false;
            UpdateUI();
        }

        /// <summary>
        /// 更新条件状态
        /// </summary>
        public void UpdateCondition(SkillCondition currentCondition)
        {
            if (_skillData == null) return;

            bool canUse = _skillData.CheckCondition(currentCondition);

            if (conditionIndicator != null)
            {
                conditionIndicator.gameObject.SetActive(!canUse);
                conditionIndicator.color = conditionNotMetColor;
            }

            // 更新图标颜色
            if (iconImage != null)
            {
                iconImage.color = canUse ? readyColor : disabledColor;
            }
        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public bool IsSkillUsable()
        {
            if (_skillData == null) return false;
            return _skillData.isReady && _remainingCooldown <= 0f;
        }

        #endregion

        #region Private Methods

        private void UpdateUI()
        {
            if (_skillData == null) return;

            // 更新等级文本
            if (levelText != null)
                levelText.text = $"Lv.{_skillData.currentLevel}";

            // 更新冷却显示
            UpdateCooldownDisplay();

            // 更新可用状态
            bool isUsable = _remainingCooldown <= 0f && _skillData.isReady;
            if (iconImage != null)
                iconImage.color = isUsable ? readyColor : cooldownColor;
        }

        private void UpdateCooldownDisplay()
        {
            if (_skillData == null || _skillData.cooldown <= 0f)
            {
                if (cooldownOverlay != null)
                    cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null)
                    cooldownText.text = "";
                return;
            }

            // 计算冷却进度（0=刚进入冷却，1=冷却完成）
            float progress = 1f - (_remainingCooldown / _skillData.cooldown);
            progress = Mathf.Clamp01(progress);

            if (cooldownOverlay != null)
            {
                // 使用径向渐变或简单填充
                cooldownOverlay.fillAmount = 1f - progress;
            }

            if (cooldownText != null)
            {
                if (_remainingCooldown > 0f)
                    cooldownText.text = Mathf.CeilToInt(_remainingCooldown).ToString();
                else
                    cooldownText.text = "";
            }
        }

        private void OnSlotClicked()
        {
            if (_skillData == null) return;

            // 检查是否可用
            if (!IsSkillUsable())
            {
                Debug.Log($"[UISkillSlot] Skill {_skillData.id} on cooldown or not ready");
                return;
            }

            // 触发回调
            onSkillClicked?.Invoke(_skillData);

            // 开始冷却
            if (_skillData.cooldown > 0f)
            {
                StartCooldown(_skillData.cooldown);
            }
        }

        #endregion

        #region Editor Helper
#if UNITY_EDITOR
        [ContextMenu("Test Skill Ready")]
        private void TestSkillReady()
        {
            var skillData = new SkillData
            {
                id = "TestSkill",
                nameTextId = "Test Skill",
                currentLevel = 1,
                cooldown = 5f,
                isReady = true
            };
            SetSkill(skillData);
        }

        [ContextMenu("Test Cooldown")]
        private void TestCooldown()
        {
            StartCooldown(5f);
        }
#endif
        #endregion
    }

    /// <summary>
    /// 技能面板（管理多个技能槽）
    /// </summary>
    public class UISkillPanel : BaseUIPanel
    {
        public override string panelId => "SkillPanel";

        [Header("技能槽配置")]
        public UISkillSlot[] skillSlots;
        public int maxActiveSkills = 4;

        [Header("条件状态")]
        public SkillCondition currentCondition = SkillCondition.InCombat;

        private void OnEnable()
        {
            // 订阅技能更新事件
            SkillManager.onSkillUsed += OnSkillUsed;
            SkillManager.onSkillCooldownEnd += OnSkillCooldownEnd;

            Refresh();
        }

        private void OnDisable()
        {
            SkillManager.onSkillUsed -= OnSkillUsed;
            SkillManager.onSkillCooldownEnd -= OnSkillCooldownEnd;
        }

        private void OnSkillUsed(int memberId, SkillData skill)
        {
            // 技能被使用后刷新
            Refresh();
        }

        private void OnSkillCooldownEnd(int memberId, SkillData skill)
        {
            // 冷却结束后刷新
            Refresh();
        }

        public override void OnOpen()
        {
            base.OnOpen();
            Refresh();
        }

        /// <summary>
        /// 刷新所有技能槽
        /// </summary>
        public void Refresh()
        {
            // 从TeamManager获取队伍成员技能
            var members = TeamManager.GetAllMembers();

            int slotIndex = 0;
            foreach (var member in members)
            {
                if (slotIndex >= skillSlots.Length) break;

                var skills = SkillManager.GetAvailableSkills(member);
                foreach (var skillId in skills)
                {
                    if (slotIndex >= skillSlots.Length) break;

                    var template = SkillManager.GetTemplate(skillId);
                    if (template != null && template.type == SkillType.Active)
                    {
                        var skillData = template.ToSkillData();
                        skillSlots[slotIndex].SetSkill(skillData);
                        skillSlots[slotIndex].UpdateCondition(currentCondition);
                        skillSlots[slotIndex].gameObject.SetActive(true);
                        slotIndex++;
                    }
                }
            }

            // 隐藏多余的槽位
            for (int i = slotIndex; i < skillSlots.Length; i++)
            {
                skillSlots[i].gameObject.SetActive(false);
            }
        }
    }
}