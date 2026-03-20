using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 重采样曲线（对标 Houdini Resample SOP）
    /// </summary>
    public class ResampleNode : PCGNodeBase
    {
        public override string Name => "Resample";
        public override string DisplayName => "Resample";
        public override string Description => "按指定间距或数量重采样曲线/多段线";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线/多段线", null, required: true),
            new PCGParamSchema("method", PCGPortDirection.Input, PCGPortType.String,
                "Method", "采样方式（length/count）", "length"),
            new PCGParamSchema("length", PCGPortDirection.Input, PCGPortType.Float,
                "Length", "每段长度（method=length 时）", 0.1f),
            new PCGParamSchema("segments", PCGPortDirection.Input, PCGPortType.Int,
                "Segments", "总段数（method=count 时）", 10),
            new PCGParamSchema("treatAsSubdivision", PCGPortDirection.Input, PCGPortType.Bool,
                "Treat as Subdivision", "是否在现有点之间细分", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "重采样后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count < 2)
            {
                ctx.LogWarning("Resample: 输入几何体点数不足");
                return SingleOutput("geometry", geo);
            }

            string method = GetParamString(parameters, "method", "length").ToLower();
            float length = GetParamFloat(parameters, "length", 0.1f);
            int segments = GetParamInt(parameters, "segments", 10);
            bool treatAsSubdivision = GetParamBool(parameters, "treatAsSubdivision", false);

            // 计算曲线总长度和每个点的累积弧长
            var cumulativeLength = new List<float> { 0f };
            float totalLength = 0f;

            for (int i = 1; i < geo.Points.Count; i++)
            {
                float segmentLength = Vector3.Distance(geo.Points[i - 1], geo.Points[i]);
                totalLength += segmentLength;
                cumulativeLength.Add(totalLength);
            }

            if (totalLength < 0.0001f)
            {
                ctx.LogWarning("Resample: 曲线长度为零");
                return SingleOutput("geometry", geo);
            }

            // 根据方法确定采样间距
            float segmentLength2;
            if (method == "count")
            {
                segmentLength2 = totalLength / Mathf.Max(1, segments);
            }
            else
            {
                segmentLength2 = Mathf.Max(0.001f, length);
            }

            // 在曲线上均匀采样
            var newPoints = new List<Vector3>();
            newPoints.Add(geo.Points[0]);

            int currentIndex = 1;
            float currentTargetLength = segmentLength2;

            while (currentTargetLength < totalLength && currentIndex < geo.Points.Count)
            {
                // 找到包含目标长度的线段
                while (currentIndex < cumulativeLength.Count && cumulativeLength[currentIndex] < currentTargetLength)
                {
                    currentIndex++;
                }

                if (currentIndex >= cumulativeLength.Count)
                    break;

                float segmentStart = cumulativeLength[currentIndex - 1];
                float segmentEnd = cumulativeLength[currentIndex];
                float segmentLen = segmentEnd - segmentStart;

                if (segmentLen > 0)
                {
                    float t = (currentTargetLength - segmentStart) / segmentLen;
                    Vector3 newPoint = Vector3.Lerp(geo.Points[currentIndex - 1], geo.Points[currentIndex], t);
                    newPoints.Add(newPoint);
                }
                else
                {
                    newPoints.Add(geo.Points[currentIndex]);
                }

                currentTargetLength += segmentLength2;
            }

            // 添加最后一个点（如果启用 subdivision 则不一定添加）
            if (!treatAsSubdivision || method == "count")
            {
                if (newPoints.Count == 0 || Vector3.Distance(newPoints[newPoints.Count - 1], geo.Points[geo.Points.Count - 1]) > 0.001f)
                {
                    newPoints.Add(geo.Points[geo.Points.Count - 1]);
                }
            }

            geo.Points = newPoints;

            ctx.Log($"Resample: method={method}, original={cumulativeLength.Count}, resampled={newPoints.Count}");
            return SingleOutput("geometry", geo);
        }
    }
}
