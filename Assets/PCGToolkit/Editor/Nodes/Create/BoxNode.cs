using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成立方体几何体（对标 Houdini Box SOP）
    /// </summary>
    public class BoxNode : PCGNodeBase
    {
        public override string Name => "Box";
        public override string DisplayName => "Box";
        public override string Description => "生成一个立方体几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("sizeX", PCGPortDirection.Input, PCGPortType.Float,
                "Size X", "X 轴尺寸", 1.0f),
            new PCGParamSchema("sizeY", PCGPortDirection.Input, PCGPortType.Float,
                "Size Y", "Y 轴尺寸", 1.0f),
            new PCGParamSchema("sizeZ", PCGPortDirection.Input, PCGPortType.Float,
                "Size Z", "Z 轴尺寸", 1.0f),
            new PCGParamSchema("center", PCGPortDirection.Input, PCGPortType.Vector3,
                "Center", "中心位置", Vector3.zero),
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
            float sizeX = GetParamFloat(parameters, "sizeX", 1.0f);
            float sizeY = GetParamFloat(parameters, "sizeY", 1.0f);
            float sizeZ = GetParamFloat(parameters, "sizeZ", 1.0f);
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            var geo = new PCGGeometry();

            // 计算半尺寸
            float hx = sizeX * 0.5f;
            float hy = sizeY * 0.5f;
            float hz = sizeZ * 0.5f;

            // 生成 8 个顶点（以 center 为中心）
            // 顶点布局：
            //      7-------6
            //     /|      /|
            //    / |     / |
            //   4-------5  |
            //   |  3----|--2
            //   | /     | /
            //   |/      |/
            //   0-------1
            geo.Points = new List<Vector3>
            {
                center + new Vector3(-hx, -hy, -hz), // 0
                center + new Vector3( hx, -hy, -hz), // 1
                center + new Vector3( hx, -hy,  hz), // 2
                center + new Vector3(-hx, -hy,  hz), // 3
                center + new Vector3(-hx,  hy, -hz), // 4
                center + new Vector3( hx,  hy, -hz), // 5
                center + new Vector3( hx,  hy,  hz), // 6
                center + new Vector3(-hx,  hy,  hz), // 7
            };

            // 生成 6 个四边形面（顺时针 winding，法线朝外）
            geo.Primitives = new List<int[]>
            {
                new[] { 0, 1, 2, 3 }, // 底面 (Y-)
                new[] { 4, 7, 6, 5 }, // 顶面 (Y+)
                new[] { 0, 4, 5, 1 }, // 前面 (Z-)
                new[] { 1, 5, 6, 2 }, // 右面 (X+)
                new[] { 2, 6, 7, 3 }, // 后面 (Z+)
                new[] { 3, 7, 4, 0 }, // 左面 (X-)
            };

            return SingleOutput("geometry", geo);
        }
    }
}
