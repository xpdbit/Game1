using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 故事节点 - 展示剧情文本内容
    /// </summary>
    [System.Serializable]
    public class StoryNode : EventTreeNodeBase
    {
        [SerializeField] private string _storyText;
        [SerializeField] private float _textSpeed = 0.05f;
        [SerializeField] private bool _showPortrait;
        [SerializeField] private string _portraitId;
        [SerializeField] private bool _autoProgress;
        [SerializeField] private float _autoProgressDelay = 3f;

        public string storyText
        {
            get => _storyText;
            set => _storyText = value;
        }

        public float textSpeed
        {
            get => _textSpeed;
            set => _textSpeed = value;
        }

        public bool showPortrait
        {
            get => _showPortrait;
            set => _showPortrait = value;
        }

        public string portraitId
        {
            get => _portraitId;
            set => _portraitId = value;
        }

        public bool autoProgress
        {
            get => _autoProgress;
            set => _autoProgress = value;
        }

        public float autoProgressDelay
        {
            get => _autoProgressDelay;
            set => _autoProgressDelay = value;
        }

        public override string NodeType => "Story";

        /// <summary>
        /// 故事节点输出到下一个节点
        /// </summary>
        public override List<EventTreeNodeBase> GetOutputNodes()
        {
            return new List<EventTreeNodeBase>();
        }

        public override bool Validate(out string errorMessage)
        {
            if (!base.Validate(out errorMessage))
                return false;

            if (string.IsNullOrEmpty(_storyText))
            {
                errorMessage = "StoryNode的故事文本不能为空";
                return false;
            }

            return true;
        }
    }
}
