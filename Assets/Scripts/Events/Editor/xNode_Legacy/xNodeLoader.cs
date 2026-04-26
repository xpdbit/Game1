using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// xNode资源加载器
    /// 负责加载和管理所有EventTreeGraph资产，构建模板缓存
    /// </summary>
    public class xNodeLoader
    {
        private static xNodeLoader _instance;
        private static xNodeLoader Instance => _instance ??= new xNodeLoader();

        /// <summary>
        /// 事件树模板缓存
        /// Key: graphId, Value: EventTreeGraph
        /// </summary>
        private Dictionary<string, EventTreeGraph> _templateCache;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized;

        private xNodeLoader()
        {
            _templateCache = new Dictionary<string, EventTreeGraph>();
        }

        /// <summary>
        /// 初始化加载器，构建模板缓存
        /// </summary>
        public static void Initialize()
        {
            Instance.LoadAllEventTreeGraphs();
        }

        /// <summary>
        /// 加载所有EventTreeGraph资产
        /// </summary>
        private void LoadAllEventTreeGraphs()
        {
            if (_isInitialized)
                return;

            _templateCache.Clear();

            // 从Resources加载所有EventTreeGraph
            var graphs = Resources.LoadAll<EventTreeGraph>("Data/EventTrees");

            if (graphs == null || graphs.Length == 0)
            {
                Debug.LogWarning("[xNodeLoader] 在 'Data/EventTrees' 路径下未找到任何事件树模板");
                _isInitialized = true;
                return;
            }

            foreach (var graph in graphs)
            {
                if (graph == null || string.IsNullOrEmpty(graph.graphId))
                    continue;

                _templateCache[graph.graphId] = graph;
            }

            _isInitialized = true;
            Debug.Log($"[xNodeLoader] 已加载 {_templateCache.Count} 个事件树模板");
        }

        /// <summary>
        /// 根据ID获取事件树模板
        /// </summary>
        public static EventTreeGraph GetTemplate(string graphId)
        {
            if (!Instance._isInitialized)
                Initialize();

            if (Instance._templateCache.TryGetValue(graphId, out var graph))
                return graph;

            Debug.LogWarning($"[xNodeLoader] 未找到事件树模板: {graphId}");
            return null;
        }

        /// <summary>
        /// 获取所有事件树模板
        /// </summary>
        public static IReadOnlyDictionary<string, EventTreeGraph> GetAllTemplates()
        {
            if (!Instance._isInitialized)
                Initialize();

            return Instance._templateCache;
        }

        /// <summary>
        /// 按分类获取事件树模板
        /// </summary>
        public static List<EventTreeGraph> GetTemplatesByCategory(string category)
        {
            if (!Instance._isInitialized)
                Initialize();

            return Instance._templateCache.Values
                .Where(g => g.category == category)
                .ToList();
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public static List<string> GetAllCategories()
        {
            if (!Instance._isInitialized)
                Initialize();

            return Instance._templateCache.Values
                .Select(g => g.category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// 刷新模板缓存（重新加载）
        /// </summary>
        public static void Refresh()
        {
            Instance._isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// 根据节点ID获取节点在图中的引用
        /// </summary>
        public static EventTreeNodeBase GetNodeById(string graphId, string nodeId)
        {
            var graph = GetTemplate(graphId);
            return graph?.GetNodeById(nodeId);
        }

        /// <summary>
        /// 验证事件树模板
        /// </summary>
        public static bool ValidateTemplate(string graphId, out List<string> errors)
        {
            var graph = GetTemplate(graphId);
            if (graph == null)
            {
                errors = new List<string> { $"模板不存在: {graphId}" };
                return false;
            }

            return graph.Validate(out errors);
        }
    }
}
