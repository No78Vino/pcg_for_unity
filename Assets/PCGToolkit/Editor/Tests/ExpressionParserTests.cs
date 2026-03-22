using NUnit.Framework;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Tests
{
    /// <summary>
    /// ExpressionParser 单元测试，覆盖第3轮修复的所有 Bug 场景。
    /// 在 Unity Test Runner (EditMode) 中运行。
    /// </summary>
    [TestFixture]
    public class ExpressionParserTests
    {
        private ExpressionParser _parser;
        private ExpressionParser.EvalContext _ctx;

        [SetUp]
        public void SetUp()
        {
            _parser = new ExpressionParser();
            _ctx = new ExpressionParser.EvalContext
            {
                Geometry = new PCGGeometry(),
                PointIndex = 0,
                PrimIndex = 0,
                TotalPoints = 1,
                TotalPrims = 0,
            };
            _ctx.Geometry.Points.Add(Vector3.zero);
        }

        private float Eval(string expr) => _parser.EvaluateFloat(expr, _ctx);
        private void Run(string code) => _parser.Execute(code, _ctx);

        // ---- Bug 1: 赋值检测不误判 !=/<=/>=  ----

        [Test]
        public void Comparison_LessEqual_NotTreatedAsAssignment()
        {
            Assert.AreEqual(1f, Eval("@P.y <= 5"), 0.001f);
        }

        [Test]
        public void Comparison_GreaterEqual_NotTreatedAsAssignment()
        {
            Assert.AreEqual(1f, Eval("5 >= 3"), 0.001f);
        }

        [Test]
        public void Comparison_NotEqual_NotTreatedAsAssignment()
        {
            Assert.AreEqual(1f, Eval("3 != 5"), 0.001f);
        }

        [Test]
        public void Assignment_NormalEquals_StillWorks()
        {
            Run("float x = 42;");
            Assert.IsTrue(_ctx.Variables.ContainsKey("x"));
            Assert.AreEqual(42f, (float)_ctx.Variables["x"], 0.001f);
        }

        // ---- Bug 2: ParseBlock 与 vector literal {} 不冲突 ----

        [Test]
        public void IfBlock_VectorLiteralAssignment_Works()
        {
            Assert.DoesNotThrow(() => Run("if (@P.y <= 5) { @Cd = {1, 0, 0}; }"));
            Assert.IsTrue(_ctx.Outputs.ContainsKey("@Cd"));
            var cd = (Vector3)_ctx.Outputs["@Cd"];
            Assert.AreEqual(1f, cd.x, 0.001f);
            Assert.AreEqual(0f, cd.y, 0.001f);
        }

        [Test]
        public void IfElse_Block_ExecutesCorrectBranch()
        {
            Run("if (1 > 2) { float r = 10; } else { float r = 20; }");
            Assert.IsTrue(_ctx.Variables.ContainsKey("r"));
            Assert.AreEqual(20f, (float)_ctx.Variables["r"], 0.001f);
        }

        // ---- Bug 3: MatchKeyword 下划线边界 ----

        [Test]
        public void LocalVar_WithUnderscorePrefix_NotMistakenForKeyword()
        {
            Run("float float_value = 7;");
            Assert.IsTrue(_ctx.Variables.ContainsKey("float_value"));
            Assert.AreEqual(7f, (float)_ctx.Variables["float_value"], 0.001f);
        }

        [Test]
        public void LocalVar_IfUnderscorePrefixed_NotMistakenForKeyword()
        {
            Run("float if_cond = 3;");
            Assert.IsTrue(_ctx.Variables.ContainsKey("if_cond"));
        }

        // ---- Bug 5: ParseUnary 负号递归调用正确 ----

        [Test]
        public void UnaryNegate_Simple_Works()
        {
            Assert.AreEqual(-5f, Eval("-5"), 0.001f);
        }

        [Test]
        public void UnaryNegate_OfLogicNot_Works()
        {
            Assert.AreEqual(-1f, Eval("-!0"), 0.001f);
        }

        [Test]
        public void LogicNot_Simple_Works()
        {
            Assert.AreEqual(1f, Eval("!0"), 0.001f);
        }

        [Test]
        public void LogicNot_OfTrue_Works()
        {
            Assert.AreEqual(0f, Eval("!1"), 0.001f);
        }

        // ---- 比较运算符综合 ----

        [Test] public void Comparison_Equal_True()   => Assert.AreEqual(1f, Eval("5 == 5"), 0.001f);
        [Test] public void Comparison_Equal_False()  => Assert.AreEqual(0f, Eval("5 == 6"), 0.001f);
        [Test] public void Comparison_Less_True()    => Assert.AreEqual(1f, Eval("3 < 5"),  0.001f);
        [Test] public void Comparison_Greater_True() => Assert.AreEqual(1f, Eval("5 > 3"),  0.001f);

        // ---- 赋值语句 ----

        [Test]
        public void Assignment_AttributeComponent_Works()
        {
            Run("@P.y = 3;");
            Assert.IsTrue(_ctx.Outputs.ContainsKey("@P"));
            Assert.AreEqual(3f, ((Vector3)_ctx.Outputs["@P"]).y, 0.001f);
        }

        [Test]
        public void Assignment_VectorLiteral_Works()
        {
            Run("@Cd = {0.5, 0.5, 0.5};");
            Assert.IsTrue(_ctx.Outputs.ContainsKey("@Cd"));
            Assert.AreEqual(0.5f, ((Vector3)_ctx.Outputs["@Cd"]).x, 0.001f);
        }

        // ---- 三元表达式 ----

        [Test] public void Ternary_TrueBranch()  => Assert.AreEqual(10f, Eval("1 > 0 ? 10 : 20"), 0.001f);
        [Test] public void Ternary_FalseBranch() => Assert.AreEqual(20f, Eval("0 > 1 ? 10 : 20"), 0.001f);

        // ---- 局部变量 ----

        [Test]
        public void LocalVariable_Float_Declaration()
        {
            Run("float h = 5.5;");
            Assert.IsTrue(_ctx.Variables.ContainsKey("h"));
            Assert.AreEqual(5.5f, (float)_ctx.Variables["h"], 0.001f);
        }

        [Test]
        public void LocalVariable_Int_Declaration()
        {
            Run("int n = 3;");
            Assert.IsTrue(_ctx.Variables.ContainsKey("n"));
            Assert.AreEqual(3, (int)(float)_ctx.Variables["n"]);
        }

        // ---- 内置函数 ----

        [Test] public void Function_Abs_Works()   => Assert.AreEqual(5f, Eval("abs(-5)"),        0.001f);
        [Test] public void Function_Min_Works()   => Assert.AreEqual(3f, Eval("min(3, 7)"),      0.001f);
        [Test] public void Function_Clamp_Works() => Assert.AreEqual(5f, Eval("clamp(10, 0, 5)"), 0.001f);
    }
}
