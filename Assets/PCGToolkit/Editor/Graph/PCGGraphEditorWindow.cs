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
  
        // ---- 执行调试相关 ----  
        private PCGAsyncGraphExecutor _asyncExecutor;  
        private PCGNodePreviewWindow _previewWindow;  
        private Label _totalTimeLabel;  
        private Label _executionStateLabel;  
        private Button _executeButton;  
        private Button _runToSelectedButton;  
        private Button _stopButton;  
  
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
        }  
  
        private void OnDisable()  
        {  
            // 停止执行  
            if (_asyncExecutor != null && _asyncExecutor.State != ExecutionState.Idle)  
                _asyncExecutor.Stop();  
  
            if (graphView != null)  
                rootVisualElement.Remove(graphView);  
        }  
  
        private void ConstructGraphView()  
        {  
            graphView = new PCGGraphView();  
            graphView.StretchToParentSize();  
            graphView.Initialize(this);  
            rootVisualElement.Add(graphView);  
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
                        visual.SetErrorState(true);  
                }  
  
                // 更新总时长  
                UpdateTotalTimeLabel();  
            };  
  
            // 整个图执行完成事件  
            _asyncExecutor.OnExecutionCompleted += totalMs =>  
            {  
                graphView.ClearAllHighlights();  
                UpdateTotalTimeLabel(totalMs);  
                UpdateExecutionStateLabel("Completed");  
                SetToolbarButtonsEnabled(true);  
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
  
        // ---- 原有文件操作方法 ----  
  
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
            UpdateExecutionStateLabel("Idle");  
        }  
  
        private void SaveGraph()  
        {  
            var path = EditorUtility.SaveFilePanelInProject(  
                "Save PCG Graph", "NewPCGGraph", "asset", "Save PCG Graph");  
            if (string.IsNullOrEmpty(path)) return;  
  
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
            UpdateExecutionStateLabel("Idle");  
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