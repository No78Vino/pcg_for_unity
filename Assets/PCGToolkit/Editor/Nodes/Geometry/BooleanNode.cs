using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 布尔运算模式
    /// </summary>
    public enum BooleanOperation
    {
        Union,
        Intersect,
        Subtract
    }

    /// <summary>
    /// 布尔运算（对标 Houdini Boolean SOP）
    /// 注意：完整实现需要 geometry3Sharp，这里提供基础框架
    /// </summary>
    public class BooleanNode : PCGNodeBase
    {
        public override string Name => "Boolean";
        public override string DisplayName => "Boolean";
        public override string Description => "对两个几何体执行布尔运算（并集/交集/差集）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("inputA", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input A", "第一个几何体", null, required: true),
            new PCGParamSchema("inputB", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input B", "第二个几何体", null, required: true),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "布尔运算类型（union/intersect/subtract）", "union"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "运算结果几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geoA = GetInputGeometry(inputGeometries, "inputA");
            var geoB = GetInputGeometry(inputGeometries, "inputB");
            string operation = GetParamString(parameters, "operation", "union");

            if (geoA.Points.Count == 0)
            {
                ctx.LogWarning("Boolean: Input A 为空");
                return SingleOutput("geometry", geoB.Clone());
            }
            if (geoB.Points.Count == 0)
            {
                ctx.LogWarning("Boolean: Input B 为空");
                return SingleOutput("geometry", operation == "subtract" ? geoA.Clone() : new PCGGeometry());
            }

            // 完整的布尔运算需要 geometry3Sharp 的 DMesh3 和 MeshBoolean
            // 这里提供简化的合并实现作为占位
            ctx.LogWarning("Boolean: 完整布尔运算需要 geometry3Sharp 集成，当前返回简化结果");

            if (operation == "union")
            {
                // 简化：直接合并两个几何体
                var result = geoA.Clone();
                int offset = result.Points.Count;
                result.Points.AddRange(geoB.Points);
                foreach (var prim in geoB.Primitives)
                {
                    var newPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                        newPrim[i] = prim[i] + offset;
                    result.Primitives.Add(newPrim);
                }
                return SingleOutput("geometry", result);
            }
            else if (operation == "subtract")
            {
                // 简化：返回 A（未真正计算差集）
                ctx.Log("Boolean: Subtract 操作需要完整 CSG 实现");
                return SingleOutput("geometry", geoA.Clone());
            }
            else // intersect
            {
                // 简化：返回空几何体
                ctx.Log("Boolean: Intersect 操作需要完整 CSG 实现");
                return SingleOutput("geometry", new PCGGeometry());
            }
        }
    }
}