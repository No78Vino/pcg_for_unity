using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成环面几何体（对标 Houdini Torus SOP）
    /// </summary>
    public class TorusNode : PCGNodeBase
    {
        public override string Name => "Torus";
        public override string DisplayName => "Torus";
        public override string Description => "生成一个环面（甜甜圈）几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radiusMajor", PCGPortDirection.Input, PCGPortType.Float,
                "Major Radius", "主半径（环心到管心的距离）", 1.0f),
            new PCGParamSchema("radiusMinor", PCGPortDirection.Input, PCGPortType.Float,
                "Minor Radius", "次半径（管的截面半径）", 0.25f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "管截面方向的分段数", 16),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "环周方向的分段数", 32),
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
            float radiusMajor = GetParamFloat(parameters, "radiusMajor", 1.0f);
            float radiusMinor = GetParamFloat(parameters, "radiusMinor", 0.25f);
            int rows = Mathf.Max(3, GetParamInt(parameters, "rows", 16));
            int columns = Mathf.Max(3, GetParamInt(parameters, "columns", 32));
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            var geo = new PCGGeometry();

            // 生成顶点
            for (int col = 0; col < columns; col++)
            {
                float theta = 2f * Mathf.PI * col / columns;
                float cosT = Mathf.Cos(theta);
                float sinT = Mathf.Sin(theta);

                for (int row = 0; row < rows; row++)
                {
                    float phi = 2f * Mathf.PI * row / rows;
                    float cosP = Mathf.Cos(phi);
                    float sinP = Mathf.Sin(phi);

                    float r = radiusMajor + radiusMinor * cosP;
                    geo.Points.Add(center + new Vector3(
                        r * cosT,
                        radiusMinor * sinP,
                        r * sinT
                    ));
                }
            }

            // 生成四边形面
            for (int col = 0; col < columns; col++)
            {
                int nextCol = (col + 1) % columns;
                for (int row = 0; row < rows; row++)
                {
                    int nextRow = (row + 1) % rows;
                    int v0 = col * rows + row;
                    int v1 = col * rows + nextRow;
                    int v2 = nextCol * rows + nextRow;
                    int v3 = nextCol * rows + row;
                    geo.Primitives.Add(new int[] { v0, v1, v2, v3 });
                }
            }

            return SingleOutput("geometry", geo);
        }
    }
}