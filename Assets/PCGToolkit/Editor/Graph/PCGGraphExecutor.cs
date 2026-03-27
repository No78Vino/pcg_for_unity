using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    public class PCGGraphExecutor
    {
        private PCGGraphData graphData;
        private PCGContext context;
        private HashSet<string> dirtyNodes = new HashSet<string>();
        private Dictionary<string, Dictionary<string, PCGGeometry>> _nodeOutputs =
            new Dictionary<string, Dictionary<string, PCGGeometry>>();

        public PCGGraphData GraphData => graphData;
        public PCGContext Context => context;

        public PCGGraphExecutor(PCGGraphData data)
        {
            graphData = data;
            context = new PCGContext();
        }

        public void Execute(bool continueOnError = false)
        {
            _nodeOutputs.Clear();
            context.NodeOutputCache.Clear();
            context.NodeCacheKeys.Clear();
            context.Logs.Clear();
            context.Errors.Clear();
            context.ContinueOnError = continueOnError;

            var sortedNodes = PCGGraphHelper.TopologicalSort(graphData);
            if (sortedNodes == null)
            {
                Debug.LogError("PCGGraphExecutor: Topological sort failed (cycle detected).");
                return;
            }

            int successCount = 0;
            int errorCount = 0;

            foreach (var nodeData in sortedNodes)
            {
                ExecuteNode(nodeData);

                var nodeErrors = context.GetNodeErrors(nodeData.NodeId);
                bool hasNodeError = nodeErrors.Any(e => e.Level >= PCGErrorLevel.Error);

                if (hasNodeError)
                {
                    errorCount++;

                    if (context.HasFatal || !continueOnError)
                    {
                        Debug.LogError(
                            $"PCGGraphExecutor: Execution stopped at {nodeData.NodeType} ({nodeData.NodeId}). " +
                            $"Completed: {successCount}, Errors: {errorCount}");
                        return;
                    }

                    if (!_nodeOutputs.ContainsKey(nodeData.NodeId))
                    {
                        _nodeOutputs[nodeData.NodeId] = new Dictionary<string, PCGGeometry>
                        {
                            { "geometry", new PCGGeometry() }
                        };
                    }

                    Debug.LogWarning(
                        $"PCGGraphExecutor: Error at {nodeData.NodeType}, continuing with empty geometry.");
                }
                else
                {
                    successCount++;
                }
            }

            Debug.Log($"PCGGraphExecutor: Execution completed. Success: {successCount}, Errors: {errorCount}, Total: {sortedNodes.Count}");
        }

        public void Execute(PCGContext externalContext)
        {
            context = externalContext;

            var sortedNodes = PCGGraphHelper.TopologicalSort(graphData);
            if (sortedNodes == null)
            {
                Debug.LogError("PCGGraphExecutor: Topological sort failed (cycle detected).");
                return;
            }

            foreach (var nodeData in sortedNodes)
            {
                ExecuteNode(nodeData);

                var nodeErrors = context.GetNodeErrors(nodeData.NodeId);
                bool hasNodeError = nodeErrors.Any(e => e.Level >= PCGErrorLevel.Error);

                if (hasNodeError && (context.HasFatal || !context.ContinueOnError))
                {
                    Debug.LogError($"PCGGraphExecutor: Execution stopped due to error at node {nodeData.NodeType} ({nodeData.NodeId})");
                    return;
                }
            }

            Debug.Log($"PCGGraphExecutor: Execution completed. {sortedNodes.Count} nodes executed.");
        }

        public void ExecuteIncremental()
        {
            if (dirtyNodes.Count == 0) return;

            // 从 dirty nodes 出发，沿下游传播，收集所有需要重新执行的节点
            var toExecute = new HashSet<string>(dirtyNodes);
            var queue = new Queue<string>(dirtyNodes);
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                foreach (var edge in graphData.Edges)
                {
                    if (edge.OutputNodeId == nodeId && !toExecute.Contains(edge.InputNodeId))
                    {
                        toExecute.Add(edge.InputNodeId);
                        queue.Enqueue(edge.InputNodeId);
                    }
                }
            }

            // 优化: 只对 dirty 子图做拓扑排序，而非全图
            var sortedSubgraph = PCGGraphHelper.TopologicalSortSubgraph(graphData, toExecute);
            if (sortedSubgraph == null || sortedSubgraph.Count == 0) return;

            foreach (var nodeData in sortedSubgraph)
            {
                ExecuteNode(nodeData);
            }

            dirtyNodes.Clear();
        }

        public void MarkDirty(string nodeId)
        {
            dirtyNodes.Add(nodeId);
            foreach (var edge in graphData.Edges)
            {
                if (edge.OutputNodeId == nodeId && !dirtyNodes.Contains(edge.InputNodeId))
                {
                    MarkDirty(edge.InputNodeId);
                }
            }
        }

        private void ExecuteNode(PCGNodeData nodeData)
        {
            var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);
            if (nodeTemplate == null)
            {
                Debug.LogError($"PCGGraphExecutor: Node type not found: {nodeData.NodeType}");
                return;
            }

            var nodeInstance = PCGNodeRegistry.GetOrCreateInstance(nodeData.NodeType)
                ?? (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());

            var inputGeometries = new Dictionary<string, PCGGeometry>();
            var inputPortCounts = new Dictionary<string, int>();
            foreach (var edge in graphData.Edges)
            {
                if (edge.InputNodeId == nodeData.NodeId)
                {
                    if (_nodeOutputs.TryGetValue(edge.OutputNodeId, out var outputs) &&
                        outputs.TryGetValue(edge.OutputPort, out var geo))
                    {
                        string portKey = edge.InputPort;
                        if (inputGeometries.ContainsKey(portKey))
                        {
                            if (!inputPortCounts.ContainsKey(portKey))
                                inputPortCounts[portKey] = 1;
                            inputPortCounts[portKey]++;
                            portKey = $"{edge.InputPort}_{inputPortCounts[portKey] - 1}";
                        }
                        inputGeometries[portKey] = geo;
                    }
                }
            }

            var parameters = new Dictionary<string, object>();
            foreach (var param in nodeData.Parameters)
            {
                parameters[param.Key] = PCGParamHelper.DeserializeParamValue(param);
            }

            foreach (var kvp in parameters)
            {
                if (kvp.Value is PCGSceneObjectRef sceneRef)
                {
                    var go = sceneRef.Resolve();
                    if (go != null)
                        context.SceneReferences[kvp.Key] = go;
                }
            }

            foreach (var edge in graphData.Edges)
            {
                if (edge.InputNodeId == nodeData.NodeId)
                {
                    var upstreamKey = $"{edge.OutputNodeId}.{edge.OutputPort}";
                    if (context.GlobalVariables.TryGetValue(upstreamKey, out var val))
                    {
                        parameters[edge.InputPort] = val;
                    }
                }
            }

            context.CurrentNodeId = nodeData.NodeId;

            // Cache query
            string cacheKey = PCGCacheManager.ComputeCacheKey(nodeData.NodeType, parameters, inputGeometries);
            context.NodeCacheKeys[nodeData.NodeId] = cacheKey;

            if (context.UseDiskCache && PCGCacheManager.TryGetGeometry(cacheKey, out var cachedGeo))
            {
                var cachedResult = new Dictionary<string, PCGGeometry> { { "geometry", cachedGeo } };
                _nodeOutputs[nodeData.NodeId] = cachedResult;
                foreach (var kvp in cachedResult)
                    context.CacheOutput($"{nodeData.NodeId}.{kvp.Key}", kvp.Value);
                return;
            }

            try
            {
                var result = nodeInstance.Execute(context, inputGeometries, parameters);

                if (result != null)
                {
                    _nodeOutputs[nodeData.NodeId] = result;
                    foreach (var kvp in result)
                    {
                        context.CacheOutput($"{nodeData.NodeId}.{kvp.Key}", kvp.Value);
                    }

                    // Write to disk cache
                    if (context.UseDiskCache)
                    {
                        foreach (var kvp in result)
                        {
                            if (kvp.Value != null && kvp.Value.Points.Count > 0)
                                PCGCacheManager.PutGeometry(cacheKey, kvp.Value, CachePersistence.Disk,
                                    nodeData.NodeType, graphData?.GraphName, nodeData.NodeId);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                context.LogError($"Exception executing node {nodeData.NodeType}: {e.Message}");

                if (context.ContinueOnError)
                {
                    _nodeOutputs[nodeData.NodeId] = new Dictionary<string, PCGGeometry>
                    {
                        { "geometry", new PCGGeometry() }
                    };
                }
            }
        }

        public PCGGeometry GetNodeOutput(string nodeId, string portName = "geometry")
        {
            return context.GetCachedOutput($"{nodeId}.{portName}");
        }

        public Dictionary<string, PCGGeometry> GetNodeAllOutputs(string nodeId)
        {
            _nodeOutputs.TryGetValue(nodeId, out var outputs);
            return outputs;
        }

        public void ClearCache()
        {
            context.ClearCache();
            _nodeOutputs.Clear();
            dirtyNodes.Clear();
        }
    }
}
