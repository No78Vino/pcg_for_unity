using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Distribute;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class DistributeNodeTests : NodeTestBase
    {
        private PCGGeometry CreateTestGrid(int rows = 5, int cols = 5)
        {
            var result = ExecuteNode<Nodes.Create.GridNode>(parameters: new Dictionary<string, object>
            {
                { "rows", rows },
                { "columns", cols },
                { "sizeX", 10f },
                { "sizeY", 10f }
            });
            return result["geometry"];
        }

        [Test]
        public void ScatterNode_GeneratesPoints()
        {
            var grid = CreateTestGrid();
            var result = ExecuteNode<ScatterNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", grid } },
                parameters: new Dictionary<string, object>
                {
                    { "count", 50 },
                    { "seed", 42 }
                });
            var geo = result["geometry"];
            Assert.Greater(geo.Points.Count, 0, "Scatter should produce points");
            Assert.LessOrEqual(geo.Points.Count, 50, "Scatter should not exceed requested count");
        }

        [Test]
        public void ScatterNode_DifferentSeed_DifferentPoints()
        {
            var grid = CreateTestGrid();
            var result1 = ExecuteNode<ScatterNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", grid } },
                parameters: new Dictionary<string, object>
                {
                    { "count", 20 },
                    { "seed", 0 }
                })["geometry"];

            var result2 = ExecuteNode<ScatterNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", grid } },
                parameters: new Dictionary<string, object>
                {
                    { "count", 20 },
                    { "seed", 999 }
                })["geometry"];

            bool anyDifferent = false;
            int checkCount = Mathf.Min(result1.Points.Count, result2.Points.Count);
            for (int i = 0; i < checkCount; i++)
            {
                if (Vector3.Distance(result1.Points[i], result2.Points[i]) > 0.001f)
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(anyDifferent, "Different seeds should produce different scatter results");
        }

        [Test]
        public void CopyToPointsNode_MultipliesGeometry()
        {
            var box = CreateTestBox(0.5f);
            int boxPts = box.Points.Count;

            var targetGeo = new PCGGeometry();
            targetGeo.Points.Add(new Vector3(0, 0, 0));
            targetGeo.Points.Add(new Vector3(2, 0, 0));
            targetGeo.Points.Add(new Vector3(4, 0, 0));
            targetGeo.Points.Add(new Vector3(6, 0, 0));

            var result = ExecuteNode<CopyToPointsNode>(
                inputs: new Dictionary<string, PCGGeometry>
                {
                    { "source", box },
                    { "target", targetGeo }
                },
                parameters: new Dictionary<string, object>
                {
                    { "usePointOrient", false },
                    { "usePointScale", false }
                });
            var geo = result["geometry"];
            Assert.AreEqual(boxPts * 4, geo.Points.Count,
                "CopyToPoints should multiply source points by target point count");
        }

        [Test]
        public void ArrayNode_Linear_MultipliesGeometry()
        {
            var box = CreateTestBox();
            int boxPts = box.Points.Count;

            var result = ExecuteNode<ArrayNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "mode", "linear" },
                    { "count", 3 },
                    { "offset", new Vector3(2f, 0f, 0f) }
                });
            var geo = result["geometry"];
            Assert.AreEqual(boxPts * 3, geo.Points.Count,
                "Array count=3 should produce 3x source points");
        }

        [Test]
        public void ArrayNode_Radial_ProducesPoints()
        {
            var box = CreateTestBox(0.5f);
            var result = ExecuteNode<ArrayNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "mode", "radial" },
                    { "count", 6 },
                    { "fullAngle", 360f }
                });
            var geo = result["geometry"];
            AssertGeometry(geo, minPoints: box.Points.Count * 6);
        }
    }
}
