using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// SubGraph 输入节点：定义子图的输入端口
    /// </summary>
    public class SubGraphInputNode : PCGNodeBase
    {
        public override string Name => "SubGraphInput";
        public override string DisplayName => "SubGraph Input";
        public override string Description => "定义子图的输入端口";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        // 动态端口在运行时创建
        public override PCGParamSchema[] Inputs => new PCGParamSchema[0];

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "从父图传入的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            // 从 context 中获取父图传入的数据
            // 父图执行器会在执行前将输入数据注入到 context
            if (ctx.TryGetExternalInput("geometry", out var geo))
            {
                ctx.Log("SubGraphInput: received geometry from parent graph");
                return SingleOutput("geometry", geo);
            }

            ctx.LogWarning("SubGraphInput: no input from parent graph");
            return SingleOutput("geometry", new PCGGeometry());
        }
    }
}