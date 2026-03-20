using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// Pack 节点：将点打包到 Point Group
    /// 对标 Houdini Pack SOP
    /// </summary>
    public class PackNode : PCGNodeBase
    {
        public override string Name => "Pack";
        public override string DisplayName => "Pack";
        public override string Description => "将几何体打包为 Point Group 或 Named Primitive";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("groupName", PCGPortDirection.Input, PCGPortType.String,
                "Group Name", "输出分组名称", "packed"),
            new PCGParamSchema("createPrimitive", PCGPortDirection.Input, PCGPortType.Bool,
                "Create Primitive", "创建一个代表打包几何体的 Primitive", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "打包后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string groupName = GetParamString(parameters, "groupName", "packed");
            bool createPrimitive = GetParamBool(parameters, "createPrimitive", true);

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Pack: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            // 将所有点添加到指定的 Point Group
            if (!geo.PointGroups.ContainsKey(groupName))
            {
                geo.PointGroups[groupName] = new HashSet<int>();
            }

            for (int i = 0; i < geo.Points.Count; i++)
            {
                geo.PointGroups[groupName].Add(i);
            }

            // 可选：创建一个代表打包几何体的 Primitive
            if (createPrimitive && geo.Primitives.Count > 0)
            {
                // 将所有现有 Primitive 添加到一个 Prim Group
                if (!geo.PrimGroups.ContainsKey(groupName))
                {
                    geo.PrimGroups[groupName] = new HashSet<int>();
                }

                for (int i = 0; i < geo.Primitives.Count; i++)
                {
                    geo.PrimGroups[groupName].Add(i);
                }

                // 添加 Detail 属性记录打包信息
                geo.DetailAttribs.SetAttribute("packed", true);
                geo.DetailAttribs.SetAttribute("packedGroupName", groupName);
                geo.DetailAttribs.SetAttribute("packedPointCount", geo.Points.Count);
                geo.DetailAttribs.SetAttribute("packedPrimCount", geo.Primitives.Count);
            }

            ctx.Log($"Pack: {geo.Points.Count} points packed into group '{groupName}'");
            return SingleOutput("geometry", geo);
        }
    }
}