using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Procedural
{
    /// <summary>
    /// L-System 程序化生成（对标 Houdini L-System SOP）
    /// </summary>
    public class LSystemNode : PCGNodeBase
    {
        public override string Name => "LSystem";
        public override string DisplayName => "L-System";
        public override string Description => "使用 Lindenmayer 系统生成分形/植物几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Procedural;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("axiom", PCGPortDirection.Input, PCGPortType.String,
                "Axiom", "初始公理字符串", "F"),
            new PCGParamSchema("rules", PCGPortDirection.Input, PCGPortType.String,
                "Rules", "产生式规则（格式: F=FF+[+F-F-F]-[-F+F+F]，多条规则用分号分隔）",
                "F=FF+[+F-F-F]-[-F+F+F]"),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "迭代次数", 3),
            new PCGParamSchema("angle", PCGPortDirection.Input, PCGPortType.Float,
                "Angle", "转向角度", 25.7f),
            new PCGParamSchema("stepLength", PCGPortDirection.Input, PCGPortType.Float,
                "Step Length", "每步前进长度", 1.0f),
            new PCGParamSchema("stepLengthScale", PCGPortDirection.Input, PCGPortType.Float,
                "Step Length Scale", "每次迭代的长度缩放", 0.5f),
            new PCGParamSchema("thickness", PCGPortDirection.Input, PCGPortType.Float,
                "Thickness", "分支粗细", 0.1f),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子（用于随机规则）", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "生成的几何体（线段）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string axiom = GetParamString(parameters, "axiom", "F");
            string rulesStr = GetParamString(parameters, "rules", "F=FF+[+F-F-F]-[-F+F+F]");
            int iterations = GetParamInt(parameters, "iterations", 3);
            float angle = GetParamFloat(parameters, "angle", 25.7f);
            float stepLength = GetParamFloat(parameters, "stepLength", 1.0f);
            float stepScale = GetParamFloat(parameters, "stepLengthScale", 0.5f);
            float thickness = GetParamFloat(parameters, "thickness", 0.1f);

            // 解析规则
            var rules = ParseRules(rulesStr);

            // 迭代展开字符串
            string current = axiom;
            for (int i = 0; i < iterations; i++)
            {
                var next = new System.Text.StringBuilder();
                foreach (char c in current)
                {
                    if (rules.TryGetValue(c, out string rule))
                        next.Append(rule);
                    else
                        next.Append(c);
                }
                current = next.ToString();
            }

            // 龟壳解释器
            var geo = new PCGGeometry();
            var points = new List<Vector3>();
            var primitives = new List<int[]>();

            var positionStack = new Stack<Vector3>();
            var rotationStack = new Stack<Quaternion>();

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            float currentStep = stepLength;

            foreach (char c in current)
            {
                switch (c)
                {
                    case 'F':
                    case 'G':
                        // 前进并画线
                        Vector3 startPos = position;
                        position += rotation * Vector3.up * currentStep;

                        int idx0 = points.Count;
                        points.Add(startPos);
                        points.Add(position);
                        primitives.Add(new int[] { idx0, idx0 + 1 });
                        break;

                    case 'f':
                        // 前进不画线
                        position += rotation * Vector3.up * currentStep;
                        break;

                    case '+':
                        // 左转
                        rotation *= Quaternion.AngleAxis(angle, Vector3.forward);
                        break;

                    case '-':
                        // 右转
                        rotation *= Quaternion.AngleAxis(-angle, Vector3.forward);
                        break;

                    case '&':
                        // 俯仰下
                        rotation *= Quaternion.AngleAxis(angle, Vector3.right);
                        break;

                    case '^':
                        // 俯仰上
                        rotation *= Quaternion.AngleAxis(-angle, Vector3.right);
                        break;

                    case '\\':
                        // 滚转左
                        rotation *= Quaternion.AngleAxis(angle, Vector3.up);
                        break;

                    case '/':
                        // 滚转右
                        rotation *= Quaternion.AngleAxis(-angle, Vector3.up);
                        break;

                    case '|':
                        // 转180度
                        rotation *= Quaternion.AngleAxis(180, Vector3.up);
                        break;

                    case '[':
                        // 保存状态
                        positionStack.Push(position);
                        rotationStack.Push(rotation);
                        currentStep *= stepScale;
                        break;

                    case ']':
                        // 恢复状态
                        if (positionStack.Count > 0)
                        {
                            position = positionStack.Pop();
                            rotation = rotationStack.Pop();
                            currentStep /= stepScale;
                        }
                        break;

                    case '!':
                        // 缩小
                        currentStep *= stepScale;
                        break;

                    case '\'':
                        // 放大
                        currentStep /= stepScale;
                        break;
                }
            }

            geo.Points = points;
            geo.Primitives = primitives;

            // 存储元数据
            geo.DetailAttribs.SetAttribute("stringLength", current.Length);
            geo.DetailAttribs.SetAttribute("iterations", iterations);

            ctx.Log($"LSystem: axiom={axiom}, iterations={iterations}, stringLength={current.Length}, output={points.Count}pts");
            return SingleOutput("geometry", geo);
        }

        private Dictionary<char, string> ParseRules(string rulesStr)
        {
            var rules = new Dictionary<char, string>();

            foreach (string ruleStr in rulesStr.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = ruleStr.Trim();
                int eqIdx = trimmed.IndexOf('=');
                if (eqIdx > 0 && eqIdx < trimmed.Length - 1)
                {
                    char symbol = trimmed[0];
                    string rule = trimmed.Substring(eqIdx + 1);
                    rules[symbol] = rule;
                }
            }

            return rules;
        }
    }
}