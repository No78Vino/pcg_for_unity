using System;  
using System.Collections.Generic;  
using System.Diagnostics;  
using System.Linq;  
using UnityEditor;  
using UnityEngine;  
using PCGToolkit.Core;  
using Debug = UnityEngine.Debug;

namespace PCGToolkit.Graph
{
    /// <summary>  
    /// 执行状态枚举  
    /// </summary>  
    public enum ExecutionState
    {
        Idle,
        Running,
        Paused,
    }

    /// <summary>  
    /// 单个节点的执行阶段  
    /// </summary>  
    public enum NodeExecutionPhase
    {
        Highlight, // 第 1 帧：高亮节点  
        Execute, // 第 2 帧：执行节点逻辑  
        ShowResult, // 第 3 帧：显示结果，推进到下一个  
    }

    /// <summary>  
    /// 节点执行结果  
    /// </summary>  
    public class NodeExecutionResult
    {
        public string NodeId;
        public string NodeType;
        public double ElapsedMs;
        public Dictionary<string, PCGGeometry> Outputs;
        public bool Success;
        public string ErrorMessage;
    }

    /// <summary>  
    /// 分帧异步执行引擎。  
    /// 通过 EditorApplication.update 驱动，每个节点分 3 帧执行：  
    ///   帧1 = 高亮节点  
    ///   帧2 = 执行 Execute()  
    ///   帧3 = 显示耗时，推进下一个  
    /// </summary>  
    public class PCGAsyncGraphExecutor
    {
        // ---- 事件 ----  
        /// <summary>节点开始执行（高亮阶段）</summary>  
        public event Action<string> OnNodeHighlight;

        /// <summary>节点执行完成，传递结果</summary>  
        public event Action<NodeExecutionResult> OnNodeCompleted;

        /// <summary>整个图执行完成，传递总耗时 ms</summary>  
        public event Action<double> OnExecutionCompleted;

        /// <summary>执行暂停（Run To Node 到达目标）</summary>  
        public event Action<string, NodeExecutionResult> OnExecutionPaused;

        /// <summary>执行状态变更</summary>  
        public event Action<ExecutionState> OnStateChanged;

        // ---- 状态 ----  
        public ExecutionState State { get; private set; } = ExecutionState.Idle;

        private PCGGraphData _graphData;
        private PCGContext _context;
        private List<PCGNodeData> _sortedNodes;
        private int _currentNodeIndex;
        private NodeExecutionPhase _currentPhase;
        private string _pauseAtNodeId; // Run To Node 的目标节点 ID，null 表示不暂停  

        private Dictionary<string, Dictionary<string, PCGGeometry>> _nodeOutputs;
        private Stopwatch _nodeStopwatch;
        private Stopwatch _totalStopwatch;
        private double _lastNodeElapsedMs;

        // ---- 公共属性 ----  
        public double TotalElapsedMs => _totalStopwatch?.Elapsed.TotalMilliseconds ?? 0;
        public int TotalNodeCount => _sortedNodes?.Count ?? 0;
        public int CompletedNodeCount => _currentNodeIndex;

        /// <summary>  
        /// 获取指定节点的输出结果  
        /// </summary>  
        public Dictionary<string, PCGGeometry> GetNodeOutput(string nodeId)
        {
            if (_nodeOutputs != null && _nodeOutputs.TryGetValue(nodeId, out var outputs))
                return outputs;
            return null;
        }
        
        // 迭代三：获取节点完整执行结果（用于预览）
        private Dictionary<string, NodeExecutionResult> _nodeResults = new Dictionary<string, NodeExecutionResult>();
        
        public NodeExecutionResult GetNodeResult(string nodeId)
        {
            if (_nodeResults.TryGetValue(nodeId, out var result))
                return result;
            return null;
        }

        /// <summary>  
        /// 执行整个图  
        /// </summary>  
        public void Execute(PCGGraphData graphData)
        {
            _pauseAtNodeId = null;
            StartExecution(graphData);
        }

        /// <summary>  
        /// 执行到指定节点后暂停  
        /// </summary>  
        public void ExecuteToNode(PCGGraphData graphData, string targetNodeId)
        {
            _pauseAtNodeId = targetNodeId;
            StartExecution(graphData);
        }

        /// <summary>  
        /// 从暂停状态继续执行  
        /// </summary>  
        public void Resume()
        {
            if (State != ExecutionState.Paused) return;
            _pauseAtNodeId = null;
            SetState(ExecutionState.Running);
            EditorApplication.update += Tick;
        }

