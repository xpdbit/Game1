using System.Collections.Generic;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 根节点 - 事件树的入口点
    /// 每个EventTreeGraph必须有一个且仅有一个RootNode
    /// </summary>
    [System.Serializable]
    public class RootNode : EventTreeNodeBase
    {
        public override string NodeType => "Root";

        /// <summary>
        /// 获取输出连接的唯一子节点
        /// </summary>
        public override List<EventTreeNodeBase> GetOutputNodes()
        {
            // Root节点输出端口连接的节点
            return new List<EventTreeNodeBase>();
        }

        public override bool Validate(out string errorMessage)
        {
            if (!base.Validate(out errorMessage))
                return false;

            // Root节点可空输出（直接结束的情况）
            return true;
        }
    }
}
