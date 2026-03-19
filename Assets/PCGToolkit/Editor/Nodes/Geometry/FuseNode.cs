using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 合并重叠顶点（对标 Houdini Fuse SOP）
    /// </summary>
    public class FuseNode : PCGNodeBase
    {
        public override string Name => "Fuse";
        public override string DisplayName => "Fuse";
        public override string Description => "合并距离阈值内的重叠顶点";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "合并距离阈值", 0.001f),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅处理指定分组（留空=全部）", ""),
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
            float distance = GetParamFloat(parameters, "distance", 0.001f);
            float distSqr = distance * distance;

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo);

            // 简化的合并算法：O(n²) 遍历
            // 对于大规模数据应使用空间加速结构（如 KD-Tree）
            int[] remap = new int[geo.Points.Count];
            for (int i = 0; i < remap.Length; i++) remap[i] = i;

            List<Vector3> newPoints = new List<Vector3>();
            Dictionary<int, int> oldToNew = new Dictionary<int, int>();

            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (remap[i] != i) continue; // 已被合并

                int newIdx = newPoints.Count;
                newPoints.Add(geo.Points[i]);
                oldToNew[i] = newIdx;

                // 查找后续点中可合并的
                for (int j = i + 1; j < geo.Points.Count; j++)
                {
                    if (remap[j] != j) continue;

                    float sqrDist = (geo.Points[i] - geo.Points[j]).sqrMagnitude;
                    if (sqrDist <= distSqr)
                    {
                        remap[j] = i; // j 合并到 i
                        oldToNew[j] = newIdx;
                    }
                }
            }

            // 更新面索引
            for (int p = 0; p < geo.Primitives.Count; p++)
            {
                var prim = geo.Primitives[p];
                for (int i = 0; i < prim.Length; i++)
                {
                    prim[i] = oldToNew[prim[i]];
                }
            }

            // 更新边索引
            for (int e = 0; e < geo.Edges.Count; e++)
            {
                var edge = geo.Edges[e];
                edge[0] = oldToNew[edge[0]];
                edge[1] = oldToNew[edge[1]];
            }

            geo.Points = newPoints;
            return SingleOutput("geometry", geo);
        }
    }
}