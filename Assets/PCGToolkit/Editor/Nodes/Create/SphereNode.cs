using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成球体几何体（对标 Houdini Sphere SOP）
    /// </summary>
    public class SphereNode : PCGNodeBase
    {
        public override string Name => "Sphere";
        public override string DisplayName => "Sphere";
        public override string Description => "生成一个球体几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "球体半径", 0.5f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "纬度方向的分段数", 16),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "经度方向的分段数", 32),
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
            float radius = GetParamFloat(parameters, "radius", 0.5f);
            int rows = Mathf.Max(2, GetParamInt(parameters, "rows", 16));
            int columns = Mathf.Max(3, GetParamInt(parameters, "columns", 32));
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            var geo = new PCGGeometry();

            // 生成 UV 球体顶点
            // 顶部极点
            geo.Points.Add(center + Vector3.up * radius);
            // 中间环带顶点
            for (int row = 1; row < rows; row++)
            {
                float phi = Mathf.PI * row / rows; // 0 ~ PI
                float y = Mathf.Cos(phi);
                float ringRadius = Mathf.Sin(phi);

                for (int col = 0; col < columns; col++)
                {
                    float theta = 2f * Mathf.PI * col / columns;
                    float x = ringRadius * Mathf.Cos(theta);
                    float z = ringRadius * Mathf.Sin(theta);
                    geo.Points.Add(center + new Vector3(x, y, z) * radius);
                }
            }
            // 底部极点
            geo.Points.Add(center + Vector3.down * radius);

            // 生成面
            // 顶部帽（三角形扇）
            int topPole = 0;
            for (int col = 0; col < columns; col++)
            {
                int nextCol = (col + 1) % columns;
                geo.Primitives.Add(new int[] { topPole, 1 + col, 1 + nextCol });
            }

            // 中间环带（四边形）
            for (int row = 0; row < rows - 2; row++)
            {
                int rowStart = 1 + row * columns;
                int nextRowStart = 1 + (row + 1) * columns;
                for (int col = 0; col < columns; col++)
                {
                    int nextCol = (col + 1) % columns;
                    geo.Primitives.Add(new int[]
                    {
                        rowStart + col,
                        rowStart + nextCol,
                        nextRowStart + nextCol,
                        nextRowStart + col
                    });
                }
            }

            // 底部帽（三角形扇）
            int bottomPole = geo.Points.Count - 1;
            int lastRingStart = 1 + (rows - 2) * columns;
            for (int col = 0; col < columns; col++)
            {
                int nextCol = (col + 1) % columns;
                geo.Primitives.Add(new int[] { bottomPole, lastRingStart + nextCol, lastRingStart + col });
            }

            return SingleOutput("geometry", geo);
        }
    }
}