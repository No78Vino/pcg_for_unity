using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 投影（对标 Houdini UVProject SOP）
    /// </summary>
    public class UVProjectNode : PCGNodeBase
    {
        public override string Name => "UVProject";
        public override string DisplayName => "UV Project";
        public override string Description => "对几何体进行 UV 投影（平面/柱面/球面/立方体）";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("projectionType", PCGPortDirection.Input, PCGPortType.String,
                "Projection Type", "投影类型（planar/cylindrical/spherical/cubic）", "planar")
            {
                EnumOptions = new[] { "planar", "cylindrical", "spherical", "cubic" }
            },
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组投影（留空=全部）", ""),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Vector3,
                "Scale", "UV 缩放", Vector3.one),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Vector3,
                "Offset", "UV 偏移", Vector3.zero),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带 UV 属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string projectionType = GetParamString(parameters, "projectionType", "planar");
            string group          = GetParamString(parameters, "group", "");
            Vector3 scale         = GetParamVector3(parameters, "scale", Vector3.one);
            Vector3 offset        = GetParamVector3(parameters, "offset", Vector3.zero);

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo);

            // 确定需要投影的顶点集合（通过面组过滤）
            HashSet<int> targetVerts = null;
            if (!string.IsNullOrEmpty(group))
            {
                if (geo.PrimGroups.TryGetValue(group, out var primSet))
                {
                    targetVerts = new HashSet<int>();
                    foreach (int pi in primSet)
                        if (pi < geo.Primitives.Count)
                            foreach (int vi in geo.Primitives[pi])
                                targetVerts.Add(vi);
                }
                else
                {
                    ctx.LogWarning($"UVProject: 面组 '{group}' 不存在，对全部面投影");
                }
            }

            // 获取或创建 UV 属性
            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null)
            {
                uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector3);
                for (int i = 0; i < geo.Points.Count; i++)
                    uvAttr.Values.Add(Vector3.zero);
            }
            else if (uvAttr.Values.Count < geo.Points.Count)
            {
                while (uvAttr.Values.Count < geo.Points.Count)
                    uvAttr.Values.Add(Vector3.zero);
            }

            // 计算包围盒中心作为投影原点
            Vector3 center = Vector3.zero;
            foreach (var p in geo.Points) center += p;
            center /= geo.Points.Count;

            for (int idx = 0; idx < geo.Points.Count; idx++)
            {
                // 如果有 group 过滤且该顶点不在目标集中，跳过
                if (targetVerts != null && !targetVerts.Contains(idx))
                    continue;

                var p = geo.Points[idx];
                Vector3 uv = Vector3.zero;

                switch (projectionType.ToLower())
                {
                    case "cylindrical":
                    {
                        Vector3 local = p - center;
                        float angle = Mathf.Atan2(local.x, local.z);
                        float u = angle / (2f * Mathf.PI) + 0.5f;
                        float v = local.y * scale.y + offset.y;
                        uv = new Vector3(u * scale.x + offset.x, v, 0f);
                        break;
                    }
                    case "spherical":
                    {
                        Vector3 local = (p - center).normalized;
                        float theta = Mathf.Atan2(local.x, local.z);
                        float phi   = Mathf.Acos(Mathf.Clamp(local.y, -1f, 1f));
                        float u     = theta / (2f * Mathf.PI) + 0.5f;
                        float v     = phi / Mathf.PI;
                        uv = new Vector3(u * scale.x + offset.x, v * scale.y + offset.y, 0f);
                        break;
                    }
                    case "cubic":
                    {
                        Vector3 local = p - center;
                        Vector3 abs   = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                        float u, v;
                        if (abs.x >= abs.y && abs.x >= abs.z)
                        {
                            u = local.z * Mathf.Sign(local.x) * scale.x + offset.x;
                            v = local.y * scale.y + offset.y;
                        }
                        else if (abs.y >= abs.x && abs.y >= abs.z)
                        {
                            u = local.x * scale.x + offset.x;
                            v = local.z * Mathf.Sign(local.y) * scale.y + offset.y;
                        }
                        else
                        {
                            u = local.x * scale.x + offset.x;
                            v = local.y * Mathf.Sign(local.z) * scale.y + offset.y;
                        }
                        uv = new Vector3(u, v, 0f);
                        break;
                    }
                    default: // planar — XZ 平面投影
                        uv = new Vector3(
                            (p.x - center.x) * scale.x + offset.x,
                            (p.z - center.z) * scale.y + offset.y,
                            0f);
                        break;
                }

                uvAttr.Values[idx] = uv;
            }

            return SingleOutput("geometry", geo);
        }
    }
}