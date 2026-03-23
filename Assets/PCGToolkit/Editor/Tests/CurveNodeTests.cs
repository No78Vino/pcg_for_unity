using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Curve;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class CurveNodeTests : NodeTestBase
    {
        private PCGGeometry CreateTestCurve(int pointCount = 10)
        {
            var result = ExecuteNode<CurveCreateNode>(parameters: new Dictionary<string, object>
            {
                { "curveType", "polyline" },
                { "shape", "circle" },
                { "pointCount", pointCount },
                { "radius", 1f }
            });
            return result["geometry"];
        }

        [Test]
        public void CurveCreateNode_Circle_HasSufficientPoints()
        {
            var result = ExecuteNode<CurveCreateNode>(parameters: new Dictionary<string, object>
            {
                { "shape", "circle" },
                { "pointCount", 12 }
            });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 12);
        }

        [Test]
        public void CurveCreateNode_Line_CreatesLine()
        {
            var result = ExecuteNode<CurveCreateNode>(parameters: new Dictionary<string, object>
            {
                { "shape", "line" },
                { "pointCount", 5 }
            });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 5);
        }

        [Test]
        public void CurveCreateNode_Bezier_OutputsPoints()
        {
            var result = ExecuteNode<CurveCreateNode>(parameters: new Dictionary<string, object>
            {
                { "curveType", "bezier" },
                { "shape", "circle" },
                { "pointCount", 8 }
            });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 4);
        }

        [Test]
        public void ResampleNode_ByCount_OutputsCorrectCount()
        {
            var curve = CreateTestCurve(20);
            var result = ExecuteNode<ResampleNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", curve } },
                parameters: new Dictionary<string, object>
                {
                    { "method", "count" },
                    { "segments", 10 }
                });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 10);
        }

        [Test]
        public void ResampleNode_ByLength_OutputsPoints()
        {
            var curve = CreateTestCurve(20);
            var result = ExecuteNode<ResampleNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", curve } },
                parameters: new Dictionary<string, object>
                {
                    { "method", "length" },
                    { "length", 0.5f }
                });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 2);
        }

        [Test]
        public void SweepNode_WithBackbone_GeneratesMesh()
        {
            var backbone = CreateTestCurve(10);
            var result = ExecuteNode<SweepNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "backbone", backbone } },
                parameters: new Dictionary<string, object>
                {
                    { "scale", 0.5f },
                    { "divisions", 6 },
                    { "capEnds", true }
                });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 10, minPrims: 1);
            AssertValidTopology(geo);
        }

        [Test]
        public void CarveNode_HalfRange_ReducesPoints()
        {
            var curve = CreateTestCurve(20);
            int originalCount = curve.Points.Count;
            var result = ExecuteNode<CarveNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", curve } },
                parameters: new Dictionary<string, object>
                {
                    { "firstU", 0f },
                    { "secondU", 0.5f }
                });
            var geo = result["geometry"];
            Assert.Less(geo.Points.Count, originalCount,
                "Carving half the curve should reduce point count");
        }

        [Test]
        public void PolyWireNode_GeneratesTubeMesh()
        {
            var curve = CreateTestCurve(10);
            var result = ExecuteNode<PolyWireNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", curve } },
                parameters: new Dictionary<string, object>
                {
                    { "radius", 0.2f },
                    { "sides", 6 }
                });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: 10, minPrims: 1);
            AssertValidTopology(geo);
        }
    }
}
