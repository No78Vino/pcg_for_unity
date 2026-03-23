using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;
using g3;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 减面（对标 Houdini PolyReduce SOP）
    /// 使用 geometry3Sharp 的 Reducer 实现高质量 QEM 减面
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

            var dmesh = GeometryBridge.ToDMesh3(geo);
            int originalCount = dmesh.TriangleCount;

            if (originalCount == 0)
            {
                ctx.LogWarning("Decimate: 转换后的 DMesh3 无三角形");
                return SingleOutput("geometry", geo);
            }

            int finalCount = targetCount > 0
                ? targetCount
                : Mathf.Max(4, Mathf.FloorToInt(originalCount * targetRatio));

            if (finalCount >= originalCount)
            {
                ctx.Log($"Decimate: 目标面数 {finalCount} >= 原始面数 {originalCount}，无需减面");
                return SingleOutput("geometry", geo);
            }

            try
            {
                var reducer = new Reducer(dmesh);

                if (preserveBoundary)
                {
                    var constraints = new MeshConstraints();
                    MeshConstraintUtil.FixAllBoundaryEdges(constraints, dmesh);
                    reducer.SetExternalConstraints(constraints);
                }

                reducer.ReduceToTriangleCount(finalCount);

                var result = GeometryBridge.FromDMesh3(dmesh);
                ctx.Log($"Decimate: {originalCount} -> {result.Primitives.Count} faces ({(float)result.Primitives.Count / originalCount * 100:F1}%)");
                return SingleOutput("geometry", result);
            }
            catch (System.Exception e)
            {
                ctx.LogError($"Decimate: g3 Reducer 异常 — {e.Message}");
                return SingleOutput("geometry", geo);
            }
        }
    }
}
