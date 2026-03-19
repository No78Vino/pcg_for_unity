using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 计算/设置法线（对标 Houdini Normal SOP）
    /// </summary>
    public class NormalNode : PCGNodeBase
    {
        public override string Name => "Normal";
        public override string DisplayName => "Normal";
        public override string Description => "重新计算几何体的法线";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "法线计算类型（point/vertex/primitive）", "point"),
            new PCGParamSchema("cuspAngle", PCGPortDirection.Input, PCGPortType.Float,
                "Cusp Angle", "锐角阈值（超过此角度的边将产生硬边法线）", 60f),
            new PCGParamSchema("weightByArea", PCGPortDirection.Input, PCGPortType.Bool,
                "Weight by Area", "是否按面积加权", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string type = GetParamString(parameters, "type", "point");
            float cuspAngle = GetParamFloat(parameters, "cuspAngle", 60f);
            bool weightByArea = GetParamBool(parameters, "weightByArea", true);

            if (geo.Points.Count == 0 || geo.Primitives.Count == 0)
                return SingleOutput("geometry", geo);

            // 创建法线属性
            var normalAttr = geo.PointAttribs.CreateAttribute("N", AttribType.Vector3);

            switch (type.ToLower())
            {
                case "primitive":
                    // 面法线
                    var primNormals = geo.PrimAttribs.CreateAttribute("N", AttribType.Vector3);
                    for (int p = 0; p < geo.Primitives.Count; p++)
                    {
                        Vector3 normal = CalculateFaceNormal(geo, p);
                        primNormals.Values.Add(normal);
                    }
                    break;

                case "point":
                default:
                    // 点法线：平均相邻面的法线
                    Vector3[] pointNormals = new Vector3[geo.Points.Count];
                    float[] weights = new float[geo.Points.Count];

                    for (int p = 0; p < geo.Primitives.Count; p++)
                    {
                        var prim = geo.Primitives[p];
                        Vector3 faceNormal = CalculateFaceNormal(geo, p);
                        float area = CalculateFaceArea(geo, p);
                        float weight = weightByArea ? area : 1f;

                        foreach (int idx in prim)
                        {
                            pointNormals[idx] += faceNormal * weight;
                            weights[idx] += weight;
                        }
                    }

                    // 归一化
                    for (int i = 0; i < geo.Points.Count; i++)
                    {
                        if (weights[i] > 0)
                            pointNormals[i] /= weights[i];
                        pointNormals[i] = pointNormals[i].normalized;
                        normalAttr.Values.Add(pointNormals[i]);
                    }
                    break;
            }

            return SingleOutput("geometry", geo);
        }

        private Vector3 CalculateFaceNormal(PCGGeometry geo, int primIndex)
        {
            var prim = geo.Primitives[primIndex];
            if (prim.Length < 3) return Vector3.up;

            Vector3 v0 = geo.Points[prim[0]];
            Vector3 v1 = geo.Points[prim[1]];
            Vector3 v2 = geo.Points[prim[2]];
            return Vector3.Cross(v1 - v0, v2 - v0).normalized;
        }

        private float CalculateFaceArea(PCGGeometry geo, int primIndex)
        {
            var prim = geo.Primitives[primIndex];
            if (prim.Length < 3) return 0f;

            float area = 0f;
            for (int i = 1; i < prim.Length - 1; i++)
            {
                Vector3 v0 = geo.Points[prim[0]];
                Vector3 v1 = geo.Points[prim[i]];
                Vector3 v2 = geo.Points[prim[i + 1]];
                area += Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            }
            return area;
        }
    }
}