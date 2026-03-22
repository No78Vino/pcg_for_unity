using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// Trim Sheet UV 映射节点（对标 Houdini UV Edit + Group Filter）
    /// 将指定 Group 面的现有 UV 归一化后重映射到目标矩形区域，
    /// 是 Trim Sheet 工作流的核心节点。
    /// </summary>
    public class UVTrimSheetNode : PCGNodeBase
    {
        public override string Name        => "UVTrimSheet";
        public override string DisplayName => "UV Trim Sheet";
        public override string Description => "将指定面组的 UV 映射到 Trim Sheet 贴图的指定矩形区域";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（需已有 UV 属性）", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要重映射的面组名（留空=全部面）", ""),
            new PCGParamSchema("uMin", PCGPortDirection.Input, PCGPortType.Float,
                "U Min", "目标矩形左边界 (0~1)", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("uMax", PCGPortDirection.Input, PCGPortType.Float,
                "U Max", "目标矩形右边界 (0~1)", 1f) { Min = 0f, Max = 1f },
            new PCGParamSchema("vMin", PCGPortDirection.Input, PCGPortType.Float,
                "V Min", "目标矩形下边界 (0~1)", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("vMax", PCGPortDirection.Input, PCGPortType.Float,
                "V Max", "目标矩形上边界 (0~1)", 1f) { Min = 0f, Max = 1f },
            new PCGParamSchema("projectionAxis", PCGPortDirection.Input, PCGPortType.String,
                "Projection Axis", "若几何体没有 UV 则自动投影的方向", "Y")
            {
                EnumOptions = new[] { "X", "Y", "Z" }
            },
            new PCGParamSchema("rotate90", PCGPortDirection.Input, PCGPortType.Bool,
                "Rotate 90°", "将 UV 旋转 90°（横竖条切换）", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（UV 已重映射）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            string group          = GetParamString(parameters, "group", "");
            float  uMin           = GetParamFloat(parameters, "uMin", 0f);
            float  uMax           = GetParamFloat(parameters, "uMax", 1f);
            float  vMin           = GetParamFloat(parameters, "vMin", 0f);
            float  vMax           = GetParamFloat(parameters, "vMax", 1f);
            string projAxis       = GetParamString(parameters, "projectionAxis", "Y").ToUpper();
            bool   rotate90       = GetParamBool(parameters, "rotate90", false);

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo);

            // 确定目标面集合
            HashSet<int> targetPrims = GetTargetPrims(geo, group);
            if (targetPrims.Count == 0)
            {
                ctx.LogWarning($"UVTrimSheet: 未找到面组 '{group}'，跳过");
                return SingleOutput("geometry", geo);
            }

            // 获取或生成 UV 属性
            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null || uvAttr.Values.Count != geo.Points.Count)
            {
                uvAttr = GeneratePlanarUV(geo, projAxis);
                ctx.Log($"UVTrimSheet: 几何体无 UV，自动生成 {projAxis} 轴平面投影");
            }

            // 收集目标面涉及的所有顶点索引
            var targetVerts = new HashSet<int>();
            foreach (int pi in targetPrims)
            {
                if (pi < geo.Primitives.Count)
                    foreach (int vi in geo.Primitives[pi])
                        targetVerts.Add(vi);
            }

            // 计算这些顶点 UV 的当前 AABB
            float srcUMin = float.MaxValue, srcUMax = float.MinValue;
            float srcVMin = float.MaxValue, srcVMax = float.MinValue;

            foreach (int vi in targetVerts)
            {
                if (vi >= uvAttr.Values.Count) continue;
                var uv = (Vector3)uvAttr.Values[vi];
                if (uv.x < srcUMin) srcUMin = uv.x;
                if (uv.x > srcUMax) srcUMax = uv.x;
                if (uv.y < srcVMin) srcVMin = uv.y;
                if (uv.y > srcVMax) srcVMax = uv.y;
            }

            float srcURange = srcUMax - srcUMin;
            float srcVRange = srcVMax - srcVMin;
            if (srcURange < 1e-6f) srcURange = 1f;
            if (srcVRange < 1e-6f) srcVRange = 1f;

            float dstURange = uMax - uMin;
            float dstVRange = vMax - vMin;

            // 将目标顶点的 UV 归一化后重映射到目标矩形
            foreach (int vi in targetVerts)
            {
                if (vi >= uvAttr.Values.Count) continue;
                var src = (Vector3)uvAttr.Values[vi];

                float nu = (src.x - srcUMin) / srcURange; // 归一化到 [0,1]
                float nv = (src.y - srcVMin) / srcVRange;

                if (rotate90)
                {
                    // 旋转 90°：(u,v) → (1-v, u)
                    float tmp = nu;
                    nu = 1f - nv;
                    nv = tmp;
                }

                float newU = uMin + nu * dstURange;
                float newV = vMin + nv * dstVRange;

                uvAttr.Values[vi] = new Vector3(newU, newV, 0f);
            }

            ctx.Log($"UVTrimSheet: Group '{(string.IsNullOrEmpty(group) ? "all" : group)}' → " +
                    $"UV rect [{uMin:F3},{uMax:F3}]×[{vMin:F3},{vMax:F3}]，" +
                    $"影响 {targetVerts.Count} 顶点");

            return SingleOutput("geometry", geo);
        }

        // ── 辅助方法 ────────────────────────────────────────────────────────────

        private HashSet<int> GetTargetPrims(PCGGeometry geo, string group)
        {
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var g))
                return new HashSet<int>(g);

            // 留空 = 全部面
            var all = new HashSet<int>();
            for (int i = 0; i < geo.Primitives.Count; i++)
                all.Add(i);
            return all;
        }

        private PCGAttribute GeneratePlanarUV(PCGGeometry geo, string axis)
        {
            // 计算包围盒
            Vector3 bMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 bMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var p in geo.Points)
            {
                if (p.x < bMin.x) bMin.x = p.x;
                if (p.y < bMin.y) bMin.y = p.y;
                if (p.z < bMin.z) bMin.z = p.z;
                if (p.x > bMax.x) bMax.x = p.x;
                if (p.y > bMax.y) bMax.y = p.y;
                if (p.z > bMax.z) bMax.z = p.z;
            }
            Vector3 size = bMax - bMin;
            if (size.x < 1e-6f) size.x = 1f;
            if (size.y < 1e-6f) size.y = 1f;
            if (size.z < 1e-6f) size.z = 1f;

            var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector3);
            foreach (var p in geo.Points)
            {
                float u, v;
                switch (axis)
                {
                    case "X":
                        u = (p.z - bMin.z) / size.z;
                        v = (p.y - bMin.y) / size.y;
                        break;
                    case "Z":
                        u = (p.x - bMin.x) / size.x;
                        v = (p.y - bMin.y) / size.y;
                        break;
                    default: // Y
                        u = (p.x - bMin.x) / size.x;
                        v = (p.z - bMin.z) / size.z;
                        break;
                }
                uvAttr.Values.Add(new Vector3(u, v, 0f));
            }
            return uvAttr;
        }
    }
}
