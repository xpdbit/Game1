using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 结束节点 - 标记事件树的结束
    /// </summary>
    [System.Serializable]
    public class EndNode : EventTreeNodeBase
    {
        public enum EndType
        {
            Normal,     // 正常结束
            GameOver,  // 游戏结束
            Loop,      // 循环回到开始
            Branch     // 分支结束（多出口）
        }

        [SerializeField] private EndType _endType = EndType.Normal;
        [SerializeField] private string _nextEventTreeId;
        [SerializeField] private bool _showSummary = true;
        [SerializeField] private string _endingText;

        public EndType endType
        {
            get => _endType;
            set => _endType = value;
        }

        public string nextEventTreeId
        {
            get => _nextEventTreeId;
            set => _nextEventTreeId = value;
        }

        public bool showSummary
        {
            get => _showSummary;
            set => _showSummary = value;
        }

        public string endingText
        {
            get => _endingText;
            set => _endingText = value;
        }

        public override string NodeType => "End";

        /// <summary>
        /// 结束节点没有输出
        /// </summary>
        public override List<EventTreeNodeBase> GetOutputNodes()
        {
            return new List<EventTreeNodeBase>();
        }

        public override bool Validate(out string errorMessage)
        {
            if (!base.Validate(out errorMessage))
                return false;

            if (_endType == EndType.Branch && string.IsNullOrEmpty(_nextEventTreeId))
            {
                errorMessage = "Branch类型的EndNode需要指定nextEventTreeId";
                return false;
            }

            return true;
        }
    }
}
