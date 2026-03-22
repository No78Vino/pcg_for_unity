using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// LOD 生成节点
    /// 自动生成 LOD 链
    /// </summary>
    public class LODGenerateNode : PCGNodeBase
    {
        public override string Name => "LODGenerate";
        public override string DisplayName => "LOD Generate";
        public override string Description => "为几何体自动生成 LOD（细节层次）链";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("lodCount", PCGPortDirection.Input, PCGPortType.Int,
                "LOD Count", "LOD 级别数量", 3),
            new PCGParamSchema("lodRatio", PCGPortDirection.Input, PCGPortType.Float,
                "LOD Ratio", "每级 LOD 的面数比例", 0.5f),
            new PCGParamSchema("screenPercentages", PCGPortDirection.Input, PCGPortType.String,
                "Screen Percentages", "各级 LOD 的屏幕占比（逗号分隔）", "0.8,0.4,0.1"),
            new PCGParamSchema("createGroup", PCGPortDirection.Input, PCGPortType.Bool,
                "Create Group", "为每级 LOD 创建分组", true),
            new PCGParamSchema("enabled", PCGPortDirection.Input, PCGPortType.Bool,
                "Enabled", "是否执行 LOD 生成", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "包含所有 LOD 的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var inputGeo = GetInputGeometry(inputGeometries, "input");
            if (!GetParamBool(parameters, "enabled", true))
                return SingleOutput("geometry", inputGeo);

            if (inputGeo.Points.Count == 0 || inputGeo.Primitives.Count == 0)
            {
                ctx.LogWarning("LODGenerate: 输入几何体为空");
                return SingleOutput("geometry", new PCGGeometry());
            }

            int lodCount = Mathf.Max(1, GetParamInt(parameters, "lodCount", 3));
            float lodRatio = Mathf.Clamp(GetParamFloat(parameters, "lodRatio", 0.5f), 0.1f, 0.9f);
            string screenPercentagesStr = GetParamString(parameters, "screenPercentages", "0.8,0.4,0.1");
            bool createGroup = GetParamBool(parameters, "createGroup", true);

            // 解析屏幕占比
            var screenPercentages = new float[lodCount];
            string[] parts = screenPercentagesStr.Split(',');
            for (int i = 0; i < lodCount; i++)
            {
                if (i < parts.Length && float.TryParse(parts[i].Trim(), out float pct))
                    screenPercentages[i] = pct;
                else
                    screenPercentages[i] = Mathf.Pow(0.5f, i); // 默认递减
            }

            // 生成 LOD 链
            var geo = new PCGGeometry();
            var allPoints = new List<Vector3>();
            var allPrimitives = new List<int[]>();
            var lodInfos = new List<(int primStart, int primCount, float screenPct)>();

            var currentGeo = inputGeo.Clone();

            for (int lod = 0; lod < lodCount; lod++)
            {
                int primStart = allPrimitives.Count;

                if (lod > 0)
                {
                    // 减面
                    currentGeo = DecimateGeometry(currentGeo, lodRatio);
                }

                int baseIdx = allPoints.Count;
                allPoints.AddRange(currentGeo.Points);

                foreach (var prim in currentGeo.Primitives)
                {
                    var newPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                        newPrim[i] = prim[i] + baseIdx;
                    allPrimitives.Add(newPrim);
                }

                int primCount = allPrimitives.Count - primStart;
                lodInfos.Add((primStart, primCount, screenPercentages[lod]));

                // 创建 LOD 分组
                if (createGroup)
                {
                    string groupName = $"LOD{lod}";
                    if (!geo.PrimGroups.ContainsKey(groupName))
                        geo.PrimGroups[groupName] = new HashSet<int>();

                    for (int p = primStart; p < primStart + primCount; p++)
                        geo.PrimGroups[groupName].Add(p);
                }
            }

            geo.Points = allPoints;
            geo.Primitives = allPrimitives;

            // 存储 LOD 信息到 Detail 属性
            geo.DetailAttribs.SetAttribute("lodCount", lodCount);
            for (int i = 0; i < lodInfos.Count; i++)
            {
                geo.DetailAttribs.SetAttribute($"lod{i}_primStart", lodInfos[i].primStart);
                geo.DetailAttribs.SetAttribute($"lod{i}_primCount", lodInfos[i].primCount);
                geo.DetailAttribs.SetAttribute($"lod{i}_screenPct", lodInfos[i].screenPct);
            }

            ctx.Log($"LODGenerate: {lodCount} LODs, screenPcts=[{string.Join(", ", screenPercentages)}]");
            return SingleOutput("geometry", geo);
        }

        private PCGGeometry DecimateGeometry(PCGGeometry geo, float ratio)
        {
            // 简单的边坍缩减面
            var result = geo.Clone();

            if (result.Primitives.Count == 0) return result;

            // 确保所有面是三角形
            var triangles = new List<int[]>();
            foreach (var prim in result.Primitives)
            {
                if (prim.Length == 3)
                    triangles.Add(prim);
                else
                {
                    for (int i = 1; i < prim.Length - 1; i++)
                        triangles.Add(new int[] { prim[0], prim[i], prim[i + 1] });
                }
            }
            result.Primitives = triangles;

            // 计算每条边的长度
            var edgeLengths = new Dictionary<(int, int), float>();
            foreach (var tri in triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    int v0 = tri[i];
                    int v1 = tri[(i + 1) % 3];
                    var edge = v0 < v1 ? (v0, v1) : (v1, v0);

                    if (!edgeLengths.ContainsKey(edge))
                    {
                        float len = Vector3.Distance(result.Points[v0], result.Points[v1]);
                        edgeLengths[edge] = len;
                    }
                }
            }

            // 按长度排序边
            var sortedEdges = new List<(int, int)>(edgeLengths.Keys);
            sortedEdges.Sort((a, b) => edgeLengths[a].CompareTo(edgeLengths[b]));

            // 坍缩最短的边
            int targetCount = Mathf.Max(4, Mathf.FloorToInt(triangles.Count * ratio));
            int collapsesNeeded = (triangles.Count - targetCount) / 2;
            int collapses = 0;

            var mergedVertices = new Dictionary<int, int>();
            var usedEdges = new HashSet<(int, int)>();

            foreach (var edge in sortedEdges)
            {
                if (collapses >= collapsesNeeded) break;

                int v0 = edge.Item1;
                int v1 = edge.Item2;

                // 检查是否已经被合并
                while (mergedVertices.ContainsKey(v0)) v0 = mergedVertices[v0];
                while (mergedVertices.ContainsKey(v1)) v1 = mergedVertices[v1];

                if (v0 == v1) continue;
                if (usedEdges.Contains((Mathf.Min(v0, v1), Mathf.Max(v0, v1)))) continue;

                usedEdges.Add((Mathf.Min(v0, v1), Mathf.Max(v0, v1)));

                // 合并 v1 到 v0
                mergedVertices[v1] = v0;

                // 更新点的位置为中点
                result.Points[v0] = (result.Points[v0] + result.Points[v1]) * 0.5f;

                collapses++;
            }

            // 更新面的顶点索引
            var newPrimitives = new List<int[]>();
            foreach (var tri in result.Primitives)
            {
                var newTri = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    int v = tri[i];
                    while (mergedVertices.ContainsKey(v)) v = mergedVertices[v];
                    newTri[i] = v;
                }

                // 检查是否退化
                if (newTri[0] != newTri[1] && newTri[1] != newTri[2] && newTri[0] != newTri[2])
                    newPrimitives.Add(newTri);
            }

            result.Primitives = newPrimitives;

            // 清理未使用的顶点
            var usedVertices = new HashSet<int>();
            foreach (var tri in result.Primitives)
            {
                usedVertices.Add(tri[0]);
                usedVertices.Add(tri[1]);
                usedVertices.Add(tri[2]);
            }

            var oldToNew = new Dictionary<int, int>();
            var newPoints = new List<Vector3>();
            int newIdx = 0;

            for (int i = 0; i < result.Points.Count; i++)
            {
                if (usedVertices.Contains(i))
                {
                    oldToNew[i] = newIdx;
                    newPoints.Add(result.Points[i]);
                    newIdx++;
                }
            }

            foreach (var tri in result.Primitives)
            {
                tri[0] = oldToNew[tri[0]];
                tri[1] = oldToNew[tri[1]];
                tri[2] = oldToNew[tri[2]];
            }

            result.Points = newPoints;

            return result;
        }
    }
}