#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Game1.Editor
{
    /// <summary>
    /// EventTree配置编辑器窗口
    /// 提供可视化编辑EventTree XML配置的功能
    /// </summary>
    public class EventTreeEditorWindow : EditorWindow
    {
        // 窗口菜单路径
        private const string MenuPath = "Window/EventTree Editor";

        // XML配置文件路径
        private const string XmlFileName = "EventTrees.xml";
        private string XmlFilePath => Path.Combine(Application.dataPath, "Resources", "Data", "EventTrees", XmlFileName);

        // 数据
        private List<EventTreeTemplate> _templates = new();
        private EventTreeTemplate _selectedTemplate;
        private EventTreeNode _selectedNode;
        private EventTreeChoice _selectedChoice;

        // 编辑器状态
        private bool _isDirty;
        private Vector2 _leftScrollPos;
        private Vector2 _centerScrollPos;
        private Vector2 _rightScrollPos;
        private string _searchFilter = string.Empty;

        // 展开状态
        private HashSet<string> _expandedNodes = new();

        // GUI样式
        private GUIStyle _nodeStyle;
        private GUIStyle _selectedNodeStyle;
        private GUIStyle _headerStyle;
        private Color _rootNodeColor = new Color(0.2f, 0.8f, 0.4f);
        private Color _choiceNodeColor = new Color(0.4f, 0.6f, 0.9f);
        private Color _endNodeColor = new Color(0.9f, 0.6f, 0.2f);
        private Color _storyNodeColor = new Color(0.7f, 0.5f, 0.8f);

        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            var window = GetWindow<EventTreeEditorWindow>("EventTree Editor");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            LoadTemplates();
            _isDirty = false;
        }

        private void OnDisable()
        {
            if (_isDirty)
            {
                if (EditorUtility.DisplayDialog("未保存的更改",
                    "有未保存的更改，是否保存？", "保存", "放弃"))
                {
                    SaveTemplates();
                }
            }
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginHorizontal();

            // 左侧：模板列表
            DrawLeftPanel();

            // 中心：节点树视图
            DrawCenterPanel();

            // 右侧：属性面板
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            // 底部工具栏
            DrawToolbar();
        }

        private void InitStyles()
        {
            if (_nodeStyle == null)
            {
                _nodeStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 4, 4),
                    margin = new RectOffset(2, 2, 2, 2)
                };

                _selectedNodeStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 4, 4),
                    margin = new RectOffset(2, 2, 2, 2),
                    border = new RectOffset(2, 2, 2, 2)
                };

                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }

        #region Left Panel - Template List

        private void DrawLeftPanel()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(200));

            EditorGUILayout.LabelField("EventTree列表", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 搜索框
            EditorGUI.BeginChangeCheck();
            _searchFilter = EditorGUILayout.TextField("搜索", _searchFilter, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                // 过滤列表
            }
            EditorGUILayout.Space();

            // 模板列表
            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);

            foreach (var template in _templates)
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !template.name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) &&
                    !template.id.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool isSelected = _selectedTemplate == template;
                string label = string.IsNullOrEmpty(template.name) ? template.id : template.name;

                if (GUILayout.Toggle(isSelected, label, GUI.skin.button))
                {
                    if (_selectedTemplate != template)
                    {
                        _selectedTemplate = template;
                        _selectedNode = null;
                        _selectedChoice = null;
                        _expandedNodes.Clear();
                        // 默认展开所有节点
                        foreach (var node in template.nodes.Values)
                        {
                            _expandedNodes.Add(node.id);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 操作按钮
            if (GUILayout.Button("+ 新建模板", GUILayout.Height(30)))
            {
                CreateNewTemplate();
            }

            if (_selectedTemplate != null)
            {
                if (GUILayout.Button("复制模板"))
                {
                    DuplicateTemplate();
                }

                if (GUILayout.Button("删除模板", GUILayout.Height(25)))
                {
                    DeleteTemplate();
                }
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region Center Panel - Tree View

        private void DrawCenterPanel()
        {
            GUILayout.BeginVertical("box");

            if (_selectedTemplate == null)
            {
                EditorGUILayout.LabelField("请选择一个模板进行编辑", EditorStyles.centeredGreyMiniLabel);
                GUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"节点结构: {_selectedTemplate.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 节点操作工具栏
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("展开全部"))
            {
                foreach (var node in _selectedTemplate.nodes.Values)
                {
                    _expandedNodes.Add(node.id);
                }
            }
            if (GUILayout.Button("折叠全部"))
            {
                _expandedNodes.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 树形视图
            _centerScrollPos = EditorGUILayout.BeginScrollView(_centerScrollPos);

            DrawNodeTree(_selectedTemplate.rootNodeId, 0);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 节点操作
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 添加根节点"))
            {
                AddRootNode();
            }
            if (_selectedNode != null)
            {
                if (GUILayout.Button("+ 添加子节点"))
                {
                    AddChildNode();
                }
                if (GUILayout.Button("删除节点"))
                {
                    DeleteSelectedNode();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawNodeTree(string nodeId, int indentLevel)
        {
            if (string.IsNullOrEmpty(nodeId) || !_selectedTemplate.nodes.TryGetValue(nodeId, out var node))
                return;

            EditorGUILayout.BeginHorizontal();

            // 缩进
            GUILayout.Space(indentLevel * 20);

            // 展开/折叠按钮
            bool hasChildren = node.choices != null && node.choices.Count > 0 &&
                              !string.IsNullOrEmpty(node.choices[0].nextNodeId);
            bool isExpanded = _expandedNodes.Contains(nodeId);

            if (hasChildren)
            {
                if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    if (isExpanded)
                        _expandedNodes.Remove(nodeId);
                    else
                        _expandedNodes.Add(nodeId);
                }
            }
            else
            {
                GUILayout.Space(22);
            }

            // 节点类型标签
            Color nodeColor = GetNodeColor(node.type);
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = nodeColor;

            string nodeLabel = $"[{node.type}] {node.title}";
            bool isSelected = _selectedNode == node;

            if (GUILayout.Button(nodeLabel, isSelected ? _selectedNodeStyle : _nodeStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                _selectedNode = node;
                _selectedChoice = null;
            }

            GUI.backgroundColor = prevColor;

            EditorGUILayout.EndHorizontal();

            // 绘制子节点
            if (isExpanded && hasChildren)
            {
                foreach (var choice in node.choices)
                {
                    if (!string.IsNullOrEmpty(choice.nextNodeId))
                    {
                        DrawChoiceConnection(choice);
                        DrawNodeTree(choice.nextNodeId, indentLevel + 1);
                    }
                }
            }
        }

        private void DrawChoiceConnection(EventTreeChoice choice)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"└─ [{choice.id}]: {choice.text}", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        private Color GetNodeColor(EventTreeNodeType type)
        {
            return type switch
            {
                EventTreeNodeType.Root => _rootNodeColor,
                EventTreeNodeType.Choice => _choiceNodeColor,
                EventTreeNodeType.End => _endNodeColor,
                EventTreeNodeType.Story => _storyNodeColor,
                _ => Color.gray
            };
        }

        #endregion

        #region Right Panel - Properties

        private void DrawRightPanel()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(300));

            if (_selectedTemplate == null)
            {
                EditorGUILayout.LabelField("属性面板", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("请选择模板", EditorStyles.centeredGreyMiniLabel);
                GUILayout.EndVertical();
                return;
            }

            // 模板属性
            EditorGUILayout.LabelField("模板属性", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _selectedTemplate.id = EditorGUILayout.TextField("ID", _selectedTemplate.id);
            _selectedTemplate.name = EditorGUILayout.TextField("名称", _selectedTemplate.name);
            _selectedTemplate.description = EditorGUILayout.TextField("描述", _selectedTemplate.description);
            _selectedTemplate.rootNodeId = EditorGUILayout.TextField("根节点ID", _selectedTemplate.rootNodeId);

            if (EditorGUI.EndChangeCheck())
            {
                _isDirty = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("━━━━━━━━━━━━", GUI.skin.label);
            EditorGUILayout.Space();

            // 节点属性
            if (_selectedNode != null)
            {
                DrawNodeProperties();
            }
            else
            {
                EditorGUILayout.LabelField("节点属性", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("请选择节点", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndVertical();
        }

        private void DrawNodeProperties()
        {
            EditorGUILayout.LabelField("节点属性", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _selectedNode.id = EditorGUILayout.TextField("节点ID", _selectedNode.id);

            // 节点类型
            _selectedNode.type = (EventTreeNodeType)EditorGUILayout.EnumPopup("类型", _selectedNode.type);

            _selectedNode.title = EditorGUILayout.TextField("标题", _selectedNode.title);
            _selectedNode.description = EditorGUILayout.TextField("描述", _selectedNode.description);
            _selectedNode.nextNodeId = EditorGUILayout.TextField("下一节点ID", _selectedNode.nextNodeId);

            EditorGUILayout.Space();

            // Choices编辑
            EditorGUILayout.LabelField("选项列表", EditorStyles.boldLabel);

            if (_selectedNode.choices == null)
                _selectedNode.choices = new List<EventTreeChoice>();

            for (int i = 0; i < _selectedNode.choices.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                DrawChoiceProperties(_selectedNode.choices[i], i);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 添加选项"))
            {
                _selectedNode.choices.Add(new EventTreeChoice
                {
                    id = Guid.NewGuid().ToString(),
                    text = "新选项",
                    nextNodeId = "",
                    effects = new List<Effect>()
                });
                _isDirty = true;
            }

            EditorGUILayout.Space();

            // Rewards编辑
            EditorGUILayout.LabelField("奖励效果", EditorStyles.boldLabel);

            if (_selectedNode.rewards == null)
                _selectedNode.rewards = new List<Effect>();

            for (int i = 0; i < _selectedNode.rewards.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                DrawEffectProperties(_selectedNode.rewards[i], i);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ 添加奖励"))
            {
                _selectedNode.rewards.Add(new Effect
                {
                    type = EffectType.Gold,
                    value = "+0",
                    target = "player"
                });
                _isDirty = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                _isDirty = true;
            }
        }

        private void DrawChoiceProperties(EventTreeChoice choice, int index)
        {
            EditorGUILayout.LabelField($"选项 {index + 1}", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();

            choice.id = EditorGUILayout.TextField("ID", choice.id);
            choice.text = EditorGUILayout.TextField("文本", choice.text);
            choice.nextNodeId = EditorGUILayout.TextField("下一节点", choice.nextNodeId);

            EditorGUILayout.Space(5);

            // 解析后的效果显示（兼容旧格式）
            EditorGUILayout.LabelField("效果 (旧格式)", EditorStyles.miniLabel);

            // goldCost
            int goldCost = 0;
            if (choice.effects != null)
            {
                foreach (var effect in choice.effects)
                {
                    if (effect.type == EffectType.Gold && effect.value.StartsWith("-"))
                    {
                        if (int.TryParse(effect.value.Substring(1), out var gc))
                            goldCost = gc;
                    }
                }
            }
            goldCost = EditorGUILayout.IntField("金币消耗", goldCost);

            // setFlag
            string setFlag = "";
            if (choice.effects != null)
            {
                foreach (var effect in choice.effects)
                {
                    if (effect.type == EffectType.Flag && effect.value.StartsWith("set:"))
                    {
                        setFlag = effect.value.Substring(4);
                    }
                }
            }
            setFlag = EditorGUILayout.TextField("设置标志", setFlag);

            // addModuleIds
            string addModuleId = "";
            if (choice.effects != null)
            {
                foreach (var effect in choice.effects)
                {
                    if (effect.type == EffectType.Module)
                    {
                        addModuleId = effect.value;
                        break;
                    }
                }
            }
            addModuleId = EditorGUILayout.TextField("添加模块ID", addModuleId);

            // 将编辑的值同步回effects
            if (EditorGUI.EndChangeCheck())
            {
                SyncChoiceEffects(choice, goldCost, setFlag, addModuleId);
                _isDirty = true;
            }

            if (GUILayout.Button("删除选项", GUILayout.Height(20)))
            {
                _selectedNode.choices.RemoveAt(index);
                _isDirty = true;
            }
        }

        private void SyncChoiceEffects(EventTreeChoice choice, int goldCost, string setFlag, string addModuleId)
        {
            if (choice.effects == null)
                choice.effects = new List<Effect>();

            // 清除旧格式的效果
            choice.effects.RemoveAll(e => e.type == EffectType.Gold && e.value.StartsWith("-"));
            choice.effects.RemoveAll(e => e.type == EffectType.Flag && e.value.StartsWith("set:"));
            choice.effects.RemoveAll(e => e.type == EffectType.Module);

            // 添加金币效果
            if (goldCost > 0)
            {
                choice.effects.Add(new Effect
                {
                    type = EffectType.Gold,
                    value = "-" + goldCost,
                    target = "player",
                    quantity = 1
                });
            }

            // 添加标志效果
            if (!string.IsNullOrEmpty(setFlag))
            {
                choice.effects.Add(new Effect
                {
                    type = EffectType.Flag,
                    value = "set:" + setFlag,
                    target = "player",
                    quantity = 1
                });
            }

            // 添加模块效果
            if (!string.IsNullOrEmpty(addModuleId))
            {
                choice.effects.Add(new Effect
                {
                    type = EffectType.Module,
                    value = addModuleId,
                    target = "player",
                    quantity = 1
                });
            }
        }

        private void DrawEffectProperties(Effect effect, int index)
        {
            EditorGUI.BeginChangeCheck();

            effect.type = (EffectType)EditorGUILayout.EnumPopup("类型", effect.type);
            effect.value = EditorGUILayout.TextField("值", effect.value);
            effect.target = EditorGUILayout.TextField("目标", effect.target);
            effect.quantity = EditorGUILayout.IntField("数量", effect.quantity);

            if (EditorGUI.EndChangeCheck())
            {
                _isDirty = true;
            }

            if (GUILayout.Button("删除", GUILayout.Height(18)))
            {
                _selectedNode.rewards.RemoveAt(index);
                _isDirty = true;
            }
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal("toolbar");

            GUILayout.Label("文件:", GUILayout.Width(40));

            if (GUILayout.Button("新建", GUILayout.Width(60)))
            {
                CreateNewTemplate();
            }

            if (GUILayout.Button("打开", GUILayout.Width(60)))
            {
                LoadTemplates();
            }

            GUI.enabled = _isDirty;
            if (GUILayout.Button("保存", GUILayout.Width(60)))
            {
                SaveTemplates();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (_isDirty)
            {
                GUILayout.Label("● 有未保存的更改", GUI.skin.label);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("格式化XML", GUILayout.Width(80)))
            {
                FormatXmlFile();
            }

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                LoadTemplates();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Template Operations

        private void LoadTemplates()
        {
            _templates.Clear();

            if (!File.Exists(XmlFilePath))
            {
                Debug.LogWarning($"[EventTreeEditor] XML文件不存在: {XmlFilePath}");
                return;
            }

            try
            {
                var xml = File.ReadAllText(XmlFilePath);
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var treeNodes = doc.SelectNodes("/EventTrees/EventTree");
                if (treeNodes != null)
                {
                    foreach (XmlNode treeNode in treeNodes)
                    {
                        if (treeNode is XmlElement treeElement)
                        {
                            var template = EventTreeTemplate.ParseFromXml(treeElement);
                            _templates.Add(template);
                        }
                    }
                }

                Debug.Log($"[EventTreeEditor] 加载了 {_templates.Count} 个模板");
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventTreeEditor] 加载失败: {ex.Message}");
            }
        }

        private void SaveTemplates()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<EventTrees>");

                foreach (var template in _templates)
                {
                    sb.AppendLine("    <EventTree>");
                    sb.AppendLine($"        <id>{EscapeXml(template.id)}</id>");
                    sb.AppendLine($"        <name>{EscapeXml(template.name)}</name>");
                    sb.AppendLine($"        <description>{EscapeXml(template.description)}</description>");
                    sb.AppendLine($"        <rootNodeId>{EscapeXml(template.rootNodeId)}</rootNodeId>");
                    sb.AppendLine("        <nodes>");

                    foreach (var node in template.nodes.Values)
                    {
                        sb.AppendLine("            <Node>");
                        sb.AppendLine($"                <id>{EscapeXml(node.id)}</id>");
                        sb.AppendLine($"                <type>{node.type}</type>");
                        sb.AppendLine($"                <title>{EscapeXml(node.title)}</title>");
                        sb.AppendLine($"                <description>{EscapeXml(node.description)}</description>");
                        sb.AppendLine($"                <isOptional>false</isOptional>");

                        if (node.choices != null && node.choices.Count > 0)
                        {
                            sb.AppendLine("                <choices>");
                            foreach (var choice in node.choices)
                            {
                                sb.AppendLine("                    <Choice>");
                                sb.AppendLine($"                        <id>{EscapeXml(choice.id)}</id>");
                                sb.AppendLine($"                        <text>{EscapeXml(choice.text)}</text>");
                                sb.AppendLine($"                        <nextNodeId>{EscapeXml(choice.nextNodeId)}</nextNodeId>");

                                // 写入旧格式效果
                                if (choice.effects != null)
                                {
                                    foreach (var effect in choice.effects)
                                    {
                                        if (effect.type == EffectType.Gold && effect.value.StartsWith("-"))
                                        {
                                            if (int.TryParse(effect.value.Substring(1), out var gc))
                                            {
                                                sb.AppendLine($"                        <goldCost>{gc}</goldCost>");
                                            }
                                        }
                                        else if (effect.type == EffectType.Flag && effect.value.StartsWith("set:"))
                                        {
                                            var flagName = effect.value.Substring(4);
                                            sb.AppendLine($"                        <setFlag>{EscapeXml(flagName)}</setFlag>");
                                        }
                                        else if (effect.type == EffectType.Module)
                                        {
                                            sb.AppendLine("                        <addModuleIds>");
                                            sb.AppendLine($"                            <moduleId>{EscapeXml(effect.value)}</moduleId>");
                                            sb.AppendLine("                        </addModuleIds>");
                                        }
                                    }
                                }

                                sb.AppendLine("                    </Choice>");
                            }
                            sb.AppendLine("                </choices>");
                        }

                        if (node.rewards != null && node.rewards.Count > 0)
                        {
                            sb.AppendLine("                <rewards>");
                            foreach (var reward in node.rewards)
                            {
                                sb.AppendLine($"                    <Effect>{EscapeXml(EffectToString(reward))}</Effect>");
                            }
                            sb.AppendLine("                </rewards>");
                        }

                        sb.AppendLine("            </Node>");
                    }

                    sb.AppendLine("        </nodes>");
                    sb.AppendLine("    </EventTree>");
                }

                sb.AppendLine("</EventTrees>");

                // 确保目录存在
                var directory = Path.GetDirectoryName(XmlFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(XmlFilePath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[EventTreeEditor] 保存成功: {XmlFilePath}");

                _isDirty = false;
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventTreeEditor] 保存失败: {ex.Message}");
                EditorUtility.DisplayDialog("保存失败", $"保存XML文件时出错:\n{ex.Message}", "确定");
            }
        }

        private string EffectToString(Effect effect)
        {
            return effect.type switch
            {
                EffectType.Gold => $"gold:{effect.value}",
                EffectType.Item => $"item:{effect.value}:{effect.quantity}",
                EffectType.HP => $"hp:{effect.value}",
                EffectType.Flag => $"flag:{effect.value}",
                EffectType.Module => $"module:{effect.value}",
                EffectType.Combat => $"combat:{effect.value}",
                _ => effect.value
            };
        }

        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private void CreateNewTemplate()
        {
            var template = new EventTreeTemplate
            {
                id = "Core.EventTree.NewEvent",
                name = "新事件树",
                description = "这是一个新创建的事件树",
                rootNodeId = "node_1",
                nodes = new Dictionary<string, EventTreeNode>()
            };

            // 创建根节点
            var rootNode = new EventTreeNode
            {
                id = "node_1",
                type = EventTreeNodeType.Root,
                title = "开始",
                description = "事件树入口",
                choices = new List<EventTreeChoice>()
            };

            template.nodes[rootNode.id] = rootNode;
            _templates.Add(template);
            _selectedTemplate = template;
            _selectedNode = rootNode;
            _expandedNodes.Clear();
            _expandedNodes.Add(rootNode.id);
            _isDirty = true;
        }

        private void DuplicateTemplate()
        {
            if (_selectedTemplate == null) return;

            var newTemplate = new EventTreeTemplate
            {
                id = _selectedTemplate.id + "_copy",
                name = _selectedTemplate.name + " (副本)",
                description = _selectedTemplate.description,
                rootNodeId = _selectedTemplate.rootNodeId,
                nodes = new Dictionary<string, EventTreeNode>()
            };

            // 深拷贝节点
            foreach (var kvp in _selectedTemplate.nodes)
            {
                var newNode = new EventTreeNode
                {
                    id = kvp.Value.id + "_copy",
                    type = kvp.Value.type,
                    title = kvp.Value.title,
                    description = kvp.Value.description,
                    nextNodeId = string.IsNullOrEmpty(kvp.Value.nextNodeId) ? "" : kvp.Value.nextNodeId + "_copy",
                    choices = new List<EventTreeChoice>(),
                    conditions = new List<EventTreeCondition>(kvp.Value.conditions),
                    rewards = new List<Effect>(kvp.Value.rewards),
                    isStartNode = kvp.Value.isStartNode
                };

                foreach (var choice in kvp.Value.choices)
                {
                    newNode.choices.Add(new EventTreeChoice
                    {
                        id = choice.id,
                        text = choice.text,
                        nextNodeId = string.IsNullOrEmpty(choice.nextNodeId) ? "" : choice.nextNodeId + "_copy",
                        effects = new List<Effect>(choice.effects),
                        conditions = new List<EventTreeCondition>(choice.conditions)
                    });
                }

                newTemplate.nodes[newNode.id] = newNode;
            }

            // 更新引用
            newTemplate.rootNodeId = _selectedTemplate.rootNodeId + "_copy";

            _templates.Add(newTemplate);
            _selectedTemplate = newTemplate;
            _isDirty = true;
        }

        private void DeleteTemplate()
        {
            if (_selectedTemplate == null) return;

            if (!EditorUtility.DisplayDialog("确认删除",
                $"确定要删除模板 '{_selectedTemplate.name}' 吗？", "删除", "取消"))
            {
                return;
            }

            _templates.Remove(_selectedTemplate);
            _selectedTemplate = null;
            _selectedNode = null;
            _isDirty = true;
        }

        #endregion

        #region Node Operations

        private void AddRootNode()
        {
            if (_selectedTemplate == null) return;

            string newId = GenerateUniqueNodeId();
            var newNode = new EventTreeNode
            {
                id = newId,
                type = EventTreeNodeType.Root,
                title = "新根节点",
                description = "",
                choices = new List<EventTreeChoice>()
            };

            _selectedTemplate.nodes[newNode.id] = newNode;
            _selectedTemplate.rootNodeId = newId;
            _selectedNode = newNode;
            _expandedNodes.Add(newNode.id);
            _isDirty = true;
        }

        private void AddChildNode()
        {
            if (_selectedTemplate == null || _selectedNode == null) return;

            string newId = GenerateUniqueNodeId();
            var newNode = new EventTreeNode
            {
                id = newId,
                type = EventTreeNodeType.Choice,
                title = "新节点",
                description = "",
                choices = new List<EventTreeChoice>()
            };

            _selectedTemplate.nodes[newNode.id] = newNode;

            // 添加一个指向新节点的选项
            var choice = new EventTreeChoice
            {
                id = "choice_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                text = "前往",
                nextNodeId = newId,
                effects = new List<Effect>()
            };

            if (_selectedNode.choices == null)
                _selectedNode.choices = new List<EventTreeChoice>();

            _selectedNode.choices.Add(choice);

            _selectedNode = newNode;
            _expandedNodes.Add(newNode.id);
            _expandedNodes.Add(_selectedTemplate.nodes[newNode.id].id);
            _isDirty = true;
        }

        private void DeleteSelectedNode()
        {
            if (_selectedTemplate == null || _selectedNode == null) return;

            // 不能删除根节点
            if (_selectedNode.id == _selectedTemplate.rootNodeId)
            {
                EditorUtility.DisplayDialog("提示", "不能删除根节点！", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("确认删除",
                $"确定要删除节点 '{_selectedNode.title}' 吗？\n这将同时删除所有引用此节点的选项。", "删除", "取消"))
            {
                return;
            }

            // 移除所有指向此节点的选项
            foreach (var node in _selectedTemplate.nodes.Values)
            {
                if (node.choices != null)
                {
                    node.choices.RemoveAll(c => c.nextNodeId == _selectedNode.id);
                }
            }

            // 移除节点
            _selectedTemplate.nodes.Remove(_selectedNode.id);

            // 如果当前选中节点被删除，选中根节点
            if (!_selectedTemplate.nodes.ContainsKey(_selectedNode.id))
            {
                _selectedNode = _selectedTemplate.nodes.Values.Count > 0
                    ? _selectedTemplate.nodes[_selectedTemplate.rootNodeId]
                    : null;
            }

            _isDirty = true;
        }

        private string GenerateUniqueNodeId()
        {
            string baseId = "node";
            int counter = 1;

            while (_selectedTemplate.nodes.ContainsKey(baseId + "_" + counter))
            {
                counter++;
            }

            return baseId + "_" + counter;
        }

        #endregion

        #region Utility

        private void FormatXmlFile()
        {
            if (!File.Exists(XmlFilePath))
            {
                EditorUtility.DisplayDialog("提示", "XML文件不存在！", "确定");
                return;
            }

            try
            {
                var doc = new XmlDocument();
                doc.Load(XmlFilePath);

                // 重新保存以格式化
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ",
                    Encoding = Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(XmlFilePath, settings))
                {
                    doc.Save(writer);
                }

                Debug.Log($"[EventTreeEditor] XML已格式化: {XmlFilePath}");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventTreeEditor] 格式化失败: {ex.Message}");
            }
        }

        #endregion
    }
}
#endif