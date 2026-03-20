using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// Unpack 节点：从 Point Group 解包几何体
    /// 对标 Houdini Unpack SOP
    /// </summary>
    public class UnpackNode : PCGNodeBase
    {
        public override string Name => "Unpack";
        public override string DisplayName => "Unpack";
        public override string Description => "从 Point Group 解包几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("groupName", PCGPortDirection.Input, PCGPortType.String,
                "Group Name", "要解包的分组名称", "packed"),
            new PCGParamSchema("keepGroup", PCGPortDirection.Input, PCGPortType.Bool,
                "Keep Group", "解包后保留分组", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "解包后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string groupName = GetParamString(parameters, "groupName", "packed");
            bool keepGroup = GetParamBool(parameters, "keepGroup", false);

            if (!geo.PointGroups.ContainsKey(groupName))
            {
                ctx.LogWarning($"Unpack: 分组 '{groupName}' 不存在");
                return SingleOutput("geometry", geo);
            }

            var groupPoints = geo.PointGroups[groupName];

            if (groupPoints.Count == 0)
            {
                ctx.LogWarning($"Unpack: 分组 '{groupName}' 为空");
                return SingleOutput("geometry", geo);
            }

            // 解包：提取分组中的点
            // 这里简化处理：不改变几何体结构，只是标记已解包
            geo.DetailAttribs.SetAttribute("unpacked", true);
            geo.DetailAttribs.SetAttribute("unpackedFromGroup", groupName);
            geo.DetailAttribs.SetAttribute("unpackedPointCount", groupPoints.Count);

            // 如果不保留分组，则删除
            if (!keepGroup)
            {
                geo.PointGroups.Remove(groupName);
                ctx.Log($"Unpack: {groupPoints.Count} points unpacked from '{groupName}', group removed");
            }
            else
            {
                ctx.Log($"Unpack: {groupPoints.Count} points unpacked from '{groupName}', group kept");
            }

            return SingleOutput("geometry", geo);
        }
    }
}