using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 选择节点 - 提供多个选项供玩家选择
    /// </summary>
    [System.Serializable]
    public class ChoiceNode : EventTreeNodeBase
    {
        [SerializeField] private List<ChoiceOption> _choices = new();

        public List<ChoiceOption> choices
        {
            get => _choices;
            set => _choices = value;
        }

        public override string NodeType => "Choice";

        /// <summary>
        /// 获取所有选择分支连接的节点
        /// </summary>
        public override List<EventTreeNodeBase> GetOutputNodes()
        {
            var nodes = new List<EventTreeNodeBase>();
            if (graph == null)
            {
                // 如果没有图谱引用，无法解析目标节点
                return nodes;
            }
            foreach (var choice in _choices)
            {
                if (string.IsNullOrEmpty(choice.targetNodeId))
                    continue;
                var targetNode = graph.GetNodeById(choice.targetNodeId);
                if (targetNode != null)
                    nodes.Add(targetNode);
            }
            return nodes;
        }

        public override bool Validate(out string errorMessage)
        {
            if (!base.Validate(out errorMessage))
                return false;

            if (_choices == null || _choices.Count == 0)
            {
                errorMessage = "ChoiceNode至少需要一个选项";
                return false;
            }

            for (int i = 0; i < _choices.Count; i++)
            {
                if (string.IsNullOrEmpty(_choices[i].text))
                {
                    errorMessage = $"ChoiceNode的第{i + 1}个选项文本不能为空";
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 选择选项数据结构
    /// </summary>
    [System.Serializable]
    public class ChoiceOption
    {
        [SerializeField] private string _text;
        [SerializeField] private string _targetNodeId;
        [SerializeField] private List<string> _requiredItems = new();
        [SerializeField] private bool _isHidden;
        [SerializeField] private string _hideCondition;

        public string text
        {
            get => _text;
            set => _text = value;
        }

        public string targetNodeId
        {
            get => _targetNodeId;
            set => _targetNodeId = value;
        }

        public List<string> requiredItems
        {
            get => _requiredItems;
            set => _requiredItems = value;
        }

        public bool isHidden
        {
            get => _isHidden;
            set => _isHidden = value;
        }

        public string hideCondition
        {
            get => _hideCondition;
            set => _hideCondition = value;
        }
    }
}
