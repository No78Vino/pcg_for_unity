using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// SubGraph 节点 — 封装一个子图为单个节点
    /// 用于控制节点图复杂度（单图上限 20~30 节点）
    /// 迭代四：完整实现子图执行
    /// </summary>
    public class SubGraphNode : PCGNodeBase
    {
        private PCGGraphData subGraphData;

        public override string Name => "SubGraph";
        public override string DisplayName => subGraphData != null ? subGraphData.GraphName : "SubGraph";
        public override string Description => "封装的子节点图";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => GetSubGraphInputs();
        public override PCGParamSchema[] Outputs => GetSubGraphOutputs();

        /// <summary>
        /// 设置子图数据
        /// </summary>
        public void SetSubGraphData(PCGGraphData data)
        {
            subGraphData = data;
        }

        /// <summary>
        /// 获取子图数据
        /// </summary>
        public PCGGraphData GetSubGraphData()
        {
            return subGraphData;
        }

        private PCGParamSchema[] GetSubGraphInputs()
        {
            if (subGraphData == null)
            {
                return new[]
                {
                    new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                        "Input", "子图输入", null),
                };
            }
            
            // 迭代四修复：从子图查找 SubGraphOutputNode（输出节点=子图的输入端口）
            var inputs = new List<PCGParamSchema>();
            foreach (var nodeData in subGraphData.Nodes)
            {
                if (nodeData.NodeType == "SubGraphInput")
                {
                    // 从参数中读取端口配置
                    string portName = "output";
                    int portTypeInt = 0;
                    
                    foreach (var param in nodeData.Parameters)
                    {
                        if (param.Key == "portName")
                        {
                            // 反序列化字符串值
                            portName = param.ValueJson;
                        }
                        if (param.Key == "portType")
                        {
                            int.TryParse(param.ValueJson, out portTypeInt);
                        }
                    }
                    
                    inputs.Add(new PCGParamSchema(portName, PCGPortDirection.Input, (PCGPortType)portTypeInt,
                        portName, "子图输入", null));
                }
            }
            
            if (inputs.Count == 0)
            {
                inputs.Add(new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                    "Input", "子图输入", null));
            }
            
            return inputs.ToArray();
        }

        private PCGParamSchema[] GetSubGraphOutputs()
        {
            if (subGraphData == null)
            {
                return new[]
                {
                    new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                        "Geometry", "子图输出"),
                };
            }
            
            // 迭代四修复：从子图查找 SubGraphInputNode（输入节点=子图的输出端口）
            var outputs = new List<PCGParamSchema>();
            foreach (var nodeData in subGraphData.Nodes)
            {
                if (nodeData.NodeType == "SubGraphOutput")
                {
                    string portName = "input";
                    int portTypeInt = 0;
                    
                    foreach (var param in nodeData.Parameters)
                    {
                        if (param.Key == "portName")
                        {
                            portName = param.ValueJson;
                        }
                        if (param.Key == "portType")
                        {
                            int.TryParse(param.ValueJson, out portTypeInt);
                        }
                    }
                    
                    outputs.Add(new PCGParamSchema(portName, PCGPortDirection.Output, (PCGPortType)portTypeInt,
                        portName, "子图输出"));
                }
            }
            
            if (outputs.Count == 0)
            {
                outputs.Add(new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                    "Geometry", "子图输出"));
            }
            
            return outputs.ToArray();
        }

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            if (subGraphData == null)
            {
                ctx.LogWarning("SubGraph: 未设置子图数据");
                var emptyResult = new PCGGeometry();
                return SingleOutput("geometry", emptyResult);
            }
            
            ctx.Log($"SubGraph: 执行子图 '{subGraphData.GraphName}'");
            
            // 创建子执行上下文
            var subContext = new PCGContext(ctx.Debug);
            
            // 将外部输入注入到子上下文
            foreach (var kvp in inputGeometries)
            {
                subContext.GlobalVariables[$"SubGraphInput.{kvp.Key}"] = kvp.Value;
            }
            
            // 创建子图执行器并执行
            var executor = new PCGGraphExecutor(subGraphData);
            executor.Execute(subContext);
            
            // 从子上下文收集输出
            var results = new Dictionary<string, PCGGeometry>();
            foreach (var kvp in subContext.GlobalVariables)
            {
                if (kvp.Key.StartsWith("SubGraphOutput.") && kvp.Value is PCGGeometry geo)
                {
                    string portName = kvp.Key.Substring("SubGraphOutput.".Length);
                    results[portName] = geo;
                }
            }
            
            // 确保至少返回一个输出
            if (results.Count == 0)
            {
                ctx.LogWarning("SubGraph: 子图无输出");
                results["geometry"] = new PCGGeometry();
            }
            
            return results;
        }
    }
}