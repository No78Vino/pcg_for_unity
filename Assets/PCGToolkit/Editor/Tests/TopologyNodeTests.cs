using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Topology;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class TopologyNodeTests : NodeTestBase
    {
        [Test]
        public void RemeshNode_IncreasesPointCount()
        {
            var box = CreateTestBox(2f);
            var result = ExecuteNode<RemeshNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "targetEdgeLength", 0.3f },
                    { "iterations", 3 },
                    { "smoothing", 0.5f },
                    { "preserveBoundary", true }
                });
            var geo = result["geometry"];
            Assert.Greater(geo.Points.Count, box.Points.Count,
                "Remesh should increase vertex count when target edge length < original");
            AssertValidTopology(geo);
        }

        [Test]
        public void RemeshNode_EmptyInput_ReturnsEmpty()
        {
            var result = ExecuteNode<RemeshNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", new PCGGeometry() } });
            var geo = result["geometry"];
            Assert.AreEqual(0, geo.Points.Count);
        }

        [Test]
        public void DecimateNode_ReducesTriangleCount()
        {
            var box = CreateTestBox(2f);
            var remeshed = ExecuteNode<RemeshNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "targetEdgeLength", 0.5f },
                    { "iterations", 2 }
                })["geometry"];

            int originalCount = remeshed.Primitives.Count;
            Assert.Greater(originalCount, 10, "Need enough triangles for decimation test");

            var result = ExecuteNode<DecimateNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", remeshed } },
                parameters: new Dictionary<string, object>
                {
                    { "targetRatio", 0.5f }
                });
            var geo = result["geometry"];

            Assert.Less(geo.Primitives.Count, originalCount,
                "Decimate should reduce triangle count");
            AssertValidTopology(geo);
        }

        [Test]
        public void DecimateNode_EmptyInput_ReturnsEmpty()
        {
            var result = ExecuteNode<DecimateNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", new PCGGeometry() } });
            var geo = result["geometry"];
            Assert.AreEqual(0, geo.Points.Count);
        }
    }
}
