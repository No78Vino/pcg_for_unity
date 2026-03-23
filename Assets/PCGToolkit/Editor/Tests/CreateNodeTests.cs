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
