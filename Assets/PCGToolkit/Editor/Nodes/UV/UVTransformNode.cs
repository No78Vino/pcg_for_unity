using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 变换（对标 Houdini UVTransform SOP）
    /// </summary>
    public class UVTransformNode : PCGNodeBase
    {
        public override string Name => "UVTransform";
        public override string DisplayName => "UV Transform";
        public override string Description => "对 UV 坐标进行平移、旋转、缩放";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("translate", PCGPortDirection.Input, PCGPortType.Vector3,
                "Translate", "UV 平移 (仅 xy 有效)", Vector3.zero),
            new PCGParamSchema("rotate", PCGPortDirection.Input, PCGPortType.Float,
                "Rotate", "UV 旋转角度", 0f),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Vector3,
                "Scale", "UV 缩放 (仅 xy 有效)", Vector3.one),
            new PCGParamSchema("pivot", PCGPortDirection.Input, PCGPortType.Vector3,
                "Pivot", "变换枢轴 (仅 xy 有效)", new Vector3(0.5f, 0.5f, 0f)),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅变换指定分组的 UV（留空=全部）", ""),
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
            Vector3 translate = GetParamVector3(parameters, "translate", Vector3.zero);
            float rotate = GetParamFloat(parameters, "rotate", 0f);
            Vector3 scale = GetParamVector3(parameters, "scale", Vector3.one);
            Vector3 pivot = GetParamVector3(parameters, "pivot", new Vector3(0.5f, 0.5f, 0f));

            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null)
            {
                ctx.LogWarning("UVTransform: 几何体没有 UV 属性");
                return SingleOutput("geometry", geo);
            }

            // 构建变换矩阵
            float cos = Mathf.Cos(rotate * Mathf.Deg2Rad);
            float sin = Mathf.Sin(rotate * Mathf.Deg2Rad);

            // 对每个 UV 执行变换
            for (int i = 0; i < uvAttr.Values.Count; i++)
            {
                Vector3 uv = (Vector3)uvAttr.Values[i];
                
                // 相对于枢轴点
                float u = uv.x - pivot.x;
                float v = uv.y - pivot.y;

                // 缩放
                u *= scale.x;
                v *= scale.y;

                // 旋转
                float newU = u * cos - v * sin;
                float newV = u * sin + v * cos;

                // 平移并恢复枢轴
                newU += pivot.x + translate.x;
                newV += pivot.y + translate.y;

                uvAttr.Values[i] = new Vector3(newU, newV, 0f);
            }

            return SingleOutput("geometry", geo);
        }
    }
}