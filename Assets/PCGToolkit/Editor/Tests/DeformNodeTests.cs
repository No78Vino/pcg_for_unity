using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Deform;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class DeformNodeTests : NodeTestBase
    {
        [Test]
        public void SmoothNode_PreservesPointCount()
        {
            var box = CreateTestBox(2f);
            int originalCount = box.Points.Count;
            var result = ExecuteNode<SmoothNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "iterations", 5 },
                    { "strength", 0.5f }
                });
            var geo = result["geometry"];
            Assert.AreEqual(originalCount, geo.Points.Count,
                "Smooth should not change point count");
            AssertValidTopology(geo);
        }

        [Test]
        public void MountainNode_PreservesPointCount()
        {
            var box = CreateTestBox(2f);
            int originalCount = box.Points.Count;
            var result = ExecuteNode<MountainNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "height", 0.5f },
                    { "frequency", 2f },
                    { "seed", 42 }
                });
            var geo = result["geometry"];
            Assert.AreEqual(originalCount, geo.Points.Count,
                "Mountain should not change point count");
            AssertValidTopology(geo);
        }

        [Test]
        public void MountainNode_DisplacesVertices()
        {
            var box = CreateTestBox(2f);
            var result = ExecuteNode<MountainNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "height", 2f },
                    { "frequency", 1f },
                    { "seed", 0 }
                });
            var geo = result["geometry"];

            bool anyDifferent = false;
            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (Vector3.Distance(geo.Points[i], box.Points[i]) > 0.001f)
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(anyDifferent, "Mountain should displace at least one vertex");
        }

        [Test]
        public void BendNode_PreservesPointCount()
        {
            var box = CreateTestBox(2f);
            int originalCount = box.Points.Count;
            var result = ExecuteNode<BendNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "angle", 45f },
                    { "captureLength", 2f }
                });
            var geo = result["geometry"];
            Assert.AreEqual(originalCount, geo.Points.Count,
                "Bend should not change point count");
            AssertValidTopology(geo);
        }

        [Test]
        public void TwistNode_PreservesPointCount()
        {
            var box = CreateTestBox(2f);
            int originalCount = box.Points.Count;
            var result = ExecuteNode<TwistNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "angle", 90f },
                    { "axis", "y" }
                });
            var geo = result["geometry"];
            Assert.AreEqual(originalCount, geo.Points.Count,
                "Twist should not change point count");
            AssertValidTopology(geo);
        }
    }
}
