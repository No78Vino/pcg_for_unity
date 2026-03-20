using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 减面（对标 Houdini PolyReduce SOP）
    /// 使用边坍缩算法
    /// </summary>
    public class DecimateNode : PCGNodeBase
    {
        public override string Name => "Decimate";
        public override string DisplayName => "Decimate";
        public override string Description => "减少网格的面数";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("targetRatio", PCGPortDirection.Input, PCGPortType.Float,
                "Target Ratio", "目标面数比例（0~1）", 0.5f) { Min = 0.01f, Max = 1f },
            new PCGParamSchema("targetCount", PCGPortDirection.Input, PCGPortType.Int,
                "Target Count", "目标面数（优先于 ratio）", 0),
            new PCGParamSchema("preserveBoundary", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Boundary", "保持边界不变", true),
            new PCGParamSchema("preserveTopology", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Topology", "保持拓扑结构", false),
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

            if (geo.Points.Count == 0 || geo.Primitives.Count == 0)
            {
                ctx.LogWarning("Decimate: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float targetRatio = GetParamFloat(parameters, "targetRatio", 0.5f);
            int targetCount = GetParamInt(parameters, "targetCount", 0);
            bool preserveBoundary = GetParamBool(parameters, "preserveBoundary", true);

            // 确保所有面都是三角形
            var triangles = new List<int[]>();
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length == 3)
                {
                    triangles.Add(prim);
                }
                else
                {
                    for (int i = 1; i < prim.Length - 1; i++)
                    {
                        triangles.Add(new int[] { prim[0], prim[i], prim[i + 1] });
                    }
                }
            }
            geo.Primitives = triangles;

            int originalCount = triangles.Count;
            int finalCount = targetCount > 0 ? targetCount : Mathf.Max(4, Mathf.FloorToInt(originalCount * targetRatio));

            if (finalCount >= originalCount)
            {
                ctx.Log($"Decimate: 目标面数 {finalCount} >= 原始面数 {originalCount}，无需减面");
                return SingleOutput("geometry", geo);
            }

            // 找边界边
            var boundaryEdges = new HashSet<(int, int)>();
            if (preserveBoundary)
            {
                var edgeCount = new Dictionary<(int, int), int>();
                foreach (var tri in triangles)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int v0 = tri[i];
                        int v1 = tri[(i + 1) % 3];
                        var edge = v0 < v1 ? (v0, v1) : (v1, v0);
                        if (!edgeCount.ContainsKey(edge)) edgeCount[edge] = 0;
                        edgeCount[edge]++;
                    }
                }

                foreach (var kvp in edgeCount)
                {
                    if (kvp.Value == 1) boundaryEdges.Add(kvp.Key);
                }
            }

            // 构建边到面的映射
            var edgeTris = new Dictionary<(int, int), List<int>>();
            for (int triIdx = 0; triIdx < geo.Primitives.Count; triIdx++)
            {
                var tri = geo.Primitives[triIdx];
                for (int i = 0; i < 3; i++)
                {
                    int v0 = tri[i];
                    int v1 = tri[(i + 1) % 3];
                    var edge = v0 < v1 ? (v0, v1) : (v1, v0);
                    if (!edgeTris.ContainsKey(edge)) edgeTris[edge] = new List<int>();
                    edgeTris[edge].Add(triIdx);
                }
            }

            // 计算所有可坍缩边的代价
            var edgeCosts = new PriorityQueue<(int, int), float>();
            foreach (var kvp in edgeTris)
            {
                if (kvp.Value.Count != 2) continue;

                var edge = kvp.Key;
                if (preserveBoundary && boundaryEdges.Contains(edge)) continue;

                float cost = CalculateCollapseCost(geo.Points, edge.Item1, edge.Item2);
                edgeCosts.Enqueue(edge, cost);
            }

            // 迭代坍缩边
            int currentCount = originalCount;

            while (currentCount > finalCount && edgeCosts.Count > 0)
            {
                var edge = edgeCosts.Dequeue();

                // 重新检查边是否仍然有效（edgeTris 可能已过期）
                // 验证边的两个顶点是否仍然存在于某个三角形中
                int v0 = edge.Item1;
                int v1 = edge.Item2;

                // 找到包含这条边的三角形
                var adjacentTris = new List<int>();
                for (int triIdx = 0; triIdx < geo.Primitives.Count; triIdx++)
                {
                    var tri = geo.Primitives[triIdx];
                    if (tri == null) continue;
                    bool hasV0 = tri[0] == v0 || tri[1] == v0 || tri[2] == v0;
                    bool hasV1 = tri[0] == v1 || tri[1] == v1 || tri[2] == v1;
                    if (hasV0 && hasV1) adjacentTris.Add(triIdx);
                }

                // 边必须恰好被 2 个三角形共享才能安全坍缩
                if (adjacentTris.Count != 2) continue;

                // 合并顶点（将 v1 合并到 v0 的中点位置）
                Vector3 newPos = (geo.Points[v0] + geo.Points[v1]) * 0.5f;
                geo.Points[v0] = newPos;

                // 更新所有引用 v1 的面
                for (int triIdx = 0; triIdx < geo.Primitives.Count; triIdx++)
                {
                    var tri = geo.Primitives[triIdx];
                    if (tri == null) continue;

                    for (int i = 0; i < 3; i++)
                    {
                        if (tri[i] == v1)
                            tri[i] = v0;
                    }

                    // 检查是否产生退化三角形（重复顶点）
                    if (tri[0] == tri[1] || tri[1] == tri[2] || tri[0] == tri[2])
                    {
                        geo.Primitives[triIdx] = null; // 标记删除
                    }
                }

                // 移除标记删除的面
                geo.Primitives.RemoveAll(t => t == null);
                currentCount = geo.Primitives.Count;
            }

            // 清理未使用的顶点
            var usedVertices = new HashSet<int>();
            foreach (var tri in geo.Primitives)
            {
                if (tri != null)
                {
                    usedVertices.Add(tri[0]);
                    usedVertices.Add(tri[1]);
                    usedVertices.Add(tri[2]);
                }
            }

            var oldToNew = new Dictionary<int, int>();
            var newPoints = new List<Vector3>();
            int newIdx = 0;

            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (usedVertices.Contains(i))
                {
                    oldToNew[i] = newIdx;
                    newPoints.Add(geo.Points[i]);
                    newIdx++;
                }
            }

            // 更新面的顶点索引
            foreach (var tri in geo.Primitives)
            {
                if (tri != null)
                {
                    tri[0] = oldToNew[tri[0]];
                    tri[1] = oldToNew[tri[1]];
                    tri[2] = oldToNew[tri[2]];
                }
            }

            geo.Points = newPoints;

            ctx.Log($"Decimate: {originalCount} -> {geo.Primitives.Count} faces ({(float)geo.Primitives.Count / originalCount * 100:F1}%)");
            return SingleOutput("geometry", geo);
        }

        private float CalculateCollapseCost(List<Vector3> points, int v0, int v1)
        {
            // 简单的代价函数：边长的平方
            return (points[v0] - points[v1]).sqrMagnitude;
        }

        // 简单优先队列实现
        private class PriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
        {
            private readonly List<(TElement element, TPriority priority)> elements = new List<(TElement, TPriority)>();

            public int Count => elements.Count;

            public void Enqueue(TElement element, TPriority priority)
            {
                elements.Add((element, priority));
                elements.Sort((a, b) => a.priority.CompareTo(b.priority));
            }

            public TElement Dequeue()
            {
                if (elements.Count == 0) throw new System.InvalidOperationException("Queue is empty");
                var item = elements[0].element;
                elements.RemoveAt(0);
                return item;
            }
        }
    }
}