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
  
        // ---- 执行调试相关 ----  
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
  
        [MenuItem("PCG Toolkit/Node Editor")]  
        public static void OpenWindow()  
        {  
            var window = GetWindow<PCGGraphEditorWindow>();  
            window.titleContent = new GUIContent("PCG Node Editor");  
            window.minSize = new Vector2(800, 600);  
        }  
  
        private void OnEnable()  
        {  
            ConstructGraphView();  
            GenerateToolbar();  
            InitializeExecutor();
            
            // 迭代一：注册 Undo/Redo 回调
            Undo.undoRedoPerformed += OnUndoRedo;
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
            graphView.StretchToParentSize();  
            graphView.Initialize(this);
            _mainContainer.Add(graphView);
            
            // 迭代一：注册脏状态回调
            graphView.OnGraphChanged += MarkDirty;
            
            // 迭代三：注册节点点击预览回调
            graphView.OnNodeClicked += OnNodeClickedForPreview;
            
            // 迭代三：创建错误面板（默认隐藏）
            _errorPanel = new PCGErrorPanel();
            _errorPanel.style.display = DisplayStyle.None;
            _errorPanel.OnErrorClicked += OnErrorClicked;
            _mainContainer.Add(_errorPanel);
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
                UpdateExecutionStateLabel("Completed");  
                SetToolbarButtonsEnabled(true);
                _progressBar.value = 100f;
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
  
            // ---- 文件操作按钮 ----  
            var newButton = new Button(() => NewGraph()) { text = "New" };  
            toolbar.Add(newButton);  
  
            var saveButton = new Button(() => SaveGraph()) { text = "Save" };  
            toolbar.Add(saveButton);  
  
            // 迭代一：新增 Save As 按钮
            var saveAsButton = new Button(() => SaveAsGraph()) { text = "Save As" };  
            toolbar.Add(saveAsButton);
  
            var loadButton = new Button(() => LoadGraph()) { text = "Load" };  
            toolbar.Add(loadButton);  
  
            // ---- 分隔 ----  
            toolbar.Add(new ToolbarSpacer());  
  
            // ---- 执行按钮 ----  
            _executeButton = new Button(() => OnExecuteClicked()) { text = "Execute" };  
            _executeButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f));  
            toolbar.Add(_executeButton);  
  
            _runToSelectedButton = new Button(() => OnRunToSelectedClicked()) { text = "Run To Selected" };  
            _runToSelectedButton.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.15f));  
            toolbar.Add(_runToSelectedButton);  
  
            _stopButton = new Button(() => OnStopClicked()) { text = "Stop" };  
            _stopButton.style.backgroundColor = new StyleColor(new Color(0.5f, 0.2f, 0.2f));  
            toolbar.Add(_stopButton);  
  
            // ---- 分隔 ----  
            toolbar.Add(new ToolbarSpacer());  
  
            // ---- 状态标签 ----  
            _executionStateLabel = new Label("Idle")  
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
  
            // ---- 弹性空间 ----  
            var spacer = new VisualElement();  
            spacer.style.flexGrow = 1;  
            toolbar.Add(spacer);  
  
            // ---- 总时长标签（右侧） ----  
            _totalTimeLabel = new Label("Total: --")  
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
  
            rootVisualElement.Add(toolbar);  
        }
        
        // ---- 迭代一：键盘快捷键 ----
        private void Update()
        {
            // 在编辑器窗口中获得焦点时处理快捷键
            if (hasFocus)
            {
                var e = Event.current;
                if (e != null && e.type == EventType.KeyDown)
                {
                    HandleKeyboardShortcut(e);
                }
            }
        }
        
        private void HandleKeyboardShortcut(Event e)
        {
            // Ctrl+S: Save
            if (e.keyCode == KeyCode.S && e.control && !e.shift)
            {
                SaveGraph();
                e.Use();
            }
            // Ctrl+Shift+S: Save As
            else if (e.keyCode == KeyCode.S && e.control && e.shift)
            {
                SaveAsGraph();
                e.Use();
            }
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
            titleContent = new GUIContent($"PCG Node Editor - {graphName}{dirtyMark}");
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