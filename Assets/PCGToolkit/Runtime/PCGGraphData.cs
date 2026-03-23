using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Graph
{
    [Serializable]
    public class PCGSerializedParameter
    {
        public string Key;
        public string ValueJson;
        public string ValueType;
    }

    [Serializable]
    public class PCGNodeData
    {
        public string NodeId;
        public string NodeType;
        public Vector2 Position;
        public List<PCGSerializedParameter> Parameters = new List<PCGSerializedParameter>();

        public void SetParameter(string key, object value)
        {
            var param = Parameters.Find(p => p.Key == key);
            if (param == null)
            {
                param = new PCGSerializedParameter { Key = key };
                Parameters.Add(param);
            }
            param.ValueType = value != null ? value.GetType().FullName : "null";
            param.ValueJson = value != null ? JsonUtility.ToJson(new JsonWrapper { Value = value.ToString() }) : "";
        }

        public string GetParameter(string key)
        {
            var param = Parameters.Find(p => p.Key == key);
            if (param == null) return null;
            return param.ValueJson;
        }
    }

    [Serializable]
    internal class JsonWrapper
    {
        public string Value;
    }

    [Serializable]
    public class PCGEdgeData
    {
        public string OutputNodeId;
        public string OutputPort;
        public string InputNodeId;
        public string InputPort;
    }
    
    [Serializable]
    public class PCGGroupData
    {
        public string GroupId;
        public string Title;
        public List<string> NodeIds = new List<string>();
        public Vector2 Position;
        public Vector2 Size;
    }
    
    [Serializable]
    public class PCGExposedParamInfo
    {
        public string NodeId;
        public string ParamName;
    }

    [Serializable]
    public class PCGStickyNoteData
    {
        public string NoteId;
        public string Title;
        public string Content;
        public Vector2 Position;
        public Vector2 Size;
    }

    [CreateAssetMenu(fileName = "NewPCGGraph", menuName = "PCG Toolkit/PCG Graph")]
    public class PCGGraphData : ScriptableObject
    {
        public const int CurrentVersion = 7;

        public int Version = CurrentVersion;
        public string GraphName = "New Graph";
        public List<PCGNodeData> Nodes = new List<PCGNodeData>();
        public List<PCGEdgeData> Edges = new List<PCGEdgeData>();
        public List<PCGGroupData> Groups = new List<PCGGroupData>();
        public List<PCGStickyNoteData> StickyNotes = new List<PCGStickyNoteData>();
        public List<PCGExposedParamInfo> ExposedParameters = new List<PCGExposedParamInfo>();

        private void OnEnable()
        {
            MigrateIfNeeded();
        }

        private void MigrateIfNeeded()
        {
            if (Version >= CurrentVersion) return;

            bool migrated = false;

            if (Version < 4)
            {
                if (Groups == null) Groups = new List<PCGGroupData>();
                if (StickyNotes == null) StickyNotes = new List<PCGStickyNoteData>();
                Debug.Log($"[PCGGraphData] Migrated '{GraphName}' from v{Version} -> v4 (Groups/StickyNotes)");
                migrated = true;
            }

            if (Version < 6)
            {
                if (ExposedParameters == null) ExposedParameters = new List<PCGExposedParamInfo>();
                Debug.Log($"[PCGGraphData] Migrated '{GraphName}' from v{Version} -> v6 (ExposedParameters)");
                migrated = true;
            }

            if (Version < 7)
            {
                migrated = true;
            }

            if (migrated)
            {
                Version = CurrentVersion;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"[PCGGraphData] Migration complete for '{GraphName}', now at v{Version}");
#endif
            }
        }

        public PCGNodeData AddNode(string nodeType, Vector2 position)
        {
            var data = new PCGNodeData
            {
                NodeId = Guid.NewGuid().ToString(),
                NodeType = nodeType,
                Position = position,
            };
            Nodes.Add(data);
            return data;
        }

        public void RemoveNode(string nodeId)
        {
            Nodes.RemoveAll(n => n.NodeId == nodeId);
            Edges.RemoveAll(e => e.OutputNodeId == nodeId || e.InputNodeId == nodeId);
        }

        public PCGEdgeData AddEdge(string outputNodeId, string outputPortName,
            string inputNodeId, string inputPortName)
        {
            var edge = new PCGEdgeData
            {
                OutputNodeId = outputNodeId,
                OutputPort = outputPortName,
                InputNodeId = inputNodeId,
                InputPort = inputPortName,
            };
            Edges.Add(edge);
            return edge;
        }

        public void Clear()
        {
            Nodes.Clear();
            Edges.Clear();
            Groups.Clear();
            StickyNotes.Clear();
            ExposedParameters.Clear();
        }

        public PCGGraphData Clone()
        {
            var copy = CreateInstance<PCGGraphData>();
            copy.Version = Version;
            copy.GraphName = GraphName;
            copy.Nodes = new List<PCGNodeData>();
            foreach (var n in Nodes)
            {
                var nd = new PCGNodeData
                {
                    NodeId = n.NodeId,
                    NodeType = n.NodeType,
                    Position = n.Position,
                };
                foreach (var p in n.Parameters)
                    nd.Parameters.Add(new PCGSerializedParameter { Key = p.Key, ValueType = p.ValueType, ValueJson = p.ValueJson });
                copy.Nodes.Add(nd);
            }
            copy.Edges = new List<PCGEdgeData>();
            foreach (var e in Edges)
                copy.Edges.Add(new PCGEdgeData { OutputNodeId = e.OutputNodeId, OutputPort = e.OutputPort, InputNodeId = e.InputNodeId, InputPort = e.InputPort });
            copy.ExposedParameters = new List<PCGExposedParamInfo>();
            foreach (var ep in ExposedParameters)
                copy.ExposedParameters.Add(new PCGExposedParamInfo { NodeId = ep.NodeId, ParamName = ep.ParamName });
            return copy;
        }
    }
}
