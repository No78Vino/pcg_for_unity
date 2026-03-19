using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 实例化（对标 Houdini Instance SOP）
    /// 标记几何体为实例引用，支持多种几何体选择
    /// </summary>
    public class InstanceNode : PCGNodeBase
    {
        public override string Name => "Instance";
        public override string DisplayName => "Instance";
        public override string Description => "按属性选择不同几何体实例化到点上";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target Points", "目标点集", null, required: true),
            new PCGParamSchema("instanceAttrib", PCGPortDirection.Input, PCGPortType.String,
                "Instance Attribute", "选择实例的属性名", "instance"),
            new PCGParamSchema("usePointOrient", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Orient", "使用点的 orient 属性控制旋转", true),
            new PCGParamSchema("usePointScale", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Scale", "使用点的 pscale 属性控制缩放", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带实例属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var target = GetInputGeometry(inputGeometries, "target");
            string instanceAttrib = GetParamString(parameters, "instanceAttrib", "instance");
            bool usePointOrient = GetParamBool(parameters, "usePointOrient", true);
            bool usePointScale = GetParamBool(parameters, "usePointScale", true);

            var result = new PCGGeometry();

            if (target.Points.Count == 0)
            {
                ctx.LogWarning("Instance: 目标点集为空");
                return SingleOutput("geometry", result);
            }

            // 复制目标点到结果
            result.Points = new List<Vector3>(target.Points);

            // 复制属性
            var instanceAttr = target.PointAttribs.GetAttribute(instanceAttrib);
            if (instanceAttr != null)
            {
                var resultInstanceAttr = result.PointAttribs.CreateAttribute(instanceAttrib, instanceAttr.Type);
                resultInstanceAttr.Values = new List<object>(instanceAttr.Values);
            }

            // 复制旋转属性
            if (usePointOrient)
            {
                var orientAttr = target.PointAttribs.GetAttribute("orient");
                if (orientAttr != null)
                {
                    var resultOrientAttr = result.PointAttribs.CreateAttribute("orient", orientAttr.Type);
                    resultOrientAttr.Values = new List<object>(orientAttr.Values);
                }
            }

            // 复制缩放属性
            if (usePointScale)
            {
                var scaleAttr = target.PointAttribs.GetAttribute("pscale");
                if (scaleAttr != null)
                {
                    var resultScaleAttr = result.PointAttribs.CreateAttribute("pscale", scaleAttr.Type);
                    resultScaleAttr.Values = new List<object>(scaleAttr.Values);
                }
            }

            // 在 Detail 属性中标记为实例
            var instanceInfoAttr = result.DetailAttribs.CreateAttribute("instanceInfo", AttribType.String);
            instanceInfoAttr.Values.Add($"instanced:{target.Points.Count} points");

            ctx.Log($"Instance: 标记了 {target.Points.Count} 个实例点");

            return SingleOutput("geometry", result);
        }
    }
}