using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 队伍UI组件
    /// 继承BaseUIPanel以支持UIManager状态管理
    /// </summary>
    public class UITeam : BaseUIPanel
    {
        public override string panelId => "TeamPanel";

        [Header("UI列表组件")]
        public UIListItems uIListItems;
        public Button closeButton;

        [Header("队伍信息")]
        public UIText teamCountText;
        public UIText totalPowerText;

        [Header("设置")]
        public bool autoRefreshOnEvent = true;  // 自动刷新开关

        private List<UITeamMember> _members = new();
        private Dictionary<int, UITeamMember> _membersById = new();
        private Action<TeamEventData> _onTeamChanged;

        public UITeamMember[] members => _members.ToArray();

        #region Unity Lifecycle
        private void Awake()
        {
            if (uIListItems == null)
                uIListItems = GetComponent<UIListItems>();

            _onTeamChanged = OnTeamEvent;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() =>
                {
                    Close();
                });
            }
        }

        private void OnEnable()
        {
            if (autoRefreshOnEvent)
            {
                TeamManager.SubscribeTeamChanged(_onTeamChanged);
            }
            Refresh();
        }

        private void OnDisable()
        {
            TeamManager.UnsubscribeTeamChanged(_onTeamChanged);
        }

        public override void OnOpen()
        {
            base.OnOpen();
            Refresh();
        }

        public override void OnClose()
        {
            base.OnClose();
        }
        #endregion

        #region Public API

        /// <summary>
        /// 刷新UI（从TeamDesign同步数据）
        /// </summary>
        public void Refresh()
        {
            Clear();

            var members = TeamManager.GetAllMembers().ToList();

            // 按等级排序
            members.Sort((a, b) => b.level.CompareTo(a.level));

            foreach (var member in members)
            {
                Append(member);
            }

            UpdateTeamInfo();
        }

        /// <summary>
        /// 添加成员到UI
        /// </summary>
        public UITeamMember Append(TeamMemberData memberData)
        {
            if (memberData == null)
                return null;

            // 检查是否已存在
            if (_membersById.ContainsKey(memberData.id))
            {
                var existing = _membersById[memberData.id];
                existing.UpdateUI();
                return existing;
            }

            // 添加新项
            if (uIListItems == null)
                return null;

            var result = uIListItems.AddItem($"TeamMember_{memberData.id}");
            var member = new UITeamMember
            {
                memberData = memberData,
                listItemRect = result.rectTransform
            };

            _members.Add(member);
            _membersById[memberData.id] = member;
            member.UpdateUI();

            return member;
        }

        /// <summary>
        /// 移除成员
        /// </summary>
        public void Remove(UITeamMember uiMember)
        {
            if (uiMember == null || uiMember.memberData == null)
                return;

            var removeResult = TeamManager.RemoveMember(uiMember.memberData.id);
            if (!removeResult.success)
            {
                Debug.LogWarning($"[UITeam] Failed to remove member: {removeResult.message}");
            }

            // UI会在事件回调中自动更新
        }

        /// <summary>
        /// 移除所有成员
        /// </summary>
        public void Clear()
        {
            _members.Clear();
            _membersById.Clear();
            if (uIListItems != null)
                uIListItems.Clear();
        }

        /// <summary>
        /// 更新队伍信息显示
        /// </summary>
        public void UpdateTeamInfo()
        {
            if (teamCountText != null)
            {
                teamCountText.text = $"{TeamManager.GetMemberCount()}/{TeamManager.RemainingSlots() + TeamManager.GetMemberCount()}";
            }

            if (totalPowerText != null)
            {
                totalPowerText.text = $"战力: {TeamManager.GetTotalCombatPower()}";
            }
        }

        /// <summary>
        /// 获取选中的成员
        /// </summary>
        public List<UITeamMember> GetSelectedMembers()
        {
            var selected = new List<UITeamMember>();
            foreach (var member in _members)
            {
                if (member.isSelected)
                    selected.Add(member);
            }
            return selected;
        }

        /// <summary>
        /// 选中成员
        /// </summary>
        public void SelectMember(UITeamMember member, bool selected)
        {
            if (member == null) return;
            member.SetSelected(selected);
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        public void SelectAll(bool selected)
        {
            foreach (var member in _members)
            {
                member.SetSelected(selected);
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// 响应队伍变化事件
        /// </summary>
        private void OnTeamEvent(TeamEventData data)
        {
            switch (data.eventType)
            {
                case TeamEventType.MemberAdded:
                    OnMemberAdded(data);
                    break;
                case TeamEventType.MemberRemoved:
                    OnMemberRemoved(data);
                    break;
                case TeamEventType.MemberUpdated:
                    OnMemberUpdated(data);
                    break;
                case TeamEventType.TeamCleared:
                    OnTeamCleared();
                    break;
                case TeamEventType.CapacityChanged:
                    UpdateTeamInfo();
                    break;
            }
        }

        private void OnMemberAdded(TeamEventData data)
        {
            if (uIListItems == null)
                return;

            var member = TeamManager.GetMember(data.memberId);
            if (member != null)
            {
                Append(member);
            }
            UpdateTeamInfo();
        }

        private void OnMemberRemoved(TeamEventData data)
        {
            if (uIListItems == null)
                return;

            if (_membersById.TryGetValue(data.memberId, out var uiMember))
            {
                _members.Remove(uiMember);
                _membersById.Remove(data.memberId);

                if (uiMember.memberData != null)
                {
                    uIListItems.RemoveItem($"TeamMember_{data.memberId}");
                }
            }
            UpdateTeamInfo();
        }

        private void OnMemberUpdated(TeamEventData data)
        {
            if (_membersById.TryGetValue(data.memberId, out var uiMember))
            {
                uiMember.UpdateUI();
            }
            UpdateTeamInfo();
        }

        private void OnTeamCleared()
        {
            Clear();
            UpdateTeamInfo();
        }

        #endregion
    }

    /// <summary>
    /// 队伍成员行UI组件
    /// </summary>
    public class UITeamMember
    {
        public TeamMemberData memberData;      // 成员数据
        public RectTransform listItemRect;     // 列表项RectTransform
        public bool isSelected { get; private set; }  // 是否选中

        // UI组件缓存
        private Button _button;
        private UIText _nameText;
        private UIText _levelText;
        private UIText _hpText;
        private UIText _powerText;
        private Image _iconImage;
        private Image _backgroundImage;
        private Image _hpBarImage;
        private Toggle _toggle;

        public Button button => _button ??= listItemRect.Find("Button")?.GetComponent<Button>();
        private UIText nameText => _nameText ??= listItemRect.Find("Button/NameText")?.GetComponentInChildren<UIText>();
        private UIText levelText => _levelText ??= listItemRect.Find("Button/LevelText")?.GetComponentInChildren<UIText>();
        private UIText hpText => _hpText ??= listItemRect.Find("Button/HpText")?.GetComponentInChildren<UIText>();
        private UIText powerText => _powerText ??= listItemRect.Find("Button/PowerText")?.GetComponentInChildren<UIText>();
        private Image iconImage => _iconImage ??= listItemRect.Find("Button/Icon")?.GetComponent<Image>();
        private Image backgroundImage => _backgroundImage ??= listItemRect.Find("Button/Background")?.GetComponent<Image>();
        private Image hpBarImage => _hpBarImage ??= listItemRect.Find("Button/HpBar")?.GetComponent<Image>();
        private Toggle toggle => _toggle ??= listItemRect.Find("Toggle")?.GetComponent<Toggle>();

        public event Action<bool> onSelectedChanged;

        public void UpdateUI()
        {
            if (memberData == null) return;

            listItemRect.name = memberData.name;

            if (nameText != null)
                nameText.text = memberData.name;

            if (levelText != null)
                levelText.text = $"Lv.{memberData.level}";

            if (hpText != null)
                hpText.text = $"{memberData.hp}/{memberData.maxHp}";

            if (powerText != null)
                powerText.text = $"战力:{memberData.GetCombatPower()}";

            // 更新HP条
            if (hpBarImage != null)
            {
                hpBarImage.fillAmount = memberData.hpPercent;
            }

            // 更新选中状态视觉
            UpdateSelectionVisual();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;
            UpdateSelectionVisual();
            onSelectedChanged?.Invoke(selected);
        }

        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelected()
        {
            SetSelected(!isSelected);
        }

        /// <summary>
        /// 更新选中状态视觉
        /// </summary>
        private void UpdateSelectionVisual()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected
                    ? new Color(0.3f, 0.6f, 1f, 0.3f)  // 选中高亮色
                    : Color.white;
            }
        }
    }
}