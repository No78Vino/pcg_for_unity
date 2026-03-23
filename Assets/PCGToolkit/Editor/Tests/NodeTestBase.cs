using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Tests
{
    public abstract class NodeTestBase
    {
        protected PCGContext CreateContext() => new PCGContext(debug: true);

        protected Dictionary<string, PCGGeometry> ExecuteNode<T>(
            Dictionary<string, PCGGeometry> inputs = null,
            Dictionary<string, object> parameters = null) where T : PCGNodeBase, new()
        {
            var node = new T();
            var ctx = CreateContext();
            return node.Execute(ctx,
                inputs ?? new Dictionary<string, PCGGeometry>(),
                parameters ?? new Dictionary<string, object>());
        }

        protected void AssertGeometry(PCGGeometry geo,
            int? minPoints = null, int? maxPoints = null,
            int? minPrims = null, int? maxPrims = null)
        {
            Assert.IsNotNull(geo);
            if (minPoints.HasValue) Assert.GreaterOrEqual(geo.Points.Count, minPoints.Value);
            if (maxPoints.HasValue) Assert.LessOrEqual(geo.Points.Count, maxPoints.Value);
            if (minPrims.HasValue) Assert.GreaterOrEqual(geo.Primitives.Count, minPrims.Value);
            if (maxPrims.HasValue) Assert.LessOrEqual(geo.Primitives.Count, maxPrims.Value);
        }

        protected void AssertValidTopology(PCGGeometry geo)
        {
            foreach (var prim in geo.Primitives)
            {
                foreach (int idx in prim)
                {
                    Assert.GreaterOrEqual(idx, 0);
                    Assert.Less(idx, geo.Points.Count,
                        $"Face references vertex {idx} but only {geo.Points.Count} vertices exist");
                }
            }
        }

        protected PCGGeometry CreateTestBox(float size = 1f)
        {
            var boxNode = new Nodes.Create.BoxNode();
            var ctx = CreateContext();
            var result = boxNode.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object>
                {
                    { "sizeX", size },
                    { "sizeY", size },
                    { "sizeZ", size }
                });
            return result["geometry"];
        }

        protected float ComputeAverageEdgeLength(PCGGeometry geo)
        {
            float totalLength = 0;
            int edgeCount = 0;
            foreach (var prim in geo.Primitives)
            {
                for (int i = 0; i < prim.Length; i++)
                {
                    int a = prim[i];
                    int b = prim[(i + 1) % prim.Length];
                    totalLength += Vector3.Distance(geo.Points[a], geo.Points[b]);
                    edgeCount++;
                }
            }
            return edgeCount > 0 ? totalLength / edgeCount : 0;
        }
    }
}
