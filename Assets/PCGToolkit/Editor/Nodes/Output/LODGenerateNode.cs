using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;
using g3;

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
                    screenPercentages[i] = Mathf.Pow(0.5f, i);
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
            try
            {
                var dmesh = GeometryBridge.ToDMesh3(geo);
                int originalCount = dmesh.TriangleCount;

                if (originalCount == 0)
                    return geo.Clone();

                int targetCount = Mathf.Max(4, Mathf.FloorToInt(originalCount * ratio));

                var reducer = new Reducer(dmesh);

                var constraints = new MeshConstraints();
                MeshConstraintUtil.FixAllBoundaryEdges(constraints, dmesh);
                reducer.SetExternalConstraints(constraints);

                reducer.ReduceToTriangleCount(targetCount);

                return GeometryBridge.FromDMesh3(dmesh);
            }
            catch (System.Exception)
            {
                return geo.Clone();
            }
        }
    }
}
