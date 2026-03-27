using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 静默执行器，用于 Live Feedback Mode。
    /// 跳过 UI 动画，支持取消和时间预算控制，每帧执行多个节点。
    /// </summary>
    public class PCGSilentExecutor
    {
        private PCGGraphData _graphData;
        private PCGContext _context;
        private List<PCGNodeData> _sortedNodes;
        private int _currentNodeIndex;
        private Dictionary<string, Dictionary<string, PCGGeometry>> _nodeOutputs;
        private Dictionary<string, NodeExecutionResult> _nodeResults;
        private volatile bool _cancellationRequested;
        private bool _isRunning;
        private Stopwatch _totalStopwatch;
        private Stopwatch _nodeStopwatch;
        private Dictionary<string, List<PCGEdgeData>> _inputEdgeMap;
        private int _nodesPerTick = 5;
        private double _timeBudgetMs = 8.0;

        public event Action<int, int> OnProgressChanged;
        public event Action<Dictionary<string, NodeExecutionResult>, double> OnSilentExecutionCompleted;
        public event Action OnSilentExecutionCancelled;
        public event Action<string> OnSilentExecutionFailed;

        public bool IsRunning => _isRunning;
        public float Progress => _sortedNodes?.Count > 0 ? (float)_currentNodeIndex / _sortedNodes.Count : 0f;

        public void Start(PCGGraphData graphData, string stopAtNodeId = null)
        {
            if (_isRunning) Cancel();

            _graphData = graphData.Clone();
            _context = new PCGContext();
            _context.UseDiskCache = true;

            _sortedNodes = PCGGraphHelper.TopologicalSort(_graphData);
            if (_sortedNodes == null || _sortedNodes.Count == 0)
            {
                OnSilentExecutionFailed?.Invoke("No nodes to execute or cycle detected.");
                return;
            }

            _sortedNodes = _sortedNodes.Where(node =>
            {
                var template = PCGNodeRegistry.GetNode(node.NodeType);
                return template != null && template.Category != PCGNodeCategory.Output;
            }).ToList();

            if (_sortedNodes.Count == 0)
            {
                OnSilentExecutionFailed?.Invoke("No executable nodes after filtering Output category.");
                return;
            }

            if (stopAtNodeId != null)
            {
                int stopIndex = _sortedNodes.FindIndex(n => n.NodeId == stopAtNodeId);
                if (stopIndex >= 0)
                    _sortedNodes = _sortedNodes.Take(stopIndex + 1).ToList();
            }

            if (_sortedNodes.Count == 0)
            {
                OnSilentExecutionFailed?.Invoke("Target node not found in executable nodes.");
                return;
            }

            _inputEdgeMap = new Dictionary<string, List<PCGEdgeData>>();
            foreach (var node in _sortedNodes)
                _inputEdgeMap[node.NodeId] = new List<PCGEdgeData>();
            foreach (var edge in _graphData.Edges)
            {
                if (_inputEdgeMap.ContainsKey(edge.InputNodeId))
                    _inputEdgeMap[edge.InputNodeId].Add(edge);
            }

            _currentNodeIndex = 0;
            _cancellationRequested = false;
            _nodeOutputs = new Dictionary<string, Dictionary<string, PCGGeometry>>();
            _nodeResults = new Dictionary<string, NodeExecutionResult>();
            _totalStopwatch = Stopwatch.StartNew();
            _nodeStopwatch = new Stopwatch();
            _isRunning = true;

            EditorApplication.update += Tick;
        }

        public void Cancel()
        {
            _cancellationRequested = true;
        }

        public void Stop()
        {
            EditorApplication.update -= Tick;
            _isRunning = false;
            _totalStopwatch?.Stop();
        }

        private void Tick()
        {
            if (_cancellationRequested)
            {
                Stop();
                OnSilentExecutionCancelled?.Invoke();
                return;
            }

            double tickStart = EditorApplication.timeSinceStartup;

            while (_currentNodeIndex < _sortedNodes.Count)
            {
                if (_cancellationRequested) break;
                if ((EditorApplication.timeSinceStartup - tickStart) * 1000.0 > _timeBudgetMs) break;

                ExecuteNodeSilent(_sortedNodes[_currentNodeIndex]);
                _currentNodeIndex++;
                OnProgressChanged?.Invoke(_currentNodeIndex, _sortedNodes.Count);
            }

            if (_currentNodeIndex >= _sortedNodes.Count && !_cancellationRequested)
                FinishExecution();
        }

        private void ExecuteNodeSilent(PCGNodeData nodeData)
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
                _nodeResults[nodeData.NodeId] = result;
                return;
            }

            var nodeInstance = PCGNodeRegistry.GetOrCreateInstance(nodeData.NodeType)
                ?? (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());

            var inputGeometries = new Dictionary<string, PCGGeometry>();
            if (_inputEdgeMap.TryGetValue(nodeData.NodeId, out var edges))
            {
                foreach (var edge in edges)
                {
                    if (_nodeOutputs.TryGetValue(edge.OutputNodeId, out var outputs) &&
                        outputs.TryGetValue(edge.OutputPort, out var geo))
                    {
                        inputGeometries[edge.InputPort] = geo;
                    }
                }
            }

            var parameters = new Dictionary<string, object>();
            foreach (var param in nodeData.Parameters)
                parameters[param.Key] = PCGParamHelper.DeserializeParamValue(param);

            foreach (var kvp in parameters)
            {
                if (kvp.Value is PCGSceneObjectRef sceneRef)
                {
                    var go = sceneRef.Resolve();
                    if (go != null)
                        _context.SceneReferences[kvp.Key] = go;
                }
            }

            if (_inputEdgeMap.TryGetValue(nodeData.NodeId, out var gvEdges))
            {
                foreach (var edge in gvEdges)
                {
                    var upstreamKey = $"{edge.OutputNodeId}.{edge.OutputPort}";
                    if (_context.GlobalVariables.TryGetValue(upstreamKey, out var val))
                        parameters[edge.InputPort] = val;
                }
            }

            _context.CurrentNodeId = nodeData.NodeId;

            string cacheKey = PCGCacheManager.ComputeCacheKey(nodeData.NodeType, parameters, inputGeometries);
            if (_context.UseDiskCache && PCGCacheManager.TryGetGeometry(cacheKey, out var cachedGeo))
            {
                result.ElapsedMs = 0;
                result.Success = true;
                result.Outputs = new Dictionary<string, PCGGeometry> { { "geometry", cachedGeo } };
                _nodeOutputs[nodeData.NodeId] = result.Outputs;
                foreach (var kvp in result.Outputs)
                    _context.CacheOutput($"{nodeData.NodeId}.{kvp.Key}", kvp.Value);
                _nodeResults[nodeData.NodeId] = result;
                return;
            }

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
                        _context.CacheOutput($"{nodeData.NodeId}.{kvp.Key}", kvp.Value);
                    if (_context.UseDiskCache)
                    {
                        foreach (var kvp in outputs)
                        {
                            if (kvp.Value != null && kvp.Value.Points.Count > 0)
                                PCGCacheManager.PutGeometry(cacheKey, kvp.Value, CachePersistence.Disk,
                                    nodeData.NodeType, null, nodeData.NodeId);
                        }
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
            _nodeResults[nodeData.NodeId] = result;
        }

        private void FinishExecution()
        {
            Stop();
            OnSilentExecutionCompleted?.Invoke(_nodeResults, _totalStopwatch.Elapsed.TotalMilliseconds);
        }

        public PCGGeometry GetLastTerminalGeometry()
        {
            if (_sortedNodes == null) return null;
            for (int i = _sortedNodes.Count - 1; i >= 0; i--)
            {
                if (_nodeResults.TryGetValue(_sortedNodes[i].NodeId, out var result) && result.OutputGeometry != null)
                    return result.OutputGeometry;
            }
            return null;
        }
    }
}
