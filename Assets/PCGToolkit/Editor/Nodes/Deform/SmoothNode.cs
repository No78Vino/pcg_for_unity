using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 平滑几何体（对标 Houdini Smooth SOP）
    /// </summary>
    public class SmoothNode : PCGNodeBase
    {
        public override string Name => "Smooth";
        public override string DisplayName => "Smooth";
        public override string Description => "对几何体进行拉普拉斯平滑";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "平滑迭代次数", 10),
            new PCGParamSchema("strength", PCGPortDirection.Input, PCGPortType.Float,
                "Strength", "平滑强度（0~1）", 0.5f) { Min = 0f, Max = 1f },
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅平滑指定分组的点（留空=全部）", ""),
            new PCGParamSchema("preserveVolume", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Volume", "保持体积（HC Laplacian）", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "平滑后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Smooth: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            int iterations = GetParamInt(parameters, "iterations", 10);
            float strength = GetParamFloat(parameters, "strength", 0.5f);
            string group = GetParamString(parameters, "group", "");
            bool preserveVolume = GetParamBool(parameters, "preserveVolume", false);

            // 构建邻接关系（从 Primitives 中提取）
            var neighbors = new List<HashSet<int>>();
            for (int i = 0; i < geo.Points.Count; i++)
                neighbors.Add(new HashSet<int>());

            foreach (var prim in geo.Primitives)
            {
                for (int i = 0; i < prim.Length; i++)
                {
                    int next = prim[(i + 1) % prim.Length];
                    neighbors[prim[i]].Add(next);
                    neighbors[next].Add(prim[i]);
                }
            }

            // 确定要平滑的点
            HashSet<int> pointsToSmooth = null;
            if (!string.IsNullOrEmpty(group) && geo.PointGroups.TryGetValue(group, out var groupSet))
            {
                pointsToSmooth = groupSet;
            }

            // 拉普拉斯平滑迭代
            var originalPositions = preserveVolume ? new List<Vector3>(geo.Points) : null;

            for (int iter = 0; iter < iterations; iter++)
            {
                var newPositions = new List<Vector3>(geo.Points);

                for (int i = 0; i < geo.Points.Count; i++)
                {
                    if (pointsToSmooth != null && !pointsToSmooth.Contains(i))
                        continue;

                    var neighborSet = neighbors[i];
                    if (neighborSet.Count == 0) continue;

                    // 计算邻居重心
                    Vector3 centroid = Vector3.zero;
                    foreach (int n in neighborSet)
                        centroid += geo.Points[n];
                    centroid /= neighborSet.Count;

                    // 插值到重心
                    newPositions[i] = Vector3.Lerp(geo.Points[i], centroid, strength);
                }

                geo.Points = newPositions;
            }

            // HC-Laplacian 体积保持修正
            if (preserveVolume && originalPositions != null)
            {
                for (int iter = 0; iter < iterations; iter++)
                {
                    var newPositions = new List<Vector3>(geo.Points);

                    for (int i = 0; i < geo.Points.Count; i++)
                    {
                        if (pointsToSmooth != null && !pointsToSmooth.Contains(i))
                            continue;

                        var neighborSet = neighbors[i];
                        if (neighborSet.Count == 0) continue;

                        // 计算当前邻居重心
                        Vector3 centroid = Vector3.zero;
                        foreach (int n in neighborSet)
                            centroid += geo.Points[n];
                        centroid /= neighborSet.Count;

                        // 计算原始邻居重心
                        Vector3 originalCentroid = Vector3.zero;
                        foreach (int n in neighborSet)
                            originalCentroid += originalPositions[n];
                        originalCentroid /= neighborSet.Count;

                        // HC修正：向原始偏移方向回退
                        Vector3 laplacian = centroid - geo.Points[i];
                        Vector3 originalDiff = originalPositions[i] - originalCentroid;
                        newPositions[i] = geo.Points[i] + laplacian * strength - originalDiff * strength * 0.5f;
                    }

                    geo.Points = newPositions;
                }
            }

            ctx.Log($"Smooth: iterations={iterations}, strength={strength}, preserveVolume={preserveVolume}");
            return SingleOutput("geometry", geo);
        }
    }
}
