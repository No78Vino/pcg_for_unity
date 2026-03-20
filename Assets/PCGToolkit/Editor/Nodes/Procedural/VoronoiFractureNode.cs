using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Procedural
{
    /// <summary>
    /// Voronoi 碎裂（对标 Houdini Voronoi Fracture SOP）
    /// </summary>
    public class VoronoiFractureNode : PCGNodeBase
    {
        public override string Name => "VoronoiFracture";
        public override string DisplayName => "Voronoi Fracture";
        public override string Description => "使用 Voronoi 图对几何体进行碎裂分割";
        public override PCGNodeCategory Category => PCGNodeCategory.Procedural;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "要碎裂的几何体", null, required: true),
            new PCGParamSchema("points", PCGPortDirection.Input, PCGPortType.Geometry,
                "Scatter Points", "Voronoi 种子点（可选，留空则自动散布）", null),
            new PCGParamSchema("numPoints", PCGPortDirection.Input, PCGPortType.Int,
                "Num Points", "自动散布的种子点数（无 points 输入时使用）", 20),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("createInterior", PCGPortDirection.Input, PCGPortType.Bool,
                "Create Interior", "是否生成内部截面", true),
            new PCGParamSchema("interiorGroup", PCGPortDirection.Input, PCGPortType.String,
                "Interior Group", "内部截面的分组名", "inside"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "碎裂后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var inputGeo = GetInputGeometry(inputGeometries, "input");

            if (inputGeo.Points.Count == 0)
            {
                ctx.LogWarning("VoronoiFracture: 输入几何体为空");
                return SingleOutput("geometry", new PCGGeometry());
            }

            var pointsGeo = GetInputGeometry(inputGeometries, "points");
            int numPoints = GetParamInt(parameters, "numPoints", 20);
            int seed = GetParamInt(parameters, "seed", 0);
            bool createInterior = GetParamBool(parameters, "createInterior", true);
            string interiorGroup = GetParamString(parameters, "interiorGroup", "inside");

            var rng = new System.Random(seed);

            // 计算输入几何体的包围盒
            Vector3 min = inputGeo.Points[0];
            Vector3 max = inputGeo.Points[0];
            foreach (var p in inputGeo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            // 生成或使用种子点
            var seedPoints = new List<Vector3>();
            if (pointsGeo != null && pointsGeo.Points.Count > 0)
            {
                seedPoints.AddRange(pointsGeo.Points);
            }
            else
            {
                // 在包围盒内随机生成种子点
                for (int i = 0; i < numPoints; i++)
                {
                    float x = min.x + (float)rng.NextDouble() * size.x;
                    float y = min.y + (float)rng.NextDouble() * size.y;
                    float z = min.z + (float)rng.NextDouble() * size.z;
                    seedPoints.Add(new Vector3(x, y, z));
                }
            }

            // 为每个 Voronoi 单元生成碎片几何体
            var geo = new PCGGeometry();
            var allPoints = new List<Vector3>();
            var allPrimitives = new List<int[]>();

            for (int i = 0; i < seedPoints.Count; i++)
            {
                Vector3 seedPoint = seedPoints[i];

                // 计算这个种子点的 Voronoi 单元
                var cellVertices = ComputeVoronoiCell(seedPoint, seedPoints, min, max, i);

                if (cellVertices.Count < 4) continue;

                // 创建碎片（凸包三角化）
                int baseIdx = allPoints.Count;
                allPoints.AddRange(cellVertices);

                // 简单扇形三角化（从中心点）
                Vector3 cellCenter = Vector3.zero;
                foreach (var v in cellVertices) cellCenter += v;
                cellCenter /= cellVertices.Count;

                // 添加中心点
                int centerIdx = allPoints.Count;
                allPoints.Add(cellCenter);

                // 计算凸包面（简化：遍历所有三角形）
                for (int j = 0; j < cellVertices.Count; j++)
                {
                    int j1 = baseIdx + j;
                    int j2 = baseIdx + (j + 1) % cellVertices.Count;

                    // 检查三角形是否面向外部
                    Vector3 v0 = allPoints[j1];
                    Vector3 v1 = allPoints[j2];
                    Vector3 v2 = cellCenter;

                    Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);

                    // 只添加面向种子点的三角形
                    if (Vector3.Dot(faceNormal, seedPoint - cellCenter) > 0)
                    {
                        allPrimitives.Add(new int[] { j1, j2, centerIdx });
                    }
                    else
                    {
                        allPrimitives.Add(new int[] { j2, j1, centerIdx });
                    }
                }

                // 创建碎片分组
                string groupName = $"piece_{i}";
                if (!geo.PrimGroups.ContainsKey(groupName))
                    geo.PrimGroups[groupName] = new HashSet<int>();

                int primStart = allPrimitives.Count - cellVertices.Count;
                for (int p = primStart; p < allPrimitives.Count; p++)
                {
                    geo.PrimGroups[groupName].Add(p);
                }
            }

            geo.Points = allPoints;
            geo.Primitives = allPrimitives;

            ctx.Log($"VoronoiFracture: seeds={seedPoints.Count}, pieces={seedPoints.Count}, output={allPoints.Count}pts, {allPrimitives.Count}faces");
            return SingleOutput("geometry", geo);
        }

        private List<Vector3> ComputeVoronoiCell(Vector3 seed, List<Vector3> allSeeds, Vector3 min, Vector3 max, int seedIndex)
        {
            // 使用半空间裁剪法计算 3D Voronoi 单元
            // 从包围盒的 6 个面（每面 2 个三角形）开始，逐步用中垂面裁剪

            // 包围盒的 8 个角点
            Vector3[] boxVerts = new Vector3[]
            {
                new Vector3(min.x, min.y, min.z), // 0
                new Vector3(max.x, min.y, min.z), // 1
                new Vector3(max.x, max.y, min.z), // 2
                new Vector3(min.x, max.y, min.z), // 3
                new Vector3(min.x, min.y, max.z), // 4
                new Vector3(max.x, min.y, max.z), // 5
                new Vector3(max.x, max.y, max.z), // 6
                new Vector3(min.x, max.y, max.z), // 7
            };

            // 包围盒的 12 个三角形（6 个面，每面 2 个三角形）
            var faces = new List<int[]>
            {
                new[]{0,2,1}, new[]{0,3,2}, // 前面 (-Z)
                new[]{4,5,6}, new[]{4,6,7}, // 后面 (+Z)
                new[]{0,1,5}, new[]{0,5,4}, // 底面 (-Y)
                new[]{2,3,7}, new[]{2,7,6}, // 顶面 (+Y)
                new[]{0,4,7}, new[]{0,7,3}, // 左面 (-X)
                new[]{1,2,6}, new[]{1,6,5}, // 右面 (+X)
            };

            var vertices = new List<Vector3>(boxVerts);

            // 用每个相邻种子点的中垂面裁剪凸多面体
            foreach (var other in allSeeds)
            {
                if (other == seed) continue;

                Vector3 midpoint = (seed + other) * 0.5f;
                Vector3 normal = (other - seed).normalized;

                // 对每个三角面进行半空间裁剪
                var newFaces = new List<int[]>();

                foreach (var face in faces)
                {
                    // 计算每个顶点到裁剪面的距离
                    float d0 = Vector3.Dot(vertices[face[0]] - midpoint, normal);
                    float d1 = Vector3.Dot(vertices[face[1]] - midpoint, normal);
                    float d2 = Vector3.Dot(vertices[face[2]] - midpoint, normal);

                    // 所有顶点都在保留侧
                    if (d0 <= 0 && d1 <= 0 && d2 <= 0)
                    {
                        newFaces.Add(face);
                        continue;
                    }

                    // 所有顶点都在裁剪侧
                    if (d0 > 0 && d1 > 0 && d2 > 0)
                        continue;

                    // 部分裁剪：计算交点并生成新三角形
                    var insideVerts = new List<int>();
                    var outsideVerts = new List<int>();
                    float[] dists = { d0, d1, d2 };

                    for (int i = 0; i < 3; i++)
                    {
                        if (dists[i] <= 0)
                            insideVerts.Add(i);
                        else
                            outsideVerts.Add(i);
                    }

                    if (insideVerts.Count == 2)
                    {
                        // 两个顶点在内侧，一个在外侧
                        int outIdx = outsideVerts[0];
                        int in0 = insideVerts[0];
                        int in1 = insideVerts[1];

                        // 计算两个交点
                        float t0 = dists[in0] / (dists[in0] - dists[outIdx]);
                        Vector3 inter0 = vertices[face[in0]] + t0 * (vertices[face[outIdx]] - vertices[face[in0]]);
                        int inter0Idx = vertices.Count;
                        vertices.Add(inter0);

                        float t1 = dists[in1] / (dists[in1] - dists[outIdx]);
                        Vector3 inter1 = vertices[face[in1]] + t1 * (vertices[face[outIdx]] - vertices[face[in1]]);
                        int inter1Idx = vertices.Count;
                        vertices.Add(inter1);

                        // 生成两个新三角形
                        newFaces.Add(new int[] { face[in0], face[in1], inter0Idx });
                        newFaces.Add(new int[] { face[in1], inter1Idx, inter0Idx });
                    }
                    else if (insideVerts.Count == 1)
                    {
                        // 一个顶点在内侧，两个在外侧
                        int inIdx = insideVerts[0];
                        int out0 = outsideVerts[0];
                        int out1 = outsideVerts[1];

                        float t0 = dists[inIdx] / (dists[inIdx] - dists[out0]);
                        Vector3 inter0 = vertices[face[inIdx]] + t0 * (vertices[face[out0]] - vertices[face[inIdx]]);
                        int inter0Idx = vertices.Count;
                        vertices.Add(inter0);

                        float t1 = dists[inIdx] / (dists[inIdx] - dists[out1]);
                        Vector3 inter1 = vertices[face[inIdx]] + t1 * (vertices[face[out1]] - vertices[face[inIdx]]);
                        int inter1Idx = vertices.Count;
                        vertices.Add(inter1);

                        newFaces.Add(new int[] { face[inIdx], inter0Idx, inter1Idx });
                    }
                }

                faces = newFaces;
                if (faces.Count == 0) break;
            }

            // 收集所有使用的顶点
            var usedVertices = new HashSet<int>();
            foreach (var face in faces)
            {
                usedVertices.Add(face[0]);
                usedVertices.Add(face[1]);
                usedVertices.Add(face[2]);
            }

            var result = new List<Vector3>();
            foreach (int idx in usedVertices)
                result.Add(vertices[idx]);

            return result;
        }
    }
}