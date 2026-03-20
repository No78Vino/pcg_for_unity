using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 向量数学运算节点
    /// </summary>
    public class MathVectorNode : PCGNodeBase
    {
        public override string Name => "MathVector";
        public override string DisplayName => "Math (Vector)";
        public override string Description => "对向量进行数学运算";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("a", PCGPortDirection.Input, PCGPortType.Vector3,
                "A", "向量 A", Vector3.zero),
            new PCGParamSchema("b", PCGPortDirection.Input, PCGPortType.Vector3,
                "B", "向量 B", Vector3.zero),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "运算类型", "add")
            {
                EnumOptions = new[] { "add", "subtract", "multiply", "divide", "dot", "cross", "normalize", "length", "distance", "lerp" }
            },
            new PCGParamSchema("t", PCGPortDirection.Input, PCGPortType.Float,
                "T", "插值参数（用于 lerp）", 0.5f) { Min = 0f, Max = 1f },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("vector", PCGPortDirection.Output, PCGPortType.Vector3,
                "Vector", "向量结果"),
            new PCGParamSchema("scalar", PCGPortDirection.Output, PCGPortType.Float,
                "Scalar", "标量结果（dot/length/distance）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            Vector3 a = GetParamVector3(parameters, "a", Vector3.zero);
            Vector3 b = GetParamVector3(parameters, "b", Vector3.zero);
            string op = GetParamString(parameters, "operation", "add").ToLower();
            float t = GetParamFloat(parameters, "t", 0.5f);

            Vector3 vectorResult = Vector3.zero;
            float scalarResult = 0f;

            switch (op)
            {
                case "add":
                    vectorResult = a + b;
                    break;
                case "subtract":
                    vectorResult = a - b;
                    break;
                case "multiply":
                    vectorResult = Vector3.Scale(a, b);
                    break;
                case "divide":
                    vectorResult = new Vector3(
                        b.x != 0 ? a.x / b.x : 0,
                        b.y != 0 ? a.y / b.y : 0,
                        b.z != 0 ? a.z / b.z : 0);
                    break;
                case "dot":
                    scalarResult = Vector3.Dot(a, b);
                    break;
                case "cross":
                    vectorResult = Vector3.Cross(a, b);
                    break;
                case "normalize":
                    vectorResult = a.normalized;
                    break;
                case "length":
                    scalarResult = a.magnitude;
                    break;
                case "distance":
                    scalarResult = Vector3.Distance(a, b);
                    break;
                case "lerp":
                    vectorResult = Vector3.Lerp(a, b, t);
                    break;
            }

            var result = new Dictionary<string, PCGGeometry>();
            
            var vecGeo = new PCGGeometry();
            vecGeo.DetailAttribs.SetAttribute("value", vectorResult);
            result["vector"] = vecGeo;

            var scalarGeo = new PCGGeometry();
            scalarGeo.DetailAttribs.SetAttribute("value", scalarResult);
            result["scalar"] = scalarGeo;

            return result;
        }
    }
}