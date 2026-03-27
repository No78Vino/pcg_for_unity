using System;
using System.Collections.Generic;
using UnityEditor;  
using UnityEditor.UIElements;  
using UnityEngine;  
using UnityEngine.UIElements;  
  
namespace PCGToolkit.Graph  
{  
    public class PCGGraphEditorWindow : EditorWindow  
    {  
        private PCGGraphView graphView;  
        private PCGGraphData currentGraph;  
  
        // ---- 迭代一：文件状态管理 ----  
        private string _currentAssetPath;   // 当前文件路径，null 表示新建未保存  
        private bool _isDirty;              // 脏状态标记

        // ---- Inspector 集成 ----
        private PCGNodeInspectorWindow _inspectorWindow;
        private PCGNodeVisual _lastSelectedNode; // 用于轮询检测选中变化

        // ---- 本地化 ----
        private Button _langButton;
        private Button _newButton;
        private Button _saveButton;
        private Button _saveAsButton;
        private Button _loadButton;
        private PCGAsyncGraphExecutor _asyncExecutor;  
        private PCGNodePreviewWindow _previewWindow;  
        private Label _totalTimeLabel;  
        private Label _executionStateLabel;  
        private Button _executeButton;  
        private Button _runToSelectedButton;  
        private Button _stopButton;
        private ProgressBar _progressBar; // 迭代三：进度条
        
        // 迭代三：错误面板
        private PCGErrorPanel _errorPanel;
        private VisualElement _mainContainer;

        // 迭代七：性能面板
        private PCGPerformancePanel _perfPanel;
        private bool _showPerfPanel = false;

        // P1-T2: Live Feedback Mode
        private bool _liveMode = false;
        private PCGSilentExecutor _silentExecutor;
        private double _lastChangeTime = 0;
        private bool _pendingSilentRun = false;
        private const double DEBOUNCE_DELAY = 0.4;
        private VisualElement _liveProgressBar;
        private Label _liveProgressLabel;
        private VisualElement _liveProgressFill;
  
        [MenuItem("PCG Toolkit/Node Editor")]  
        public static PCGGraphEditorWindow OpenWindow()  
        {  
            var window = GetWindow<PCGGraphEditorWindow>();  
            window.titleContent = new GUIContent("PCG Node Editor");  
            window.minSize = new Vector2(800, 600);
            return window;
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            InitializeExecutor();

            // 迭代一：注册 Undo/Redo 回调
            Undo.undoRedoPerformed += OnUndoRedo;

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);

            // 迭代四：注册本地化变更回调
            PCGToolkit.Core.PCGLocalization.OnLanguageChanged += RefreshToolbarLabels;

            // P1-T2: Live 模式初始化
            _liveMode = EditorPrefs.GetBool("PCGToolkit_LiveMode", false);
            _silentExecutor = new PCGSilentExecutor();
            _silentExecutor.OnProgressChanged += OnSilentExecutionProgress;
            _silentExecutor.OnSilentExecutionCompleted += OnSilentExecutionCompleted;
            _silentExecutor.OnSilentExecutionCancelled += () =>
            {
                _liveProgressLabel.text = "Live: Cancelled";
            };
            _silentExecutor.OnSilentExecutionFailed += (err) =>
            {
                _liveProgressLabel.text = $"Live: Error - {err}";
            };

            // 自动打开 Inspector（现在是唯一的参数编辑入口）
            EditorApplication.delayCall += () =>
            {
                if (this != null && _inspectorWindow == null)
                {
                    _inspectorWindow = PCGNodeInspectorWindow.Open();
                    _inspectorWindow.BindGraphView(graphView);
                }
            };

            PCGHoudiniLayout.ApplyLayoutIfFirstTime();
        }

        private void Update()
        {
            // P1-T2: Live 模式防抖检测
            if (_liveMode && _pendingSilentRun)
            {
                if (EditorApplication.timeSinceStartup - _lastChangeTime >= DEBOUNCE_DELAY)
                {
                    _pendingSilentRun = false;
                    StartSilentExecution();
                }
            }

            // 轮询检测选中节点变化（GraphView 没有原生的 OnSelectionChanged）
            if (graphView == null) return;
            var currentSelected = graphView.GetSelectedNodeVisual();
            if (currentSelected != _lastSelectedNode)
            {
                _lastSelectedNode = currentSelected;
                if (_inspectorWindow != null)
                    _inspectorWindow.InspectNode(currentSelected);
            }
        }

