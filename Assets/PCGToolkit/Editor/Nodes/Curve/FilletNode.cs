using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 曲线倒角/圆角（对标 Houdini Fillet SOP）
    /// </summary>
    public class FilletNode : PCGNodeBase
    {
        public override string Name => "Fillet";
        public override string DisplayName => "Fillet";
        public override string Description => "对曲线或多段线的拐角进行倒角/圆角处理";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线/多段线", null, required: true),
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "倒角半径", 0.1f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "每个拐角的分段数", 4),
            new PCGParamSchema("preserveEnds", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Ends", "保持两端点不变", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "倒角后的曲线"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count < 3)
            {
                ctx.LogWarning("Fillet: 输入曲线点数不足");
                return SingleOutput("geometry", geo);
            }

            float radius = GetParamFloat(parameters, "radius", 0.1f);
            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 4));
            bool preserveEnds = GetParamBool(parameters, "preserveEnds", true);

            var newPoints = new List<Vector3>();

            // 处理每个拐角（除了首尾）
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 current = geo.Points[i];

                // 首尾点处理
                if (i == 0 || i == geo.Points.Count - 1)
                {
                    if (preserveEnds || i == 0 || i == geo.Points.Count - 1)
                    {
                        newPoints.Add(current);
                        continue;
                    }
                }

                // 获取前后点
                Vector3 prev = geo.Points[i - 1];
                Vector3 next = geo.Points[i + 1];

                // 计算前后方向
                Vector3 dirIn = (current - prev).normalized;
                Vector3 dirOut = (next - current).normalized;

                // 计算夹角
                float dot = Vector3.Dot(dirIn, dirOut);
                float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));

                // 如果角度太小（接近直线），直接保留点
                if (angle < 0.01f)
                {
                    newPoints.Add(current);
                    continue;
                }

                // 计算倒角实际偏移距离（不能超过线段长度的一半）
                float distToPrev = Vector3.Distance(prev, current);
                float distToNext = Vector3.Distance(current, next);
                float maxOffset = Mathf.Min(distToPrev, distToNext) * 0.4f;

                // 计算倒角起点和终点距离拐角的距离
                float tanHalfAngle = Mathf.Tan(angle * 0.5f);
                float offsetDist = radius / tanHalfAngle;
                offsetDist = Mathf.Min(offsetDist, maxOffset);

                // 倒角的起点和终点
                Vector3 filletStart = current - dirIn * offsetDist;
                Vector3 filletEnd = current + dirOut * offsetDist;

                // 计算圆弧中心
                Vector3 bisector = (dirIn + dirOut).normalized;
                float centerDist = offsetDist / Mathf.Sin(angle * 0.5f);
                Vector3 center = current - bisector * (centerDist - radius / Mathf.Cos(angle * 0.5f));

                // 计算圆弧平面
                Vector3 normal = Vector3.Cross(dirIn, dirOut).normalized;
                if (normal.sqrMagnitude < 0.001f)
                    normal = Vector3.up;

                // 添加倒角起点
                newPoints.Add(filletStart);

                // 生成圆弧段（使用球面线性插值）
                Vector3 fromCenter = filletStart - center;
                Vector3 toCenter = filletEnd - center;

                float arcAngle = Vector3.Angle(fromCenter, toCenter) * Mathf.Deg2Rad;
                float sinArc = Mathf.Sin(arcAngle);

                for (int j = 1; j < divisions; j++)
                {
                    float t = (float)j / divisions;

                    Vector3 arcPoint;
                    if (sinArc < 0.001f)
                    {
                        // 角度太小，退化为线性插值
                        arcPoint = Vector3.Lerp(filletStart, filletEnd, t);
                    }
                    else
                    {
                        float a = Mathf.Sin((1f - t) * arcAngle) / sinArc;
                        float b = Mathf.Sin(t * arcAngle) / sinArc;
                        arcPoint = center + a * fromCenter + b * toCenter;
                    }
                    newPoints.Add(arcPoint);
                }

                // 添加倒角终点
                newPoints.Add(filletEnd);
            }

            geo.Points = newPoints;

            ctx.Log($"Fillet: radius={radius}, divisions={divisions}, points={geo.Points.Count}");
            return SingleOutput("geometry", geo);
        }
    }
}