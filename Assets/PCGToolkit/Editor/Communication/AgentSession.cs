using System;
using System.Collections.Generic;
using PCGToolkit.Graph;
using UnityEngine;

namespace PCGToolkit.Communication
{
    public class AgentSession
    {
        private readonly Dictionary<string, PCGGraphData> _activeGraphs
            = new Dictionary<string, PCGGraphData>();

        public string CreateGraph(string graphName)
        {
            var graphData = ScriptableObject.CreateInstance<PCGGraphData>();
            graphData.GraphName = string.IsNullOrEmpty(graphName) ? "Untitled" : graphName;

            string graphId = Guid.NewGuid().ToString().Substring(0, 8);
            _activeGraphs[graphId] = graphData;
            return graphId;
        }

        public PCGGraphData GetGraph(string graphId)
        {
            _activeGraphs.TryGetValue(graphId, out var graph);
            return graph;
        }

        public void RemoveGraph(string graphId)
        {
            if (_activeGraphs.TryGetValue(graphId, out var graph))
            {
                if (graph != null)
                    UnityEngine.Object.DestroyImmediate(graph);
                _activeGraphs.Remove(graphId);
            }
        }

        public List<string> ListGraphIds()
        {
            return new List<string>(_activeGraphs.Keys);
        }

        public List<(string id, string name, int nodeCount)> ListGraphSummaries()
        {
            var result = new List<(string id, string name, int nodeCount)>();
            foreach (var kvp in _activeGraphs)
            {
                string name = kvp.Value != null ? kvp.Value.GraphName : "Unknown";
                int nodeCount = kvp.Value != null ? kvp.Value.Nodes.Count : 0;
                result.Add((kvp.Key, name, nodeCount));
            }
            return result;
        }

        public int Count => _activeGraphs.Count;
    }
}
