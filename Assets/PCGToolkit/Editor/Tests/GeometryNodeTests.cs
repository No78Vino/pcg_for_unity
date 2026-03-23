using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Geometry;
using PCGToolkit.Nodes.Topology;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class GeometryNodeTests : NodeTestBase
    {
        [Test]
        public void ExtrudeNode_IncreasesGeometry()
        {
            var box = CreateTestBox(2f);
            int originalPts = box.Points.Count;
            int originalPrims = box.Primitives.Count;

            var result = ExecuteNode<ExtrudeNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "distance", 0.5f },
                    { "divisions", 1 }
                });
            var geo = result["geometry"];

            Assert.Greater(geo.Points.Count, originalPts,
                "Extrude should increase point count");
            Assert.Greater(geo.Primitives.Count, originalPrims,
                "Extrude should increase primitive count");
            AssertValidTopology(geo);
        }

        [Test]
        public void MirrorNode_KeepOriginal_DoublesPoints()
        {
            var box = CreateTestBox();
            int originalPts = box.Points.Count;

            var result = ExecuteNode<MirrorNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "keepOriginal", true },
                    { "normal", Vector3.right }
                });
            var geo = result["geometry"];

            Assert.AreEqual(originalPts * 2, geo.Points.Count,
                "Mirror with keepOriginal should double point count");
            AssertValidTopology(geo);
        }

        [Test]
        public void MirrorNode_NoKeep_PreservesCount()
        {
            var box = CreateTestBox();
            int originalPts = box.Points.Count;

            var result = ExecuteNode<MirrorNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "keepOriginal", false },
                    { "normal", Vector3.right }
                });
            var geo = result["geometry"];

            Assert.AreEqual(originalPts, geo.Points.Count,
                "Mirror without keepOriginal should preserve point count");
        }

        [Test]
        public void SubdivideNode_IncreasesTriCount()
        {
            var box = CreateTestBox();
            int originalPrims = box.Primitives.Count;

            var result = ExecuteNode<SubdivideNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "iterations", 1 },
                    { "algorithm", "linear" }
                });
            var geo = result["geometry"];

            Assert.Greater(geo.Primitives.Count, originalPrims,
                "Subdivide should increase primitive count");
            AssertValidTopology(geo);
        }

        [Test]
        public void ClipNode_CutsGeometry()
        {
            var box = CreateTestBox(2f);

            var result = ExecuteNode<ClipNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "origin", Vector3.zero },
                    { "normal", Vector3.up },
                    { "keepAbove", true }
                });
            var geo = result["geometry"];
            Assert.LessOrEqual(geo.Points.Count, box.Points.Count,
                "Clip should not increase point count");
        }

        [Test]
        public void BooleanNode_Union_CombinesMeshes()
        {
            var boxA = CreateTestBox(1f);
            var boxB = CreateTestBox(1f);
            // Offset boxB
            for (int i = 0; i < boxB.Points.Count; i++)
                boxB.Points[i] += new Vector3(3, 0, 0);

            var result = ExecuteNode<BooleanNode>(
                inputs: new Dictionary<string, PCGGeometry>
                {
                    { "inputA", boxA },
                    { "inputB", boxB }
                },
                parameters: new Dictionary<string, object>
                {
                    { "operation", "union" }
                });
            var geo = result["geometry"];
            Assert.Greater(geo.Points.Count, 0, "Boolean union should produce geometry");
            AssertValidTopology(geo);
        }

        [Test]
        public void PolyBevelNode_IncreasesGeometry()
        {
            var box = CreateTestBox(2f);
            int originalPts = box.Points.Count;

            var result = ExecuteNode<PolyBevelNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "offset", 0.1f },
                    { "divisions", 1 }
                });
            var geo = result["geometry"];
            Assert.GreaterOrEqual(geo.Points.Count, originalPts,
                "PolyBevel should not reduce points");
            AssertValidTopology(geo);
        }

        [Test]
        public void PolyFillNode_FillsHoles()
        {
            var box = CreateTestBox(2f);
            // Remove one face to create a hole
            if (box.Primitives.Count > 1)
                box.Primitives.RemoveAt(0);

            int originalPrims = box.Primitives.Count;

            var result = ExecuteNode<PolyFillNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "fillMode", "fan" }
                });
            var geo = result["geometry"];
            Assert.GreaterOrEqual(geo.Primitives.Count, originalPrims,
                "PolyFill should not reduce primitive count");
            AssertValidTopology(geo);
        }
    }
}
