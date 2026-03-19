using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 展开（对标 Houdini UVUnwrap / UVFlatten SOP）
    /// 注意：完整实现需要 xatlas 库，这里提供基础框架
    /// </summary>
    public class UVUnwrapNode : PCGNodeBase
    {
        public override string Name => "UVUnwrap";
        public override string DisplayName => "UV Unwrap";
        public override string Description => "自动展开几何体的 UV（使用 xatlas）";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组展开（留空=全部）", ""),
            new PCGParamSchema("maxStretch", PCGPortDirection.Input, PCGPortType.Float,
                "Max Stretch", "最大拉伸阈值", 0.5f),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "图集分辨率", 1024),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带展开的 UV）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float maxStretch = GetParamFloat(parameters, "maxStretch", 0.5f);
            int resolution = GetParamInt(parameters, "resolution", 1024);

            if (geo.Points.Count == 0 || geo.Primitives.Count == 0)
            {
                ctx.LogWarning("UVUnwrap: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            // 检查是否有现有的 UV 属性
            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null)
            {
                uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector3);
            }
            uvAttr.Values.Clear();

            // 简化实现：为每个顶点生成基于位置的投影 UV
            // 完整实现需要调用 xatlas 库进行参数化展开
            ctx.LogWarning("UVUnwrap: 完整 UV 展开需要 xatlas 库，当前使用简化投影");

            // 计算包围盒
            Vector3 min = geo.Points[0], max = geo.Points[0];
            foreach (var p in geo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 size = max - min;

            // 使用简单的平面投影作为占位
            foreach (var p in geo.Points)
            {
                Vector3 uv = new Vector3(
                    size.x > 0 ? (p.x - min.x) / size.x : 0f,
                    size.z > 0 ? (p.z - min.z) / size.z : 0f,
                    0f
                );
                uvAttr.Values.Add(uv);
            }

            // TODO: 集成 xatlas 进行真正的 UV 展开
            // 1. 将 PCGGeometry 转换为 xatlas 输入格式
            // 2. 调用 xatlasAtlas_AddMesh
            // 3. 调用 xatlasAtlas_Generate
            // 4. 提取展开后的 UV 坐标

            return SingleOutput("geometry", geo);
        }
    }
}