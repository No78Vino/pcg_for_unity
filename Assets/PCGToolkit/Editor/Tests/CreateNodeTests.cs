using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Create;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class CreateNodeTests : NodeTestBase
    {
        [Test]
        public void BoxNode_DefaultParams_CreatesValidGeometry()
        {
            var result = ExecuteNode<BoxNode>();
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 8, minPrims: 6);
            AssertValidTopology(geo);
        }

        [Test]
        public void BoxNode_CustomSize_HasCorrectExtent()
        {
            var result = ExecuteNode<BoxNode>(parameters: new Dictionary<string, object>
            {
                { "sizeX", 3f },
                { "sizeY", 2f },
                { "sizeZ", 1f }
            });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 8);
            AssertValidTopology(geo);

            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var p in geo.Points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
            }
            Assert.AreEqual(3f, maxX - minX, 0.01f, "Box X extent should be 3.0");
        }

        [Test]
        public void SphereNode_DefaultParams_HasSufficientPoints()
        {
            var result = ExecuteNode<SphereNode>();
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 10, minPrims: 4);
            AssertValidTopology(geo);
        }

        [Test]
        public void GridNode_DefaultParams_CreatesGrid()
        {
            var result = ExecuteNode<GridNode>();
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 4, minPrims: 1);
            AssertValidTopology(geo);
        }

        [Test]
        public void MergeNode_TwoBoxes_CombinesGeometry()
        {
            var box1 = CreateTestBox();
            var box2 = CreateTestBox();
            var result = ExecuteNode<MergeNode>(inputs: new Dictionary<string, PCGGeometry>
            {
                { "input0", box1 },
                { "input1", box2 }
            });
            var geo = result["geometry"];
            Assert.GreaterOrEqual(geo.Points.Count, box1.Points.Count + box2.Points.Count);
        }

        // T1: 新增测试用例
        [Test]
        public void MergeNode_FiveInputs_CombinesAll()
        {
            var box0 = CreateTestBox();
            var box1 = CreateTestBox();
            var box2 = CreateTestBox();
            var box3 = CreateTestBox();
            var box4 = CreateTestBox();
            var result = ExecuteNode<MergeNode>(inputs: new Dictionary<string, PCGGeometry>
            {
                { "input0", box0 },
                { "input1", box1 },
                { "input2", box2 },
                { "input3", box3 },
                { "input4", box4 }
            });
            var geo = result["geometry"];
            Assert.AreEqual(box0.Points.Count + box1.Points.Count + box2.Points.Count + box3.Points.Count + box4.Points.Count, geo.Points.Count);
            Assert.AreEqual(box0.Primitives.Count + box1.Primitives.Count + box2.Primitives.Count + box3.Primitives.Count + box4.Primitives.Count, geo.Primitives.Count);
        }

        [Test]
        public void MergeNode_SparseInputs_SkipsEmpty()
        {
            var box0 = CreateTestBox();
            var box3 = CreateTestBox();
            var result = ExecuteNode<MergeNode>(inputs: new Dictionary<string, PCGGeometry>
            {
                { "input0", box0 },
                // input1, input2 留空
                { "input3", box3 }
                // input4 留空
            });
            var geo = result["geometry"];
            Assert.AreEqual(box0.Points.Count + box3.Points.Count, geo.Points.Count);
        }

        [Test]
        public void MergeNode_SingleInput_PassThrough()
        {
            var box = CreateTestBox();
            var result = ExecuteNode<MergeNode>(inputs: new Dictionary<string, PCGGeometry>
            {
                { "input0", box }
            });
            var geo = result["geometry"];
            Assert.AreEqual(box.Points.Count, geo.Points.Count);
            Assert.AreEqual(box.Primitives.Count, geo.Primitives.Count);
        }

        [Test]
        public void MergeNode_AttributePreservation()
        {
            var box1 = CreateTestBox();
            var box2 = CreateTestBox();

            // 为 box1 设置 Cd 属性
            var cd1 = box1.PointAttribs.CreateAttribute("Cd", AttribType.Color, Color.white);
            for (int i = 0; i < box1.Points.Count; i++)
                cd1.Values.Add(Color.red);

            // 为 box2 设置不同的 Cd 属性
            var cd2 = box2.PointAttribs.CreateAttribute("Cd", AttribType.Color, Color.white);
            for (int i = 0; i < box2.Points.Count; i++)
                cd2.Values.Add(Color.blue);

            var result = ExecuteNode<MergeNode>(inputs: new Dictionary<string, PCGGeometry>
            {
                { "input0", box1 },
                { "input1", box2 }
            });
            var geo = result["geometry"];

            var cdAttr = geo.PointAttribs.GetAttribute("Cd");
            Assert.IsNotNull(cdAttr);
            Assert.AreEqual(geo.Points.Count, cdAttr.Values.Count);
        }

        [Test]
        public void MergeNode_AsymmetricAttributes()
        {
            var box1 = CreateTestBox();
            var box2 = CreateTestBox();

            // box1 有 Cd 属性
            var cd1 = box1.PointAttribs.CreateAttribute("Cd", AttribType.Color, Color.white);
            for (int i = 0; i < box1.Points.Count; i++)
                cd1.Values.Add(Color.red);

            // box2 没有 Cd 属性

            var result = ExecuteNode<MergeNode>(inputs: new Dictionary<string, PCGGeometry>
            {
                { "input0", box1 },
                { "input1", box2 }
            });
            var geo = result["geometry"];

            var cdAttr = geo.PointAttribs.GetAttribute("Cd");
            Assert.IsNotNull(cdAttr);
            Assert.AreEqual(geo.Points.Count, cdAttr.Values.Count);
            // box1 的点应该是红色
            for (int i = 0; i < box1.Points.Count; i++)
                Assert.AreEqual(Color.red, cdAttr.Values[i]);
            // box2 的点应该是默认值
            for (int i = box1.Points.Count; i < geo.Points.Count; i++)
                Assert.AreEqual(Color.white, cdAttr.Values[i]);
        }

        [Test]
        public void LineNode_DefaultParams_CreatesLine()
        {
            var result = ExecuteNode<LineNode>();
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 2);
        }

        [Test]
        public void TransformNode_Translate_MovesPoints()
        {
            var box = CreateTestBox();
            Vector3 center = Vector3.zero;
            foreach (var p in box.Points) center += p;
            center /= box.Points.Count;

            var result = ExecuteNode<TransformNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "translate", new Vector3(10, 0, 0) },
                    { "rotate", Vector3.zero },
                    { "scale", Vector3.one }
                });
            var geo = result["geometry"];

            Vector3 newCenter = Vector3.zero;
            foreach (var p in geo.Points) newCenter += p;
            newCenter /= geo.Points.Count;

            Assert.AreEqual(center.x + 10f, newCenter.x, 0.01f);
            AssertValidTopology(geo);
        }
    }
}
