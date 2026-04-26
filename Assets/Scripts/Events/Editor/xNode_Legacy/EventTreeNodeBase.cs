using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 事件树节点基类
    /// 基于xNode架构设计，当前为占位符实现
    /// 待xNode安装后需迁移至继承Node类
    /// </summary>
    [Serializable]
    public abstract class EventTreeNodeBase
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private List<string> _conditions = new();
        [SerializeField] private List<EventReward> _rewards = new();

        // 父图谱引用（非序列化，用于运行时查找）
        [NonSerialized] private EventTreeGraph _graph;

        public EventTreeGraph graph
        {
            get => _graph;
            internal set => _graph = value;
        }

        public string nodeId
        {
            get => _nodeId;
            set => _nodeId = value;
        }

        public string title
        {
            get => _title;
            set => _title = value;
        }

        public string description
        {
            get => _description;
            set => _description = value;
        }

        public List<string> conditions
        {
            get => _conditions;
            set => _conditions = value;
        }

        public List<EventReward> rewards
        {
            get => _rewards;
            set => _rewards = value;
        }

        /// <summary>
        /// 获取输出端口连接的下一个节点
        /// </summary>
        public abstract List<EventTreeNodeBase> GetOutputNodes();

        /// <summary>
        /// 节点类型标识
        /// </summary>
        public abstract string NodeType { get; }

        /// <summary>
        /// 验证节点配置是否有效
        /// </summary>
        public virtual bool Validate(out string errorMessage)
        {
            if (string.IsNullOrEmpty(_nodeId))
            {
                errorMessage = "NodeId不能为空";
                return false;
            }
            errorMessage = null;
            return true;
        }
    }

    /// <summary>
    /// 事件奖励数据结构
    /// </summary>
    [Serializable]
    public class EventReward
    {
        [SerializeField] private string _type;
        [SerializeField] private string _itemId;
        [SerializeField] private int _amount;
        [SerializeField] private float _chance = 1f;

        public string type
        {
            get => _type;
            set => _type = value;
        }

        public string itemId
        {
            get => _itemId;
            set => _itemId = value;
        }

        public int amount
        {
            get => _amount;
            set => _amount = value;
        }

        public float chance
        {
            get => _chance;
            set => _chance = Mathf.Clamp01(value);
        }
    }

    /// <summary>
    /// xNode端口定义（占位）
    /// 待xNode安装后替换为XNode.NodePort
    /// </summary>
    [Serializable]
    public class NodePortPlaceholder
    {
        [SerializeField] private string _fieldName;
        [SerializeField] private List<string> _connectionIds = new();

        public string fieldName
        {
            get => _fieldName;
            set => _fieldName = value;
        }

        public List<string> connectionIds
        {
            get => _connectionIds;
            set => _connectionIds = value;
        }

        public void AddConnection(string nodeId)
        {
            if (!_connectionIds.Contains(nodeId))
            {
                _connectionIds.Add(nodeId);
            }
        }

        public void RemoveConnection(string nodeId)
        {
            _connectionIds.Remove(nodeId);
        }
    }
}
