using System.Collections.Generic;
using NUnit.Framework;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class SelectionTests : NodeTestBase
    {
        private PCGGeometry CreateQuadGeo()
        {
            var geo = new PCGGeometry();
            // 4 points forming a 2x2 grid (4 quads = 8 triangles)
            geo.Points.Add(new Vector3(0, 0, 0)); // 0
            geo.Points.Add(new Vector3(1, 0, 0)); // 1
            geo.Points.Add(new Vector3(2, 0, 0)); // 2
            geo.Points.Add(new Vector3(0, 0, 1)); // 3
            geo.Points.Add(new Vector3(1, 0, 1)); // 4
            geo.Points.Add(new Vector3(2, 0, 1)); // 5
            geo.Points.Add(new Vector3(0, 0, 2)); // 6
            geo.Points.Add(new Vector3(1, 0, 2)); // 7
            geo.Points.Add(new Vector3(2, 0, 2)); // 8

            // 4 quads (each as 2 triangles = 8 primitives)
            geo.Primitives.Add(new int[] { 0, 1, 4 }); // 0
            geo.Primitives.Add(new int[] { 0, 4, 3 }); // 1
            geo.Primitives.Add(new int[] { 1, 2, 5 }); // 2
            geo.Primitives.Add(new int[] { 1, 5, 4 }); // 3
            geo.Primitives.Add(new int[] { 3, 4, 7 }); // 4
            geo.Primitives.Add(new int[] { 3, 7, 6 }); // 5
            geo.Primitives.Add(new int[] { 4, 5, 8 }); // 6
            geo.Primitives.Add(new int[] { 4, 8, 7 }); // 7

            return geo;
        }

        [SetUp]
        public void Setup()
        {
            PCGSelectionState.Clear();
            PCGSelectionState.SourceGeometry = null;
        }

        // ---- A8: Phase A Tests ----

        [Test]
        public void SelectionState_AddFace_ContainsCorrectIndex()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(2);

            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(2));
            Assert.AreEqual(1, PCGSelectionState.SelectionCount);
        }

        [Test]
        public void SelectionState_ShiftAddMultipleFaces()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);
            PCGSelectionState.AddToSelection(1);
            PCGSelectionState.AddToSelection(3);

            Assert.AreEqual(3, PCGSelectionState.SelectedPrimIndices.Count);
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(0));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(1));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(3));
        }

        [Test]
        public void SelectionState_CtrlRemoveFace()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);
            PCGSelectionState.AddToSelection(1);
            PCGSelectionState.RemoveFromSelection(0);

            Assert.AreEqual(1, PCGSelectionState.SelectedPrimIndices.Count);
            Assert.IsFalse(PCGSelectionState.SelectedPrimIndices.Contains(0));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(1));
        }

        [Test]
        public void SelectionState_Toggle()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.ToggleSelection(5);
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(5));

            PCGSelectionState.ToggleSelection(5);
            Assert.IsFalse(PCGSelectionState.SelectedPrimIndices.Contains(5));
        }

        [Test]
        public void SelectionState_Clear()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);
            PCGSelectionState.AddToSelection(1);
            PCGSelectionState.Clear();

            Assert.AreEqual(0, PCGSelectionState.SelectionCount);
        }

        [Test]
        public void SceneMeshBridge_TriangleToPrimMapping()
        {
            var geo = CreateQuadGeo();
            var bridge = new PCGSceneMeshBridge();
            bridge.Instantiate(geo);

            Assert.IsTrue(bridge.IsValid);
            // Each triangle prim maps 1:1 to a Unity triangle
            Assert.AreEqual(8, bridge.UnityTriToPcgPrim.Count);

            for (int i = 0; i < 8; i++)
            {
                Assert.IsTrue(bridge.UnityTriToPcgPrim.ContainsKey(i));
                Assert.AreEqual(i, bridge.UnityTriToPcgPrim[i]);
            }

            bridge.Dispose();
        }

        [Test]
        public void SceneMeshBridge_VertexMapping()
        {
            var geo = CreateQuadGeo();
            var bridge = new PCGSceneMeshBridge();
            bridge.Instantiate(geo);

            Assert.AreEqual(9, bridge.UnityVertToPcgPoint.Count);
            for (int i = 0; i < 9; i++)
                Assert.AreEqual(i, bridge.UnityVertToPcgPoint[i]);

            bridge.Dispose();
        }

        [Test]
        public void SceneMeshBridge_NGon_TriangleToPrimMapping()
        {
            var geo = new PCGGeometry();
            geo.Points.Add(new Vector3(0, 0, 0));
            geo.Points.Add(new Vector3(1, 0, 0));
            geo.Points.Add(new Vector3(1.5f, 0, 0.5f));
            geo.Points.Add(new Vector3(1, 0, 1));
            geo.Points.Add(new Vector3(0, 0, 1));
            // 1 pentagon = N-gon with 5 vertices -> 3 triangles
            geo.Primitives.Add(new int[] { 0, 1, 2, 3, 4 });

            var bridge = new PCGSceneMeshBridge();
            bridge.Instantiate(geo);

            Assert.IsTrue(bridge.IsValid);
            // Pentagon should produce 3 Unity triangles
            Assert.AreEqual(3, bridge.UnityTriToPcgPrim.Count);

            // All 3 Unity triangles should map to PCG Primitive 0
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(0, bridge.UnityTriToPcgPrim[i]);

            Assert.AreEqual(1, bridge.PcgPrimToUnityTris.Count);
            Assert.AreEqual(3, bridge.PcgPrimToUnityTris[0].Count);

            bridge.Dispose();
        }

        [Test]
        public void SceneSelectionInputNode_FaceMode_PrimGroupOutput()
        {
            var geo = CreateQuadGeo();
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);
            PCGSelectionState.AddToSelection(2);

            var node = new Nodes.SceneSelectionInputNode();
            var ctx = CreateContext();
            var result = node.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object> { { "groupName", "selected" } });

            var outGeo = result["geometry"];
            Assert.IsNotNull(outGeo);
            Assert.IsTrue(outGeo.PrimGroups.ContainsKey("selected"));
            Assert.AreEqual(2, outGeo.PrimGroups["selected"].Count);
            Assert.IsTrue(outGeo.PrimGroups["selected"].Contains(0));
            Assert.IsTrue(outGeo.PrimGroups["selected"].Contains(2));
        }

        [Test]
        public void SceneSelectionInputNode_VertexMode_PointGroupOutput()
        {
            var geo = CreateQuadGeo();
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.SetMode(PCGSelectMode.Vertex);
            PCGSelectionState.AddToSelection(0);
            PCGSelectionState.AddToSelection(4);

            var node = new Nodes.SceneSelectionInputNode();
            var ctx = CreateContext();
            var result = node.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object> { { "groupName", "selected" } });

            var outGeo = result["geometry"];
            Assert.IsNotNull(outGeo);
            Assert.IsTrue(outGeo.PointGroups.ContainsKey("selected"));
            Assert.IsTrue(outGeo.PointGroups["selected"].Contains(0));
            Assert.IsTrue(outGeo.PointGroups["selected"].Contains(4));
        }

        [Test]
        public void SceneSelectionInputNode_EdgeMode_PointGroupFromEdge()
        {
            var geo = CreateQuadGeo();
            geo.BuildEdges();
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.SetMode(PCGSelectMode.Edge);
            // Select the first edge
            if (geo.Edges.Count > 0)
                PCGSelectionState.AddToSelection(0);

            var node = new Nodes.SceneSelectionInputNode();
            var ctx = CreateContext();
            var result = node.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object> { { "groupName", "selected" } });

            var outGeo = result["geometry"];
            Assert.IsNotNull(outGeo);
            if (geo.Edges.Count > 0)
            {
                Assert.IsTrue(outGeo.PointGroups.ContainsKey("selected"));
                Assert.AreEqual(2, outGeo.PointGroups["selected"].Count);
            }
        }

        // ---- C4: Phase C Tests ----

        [Test]
        public void SelectionState_Serialization_RoundTrip()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(1);
            PCGSelectionState.AddToSelection(3);
            PCGSelectionState.AddToSelection(5);

            string json = PCGSelectionState.SerializeToJson();
            Assert.IsFalse(string.IsNullOrEmpty(json));

            PCGSelectionState.Clear();
            Assert.AreEqual(0, PCGSelectionState.SelectionCount);

            PCGSelectionState.RestoreFromJson(json);
            Assert.AreEqual(PCGSelectMode.Face, PCGSelectionState.CurrentMode);
            Assert.AreEqual(3, PCGSelectionState.SelectedPrimIndices.Count);
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(1));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(3));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(5));
        }

        [Test]
        public void SelectionState_ModeSwitch()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);
            Assert.AreEqual(1, PCGSelectionState.SelectionCount);

            PCGSelectionState.SetMode(PCGSelectMode.Vertex);
            Assert.AreEqual(0, PCGSelectionState.SelectionCount);
            PCGSelectionState.AddToSelection(4);
            Assert.AreEqual(1, PCGSelectionState.SelectionCount);

            // Face selection should still be there
            Assert.AreEqual(1, PCGSelectionState.SelectedPrimIndices.Count);
        }

        [Test]
        public void SceneMeshBridge_GetPrimCenter()
        {
            var geo = CreateQuadGeo();
            var bridge = new PCGSceneMeshBridge();
            bridge.Instantiate(geo);

            // Prim 0: vertices 0,1,4 -> (0,0,0), (1,0,0), (1,0,1) -> center = (2/3, 0, 1/3)
            Vector3 center = bridge.GetPrimCenter(0);
            Assert.AreEqual(0, center.y, 0.01f);
            Assert.Greater(center.x, 0);

            bridge.Dispose();
        }

        [Test]
        public void SceneMeshBridge_Dispose_CleansUp()
        {
            var geo = CreateQuadGeo();
            var bridge = new PCGSceneMeshBridge();
            bridge.Instantiate(geo);
            Assert.IsTrue(bridge.IsValid);

            bridge.Dispose();
            Assert.IsFalse(bridge.IsValid);
            Assert.AreEqual(0, bridge.UnityTriToPcgPrim.Count);
        }
    }
}
