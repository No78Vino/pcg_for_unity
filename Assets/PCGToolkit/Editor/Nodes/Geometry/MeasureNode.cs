using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 测量几何属性（对标 Houdini Measure SOP）
    /// </summary>
    public class MeasureNode : PCGNodeBase
    {
        public override string Name => "Measure";
        public override string DisplayName => "Measure";
        public override string Description => "测量面积、周长、曲率等几何属性并写入属性";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "测量类型（area/perimeter/curvature/volume）", "area"),
            new PCGParamSchema("attribName", PCGPortDirection.Input, PCGPortType.String,
                "Attribute Name", "存储结果的属性名", "area"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带测量属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string type = GetParamString(parameters, "type", "area");
            string attribName = GetParamString(parameters, "attribName", "area");

            switch (type.ToLower())
            {
                case "area":
                    // 计算每个面的面积
                    var areaAttr = geo.PrimAttribs.CreateAttribute(attribName, AttribType.Float);
                    for (int p = 0; p < geo.Primitives.Count; p++)
                    {
                        float area = CalculateFaceArea(geo, p);
                        areaAttr.Values.Add(area);
                    }
                    break;

                case "perimeter":
                    // 计算每个面的周长
                    var perimeterAttr = geo.PrimAttribs.CreateAttribute(attribName, AttribType.Float);
                    for (int p = 0; p < geo.Primitives.Count; p++)
                    {
                        float perimeter = CalculateFacePerimeter(geo, p);
                        perimeterAttr.Values.Add(perimeter);
                    }
                    break;

                case "volume":
                    // 计算总体积（近似）
                    var volumeAttr = geo.DetailAttribs.CreateAttribute(attribName, AttribType.Float);
                    float totalVolume = CalculateVolume(geo);
                    volumeAttr.Values.Add(totalVolume);
                    break;

                case "curvature":
                    // 曲率计算较为复杂，这里简化处理
                    ctx.LogWarning("Measure: curvature 计算需要更复杂的实现");
                    break;
            }

            return SingleOutput("geometry", geo);
        }

        private float CalculateFaceArea(PCGGeometry geo, int primIndex)
        {
            var prim = geo.Primitives[primIndex];
            if (prim.Length < 3) return 0f;

            float area = 0f;
            // 将多边形分解为三角形
            for (int i = 1; i < prim.Length - 1; i++)
            {
                Vector3 v0 = geo.Points[prim[0]];
                Vector3 v1 = geo.Points[prim[i]];
                Vector3 v2 = geo.Points[prim[i + 1]];
                area += Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            }
            return area;
        }

        private float CalculateFacePerimeter(PCGGeometry geo, int primIndex)
        {
            var prim = geo.Primitives[primIndex];
            if (prim.Length < 2) return 0f;

            float perimeter = 0f;
            for (int i = 0; i < prim.Length; i++)
            {
                int next = (i + 1) % prim.Length;
                perimeter += Vector3.Distance(geo.Points[prim[i]], geo.Points[prim[next]]);
            }
            return perimeter;
        }

        private float CalculateVolume(PCGGeometry geo)
        {
            // 使用有符号体积法计算封闭网格体积
            float volume = 0f;

            // 将所有面转换为三角形
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length < 3) continue;

                for (int i = 1; i < prim.Length - 1; i++)
                {
                    Vector3 v0 = geo.Points[prim[0]];
                    Vector3 v1 = geo.Points[prim[i]];
                    Vector3 v2 = geo.Points[prim[i + 1]];

                    // 有符号体积 = v0 · (v1 × v2) / 6
                    volume += Vector3.Dot(v0, Vector3.Cross(v1, v2)) / 6f;
                }
            }

            return Mathf.Abs(volume);
        }
    }
}