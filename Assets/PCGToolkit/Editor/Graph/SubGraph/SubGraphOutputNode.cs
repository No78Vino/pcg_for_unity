using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 迭代四：SubGraph 输出节点
    /// 在子图中标记输出端口，用于向外部返回数据
    /// </summary>
    public class SubGraphOutputNode : PCGNodeBase
    {
        private string _portName = "output";
        private PCGPortType _portType = PCGPortType.Geometry;

        public override string Name => "SubGraphOutput";
        public override string DisplayName => "SubGraph Output";
        public override string Description => "子图的输出端口";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema(_portName, PCGPortDirection.Input, _portType,
                _portName, "子图输出端口", null, required: true),
        };

        public override PCGParamSchema[] Outputs => new PCGParamSchema[0]; // 无输出

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
            // 将输入存储到 context.GlobalVariables 供 SubGraphNode 收集
            var geo = GetInputGeometry(inputGeometries, _portName);
            if (geo != null)
            {
                ctx.GlobalVariables[$"SubGraphOutput.{_portName}"] = geo;
            }
            
            return new Dictionary<string, PCGGeometry>(); // 无输出
        }
    }
}