        private void OnDisable()
        {
            // 停止执行  
            if (_asyncExecutor != null && _asyncExecutor.State != ExecutionState.Idle)
                _asyncExecutor.Stop();

            if (graphView != null && _mainContainer != null)
                _mainContainer.Remove(graphView);

            // 迭代一：注销 Undo/Redo 回调
            Undo.undoRedoPerformed -= OnUndoRedo;
            
            // 注销全局键盘事件回调
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown);

            // 迭代四：注销本地化回调
            PCGToolkit.Core.PCGLocalization.OnLanguageChanged -= RefreshToolbarLabels;

            // P1-T2: 清理静默执行器
            if (_silentExecutor != null && _silentExecutor.IsRunning)
                _silentExecutor.Stop();
        }

        private void ConstructGraphView()  
        {  
            // 迭代三：创建主容器（GraphView + ErrorPanel）
            _mainContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column,
                }
            };
            rootVisualElement.Add(_mainContainer);
            
            graphView = new PCGGraphView();
            graphView.style.flexGrow = 1; // 迭代四修复：使用flexGrow代替StretchToParentSize
            graphView.Initialize(this);
            _mainContainer.Add(graphView);
            
            // 迭代一：注册脏状态回调
            graphView.OnGraphChanged += MarkDirty;

            // P1-T2: Live 模式触发逻辑
            graphView.OnGraphChanged += () =>
            {
                if (_liveMode)
                {
                    _lastChangeTime = EditorApplication.timeSinceStartup;
                    _pendingSilentRun = true;
                    if (_silentExecutor != null && _silentExecutor.IsRunning)
                        _silentExecutor.Cancel();
                }
            };

            // 连线/断线时刷新 Inspector（参数连接状态变化）
            graphView.OnGraphChanged += () =>
            {
                if (_inspectorWindow != null && _lastSelectedNode != null)
                    _inspectorWindow.ForceRebuildCurrentNode();
            };
            
            // 迭代三：注册节点点击预览回调
            graphView.OnNodeClicked += OnNodeClickedForPreview;
            
            // 迭代三：创建错误面板（默认隐藏）
            _errorPanel = new PCGErrorPanel();
            _errorPanel.style.display = DisplayStyle.None;
            _errorPanel.OnErrorClicked += OnErrorClicked;
            _mainContainer.Add(_errorPanel);

            // 迭代七：创建性能面板（默认隐藏）
            _perfPanel = new PCGPerformancePanel();
            _perfPanel.style.display = DisplayStyle.None;
            _mainContainer.Add(_perfPanel);

            // P1-T2: Live 进度条
            _liveProgressBar = new VisualElement
            {
                style =
                {
                    height = 20,
                    backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.6f)),
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 8,
                    paddingRight = 8,
                    alignItems = Align.Center,
                }
            };
            _liveProgressFill = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    height = new Length(100, LengthUnit.Percent),
                    width = new Length(0, LengthUnit.Percent),
                    backgroundColor = new StyleColor(new Color(0.2f, 0.7f, 0.8f, 0.4f)),
                }
            };
            _liveProgressBar.Add(_liveProgressFill);
            _liveProgressLabel = new Label("Live: Idle")
            {
                style =
                {
                    color = new StyleColor(Color.white),
                    fontSize = 11,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            };
            _liveProgressBar.Add(_liveProgressLabel);
            _liveProgressBar.style.display = _liveMode ? DisplayStyle.Flex : DisplayStyle.None;
            _mainContainer.Add(_liveProgressBar);
        }
        
        // 迭代三：节点点击预览
        private void OnNodeClickedForPreview(string nodeId)
        {
            // 只有执行完成后才能预览
            if (_asyncExecutor.State != ExecutionState.Idle && 
                _asyncExecutor.State != ExecutionState.Paused)
                return;
            
            var result = _asyncExecutor.GetNodeResult(nodeId);
            if (result == null || result.Outputs == null || result.Outputs.Count == 0)
                return;
            
            // 获取第一个 Geometry 输出
            PCGToolkit.Core.PCGGeometry previewGeo = null;
            foreach (var kvp in result.Outputs)
            {
                if (kvp.Value != null)
                {
                    previewGeo = kvp.Value;
                    break;
                }
            }
            
            if (previewGeo == null) return;
            
            // 打开预览窗口
            if (_previewWindow == null)
                _previewWindow = PCGNodePreviewWindow.Open();
            
            _previewWindow.SetPreviewData(nodeId, result.NodeType, previewGeo, result.ElapsedMs);
            _previewWindow.Show();
            _previewWindow.Focus();
        }
        
        // 迭代三：错误点击高亮节点
        private void OnErrorClicked(string nodeId)
        {
            graphView.ClearAllHighlights();
            var visual = graphView.FindNodeVisual(nodeId);
            if (visual != null)
            {
                visual.SetHighlight(true);
                visual.SetErrorState(true);
            }
        }  
  
        private void InitializeExecutor()  
        {  
            _asyncExecutor = new PCGAsyncGraphExecutor();  
  
            // 节点高亮事件  
            _asyncExecutor.OnNodeHighlight += nodeId =>  
            {  
                graphView.ClearAllHighlights();  
                var visual = graphView.FindNodeVisual(nodeId);  
                if (visual != null)  
                    visual.SetHighlight(true);  
            };  
  
            // 节点执行完成事件
            _asyncExecutor.OnNodeCompleted += result =>
            {
                var visual = graphView.FindNodeVisual(result.NodeId);
                if (visual != null)
                {
                    visual.SetHighlight(false);
                    visual.ShowExecutionTime(result.ElapsedMs);

                    // 更新 Inspector 的执行结果信息
                    if (_inspectorWindow != null && visual == _lastSelectedNode)
                    {
                        _inspectorWindow.UpdateExecutionInfo(result.ElapsedMs, result.OutputGeometry);
                    }

                    if (!result.Success)
                    {
                        visual.SetErrorState(true);
                        // 迭代三：添加到错误面板
                        _errorPanel.AddError(result.NodeId, result.NodeType, result.ErrorMessage ?? "Execution failed");
                        _errorPanel.style.display = DisplayStyle.Flex;
                    }
                }

                // 更新总时长和进度条
                UpdateTotalTimeLabel();
                UpdateProgressBar();
            };  
  
            // 整个图执行完成事件  
            _asyncExecutor.OnExecutionCompleted += totalMs =>  
            {  
                graphView.ClearAllHighlights();  
                UpdateTotalTimeLabel(totalMs);  
                UpdateExecutionStateLabel(PCGToolkit.Core.PCGLocalization.Get("state.completed"));  
                SetToolbarButtonsEnabled(true);
                _progressBar.value = 100f;

                // 迭代七：收集性能数据
                if (_showPerfPanel && _perfPanel != null && currentGraph != null)
                    _perfPanel.CollectFromExecutor(_asyncExecutor, currentGraph);

                Debug.Log($"PCG Graph execution completed. Total: {totalMs:F1}ms");  
            };  
  
            // 执行暂停事件（Run To Selected 到达目标）  
            _asyncExecutor.OnExecutionPaused += (nodeId, result) =>  
            {  
                graphView.ClearAllHighlights();  
                var visual = graphView.FindNodeVisual(nodeId);  
                if (visual != null)  
                    visual.SetHighlight(true); // 保持暂停节点高亮  
  
                UpdateTotalTimeLabel();  
                UpdateExecutionStateLabel($"Paused at {result.NodeType}");  
                SetToolbarButtonsEnabled(true);
                UpdateProgressBar();
  
                // 尝试打开预览窗口  
                ShowPreviewForNode(nodeId, result);  
            };  
  
            // 状态变更事件  
            _asyncExecutor.OnStateChanged += state =>  
            {  
                switch (state)  
                {  
                    case ExecutionState.Running:  
                        UpdateExecutionStateLabel("Running...");  
                        break;  
                    case ExecutionState.Paused:  
                        UpdateExecutionStateLabel("Paused");  
                        break;  
                    case ExecutionState.Idle:  
                        UpdateExecutionStateLabel("Idle");  
                        break;  
                }  
            };  
        }  
  
        private void GenerateToolbar()  
        {  
            var toolbar = new Toolbar();
            Func<string, string> L = PCGToolkit.Core.PCGLocalization.Get;
  
            // ---- 文件操作按钮 ----  
            _newButton = new Button(() => NewGraph()) { text = L("btn.new") };  
            toolbar.Add(_newButton);  
  
            _saveButton = new Button(() => SaveGraph()) { text = L("btn.save") };  
            toolbar.Add(_saveButton);  
  
            // 迭代一：新增 Save As 按钮
            _saveAsButton = new Button(() => SaveAsGraph()) { text = L("btn.saveas") };  
            toolbar.Add(_saveAsButton);
  
            _loadButton = new Button(() => LoadGraph()) { text = L("btn.load") };  
            toolbar.Add(_loadButton);  
  
            // ---- 分隔 ----  
            toolbar.Add(new ToolbarSpacer());  
  
            // ---- 执行按钮 ----  
            _executeButton = new Button(() => OnExecuteClicked()) { text = L("btn.execute") };  
            _executeButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f));  
            toolbar.Add(_executeButton);  
  
            _runToSelectedButton = new Button(() => OnRunToSelectedClicked()) { text = L("btn.runToSelected") };  
            _runToSelectedButton.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.15f));  
            toolbar.Add(_runToSelectedButton);  
  
            _stopButton = new Button(() => OnStopClicked()) { text = L("btn.stop") };  
            _stopButton.style.backgroundColor = new StyleColor(new Color(0.5f, 0.2f, 0.2f));  
            toolbar.Add(_stopButton);

            // P1-T2: Live 模式 Toggle 按钮
            var liveToggle = new ToolbarToggle { text = "Live", value = _liveMode };
            liveToggle.style.backgroundColor = _liveMode ? new StyleColor(new Color(0.1f, 0.6f, 0.2f)) : default;
            liveToggle.RegisterValueChangedCallback(evt =>
            {
                _liveMode = evt.newValue;
                EditorPrefs.SetBool("PCGToolkit_LiveMode", _liveMode);
                liveToggle.style.backgroundColor = _liveMode ? new StyleColor(new Color(0.1f, 0.6f, 0.2f)) : default;
                if (_liveProgressBar != null)
                    _liveProgressBar.style.display = _liveMode ? DisplayStyle.Flex : DisplayStyle.None;
                if (!_liveMode && _silentExecutor != null && _silentExecutor.IsRunning)
                    _silentExecutor.Cancel();
            });
            toolbar.Add(liveToggle);  
            // C3: Export All
            var exportAllButton = new Button(OnExportAllClicked)
            {
                text = "Export All",
                tooltip = "执行所有 enabled=true 的 Output 节点"
            };
            exportAllButton.style.backgroundColor = new StyleColor(new Color(0.3f, 0.2f, 0.5f));
            toolbar.Add(exportAllButton);
  
            // ---- 分隔 ----  
            toolbar.Add(new ToolbarSpacer());  
  
            // ---- 状态标签 ----  
            _executionStateLabel = new Label(L("state.idle"))  
            {  
                style =  
                {  
                    unityTextAlign = TextAnchor.MiddleLeft,  
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)),  
                    marginLeft = 4,  
                    marginRight = 8,  
                }  
            };  
            toolbar.Add(_executionStateLabel);
            
            // 迭代三：进度条
            _progressBar = new ProgressBar { title = "", value = 0 };
            _progressBar.style.width = 100;
            _progressBar.style.height = 16;
            toolbar.Add(_progressBar);

            // Inspector 按钮
            var inspectorButton = new Button(ToggleInspectorWindow)
            {
                text = L("btn.inspector"),
                tooltip = "Open the Node Inspector panel"
            };
            toolbar.Add(inspectorButton);

            // 4: Spreadsheet 按钮
            var spreadsheetButton = new Button(() =>
            {
                var node = graphView?.GetSelectedNodeVisual();
                if (node != null && _asyncExecutor != null)
                {
                    var result = _asyncExecutor.GetNodeResult(node.NodeId);
                    if (result?.OutputGeometry != null)
                    {
                        var window = PCGGeometrySpreadsheetWindow.Open();
                        window.SetGeometry(result.OutputGeometry, node.PCGNode?.DisplayName ?? "");
                    }
                    else
                    {
                        Debug.Log("Please execute the graph first to see node results in the Spreadsheet.");
                    }
                }
                else
                {
                    PCGGeometrySpreadsheetWindow.Open();
                }
            })
            {
                text = "Spreadsheet",
                tooltip = "Open Geometry Spreadsheet"
            };
            toolbar.Add(spreadsheetButton);

            // 迭代七：性能面板切换按钮
            var perfToggle = new ToolbarToggle { text = "Perf" };
            perfToggle.RegisterValueChangedCallback(evt =>
            {
                _showPerfPanel = evt.newValue;
                _perfPanel.style.display = _showPerfPanel
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });
            toolbar.Add(perfToggle);

            // 迭代四：语言切换按钮
            _langButton = new Button(() =>
            {
                PCGToolkit.Core.PCGLocalization.ToggleLanguage();
            })
            {
                text = L("btn.lang"),
                tooltip = "Switch language / 切换语言"
            };
            _langButton.style.marginLeft = 4;
            toolbar.Add(_langButton);

            // ---- 弹性空间 ----  
            var spacer = new VisualElement();  
            spacer.style.flexGrow = 1;  
            toolbar.Add(spacer);  
  
            // ---- 总时长标签（右侧） ----  
            _totalTimeLabel = new Label(L("total.label"))  
            {  
                style =  
                {  
                    unityTextAlign = TextAnchor.MiddleRight,  
                    color = new StyleColor(new Color(0.9f, 0.9f, 0.3f)),  
                    marginRight = 8,  
                    fontSize = 12,  
                }  
            };  
            toolbar.Add(_totalTimeLabel);
            
            // 版本信息标签
            var versionLabel = new Label($"v{PCGToolkit.Core.PCGToolkitVersion.Version}")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleRight,
                    color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
                    marginRight = 8,
                    fontSize = 10,
                }
            };
            versionLabel.tooltip = $"PCG Toolkit {PCGToolkit.Core.PCGToolkitVersion.FullVersion}\nBuild: {PCGToolkit.Core.PCGToolkitVersion.BuildDate}";
            toolbar.Add(versionLabel);
  
            rootVisualElement.Add(toolbar);  
        }
        
        // ---- 迭代四：本地化刷新 ----

        private void RefreshToolbarLabels()
        {
            Func<string, string> L = PCGToolkit.Core.PCGLocalization.Get;
            if (_newButton != null) _newButton.text = L("btn.new");
            if (_saveButton != null) _saveButton.text = L("btn.save");
            if (_saveAsButton != null) _saveAsButton.text = L("btn.saveas");
            if (_loadButton != null) _loadButton.text = L("btn.load");
            if (_executeButton != null) _executeButton.text = L("btn.execute");
            if (_runToSelectedButton != null) _runToSelectedButton.text = L("btn.runToSelected");
            if (_stopButton != null) _stopButton.text = L("btn.stop");
            if (_langButton != null) _langButton.text = L("btn.lang");
        }

        // ---- 迭代一：键盘快捷键 ----
        
        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            // 如果焦点在文本输入框，不拦截快捷键
            if (evt.target is TextField || evt.target is FloatField || evt.target is IntegerField)
                return;
            
            HandleKeyboardShortcut(evt);
        }
        
        private void ToggleInspectorWindow()
        {
            if (_inspectorWindow == null)
            {
                _inspectorWindow = PCGNodeInspectorWindow.Open();
                _inspectorWindow.BindGraphView(graphView);
                // 立即显示当前选中
                var currentSelected = graphView.GetSelectedNodeVisual();
                _inspectorWindow.InspectNode(currentSelected);
            }
            else
            {
                _inspectorWindow.Close();
                _inspectorWindow = null;
            }
        }

        /// <summary>
        /// 供 PCGNodeInspectorWindow.TryRebindGraphView() 调用，重新绑定 graphView 并刷新
        /// </summary>
        public void BindInspector(PCGNodeInspectorWindow inspector)
        {
            _inspectorWindow = inspector;
            inspector.BindGraphView(graphView);
            var currentSelected = graphView?.GetSelectedNodeVisual();
            if (currentSelected != null)
                inspector.InspectNode(currentSelected);
        }

        /// <summary>
        /// 供 Inspector Undo 使用，获取当前图数据对象
        /// </summary>
        public PCGGraphData GetCurrentGraphDataForUndo()
        {
            return currentGraph;
        }

        public PCGGraphView GetGraphView() => graphView;

        private void HandleKeyboardShortcut(KeyDownEvent evt)
        {
            // Ctrl+S: Save
            if (evt.keyCode == KeyCode.S && evt.ctrlKey && !evt.shiftKey)
            {
                SaveGraph();
                evt.StopPropagation();
            }
            // Ctrl+Shift+S: Save As
            else if (evt.keyCode == KeyCode.S && evt.ctrlKey && evt.shiftKey)
            {
                SaveAsGraph();
                evt.StopPropagation();
            }
            // Ctrl+Shift+F: Quick Select Faces on active scene object
            else if (evt.keyCode == KeyCode.F && evt.ctrlKey && evt.shiftKey)
            {
                var go = Selection.activeGameObject;
                if (go != null)
                    PCGToolkit.Tools.PCGQuickSelect.Launch(go);
                evt.StopPropagation();
            }
        }
        
        // ---- P1-T2: Live Feedback Mode ----

        private void StartSilentExecution()
        {
            if (graphView == null) return;
            var data = graphView.SaveToGraphData();
            if (data == null || data.Nodes.Count == 0) return;
            _silentExecutor.Start(data);
        }

        private void OnSilentExecutionProgress(int completed, int total)
        {
            if (_liveProgressFill != null)
                _liveProgressFill.style.width = new Length((float)completed / total * 100f, LengthUnit.Percent);
            if (_liveProgressLabel != null)
                _liveProgressLabel.text = $"Live: {completed}/{total} nodes...";
        }

        private void OnSilentExecutionCompleted(Dictionary<string, NodeExecutionResult> results, double totalMs)
        {
            if (_liveProgressLabel != null)
                _liveProgressLabel.text = $"Live: Done ({totalMs:F1}ms)";
            if (_liveProgressFill != null)
                _liveProgressFill.style.width = new Length(100, LengthUnit.Percent);

            // 2秒后淡出
            rootVisualElement.schedule.Execute(() =>
            {
                if (_silentExecutor != null && !_silentExecutor.IsRunning)
                {
                    if (_liveProgressLabel != null)
                        _liveProgressLabel.text = "Live: Idle";
                    if (_liveProgressFill != null)
                        _liveProgressFill.style.width = new Length(0, LengthUnit.Percent);
                }
            }).ExecuteLater(2000);

            // 预览结果到 Scene
            var previewGeo = _silentExecutor.GetLastTerminalGeometry();

            // 如果用户选中了某个节点，优先预览该节点
            var selectedNode = graphView?.GetSelectedNodeVisual();
            if (selectedNode != null && results != null && results.TryGetValue(selectedNode.NodeId, out var selResult))
                previewGeo = selResult.OutputGeometry ?? previewGeo;

            if (previewGeo != null)
                PCGScenePreview.InjectToScene(previewGeo, "PCG_LivePreview");
        }

        // ---- 迭代一：脏状态管理 ----
        
        private void MarkDirty()
        {
            if (_isDirty) return;
            _isDirty = true;
            UpdateWindowTitle();
        }
        
        private void ClearDirty()
        {
            _isDirty = false;
            UpdateWindowTitle();
        }
        
        private void UpdateWindowTitle()
        {
            string graphName = string.IsNullOrEmpty(_currentAssetPath) 
                ? "New Graph" 
                : System.IO.Path.GetFileNameWithoutExtension(_currentAssetPath);
            string dirtyMark = _isDirty ? "*" : "";
            titleContent = new GUIContent(
                $"PCG Node Editor v{PCGToolkit.Core.PCGToolkitVersion.Version} - {graphName}{dirtyMark}");
        }
        
        // ---- 迭代一：Undo/Redo 支持 ----
        
        private void OnUndoRedo()
        {
            if (currentGraph == null) return;
            
            // 从 currentGraph 重新加载视图
            graphView.LoadGraph(currentGraph);
            graphView.ClearAllHighlights();
            graphView.ClearAllExecutionTimes();
            
            Debug.Log("Undo/Redo performed, graph view refreshed.");
        }
  
        // ---- 执行操作 ----  
  
        private void OnExecuteClicked()  
        {
            // P1-T2: 手动执行时打断静默运行
            if (_silentExecutor != null && _silentExecutor.IsRunning)
                _silentExecutor.Cancel();

            if (_asyncExecutor.State == ExecutionState.Paused)  
            {  
                // 从暂停状态继续  
                _asyncExecutor.Resume();  
                SetToolbarButtonsEnabled(false);  
                return;  
            }  
  
            var data = GetCurrentGraphData();  
            if (data == null || data.Nodes.Count == 0)  
            {  
                Debug.LogWarning("No nodes to execute.");  
                return;  
            }  
  
            // 清除之前的执行状态  
            graphView.ClearAllHighlights();  
            graphView.ClearAllExecutionTimes();  
            _errorPanel.ClearErrors();
            _totalTimeLabel.text = "Total: --";
            _progressBar.value = 0;
  
            SetToolbarButtonsEnabled(false);  
            _asyncExecutor.Execute(data);  
        }  
  
        private void OnRunToSelectedClicked()  
        {  
            var selectedVisual = graphView.GetSelectedNodeVisual();  
            if (selectedVisual == null)  
            {  
                Debug.LogWarning("Please select a node first.");  
                return;  
            }  
  
            var data = GetCurrentGraphData();  
            if (data == null || data.Nodes.Count == 0)  
            {  
                Debug.LogWarning("No nodes to execute.");  
                return;  
            }  
  
            // 清除之前的执行状态  
            graphView.ClearAllHighlights();  
            graphView.ClearAllExecutionTimes(); 
            _errorPanel.ClearErrors();
            _totalTimeLabel.text = "Total: --";
            _progressBar.value = 0;
  
            SetToolbarButtonsEnabled(false);  
            _asyncExecutor.ExecuteToNode(data, selectedVisual.NodeId);  
        }  
  
        private void OnStopClicked()  
        {  
            _asyncExecutor.Stop();  
            graphView.ClearAllHighlights();  
            SetToolbarButtonsEnabled(true);  
            UpdateExecutionStateLabel("Stopped");  
        }

        // C3: Export All — 执行图，但所有 enabled=false 的节点会自己透传
        private void OnExportAllClicked()
        {
            var data = GetCurrentGraphData();
            if (data == null || data.Nodes.Count == 0) return;
            graphView.ClearAllHighlights();
            graphView.ClearAllExecutionTimes();
            _errorPanel?.ClearErrors();
            _asyncExecutor.Execute(data);
            Debug.Log("[PCGGraphEditorWindow] Export All triggered.");
        }
  
        // ---- 辅助方法 ----  
  
        private PCGGraphData GetCurrentGraphData()  
        {  
            // 始终从当前视图获取最新数据  
            var data = graphView.SaveToGraphData();  
            currentGraph = data;  
            return data;  
        }  
  
        private void SetToolbarButtonsEnabled(bool enabled)  
        {  
            // 执行中禁用 Execute 和 Run To Selected，但保留 Stop  
            _executeButton.SetEnabled(enabled || _asyncExecutor.State == ExecutionState.Paused);  
            _runToSelectedButton.SetEnabled(enabled);  
            _stopButton.SetEnabled(!enabled || _asyncExecutor.State == ExecutionState.Paused);  
        }  
  
        private void UpdateTotalTimeLabel(double? totalMs = null)  
        {  
            var ms = totalMs ?? _asyncExecutor.TotalElapsedMs;  
            _totalTimeLabel.text = $"Total: {ms:F1}ms ({_asyncExecutor.CompletedNodeCount}/{_asyncExecutor.TotalNodeCount})";  
        }
        
        private void UpdateProgressBar()
        {
            if (_asyncExecutor.TotalNodeCount > 0)
            {
                _progressBar.value = (float)_asyncExecutor.CompletedNodeCount / _asyncExecutor.TotalNodeCount * 100f;
            }
        }
  
        private void UpdateExecutionStateLabel(string state)  
        {  
            if (_executionStateLabel != null)  
                _executionStateLabel.text = state;  
        }  
  
        private void ShowPreviewForNode(string nodeId, NodeExecutionResult result)  
        {  
            if (result.Outputs == null || result.Outputs.Count == 0) return;  
  
            // 取第一个 Geometry 输出用于预览  
            PCGToolkit.Core.PCGGeometry previewGeo = null;  
            foreach (var kvp in result.Outputs)  
            {  
                if (kvp.Value != null)  
                {  
                    previewGeo = kvp.Value;  
                    break;  
                }  
            }  
  
            if (previewGeo == null) return;  
  
            // 打开或获取预览窗口  
            if (_previewWindow == null)  
                _previewWindow = PCGNodePreviewWindow.Open();  
  
            _previewWindow.SetPreviewData(nodeId, result.NodeType, previewGeo, result.ElapsedMs);  
            _previewWindow.Show();  
            _previewWindow.Focus();  
        }  
  
        // ---- 文件操作方法 ----  
  
        private void NewGraph()  
        {  
            if (_asyncExecutor.State != ExecutionState.Idle)  
                _asyncExecutor.Stop();  
  
            currentGraph = ScriptableObject.CreateInstance<PCGGraphData>();  
            currentGraph.GraphName = "New Graph";  
            graphView.LoadGraph(currentGraph);  
            graphView.ClearAllHighlights();  
            graphView.ClearAllExecutionTimes();  
            _totalTimeLabel.text = "Total: --";
            _progressBar.value = 0;
            UpdateExecutionStateLabel("Idle");
            
            // 迭代一：重置文件状态
            _currentAssetPath = null;
            ClearDirty();
        }  
  
        private void SaveGraph()  
        {
            // 迭代一：如果有当前路径，直接覆盖保存；否则走 SaveAs
            if (!string.IsNullOrEmpty(_currentAssetPath))
            {
                SaveToPath(_currentAssetPath);
                return;
            }
            
            // 没有路径时走 SaveAs
            SaveAsGraph();
        }
        
        private void SaveAsGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject(  
                "Save PCG Graph", "NewPCGGraph", "asset", "Save PCG Graph");  
            if (string.IsNullOrEmpty(path)) return;
            
            SaveToPath(path);
        }
        
        private void SaveToPath(string path)
        {
            var data = graphView.SaveToGraphData();  
            data.GraphName = System.IO.Path.GetFileNameWithoutExtension(path);  
  
            var existing = AssetDatabase.LoadAssetAtPath<PCGGraphData>(path);  
            if (existing != null)  
            {  
                EditorUtility.CopySerialized(data, existing);  
                AssetDatabase.SaveAssets();  
            }  
            else  
            {  
                AssetDatabase.CreateAsset(data, path);  
                AssetDatabase.SaveAssets();  
            }  
  
            AssetDatabase.Refresh();  
            currentGraph = AssetDatabase.LoadAssetAtPath<PCGGraphData>(path);
            _currentAssetPath = path;
            ClearDirty();
            Debug.Log($"Graph saved to {path}");
        }
  
        private void LoadGraph()  
        {  
            if (_asyncExecutor.State != ExecutionState.Idle)  
                _asyncExecutor.Stop();  
  
            var path = EditorUtility.OpenFilePanel("Load PCG Graph", "Assets", "asset");  
            if (string.IsNullOrEmpty(path)) return;  
  
            if (path.StartsWith(Application.dataPath))  
                path = "Assets" + path.Substring(Application.dataPath.Length);  
  
            var data = AssetDatabase.LoadAssetAtPath<PCGGraphData>(path);  
            if (data == null)  
            {  
                Debug.LogError($"Failed to load graph from {path}");  
                return;  
            }  
  
            currentGraph = data;  
            graphView.LoadGraph(data);  
            graphView.ClearAllHighlights();  
            graphView.ClearAllExecutionTimes();  
            _totalTimeLabel.text = "Total: --";
            _progressBar.value = 0;
            UpdateExecutionStateLabel("Idle");
            
            // 迭代一：设置当前路径并清除脏状态
            _currentAssetPath = path;
            ClearDirty();
            Debug.Log($"Graph loaded from {path}");  
        }  
  
        // 保留旧的 ExecuteGraph 作为同步执行的备选（不再从工具栏调用）  
        private void ExecuteGraph()  
        {  
            if (currentGraph == null)  
                currentGraph = graphView.SaveToGraphData();  
            else  
            {  
                var latestData = graphView.SaveToGraphData();  
                currentGraph.Nodes = latestData.Nodes;  
                currentGraph.Edges = latestData.Edges;  
            }  
  
            var executor = new PCGGraphExecutor(currentGraph);  
            executor.Execute();  
            Debug.Log("Graph execution completed (sync).");  
        }  
    }  
}