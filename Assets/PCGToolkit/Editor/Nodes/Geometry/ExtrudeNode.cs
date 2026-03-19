using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 挤出面（对标 Houdini PolyExtrude SOP）
    /// </summary>
    public class ExtrudeNode : PCGNodeBase
    {
        public override string Name => "Extrude";
        public override string DisplayName => "Extrude";
        public override string Description => "沿法线方向挤出几何体的面";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要挤出的面分组（留空=全部面）", ""),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "挤出距离", 0.5f),
            new PCGParamSchema("inset", PCGPortDirection.Input, PCGPortType.Float,
                "Inset", "内缩距离", 0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "挤出方向的分段数", 1),
            new PCGParamSchema("outputFront", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Front", "是否输出顶面", true),
            new PCGParamSchema("outputSide", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Side", "是否输出侧面", true),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string group = GetParamString(parameters, "group", "");
            float distance = GetParamFloat(parameters, "distance", 0.5f);
            float inset = GetParamFloat(parameters, "inset", 0f);
            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
            bool outputFront = GetParamBool(parameters, "outputFront", true);
            bool outputSide = GetParamBool(parameters, "outputSide", true);

            if (geo.Primitives.Count == 0)
            {
                ctx.LogWarning("Extrude: 输入几何体没有面");
                return SingleOutput("geometry", geo);
            }

            var result = new PCGGeometry();

            // 确定要挤出的面
            HashSet<int> primsToExtrude = new HashSet<int>();
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var groupPrims))
            {
                primsToExtrude = groupPrims;
            }
            else
            {
                for (int i = 0; i < geo.Primitives.Count; i++)
                    primsToExtrude.Add(i);
            }

            // 记录原始顶点到新顶点的映射
            Dictionary<int, List<int>> extrudedVertices = new Dictionary<int, List<int>>();

            foreach (int primIdx in primsToExtrude)
            {
                var prim = geo.Primitives[primIdx];
                if (prim.Length < 3) continue;

                // 计算面法线
                Vector3 normal = CalculateFaceNormal(geo.Points, prim);

                // 计算面中心
                Vector3 center = Vector3.zero;
                foreach (int idx in prim) center += geo.Points[idx];
                center /= prim.Length;

                // 为每个分段创建侧面
                for (int d = 0; d <= divisions; d++)
                {
                    float t = (float)d / divisions;
                    float offset = distance * t;
                    float insetAmount = inset * t;

                    // 创建当前层的顶点
                    int[] layerVertices = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                    {
                        Vector3 origPos = geo.Points[prim[i]];
                        Vector3 toCenter = center - origPos;
                        Vector3 newPos = origPos + normal * offset + toCenter.normalized * insetAmount;

                        int newIdx = result.Points.Count;
                        result.Points.Add(newPos);
                        layerVertices[i] = newIdx;

                        // 记录映射
                        if (!extrudedVertices.ContainsKey(prim[i]))
                            extrudedVertices[prim[i]] = new List<int>();
                        extrudedVertices[prim[i]].Add(newIdx);
                    }

                    // 创建侧面（除了第一层）
                    if (d > 0 && outputSide)
                    {
                        for (int i = 0; i < prim.Length; i++)
                        {
                            int next = (i + 1) % prim.Length;
                            int prevLayer = d - 1;
                            int prevIdx = prevLayer * prim.Length + result.Points.Count - (divisions + 1) * prim.Length + i;
                            int prevNextIdx = prevLayer * prim.Length + result.Points.Count - (divisions + 1) * prim.Length + next;
                            
                            // 重新计算前一层的索引
                            prevIdx = (d - 1) * prim.Length + i;
                            prevNextIdx = (d - 1) * prim.Length + next;
                            
                            result.Primitives.Add(new int[] { prevIdx, prevNextIdx, layerVertices[next], layerVertices[i] });
                        }
                    }
                }

                // 输出顶面
                if (outputFront)
                {
                    int lastLayerStart = divisions * prim.Length;
                    int[] frontPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                    {
                        frontPrim[i] = lastLayerStart + (prim.Length - 1 - i); // 反向以保持正确朝向
                    }
                    result.Primitives.Add(frontPrim);
                }
            }

            // 添加未挤出的原始面
            for (int i = 0; i < geo.Primitives.Count; i++)
            {
                if (!primsToExtrude.Contains(i))
                {
                    result.Primitives.Add((int[])geo.Primitives[i].Clone());
                }
            }

            // 添加未涉及的原始顶点
            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (!extrudedVertices.ContainsKey(i))
                {
                    result.Points.Add(geo.Points[i]);
                }
            }

            return SingleOutput("geometry", result);
        }

        private Vector3 CalculateFaceNormal(List<Vector3> points, int[] prim)
        {
            if (prim.Length < 3) return Vector3.up;
            Vector3 v0 = points[prim[0]];
            Vector3 v1 = points[prim[1]];
            Vector3 v2 = points[prim[2]];
            return Vector3.Cross(v1 - v0, v2 - v0).normalized;
        }
    }
}