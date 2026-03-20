using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 浮点数学运算节点（对标 Houdini Math SOP）
    /// </summary>
    public class MathFloatNode : PCGNodeBase
    {
        public override string Name => "MathFloat";
        public override string DisplayName => "Math (Float)";
        public override string Description => "对浮点数进行数学运算";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("a", PCGPortDirection.Input, PCGPortType.Float,
                "A", "操作数 A", 0f),
            new PCGParamSchema("b", PCGPortDirection.Input, PCGPortType.Float,
                "B", "操作数 B", 0f),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "运算类型", "add")
            {
                EnumOptions = new[] { "add", "subtract", "multiply", "divide", "mod", "pow", "min", "max", "abs", "floor", "ceil", "round", "sqrt", "sin", "cos", "tan" }
            },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("result", PCGPortDirection.Output, PCGPortType.Float,
                "Result", "计算结果"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            float a = GetParamFloat(parameters, "a", 0f);
            float b = GetParamFloat(parameters, "b", 0f);
            string op = GetParamString(parameters, "operation", "add").ToLower();

            float result = op switch
            {
                "add" => a + b,
                "subtract" => a - b,
                "multiply" => a * b,
                "divide" => b != 0 ? a / b : 0f,
                "mod" => b != 0 ? a % b : 0f,
                "pow" => Mathf.Pow(a, b),
                "min" => Mathf.Min(a, b),
                "max" => Mathf.Max(a, b),
                "abs" => Mathf.Abs(a),
                "floor" => Mathf.Floor(a),
                "ceil" => Mathf.Ceil(a),
                "round" => Mathf.Round(a),
                "sqrt" => a >= 0 ? Mathf.Sqrt(a) : 0f,
                "sin" => Mathf.Sin(a * Mathf.Deg2Rad),
                "cos" => Mathf.Cos(a * Mathf.Deg2Rad),
                "tan" => Mathf.Tan(a * Mathf.Deg2Rad),
                _ => a,
            };

            var geo = new PCGGeometry();
            geo.DetailAttribs.SetAttribute("value", result);
            return SingleOutput("result", geo);
        }
    }
}