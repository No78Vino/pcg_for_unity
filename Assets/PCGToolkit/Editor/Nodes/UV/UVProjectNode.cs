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
                "Projection Type", "投影类型（planar/cylindrical/spherical/cubic）", "planar"),
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
            Vector3 scale = GetParamVector3(parameters, "scale", Vector3.one);
            Vector3 offset = GetParamVector3(parameters, "offset", Vector3.zero);

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo);

            // 计算 UV 并存储为 Vector3 属性（x=U, y=V, z=0）
            var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector3);

            // 计算包围盒中心作为投影原点
            Vector3 center = Vector3.zero;
            foreach (var p in geo.Points) center += p;
            center /= geo.Points.Count;

            switch (projectionType.ToLower())
            {
                case "planar":
                    // XZ 平面投影（从上方投射）
                    foreach (var p in geo.Points)
                    {
                        Vector3 uv = new Vector3(
                            (p.x - center.x) * scale.x + offset.x,
                            (p.z - center.z) * scale.y + offset.y,
                            0f
                        );
                        uvAttr.Values.Add(uv);
                    }
                    break;

                case "cylindrical":
                    // 柱面投影（Y轴为轴心）
                    foreach (var p in geo.Points)
                    {
                        Vector3 local = p - center;
                        float angle = Mathf.Atan2(local.x, local.z);
                        float u = angle / (2f * Mathf.PI) + 0.5f;
                        float v = local.y * scale.y + offset.y;
                        uvAttr.Values.Add(new Vector3(u * scale.x + offset.x, v, 0f));
                    }
                    break;

                case "spherical":
                    // 球面投影
                    foreach (var p in geo.Points)
                    {
                        Vector3 local = (p - center).normalized;
                        float theta = Mathf.Atan2(local.x, local.z);
                        float phi = Mathf.Acos(local.y);
                        float u = theta / (2f * Mathf.PI) + 0.5f;
                        float v = phi / Mathf.PI;
                        uvAttr.Values.Add(new Vector3(u * scale.x + offset.x, v * scale.y + offset.y, 0f));
                    }
                    break;

                case "cubic":
                    // 立方体投影（选择投影面积最大的面）
                    foreach (var p in geo.Points)
                    {
                        Vector3 local = p - center;
                        Vector3 abs = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                        Vector2 uv;

                        if (abs.x >= abs.y && abs.x >= abs.z)
                        {
                            // X 面
                            uv = new Vector2(
                                local.z * Mathf.Sign(local.x) * scale.x + offset.x,
                                local.y * scale.y + offset.y
                            );
                        }
                        else if (abs.y >= abs.x && abs.y >= abs.z)
                        {
                            // Y 面
                            uv = new Vector2(
                                local.x * scale.x + offset.x,
                                local.z * Mathf.Sign(local.y) * scale.y + offset.y
                            );
                        }
                        else
                        {
                            // Z 面
                            uv = new Vector2(
                                local.x * scale.x + offset.x,
                                local.y * Mathf.Sign(local.z) * scale.y + offset.y
                            );
                        }
                        uvAttr.Values.Add(new Vector3(uv.x, uv.y, 0f));
                    }
                    break;

                default:
                    // 默认平面投影
                    foreach (var p in geo.Points)
                    {
                        uvAttr.Values.Add(new Vector3(
                            (p.x - center.x) * scale.x + offset.x,
                            (p.z - center.z) * scale.y + offset.y,
                            0f
                        ));
                    }
                    break;
            }

            return SingleOutput("geometry", geo);
        }
    }
}