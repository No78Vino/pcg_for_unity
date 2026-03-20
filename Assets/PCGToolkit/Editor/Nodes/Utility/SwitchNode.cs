using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// Switch 节点：根据整数索引从多个输入中选择一个输出。
    /// 对标 Houdini Switch SOP。
    /// </summary>
    public class SwitchNode : PCGNodeBase
    {
        public override string Name => "Switch";
        public override string DisplayName => "Switch";
        public override string Description => "根据索引从多个输入中选择一个几何体输出";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        private const int MaxInputs = 4;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input0", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 0", "第一个输入几何体", null, required: false),
            new PCGParamSchema("input1", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 1", "第二个输入几何体", null, required: false),
            new PCGParamSchema("input2", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 2", "第三个输入几何体", null, required: false),
            new PCGParamSchema("input3", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 3", "第四个输入几何体", null, required: false),
            new PCGParamSchema("index", PCGPortDirection.Input, PCGPortType.Int,
                "Select Index", "选择输入的索引（0-3）", 0)
            {
                Min = 0, Max = MaxInputs - 1
            },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "选中的输入几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            int index = GetParamInt(parameters, "index", 0);

            // Clamp 到有效范围
            index = Mathf.Clamp(index, 0, MaxInputs - 1);

            // 查找实际连接的输入端口列表
            var connectedInputs = new List<string>();
            for (int i = 0; i < MaxInputs; i++)
            {
                string portName = $"input{i}";
                if (inputGeometries != null && inputGeometries.ContainsKey(portName) 
                    && inputGeometries[portName] != null)
                {
                    connectedInputs.Add(portName);
                }
            }

            if (connectedInputs.Count == 0)
            {
                ctx.LogWarning("Switch: No inputs connected, outputting empty geometry.");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 如果 index 超出已连接的输入数量，clamp 到最后一个
            string selectedPort = $"input{index}";
            
            // 优先使用精确索引
            if (inputGeometries.ContainsKey(selectedPort) && inputGeometries[selectedPort] != null)
            {
                ctx.Log($"Switch: Selected input{index}");
                return SingleOutput("geometry", inputGeometries[selectedPort]);
            }
            
            // 如果精确索引未连接，fallback 到最近的已连接输入
            // 向下搜索
            for (int i = index; i >= 0; i--)
            {
                string port = $"input{i}";
                if (inputGeometries.ContainsKey(port) && inputGeometries[port] != null)
                {
                    ctx.LogWarning($"Switch: input{index} not connected, falling back to input{i}");
                    return SingleOutput("geometry", inputGeometries[port]);
                }
            }
            // 向上搜索
            for (int i = index + 1; i < MaxInputs; i++)
            {
                string port = $"input{i}";
                if (inputGeometries.ContainsKey(port) && inputGeometries[port] != null)
                {
                    ctx.LogWarning($"Switch: input{index} not connected, falling back to input{i}");
                    return SingleOutput("geometry", inputGeometries[port]);
                }
            }

            ctx.LogWarning("Switch: No valid input found, outputting empty geometry.");
            return SingleOutput("geometry", new PCGGeometry());
        }
    }
}