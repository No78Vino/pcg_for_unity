using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 裁切曲线（对标 Houdini Carve SOP）
    /// </summary>
    public class CarveNode : PCGNodeBase
    {
        public override string Name => "Carve";
        public override string DisplayName => "Carve";
        public override string Description => "按参数范围裁切曲线（保留指定比例范围内的段）";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线", null, required: true),
            new PCGParamSchema("firstU", PCGPortDirection.Input, PCGPortType.Float,
                "First U", "起始参数（0~1）", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("secondU", PCGPortDirection.Input, PCGPortType.Float,
                "Second U", "结束参数（0~1）", 1f) { Min = 0f, Max = 1f },
            new PCGParamSchema("cutAtFirstU", PCGPortDirection.Input, PCGPortType.Bool,
                "Cut at First U", "在起始参数处切断", true),
            new PCGParamSchema("cutAtSecondU", PCGPortDirection.Input, PCGPortType.Bool,
                "Cut at Second U", "在结束参数处切断", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "裁切后的曲线"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count < 2)
            {
                ctx.LogWarning("Carve: 输入曲线点数不足");
                return SingleOutput("geometry", geo);
            }

            float firstU = GetParamFloat(parameters, "firstU", 0f);
            float secondU = GetParamFloat(parameters, "secondU", 1f);
            bool cutAtFirstU = GetParamBool(parameters, "cutAtFirstU", true);
            bool cutAtSecondU = GetParamBool(parameters, "cutAtSecondU", true);

            // 确保参数有效
            if (firstU > secondU)
            {
                float temp = firstU;
                firstU = secondU;
                secondU = temp;
            }
            firstU = Mathf.Clamp01(firstU);
            secondU = Mathf.Clamp01(secondU);

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
                ctx.LogWarning("Carve: 曲线长度为零");
                return SingleOutput("geometry", geo);
            }

            // 找到 U 参数对应的点
            Vector3? firstPoint = null;
            Vector3? secondPoint = null;
            int firstIndex = 0;
            int secondIndex = geo.Points.Count - 1;

            if (cutAtFirstU && firstU > 0f)
            {
                float targetLength = firstU * totalLength;
                for (int i = 1; i < cumulativeLength.Count; i++)
                {
                    if (cumulativeLength[i] >= targetLength)
                    {
                        float segmentStart = cumulativeLength[i - 1];
                        float segmentLength = cumulativeLength[i] - segmentStart;
                        float t = segmentLength > 0 ? (targetLength - segmentStart) / segmentLength : 0f;
                        firstPoint = Vector3.Lerp(geo.Points[i - 1], geo.Points[i], t);
                        firstIndex = i;
                        break;
                    }
                }
            }

            if (cutAtSecondU && secondU < 1f)
            {
                float targetLength = secondU * totalLength;
                for (int i = 1; i < cumulativeLength.Count; i++)
                {
                    if (cumulativeLength[i] >= targetLength)
                    {
                        float segmentStart = cumulativeLength[i - 1];
                        float segmentLength = cumulativeLength[i] - segmentStart;
                        float t = segmentLength > 0 ? (targetLength - segmentStart) / segmentLength : 0f;
                        secondPoint = Vector3.Lerp(geo.Points[i - 1], geo.Points[i], t);
                        secondIndex = i - 1;
                        break;
                    }
                }
            }

            // 构建新的点列表
            var newPoints = new List<Vector3>();

            if (firstPoint.HasValue)
                newPoints.Add(firstPoint.Value);

            int startIndex = cutAtFirstU && firstU > 0f ? firstIndex : 0;
            int endIndex = cutAtSecondU && secondU < 1f ? secondIndex : geo.Points.Count - 1;

            for (int i = startIndex; i <= endIndex && i < geo.Points.Count; i++)
            {
                newPoints.Add(geo.Points[i]);
            }

            if (secondPoint.HasValue && secondPoint.Value != geo.Points[endIndex])
                newPoints.Add(secondPoint.Value);

            geo.Points = newPoints;

            ctx.Log($"Carve: firstU={firstU:F2}, secondU={secondU:F2}, points={geo.Points.Count}");
            return SingleOutput("geometry", geo);
        }
    }
}
