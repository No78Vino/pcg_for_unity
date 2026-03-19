using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成圆形（多边形或线）几何体（对标 Houdini Circle SOP）
    /// </summary>
    public class CircleNode : PCGNodeBase
    {
        public override string Name => "Circle";
        public override string DisplayName => "Circle";
        public override string Description => "生成一个圆形几何体（多边形或线）";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "半径", 1.0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "分段数", 16),
            new PCGParamSchema("arc", PCGPortDirection.Input, PCGPortType.Float,
                "Arc", "弧度角（360 = 完整圆）", 360f),
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
            float radius = GetParamFloat(parameters, "radius", 1.0f);
            int divisions = Mathf.Max(3, GetParamInt(parameters, "divisions", 16));
            float arc = Mathf.Clamp(GetParamFloat(parameters, "arc", 360f), 0f, 360f);
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            var geo = new PCGGeometry();

            bool isFullCircle = Mathf.Approximately(arc, 360f);
            float arcRad = arc * Mathf.Deg2Rad;
            int actualDivisions = isFullCircle ? divisions : Mathf.Max(2, divisions);

            // 生成顶点（在 XZ 平面上）
            for (int i = 0; i < actualDivisions; i++)
            {
                float angle = arcRad * i / (actualDivisions - 1);
                if (isFullCircle)
                {
                    angle = 2f * Mathf.PI * i / divisions;
                }
                geo.Points.Add(center + new Vector3(
                    radius * Mathf.Cos(angle),
                    0,
                    radius * Mathf.Sin(angle)
                ));
            }

            if (isFullCircle)
            {
                // 完整圆：添加中心点，生成三角形扇
                int centerIdx = geo.Points.Count;
                geo.Points.Add(center);
                for (int i = 0; i < divisions; i++)
                {
                    int next = (i + 1) % divisions;
                    geo.Primitives.Add(new int[] { centerIdx, i, next });
                }
            }
            // 否则只保留顶点，形成弧线（无边和面）

            return SingleOutput("geometry", geo);
        }
    }
}