        /// <summary>  
        /// 停止执行  
        /// </summary>  
        public void Stop()
        {
            if (State == ExecutionState.Idle) return;
            EditorApplication.update -= Tick;
            _totalStopwatch?.Stop();
            SetState(ExecutionState.Idle);
            Debug.Log($"[PCGAsyncExecutor] Execution stopped. Completed {_currentNodeIndex}/{TotalNodeCount} nodes.");
        }

        // ---- 内部方法 ----  

        private void StartExecution(PCGGraphData graphData)
        {
            if (State != ExecutionState.Idle)
            {
                Stop();
            }

            _graphData = graphData;
            _context = new PCGContext();
            _nodeOutputs = new Dictionary<string, Dictionary<string, PCGGeometry>>();
            _currentNodeIndex = 0;
            _currentPhase = NodeExecutionPhase.Highlight;

            // 拓扑排序  
            _sortedNodes = TopologicalSort(graphData);
            if (_sortedNodes == null || _sortedNodes.Count == 0)
            {
                Debug.LogError("[PCGAsyncExecutor] No nodes to execute or cycle detected.");
                return;
            }

            _totalStopwatch = Stopwatch.StartNew();
            _nodeStopwatch = new Stopwatch();

            SetState(ExecutionState.Running);
            EditorApplication.update += Tick;
        }

        private void SetState(ExecutionState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>  
        /// 每帧 tick，驱动执行状态机  
        /// </summary>  
        private void Tick()
        {
            if (State != ExecutionState.Running) return;
            if (_currentNodeIndex >= _sortedNodes.Count)
            {
                // 全部执行完毕  
                FinishExecution();
                return;
            }

            var nodeData = _sortedNodes[_currentNodeIndex];

            switch (_currentPhase)
            {
                case NodeExecutionPhase.Highlight:
                    // 帧 1：高亮节点  
                    OnNodeHighlight?.Invoke(nodeData.NodeId);
                    _currentPhase = NodeExecutionPhase.Execute;
                    // 强制刷新编辑器界面  
                    SceneView.RepaintAll();
                    break;

                case NodeExecutionPhase.Execute:
                    // 帧 2：执行节点  
                    var result = ExecuteNodeInternal(nodeData);
                    _lastNodeElapsedMs = result.ElapsedMs;

                    if (!result.Success)
                    {
                        Debug.LogError($"[PCGAsyncExecutor] Node {nodeData.NodeType} failed: {result.ErrorMessage}");
                        _nodeResults[nodeData.NodeId] = result; // 迭代三：保存失败结果
                        OnNodeCompleted?.Invoke(result);
                        Stop();
                        return;
                    }

                    _currentPhase = NodeExecutionPhase.ShowResult;
                    break;

                case NodeExecutionPhase.ShowResult:
                    // 帧 3：显示结果，推进  
                    var completedResult = new NodeExecutionResult
                    {
                        NodeId = nodeData.NodeId,
                        NodeType = nodeData.NodeType,
                        ElapsedMs = _lastNodeElapsedMs,
                        Outputs = GetNodeOutput(nodeData.NodeId),
                        Success = true,
                    };
                    _nodeResults[nodeData.NodeId] = completedResult; // 迭代三：保存成功结果
                    OnNodeCompleted?.Invoke(completedResult);

                    // 检查是否需要在此节点暂停  
                    if (_pauseAtNodeId != null && nodeData.NodeId == _pauseAtNodeId)
                    {
                        EditorApplication.update -= Tick;
                        SetState(ExecutionState.Paused);
                        OnExecutionPaused?.Invoke(nodeData.NodeId, completedResult);
                        return;
                    }

                    // 推进到下一个节点  
                    _currentNodeIndex++;
                    _currentPhase = NodeExecutionPhase.Highlight;

                    if (_currentNodeIndex >= _sortedNodes.Count)
                    {
                        FinishExecution();
                    }

                    break;
            }
        }

        private void FinishExecution()
        {
            EditorApplication.update -= Tick;
            _totalStopwatch.Stop();
            var totalMs = _totalStopwatch.Elapsed.TotalMilliseconds;
            SetState(ExecutionState.Idle);
            OnExecutionCompleted?.Invoke(totalMs);
            Debug.Log($"[PCGAsyncExecutor] Execution completed. Total: {totalMs:F1}ms, {_sortedNodes.Count} nodes.");
        }

        private NodeExecutionResult ExecuteNodeInternal(PCGNodeData nodeData)
        {
            var result = new NodeExecutionResult
            {
                NodeId = nodeData.NodeId,
                NodeType = nodeData.NodeType,
            };

            var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);
            if (nodeTemplate == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Node type not found: {nodeData.NodeType}";
                return result;
            }

            var nodeInstance = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());

