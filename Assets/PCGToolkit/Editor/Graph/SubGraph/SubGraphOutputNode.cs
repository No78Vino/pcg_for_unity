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
        public override string Name => "SubGraphOutput";
        public override string DisplayName => "SubGraph Output";
        public override string Description => "子图的输出端口";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            // 迭代四修复：添加配置参数，让端口配置能够被持久化
            new PCGParamSchema("portName", PCGPortDirection.Input, PCGPortType.String,
                "Port Name", "端口名称", "output"),
            new PCGParamSchema("portType", PCGPortDirection.Input, PCGPortType.Int,  
                    "Port Type", "端口类型 (0=Geometry, 1=Float, 2=Int, 3=Bool, 4=String, 5=Vector3, 6=Color)", 0)  
                { Min = 0, Max = 6 },
        };

        public override PCGParamSchema[] Outputs => new PCGParamSchema[0]; // 无输出

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var portName = GetParamString(parameters, "portName", "output");
            
            // 尝试从连接的输入获取几何体
            var geo = GetInputGeometry(inputGeometries, portName);
            if (geo != null)
            {
                ctx.GlobalVariables[$"SubGraphOutput.{portName}"] = geo;
            }
            else
            {
                ctx.LogWarning($"SubGraphOutput: 未找到输入 '{portName}'");
            }
            
            return new Dictionary<string, PCGGeometry>(); // 无输出
        }
    }
}