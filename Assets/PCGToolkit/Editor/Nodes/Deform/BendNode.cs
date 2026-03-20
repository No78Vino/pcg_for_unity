using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 弯曲变形（对标 Houdini Bend SOP）
    /// </summary>
    public class BendNode : PCGNodeBase
    {
        public override string Name => "Bend";
        public override string DisplayName => "Bend";
        public override string Description => "沿指定轴弯曲几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("angle", PCGPortDirection.Input, PCGPortType.Float,
                "Angle", "弯曲角度", 90f),
            new PCGParamSchema("upAxis", PCGPortDirection.Input, PCGPortType.String,
                "Up Axis", "弯曲轴向（x/y/z）", "y"),
            new PCGParamSchema("captureOrigin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Capture Origin", "弯曲起始点", Vector3.zero),
            new PCGParamSchema("captureLength", PCGPortDirection.Input, PCGPortType.Float,
                "Capture Length", "受影响的长度范围", 1.0f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "弯曲后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Bend: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float angle = GetParamFloat(parameters, "angle", 90f);
            string upAxis = GetParamString(parameters, "upAxis", "y").ToLower();
            Vector3 captureOrigin = GetParamVector3(parameters, "captureOrigin", Vector3.zero);
            float captureLength = GetParamFloat(parameters, "captureLength", 1.0f);

            int axisIndex = upAxis == "x" ? 0 : (upAxis == "z" ? 2 : 1);

            // 计算弯曲半径（角度越大，半径越小）
            float angleRad = angle * Mathf.Deg2Rad;
            float radius = captureLength / angleRad;

            // 对每个点应用弯曲变换
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i] - captureOrigin;

                // 获取点在弯曲轴上的位置
                float posOnAxis = axisIndex == 0 ? p.x : (axisIndex == 2 ? p.z : p.y);
                posOnAxis = Mathf.Clamp(posOnAxis, 0f, captureLength);

                // 计算该位置对应的弯曲角度比例
                float t = posOnAxis / captureLength;
                float bendAngle = angleRad * t;

                if (axisIndex == 1) // Y轴弯曲
                {
                    // 原本在 Y 方向的位置转换为圆弧上的角度
                    // X 方向保持不变，Z 方向的位置加上半径
                    float xOffset = p.x;
                    float zOffset = p.z;

                    // 弯曲后：沿 XZ 平面圆弧
                    float arcRadius = radius + zOffset;
                    float newX = xOffset;
                    float newY = Mathf.Sin(bendAngle) * arcRadius;
                    float newZ = Mathf.Cos(bendAngle) * arcRadius - radius;

                    p = new Vector3(newX, newY, newZ);
                }
                else if (axisIndex == 0) // X轴弯曲
                {
                    float arcRadius = radius + p.z;
                    float newX = Mathf.Sin(bendAngle) * arcRadius;
                    float newY = p.y;
                    float newZ = Mathf.Cos(bendAngle) * arcRadius - radius;

                    p = new Vector3(newX, newY, newZ);
                }
                else // Z轴弯曲
                {
                    float arcRadius = radius + p.x;
                    float newX = Mathf.Cos(bendAngle) * arcRadius - radius;
                    float newY = p.y;
                    float newZ = Mathf.Sin(bendAngle) * arcRadius;

                    p = new Vector3(newX, newY, newZ);
                }

                geo.Points[i] = p + captureOrigin;
            }

            ctx.Log($"Bend: angle={angle}°, radius={radius:F2}, upAxis={upAxis}");
            return SingleOutput("geometry", geo);
        }
    }
}
