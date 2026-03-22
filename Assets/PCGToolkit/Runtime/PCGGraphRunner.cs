using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using PCGToolkit.Core;
#endif

namespace PCGToolkit.Graph
{
    /// <summary>
    /// HDA 风格运行时组件：将 PCGGraphData 资产绑定到场景 GameObject，
    /// 可在 Inspector 中覆盖暴露参数并触发图执行。
    /// 图执行仅在 Unity Editor 中有效。
    /// </summary>
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

        /// <summary>
        /// 上次执行输出的 PCGGeometry 对象（运行时为 object，避免直接依赖 Editor 程序集类型）
        /// </summary>
        [System.NonSerialized]
        public object LastOutput;

        private void Start()
        {
            if (RunOnStart) Run();
        }

        public void Run()
        {
#if UNITY_EDITOR
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

            PCGGeometry lastGeo = null;
            foreach (var nodeData in dataCopy.Nodes)
            {
                var geo = executor.GetNodeOutput(nodeData.NodeId, "geometry");
                if (geo != null && geo.Points.Count > 0)
                    lastGeo = geo;
            }

            LastOutput = lastGeo;

            if (InstantiateOutput && lastGeo != null)
                ApplyOutputToScene(lastGeo);
#else
            Debug.LogWarning("[PCGGraphRunner] Graph execution is only available in the Unity Editor.");
#endif
        }

        private void ApplyOutputToScene(object geoObj)
        {
#if UNITY_EDITOR
            var geo = geoObj as PCGGeometry;
            if (geo == null) return;

            var mesh = PCGGeometryToMesh.Convert(geo);
            var target = OutputTarget != null ? OutputTarget : new GameObject("PCG_Output");
            if (target.transform.parent != transform)
                target.transform.SetParent(transform);

            var mf = target.GetComponent<MeshFilter>() ?? target.AddComponent<MeshFilter>();
            var mr = target.GetComponent<MeshRenderer>() ?? target.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (OutputTarget == null) OutputTarget = target;
#endif
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
