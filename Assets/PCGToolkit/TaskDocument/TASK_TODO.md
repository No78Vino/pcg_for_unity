```json
{
  "meta": {
    "project": "No78Vino/pcg_for_unity",
    "branch": "main",
    "iteration": "v0.6.0",
    "previousVersion": "v0.5.0-alpha",
    "versionFile": "Assets/PCGToolkit/Editor/Core/PCGToolkitVersion.cs",
    "date": "2026-03-27",
    "totalPhases": 3,
    "totalTasks": 12,
    "language": "C#",
    "framework": "Unity Editor (UIElements + EditorApplication.update)"
  },

  "codebaseContext": {
    "entryPoint": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
    "graphView": "Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs",
    "asyncExecutor": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",
    "syncExecutor": "Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs",
    "scenePreview": "Assets/PCGToolkit/Editor/Graph/PCGScenePreview.cs",
    "nodeInterface": "Assets/PCGToolkit/Editor/Core/IPCGNode.cs",
    "nodeBase": "Assets/PCGToolkit/Editor/Core/PCGNodeBase.cs",
    "nodeRegistry": "Assets/PCGToolkit/Editor/Core/PCGNodeRegistry.cs",
    "graphData": "Assets/PCGToolkit/Runtime/PCGGraphData.cs",
    "context": "Assets/PCGToolkit/Editor/Core/PCGContext.cs",
    "cacheManager": "Assets/PCGToolkit/Editor/Core/PCGCacheManager.cs",
    "geometry": "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",
    "graphHelper": "Assets/PCGToolkit/Editor/Core/PCGGraphHelper.cs",
    "errorPanel": "Assets/PCGToolkit/Editor/Graph/PCGErrorPanel.cs",
    "perfPanel": "Assets/PCGToolkit/Editor/Graph/PCGPerformancePanel.cs",
    "breakpointManager": "Assets/PCGToolkit/Editor/Graph/PCGBreakpointManager.cs",
    "skillAdapter": "Assets/PCGToolkit/Editor/Skill/PCGNodeSkillAdapter.cs",
    "skillRegistry": "Assets/PCGToolkit/Editor/Skill/SkillRegistry.cs",
    "skillExecutor": "Assets/PCGToolkit/Editor/Skill/SkillExecutor.cs",
    "skillExporter": "Assets/PCGToolkit/Editor/Skill/SkillSchemaExporter.cs",
    "nodesRoot": "Assets/PCGToolkit/Editor/Nodes/",
    "nodeCategories": [
      { "name": "Create",     "dir": "Nodes/Create/",     "tier": 0, "count": 16 },
      { "name": "Attribute",  "dir": "Nodes/Attribute/",  "tier": 0, "count": 8  },
      { "name": "Geometry",   "dir": "Nodes/Geometry/",   "tier": 1, "count": 20 },
      { "name": "UV",         "dir": "Nodes/UV/",         "tier": 2, "count": 5  },
      { "name": "Distribute", "dir": "Nodes/Distribute/", "tier": 3, "count": 6  },
      { "name": "Curve",      "dir": "Nodes/Curve/",      "tier": 4, "count": 6  },
      { "name": "Deform",     "dir": "Nodes/Deform/",     "tier": 5, "count": 8  },
      { "name": "Topology",   "dir": "Nodes/Topology/",   "tier": 6, "count": 8  },
      { "name": "Procedural", "dir": "Nodes/Procedural/", "tier": 7, "count": 3  },
      { "name": "Output",     "dir": "Nodes/Output/",     "tier": 8, "count": 7  },
      { "name": "Utility",    "dir": "Nodes/Utility/",    "tier": 0, "count": 21 },
      { "name": "Input",      "dir": "Nodes/Input/",      "tier": 0, "count": 3  }
    ],
    "existingPatterns": {
      "eventDriven": "PCGGraphView.OnGraphChanged (Action event)",
      "asyncExecution": "PCGAsyncGraphExecutor uses EditorApplication.update + 3-phase state machine (Highlight→Execute→ShowResult)",
      "topologicalSort": "PCGGraphHelper.TopologicalSort (Kahn algorithm)",
      "nodeInstantiation": "Activator.CreateInstance per execution",
      "cacheStrategy": "L1 memory + L2 disk, SHA256 cache key, Clone on read",
      "scenePreview": "PCGScenePreview.InjectToScene creates temp GameObjects with HideFlags.DontSave"
    }
  },

  "phases": [
    {
      "id": "P1",
      "name": "即时反馈模式 (Live Feedback Mode)",
      "priority": "critical",
      "description": "编辑器新增即时反馈模式：Graph一旦有改动，后台静默运行并在Scene中实时预览结果",
      "tasks": [
        {
          "id": "P1-T1",
          "name": "创建 PCGSilentExecutor",
          "description": "新建静默执行器，跳过UI动画，支持取消和进度回调",
          "dependencies": [],
          "steps": [
            {
              "action": "CREATE_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGSilentExecutor.cs",
              "namespace": "PCGToolkit.Graph",
              "className": "PCGSilentExecutor",
              "spec": {
                "fields": [
                  { "name": "_graphData",            "type": "PCGGraphData" },
                  { "name": "_context",              "type": "PCGContext" },
                  { "name": "_sortedNodes",          "type": "List<PCGNodeData>" },
                  { "name": "_currentNodeIndex",     "type": "int" },
                  { "name": "_nodeOutputs",          "type": "Dictionary<string, Dictionary<string, PCGGeometry>>" },
                  { "name": "_nodeResults",          "type": "Dictionary<string, NodeExecutionResult>" },
                  { "name": "_cancellationRequested","type": "volatile bool" },
                  { "name": "_isRunning",            "type": "bool" },
                  { "name": "_totalStopwatch",       "type": "Stopwatch" },
                  { "name": "_nodeStopwatch",        "type": "Stopwatch" },
                  { "name": "_inputEdgeMap",         "type": "Dictionary<string, List<PCGEdgeData>>", "note": "预建邻接表，key=InputNodeId" },
                  { "name": "_nodesPerTick",         "type": "int", "default": 5, "note": "每帧执行的最大节点数" },
                  { "name": "_timeBudgetMs",         "type": "double", "default": 8.0, "note": "每帧时间预算(ms)" }
                ],
                "events": [
                  { "name": "OnProgressChanged",           "signature": "Action<int, int>",    "note": "(completedCount, totalCount)" },
                  { "name": "OnSilentExecutionCompleted",  "signature": "Action<Dictionary<string, NodeExecutionResult>, double>", "note": "(allResults, totalMs)" },
                  { "name": "OnSilentExecutionCancelled",  "signature": "Action" },
                  { "name": "OnSilentExecutionFailed",     "signature": "Action<string>",      "note": "errorMessage" }
                ],
                "methods": [
                  {
                    "name": "Start",
                    "params": "PCGGraphData graphData",
                    "logic": [
                      "如果 _isRunning 则先 Cancel()",
                      "克隆 graphData 避免运行期间被修改: graphData.Clone()",
                      "拓扑排序: PCGGraphHelper.TopologicalSort",
                      "过滤掉 PCGNodeCategory.Output 类别的节点 (通过 PCGNodeRegistry.GetNode(nodeType).Category 判断)",
                      "预建邻接表 _inputEdgeMap: 遍历 Edges, 按 InputNodeId 分组",
                      "重置 _currentNodeIndex=0, _cancellationRequested=false",
                      "创建 PCGContext, _nodeOutputs, _nodeResults",
                      "启动 _totalStopwatch",
                      "注册 EditorApplication.update += Tick",
                      "设置 _isRunning = true"
                    ]
                  },
                  {
                    "name": "Cancel",
                    "params": "",
                    "logic": [
                      "设置 _cancellationRequested = true",
                      "注意: 不立即 Stop, 让 Tick 中自然检测到后退出"
                    ]
                  },
                  {
                    "name": "Stop",
                    "params": "",
                    "logic": [
                      "EditorApplication.update -= Tick",
                      "_isRunning = false",
                      "_totalStopwatch?.Stop()"
                    ]
                  },
                  {
                    "name": "Tick",
                    "params": "",
                    "visibility": "private",
                    "logic": [
                      "如果 _cancellationRequested: Stop(), OnSilentExecutionCancelled?.Invoke(), return",
                      "记录 tickStart = EditorApplication.timeSinceStartup",
                      "循环执行节点 while (_currentNodeIndex < _sortedNodes.Count):",
                      "  检查 _cancellationRequested → break",
                      "  检查已用时间 > _timeBudgetMs → break (留给下一帧)",
                      "  执行当前节点 ExecuteNodeSilent(_sortedNodes[_currentNodeIndex])",
                      "  _currentNodeIndex++",
                      "  OnProgressChanged?.Invoke(_currentNodeIndex, _sortedNodes.Count)",
                      "循环结束后检查是否全部完成 → FinishExecution()"
                    ]
                  },
                  {
                    "name": "ExecuteNodeSilent",
                    "params": "PCGNodeData nodeData",
                    "visibility": "private",
                    "logic": [
                      "复用 PCGAsyncGraphExecutor.ExecuteNodeInternal 的核心逻辑",
                      "关键区别: 使用 _inputEdgeMap[nodeData.NodeId] 替代遍历全部 Edges",
                      "使用 PCGNodeRegistry.GetNode + Activator.CreateInstance (后续 P2-T1 会优化为实例池)",
                      "收集输入几何体、参数、GlobalVariables",
                      "执行 nodeInstance.Execute()",
                      "保存结果到 _nodeOutputs 和 _nodeResults",
                      "不触发任何 UI 事件"
                    ]
                  },
                  {
                    "name": "FinishExecution",
                    "params": "",
                    "visibility": "private",
                    "logic": [
                      "Stop()",
                      "OnSilentExecutionCompleted?.Invoke(_nodeResults, _totalStopwatch.Elapsed.TotalMilliseconds)"
                    ]
                  },
                  {
                    "name": "GetLastTerminalGeometry",
                    "params": "",
                    "returns": "PCGGeometry",
                    "logic": [
                      "从 _sortedNodes 末尾向前找第一个有非空输出的节点",
                      "返回其 OutputGeometry"
                    ]
                  }
                ],
                "properties": [
                  { "name": "IsRunning", "type": "bool", "getter": "_isRunning" },
                  { "name": "Progress",  "type": "float", "getter": "_sortedNodes?.Count > 0 ? (float)_currentNodeIndex / _sortedNodes.Count : 0f" }
                ]
              }
            }
          ]
        },
        {
          "id": "P1-T2",
          "name": "修改 PCGGraphEditorWindow 集成 Live 模式",
          "description": "在编辑器窗口中添加 Live 模式开关、防抖逻辑和静默执行器集成",
          "dependencies": ["P1-T1"],
          "steps": [
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
              "changes": [
                {
                  "location": "class fields (after line 43)",
                  "operation": "ADD",
                  "code_description": "新增字段",
                  "fields": [
                    "private bool _liveMode = false;",
                    "private PCGSilentExecutor _silentExecutor;",
                    "private double _lastChangeTime = 0;",
                    "private bool _pendingSilentRun = false;",
                    "private const double DEBOUNCE_DELAY = 0.4;",
                    "private VisualElement _liveProgressBar;",
                    "private Label _liveProgressLabel;",
                    "private VisualElement _liveProgressFill;"
                  ]
                },
                {
                  "location": "OnEnable() method",
                  "operation": "APPEND",
                  "code_description": "恢复 Live 模式状态并初始化静默执行器",
                  "logic": [
                    "_liveMode = EditorPrefs.GetBool(\"PCGToolkit_LiveMode\", false);",
                    "_silentExecutor = new PCGSilentExecutor();",
                    "绑定 _silentExecutor 事件 (见 P1-T4)"
                  ]
                },
                {
                  "location": "Update() method (line 81-92)",
                  "operation": "APPEND",
                  "code_description": "添加防抖检测逻辑",
                  "logic": [
                    "if (_liveMode && _pendingSilentRun)",
                    "  if (EditorApplication.timeSinceStartup - _lastChangeTime >= DEBOUNCE_DELAY)",
                    "    _pendingSilentRun = false;",
                    "    StartSilentExecution();"
                  ]
                },
                {
                  "location": "ConstructGraphView() method (after _mainContainer.Add(_perfPanel) at line 153)",
                  "operation": "APPEND",
                  "code_description": "创建底部 Live 进度条 UI",
                  "logic": [
                    "创建 _liveProgressBar VisualElement: height=20px, background=rgba(0,0,0,0.6), flexDirection=Row",
                    "创建 _liveProgressFill VisualElement: background=cyan/teal, height=100%, width=0%",
                    "创建 _liveProgressLabel Label: 'Live: Idle', color=white, fontSize=11",
                    "_liveProgressBar.style.display = _liveMode ? DisplayStyle.Flex : DisplayStyle.None",
                    "_mainContainer.Add(_liveProgressBar)"
                  ]
                },
                {
                  "location": "ConstructGraphView() 中 graphView.OnGraphChanged 回调 (line 132-139)",
                  "operation": "APPEND_TO_CALLBACK",
                  "code_description": "在 OnGraphChanged 回调中添加 Live 模式触发逻辑",
                  "logic": [
                    "if (_liveMode)",
                    "  _lastChangeTime = EditorApplication.timeSinceStartup;",
                    "  _pendingSilentRun = true;",
                    "  if (_silentExecutor != null && _silentExecutor.IsRunning)",
                    "    _silentExecutor.Cancel();"
                  ]
                },
                {
                  "location": "GenerateToolbar() method (after _stopButton at line 328)",
                  "operation": "INSERT",
                  "code_description": "添加 Live 模式 Toggle 按钮",
                  "logic": [
                    "var liveToggle = new ToolbarToggle { text = 'Live', value = _liveMode };",
                    "liveToggle.style.backgroundColor = _liveMode ? new Color(0.1f, 0.6f, 0.2f) : default;",
                    "liveToggle.RegisterValueChangedCallback(evt => {",
                    "  _liveMode = evt.newValue;",
                    "  EditorPrefs.SetBool('PCGToolkit_LiveMode', _liveMode);",
                    "  liveToggle.style.backgroundColor = _liveMode ? new Color(0.1f, 0.6f, 0.2f) : default;",
                    "  _liveProgressBar.style.display = _liveMode ? DisplayStyle.Flex : DisplayStyle.None;",
                    "  if (!_liveMode && _silentExecutor.IsRunning) _silentExecutor.Cancel();",
                    "});",
                    "toolbar.Add(liveToggle);"
                  ]
                },
                {
                  "location": "OnExecuteClicked() method",
                  "operation": "PREPEND",
                  "code_description": "手动执行时打断静默运行",
                  "logic": [
                    "if (_silentExecutor != null && _silentExecutor.IsRunning)",
                    "  _silentExecutor.Cancel();"
                  ]
                },
                {
                  "location": "OnDisable() method",
                  "operation": "APPEND",
                  "code_description": "清理静默执行器",
                  "logic": [
                    "if (_silentExecutor != null && _silentExecutor.IsRunning)",
                    "  _silentExecutor.Stop();"
                  ]
                }
              ]
            },
            {
              "action": "ADD_METHOD",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
              "methods": [
                {
                  "name": "StartSilentExecution",
                  "visibility": "private",
                  "logic": [
                    "if (graphView == null) return;",
                    "var data = graphView.SaveToGraphData();",
                    "if (data == null || data.Nodes.Count == 0) return;",
                    "_silentExecutor.Start(data);"
                  ]
                },
                {
                  "name": "OnSilentExecutionProgress",
                  "visibility": "private",
                  "params": "int completed, int total",
                  "logic": [
                    "float pct = (float)completed / total;",
                    "_liveProgressFill.style.width = new Length(pct * 100, LengthUnit.Percent);",
                    "_liveProgressLabel.text = $'Live: {completed}/{total} nodes...';"
                  ]
                },
                {
                  "name": "OnSilentExecutionCompleted",
                  "visibility": "private",
                  "params": "Dictionary<string, NodeExecutionResult> results, double totalMs",
                  "logic": [
                    "_liveProgressLabel.text = $'Live: Done ({totalMs:F1}ms)';",
                    "_liveProgressFill.style.width = new Length(100, LengthUnit.Percent);",
                    "// 2秒后淡出",
                    "schedule.Execute(() => {",
                    "  if (!_silentExecutor.IsRunning)",
                    "    _liveProgressLabel.text = 'Live: Idle';",
                    "    _liveProgressFill.style.width = new Length(0, LengthUnit.Percent);",
                    "}).ExecuteLater(2000);",
                    "// 预览结果到 Scene",
                    "var previewGeo = _silentExecutor.GetLastTerminalGeometry();",
                    "// 如果用户选中了某个节点，优先预览该节点",
                    "var selectedNode = graphView.GetSelectedNodeVisual();",
                    "if (selectedNode != null && results.TryGetValue(selectedNode.NodeId, out var selResult))",
                    "  previewGeo = selResult.OutputGeometry ?? previewGeo;",
                    "if (previewGeo != null)",
                    "  PCGScenePreview.InjectToScene(previewGeo, 'PCG_LivePreview');"
                  ]
                }
              ]
            }
          ]
        },
        {
          "id": "P1-T3",
          "name": "增强 PCGScenePreview 支持 Live 模式",
          "description": "确保 InjectToScene 能正确清理旧的 Live 预览对象",
          "dependencies": [],
          "steps": [
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGScenePreview.cs",
              "changes": [
                {
                  "location": "InjectToScene method (line 57-80)",
                  "operation": "ENHANCE",
                  "code_description": "在清理旧对象前，额外按 label 名查找并销毁场景中残留的同名对象",
                  "logic": [
                    "// 清理场景中同名旧对象 (防止 Live 模式残留)",
                    "var existing = GameObject.Find(label);",
                    "if (existing != null && existing.hideFlags == HideFlags.DontSave)",
                    "  Object.DestroyImmediate(existing);"
                  ]
                }
              ]
            }
          ]
        },
        {
          "id": "P1-T4",
          "name": "事件绑定与互斥逻辑",
          "description": "在 PCGGraphEditorWindow 中绑定静默执行器的所有事件回调",
          "dependencies": ["P1-T1", "P1-T2"],
          "steps": [
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
              "changes": [
                {
                  "location": "OnEnable() 中初始化 _silentExecutor 后",
                  "operation": "ADD",
                  "code_description": "绑定静默执行器事件",
                  "logic": [
                    "_silentExecutor.OnProgressChanged += OnSilentExecutionProgress;",
                    "_silentExecutor.OnSilentExecutionCompleted += OnSilentExecutionCompleted;",
                    "_silentExecutor.OnSilentExecutionCancelled += () => {",
                    "  _liveProgressLabel.text = 'Live: Cancelled';",
                    "};",
                    "_silentExecutor.OnSilentExecutionFailed += (err) => {",
                    "  _liveProgressLabel.text = $'Live: Error - {err}';",
                    "};"
                  ]
                }
              ]
            }
          ]
        }
      ]
    },

    {
      "id": "P2",
      "name": "全面优化 Debug 框架与运行效率",
      "priority": "high",
      "description": "优化执行引擎、缓存系统、Debug 面板和内存管理",
      "tasks": [
        {
          "id": "P2-T1",
          "name": "执行引擎优化",
          "description": "邻接表预建、节点实例池、FastMode",
          "dependencies": [],
          "steps": [
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",
              "changes": [
                {
                  "location": "class fields",
                  "operation": "ADD",
                  "fields": [
                    "private Dictionary<string, List<PCGEdgeData>> _inputEdgeMap;",
                    "public bool FastMode { get; set; } = false;"
                  ]
                },
                {
                  "location": "StartExecution() method (line 174-201)",
                  "operation": "APPEND_AFTER_TOPO_SORT",
                  "code_description": "拓扑排序后预建邻接表",
                  "logic": [
                    "_inputEdgeMap = new Dictionary<string, List<PCGEdgeData>>();",
                    "foreach (var node in _sortedNodes)",
                    "  _inputEdgeMap[node.NodeId] = new List<PCGEdgeData>();",
                    "foreach (var edge in _graphData.Edges)",
                    "  if (_inputEdgeMap.ContainsKey(edge.InputNodeId))",
                    "    _inputEdgeMap[edge.InputNodeId].Add(edge);"
                  ]
                },
                {
                  "location": "Tick() method (line 213-284)",
                  "operation": "MODIFY",
                  "code_description": "FastMode 时跳过 Highlight 和 ShowResult 阶段",
                  "logic": [
                    "if (FastMode) {",
                    "  // 直接执行节点，跳过动画",
                    "  var result = ExecuteNodeInternal(nodeData);",
                    "  _nodeResults[nodeData.NodeId] = result;",
                    "  if (!result.Success) { Stop(); return; }",
                    "  _currentNodeIndex++;",
                    "  if (_currentNodeIndex >= _sortedNodes.Count) FinishExecution();",
                    "  return;",
                    "}",
                    "// 原有 3-phase 逻辑保持不变..."
                  ]
                },
                {
                  "location": "ExecuteNodeInternal() method (line 297-434)",
                  "operation": "MODIFY",
                  "code_description": "使用 _inputEdgeMap 替代遍历全部 Edges",
                  "logic": [
                    "将 foreach (var edge in _graphData.Edges) { if (edge.InputNodeId == nodeData.NodeId) ... }",
                    "替换为 if (_inputEdgeMap.TryGetValue(nodeData.NodeId, out var edges)) foreach (var edge in edges) ...",
                    "共有两处需要替换: 收集输入几何体 和 收集上游 GlobalVariables"
                  ]
                }
              ]
            },
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Core/PCGNodeRegistry.cs",
              "changes": [
                {
                  "location": "class body",
                  "operation": "ADD",
                  "code_description": "新增节点实例池",
                  "methods": [
                    {
                      "name": "GetOrCreateInstance",
                      "params": "string nodeType",
                      "returns": "IPCGNode",
                      "logic": [
                        "private static Dictionary<string, IPCGNode> _instancePool = new Dictionary<string, IPCGNode>();",
                        "EnsureInitialized();",
                        "if (_instancePool.TryGetValue(nodeType, out var cached)) return cached;",
                        "var template = GetNode(nodeType);",
                        "if (template == null) return null;",
                        "var instance = (IPCGNode)Activator.CreateInstance(template.GetType());",
                        "_instancePool[nodeType] = instance;",
                        "return instance;",
                        "注意: 节点 Execute 方法必须是无状态的(当前设计已满足)"
                      ]
                    }
                  ]
                }
              ]
            },
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",
              "changes": [
                {
                  "location": "ExecuteNodeInternal() line 308",
                  "operation": "REPLACE",
                  "from": "var nodeInstance = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());",
                  "to": "var nodeInstance = PCGNodeRegistry.GetOrCreateInstance(nodeData.NodeType) ?? (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());"
                }
              ]
            },
            {
              "action": "MODIFY_FILE",
              "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs",
              "changes": [
                {
                  "location": "ExecuteNode() line 166",
                  "operation": "REPLACE",
                  "from": "var nodeInstance = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());",
                  "to": "var nodeInstance = PCGNodeRegistry.GetOrCreateInstance(nodeData.NodeType) ?? (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());"
                }
              ]
            }
          ]
        },
        {  
          "id": "P2-T2",  
          "name": "Cache 优化",  
          "description": "增量 hash、Copy-on-Write 语义",  
          "dependencies": [],  
          "steps": [  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",  
              "changes": [  
                {  
                  "location": "class fields (after line 31, PrimGroups)",  
                  "operation": "ADD",  
                  "code_description": "新增内容 hash 缓存字段",  
                  "fields": [  
                    "private string _contentHash = null;",  
                    "private bool _hashDirty = true;"  
                  ]  
                },  
                {  
                  "location": "class body",  
                  "operation": "ADD_METHOD",  
                  "methods": [  
                    {  
                      "name": "MarkDirty",  
                      "visibility": "public",  
                      "logic": [  
                        "_hashDirty = true;",  
                        "_contentHash = null;"  
                      ]  
                    },  
                    {  
                      "name": "GetContentHash",  
                      "visibility": "public",  
                      "returns": "string",  
                      "logic": [  
                        "if (!_hashDirty && _contentHash != null) return _contentHash;",  
                        "_contentHash = PCGGeometrySerializer.ComputeHash(this);",  
                        "_hashDirty = false;",  
                        "return _contentHash;"  
                      ]  
                    }  
                  ]  
                },  
                {  
                  "location": "Clone() method (line 36-54)",  
                  "operation": "APPEND",  
                  "code_description": "Clone 后标记新对象 hash dirty",  
                  "logic": [  
                    "clone._hashDirty = true;",  
                    "clone._contentHash = null;"  
                  ]  
                },  
                {  
                  "location": "Clear() method (line 87-98)",  
                  "operation": "APPEND",  
                  "code_description": "Clear 后标记 hash dirty",  
                  "logic": [  
                    "_hashDirty = true;",  
                    "_contentHash = null;"  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Core/PCGCacheManager.cs",  
              "changes": [  
                {  
                  "location": "ComputeCacheKey() method (line 53-87)",  
                  "operation": "MODIFY",  
                  "code_description": "使用 PCGGeometry.GetContentHash() 替代每次全量 ComputeHash",  
                  "logic": [  
                    "将 line 77: sb.Append(PCGGeometrySerializer.ComputeHash(kvp.Value));",  
                    "替换为: sb.Append(kvp.Value?.GetContentHash() ?? \"null\");",  
                    "这样对于未修改的几何体，直接返回缓存的 hash，避免重复计算"  
                  ]  
                },  
                {  
                  "location": "class body (after TryGetGeometry method, line 128)",  
                  "operation": "ADD_METHOD",  
                  "code_description": "新增只读版本的 TryGet，不做 Clone",  
                  "methods": [  
                    {  
                      "name": "TryGetGeometryReadOnly",  
                      "visibility": "public static",  
                      "params": "string cacheKey, out PCGGeometry geo",  
                      "returns": "bool",  
                      "logic": [  
                        "EnsureInitialized();",  
                        "// L1: memory cache — 返回原始引用，不 Clone",  
                        "if (_memoryCache.TryGetValue(cacheKey, out var memEntry))",  
                        "  memEntry.LastAccessed = DateTime.UtcNow;",  
                        "  geo = memEntry.Geometry;",  
                        "  _hitCount++;",  
                        "  return true;",  
                        "// L2: disk cache — 反序列化后存入 L1 并返回引用",  
                        "if (_manifest.TryGetValue(cacheKey, out var entry) && !string.IsNullOrEmpty(entry.DiskFilePath))",  
                        "  var loaded = PCGGeometrySerializer.DeserializeFromFile(entry.DiskFilePath);",  
                        "  if (loaded != null)",  
                        "    entry.LastAccessedAtTicks = DateTime.UtcNow.Ticks;",  
                        "    _memoryCache[cacheKey] = new MemoryCacheEntry { Geometry = loaded, CreatedAt = new DateTime(entry.CreatedAtTicks), LastAccessed = DateTime.UtcNow, SizeEstimate = EstimateGeometrySize(loaded) };",  
                        "    geo = loaded;",  
                        "    _hitCount++;",  
                        "    return true;",  
                        "_missCount++;",  
                        "geo = null;",  
                        "return false;",  
                        "// 注意: 调用方如果需要修改返回的 geo，必须自行 Clone()"  
                      ]  
                    }  
                  ]  
                },  
                {  
                  "location": "TryGetGeometry() method (line 89-128)",  
                  "operation": "ADD_COMMENT",  
                  "code_description": "保留原有 Clone 版本，添加注释说明两个版本的区别",  
                  "logic": [  
                    "/// <summary>",  
                    "/// 获取缓存几何体（Clone 版本，返回独立副本，调用方可安全修改）",  
                    "/// 如果不需要修改，使用 TryGetGeometryReadOnly 避免 Clone 开销",  
                    "/// </summary>"  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",  
              "changes": [  
                {  
                  "location": "ExecuteNodeInternal() cache hit 分支 (line 383-393)",  
                  "operation": "MODIFY",  
                  "code_description": "对于只读节点（Transform 类除外）使用 ReadOnly 版本",  
                  "logic": [  
                    "// 静默运行模式下使用 ReadOnly 版本减少 Clone 开销",  
                    "// 注意: 需要修改几何体的节点（Transform, Noise, Deform 等）仍需在 Execute 内部 Clone",  
                    "if (_context.UseDiskCache && PCGCacheManager.TryGetGeometryReadOnly(cacheKey, out var cachedGeo))",  
                    "  result.Outputs = new Dictionary<string, PCGGeometry> { { \"geometry\", cachedGeo } };",  
                    "  // 后续节点如果需要修改此几何体，应在 Execute 开头 Clone"  
                  ]  
                }  
              ]  
            }  
          ]  
        },  
        {  
          "id": "P2-T3",  
          "name": "Debug 框架增强",  
          "description": "ErrorPanel 过滤/搜索、PerformancePanel 火焰图、SceneView Debug Overlay",  
          "dependencies": [],  
          "steps": [  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Graph/PCGErrorPanel.cs",  
              "changes": [  
                {  
                  "location": "class fields (after line 15)",  
                  "operation": "ADD",  
                  "fields": [  
                    "private bool _showWarnings = true;",  
                    "private bool _showErrors = true;",  
                    "private string _searchText = \"\";",  
                    "private ToolbarToggle _warningToggle;",  
                    "private ToolbarToggle _errorToggle;",  
                    "private TextField _searchField;"  
                  ]  
                },  
                {  
                  "location": "constructor, after titleLabel (line 48)",  
                  "operation": "INSERT",  
                  "code_description": "在标题栏中添加过滤 Toggle 和搜索框",  
                  "logic": [  
                    "// Warning 过滤 Toggle",  
                    "_warningToggle = new ToolbarToggle { text = '⚠ Warn', value = true };",  
                    "_warningToggle.style.width = 60;",  
                    "_warningToggle.RegisterValueChangedCallback(evt => { _showWarnings = evt.newValue; RefreshFilteredView(); });",  
                    "header.Add(_warningToggle);",  
                    "",  
                    "// Error 过滤 Toggle",  
                    "_errorToggle = new ToolbarToggle { text = '✖ Error', value = true };",  
                    "_errorToggle.style.width = 60;",  
                    "_errorToggle.RegisterValueChangedCallback(evt => { _showErrors = evt.newValue; RefreshFilteredView(); });",  
                    "header.Add(_errorToggle);",  
                    "",  
                    "// 搜索框",  
                    "_searchField = new TextField { value = '' };",  
                    "_searchField.style.width = 150;",  
                    "_searchField.style.height = 18;",  
                    "_searchField.RegisterValueChangedCallback(evt => { _searchText = evt.newValue; RefreshFilteredView(); });",  
                    "header.Add(_searchField);"  
                  ]  
                },  
                {  
                  "location": "class body (after ClearErrors method)",  
                  "operation": "ADD_METHOD",  
                  "methods": [  
                    {  
                      "name": "RefreshFilteredView",  
                      "visibility": "private",  
                      "logic": [  
                        "_scrollView.Clear();",  
                        "foreach (var entry in _errors)",  
                        "  // 过滤: Warning/Error Toggle",  
                        "  if (entry.IsWarning && !_showWarnings) continue;",  
                        "  if (!entry.IsWarning && !_showErrors) continue;",  
                        "  // 过滤: 搜索文本",  
                        "  if (!string.IsNullOrEmpty(_searchText))",  
                        "    if (!entry.NodeName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) &&",  
                        "        !entry.Message.Contains(_searchText, StringComparison.OrdinalIgnoreCase))",  
                        "      continue;",  
                        "  _scrollView.Add(CreateErrorElement(entry));"  
                      ]  
                    }  
                  ]  
                },  
                {  
                  "location": "PCGErrorEntry class (line 172-186)",  
                  "operation": "ADD_FIELD",  
                  "code_description": "新增 Fatal 级别支持",  
                  "fields": [  
                    "public enum ErrorLevel { Warning, Error, Fatal }",  
                    "public ErrorLevel Level;"  
                  ],  
                  "note": "将 IsWarning bool 迁移为 ErrorLevel enum，保持向后兼容: IsWarning => Level == Warning"  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Graph/PCGPerformancePanel.cs",  
              "changes": [  
                {  
                  "location": "class fields (after line 24)",  
                  "operation": "ADD",  
                  "fields": [  
                    "private enum ViewMode { Table, FlameChart }",  
                    "private ViewMode _viewMode = ViewMode.Table;",  
                    "private VisualElement _flameChartContainer;",  
                    "private Button _tableButton;",  
                    "private Button _flameButton;"  
                  ]  
                },  
                {  
                  "location": "constructor, after titleLabel (line 55)",  
                  "operation": "INSERT",  
                  "code_description": "添加 Table/FlameChart 切换按钮",  
                  "logic": [  
                    "_tableButton = new Button(() => SwitchView(ViewMode.Table)) { text = 'Table' };",  
                    "_tableButton.style.width = 50; _tableButton.style.height = 18; _tableButton.style.fontSize = 10;",  
                    "header.Add(_tableButton);",  
                    "",  
                    "_flameButton = new Button(() => SwitchView(ViewMode.FlameChart)) { text = 'Flame' };",  
                    "_flameButton.style.width = 50; _flameButton.style.height = 18; _flameButton.style.fontSize = 10;",  
                    "header.Add(_flameButton);"  
                  ]  
                },  
                {  
                  "location": "class body",  
                  "operation": "ADD_METHODS",  
                  "methods": [  
                    {  
                      "name": "SwitchView",  
                      "params": "ViewMode mode",  
                      "visibility": "private",  
                      "logic": [  
                        "_viewMode = mode;",  
                        "// 隐藏/显示对应容器",  
                        "_listContainer.parent.style.display = mode == ViewMode.Table ? DisplayStyle.Flex : DisplayStyle.None;",  
                        "_flameChartContainer.style.display = mode == ViewMode.FlameChart ? DisplayStyle.Flex : DisplayStyle.None;",  
                        "if (mode == ViewMode.FlameChart) RenderFlameChart();"  
                      ]  
                    },  
                    {  
                      "name": "RenderFlameChart",  
                      "visibility": "private",  
                      "logic": [  
                        "_flameChartContainer.Clear();",  
                        "if (_entries.Count == 0) return;",  
                        "",  
                        "double totalMs = _entries.Sum(e => e.ElapsedMs);",  
                        "if (totalMs <= 0) totalMs = 1;",  
                        "",  
                        "// 按拓扑顺序排列（保持原始顺序，不按耗时排序）",  
                        "var orderedEntries = new List<NodePerfEntry>(_entries);",  
                        "// 注意: _entries 在 CollectFromExecutor 中已按耗时排序，火焰图需要按拓扑顺序",  
                        "// 需要保存原始拓扑顺序的副本",  
                        "",  
                        "foreach (var entry in orderedEntries)",  
                        "  float widthPct = (float)(entry.ElapsedMs / totalMs) * 100f;",  
                        "  if (widthPct < 0.5f) widthPct = 0.5f; // 最小可见宽度",  
                        "",  
                        "  var block = new VisualElement();",  
                        "  block.style.width = new Length(widthPct, LengthUnit.Percent);",  
                        "  block.style.height = 24;",  
                        "  block.style.backgroundColor = GetHeatColor(entry.ElapsedMs);",  
                        "  block.style.borderRightWidth = 1;",  
                        "  block.style.borderRightColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));",  
                        "",  
                        "  var label = new Label(entry.NodeType);",  
                        "  label.style.fontSize = 9;",  
                        "  label.style.overflow = Overflow.Hidden;",  
                        "  label.style.color = new StyleColor(Color.white);",  
                        "  block.Add(label);",  
                        "",  
                        "  block.tooltip = $'{entry.NodeType}\\n{entry.ElapsedMs:F2}ms\\nPts: {entry.OutputPoints}\\nPrims: {entry.OutputPrims}';",  
                        "  _flameChartContainer.Add(block);"  
                      ]  
                    },  
                    {  
                      "name": "GetHeatColor",  
                      "params": "double elapsedMs",  
                      "returns": "StyleColor",  
                      "visibility": "private static",  
                      "logic": [  
                        "// 绿(快) → 黄(中) → 红(慢)",  
                        "if (elapsedMs > 50) return new StyleColor(new Color(0.8f, 0.2f, 0.2f, 0.9f));",  
                        "if (elapsedMs > 10) return new StyleColor(new Color(0.8f, 0.6f, 0.1f, 0.9f));",  
                        "return new StyleColor(new Color(0.2f, 0.6f, 0.3f, 0.9f));"  
                      ]  
                    }  
                  ]  
                },  
                {  
                  "location": "constructor, after scrollView (line 91)",  
                  "operation": "INSERT",  
                  "code_description": "创建火焰图容器",  
                  "logic": [  
                    "_flameChartContainer = new VisualElement();",  
                    "_flameChartContainer.style.flexDirection = FlexDirection.Row;",  
                    "_flameChartContainer.style.flexWrap = Wrap.NoWrap;",  
                    "_flameChartContainer.style.height = 28;",  
                    "_flameChartContainer.style.display = DisplayStyle.None;",  
                    "Add(_flameChartContainer);"  
                  ]  
                },  
                {  
                  "location": "CollectFromExecutor() method (line 94-166)",  
                  "operation": "APPEND",  
                  "code_description": "收集数据后如果当前是火焰图模式则自动刷新",  
                  "logic": [  
                    "if (_viewMode == ViewMode.FlameChart) RenderFlameChart();"  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Graph/PCGDebugOverlay.cs",  
              "namespace": "PCGToolkit.Graph",  
              "className": "PCGDebugOverlay",  
              "spec": {  
                "attributes": ["InitializeOnLoad"],  
                "isStatic": true,  
                "description": "SceneView 左上角叠加显示当前 PCG 执行状态和几何体统计",  
                "fields": [  
                  { "name": "_active",           "type": "static bool",   "default": false },  
                  { "name": "_currentNodeName",  "type": "static string", "default": "\"\"" },  
                  { "name": "_progress",         "type": "static float",  "default": 0 },  
                  { "name": "_totalNodes",       "type": "static int",    "default": 0 },  
                  { "name": "_completedNodes",   "type": "static int",    "default": 0 },  
                  { "name": "_lastGeoStats",     "type": "static string", "default": "\"\"" },  
                  { "name": "_isLiveMode",       "type": "static bool",   "default": false }  
                ],  
                "staticConstructor": [  
                  "SceneView.duringSceneGui += OnSceneGUI;"  
                ],  
                "methods": [  
                  {  
                    "name": "Show",  
                    "visibility": "public static",  
                    "params": "string nodeName, int completed, int total, PCGGeometry currentGeo",  
                    "logic": [  
                      "_active = true;",  
                      "_currentNodeName = nodeName;",  
                      "_completedNodes = completed;",  
                      "_totalNodes = total;",  
                      "_progress = total > 0 ? (float)completed / total : 0;",  
                      "if (currentGeo != null)",  
                      "  _lastGeoStats = $'Pts: {currentGeo.Points.Count:N0} | Prims: {currentGeo.Primitives.Count:N0}';",  
                      "else",  
                      "  _lastGeoStats = '';",  
                      "SceneView.RepaintAll();"  
                    ]  
                  },  
                  {  
                    "name": "Hide",  
                    "visibility": "public static",  
                    "logic": [  
                      "_active = false;",  
                      "SceneView.RepaintAll();"  
                    ]  
                  },  
                  {  
                    "name": "SetLiveMode",  
                    "visibility": "public static",  
                    "params": "bool isLive",  
                    "logic": [ "_isLiveMode = isLive;" ]  
                  },  
                  {  
                    "name": "OnSceneGUI",  
                    "visibility": "private static",  
                    "params": "SceneView sceneView",  
                    "logic": [  
                      "if (!_active) return;",  
                      "Handles.BeginGUI();",  
                      "var rect = new Rect(10, 10, 280, 80);",  
                      "GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);",  
                      "",  
                      "string modeLabel = _isLiveMode ? '[LIVE]' : '[EXEC]';",  
                      "GUI.Label(new Rect(14, 12, 270, 18), $'{modeLabel} {_currentNodeName}', EditorStyles.boldLabel);",  
                      "GUI.Label(new Rect(14, 30, 270, 18), $'Progress: {_completedNodes}/{_totalNodes} ({_progress:P0})');",  
                      "GUI.Label(new Rect(14, 48, 270, 18), _lastGeoStats);",  
                      "",  
                      "// 进度条",  
                      "EditorGUI.ProgressBar(new Rect(14, 66, 262, 12), _progress, '');",  
                      "Handles.EndGUI();"  
                    ]  
                  }  
                ]  
              }  
            }  
          ]  
        },  
        {  
          "id": "P2-T4",  
          "name": "内存优化",  
          "description": "AttributeStore 延迟初始化、几何体大对象减少 GC",  
          "dependencies": [],  
          "steps": [  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Core/AttributeStore.cs",  
              "changes": [  
                {  
                  "location": "line 66: private Dictionary<string, PCGAttribute> _attributes = new Dictionary<string, PCGAttribute>();",  
                  "operation": "REPLACE",  
                  "from": "private Dictionary<string, PCGAttribute> _attributes = new Dictionary<string, PCGAttribute>();",  
                  "to": "private Dictionary<string, PCGAttribute> _attributes = null;",  
                  "note": "延迟初始化，只在首次写入时创建"  
                },  
                {  
                  "location": "class body",  
                  "operation": "ADD_METHOD",  
                  "methods": [  
                    {  
                      "name": "EnsureDict",  
                      "visibility": "private",  
                      "logic": [  
                        "if (_attributes == null) _attributes = new Dictionary<string, PCGAttribute>(4);"  
                      ],  
                      "note": "初始容量 4，大多数节点属性不多"  
                    }  
                  ]  
                },  
                {  
                  "location": "所有写入方法 (CreateAttribute, SetAttribute)",  
                  "operation": "PREPEND",  
                  "code_description": "在写入前调用 EnsureDict()",  
                  "logic": [ "EnsureDict();" ]  
                },  
                {  
                  "location": "所有读取方法 (GetAttribute, HasAttribute, GetAttributeNames, GetAllAttributes, RemoveAttribute)",  
                  "operation": "MODIFY",  
                  "code_description": "读取时检查 null",  
                  "logic": [  
                    "GetAttribute: if (_attributes == null) return null; ...",  
                    "HasAttribute: return _attributes != null && _attributes.ContainsKey(name);",  
                    "GetAttributeNames: return _attributes?.Keys ?? Enumerable.Empty<string>();",  
                    "GetAllAttributes: return _attributes?.Values ?? Enumerable.Empty<PCGAttribute>();",  
                    "RemoveAttribute: return _attributes != null && _attributes.Remove(name);"  
                  ]  
                },  
                {  
                  "location": "Clear() method",  
                  "operation": "REPLACE",  
                  "logic": [ "_attributes = null;  // 释放引用，下次写入时重新创建" ]  
                },  
                {  
                  "location": "Clone() method",  
                  "operation": "MODIFY",  
                  "logic": [  
                    "if (_attributes == null) return new AttributeStore();",  
                    "var clone = new AttributeStore();",  
                    "clone._attributes = new Dictionary<string, PCGAttribute>(_attributes.Count);",  
                    "foreach (var kvp in _attributes)",  
                    "  clone._attributes[kvp.Key] = kvp.Value.Clone();",  
                    "return clone;"  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",  
              "changes": [  
                {  
                  "location": "class fields (line 14-31)",  
                  "operation": "ADD_COMMENT",  
                  "code_description": "为未来的 ListPool 优化预留注释",  
                  "logic": [  
                    "// TODO P2-T4: 考虑使用 UnityEngine.Pool.ListPool<Vector3>.Get() / Release()",  
                    "// 对于频繁创建/销毁的 PCGGeometry（如 Live 模式每次静默运行），",  
                    "// 可以显著减少 GC 压力。当前暂不实施，待 Live 模式上线后根据 Profiler 数据决定。"  
                  ]  
                }  
              ]  
            }  
          ]  
        },  
        {  
          "id": "P2-T5",  
          "name": "增量执行优化",  
          "description": "dirty 子图局部拓扑排序",  
          "dependencies": [],  
          "steps": [  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Core/PCGGraphHelper.cs",  
              "changes": [  
                {  
                  "location": "class body (after TopologicalSort method, line 66)",  
                  "operation": "ADD_METHOD",  
                  "methods": [  
                    {  
                      "name": "TopologicalSortSubgraph",  
                      "visibility": "public static",  
                      "params": "PCGGraphData graphData, HashSet<string> subgraphNodeIds",  
                      "returns": "List<PCGNodeData>",  
                      "logic": [  
                        "// 只对 subgraphNodeIds 中的节点做拓扑排序",  
                        "var nodeMap = new Dictionary<string, PCGNodeData>();",  
                        "var inDegree = new Dictionary<string, int>();",  
                        "var adjacency = new Dictionary<string, List<string>>();",  
                        "",  
                        "foreach (var node in graphData.Nodes)",  
                        "  if (!subgraphNodeIds.Contains(node.NodeId)) continue;",  
                        "  nodeMap[node.NodeId] = node;",  
                        "  inDegree[node.NodeId] = 0;",  
                        "  adjacency[node.NodeId] = new List<string>();",  
                        "",  
                        "foreach (var edge in graphData.Edges)",  
                        "  if (subgraphNodeIds.Contains(edge.OutputNodeId) && subgraphNodeIds.Contains(edge.InputNodeId))",  
                        "    adjacency[edge.OutputNodeId].Add(edge.InputNodeId);",  
                        "    inDegree[edge.InputNodeId]++;",  
                        "",  
                        "// Kahn 算法（同 TopologicalSort）",  
                        "var queue = new Queue<string>();",  
                        "foreach (var kvp in inDegree)",  
                        "  if (kvp.Value == 0) queue.Enqueue(kvp.Key);",  
                        "",  
                        "var sorted = new List<PCGNodeData>();",  
                        "while (queue.Count > 0)",  
                        "  var nodeId = queue.Dequeue();",  
                        "  sorted.Add(nodeMap[nodeId]);",  
                        "  foreach (var neighbor in adjacency[nodeId])",  
                        "    inDegree[neighbor]--;",  
                        "    if (inDegree[neighbor] == 0) queue.Enqueue(neighbor);",  
                        "",  
                        "return sorted;"  
                      ]  
                    }  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs",  
              "changes": [  
                {  
                  "location": "ExecuteIncremental() method (line 112-143)",  
                  "operation": "REPLACE",  
                  "code_description": "使用子图拓扑排序替代全图排序",  
                  "logic": [  
                    "public void ExecuteIncremental()",  
                    "{",  
                    "  if (dirtyNodes.Count == 0) return;",  
                    "",  
                    "  // 从 dirty nodes 出发，沿下游传播，收集所有需要重新执行的节点",  
                    "  var toExecute = new HashSet<string>(dirtyNodes);",  
                    "  var queue = new Queue<string>(dirtyNodes);",  
                    "  while (queue.Count > 0)",  
                    "  {",  
                    "    var nodeId = queue.Dequeue();",  
                    "    foreach (var edge in graphData.Edges)",  
                    "    {",  
                    "      if (edge.OutputNodeId == nodeId && !toExecute.Contains(edge.InputNodeId))",  
                    "      {",  
                    "        toExecute.Add(edge.InputNodeId);",  
                    "        queue.Enqueue(edge.InputNodeId);",  
                    "      }",  
                    "    }",  
                    "  }",  
                    "",  
                    "  // 优化: 只对 dirty 子图做拓扑排序，而非全图",  
                    "  var sortedSubgraph = PCGGraphHelper.TopologicalSortSubgraph(graphData, toExecute);",  
                    "  if (sortedSubgraph == null || sortedSubgraph.Count == 0) return;",  
                    "",  
                    "  foreach (var nodeData in sortedSubgraph)",  
                    "  {",  
                    "    ExecuteNode(nodeData);",  
                    "  }",  
                    "",  
                    "  dirtyNodes.Clear();",  
                    "}"  
                  ]  
                }  
              ]  
            }  
          ]  
        }  
      ]  
    },  
  
    {  
      "id": "P3",  
      "name": "节点大类 Skill 文档",  
      "priority": "medium",  
      "description": "为所有 12 个节点大类编写 AI Agent 可高效解读的 Skill 文档",  
      "tasks": [  
        {  
          "id": "P3-T1",  
          "name": "创建文档目录和模板",  
          "description": "建立 Skill 文档目录结构和统一模板",  
          "dependencies": [],  
          "steps": [  
            {  
              "action": "CREATE_DIRECTORY",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/"  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/_TEMPLATE.md",  
              "content_spec": {  
                "format": "markdown",  
                "template": [  
                  "# {CategoryDisplayName} Skill 文档",  
                  "",  
                  "## 概述",  
                  "- **类别**: {CategoryEnum} (Tier {TierNumber})",  
                  "- **节点数量**: {NodeCount}",  
                  "- **适用场景**: {UseCaseDescription}",  
                  "",  
                  "## 节点列表",  
                  "",  
                  "### {NodeDisplayName} (`{NodeName}`)",  
                  "- **Houdini 对标**: {HoudiniSOPName}",  
                  "- **功能**: {Description}",  
                  "- **输入端口**:",  
                  "  | 端口名 | 类型 | 默认值 | 必填 | 说明 |",  
                  "  |--------|------|--------|------|------|",  
                  "  | {PortName} | {PortType} | {DefaultValue} | {Required} | {PortDescription} |",  
                  "- **输出端口**:",  
                  "  | 端口名 | 类型 | 说明 |",  
                  "  |--------|------|------|",  
                  "  | geometry | PCGGeometry | {OutputDescription} |",  
                  "- **使用示例**: `{UpstreamNode} → {ThisNode} → {DownstreamNode}`",  
                  "- **AI Agent 调用示例**:",  
                  "  ```json",  
                  "  { \"skill\": \"{NodeName}\", \"parameters\": { {ExampleParams} } }",  
                  "  ```",  
                  "- **注意事项**: {Caveats}",  
                  "",  
                  "## 常见组合模式 (Recipes)",  
                  "### Recipe 1: {RecipeName}",  
                  "```",  
                  "{Node1} → {Node2} → {Node3}",  
                  "```",  
                  "**说明**: {RecipeDescription}"  
                ]  
              }  
            }  
          ]  
        },  
        {  
          "id": "P3-T2",  
          "name": "生成 12 份 Skill 文档",  
          "description": "为每个 PCGNodeCategory 生成一份完整的 Skill 文档",  
          "dependencies": ["P3-T1"],  
          "steps": [  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Create.md",  
              "spec": {  
                "category": "Create",  
                "tier": 0,  
                "description": "基础几何体生成节点，是所有 PCG 工作流的起点",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Create/",  
                "nodes": [  
                  { "file": "BoxNode.cs",             "houdini": "Box SOP",              "recipe_role": "起点" },  
                  { "file": "SphereNode.cs",          "houdini": "Sphere SOP",           "recipe_role": "起点" },  
                  { "file": "GridNode.cs",            "houdini": "Grid SOP",             "recipe_role": "起点/地形基底" },  
                  { "file": "CircleNode.cs",          "houdini": "Circle SOP",           "recipe_role": "起点/曲线源" },  
                  { "file": "LineNode.cs",            "houdini": "Line SOP",             "recipe_role": "起点/路径源" },  
                  { "file": "TubeNode.cs",            "houdini": "Tube SOP",             "recipe_role": "起点" },  
                  { "file": "TorusNode.cs",           "houdini": "Torus SOP",            "recipe_role": "起点" },  
                  { "file": "PlatonicSolidsNode.cs",  "houdini": "Platonic Solids SOP",  "recipe_role": "起点" },  
                  { "file": "HeightfieldNode.cs",     "houdini": "Heightfield SOP",      "recipe_role": "地形生成" },  
                  { "file": "FontNode.cs",            "houdini": "Font SOP",             "recipe_role": "文字几何体" },  
                  { "file": "ImportMeshNode.cs",      "houdini": "File SOP",             "recipe_role": "外部资产导入" },  
                  { "file": "MergeNode.cs",           "houdini": "Merge SOP",            "recipe_role": "合并多个几何体" },  
                  { "file": "DeleteNode.cs",          "houdini": "Delete SOP",           "recipe_role": "过滤/删除元素" },  
                  { "file": "TransformNode.cs",       "houdini": "Transform SOP",        "recipe_role": "变换操作" },  
                  { "file": "GroupCreateNode.cs",     "houdini": "Group Create SOP",     "recipe_role": "分组创建" },  
                  { "file": "GroupExpressionNode.cs", "houdini": "Group Expression SOP", "recipe_role": "表达式分组" }  
                ],  
                "recipes": [  
                  { "name": "基础建模起手式", "flow": "Box → Transform → Extrude → Normal", "desc": "从基础体开始建模" },  
                  { "name": "地形生成", "flow": "Grid → Noise → Normal → MaterialAssign", "desc": "程序化地形" },  
                  { "name": "多体合并", "flow": "[Box, Sphere, Tube] → Merge → Boolean", "desc": "CSG 布尔建模" }  
                ],  
                "dataExtractionMethod": "从每个 .cs 文件中提取 Name, DisplayName, Description, Inputs[], Outputs[] 属性值"  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Attribute.md",  
              "spec": {  
                "category": "Attribute",  
                "tier": 0,  
                "description": "属性操作节点，用于创建、修改、传递几何体上的自定义属性（如颜色、密度、权重等）",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Attribute/",  
                "nodes": [  
                  { "file": "AttributeCreateNode.cs",    "houdini": "Attribute Create SOP" },  
                  { "file": "AttributeSetNode.cs",       "houdini": "Attribute Wrangle (set)" },  
                  { "file": "AttributeDeleteNode.cs",    "houdini": "Attribute Delete SOP" },  
                  { "file": "AttributeCopyNode.cs",      "houdini": "Attribute Copy SOP" },  
                  { "file": "AttributePromoteNode.cs",   "houdini": "Attribute Promote SOP" },  
                  { "file": "AttributeRandomizeNode.cs", "houdini": "Attribute Randomize SOP" },  
                  { "file": "AttributeTransferNode.cs",  "houdini": "Attribute Transfer SOP" },  
                  { "file": "AttribWrangleNode.cs",      "houdini": "Attribute Wrangle SOP" }  
                ],  
                "recipes": [  
                  { "name": "颜色驱动分布", "flow": "Grid → AttributeCreate(density) → Scatter → CopyToPoints", "desc": "用属性控制散布密度" },  
                  { "name": "属性传递上色", "flow": "Sphere → AttributeRandomize(Cd) → AttributeTransfer → Target", "desc": "从源几何体传递颜色到目标" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Geometry.md",  
              "spec": {  
                "category": "Geometry",  
                "tier": 1,  
                "description": "核心几何操作节点，提供挤出、布尔、细分、镜像等建模能力",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Geometry/",  
                "nodes": [  
                  { "file": "ExtrudeNode.cs",        "houdini": "PolyExtrude SOP" },  
                  { "file": "BooleanNode.cs",        "houdini": "Boolean SOP" },  
                  { "file": "SubdivideNode.cs",      "houdini": "Subdivide SOP" },  
                  { "file": "MirrorNode.cs",         "houdini": "Mirror SOP" },  
                  { "file": "FuseNode.cs",           "houdini": "Fuse SOP" },  
                  { "file": "TriangulateNode.cs",    "houdini": "Divide SOP (triangulate)" },  
                  { "file": "NormalNode.cs",         "houdini": "Normal SOP" },  
                  { "file": "FacetNode.cs",          "houdini": "Facet SOP" },  
                  { "file": "BlastNode.cs",          "houdini": "Blast SOP" },  
                  { "file": "ClipNode.cs",           "houdini": "Clip SOP" },  
                  { "file": "InsetNode.cs",          "houdini": "PolyExtrude (inset mode)" },  
                  { "file": "PeakNode.cs",           "houdini": "Peak SOP" },  
                  { "file": "PolyExpand2DNode.cs",   "houdini": "PolyExpand2D SOP" },  
                  { "file": "ReverseNode.cs",        "houdini": "Reverse SOP" },  
                  { "file": "SortNode.cs",           "houdini": "Sort SOP" },  
                  { "file": "MeasureNode.cs",        "houdini": "Measure SOP" },  
                  { "file": "ConnectivityNode.cs",   "houdini": "Connectivity SOP" },  
                  { "file": "MaterialAssignNode.cs", "houdini": "Material SOP" },  
                  { "file": "PackNode.cs",           "houdini": "Pack SOP" },  
                  { "file": "UnpackNode.cs",         "houdini": "Unpack SOP" }  
                ],  
                "recipes": [  
                  { "name": "建筑外墙", "flow": "Box → Extrude(faces) → Inset → Extrude(windows) → Normal", "desc": "程序化建筑立面" },  
                  { "name": "CSG 布尔", "flow": "[Box, Sphere] → Boolean(subtract) → Fuse → Normal", "desc": "布尔减法建模" },  
                  { "name": "对称建模", "flow": "Box → Extrude → Mirror → Fuse → Subdivide", "desc": "镜像对称后细分" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_UV.md",  
              "spec": {  
                "category": "UV",  
                "tier": 2,  
                "description": "UV 操作节点，提供投影、展开、布局、变换等 UV 编辑能力",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/UV/",  
                "nodes": [  
                  { "file": "UVProjectNode.cs",    "houdini": "UV Project SOP" },  
                  { "file": "UVUnwrapNode.cs",     "houdini": "UV Unwrap SOP" },  
                  { "file": "UVLayoutNode.cs",     "houdini": "UV Layout SOP" },  
                  { "file": "UVTransformNode.cs",  "houdini": "UV Transform SOP" },  
                  { "file": "UVTrimSheetNode.cs",  "houdini": "UV Flatten + manual trim" }  
                ],  
                "recipes": [  
                  { "name": "自动 UV 流程", "flow": "Geometry → UVProject(box) → UVLayout → MaterialAssign", "desc": "快速自动 UV" },  
                  { "name": "Trim Sheet", "flow": "Geometry → UVProject → UVTrimSheet → MaterialAssign", "desc": "Trim Sheet UV 映射" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Distribute.md",  
              "spec": {  
                "category": "Distribute",  
                "tier": 3,  
                "description": "分布与实例化节点，用于在几何体表面散布点、复制实例、创建阵列",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Distribute/",  
                "nodes": [  
                  { "file": "ScatterNode.cs",          "houdini": "Scatter SOP" },  
                  { "file": "CopyToPointsNode.cs",     "houdini": "Copy to Points SOP" },  
                  { "file": "ArrayNode.cs",            "houdini": "Copy and Transform SOP" },  
                  { "file": "InstanceNode.cs",         "houdini": "Instance SOP" },  
                  { "file": "PointsFromVolumeNode.cs", "houdini": "Points from Volume SOP" },  
                  { "file": "RayNode.cs",              "houdini": "Ray SOP" }  
                ],  
                "recipes": [  
                  { "name": "植被散布", "flow": "Grid → Noise → Scatter(density_attr) → CopyToPoints(tree) → Instance", "desc": "程序化植被分布" },  
                  { "name": "环形阵列", "flow": "Box → Array(radial) → Merge", "desc": "环形复制阵列" },  
                  { "name": "表面贴合", "flow": "Scatter → CopyToPoints → Ray(project_to_terrain)", "desc": "实例贴合地形" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Curve.md",  
              "spec": {  
                "category": "Curve",  
                "tier": 4,  
                "description": "曲线与路径节点，用于创建曲线、沿曲线扫掠生成几何体、管线建模",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Curve/",  
                "nodes": [  
                  { "file": "CurveCreateNode.cs", "houdini": "Curve SOP" },  
                  { "file": "ResampleNode.cs",    "houdini": "Resample SOP" },  
                  { "file": "SweepNode.cs",       "houdini": "Sweep SOP" },  
                  { "file": "PolyWireNode.cs",    "houdini": "PolyWire SOP" },  
                  { "file": "CarveNode.cs",       "houdini": "Carve SOP" },  
                  { "file": "FilletNode.cs",      "houdini": "Fillet SOP" }  
                ],  
                "recipes": [  
                  { "name": "管道建模", "flow": "CurveCreate → Resample → PolyWire → Normal", "desc": "沿曲线生成管道" },  
                  { "name": "道路生成", "flow": "CurveCreate → Resample → Sweep(road_profile) → UVProject", "desc": "沿路径扫掠道路截面" },  
                  { "name": "动画路径", "flow": "CurveCreate → Carve(animate_u) → Resample", "desc": "曲线裁剪动画" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Deform.md",  
              "spec": {  
                "category": "Deform",  
                "tier": 5,  
                "description": "噪声与变形节点，用于对几何体施加程序化变形（噪声、弯曲、扭曲、锥化等）",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Deform/",  
                "nodes": [  
                  { "file": "NoiseNode.cs",    "houdini": "Attribute Noise SOP" },  
                  { "file": "MountainNode.cs", "houdini": "Mountain SOP" },  
                  { "file": "BendNode.cs",     "houdini": "Bend SOP" },  
                  { "file": "TwistNode.cs",    "houdini": "Twist SOP" },  
                  { "file": "TaperNode.cs",    "houdini": "Taper SOP" },  
                  { "file": "LatticeNode.cs",  "houdini": "Lattice SOP" },  
                  { "file": "SmoothNode.cs",   "houdini": "Smooth SOP" },  
                  { "file": "CreepNode.cs",    "houdini": "Creep SOP" }  
                ],  
                "recipes": [  
                  { "name": "有机地形", "flow": "Grid → Mountain → Smooth → Normal", "desc": "噪声地形 + 平滑" },  
                  { "name": "弯曲柱体", "flow": "Tube → Bend(angle=45) → Twist → Normal", "desc": "程序化弯曲管道" },  
                  { "name": "风化效果", "flow": "Geometry → Noise(low_freq) → Noise(high_freq) → Smooth", "desc": "多层噪声模拟风化" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Topology.md",  
              "spec": {  
                "category": "Topology",  
                "tier": 6,  
                "description": "高级拓扑操作节点，提供倒角、桥接、填充、分割、重拓扑等能力",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Topology/",  
                "nodes": [  
                  { "file": "PolyBevelNode.cs",           "houdini": "PolyBevel SOP" },  
                  { "file": "PolyBridgeNode.cs",          "houdini": "PolyBridge SOP" },  
                  { "file": "PolyFillNode.cs",            "houdini": "PolyFill SOP" },  
                  { "file": "PolySplitNode.cs",           "houdini": "PolySplit SOP" },  
                  { "file": "EdgeDivideNode.cs",          "houdini": "Edge Divide" },  
                  { "file": "RemeshNode.cs",              "houdini": "Remesh SOP" },  
                  { "file": "DecimateNode.cs",            "houdini": "PolyReduce SOP" },  
                  { "file": "ConvexDecompositionNode.cs", "houdini": "Convex Decomposition SOP" }  
                ],  
                "recipes": [  
                  { "name": "硬表面倒角", "flow": "Box → Extrude → PolyBevel → Subdivide → Normal", "desc": "硬表面建模标准流程" },  
                  { "name": "LOD 简化", "flow": "HighPolyMesh → Decimate(ratio=0.3) → Normal", "desc": "减面生成 LOD" },  
                  { "name": "碰撞体生成", "flow": "Mesh → ConvexDecomposition → ExportMesh", "desc": "凸分解生成碰撞体" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Procedural.md",  
              "spec": {  
                "category": "Procedural",  
                "tier": 7,  
                "description": "程序化规则节点，提供 L-System、Voronoi 碎裂、WFC 波函数坍缩等高级程序化生成能力",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Procedural/",  
                "nodes": [  
                  { "file": "LSystemNode.cs",          "houdini": "L-System SOP" },  
                  { "file": "VoronoiFractureNode.cs",  "houdini": "Voronoi Fracture SOP" },  
                  { "file": "WFCNode.cs",              "houdini": "无直接对标 (Wave Function Collapse)" }  
                ],  
                "recipes": [  
                  { "name": "程序化树木", "flow": "LSystem(tree_rule) → PolyWire → Normal → Scatter(leaves)", "desc": "L-System 生成树干 + 散布树叶" },  
                  { "name": "破碎效果", "flow": "Box → VoronoiFracture(scatter_count=20) → Pack → Instance", "desc": "Voronoi 碎裂模拟" },  
                  { "name": "关卡布局", "flow": "WFC(tileset, constraints) → CopyToPoints → Instance", "desc": "WFC 自动关卡生成" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Output.md",  
              "spec": {  
                "category": "Output",  
                "tier": 8,  
                "description": "资产输出节点，将 PCG 结果导出为 FBX、Mesh、Prefab、Material、Scene 等 Unity 资产",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Output/",  
                "nodes": [  
                  { "file": "ExportFBXNode.cs",      "houdini": "ROP FBX Output" },  
                  { "file": "ExportMeshNode.cs",     "houdini": "ROP Geometry Output" },  
                  { "file": "SavePrefabNode.cs",     "houdini": "无直接对标 (Unity Prefab)" },  
                  { "file": "AssemblePrefabNode.cs", "houdini": "无直接对标 (Unity Prefab Assembly)" },  
                  { "file": "SaveMaterialNode.cs",   "houdini": "无直接对标 (Unity Material)" },  
                  { "file": "SaveSceneNode.cs",      "houdini": "无直接对标 (Unity Scene)" },  
                  { "file": "LODGenerateNode.cs",    "houdini": "无直接对标 (Unity LODGroup)" }  
                ],  
                "recipes": [  
                  { "name": "完整资产导出", "flow": "Geometry → Normal → UVProject → MaterialAssign → SavePrefab", "desc": "几何体到 Prefab 完整流程" },  
                  { "name": "LOD 链", "flow": "Mesh → [Decimate(0.5), Decimate(0.2)] → LODGenerate → SavePrefab", "desc": "自动 LOD 生成" },  
                  { "name": "FBX 导出", "flow": "Geometry → Triangulate → Normal → ExportFBX", "desc": "导出为 FBX 文件" }  
                ],  
                "note": "Output 节点在 Live 模式下会被跳过（P1-T1 中定义的 PCGSilentExecutor 过滤逻辑）"  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Utility.md",  
              "spec": {  
                "category": "Utility",  
                "tier": 0,  
                "description": "工具节点，提供常量输入、数学运算、流程控制（Switch/Split/ForEach）、子图等基础设施",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Utility/",  
                "nodes": [  
                  { "file": "ConstFloatNode.cs",     "houdini": "Constant (float)" },  
                  { "file": "ConstIntNode.cs",       "houdini": "Constant (int)" },  
                  { "file": "ConstBoolNode.cs",      "houdini": "Constant (bool)" },  
                  { "file": "ConstStringNode.cs",    "houdini": "Constant (string)" },  
                  { "file": "ConstVector3Node.cs",   "houdini": "Constant (vector3)" },  
                  { "file": "ConstColorNode.cs",     "houdini": "Constant (color)" },  
                  { "file": "MathFloatNode.cs",      "houdini": "VEX math ops" },  
                  { "file": "MathVectorNode.cs",     "houdini": "VEX vector ops" },  
                  { "file": "FitRangeNode.cs",       "houdini": "Fit Range VEX" },  
                  { "file": "RandomNode.cs",         "houdini": "Random VEX" },  
                  { "file": "RampNode.cs",           "houdini": "Ramp Parameter" },  
                  { "file": "CompareNode.cs",        "houdini": "Compare VEX" },  
                  { "file": "SwitchNode.cs",         "houdini": "Switch SOP" },  
                  { "file": "SplitNode.cs",          "houdini": "Split SOP" },  
                  { "file": "ForEachNode.cs",        "houdini": "For-Each SOP" },  
                  { "file": "CacheNode.cs",          "houdini": "Cache SOP" },  
                  { "file": "NullNode.cs",           "houdini": "Null SOP" },  
                  { "file": "GroupCombineNode.cs",   "houdini": "Group Combine SOP" },  
                  { "file": "SubGraphNode.cs",       "houdini": "Subnet / HDA" },  
                  { "file": "SubGraphInputNode.cs",  "houdini": "Subnet Input" },  
                  { "file": "SubGraphOutputNode.cs", "houdini": "Subnet Output" }  
                ],  
                "recipes": [  
                  { "name": "参数化控制", "flow": "ConstFloat(seed) → Random → Scatter(count)", "desc": "用常量驱动随机种子" },  
                  { "name": "条件分支", "flow": "Geometry → Split(group_expr) → [BranchA, BranchB] → Merge", "desc": "按条件分流处理后合并" },  
                  { "name": "子图复用", "flow": "SubGraphInput → [内部节点链] → SubGraphOutput → SubGraph(多次调用)", "desc": "封装可复用的节点子图" },  
                  { "name": "循环处理", "flow": "Geometry → ForEach(piece) → [Transform, Noise] → Merge", "desc": "对每个 piece 独立处理" },  
                  { "name": "数学驱动", "flow": "ConstFloat → MathFloat(sin) → FitRange(0,1) → Ramp → AttributeSet", "desc": "数学函数驱动属性值" }  
                ]  
              }  
            },  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/SKILL_Input.md",  
              "spec": {  
                "category": "Input",  
                "tier": 0,  
                "description": "场景输入节点，从 Unity Scene 中的 GameObject 读取网格、点云或交互选择结果，作为 PCG 工作流的输入源",  
                "sourceDir": "Assets/PCGToolkit/Editor/Nodes/Input/",  
                "nodes": [  
                  {  
                    "file": "SceneObjectInputNode.cs",  
                    "name": "SceneObjectInput",  
                    "displayName": "Scene Object Input",  
                    "houdini": "File SOP / Object Merge SOP",  
                    "description": "读取场景 GameObject 的 MeshFilter，转为 PCGGeometry",  
                    "inputs": [  
                      { "port": "target",         "type": "SceneObject", "default": null,  "required": false, "desc": "场景中的 GameObject (需要 MeshFilter)" },  
                      { "port": "applyTransform",  "type": "Bool",        "default": false, "required": false, "desc": "是否将 world 变换烘焙到顶点" },  
                      { "port": "readMaterials",    "type": "Bool",        "default": true,  "required": false, "desc": "是否将材质路径写入 @material 属性" }  
                    ],  
                    "outputs": [  
                      { "port": "geometry", "type": "Geometry", "desc": "从 MeshFilter 转换的 PCGGeometry" }  
                    ]  
                  },  
                  {  
                    "file": "ScenePointsInputNode.cs",  
                    "name": "ScenePointsInput",  
                    "displayName": "Scene Points Input",  
                    "houdini": "Object Merge SOP (points only)",  
                    "description": "将场景 GameObject 的子对象位置转为点云 PCGGeometry",  
                    "inputs": [  
                      { "port": "target",      "type": "SceneObject", "default": null,    "required": false, "desc": "父 GameObject（子对象用作点云）" },  
                      { "port": "includeRoot",  "type": "Bool",        "default": false,   "required": false, "desc": "是否包含根对象自身" },  
                      { "port": "readNames",    "type": "Bool",        "default": true,    "required": false, "desc": "将子对象名写入 @name 属性" },  
                      { "port": "space",         "type": "String",      "default": "World", "required": false, "desc": "World / Local", "enumOptions": ["World", "Local"] }  
                    ],  
                    "outputs": [  
                      { "port": "geometry", "type": "Geometry", "desc": "仅含点的 PCGGeometry（含 orient, pscale, name 属性）" }  
                    ]  
                  },  
                  {  
                    "file": "SceneSelectionInputNode.cs",  
                    "name": "SceneSelectionInput",  
                    "displayName": "Scene Selection Input",  
                    "houdini": "无直接对标 (交互式选择)",  
                    "description": "读取场景交互选择结果，输出带选择 Group 的几何体",  
                    "inputs": [  
                      { "port": "target",               "type": "SceneObject", "default": null,       "required": false, "desc": "场景中的 GameObject (需要 MeshFilter)" },  
                      { "port": "groupName",             "type": "String",      "default": "selected", "required": false, "desc": "输出的 Group 名称" },  
                      { "port": "applyTransform",        "type": "Bool",        "default": true,       "required": false, "desc": "是否烘焙世界变换" },  
                      { "port": "readMaterials",          "type": "Bool",        "default": true,       "required": false, "desc": "是否读取材质" },  
                      { "port": "serializedSelection",   "type": "String",      "default": "",         "required": false, "desc": "序列化的选择数据（自动管理）" }  
                    ],  
                    "outputs": [  
                      { "port": "geometry", "type": "Geometry", "desc": "完整几何体，带 PrimGroups/PointGroups 标记选择结果" }  
                    ],  
                    "note": "支持 Face/Vertex/Edge 三种选择模式，选择数据通过 PCGSelectionState 管理"  
                  }  
                ],  
                "recipes": [  
                  { "name": "场景网格编辑", "flow": "SceneObjectInput → Extrude → Normal → SavePrefab", "desc": "读取场景物体进行程序化编辑" },  
                  { "name": "点云驱动实例", "flow": "ScenePointsInput → CopyToPoints(tree_prefab) → Instance", "desc": "用场景空物体位置驱动实例化" },  
                  { "name": "交互式局部编辑", "flow": "SceneSelectionInput(faces) → Blast(group=selected, invert) → Extrude → Merge", "desc": "选择面后局部挤出" }  
                ]  
              }  
            }  
          ]  
        },  
        {  
          "id": "P3-T3",  
          "name": "更新索引与 Skill 系统集成",  
          "description": "创建文档索引文件，更新 SkillSchemaExporter 支持导出带文档链接的 schema",  
          "dependencies": ["P3-T2"],  
          "steps": [  
            {  
              "action": "CREATE_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/Docs/INDEX.md",  
              "content_spec": {  
                "format": "markdown",  
                "structure": [  
                  "# PCG Toolkit Skill 文档索引",  
                  "",  
                  "## 文档列表",  
                  "| Category | Tier | 节点数 | 文档 |",  
                  "|----------|------|--------|------|",  
                  "| Create | 0 | 16 | [SKILL_Create.md](SKILL_Create.md) |",  
                  "| Attribute | 0 | 8 | [SKILL_Attribute.md](SKILL_Attribute.md) |",  
                  "| Geometry | 1 | 20 | [SKILL_Geometry.md](SKILL_Geometry.md) |",  
                  "| UV | 2 | 5 | [SKILL_UV.md](SKILL_UV.md) |",  
                  "| Distribute | 3 | 6 | [SKILL_Distribute.md](SKILL_Distribute.md) |",  
                  "| Curve | 4 | 6 | [SKILL_Curve.md](SKILL_Curve.md) |",  
                  "| Deform | 5 | 8 | [SKILL_Deform.md](SKILL_Deform.md) |",  
                  "| Topology | 6 | 8 | [SKILL_Topology.md](SKILL_Topology.md) |",  
                  "| Procedural | 7 | 3 | [SKILL_Procedural.md](SKILL_Procedural.md) |",  
                  "| Output | 8 | 7 | [SKILL_Output.md](SKILL_Output.md) |",  
                  "| Utility | 0 | 21 | [SKILL_Utility.md](SKILL_Utility.md) |",  
                  "| Input | 0 | 3 | [SKILL_Input.md](SKILL_Input.md) |",  
                  "",  
                  "## 快速查找",  
                  "- 总节点数: ~111",  
                  "- 生成日期: {auto}",  
                  "- 配套 JSON Schema: 通过 `SkillSchemaExporter.ExportAll()` 导出"  
                ]  
              }  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/Editor/Skill/SkillSchemaExporter.cs",  
              "changes": [  
                {  
                  "location": "class body (after ExportToFile method, line 63)",  
                  "operation": "ADD_METHOD",  
                  "methods": [  
                    {  
                      "name": "ExportByCategory",  
                      "visibility": "public static",  
                      "params": "string category",  
                      "returns": "string",  
                      "logic": [  
                        "SkillRegistry.EnsureInitialized();",  
                        "var sb = new StringBuilder();",  
                        "sb.AppendLine('{');",  
                        "sb.AppendLine($'  \"category\": \"{category}\",');",  
                        "sb.AppendLine('  \"skills\": [');",  
                        "",  
                        "bool first = true;",  
                        "foreach (var skill in SkillRegistry.GetAllSkills())",  
                        "  // 通过 PCGNodeRegistry 获取节点的 Category",  
                        "  var node = PCGNodeRegistry.GetNode(skill.Name);",  
                        "  if (node == null || node.Category.ToString() != category) continue;",  
                        "  if (!first) sb.AppendLine(',');",  
                        "  first = false;",  
                        "  sb.Append('    ');",  
                        "  sb.Append(skill.GetJsonSchema());",  
                        "",  
                        "sb.AppendLine();",  
                        "sb.AppendLine('  ]');",  
                        "sb.Append('}');",  
                        "return sb.ToString();"  
                      ]  
                    },  
                    {  
                      "name": "ExportAllByCategory",  
                      "visibility": "public static",  
                      "params": "string outputDir",  
                      "returns": "void",  
                      "logic": [  
                        "var categories = System.Enum.GetNames(typeof(PCGNodeCategory));",  
                        "foreach (var cat in categories)",  
                        "  string json = ExportByCategory(cat);",  
                        "  string filePath = System.IO.Path.Combine(outputDir, $'SKILL_{cat}_schema.json');",  
                        "  System.IO.File.WriteAllText(filePath, json, Encoding.UTF8);",  
                        "Debug.Log($'SkillSchemaExporter: Exported {categories.Length} category schemas to {outputDir}');",  
                        "if (outputDir.StartsWith('Assets/'))",  
                        "  AssetDatabase.Refresh();"  
                      ]  
                    }  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/AI_AGENT_GUIDE.md",  
              "changes": [  
                {  
                  "location": "文档末尾",  
                  "operation": "APPEND",  
                  "code_description": "添加 Skill 文档引用章节",  
                  "content": [  
                    "",  
                    "## Skill 文档",  
                    "",  
                    "每个节点大类都有详细的 Skill 文档，位于 `Assets/PCGToolkit/Editor/Skill/Docs/` 目录：",  
                    "",  
                    "- 文档索引: `Docs/INDEX.md`",  
                    "- 每份文档包含: 节点功能、输入输出端口、参数说明、使用示例、Houdini 对标、常见组合模式",  
                    "- JSON Schema 导出: 调用 `SkillSchemaExporter.ExportAllByCategory(outputDir)` 可按类别导出机器可读的 JSON schema",  
                    "",  
                    "### 快速查询示例",  
                    "```csharp",  
                    "// 导出单个类别的 JSON schema",  
                    "string json = SkillSchemaExporter.ExportByCategory(\"Geometry\");",  
                    "",  
                    "// 导出所有类别到目录",  
                    "SkillSchemaExporter.ExportAllByCategory(\"Assets/PCGToolkit/Editor/Skill/Docs/\");",  
                    "```"  
                  ]  
                }  
              ]  
            },  
            {  
              "action": "MODIFY_FILE",  
              "path": "Assets/PCGToolkit/HandBook.md",  
              "changes": [  
                {  
                  "location": "文档末尾",  
                  "operation": "APPEND",  
                  "content": [  
                    "",  
                    "## Skill 文档参考",  
                    "",  
                    "详细的节点 Skill 文档位于 `Editor/Skill/Docs/` 目录，按节点大类组织：",  
                    "Create / Attribute / Geometry / UV / Distribute / Curve / Deform / Topology / Procedural / Output / Utility / Input",  
                    "",  
                    "每份文档包含节点的完整端口定义、参数说明、Houdini 对标和常见组合模式。"  
                  ]  
                }  
              ]  
            }  
          ]  
        }  
      ]  
    }  
  ],  
  
  "executionOrder": {  
    "description": "建议的执行顺序，考虑依赖关系和优先级",  
    "order": [  
      {  
        "batch": 1,  
        "parallel": true,  
        "tasks": ["P1-T1", "P1-T3", "P2-T1", "P2-T2", "P2-T4", "P2-T5", "P3-T1"],  
        "reason": "这些任务互相独立，可以并行开发"  
      },  
      {  
        "batch": 2,  
        "parallel": true,  
        "tasks": ["P1-T2", "P2-T3", "P3-T2"],  
        "reason": "P1-T2 依赖 P1-T1；P2-T3 可独立但建议在 P2-T1 后；P3-T2 依赖 P3-T1"  
      },  
      {  
        "batch": 3,  
        "parallel": false,  
        "tasks": ["P1-T4", "P3-T3"],  
        "reason": "P1-T4 依赖 P1-T1 和 P1-T2；P3-T3 依赖 P3-T2"  
      }  
    ]  
  },  
  
  "testingStrategy": {  
    "P1": {  
      "tests": [  
        { "name": "Live 模式开关", "steps": "打开 Graph → 点击 Live Toggle → 验证按钮高亮 + 进度条可见" },  
        { "name": "防抖验证", "steps": "Live 模式下快速连续修改 3 个节点参数 → 验证只触发 1 次静默运行" },  
        { "name": "打断重启", "steps": "Live 模式下修改参数 → 等静默运行到一半 → 再次修改 → 验证旧运行被取消、新运行启动" },  
        { "name": "Output 跳过", "steps": "Graph 包含 SavePrefab 节点 → Live 模式运行 → 验证不生成 Prefab 文件" },  
        { "name": "Scene 预览", "steps": "Live 模式运行完成 → 验证 Scene 中出现 PCG_LivePreview 对象" },  
        { "name": "手动执行互斥", "steps": "Live 模式运行中 → 点击 Execute → 验证静默运行被取消、手动执行正常进行" }  
      ]  
    },  
    "P2": {  
      "tests": [  
        { "name": "邻接表正确性", "steps": "对比优化前后的执行结果，验证输出几何体一致" },  
        { "name": "实例池", "steps": "连续执行同一 Graph 10 次 → 验证 Activator.CreateInstance 只被调用 N 次（N=节点类型数）" },  
        { "name": "Cache ReadOnly", "steps": "Cache hit 时验证返回的几何体与缓存中的是同一引用（ReferenceEquals）" },  
        { "name": "增量 hash", "steps": "未修改的 PCGGeometry 连续调用 GetContentHash() → 验证第二次不重新计算" },  
        { "name": "ErrorPanel 过滤", "steps": "生成 Warning + Error → 关闭 Warning Toggle → 验证只显示 Error" },  
        { "name": "FlameChart", "steps": "执行 Graph → 切换到 Flame 视图 → 验证节点块宽度与耗时成比例" },  
        { "name": "子图拓扑排序", "steps": "修改中间节点 → ExecuteIncremental → 验证只执行 dirty 子图中的节点" }  
      ]  
    },  
    "P3": {  
      "tests": [  
        { "name": "文档完整性", "steps": "验证 12 份 SKILL_*.md 文件都存在且非空" },  
        { "name": "节点覆盖率", "steps": "对比 PCGNodeRegistry.GetAllNodes() 与文档中列出的节点 → 验证 100% 覆盖" },  
        { "name": "ExportByCategory", "steps": "调用 SkillSchemaExporter.ExportByCategory('Geometry') → 验证返回的 JSON 只包含 Geometry 类别节点" },  
        { "name": "INDEX 链接", "steps": "验证 INDEX.md 中所有链接指向的文件都存在" }  
      ]  
    }  
  },  
  
  "versionBump": {  
    "file": "Assets/PCGToolkit/Editor/Core/PCGToolkitVersion.cs",  
    "from": "0.5.0-alpha",  
    "to": "0.6.0",  
    "changelogEntry": [  
      "feat: Live Feedback Mode — 即时反馈模式，Graph 改动后自动静默运行并在 Scene 预览",  
      "feat: PCGSilentExecutor — 后台静默执行器，支持取消、防抖、进度回调",  
      "feat: Live 进度条 UI — Graph 底部显示静默运行进度",  
      "feat: PCGDebugOverlay — SceneView 叠加显示执行状态和几何体统计",  
      "feat: PerformancePanel FlameChart — 火焰图视图按拓扑顺序展示节点耗时",  
      "feat: ErrorPanel 过滤/搜索 — 支持 Warning/Error 过滤和文本搜索",  
      "feat: SkillSchemaExporter.ExportByCategory — 按类别导出 Skill JSON schema",  
      "feat: 12 份 Skill 文档 — 覆盖所有 111 个节点的完整端口/参数/示例文档",  
      "perf: 邻接表预建 — ExecuteNodeInternal 输入收集从 O(N*E) 降为 O(E_node)",  
      "perf: 节点实例池 — 避免每次 Activator.CreateInstance 反射开销",  
      "perf: Cache ReadOnly — TryGetGeometryReadOnly 避免不必要的 Clone",  
      "perf: 增量 hash — PCGGeometry.GetContentHash() 缓存未修改几何体的 hash",  
      "perf: AttributeStore 延迟初始化 — 减少空属性字典的内存分配",  
      "perf: 子图拓扑排序 — ExecuteIncremental 只对 dirty 子图排序",  
      "perf: FastMode — PCGAsyncGraphExecutor 跳过动画帧，单帧执行节点"  
    ]  
  }  
}
```