using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;
using g3;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 重新网格化（对标 Houdini Remesh SOP）
    /// 使用 geometry3Sharp 的 Remesher 实现高质量均匀三角化
    /// </summary>
    public class RemeshNode : PCGNodeBase
    {
        public override string Name => "Remesh";
        public override string DisplayName => "Remesh";
        public override string Description => "重新生成均匀的三角形网格";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("targetEdgeLength", PCGPortDirection.Input, PCGPortType.Float,
                "Target Edge Length", "目标边长", 0.5f),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "迭代次数", 3),
            new PCGParamSchema("smoothing", PCGPortDirection.Input, PCGPortType.Float,
                "Smoothing", "平滑系数", 0.5f),
            new PCGParamSchema("preserveBoundary", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Boundary", "保持边界不变", true),
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
                ctx.LogWarning("Remesh: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float targetLength = GetParamFloat(parameters, "targetEdgeLength", 0.5f);
            int iterations = GetParamInt(parameters, "iterations", 3);
            float smoothing = GetParamFloat(parameters, "smoothing", 0.5f);
            bool preserveBoundary = GetParamBool(parameters, "preserveBoundary", true);

            var dmesh = GeometryBridge.ToDMesh3(geo);

            if (dmesh.TriangleCount == 0)
            {
                ctx.LogWarning("Remesh: 转换后的 DMesh3 无三角形");
                return SingleOutput("geometry", geo);
            }

            try
            {
                var remesher = new Remesher(dmesh);
                remesher.SetTargetEdgeLength(targetLength);
                remesher.SmoothSpeedT = smoothing;
                remesher.EnableSmoothing = smoothing > 0;
                remesher.EnableFlips = true;
                remesher.EnableSplits = true;
                remesher.EnableCollapses = true;

                if (preserveBoundary)
                {
                    var constraints = new MeshConstraints();
                    MeshConstraintUtil.FixAllBoundaryEdges(constraints, dmesh);
                    remesher.SetExternalConstraints(constraints);
                }

                for (int i = 0; i < iterations; i++)
                {
                    remesher.BasicRemeshPass();
                }

                var result = GeometryBridge.FromDMesh3(dmesh);
                ctx.Log($"Remesh: targetLength={targetLength}, iterations={iterations}, output={result.Points.Count}pts, {result.Primitives.Count}tris");
                return SingleOutput("geometry", result);
            }
            catch (System.Exception e)
            {
                ctx.LogError($"Remesh: g3 Remesher 异常 — {e.Message}");
                return SingleOutput("geometry", geo);
            }
        }
    }
}
