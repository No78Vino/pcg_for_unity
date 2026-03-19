using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 布局/排列（对标 Houdini UVLayout SOP）
    /// 注意：完整实现需要 xatlas 库
    /// </summary>
    public class UVLayoutNode : PCGNodeBase
    {
        public override string Name => "UVLayout";
        public override string DisplayName => "UV Layout";
        public override string Description => "重新排列 UV 岛以优化空间利用率";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("padding", PCGPortDirection.Input, PCGPortType.Float,
                "Padding", "UV 岛之间的间距", 0.01f),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "布局分辨率", 1024),
            new PCGParamSchema("rotateIslands", PCGPortDirection.Input, PCGPortType.Bool,
                "Rotate Islands", "是否允许旋转 UV 岛以优化排列", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float padding = GetParamFloat(parameters, "padding", 0.01f);
            int resolution = GetParamInt(parameters, "resolution", 1024);
            bool rotateIslands = GetParamBool(parameters, "rotateIslands", true);

            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null)
            {
                ctx.LogWarning("UVLayout: 几何体没有 UV 属性，请先使用 UVProject 或 UVUnwrap");
                return SingleOutput("geometry", geo);
            }

            // 简化实现：将 UV 归一化到 [0,1] 范围
            // 完整实现需要 xatlas 进行 UV 岛打包
            ctx.LogWarning("UVLayout: 完整 UV 布局需要 xatlas 库，当前使用简化处理");

            // 找到当前 UV 的范围
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (var val in uvAttr.Values)
            {
                Vector2 uv = (Vector3)val;
                min = Vector2.Min(min, uv);
                max = Vector2.Max(max, uv);
            }

            // 归一化到 [padding, 1-padding]
            Vector2 size = max - min;
            if (size.x > 0 && size.y > 0)
            {
                float usableSize = 1f - 2f * padding;
                for (int i = 0; i < uvAttr.Values.Count; i++)
                {
                    Vector2 uv = (Vector3)uvAttr.Values[i];
                    uv = new Vector2(
                        padding + (uv.x - min.x) / size.x * usableSize,
                        padding + (uv.y - min.y) / size.y * usableSize
                    );
                    uvAttr.Values[i] = new Vector3(uv.x, uv.y, 0f);
                }
            }

            // TODO: 集成 xatlas 进行真正的 UV 岛打包
            // 1. 识别 UV 岛（连续的 UV 区域）
            // 2. 计算每个岛的包围盒
            // 3. 使用装箱算法排列岛（允许旋转）
            // 4. 更新 UV 坐标

            return SingleOutput("geometry", geo);
        }
    }
}