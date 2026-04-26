using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 事件树图谱 - 包含所有节点及其连接关系
    /// 基于xNode架构设计，当前为占位符实现
    /// 待xNode安装后需迁移至继承NodeGraph类
    /// </summary>
    [CreateAssetMenu(fileName = "NewEventTree", menuName = "Game1/Events/Event Tree Graph")]
    public class EventTreeGraph : ScriptableObject
    {
        [SerializeField] private string _graphId;
        [SerializeField] private string _graphName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private List<EventTreeNodeBase> _nodes = new();
        [SerializeField] private List<NodeConnection> _connections = new();
        [SerializeField] private string _category;
        [SerializeField] private int _version = 1;

        // Dictionary缓存：节点ID -> 节点映射，用于O(1)查找
        [NonSerialized] private Dictionary<string, EventTreeNodeBase> _nodeCache;

        public string graphId
        {
            get => _graphId;
            set => _graphId = value;
        }

        public string graphName
        {
            get => _graphName;
            set => _graphName = value;
        }

        public string description
        {
            get => _description;
            set => _description = value;
        }

        public List<EventTreeNodeBase> nodes
        {
            get => _nodes;
            set => _nodes = value;
        }

        public List<NodeConnection> connections
        {
            get => _connections;
            set => _connections = value;
        }

        public string category
        {
            get => _category;
            set => _category = value;
        }

        public int version
        {
            get => _version;
            set => _version = value;
        }

        /// <summary>
        /// 获取根节点
        /// </summary>
        public RootNode GetRootNode()
        {
            foreach (var node in _nodes)
            {
                if (node is RootNode rootNode)
                    return rootNode;
            }
            return null;
        }

        /// <summary>
        /// 根据ID获取节点（O(1)查找）
        /// </summary>
        public EventTreeNodeBase GetNodeById(string nodeId)
        {
            if (_nodeCache == null || _nodeCache.Count != _nodes.Count)
                RebuildNodeCache();

            return _nodeCache.TryGetValue(nodeId, out var node) ? node : null;
        }

        /// <summary>
        /// 添加节点到图谱
        /// </summary>
        public void AddNode(EventTreeNodeBase node)
        {
            if (!_nodes.Contains(node))
            {
                _nodes.Add(node);
                node.graph = this;
                if (_nodeCache != null)
                    _nodeCache[node.nodeId] = node;
            }
        }

        /// <summary>
        /// 从图谱移除节点
        /// </summary>
        public void RemoveNode(EventTreeNodeBase node)
        {
            _nodes.Remove(node);
            node.graph = null;
            if (_nodeCache != null && !string.IsNullOrEmpty(node.nodeId))
                _nodeCache.Remove(node.nodeId);
            // 同时移除相关连接
            _connections.RemoveAll(c => c.sourceNodeId == node.nodeId || c.targetNodeId == node.nodeId);
        }

        /// <summary>
        /// 重建节点缓存
        /// </summary>
        private void RebuildNodeCache()
        {
            _nodeCache = new Dictionary<string, EventTreeNodeBase>(_nodes.Count);
            foreach (var node in _nodes)
            {
                if (!string.IsNullOrEmpty(node.nodeId))
                    _nodeCache[node.nodeId] = node;
            }
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        public void AddConnection(string sourceNodeId, string sourceField, string targetNodeId)
        {
            var connection = new NodeConnection
            {
                sourceNodeId = sourceNodeId,
                sourceField = sourceField,
                targetNodeId = targetNodeId
            };
            _connections.Add(connection);
        }

        /// <summary>
        /// 验证图谱完整性
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            // 检查根节点
            var rootNodes = _nodes.FindAll(n => n is RootNode);
            if (rootNodes.Count == 0)
            {
                errors.Add("事件树必须包含一个根节点");
            }
            else if (rootNodes.Count > 1)
            {
                errors.Add("事件树只能包含一个根节点");
            }

            // 检查结束节点
            var endNodes = _nodes.FindAll(n => n is EndNode);
            if (endNodes.Count == 0)
            {
                errors.Add("事件树必须包含至少一个结束节点");
            }

            // 检查所有节点的ID唯一性
            var ids = new HashSet<string>();
            foreach (var node in _nodes)
            {
                if (!ids.Add(node.nodeId))
                {
                    errors.Add($"发现重复的节点ID: {node.nodeId}");
                }
            }

            // 验证每个节点
            foreach (var node in _nodes)
            {
                if (!node.Validate(out string nodeError))
                {
                    errors.Add($"节点 {node.nodeId} 验证失败: {nodeError}");
                }
            }

            return errors.Count == 0;
        }
    }

    /// <summary>
    /// 节点连接数据结构
    /// </summary>
    [Serializable]
    public class NodeConnection
    {
        [SerializeField] private string _sourceNodeId;
        [SerializeField] private string _sourceField;
        [SerializeField] private string _targetNodeId;

        public string sourceNodeId
        {
            get => _sourceNodeId;
            set => _sourceNodeId = value;
        }

        public string sourceField
        {
            get => _sourceField;
            set => _sourceField = value;
        }

        public string targetNodeId
        {
            get => _targetNodeId;
            set => _targetNodeId = value;
        }
    }
}
