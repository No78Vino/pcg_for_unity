using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// Null 节点：直通传递几何体，不做任何修改。
    /// 用于组织图结构、标记检查点、作为 SubGraph 的输出标记。
    /// 对标 Houdini Null SOP。
    /// </summary>
    public class NullNode : PCGNodeBase
    {
        public override string Name => "Null";
        public override string DisplayName => "Null";
        public override string Description => "直通传递几何体，不做任何修改（用于组织图结构和标记检查点）";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "直通输出（与输入完全相同）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            ctx.Log("Null: pass-through");
            return SingleOutput("geometry", geo);
        }
    }
}