using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// Editor-only 桥接类，持有 PCGGraphRunner 的所有执行逻辑。
    /// PCGGraphRunner（Runtime）调用此类的静态方法，避免 Runtime 程序集直接引用 Editor 程序集。
    /// </summary>
    public static class PCGGraphRunnerBridge
    {
        public static void Run(PCGGraphRunner runner)
        {
            if (runner.GraphAsset == null)
            {
                Debug.LogError("[PCGGraphRunner] GraphAsset is not assigned.");
                return;
            }

            // 深拷贝图数据，避免污染资产
            var dataCopy = runner.GraphAsset.Clone();

            // 将 ExposedParams 覆盖到对应节点参数
            foreach (var ep in runner.ExposedParams)
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

            // 找最后一个有效的 geometry 输出
            PCGGeometry lastGeo = null;
            foreach (var nodeData in dataCopy.Nodes)
            {
                var geo = executor.GetNodeOutput(nodeData.NodeId, "geometry");
                if (geo != null && geo.Points.Count > 0)
                    lastGeo = geo;
            }

            if (runner.InstantiateOutput && lastGeo != null)
                ApplyOutputToScene(runner, lastGeo);
        }

        private static void ApplyOutputToScene(PCGGraphRunner runner, PCGGeometry geo)
        {
            var mesh = PCGGeometryToMesh.Convert(geo);
            var target = runner.OutputTarget != null
                ? runner.OutputTarget
                : new GameObject("PCG_Output");

            if (target.transform.parent != runner.transform)
                target.transform.SetParent(runner.transform);

            var mf = target.GetComponent<MeshFilter>() ?? target.AddComponent<MeshFilter>();
            var mr = target.GetComponent<MeshRenderer>() ?? target.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            if (runner.OutputTarget == null)
                runner.OutputTarget = target;
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
