using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// SubGraph 输出节点：定义子图的输出端口
    /// </summary>
    public class SubGraphOutputNode : PCGNodeBase
    {
        public override string Name => "SubGraphOutput";
        public override string DisplayName => "SubGraph Output";
        public override string Description => "定义子图的输出端口";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Input, PCGPortType.Geometry,
                "Geometry", "输出到父图的几何体", null, required: true),
        };

        public override PCGParamSchema[] Outputs => new PCGParamSchema[0];

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "geometry");

            // 将输出存储到 context 中，供父图获取
            ctx.SetExternalOutput("geometry", geo);

            ctx.Log($"SubGraphOutput: outputting geometry with {geo.Points.Count} points");
            return new Dictionary<string, PCGGeometry>();
        }
    }
}