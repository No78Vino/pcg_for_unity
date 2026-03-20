using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 锥化变形（对标 Houdini Taper SOP）
    /// </summary>
    public class TaperNode : PCGNodeBase
    {
        public override string Name => "Taper";
        public override string DisplayName => "Taper";
        public override string Description => "沿指定轴对几何体进行锥化（渐变缩放）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("scaleStart", PCGPortDirection.Input, PCGPortType.Float,
                "Scale Start", "起始端缩放", 1.0f),
            new PCGParamSchema("scaleEnd", PCGPortDirection.Input, PCGPortType.Float,
                "Scale End", "结束端缩放", 0.0f),
            new PCGParamSchema("axis", PCGPortDirection.Input, PCGPortType.String,
                "Axis", "锥化轴（x/y/z）", "y"),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "锥化中心", Vector3.zero),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "锥化后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Taper: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float scaleStart = GetParamFloat(parameters, "scaleStart", 1.0f);
            float scaleEnd = GetParamFloat(parameters, "scaleEnd", 0.0f);
            string axis = GetParamString(parameters, "axis", "y").ToLower();
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);

            int axisIndex = axis == "x" ? 0 : (axis == "z" ? 2 : 1);

            // 计算几何体在轴向上的范围
            float minCoord = float.MaxValue;
            float maxCoord = float.MinValue;

            foreach (var p in geo.Points)
            {
                float coord = axisIndex == 0 ? p.x - origin.x : (axisIndex == 2 ? p.z - origin.z : p.y - origin.y);
                if (coord < minCoord) minCoord = coord;
                if (coord > maxCoord) maxCoord = coord;
            }

            float range = maxCoord - minCoord;
            if (range < 0.0001f) range = 1f;

            // 对每个点应用锥化
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i] - origin;
                float coord = axisIndex == 0 ? p.x : (axisIndex == 2 ? p.z : p.y);
                float t = (coord - minCoord) / range; // 0~1 比例

                // 线性插值缩放比例
                float scale = Mathf.Lerp(scaleStart, scaleEnd, t);

                // 应用缩放到垂直于轴的截面
                if (axisIndex == 0) // X轴：缩放 YZ 平面
                {
                    p.y *= scale;
                    p.z *= scale;
                }
                else if (axisIndex == 2) // Z轴：缩放 XY 平面
                {
                    p.x *= scale;
                    p.y *= scale;
                }
                else // Y轴：缩放 XZ 平面
                {
                    p.x *= scale;
                    p.z *= scale;
                }

                geo.Points[i] = p + origin;
            }

            ctx.Log($"Taper: scaleStart={scaleStart}, scaleEnd={scaleEnd}, axis={axis}");
            return SingleOutput("geometry", geo);
        }
    }
}
