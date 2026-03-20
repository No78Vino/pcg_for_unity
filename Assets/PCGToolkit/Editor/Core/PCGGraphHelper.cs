using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 图操作工具类，提供拓扑排序等共享算法。
    /// </summary>
    public static class PCGGraphHelper
    {
        /// <summary>
        /// 对节点图进行拓扑排序（Kahn 算法）。
        /// 返回排序后的节点列表，如果存在环则返回 null。
        /// </summary>
        public static List<PCGNodeData> TopologicalSort(PCGGraphData graphData)
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
                Debug.LogError(
                    $"[PCGGraphHelper] Cycle detected! Sorted {sorted.Count} nodes out of {graphData.Nodes.Count}.");
                return null;
            }

            return sorted;
        }
    }
}