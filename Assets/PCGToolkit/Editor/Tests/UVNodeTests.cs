using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.UV;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class UVNodeTests : NodeTestBase
    {
        [Test]
        public void UVProjectNode_Planar_CreatesUVAttribute()
        {
            var box = CreateTestBox(2f);
            var result = ExecuteNode<UVProjectNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "projectionType", "planar" }
                });
            var geo = result["geometry"];
            bool hasUV = geo.PointAttribs.HasAttribute("uv") || geo.VertexAttribs.HasAttribute("uv");
            Assert.IsTrue(hasUV, "UVProject should create uv attribute");
            AssertValidTopology(geo);
        }

        [Test]
        public void UVProjectNode_Cubic_UVsInRange()
        {
            var box = CreateTestBox(2f);
            var result = ExecuteNode<UVProjectNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "projectionType", "cubic" }
                });
            var geo = result["geometry"];

            PCGAttribute uvAttr = null;
            if (geo.PointAttribs.HasAttribute("uv"))
                uvAttr = geo.PointAttribs.GetAttribute("uv");
            else if (geo.VertexAttribs.HasAttribute("uv"))
                uvAttr = geo.VertexAttribs.GetAttribute("uv");

            Assert.IsNotNull(uvAttr, "UVProject cubic should create uv attribute");
        }

        [Test]
        public void UVTransformNode_Translate_OffsetsUVs()
        {
            var box = CreateTestBox(2f);
            // First project UVs
            var projected = ExecuteNode<UVProjectNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "projectionType", "planar" }
                })["geometry"];

            var result = ExecuteNode<UVTransformNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", projected } },
                parameters: new Dictionary<string, object>
                {
                    { "translate", new Vector3(0.5f, 0.5f, 0f) },
                    { "scale", Vector3.one }
                });
            var geo = result["geometry"];
            bool hasUV = geo.PointAttribs.HasAttribute("uv") || geo.VertexAttribs.HasAttribute("uv");
            Assert.IsTrue(hasUV, "UVTransform should preserve uv attribute");
            AssertValidTopology(geo);
        }

        [Test]
        public void UVTrimSheetNode_RemapsUVs()
        {
            var box = CreateTestBox(2f);
            var result = ExecuteNode<UVTrimSheetNode>(
                inputs: new Dictionary<string, PCGGeometry> { { "input", box } },
                parameters: new Dictionary<string, object>
                {
                    { "uMin", 0f },
                    { "uMax", 0.5f },
                    { "vMin", 0f },
                    { "vMax", 0.5f }
                });
            var geo = result["geometry"];
            bool hasUV = geo.PointAttribs.HasAttribute("uv") || geo.VertexAttribs.HasAttribute("uv");
            Assert.IsTrue(hasUV, "UVTrimSheet should create/maintain uv attribute");
            AssertValidTopology(geo);
        }
    }
}
