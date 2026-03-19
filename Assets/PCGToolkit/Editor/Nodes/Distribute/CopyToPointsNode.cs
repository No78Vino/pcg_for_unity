using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 将几何体复制到点上（对标 Houdini CopyToPoints SOP）
    /// </summary>
    public class CopyToPointsNode : PCGNodeBase
    {
        public override string Name => "CopyToPoints";
        public override string DisplayName => "Copy To Points";
        public override string Description => "将源几何体复制到目标点的每个位置上";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("source", PCGPortDirection.Input, PCGPortType.Geometry,
                "Source", "要复制的源几何体", null, required: true),
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target Points", "目标点集", null, required: true),
            new PCGParamSchema("usePointOrient", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Orient", "使用点的 orient 属性控制旋转", true),
            new PCGParamSchema("usePointScale", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Scale", "使用点的 pscale 属性控制缩放", true),
            new PCGParamSchema("pack", PCGPortDirection.Input, PCGPortType.Bool,
                "Pack", "是否将副本打包为实例", false),
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
            var source = GetInputGeometry(inputGeometries, "source");
            var target = GetInputGeometry(inputGeometries, "target");
            bool usePointOrient = GetParamBool(parameters, "usePointOrient", true);
            bool usePointScale = GetParamBool(parameters, "usePointScale", true);

            var result = new PCGGeometry();

            if (source.Points.Count == 0)
            {
                ctx.LogWarning("CopyToPoints: 源几何体为空");
                return SingleOutput("geometry", result);
            }

            if (target.Points.Count == 0)
            {
                ctx.LogWarning("CopyToPoints: 目标点集为空");
                return SingleOutput("geometry", result);
            }

            // 获取属性
            PCGAttribute orientAttr = null;
            PCGAttribute scaleAttr = null;

            if (usePointOrient)
                orientAttr = target.PointAttribs.GetAttribute("orient");
            if (usePointScale)
                scaleAttr = target.PointAttribs.GetAttribute("pscale");

            // 对每个目标点复制源几何体
            foreach (int primIdx in target.Primitives)
            {
                // 跳过
            }

            for (int pointIdx = 0; pointIdx < target.Points.Count; pointIdx++)
            {
                Vector3 position = target.Points[pointIdx];
                Quaternion rotation = Quaternion.identity;
                float scale = 1f;

                // 从属性读取旋转
                if (orientAttr != null && pointIdx < orientAttr.Values.Count)
                {
                    var orient = (Vector3)orientAttr.Values[pointIdx];
                    rotation = Quaternion.Euler(orient);
                }

                // 从属性读取缩放
                if (scaleAttr != null && pointIdx < scaleAttr.Values.Count)
                {
                    scale = (float)scaleAttr.Values[pointIdx];
                }

                // 计算顶点偏移
                int vertexOffset = result.Points.Count;

                // 复制变换后的顶点
                foreach (var srcPoint in source.Points)
                {
                    Vector3 transformed = rotation * (srcPoint * scale) + position;
                    result.Points.Add(transformed);
                }

                // 复制面（调整索引）
                foreach (var srcPrim in source.Primitives)
                {
                    var newPrim = new int[srcPrim.Length];
                    for (int i = 0; i < srcPrim.Length; i++)
                    {
                        newPrim[i] = srcPrim[i] + vertexOffset;
                    }
                    result.Primitives.Add(newPrim);
                }
            }

            ctx.Log($"CopyToPoints: 复制了 {target.Points.Count} 个实例，共 {result.Points.Count} 个顶点");
            return SingleOutput("geometry", result);
        }
    }
}