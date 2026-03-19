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
        public override string Name => "SubGraphInput";
        public override string DisplayName => "SubGraph Input";
        public override string Description => "子图的输入端口";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            // 迭代四修复：添加配置参数，让端口配置能够被持久化
            new PCGParamSchema("portName", PCGPortDirection.Input, PCGPortType.String,
                "Port Name", "端口名称", "input"),
            new PCGParamSchema("portType", PCGPortDirection.Input, PCGPortType.Int,  
                    "Port Type", "端口类型 (0=Geometry, 1=Float, 2=Int, 3=Bool, 4=String, 5=Vector3, 6=Color)", 0)  
                { Min = 0, Max = 6 },
        };

        public override PCGParamSchema[] Outputs => new PCGParamSchema[0]; // 动态生成

        /// <summary>
        /// 动态生成输出端口
        /// </summary>
        public override PCGParamSchema[] GetDynamicOutputs(Dictionary<string, object> parameters)
        {
            var portName = GetParamString(parameters, "portName", "input");
            var portTypeInt = GetParamInt(parameters, "portType", 0);
            var portType = (PCGPortType)portTypeInt;
            
            return new[]
            {
                new PCGParamSchema(portName, PCGPortDirection.Output, portType,
                    portName, "子图输入端口", null),
            };
        }

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var portName = GetParamString(parameters, "portName", "input");
            
            // 输入节点的值由 SubGraphNode 在执行前注入到 context.GlobalVariables
            var key = $"SubGraphInput.{portName}";
            if (ctx.GlobalVariables.TryGetValue(key, out var value) && value is PCGGeometry geo)
            {
                return SingleOutput(portName, geo);
            }
            
            ctx.LogWarning($"SubGraphInput: 未找到输入 '{portName}'");
            return SingleOutput(portName, new PCGGeometry());
        }
    }
}