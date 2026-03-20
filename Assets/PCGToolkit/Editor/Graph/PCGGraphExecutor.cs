using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 节点图 DAG 执行引擎
    /// 支持拓扑排序、脏标记增量执行、缓存
    /// </summary>
    public class PCGGraphExecutor
    {
        private PCGGraphData graphData;
        private PCGContext context;
        private HashSet<string> dirtyNodes = new HashSet<string>();
        private Dictionary<string, Dictionary<string, PCGGeometry>> _nodeOutputs =
            new Dictionary<string, Dictionary<string, PCGGeometry>>();

        public PCGGraphExecutor(PCGGraphData data)
        {
            graphData = data;
            context = new PCGContext();
        }

        /// <summary>
        /// 执行整个节点图
        /// </summary>
        public void Execute()
        {
            _nodeOutputs.Clear();
            context.ClearCache();

            var sortedNodes = PCGGraphHelper.TopologicalSort(graphData);
            if (sortedNodes == null)
            {
                Debug.LogError("PCGGraphExecutor: Topological sort failed (cycle detected).");
                return;
            }

            foreach (var nodeData in sortedNodes)
            {
                ExecuteNode(nodeData);
                if (context.HasError)
                {
                    Debug.LogError(
                        $"PCGGraphExecutor: Execution stopped due to error at node {nodeData.NodeType} ({nodeData.NodeId})");
                    return;
                }
            }

            Debug.Log($"PCGGraphExecutor: Execution completed. {sortedNodes.Count} nodes executed.");
        }
        
        /// <summary>
        /// 使用外部上下文执行节点图（用于 SubGraph）
        /// </summary>
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
                if (context.HasError)
                {
                    Debug.LogError($"PCGGraphExecutor: Execution stopped due to error at node {nodeData.NodeType} ({nodeData.NodeId})");
                    return;
                }
            }

            Debug.Log($"PCGGraphExecutor: Execution completed. {sortedNodes.Count} nodes executed.");
        }

        /// <summary>
        /// 增量执行（仅执行脏节点及其下游）
        /// </summary>
        public void ExecuteIncremental()
        {
            if (dirtyNodes.Count == 0) return;

            // 收集所有需要重新执行的节点（脏节点 + 下游节点）
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

            var sortedNodes = TopologicalSort();
            if (sortedNodes == null) return;

            foreach (var nodeData in sortedNodes)
            {
                if (toExecute.Contains(nodeData.NodeId))
                {
                    ExecuteNode(nodeData);
                }
            }

            dirtyNodes.Clear();
        }

        /// <summary>
        /// 标记节点为脏（参数变更时调用）
        /// </summary>
        public void MarkDirty(string nodeId)
        {
            dirtyNodes.Add(nodeId);
            // 递归标记所有下游节点
            foreach (var edge in graphData.Edges)
            {
                if (edge.OutputNodeId == nodeId && !dirtyNodes.Contains(edge.InputNodeId))
                {
                    MarkDirty(edge.InputNodeId);
                }
            }
        }

        /// <summary>
        /// 拓扑排序（Kahn 算法）
        /// </summary>
        private List<PCGNodeData> TopologicalSort()
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
                // 数据从 OutputNode 流向 InputNode
                if (adjacency.ContainsKey(edge.OutputNodeId) && inDegree.ContainsKey(edge.InputNodeId))
                {
                    adjacency[edge.OutputNodeId].Add(edge.InputNodeId);
                    inDegree[edge.InputNodeId]++;
                }
            }

            // BFS：从入度为 0 的节点开始
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

            // 如果排序结果数量不等于节点数量，说明存在环
            if (sorted.Count != graphData.Nodes.Count)
            {
                Debug.LogError(
                    $"PCGGraphExecutor: Cycle detected! Sorted {sorted.Count} nodes out of {graphData.Nodes.Count}.");
                return null;
            }

            return sorted;
        }

        /// <summary>
        /// 执行单个节点
        /// </summary>
        private void ExecuteNode(PCGNodeData nodeData)
        {
            var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);
            if (nodeTemplate == null)
            {
                Debug.LogError($"PCGGraphExecutor: Node type not found: {nodeData.NodeType}");
                return;
            }

            // 创建新实例执行（避免污染注册表中的模板实例）
            var nodeInstance = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());

            // 收集输入几何体（从上游节点的输出缓存中获取）
            var inputGeometries = new Dictionary<string, PCGGeometry>();
            foreach (var edge in graphData.Edges)
            {
                if (edge.InputNodeId == nodeData.NodeId)
                {
                    if (_nodeOutputs.TryGetValue(edge.OutputNodeId, out var outputs) &&
                        outputs.TryGetValue(edge.OutputPortName, out var geo))
                    {
                        inputGeometries[edge.InputPortName] = geo;
                    }
                }
            }

            // 收集参数（反序列化为正确类型）
            var parameters = new Dictionary<string, object>();
            foreach (var param in nodeData.Parameters)
            {
                parameters[param.Key] = PCGParamHelper.DeserializeParamValue(param);
            }

            // 从上游 Const 节点的 GlobalVariables 中获取值
            foreach (var edge in graphData.Edges)
            {
                if (edge.InputNodeId == nodeData.NodeId)
                {
                    var upstreamKey = $"{edge.OutputNodeId}.{edge.OutputPortName}";
                    if (context.GlobalVariables.TryGetValue(upstreamKey, out var val))
                    {
                        parameters[edge.InputPortName] = val;
                    }
                }
            }

            // 执行节点
            context.CurrentNodeId = nodeData.NodeId;
            try
            {
                var result = nodeInstance.Execute(context, inputGeometries, parameters);

                // 缓存输出
                if (result != null)
                {
                    _nodeOutputs[nodeData.NodeId] = result;
                    foreach (var kvp in result)
                    {
                        context.CacheOutput($"{nodeData.NodeId}.{kvp.Key}", kvp.Value);
                    }
                }
            }
            catch (Exception e)
            {
                context.LogError($"Exception executing node {nodeData.NodeType}: {e.Message}");
            }
        }

        /// <summary>
        /// 获取节点的执行结果
        /// </summary>
        public PCGGeometry GetNodeOutput(string nodeId, string portName = "geometry")
        {
            return context.GetCachedOutput($"{nodeId}.{portName}");
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            context.ClearCache();
            _nodeOutputs.Clear();
            dirtyNodes.Clear();
        }
    }
}