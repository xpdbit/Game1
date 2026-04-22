using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 选择对话框选项数据
    /// </summary>
    [Serializable]
    public class ChoiceOption
    {
        public string optionId;          // 选项唯一ID
        public string text;               // 显示文本
        public ChoiceType choiceType;    // 选项类型
        public Sprite icon;              // 图标（可选）
        public bool isEnabled = true;    // 是否可用
        public int cost;                 // 选择消耗（如金币）
        public int requiredItemId;       // 需要物品（可选）
        public List<string> tagRequirements = new();  // 需要标签

        /// <summary>
        /// 创建选项
        /// </summary>
        public static ChoiceOption Create(string optionId, string text, ChoiceType choiceType = ChoiceType.Normal)
        {
            return new ChoiceOption
            {
                optionId = optionId,
                text = text,
                choiceType = choiceType
            };
        }
    }

    /// <summary>
    /// 选项类型
    /// </summary>
    public enum ChoiceType
    {
        Normal,     // 普通选项
        Confirm,    // 确认/肯定
        Cancel,     // 取消/否定
        Danger,     // 危险选项
        Special     // 特殊选项
    }

    /// <summary>
    /// 对话框类型
    /// </summary>
    public enum DialogType
    {
        Selection,  // 选择对话框（多个选项）
        Confirm,    // 确认对话框（是/否）
        Information,// 信息对话框（仅显示）
        Combat      // 战斗对话框
    }

    /// <summary>
    /// 选择事件数据
    /// </summary>
    public class SelectionEventData
    {
        public DialogType dialogType;
        public string title;
        public string description;
        public List<ChoiceOption> options;
        public ChoiceOption selectedOption;
        public Action<ChoiceOption> onOptionSelected;
        public Action onDialogClosed;

        public static SelectionEventData CreateSelectionDialog(
            string title,
            string description,
            List<ChoiceOption> options,
            Action<ChoiceOption> onSelected = null)
        {
            return new SelectionEventData
            {
                dialogType = DialogType.Selection,
                title = title,
                description = description,
                options = options,
                onOptionSelected = onSelected
            };
        }
    }
}