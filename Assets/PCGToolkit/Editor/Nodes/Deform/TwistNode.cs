using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 扭转变形（对标 Houdini Twist SOP）
    /// </summary>
    public class TwistNode : PCGNodeBase
    {
        public override string Name => "Twist";
        public override string DisplayName => "Twist";
        public override string Description => "沿指定轴扭转几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("angle", PCGPortDirection.Input, PCGPortType.Float,
                "Angle", "总扭转角度", 180f),
            new PCGParamSchema("axis", PCGPortDirection.Input, PCGPortType.String,
                "Axis", "扭转轴（x/y/z）", "y"),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "扭转中心", Vector3.zero),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "扭转后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Twist: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float angle = GetParamFloat(parameters, "angle", 180f);
            string axis = GetParamString(parameters, "axis", "y").ToLower();
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);

            // 计算几何体在轴向上的范围
            float minCoord = float.MaxValue;
            float maxCoord = float.MinValue;
            int axisIndex = axis == "x" ? 0 : (axis == "z" ? 2 : 1);

            foreach (var p in geo.Points)
            {
                float coord = axisIndex == 0 ? p.x - origin.x : (axisIndex == 2 ? p.z - origin.z : p.y - origin.y);
                if (coord < minCoord) minCoord = coord;
                if (coord > maxCoord) maxCoord = coord;
            }

            float range = maxCoord - minCoord;
            if (range < 0.0001f) range = 1f;

            // 对每个点应用扭曲
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i] - origin;
                float coord = axisIndex == 0 ? p.x : (axisIndex == 2 ? p.z : p.y);
                float t = (coord - minCoord) / range; // 0~1 比例

                // 根据比例计算旋转角度
                float twistAngle = angle * t;
                Quaternion rotation;

                if (axisIndex == 0) // X轴
                {
                    rotation = Quaternion.AngleAxis(twistAngle, Vector3.right);
                    p = rotation * new Vector3(0, p.y, p.z);
                    p.x = coord;
                }
                else if (axisIndex == 2) // Z轴
                {
                    rotation = Quaternion.AngleAxis(twistAngle, Vector3.forward);
                    p = rotation * new Vector3(p.x, p.y, 0);
                    p.z = coord;
                }
                else // Y轴（默认）
                {
                    rotation = Quaternion.AngleAxis(twistAngle, Vector3.up);
                    p = rotation * new Vector3(p.x, 0, p.z);
                    p.y = coord;
                }

                geo.Points[i] = p + origin;
            }

            ctx.Log($"Twist: angle={angle}°, axis={axis}, range={range:F2}");
            return SingleOutput("geometry", geo);
        }
    }
}
