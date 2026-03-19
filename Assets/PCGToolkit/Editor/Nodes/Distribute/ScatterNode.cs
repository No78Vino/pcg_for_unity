using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 在几何体表面散布点（对标 Houdini Scatter SOP）
    /// </summary>
    public class ScatterNode : PCGNodeBase
    {
        public override string Name => "Scatter";
        public override string DisplayName => "Scatter";
        public override string Description => "在几何体表面随机散布点";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（散布表面）", null, required: true),
            new PCGParamSchema("count", PCGPortDirection.Input, PCGPortType.Int,
                "Count", "散布点数量", 100),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("densityAttrib", PCGPortDirection.Input, PCGPortType.String,
                "Density Attribute", "密度属性名（控制分布密度）", ""),
            new PCGParamSchema("relaxIterations", PCGPortDirection.Input, PCGPortType.Int,
                "Relax Iterations", "松弛迭代次数（使分布更均匀）", 0),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅在指定面分组上散布", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "散布点几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var inputGeo = GetInputGeometry(inputGeometries, "input");
            int count = Mathf.Max(0, GetParamInt(parameters, "count", 100));
            int seed = GetParamInt(parameters, "seed", 0);
            int relaxIterations = GetParamInt(parameters, "relaxIterations", 0);
            string group = GetParamString(parameters, "group", "");

            var geo = new PCGGeometry();

            if (inputGeo.Points.Count == 0 || inputGeo.Primitives.Count == 0 || count == 0)
            {
                return SingleOutput("geometry", geo);
            }

            System.Random rng = new System.Random(seed);

            // 计算每个面的面积
            List<int> primIndices = new List<int>();
            List<float> primAreas = new List<float>();
            float totalArea = 0f;

            if (!string.IsNullOrEmpty(group) && inputGeo.PrimGroups.TryGetValue(group, out var groupPrims))
            {
                foreach (int primIdx in groupPrims)
                {
                    if (primIdx >= 0 && primIdx < inputGeo.Primitives.Count)
                    {
                        float area = CalculatePrimArea(inputGeo, primIdx);
                        primIndices.Add(primIdx);
                        primAreas.Add(area);
                        totalArea += area;
                    }
                }
            }
            else
            {
                for (int i = 0; i < inputGeo.Primitives.Count; i++)
                {
                    float area = CalculatePrimArea(inputGeo, i);
                    primIndices.Add(i);
                    primAreas.Add(area);
                    totalArea += area;
                }
            }

            if (totalArea <= 0)
            {
                ctx.LogWarning("Scatter: 输入几何体的总面积为 0");
                return SingleOutput("geometry", geo);
            }

            // 按面积加权随机选择面并生成点
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                // 按面积随机选择面
                float r = (float)rng.NextDouble() * totalArea;
                float accum = 0f;
                int selectedPrim = -1;

                for (int j = 0; j < primAreas.Count; j++)
                {
                    accum += primAreas[j];
                    if (accum >= r)
                    {
                        selectedPrim = primIndices[j];
                        break;
                    }
                }

                if (selectedPrim >= 0)
                {
                    Vector3 point = SamplePointOnPrim(inputGeo, selectedPrim, rng);
                    points.Add(point);
                }
            }

            // 松弛迭代（可选）
            if (relaxIterations > 0 && points.Count > 1)
            {
                points = RelaxPoints(points, relaxIterations);
            }

            // 输出点几何体
            geo.Points = points;
            // 创建索引属性（原始面索引）
            var indexAttr = geo.PointAttribs.CreateAttribute("sourcePrim", AttribType.Int);

            return SingleOutput("geometry", geo);
        }

        private float CalculatePrimArea(PCGGeometry geo, int primIndex)
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

        private Vector3 SamplePointOnPrim(PCGGeometry geo, int primIndex, System.Random rng)
        {
            var prim = geo.Primitives[primIndex];
            if (prim.Length < 3) return geo.Points[prim[0]];

            // 三角化后的随机采样
            int triCount = prim.Length - 2;
            int selectedTri = rng.Next(triCount);

            Vector3 v0 = geo.Points[prim[0]];
            Vector3 v1 = geo.Points[prim[selectedTri + 1]];
            Vector3 v2 = geo.Points[prim[selectedTri + 2]];

            // 三角形内随机采样
            float r1 = (float)rng.NextDouble();
            float r2 = (float)rng.NextDouble();
            if (r1 + r2 > 1)
            {
                r1 = 1 - r1;
                r2 = 1 - r2;
            }

            return v0 + (v1 - v0) * r1 + (v2 - v0) * r2;
        }

        private List<Vector3> RelaxPoints(List<Vector3> points, int iterations)
        {
            // 简单的 Lloyd 松弛
            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 avg = points[i];
                    int neighborCount = 1;

                    // 找最近的几个点并平均
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (i != j && Vector3.Distance(points[i], points[j]) < 0.5f)
                        {
                            avg += points[j];
                            neighborCount++;
                        }
                    }

                    points[i] = avg / neighborCount;
                }
            }
            return points;
        }
    }
}