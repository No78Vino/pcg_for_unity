using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 对几何体进行平移/旋转/缩放变换（对标 Houdini Transform SOP）
    /// </summary>
    public class TransformNode : PCGNodeBase
    {
        public override string Name => "Transform";
        public override string DisplayName => "Transform";
        public override string Description => "对几何体进行平移、旋转、缩放变换";
        public override PCGNodeCategory Category => PCGNodeCategory.Transform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("translate", PCGPortDirection.Input, PCGPortType.Vector3,
                "Translate", "平移量", Vector3.zero),
            new PCGParamSchema("rotate", PCGPortDirection.Input, PCGPortType.Vector3,
                "Rotate", "旋转角度（欧拉角）", Vector3.zero),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Vector3,
                "Scale", "缩放比例", Vector3.one),
            new PCGParamSchema("uniformScale", PCGPortDirection.Input, PCGPortType.Float,
                "Uniform Scale", "统一缩放", 1.0f),
            new PCGParamSchema("pivot", PCGPortDirection.Input, PCGPortType.Vector3,
                "Pivot", "变换枢轴点", Vector3.zero),
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
            Vector3 rotate = GetParamVector3(parameters, "rotate", Vector3.zero);
            Vector3 scale = GetParamVector3(parameters, "scale", Vector3.one);
            float uniformScale = GetParamFloat(parameters, "uniformScale", 1.0f);
            Vector3 pivot = GetParamVector3(parameters, "pivot", Vector3.zero);

            // 构建变换矩阵：pivot -> scale -> rotate -> translate
            Quaternion rotation = Quaternion.Euler(rotate);
            Vector3 finalScale = scale * uniformScale;

            // 对每个顶点应用变换
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i];
                // 相对于枢轴点
                p -= pivot;
                // 缩放
                p = Vector3.Scale(p, finalScale);
                // 旋转
                p = rotation * p;
                // 平移
                p += pivot + translate;
                geo.Points[i] = p;
            }

            return SingleOutput("geometry", geo);
        }
    }
}