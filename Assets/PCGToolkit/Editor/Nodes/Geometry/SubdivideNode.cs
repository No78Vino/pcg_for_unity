using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 细分几何体（对标 Houdini Subdivide SOP）
    /// </summary>
    public class SubdivideNode : PCGNodeBase
    {
        public override string Name => "Subdivide";
        public override string DisplayName => "Subdivide";
        public override string Description => "对几何体进行细分（Catmull-Clark 或 Linear）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "细分迭代次数", 1),
            new PCGParamSchema("algorithm", PCGPortDirection.Input, PCGPortType.String,
                "Algorithm", "细分算法（catmull-clark / linear）", "linear"),
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
            var geo = GetInputGeometry(inputGeometries, "input");
            int iterations = Mathf.Max(1, GetParamInt(parameters, "iterations", 1));
            string algorithm = GetParamString(parameters, "algorithm", "linear");

            for (int iter = 0; iter < iterations; iter++)
            {
                geo = algorithm.ToLower() == "catmull-clark" 
                    ? SubdivideCatmullClark(geo) 
                    : SubdivideLinear(geo);
            }

            return SingleOutput("geometry", geo);
        }

        private PCGGeometry SubdivideLinear(PCGGeometry geo)
        {
            var result = new PCGGeometry();

            // Linear 细分：每个四边形分成 4 个，每个三角形分成 4 个
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length == 4)
                {
                    // 四边形细分
                    int v0 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[0]]);
                    int v1 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[1]]);
                    int v2 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[2]]);
                    int v3 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[3]]);

                    // 边中点
                    int m01 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[0]] + geo.Points[prim[1]]) * 0.5f);
                    int m12 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[1]] + geo.Points[prim[2]]) * 0.5f);
                    int m23 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[2]] + geo.Points[prim[3]]) * 0.5f);
                    int m30 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[3]] + geo.Points[prim[0]]) * 0.5f);

                    // 中心点
                    int center = result.Points.Count;
                    result.Points.Add((geo.Points[prim[0]] + geo.Points[prim[1]] + 
                                       geo.Points[prim[2]] + geo.Points[prim[3]]) * 0.25f);

                    // 4 个子四边形
                    result.Primitives.Add(new int[] { v0, m01, center, m30 });
                    result.Primitives.Add(new int[] { m01, v1, m12, center });
                    result.Primitives.Add(new int[] { center, m12, v2, m23 });
                    result.Primitives.Add(new int[] { m30, center, m23, v3 });
                }
                else if (prim.Length == 3)
                {
                    // 三角形细分
                    int v0 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[0]]);
                    int v1 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[1]]);
                    int v2 = result.Points.Count;
                    result.Points.Add(geo.Points[prim[2]]);

                    // 边中点
                    int m01 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[0]] + geo.Points[prim[1]]) * 0.5f);
                    int m12 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[1]] + geo.Points[prim[2]]) * 0.5f);
                    int m20 = result.Points.Count;
                    result.Points.Add((geo.Points[prim[2]] + geo.Points[prim[0]]) * 0.5f);

                    // 4 个子三角形
                    result.Primitives.Add(new int[] { v0, m01, m20 });
                    result.Primitives.Add(new int[] { m01, v1, m12 });
                    result.Primitives.Add(new int[] { m20, m12, v2 });
                    result.Primitives.Add(new int[] { m01, m12, m20 });
                }
                else
                {
                    // 其他多边形：直接复制
                    result.Primitives.Add((int[])prim.Clone());
                    foreach (int idx in prim)
                        result.Points.Add(geo.Points[idx]);
                }
            }

            return result;
        }

        private PCGGeometry SubdivideCatmullClark(PCGGeometry geo)
        {
            // Catmull-Clark 细分的简化实现
            // 完整实现需要考虑折痕、边界等特殊情况
            ctx.LogWarning("Subdivide: Catmull-Clark 细分使用简化实现");

            // 第一次迭代使用 linear，后续可改进
            return SubdivideLinear(geo);
        }
    }
}