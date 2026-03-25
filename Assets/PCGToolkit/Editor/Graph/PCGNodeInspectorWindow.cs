using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PCGToolkit.Core;
using PCGCore = PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 独立的节点参数 Inspector 面板（对标 Houdini 的 Parameter Editor）。
    /// 选中节点时显示完整参数列表，支持大尺寸控件、分组折叠、帮助文本。
    /// </summary>
    public class PCGNodeInspectorWindow : EditorWindow
    {
        private PCGNodeVisual _currentNode;
        private PCGGraphView _graphView;
        
        // UI 元素
        private VisualElement _headerContainer;
        private Label _nodeNameLabel;
        private Label _nodeDescLabel;
        private Label _nodeCategoryLabel;
        private VisualElement _paramContainer;
        private ScrollView _paramScrollView;
        private VisualElement _statsContainer;
        private Label _executionTimeLabel;
        private VisualElement _geometryDebugContainer;
        private PCGGeometry _lastGeometry;
        
        // 参数控件映射（用于双向同步）
        private Dictionary<string, VisualElement> _paramWidgets = new();
        
        [MenuItem("PCG Toolkit/Node Inspector")]
        public static PCGNodeInspectorWindow Open()
        {
            var window = GetWindow<PCGNodeInspectorWindow>();
            window.titleContent = new GUIContent("PCG Inspector");
            window.minSize = new Vector2(300, 400);
            return window;
        }

        public void BindGraphView(PCGGraphView graphView)
        {
            _graphView = graphView;
        }

        private void OnEnable()
        {
            BuildUI();
            ShowEmpty();
            // Inspector 重新打开时，尝试从已存在的 EditorWindow 重新获取 graphView
            TryRebindGraphView();
        }

        private void TryRebindGraphView()
        {
            if (_graphView != null) return;
            // HasOpenInstances 检查 EditorWindow 是否已打开（Unity 2020.1+）
            var editorWindow = GetWindow<PCGGraphEditorWindow>(false, null, false);
            if (editorWindow != null)
                editorWindow.BindInspector(this);
        }

        /// <summary>
        /// 强制重建当前节点 Inspector（不检查 _currentNode 是否相同）
        /// </summary>
        public void ForceRebuildCurrentNode()
        {
            if (_currentNode == null) return;
            RebuildForNode(_currentNode);
        }

        /// <summary>
        /// 由 PCGGraphEditorWindow 在选中节点变化时调用
        /// </summary>
        public void InspectNode(PCGNodeVisual nodeVisual)
        {
            if (nodeVisual == _currentNode) return;
            _currentNode = nodeVisual;
            
            if (nodeVisual == null)
            {
                ShowEmpty();
                return;
            }
            
            RebuildForNode(nodeVisual);
        }

        /// <summary>
        /// 更新执行结果信息（执行完成后调用）
        /// </summary>
        public void UpdateExecutionInfo(double elapsedMs, PCGGeometry geometry)
        {
            _lastGeometry = geometry;

            if (_executionTimeLabel != null)
                _executionTimeLabel.text = $"Execution: {elapsedMs:F2}ms";

            if (_geometryDebugContainer != null)
            {
                BuildGeometryDebugPanel(geometry);
            }
        }

        /// <summary>
        /// A2: 构建Geometry Debug面板
        /// </summary>
        private void BuildGeometryDebugPanel(PCGGeometry geo)
        {
            _geometryDebugContainer.Clear();

            if (geo == null)
            {
                _geometryDebugContainer.Add(new Label("Geometry: null")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)) }
                });
                return;
            }

            // Topology 折叠面板
            var topologyFoldout = new Foldout { text = "Topology", value = true };
            topologyFoldout.style.marginBottom = 4;
            topologyFoldout.Add(new Label($"Points: {geo.Points.Count}")
                { style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)) } });
            topologyFoldout.Add(new Label($"Primitives: {geo.Primitives.Count}")
                { style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)) } });
            topologyFoldout.Add(new Label($"Edges: {geo.Edges.Count}")
                { style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)) } });

            int totalVerts = 0;
            foreach (var prim in geo.Primitives) totalVerts += prim.Length;
            topologyFoldout.Add(new Label($"Total Vertices: {totalVerts}")
                { style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)) } });
            _geometryDebugContainer.Add(topologyFoldout);

            // Point Attributes 折叠面板
            BuildAttribFoldout(_geometryDebugContainer, "Point Attributes",
                geo.PointAttribs, geo.Points.Count, isError: false);

            // Vertex Attributes 折叠面板
            BuildAttribFoldout(_geometryDebugContainer, "Vertex Attributes",
                geo.VertexAttribs, totalVerts, isError: false);

            // Primitive Attributes 折叠面板
            BuildAttribFoldout(_geometryDebugContainer, "Primitive Attributes",
                geo.PrimAttribs, geo.Primitives.Count, isError: true);

            // Detail Attributes 折叠面板
            BuildAttribFoldout(_geometryDebugContainer, "Detail Attributes",
                geo.DetailAttribs, 1, isError: true);

            // Point Groups 折叠面板
            BuildGroupsFoldout(_geometryDebugContainer, "Point Groups",
                geo.PointGroups, isError: false);

            // Prim Groups 折叠面板
            BuildGroupsFoldout(_geometryDebugContainer, "Prim Groups",
                geo.PrimGroups, isError: true);

            // Validation 折叠面板
            BuildValidationFoldout(_geometryDebugContainer, geo);
        }

        private void BuildAttribFoldout(VisualElement parent, string title,
            AttributeStore attribStore, int expectedCount, bool isError)
        {
            var foldout = new Foldout { text = title, value = false };
            foldout.style.marginBottom = 4;

            var attrs = attribStore.GetAllAttributes();
            if (!attrs.Any())
            {
                foldout.Add(new Label("(none)")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)) }
                });
            }
            else
            {
                foreach (var attr in attrs)
                {
                    bool hasError = attr.Values.Count != expectedCount;
                    var attrColor = hasError
                        ? new StyleColor(new Color(1f, 0.3f, 0.3f))
                        : new StyleColor(new Color(0.7f, 0.7f, 0.7f));

                    var attrRow = new VisualElement
                    {
                        style = { flexDirection = FlexDirection.Row, marginBottom = 2 }
                    };

                    var nameLabel = new Label($"{attr.Name}:")
                    {
                        style = { fontSize = 10, color = attrColor, width = 80 }
                    };
                    attrRow.Add(nameLabel);

                    var countLabel = new Label($"{attr.Type} [{attr.Values.Count}/{expectedCount}]")
                    {
                        style = { fontSize = 10, color = attrColor }
                    };
                    attrRow.Add(countLabel);

                    foldout.Add(attrRow);

                    // 显示前20条Values的具体值（可展开）
                    if (attr.Values.Count > 0)
                    {
                        var valuesFoldout = new Foldout
                        {
                            text = $"Values ({System.Math.Min(attr.Values.Count, 20)})",
                            value = false
                        };
                        valuesFoldout.style.marginLeft = 10;

                        int displayCount = System.Math.Min(attr.Values.Count, 20);
                        for (int i = 0; i < displayCount; i++)
                        {
                            var valLabel = new Label($"[{i}]: {TruncateValue(attr.Values[i])}")
                            {
                                style = { fontSize = 9, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)) }
                            };
                            valuesFoldout.Add(valLabel);
                        }
                        if (attr.Values.Count > 20)
                        {
                            valuesFoldout.Add(new Label($"... and {attr.Values.Count - 20} more")
                            {
                                style = { fontSize = 9, color = new StyleColor(new Color(0.4f, 0.4f, 0.4f)) }
                            });
                        }
                        foldout.Add(valuesFoldout);
                    }
                }
            }

            parent.Add(foldout);
        }

        private void BuildGroupsFoldout(VisualElement parent, string title,
            Dictionary<string, HashSet<int>> groups, bool isError)
        {
            var foldout = new Foldout { text = title, value = false };
            foldout.style.marginBottom = 4;

            if (groups.Count == 0)
            {
                foldout.Add(new Label("(none)")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)) }
                });
            }
            else
            {
                foreach (var kvp in groups)
                {
                    var groupFoldout = new Foldout
                    {
                        text = $"{kvp.Key} ({kvp.Value.Count})",
                        value = false
                    };

                    // 显示前50个索引
                    var indicesList = new List<int>(kvp.Value);
                    indicesList.Sort();
                    int displayCount = System.Math.Min(indicesList.Count, 50);
                    var indicesStr = string.Join(", ",
                        indicesList.Take(displayCount).Select(i => i.ToString()));
                    if (indicesList.Count > 50)
                        indicesStr += ", ...";

                    groupFoldout.Add(new Label(indicesStr)
                    {
                        style = { fontSize = 9, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), whiteSpace = WhiteSpace.Normal }
                    });

                    foldout.Add(groupFoldout);
                }
            }

            parent.Add(foldout);
        }

        private void BuildValidationFoldout(VisualElement parent, PCGGeometry geo)
        {
            var foldout = new Foldout { text = "Validation", value = false };
            foldout.style.marginBottom = 4;

            var messages = GeometryValidator.Validate(geo);

            if (messages.Count == 0)
            {
                foldout.Add(new Label("No issues found")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.3f, 0.9f, 0.3f)) }
                });
            }
            else
            {
                foreach (var msg in messages)
                {
                    var color = msg.Severity == GeometryValidator.Severity.Error
                        ? new StyleColor(new Color(1f, 0.3f, 0.3f))
                        : new StyleColor(new Color(1f, 0.9f, 0.3f));

                    var prefix = msg.Severity == GeometryValidator.Severity.Error ? "[Error]" : "[Warning]";
                    foldout.Add(new Label($"{prefix} {msg.Message}")
                    {
                        style = { fontSize = 9, color = color, whiteSpace = WhiteSpace.Normal }
                    });
                }
            }

            parent.Add(foldout);
        }

        private string TruncateValue(object value)
        {
            if (value == null) return "null";
            var str = value.ToString();
            if (str.Length > 30)
                return str.Substring(0, 30) + "...";
            return str;
        }

        // ---- UI 构建 ----

        private void BuildUI()
        {
            var root = rootVisualElement;
            root.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            
            // Header
            _headerContainer = new VisualElement
            {
                style =
                {
                    paddingLeft = 12, paddingRight = 12,
                    paddingTop = 8, paddingBottom = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.35f, 0.35f, 0.35f)),
                }
            };
            
            _nodeNameLabel = new Label("No Selection")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(Color.white),
                }
            };
            _headerContainer.Add(_nodeNameLabel);
            
            _nodeCategoryLabel = new Label("")
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(0.6f, 0.8f, 0.6f)),
                    marginTop = 2,
                }
            };
            _headerContainer.Add(_nodeCategoryLabel);
            
            _nodeDescLabel = new Label("")
            {
                style =
                {
                    fontSize = 11,
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)),
                    marginTop = 4,
                    whiteSpace = WhiteSpace.Normal,
                }
            };
            _headerContainer.Add(_nodeDescLabel);
            
            root.Add(_headerContainer);
            
            // Parameter ScrollView
            _paramScrollView = new ScrollView
            {
                style = { flexGrow = 1 }
            };
            _paramContainer = new VisualElement
            {
                style =
                {
                    paddingLeft = 12, paddingRight = 12,
                    paddingTop = 8, paddingBottom = 8,
                }
            };
            _paramScrollView.Add(_paramContainer);
            root.Add(_paramScrollView);
            
            // Stats Footer
            _statsContainer = new VisualElement
            {
                style =
                {
                    paddingLeft = 12, paddingRight = 12,
                    paddingTop = 6, paddingBottom = 6,
                    borderTopWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.35f, 0.35f, 0.35f)),
                    backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f)),
                }
            };
            
            _executionTimeLabel = new Label("Execution: --")
            {
                style = { fontSize = 10, color = new StyleColor(new Color(0.9f, 0.9f, 0.3f)) }
            };
            _statsContainer.Add(_executionTimeLabel);

            // A2: Geometry Debug 可折叠面板
            _geometryDebugContainer = new VisualElement
            {
                style = { marginTop = 4 }
            };
            _statsContainer.Add(_geometryDebugContainer);

            root.Add(_statsContainer);
        }

        private void ShowEmpty()
        {
            _currentNode = null;
            _lastGeometry = null;
            _nodeNameLabel.text = "No Selection";
            _nodeCategoryLabel.text = "";
            _nodeDescLabel.text = "Select a node in the graph to inspect its parameters.";
            _paramContainer.Clear();
            _paramWidgets.Clear();
            _executionTimeLabel.text = "Execution: --";
            if (_geometryDebugContainer != null)
                _geometryDebugContainer.Clear();
        }

        private void RebuildForNode(PCGNodeVisual nodeVisual)
        {
            var pcgNode = nodeVisual.PCGNode;
            
            // Header
            _nodeNameLabel.text = pcgNode.DisplayName;
            _nodeCategoryLabel.text = $"[{pcgNode.Category}] {pcgNode.Name}";
            _nodeDescLabel.text = pcgNode.Description;
            
            // Parameters
            _paramContainer.Clear();
            _paramWidgets.Clear();
            
            if (pcgNode.Inputs == null || pcgNode.Inputs.Length == 0)
            {
                _paramContainer.Add(new Label("No parameters")
                {
                    style = { color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), fontSize = 11 }
                });
                return;
            }
            
            // 分组：Geometry 输入端口 vs 参数端口
            var geoInputs = pcgNode.Inputs.Where(s => s.PortType == PCGPortType.Geometry || s.PortType == PCGPortType.Any).ToList();
            var paramInputs = pcgNode.Inputs.Where(s => s.PortType != PCGPortType.Geometry && s.PortType != PCGPortType.Any).ToList();
            
            // Geometry 输入信息（只读显示）
            if (geoInputs.Count > 0)
            {
                var geoFoldout = new Foldout { text = "Geometry Inputs", value = true };
                geoFoldout.style.marginBottom = 8;
                
                foreach (var schema in geoInputs)
                {
                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                    var nameLabel = new Label(schema.DisplayName)
                    {
                        style = { width = 120, fontSize = 11, color = new StyleColor(new Color(0.2f, 0.8f, 0.4f)) }
                    };
                    var statusLabel = new Label(nodeVisual.IsPortConnected(schema.Name) ? "Connected" : "Not connected")
                    {
                        style =
                        {
                            fontSize = 10,
                            color = new StyleColor(nodeVisual.IsPortConnected(schema.Name) 
                                ? new Color(0.5f, 0.9f, 0.5f) 
                                : new Color(0.6f, 0.4f, 0.4f))
                        }
                    };
                    row.Add(nameLabel);
                    row.Add(statusLabel);
                    geoFoldout.Add(row);
                }
                
                _paramContainer.Add(geoFoldout);
            }
            
            // 参数编辑
            if (paramInputs.Count > 0)
            {
                var paramFoldout = new Foldout { text = "Parameters", value = true };
                paramFoldout.style.marginBottom = 8;
                
                var currentDefaults = nodeVisual.GetPortDefaultValues();
                
                foreach (var schema in paramInputs)
                {
                    bool isConnected = nodeVisual.IsPortConnected(schema.Name);
                    var paramRow = CreateInspectorParam(schema, currentDefaults, nodeVisual, isConnected);
                    paramFoldout.Add(paramRow);
                }
                
                _paramContainer.Add(paramFoldout);
            }

            // ---- 预设操作区 (A3) ----
            BuildPresetSection(nodeVisual);

            // SceneSelectionInputNode: Selection Tool 入口
            if (pcgNode.Name == "SceneSelectionInput")
                BuildSelectionToolSection(nodeVisual);

            // C2: Output 节点显示单独执行按钮
            if (pcgNode.Category == PCGNodeCategory.Output)
                BuildExportButton(nodeVisual);
        }

        // ---- 预设操作 (A3) ----

        private void BuildPresetSection(PCGNodeVisual nodeVisual)
        {
            string nodeType = nodeVisual.PCGNode.Name;

            var foldout = new Foldout { text = "Presets", value = false };
            foldout.style.marginTop = 4;
            foldout.style.marginBottom = 4;

            // 先建列表容器
            var presetListContainer = new VisualElement();

            // Save Preset 行
            var saveRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            var presetNameField = new TextField { value = "", style = { flexGrow = 1 } };
            presetNameField.tooltip = "Preset name";
            var saveBtn = new Button(() =>
            {
                string name = presetNameField.value.Trim();
                if (string.IsNullOrEmpty(name)) name = "Default";
                PCGPresetManager.SavePreset(nodeType, name, nodeVisual.GetPortDefaultValues());
                RefreshPresetList(presetListContainer, nodeVisual, nodeType);
            }) { text = "Save" };
            saveRow.Add(presetNameField);
            saveRow.Add(saveBtn);
            foldout.Add(saveRow);

            foldout.Add(presetListContainer);
            RefreshPresetList(presetListContainer, nodeVisual, nodeType);

            _paramContainer.Add(foldout);
        }

        private void RefreshPresetList(VisualElement container, PCGNodeVisual nodeVisual, string nodeType)
        {
            container.Clear();

            var presets = PCGPresetManager.GetPresetsForNode(nodeType);
            if (presets == null || presets.Length == 0)
            {
                container.Add(new Label("No saved presets")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)) }
                });
                return;
            }

            foreach (var filePath in presets)
            {
                string presetName = PCGPresetManager.GetPresetName(filePath);
                string capturedPath = filePath;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
                var nameLabel = new Label(presetName) { style = { flexGrow = 1, fontSize = 11 } };
                var loadBtn = new Button(() =>
                {
                    var data = PCGPresetManager.LoadPreset(capturedPath);
                    if (data != null)
                    {
                        nodeVisual.SetPortDefaultValues(data);
                        _graphView?.NotifyGraphChanged();
                        _currentNode = null;
                        RebuildForNode(nodeVisual);
                    }
                }) { text = "Load", style = { width = 50 } };
                row.Add(nameLabel);
                row.Add(loadBtn);
                container.Add(row);
            }
        }

        // Selection Tool 入口（SceneSelectionInputNode 专用）
        private void BuildSelectionToolSection(PCGNodeVisual nodeVisual)
        {
            var foldout = new Foldout { text = "Selection Tool", value = true };
            foldout.style.marginTop = 4;
            foldout.style.marginBottom = 4;

            var openBtn = new Button(() =>
            {
                UnityEditor.EditorTools.ToolManager.SetActiveTool<Tools.PCGSelectionTool>();
                EditorApplication.delayCall += () =>
                {
                    var tool = Tools.PCGSelectionTool.ActiveInstance;
                    if (tool == null) return;

                    var defaults = nodeVisual.GetPortDefaultValues();
                    GameObject targetGo = null;
                    if (defaults.TryGetValue("target", out var targetVal))
                    {
                        if (targetVal is PCGCore.PCGSceneObjectRef sceneRef)
                            targetGo = sceneRef.Resolve();
                        else if (targetVal is GameObject go)
                            targetGo = go;
                    }

                    if (targetGo == null)
                    {
                        Debug.LogWarning("[PCGSelectionTool] No target GameObject set on SceneSelectionInputNode. Please assign a target first.");
                        return;
                    }

                    var mf = targetGo.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                    {
                        Debug.LogWarning($"[PCGSelectionTool] GameObject '{targetGo.name}' has no MeshFilter or mesh is null.");
                        return;
                    }

                    var geo = PCGCore.PCGGeometryToMesh.FromMesh(mf.sharedMesh);

                    bool applyTransform = true;
                    if (defaults.TryGetValue("applyTransform", out var atVal) && atVal is bool at)
                        applyTransform = at;
                    if (applyTransform)
                    {
                        var matrix = targetGo.transform.localToWorldMatrix;
                        for (int i = 0; i < geo.Points.Count; i++)
                            geo.Points[i] = matrix.MultiplyPoint3x4(geo.Points[i]);
                        var normalAttr = geo.PointAttribs.GetAttribute("N");
                        if (normalAttr != null)
                        {
                            var normalMat = matrix.inverse.transpose;
                            for (int i = 0; i < normalAttr.Values.Count; i++)
                            {
                                if (normalAttr.Values[i] is Vector3 n)
                                    normalAttr.Values[i] = normalMat.MultiplyVector(n).normalized;
                            }
                        }
                    }

                    tool.SetGeometry(geo);
                    Debug.Log($"[PCGSelectionTool] Geometry loaded from '{targetGo.name}': {geo.Points.Count} points, {geo.Primitives.Count} prims");
                };
            })
            {
                text = "Open Selection Tool",
                tooltip = "激活 PCG 交互选择工具",
                style = { backgroundColor = new StyleColor(new Color(0.1f, 0.4f, 0.6f)) }
            };
            foldout.Add(openBtn);

            foldout.Add(Tools.PCGSelectionUIHelper.CreateStatsLabel());

            var advElements = Tools.PCGSelectionUIHelper.CreateAdvancedSelectionButtons();
            foldout.Add(advElements.Root);

            _paramContainer.Add(foldout);
        }

        // C2: Output 节点 Export 按钮
        private void BuildExportButton(PCGNodeVisual nodeVisual)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };

            var exportBtn = new Button(() => _graphView?.ExecuteSingleOutputNode(nodeVisual))
            {
                text = "Export This Node",
                tooltip = "立即执行此 Output 节点",
                style = { flexGrow = 1, backgroundColor = new StyleColor(new Color(0.3f, 0.2f, 0.5f)) }
            };
            row.Add(exportBtn);

            // D2: Inject to Scene 按钮（预览用）
            var injectBtn = new Button(() =>
            {
                var geo = _graphView?.GetLastOutputGeometry(nodeVisual.NodeId);
                if (geo != null)
                    PCGScenePreview.InjectToScene(geo, nodeVisual.PCGNode.DisplayName);
                else
                    UnityEngine.Debug.LogWarning("先执行图再注入场景");
            })
            {
                text = "Inject to Scene",
                tooltip = "将上次执行结果临时注入场景（预览用）",
                style = { flexGrow = 1, backgroundColor = new StyleColor(new Color(0.1f, 0.3f, 0.5f)) }
            };
            row.Add(injectBtn);

            _paramContainer.Add(row);
        }

        /// <summary>
        /// 为单个参数创建 Inspector 中的编辑控件（比节点内联版本更大、更完整）
        /// </summary>
        private VisualElement CreateInspectorParam(
            PCGParamSchema schema, 
            Dictionary<string, object> currentValues,
            PCGNodeVisual nodeVisual,
            bool isConnected)
        {
            var container = new VisualElement
            {
                style =
                {
                    marginBottom = 6,
                    paddingBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f)),
                }
            };
            
            // 参数名 + 描述
            var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var nameLabel = new Label(schema.DisplayName)
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(GetPortLabelColor(schema.PortType)),
                    width = 120,
                }
            };
            headerRow.Add(nameLabel);
            
            if (!string.IsNullOrEmpty(schema.Description))
            {
                var descLabel = new Label(schema.Description)
                {
                    style =
                    {
                        fontSize = 9,
                        color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
                        flexGrow = 1,
                        unityTextAlign = TextAnchor.MiddleLeft,
                    }
                };
                headerRow.Add(descLabel);
            }

            // E5: Expose 切换按钮（仅参数端口，非 Geometry/Any）
            if (schema.PortType != PCGPortType.Geometry && schema.PortType != PCGPortType.Any)
            {
                bool isExposed = _graphView != null && _graphView.IsParamExposed(nodeVisual.NodeId, schema.Name);
                var exposeToggle = new Toggle("")
                {
                    value = isExposed,
                    tooltip = "Expose to PCGGraphRunner"
                };
                exposeToggle.style.width = 20;
                exposeToggle.style.marginLeft = 4;
                if (isExposed)
                    exposeToggle.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.3f, 0.8f, 1.0f));
                exposeToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_graphView == null) TryRebindGraphView();
                    _graphView?.SetParamExposed(nodeVisual.NodeId, schema.Name, evt.newValue);
                });
                headerRow.Add(exposeToggle);
            }

            container.Add(headerRow);
            
            // 如果已连接，显示 "Connected" 标签，禁用编辑
            if (isConnected)
            {
                var connectedLabel = new Label("(Connected — value from upstream)")
                {
                    style =
                    {
                        fontSize = 10,
                        color = new StyleColor(new Color(0.5f, 0.7f, 0.5f)),
                        marginTop = 2,
                        unityFontStyleAndWeight = FontStyle.Italic
                    }
                };
                container.Add(connectedLabel);
                return container;
            }
            
            // 创建编辑控件
            currentValues.TryGetValue(schema.Name, out var currentVal);
            var widget = CreateInspectorWidget(schema, currentVal, nodeVisual);
            if (widget != null)
            {
                widget.style.marginTop = 4;
                container.Add(widget);
                _paramWidgets[schema.Name] = widget;
            }
            
            return container;
        }

        private VisualElement CreateInspectorWidget(
            PCGParamSchema schema, object currentValue, PCGNodeVisual nodeVisual)
        {
            // Enum/Dropdown
            if (schema.EnumOptions != null && schema.EnumOptions.Length > 0)
            {
                var currentStr = currentValue as string ?? schema.EnumOptions[0];
                var defaultIndex = System.Array.IndexOf(schema.EnumOptions, currentStr);
                if (defaultIndex < 0) defaultIndex = 0;
                
                var popup = new PopupField<string>(
                    schema.DisplayName, schema.EnumOptions.ToList(), defaultIndex);
                popup.style.flexGrow = 1;
                popup.RegisterValueChangedCallback(evt =>
                {
                    SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                });
                return popup;
            }

            switch (schema.PortType)
            {
                case PCGPortType.Float:
                {
                    var val = currentValue is float f ? f : 0f;
                    
                    if (schema.Min != float.MinValue && schema.Max != float.MaxValue)
                    {
                        // Slider + 数值输入
                        var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                        var slider = new Slider(schema.DisplayName, schema.Min, schema.Max)
                        {
                            value = val,
                            showInputField = true,
                            style = { flexGrow = 1 }
                        };
                        slider.RegisterValueChangedCallback(evt =>
                        {
                            SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                        });
                        row.Add(slider);
                        return row;
                    }
                    else
                    {
                        var field = new FloatField(schema.DisplayName)
                        {
                            value = val,
                            style = { flexGrow = 1 }
                        };
                        field.RegisterValueChangedCallback(evt =>
                        {
                            var v = evt.newValue;
                            if (schema.Min != float.MinValue && v < schema.Min) v = schema.Min;
                            if (schema.Max != float.MaxValue && v > schema.Max) v = schema.Max;
                            if (v != evt.newValue) field.SetValueWithoutNotify(v);
                            SyncValueToNode(nodeVisual, schema.Name, v);
                        });
                        return field;
                    }
                }

                case PCGPortType.Int:
                {
                    var val = currentValue is int i ? i : 0;
                    
                    if (schema.Min != float.MinValue && schema.Max != float.MaxValue)
                    {
                        var slider = new SliderInt(schema.DisplayName, (int)schema.Min, (int)schema.Max)
                        {
                            value = val,
                            showInputField = true,
                            style = { flexGrow = 1 }
                        };
                        slider.RegisterValueChangedCallback(evt =>
                        {
                            SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                        });
                        return slider;
                    }
                    else
                    {
                        var field = new IntegerField(schema.DisplayName)
                        {
                            value = val,
                            style = { flexGrow = 1 }
                        };
                        field.RegisterValueChangedCallback(evt =>
                        {
                            var v = evt.newValue;
                            if (schema.Min != float.MinValue && v < (int)schema.Min) v = (int)schema.Min;
                            if (schema.Max != float.MaxValue && v > (int)schema.Max) v = (int)schema.Max;
                            if (v != evt.newValue) field.SetValueWithoutNotify(v);
                            SyncValueToNode(nodeVisual, schema.Name, v);
                        });
                        return field;
                    }
                }

                case PCGPortType.Bool:
                {
                    var val = currentValue is bool b && b;
                    var toggle = new Toggle(schema.DisplayName)
                    {
                        value = val,
                        style = { flexGrow = 1 }
                    };
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                    });
                    return toggle;
                }

                case PCGPortType.String:
                {
                    var val = currentValue as string ?? "";
                    var field = new TextField(schema.DisplayName)
                    {
                        value = val,
                        multiline = val.Length > 50, // 长文本自动多行
                        style = { flexGrow = 1 }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                    });
                    return field;
                }

                case PCGPortType.Vector3:
                {
                    var val = currentValue is Vector3 v ? v : Vector3.zero;
                    var field = new Vector3Field(schema.DisplayName)
                    {
                        value = val,
                        style = { flexGrow = 1 }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                    });
                    return field;
                }

                case PCGPortType.Color:
                {
                    var val = currentValue is Color c ? c : Color.white;
                    var field = new ColorField(schema.DisplayName)
                    {
                        value = val,
                        showAlpha = true,
                        style = { flexGrow = 1 }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        SyncValueToNode(nodeVisual, schema.Name, evt.newValue);
                    });
                    return field;
                }

                case PCGPortType.SceneObject:
                {
                    var objType = schema.ObjectType ?? typeof(GameObject);
                    GameObject currentGo = null;
                    if (currentValue is PCGCore.PCGSceneObjectRef sceneRef)
                        currentGo = sceneRef.Resolve();
                    else if (currentValue is GameObject go)
                        currentGo = go;

                    var field = new ObjectField(schema.DisplayName)
                    {
                        objectType = objType,
                        allowSceneObjects = schema.AllowSceneObjects,
                        value = currentGo,
                        style = { flexGrow = 1 }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        var newGo = evt.newValue as GameObject;
                        var newRef = newGo != null ? new PCGCore.PCGSceneObjectRef(newGo) : null;
                        SyncValueToNode(nodeVisual, schema.Name, newRef);
                    });
                    return field;
                }
            }

            return null;
        }

        /// <summary>
        /// 将 Inspector 中修改的值同步回节点
        /// </summary>
        private void SyncValueToNode(PCGNodeVisual nodeVisual, string paramName, object value)
        {
            // Undo support: record graph data state before modification
            var editorWindow = GetWindow<PCGGraphEditorWindow>(false, null, false);
            if (editorWindow != null)
            {
                var graphData = editorWindow.GetCurrentGraphDataForUndo();
                if (graphData != null)
                    Undo.RecordObject(graphData, $"Change {paramName}");
            }

            nodeVisual.SetPortDefaultValues(new Dictionary<string, object> { { paramName, value } });
            
            _graphView?.NotifyGraphChanged();
        }

        private Color GetPortLabelColor(PCGPortType portType)
        {
            return portType switch
            {
                PCGPortType.Float       => new Color(0.4f, 0.6f, 1.0f),
                PCGPortType.Int         => new Color(0.3f, 0.9f, 0.9f),
                PCGPortType.Vector3     => new Color(1.0f, 0.8f, 0.2f),
                PCGPortType.String      => new Color(1.0f, 0.4f, 0.6f),
                PCGPortType.Bool        => new Color(0.9f, 0.3f, 0.3f),
                PCGPortType.Color       => Color.white,
                PCGPortType.SceneObject => new Color(1.0f, 0.6f, 0.1f),
                _                       => new Color(0.8f, 0.8f, 0.8f),
            };
        }
    }
}