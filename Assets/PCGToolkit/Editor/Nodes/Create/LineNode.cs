using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成线段几何体（对标 Houdini Line SOP）
    /// </summary>
    public class LineNode : PCGNodeBase
    {
        public override string Name => "Line";
        public override string DisplayName => "Line";
        public override string Description => "生成一条线段几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "起点", Vector3.zero),
            new PCGParamSchema("direction", PCGPortDirection.Input, PCGPortType.Vector3,
                "Direction", "方向", Vector3.up),
            new PCGParamSchema("length", PCGPortDirection.Input, PCGPortType.Float,
                "Length", "长度", 1.0f),
            new PCGParamSchema("points", PCGPortDirection.Input, PCGPortType.Int,
                "Points", "点数（包含起点和终点）", 2),
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
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);
            Vector3 direction = GetParamVector3(parameters, "direction", Vector3.up).normalized;
            float length = GetParamFloat(parameters, "length", 1.0f);
            int pointCount = Mathf.Max(2, GetParamInt(parameters, "points", 2));

            var geo = new PCGGeometry();

            // 生成沿线段的顶点
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                geo.Points.Add(origin + direction * (length * t));
            }

            // 生成边（线段没有面，只有边）
            for (int i = 0; i < pointCount - 1; i++)
            {
                geo.Edges.Add(new int[] { i, i + 1 });
            }

            return SingleOutput("geometry", geo);
        }
    }
}