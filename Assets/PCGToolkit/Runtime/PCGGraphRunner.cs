using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Runtime
{
    /// <summary>
    /// HDA 风格运行时组件：将 PCGGraphData 资产绑定到场景 GameObject，
    /// 可在 Inspector 中覆盖暴露参数并触发图执行（Editor Only）。
    /// </summary>
    [AddComponentMenu("PCG Toolkit/PCG Graph Runner")]
    public class PCGGraphRunner : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Graph Asset")]
        public PCGToolkit.Graph.PCGGraphData GraphAsset;
#endif

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
#if UNITY_EDITOR
            if (RunOnStart) Run();
#endif
        }

#if UNITY_EDITOR
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
                    nodeData.Parameters.Add(new PCGToolkit.Graph.PCGSerializedParameter
                        { Key = ep.ParamName, ValueType = ep.ValueType, ValueJson = valJson });
                }
            }

            var executor = new PCGToolkit.Graph.PCGGraphExecutor(dataCopy);
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
            mr.sharedMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (OutputTarget == null) OutputTarget = target;
        }
#endif

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


        [Header("Exposed Parameters")]
        public List<PCGExposedParam> ExposedParams = new List<PCGExposedParam>();

        [Header("Output")]
        [Tooltip("执行后将结果网格放置到此 GameObject（留空则创建子物体）")]
        public GameObject OutputTarget;
        [Tooltip("是否在 Start() 时自动执行")]
        public bool RunOnStart = false;
        [Tooltip("是否将输出 Mesh 实例化为子 GameObject")]
        public bool InstantiateOutput = true;

        // 运行时缓存
        [System.NonSerialized]
        public PCGGeometry LastOutput;

        private void Start()
        {
#if UNITY_EDITOR
            if (RunOnStart) Run();
#endif
        }

#if UNITY_EDITOR
        /// <summary>执行图（Editor Only）</summary>
        public void Run()
        {
            if (GraphAsset == null)
            {
                Debug.LogError("[PCGGraphRunner] GraphAsset is not assigned.");
                return;
            }

            var dataCopy = GraphAsset.Clone();

            // 将 ExposedParams 覆盖到对应节点参数
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
                    nodeData.Parameters.Add(new PCGToolkit.Graph.PCGSerializedParameter
                        { Key = ep.ParamName, ValueType = ep.ValueType, ValueJson = valJson });
                }
            }

            var executor = new PCGToolkit.Graph.PCGGraphExecutor(dataCopy);
            executor.Execute();

            // 找最后一个有效输出
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
            mr.sharedMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (OutputTarget == null) OutputTarget = target;
        }
#endif

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