            // 收集输入几何体  
            var inputGeometries = new Dictionary<string, PCGGeometry>();
            foreach (var edge in _graphData.Edges)
            {
                if (edge.InputNodeId == nodeData.NodeId)
                {
                    // 先尝试从几何体输出中获取  
                    if (_nodeOutputs.TryGetValue(edge.OutputNodeId, out var outputs) &&
                        outputs.TryGetValue(edge.OutputPortName, out var geo))
                    {
                        inputGeometries[edge.InputPortName] = geo;
                    }
                }
            }

            // 收集参数：先从序列化的默认值中获取  
            var parameters = new Dictionary<string, object>();
            foreach (var param in nodeData.Parameters)
            {
                parameters[param.Key] = DeserializeParamValue(param);
            }

            // 收集参数：从上游 Const 节点的 GlobalVariables 中获取值  
            // （Const 节点将值存入 ctx.GlobalVariables["{nodeId}.value"]）  
            foreach (var edge in _graphData.Edges)
            {
                if (edge.InputNodeId == nodeData.NodeId)
                {
                    var upstreamKey = $"{edge.OutputNodeId}.{edge.OutputPortName}";
                    if (_context.GlobalVariables.TryGetValue(upstreamKey, out var val))
                    {
                        // 上游通过 GlobalVariables 传递的值覆盖默认值  
                        parameters[edge.InputPortName] = val;
                    }
                }
            }

            // 执行并计时  
            _context.CurrentNodeId = nodeData.NodeId;
            _nodeStopwatch.Restart();

            try
            {
                var outputs = nodeInstance.Execute(_context, inputGeometries, parameters);
                _nodeStopwatch.Stop();

                result.ElapsedMs = _nodeStopwatch.Elapsed.TotalMilliseconds;
                result.Outputs = outputs;
                result.Success = true;

                if (outputs != null)
                {
                    _nodeOutputs[nodeData.NodeId] = outputs;
                    foreach (var kvp in outputs)
                    {
                        _context.CacheOutput($"{nodeData.NodeId}.{kvp.Key}", kvp.Value);
                    }
                }
            }
            catch (Exception e)
            {
                _nodeStopwatch.Stop();
                result.ElapsedMs = _nodeStopwatch.Elapsed.TotalMilliseconds;
                result.Success = false;
                result.ErrorMessage = e.Message;
            }

            return result;
        }

        /// <summary>  
        /// 反序列化参数值（与 PCGGraphView 中的逻辑一致）  
        /// </summary>  
        private object DeserializeParamValue(PCGSerializedParameter param)
        {
            try
            {
                switch (param.ValueType)
                {
                    case "float":
                        return float.Parse(param.ValueJson,
                            System.Globalization.CultureInfo.InvariantCulture);
                    case "int":
                        return int.Parse(param.ValueJson);
                    case "bool":
                        return bool.Parse(param.ValueJson);
                    case "string":
                        return param.ValueJson;
                    case "Vector3":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 3)
                        {
                            return new Vector3(
                                float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
                        }

                        return Vector3.zero;
                    }
                    case "Color":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 4)
                        {
                            return new Color(
                                float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
                        }

                        return Color.white;
                    }
                    case "null":
                    case null:
                    case "":
                        return param.ValueJson; // fallback: 返回原始字符串  
                    default:
                        return param.ValueJson;
                }
            }
            catch
            {
                return param.ValueJson;
            }
        }

        /// <summary>  
        /// 拓扑排序（Kahn 算法）  
        /// </summary>  
        private List<PCGNodeData> TopologicalSort(PCGGraphData graphData)
        {
            var nodeMap = new Dictionary<string, PCGNodeData>();
            var inDegree = new Dictionary<string, int>();
            var adjacency = new Dictionary<string, List<string>>();

            foreach (var node in graphData.Nodes)
            {
                nodeMap[node.NodeId] = node;
                inDegree[node.NodeId] = 0;
                adjacency[node.NodeId] = new List<string>();
            }

            foreach (var edge in graphData.Edges)
            {
                if (adjacency.ContainsKey(edge.OutputNodeId) && inDegree.ContainsKey(edge.InputNodeId))
                {
                    adjacency[edge.OutputNodeId].Add(edge.InputNodeId);
                    inDegree[edge.InputNodeId]++;
                }
            }

            var queue = new Queue<string>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                    queue.Enqueue(kvp.Key);
            }

            var sorted = new List<PCGNodeData>();
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                sorted.Add(nodeMap[nodeId]);
                foreach (var neighbor in adjacency[nodeId])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                        queue.Enqueue(neighbor);
                }
            }

            if (sorted.Count != graphData.Nodes.Count)
            {
                Debug.LogError($"[PCGAsyncExecutor] Cycle detected! Sorted {sorted.Count}/{graphData.Nodes.Count}");
                return null;
            }

            return sorted;
        }
    }
}