using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 迭代四：SubGraph 输入节点
    /// 在子图中标记输入端口，用于从外部接收数据
    /// </summary>
    public class SubGraphInputNode : PCGNodeBase
    {
        private string _portName = "input";
        private PCGPortType _portType = PCGPortType.Geometry;

        public override string Name => "SubGraphInput";
        public override string DisplayName => "SubGraph Input";
        public override string Description => "子图的输入端口";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new PCGParamSchema[0]; // 无输入

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema(_portName, PCGPortDirection.Output, _portType,
                _portName, "子图输入端口", null),
        };

        /// <summary>
        /// 设置端口配置
        /// </summary>
        public void SetPortConfig(string portName, PCGPortType portType)
        {
            _portName = portName;
            _portType = portType;
        }

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            // 输入节点的值由 SubGraphNode 在执行前注入到 context.GlobalVariables
            var key = $"SubGraphInput.{_portName}";
            if (ctx.GlobalVariables.TryGetValue(key, out var value) && value is PCGGeometry geo)
            {
                return SingleOutput(_portName, geo);
            }
            
            ctx.LogWarning($"SubGraphInput: 未找到输入 '{_portName}'");
            return SingleOutput(_portName, new PCGGeometry());
        }
    }
}