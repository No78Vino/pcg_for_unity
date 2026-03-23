using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Utility;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class UtilityNodeTests : NodeTestBase
    {
        [Test]
        public void SwitchNode_Index0_ReturnsFirstInput()
        {
            var box1 = CreateTestBox(1f);
            var box2 = CreateTestBox(2f);

            var result = ExecuteNode<SwitchNode>(
                inputs: new Dictionary<string, PCGGeometry>
                {
                    { "input0", box1 },
                    { "input1", box2 }
                },
                parameters: new Dictionary<string, object>
                {
                    { "index", 0 }
                });
            var geo = result["geometry"];
            Assert.AreEqual(box1.Points.Count, geo.Points.Count,
                "Switch index=0 should return first input");
        }

        [Test]
        public void SwitchNode_Index1_ReturnsSecondInput()
        {
            var box1 = CreateTestBox(1f);
            var box2 = CreateTestBox(2f);

            var result = ExecuteNode<SwitchNode>(
                inputs: new Dictionary<string, PCGGeometry>
                {
                    { "input0", box1 },
                    { "input1", box2 }
                },
                parameters: new Dictionary<string, object>
                {
                    { "index", 1 }
                });
            var geo = result["geometry"];
            Assert.AreEqual(box2.Points.Count, geo.Points.Count,
                "Switch index=1 should return second input");
        }

        [Test]
        public void SplitNode_WithGroup_SplitsGeometry()
        {
            var box = CreateTestBox();
            box.PrimGroups["top"] = new HashSet<int> { 0 };

            var result = ExecuteNode<SplitNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "group", "top" }
                });

            Assert.IsTrue(result.ContainsKey("matched"), "Split should have 'matched' output");
            Assert.IsTrue(result.ContainsKey("unmatched"), "Split should have 'unmatched' output");
            var matched = result["matched"];
            var unmatched = result["unmatched"];
            Assert.Greater(matched.Primitives.Count, 0, "Matched output should have primitives");
            Assert.Greater(unmatched.Primitives.Count, 0, "Unmatched output should have primitives");
        }

        [Test]
        public void NullNode_PassesThrough()
        {
            var box = CreateTestBox();
            var result = ExecuteNode<NullNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } });
            var geo = result["geometry"];
            Assert.AreEqual(box.Points.Count, geo.Points.Count,
                "Null should pass through unchanged");
        }

        [Test]
        public void NullNode_EmptyInput_ReturnsEmpty()
        {
            var result = ExecuteNode<NullNode>();
            var geo = result["geometry"];
            Assert.AreEqual(0, geo.Points.Count);
        }

        [Test]
        public void MathFloatNode_Add_ReturnsSum()
        {
            var result = ExecuteNode<MathFloatNode>(parameters: new Dictionary<string, object>
            {
                { "a", 3f },
                { "b", 4f },
                { "operation", "add" }
            });
            var geo = result["result"];
            Assert.IsNotNull(geo);
            Assert.IsTrue(geo.DetailAttribs.HasAttribute("value"),
                "MathFloat should store result in DetailAttribs 'value'");
            float val = (float)geo.DetailAttribs.GetAttribute("value").Values[0];
            Assert.AreEqual(7f, val, 0.001f);
        }

        [Test]
        public void MathFloatNode_Multiply_ReturnsProduct()
        {
            var result = ExecuteNode<MathFloatNode>(parameters: new Dictionary<string, object>
            {
                { "a", 5f },
                { "b", 6f },
                { "operation", "multiply" }
            });
            var geo = result["result"];
            float val = (float)geo.DetailAttribs.GetAttribute("value").Values[0];
            Assert.AreEqual(30f, val, 0.001f);
        }

        [Test]
        public void MathFloatNode_Sqrt_ReturnsRoot()
        {
            var result = ExecuteNode<MathFloatNode>(parameters: new Dictionary<string, object>
            {
                { "a", 25f },
                { "operation", "sqrt" }
            });
            var geo = result["result"];
            float val = (float)geo.DetailAttribs.GetAttribute("value").Values[0];
            Assert.AreEqual(5f, val, 0.001f);
        }

        [Test]
        public void CompareNode_Greater_ReturnsTrue()
        {
            var node = new CompareNode();
            var ctx = CreateContext();
            var result = node.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object>
                {
                    { "a", 5f },
                    { "b", 3f },
                    { "operation", "greater" }
                });

            Assert.IsTrue(ctx.GlobalVariables.ContainsKey("Compare.result"),
                "Compare should store result in GlobalVariables");
        }

        [Test]
        public void CompareNode_Equal_WithTolerance()
        {
            var node = new CompareNode();
            var ctx = CreateContext();
            node.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object>
                {
                    { "a", 1.0f },
                    { "b", 1.00005f },
                    { "operation", "equal" },
                    { "tolerance", 0.001f }
                });

            Assert.IsTrue(ctx.GlobalVariables.ContainsKey("Compare.result"));
        }
    }
}
