using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    [AddComponentMenu("PCG Toolkit/PCG Graph Runner")]
    public class PCGGraphRunner : MonoBehaviour
    {
        [Header("Graph Asset")]
        public PCGGraphData GraphAsset;

        [Header("Exposed Parameters")]
        public List<PCGExposedParam> ExposedParams = new List<PCGExposedParam>();

        [Header("Output")]
        [Tooltip("执行后将结果网格放置到此 GameObject（留空则创建子物体）")]
        public GameObject OutputTarget;
        [Tooltip("是否在 Start() 时自动执行")]
        public bool RunOnStart = false;
        [Tooltip("是否将输出 Mesh 实例化为子 GameObject")]
        public bool InstantiateOutput = true;

        [System.NonSerialized]
        public PCGGeometry LastOutput;

        private void Start()
        {
            if (RunOnStart) Run();
        }

        public void Run()
        {
            if (GraphAsset == null)
            {
                Debug.LogError("[PCGGraphRunner] GraphAsset is not assigned.");
                return;
            }

            var dataCopy = GraphAsset.Clone();

            foreach (var ep in ExposedParams)
            {
                var nodeData = dataCopy.Nodes.Find(n => n.NodeId == ep.NodeId);
                if (nodeData == null) continue;

                string valJson = SerializeValue(ep);
                var existing = nodeData.Parameters.Find(p => p.Key == ep.ParamName);
                if (existing != null)
                {
                    existing.ValueJson = valJson;
                    existing.ValueType = ep.ValueType;
                }
                else
                {
                    nodeData.Parameters.Add(new PCGSerializedParameter
                        { Key = ep.ParamName, ValueType = ep.ValueType, ValueJson = valJson });
                }
            }

            var executor = new PCGGraphExecutor(dataCopy);
            executor.Execute();

            foreach (var nodeData in dataCopy.Nodes)
            {
                var geo = executor.GetNodeOutput(nodeData.NodeId, "geometry");
                if (geo != null && geo.Points.Count > 0)
                    LastOutput = geo;
            }

            if (InstantiateOutput && LastOutput != null)
                ApplyOutputToScene(LastOutput);
        }

        private void ApplyOutputToScene(PCGGeometry geo)
        {
            var mesh = PCGGeometryToMesh.Convert(geo);
            var target = OutputTarget != null ? OutputTarget : new GameObject("PCG_Output");
            if (target.transform.parent != transform)
                target.transform.SetParent(transform);

            var mf = target.GetComponent<MeshFilter>() ?? target.AddComponent<MeshFilter>();
            var mr = target.GetComponent<MeshRenderer>() ?? target.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (OutputTarget == null) OutputTarget = target;
        }

        private static string SerializeValue(PCGExposedParam ep)
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            switch (ep.ValueType)
            {
                case "float":   return ep.FloatValue.ToString(ic);
                case "int":     return ep.IntValue.ToString();
                case "bool":    return ep.BoolValue.ToString().ToLower();
                case "string":  return ep.StringValue ?? "";
                case "Vector3":
                    var v = ep.Vector3Value;
                    return $"{v.x.ToString(ic)},{v.y.ToString(ic)},{v.z.ToString(ic)}";
                case "Color":
                    var c = ep.ColorValue;
                    return $"{c.r.ToString(ic)},{c.g.ToString(ic)},{c.b.ToString(ic)},{c.a.ToString(ic)}";
                default:
                    return ep.StringValue ?? "";
            }
        }
    }
}
