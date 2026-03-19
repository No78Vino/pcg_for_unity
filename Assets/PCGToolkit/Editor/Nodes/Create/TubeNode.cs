using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成管状/圆柱体几何体（对标 Houdini Tube SOP）
    /// </summary>
    public class TubeNode : PCGNodeBase
    {
        public override string Name => "Tube";
        public override string DisplayName => "Tube";
        public override string Description => "生成一个管状/圆柱体几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radiusOuter", PCGPortDirection.Input, PCGPortType.Float,
                "Outer Radius", "外半径", 0.5f),
            new PCGParamSchema("radiusInner", PCGPortDirection.Input, PCGPortType.Float,
                "Inner Radius", "内半径（0 时为实心圆柱）", 0f),
            new PCGParamSchema("height", PCGPortDirection.Input, PCGPortType.Float,
                "Height", "高度", 1.0f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "高度方向的分段数", 1),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "圆周方向的分段数", 16),
            new PCGParamSchema("endCaps", PCGPortDirection.Input, PCGPortType.Bool,
                "End Caps", "是否封口", true),
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
            float radiusOuter = GetParamFloat(parameters, "radiusOuter", 0.5f);
            float radiusInner = GetParamFloat(parameters, "radiusInner", 0f);
            float height = GetParamFloat(parameters, "height", 1.0f);
            int rows = Mathf.Max(1, GetParamInt(parameters, "rows", 1));
            int columns = Mathf.Max(3, GetParamInt(parameters, "columns", 16));
            bool endCaps = GetParamBool(parameters, "endCaps", true);

            var geo = new PCGGeometry();
            bool isSolid = radiusInner <= 0f;

            float halfHeight = height * 0.5f;

            // 生成顶点
            if (isSolid)
            {
                // 实心圆柱：每层一个中心点 + columns 个边缘点
                for (int row = 0; row <= rows; row++)
                {
                    float y = -halfHeight + height * row / rows;
                    // 边缘点
                    for (int col = 0; col < columns; col++)
                    {
                        float angle = 2f * Mathf.PI * col / columns;
                        geo.Points.Add(new Vector3(
                            radiusOuter * Mathf.Cos(angle),
                            y,
                            radiusOuter * Mathf.Sin(angle)
                        ));
                    }
                }
            }
            else
            {
                // 管状：每层 columns 个内圈点 + columns 个外圈点
                for (int row = 0; row <= rows; row++)
                {
                    float y = -halfHeight + height * row / rows;
                    for (int col = 0; col < columns; col++)
                    {
                        float angle = 2f * Mathf.PI * col / columns;
                        // 外圈
                        geo.Points.Add(new Vector3(
                            radiusOuter * Mathf.Cos(angle),
                            y,
                            radiusOuter * Mathf.Sin(angle)
                        ));
                    }
                    for (int col = 0; col < columns; col++)
                    {
                        float angle = 2f * Mathf.PI * col / columns;
                        // 内圈
                        geo.Points.Add(new Vector3(
                            radiusInner * Mathf.Cos(angle),
                            y,
                            radiusInner * Mathf.Sin(angle)
                        ));
                    }
                }
            }

            // 生成面
            if (isSolid)
            {
                // 侧面
                for (int row = 0; row < rows; row++)
                {
                    int rowStart = row * columns;
                    int nextRowStart = (row + 1) * columns;
                    for (int col = 0; col < columns; col++)
                    {
                        int nextCol = (col + 1) % columns;
                        geo.Primitives.Add(new int[]
                        {
                            rowStart + col,
                            nextRowStart + col,
                            nextRowStart + nextCol,
                            rowStart + nextCol
                        });
                    }
                }

                // 封口
                if (endCaps)
                {
                    // 顶面中心点
                    int topCenter = geo.Points.Count;
                    geo.Points.Add(new Vector3(0, halfHeight, 0));
                    // 底面中心点
                    int bottomCenter = geo.Points.Count;
                    geo.Points.Add(new Vector3(0, -halfHeight, 0));

                    int topRing = rows * columns;
                    int bottomRing = 0;

                    for (int col = 0; col < columns; col++)
                    {
                        int nextCol = (col + 1) % columns;
                        // 顶面（顺时针，法线朝上）
                        geo.Primitives.Add(new int[] { topCenter, topRing + nextCol, topRing + col });
                        // 底面（顺时针，法线朝下）
                        geo.Primitives.Add(new int[] { bottomCenter, bottomRing + col, bottomRing + nextCol });
                    }
                }
            }
            else
            {
                // 管状侧面（外侧面 + 内侧面）
                for (int row = 0; row < rows; row++)
                {
                    int rowStart = row * columns * 2;
                    int nextRowStart = (row + 1) * columns * 2;
                    for (int col = 0; col < columns; col++)
                    {
                        int nextCol = (col + 1) % columns;
                        // 外侧面
                        geo.Primitives.Add(new int[]
                        {
                            rowStart + col,
                            nextRowStart + col,
                            nextRowStart + nextCol,
                            rowStart + nextCol
                        });
                        // 内侧面（注意反向）
                        int innerCol = columns + col;
                        int innerNextCol = columns + nextCol;
                        geo.Primitives.Add(new int[]
                        {
                            rowStart + innerNextCol,
                            nextRowStart + innerNextCol,
                            nextRowStart + innerCol,
                            rowStart + innerCol
                        });
                    }
                }

                // 封口环
                if (endCaps)
                {
                    int topRing = rows * columns * 2;
                    int bottomRing = 0;
                    for (int col = 0; col < columns; col++)
                    {
                        int nextCol = (col + 1) % columns;
                        // 顶环
                        geo.Primitives.Add(new int[]
                        {
                            topRing + col,
                            topRing + columns + col,
                            topRing + columns + nextCol,
                            topRing + nextCol
                        });
                        // 底环
                        geo.Primitives.Add(new int[]
                        {
                            bottomRing + nextCol,
                            bottomRing + columns + nextCol,
                            bottomRing + columns + col,
                            bottomRing + col
                        });
                    }
                }
            }

            return SingleOutput("geometry", geo);
        }
    }
}