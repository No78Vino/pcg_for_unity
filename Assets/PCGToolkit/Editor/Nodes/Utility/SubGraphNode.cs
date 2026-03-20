using System.Collections.Generic;
using PCGToolkit.Core;
using PCGToolkit.Graph;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// SubGraph 节点：实例化并执行另一个 PCG 图
    /// 对标 Houdini SubNetwork / Object Merge
    /// </summary>
    public class SubGraphNode : PCGNodeBase
    {
        public override string Name => "SubGraph";
        public override string DisplayName => "SubGraph";
        public override string Description => "实例化并执行子图";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "传入子图的几何体", null, required: false),
            new PCGParamSchema("subGraphPath", PCGPortDirection.Input, PCGPortType.String,
                "SubGraph Path", "子图资源路径（Assets/...）", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "子图输出的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string subGraphPath = GetParamString(parameters, "subGraphPath", "");

            if (string.IsNullOrEmpty(subGraphPath))
            {
                ctx.LogWarning("SubGraph: subGraphPath is empty");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 加载子图数据
            var subGraphAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<PCGGraphAsset>(subGraphPath);
            if (subGraphAsset == null || subGraphAsset.GraphData == null)
            {
                ctx.LogWarning($"SubGraph: Failed to load subgraph at {subGraphPath}");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 创建子图执行器
            var subExecutor = new PCGGraphExecutor(subGraphAsset.GraphData);

            // 准备输入数据（注入到 context）
            var inputGeo = GetInputGeometry(inputGeometries, "input");
            ctx.SetExternalInput("geometry", inputGeo);

            // 执行子图
            try
            {
                subExecutor.Execute(ctx);
            }
            catch (System.Exception e)
            {
                ctx.LogError($"SubGraph execution failed: {e.Message}");
            }

            // 获取输出
            if (ctx.TryGetExternalOutput("geometry", out var outputGeo))
            {
                ctx.Log($"SubGraph: executed successfully, output {outputGeo.Points.Count} points");
                return SingleOutput("geometry", outputGeo);
            }

            ctx.LogWarning("SubGraph: no output from subgraph");
            return SingleOutput("geometry", new PCGGeometry());
        }
    }
}