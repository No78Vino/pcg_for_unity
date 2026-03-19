using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成平面网格几何体（对标 Houdini Grid SOP）
    /// </summary>
    public class GridNode : PCGNodeBase
    {
        public override string Name => "Grid";
        public override string DisplayName => "Grid";
        public override string Description => "生成一个平面网格几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("sizeX", PCGPortDirection.Input, PCGPortType.Float,
                "Size X", "X 方向尺寸", 10f),
            new PCGParamSchema("sizeY", PCGPortDirection.Input, PCGPortType.Float,
                "Size Y", "Y 方向尺寸", 10f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "行数", 10),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "列数", 10),
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
            float sizeX = GetParamFloat(parameters, "sizeX", 10f);
            float sizeY = GetParamFloat(parameters, "sizeY", 10f);
            int rows = Mathf.Max(1, GetParamInt(parameters, "rows", 10));
            int columns = Mathf.Max(1, GetParamInt(parameters, "columns", 10));
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            var geo = new PCGGeometry();

            float halfX = sizeX * 0.5f;
            float halfY = sizeY * 0.5f;
            float stepX = sizeX / columns;
            float stepY = sizeY / rows;

            // 生成顶点（在 XZ 平面上，Y=0）
            for (int row = 0; row <= rows; row++)
            {
                for (int col = 0; col <= columns; col++)
                {
                    float x = -halfX + col * stepX;
                    float z = -halfY + row * stepY;
                    geo.Points.Add(center + new Vector3(x, 0, z));
                }
            }

            // 生成四边形面
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int v0 = row * (columns + 1) + col;
                    int v1 = v0 + 1;
                    int v2 = v0 + columns + 2;
                    int v3 = v0 + columns + 1;
                    geo.Primitives.Add(new int[] { v0, v1, v2, v3 });
                }
            }

            return SingleOutput("geometry", geo);
        }
    }
}