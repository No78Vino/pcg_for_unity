using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Tools;
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

        // ---- Phase B7 + C4: Grow/Shrink, SelectByNormal/Material, Integration Tests ----

        private PCGSelectionTool CreateToolWithGeo(PCGGeometry geo)
        {
            var tool = ScriptableObject.CreateInstance<PCGSelectionTool>();
            tool.SetGeometry(geo);
            return tool;
        }

        private PCGGeometry Create3x3GridGeo()
        {
            var geo = new PCGGeometry();
            // 4x4 = 16 points forming a 3x3 grid (9 quads = 18 triangle prims)
            for (int z = 0; z < 4; z++)
                for (int x = 0; x < 4; x++)
                    geo.Points.Add(new Vector3(x, 0, z));

            for (int z = 0; z < 3; z++)
            {
                for (int x = 0; x < 3; x++)
                {
                    int bl = z * 4 + x;
                    int br = bl + 1;
                    int tl = bl + 4;
                    int tr = tl + 1;
                    geo.Primitives.Add(new int[] { bl, br, tr });
                    geo.Primitives.Add(new int[] { bl, tr, tl });
                }
            }
            return geo;
        }

        [Test]
        public void GrowSelection_Face_ExpandsToAdjacentFaces()
        {
            var geo = CreateQuadGeo();
            var tool = CreateToolWithGeo(geo);

            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);

            tool.GrowSelection();

            // Prim 0 has verts {0,1,4}. Adjacent prims sharing these verts: 1(0,4,3), 2(1,2,5), 3(1,5,4), 4(3,4,7), 6(4,5,8), 7(4,8,7)
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(0));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Count > 1);
            // Prim 1 shares verts 0,4 with prim 0
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(1));
            // Prim 3 shares vert 1,4 with prim 0
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(3));

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void ShrinkSelection_Face_RemovesBoundaryFaces()
        {
            // 3x3 grid: 18 tri prims. Select the center 3x3 block (all 18).
            // When all faces are selected, no face has an unselected neighbor, so shrink keeps all.
            // Instead, select inner 6 prims (center quad + 2 adjacent quads) to test meaningful shrink.
            var geo = CreateQuadGeo(); // 2x2 grid, 8 prims
            var tool = CreateToolWithGeo(geo);

            PCGSelectionState.SetMode(PCGSelectMode.Face);
            // Select all 8 prims in the 2x2 grid
            for (int i = 0; i < 8; i++)
                PCGSelectionState.SelectedPrimIndices.Add(i);

            tool.ShrinkSelection();

            // In a 2x2 grid (4 quads = 8 tri prims), every triangle shares at least one vertex
            // with the mesh boundary. Since all prims are selected and there are no unselected
            // adjacent prims, shrink should NOT remove any faces.
            // (Boundary detection only considers selected vs unselected prims, not mesh edges.)
            Assert.AreEqual(8, PCGSelectionState.SelectedPrimIndices.Count);

            // Now deselect the outer ring: remove prims 0,1,2,5 (top-left and bottom-left quads)
            PCGSelectionState.SelectedPrimIndices.Clear();
            // Select only center-adjacent prims 3(1,5,4), 4(3,4,7), 6(4,5,8), 7(4,8,7)
            PCGSelectionState.SelectedPrimIndices.Add(3);
            PCGSelectionState.SelectedPrimIndices.Add(4);
            PCGSelectionState.SelectedPrimIndices.Add(6);
            PCGSelectionState.SelectedPrimIndices.Add(7);

            tool.ShrinkSelection();

            // Prims 3,4,6,7 share vertices with unselected prims 0,1,2,5.
            // All 4 are boundary prims, so all should be removed.
            Assert.AreEqual(0, PCGSelectionState.SelectedPrimIndices.Count);

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void GrowSelection_Vertex_ExpandsToAdjacentVertices()
        {
            var geo = CreateQuadGeo();
            var tool = CreateToolWithGeo(geo);

            PCGSelectionState.SetMode(PCGSelectMode.Vertex);
            // Select center vertex 4
            PCGSelectionState.AddToSelection(4);

            tool.GrowSelection();

            // Vertex 4 is shared by prims 0(0,1,4), 1(0,4,3), 3(1,5,4), 4(3,4,7), 6(4,5,8), 7(4,8,7)
            // All vertices of those prims should be added: 0,1,3,4,5,6,7,8
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(4));
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(0));
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(1));
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(3));
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(5));
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(7));
            Assert.IsTrue(PCGSelectionState.SelectedPointIndices.Contains(8));

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void GrowSelection_Edge_ExpandsToAdjacentEdges()
        {
            var geo = CreateQuadGeo();
            geo.BuildEdges();
            var tool = CreateToolWithGeo(geo);

            PCGSelectionState.SetMode(PCGSelectMode.Edge);
            // Select the first edge
            Assert.Greater(geo.Edges.Count, 0);
            PCGSelectionState.AddToSelection(0);

            int endpoint0 = tool.Bridge.Geometry.Edges[0][0];
            int endpoint1 = tool.Bridge.Geometry.Edges[0][1];

            tool.GrowSelection();

            // After grow, all edges sharing endpoints with edge 0 should be selected
            Assert.IsTrue(PCGSelectionState.SelectedEdgeIndices.Count > 1);
            foreach (int edgeIdx in PCGSelectionState.SelectedEdgeIndices)
            {
                var edge = tool.Bridge.Geometry.Edges[edgeIdx];
                bool sharesEndpoint = edge[0] == endpoint0 || edge[0] == endpoint1 ||
                                       edge[1] == endpoint0 || edge[1] == endpoint1;
                Assert.IsTrue(sharesEndpoint || edgeIdx == 0);
            }

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void ShrinkSelection_Vertex_RemovesBoundaryVertices()
        {
            var geo = CreateQuadGeo(); // 9 vertices in a 3x3 grid
            var tool = CreateToolWithGeo(geo);

            PCGSelectionState.SetMode(PCGSelectMode.Vertex);
            // Select all 9 vertices
            for (int i = 0; i < 9; i++)
                PCGSelectionState.SelectedPointIndices.Add(i);

            tool.ShrinkSelection();

            // Vertex 4 (center) is shared by 6 prims, all its prim-neighbors (0,1,3,5,6,7,8) are selected.
            // Edge vertices (0,1,2,3,5,6,7,8) share prims with vertices outside the selected set? No - all are selected.
            // Since all vertices are selected, no vertex has an unselected neighbor via prims => none removed.
            Assert.AreEqual(9, PCGSelectionState.SelectedPointIndices.Count);

            // Now select only center + 4 direct cardinal neighbors: 1,3,4,5,7
            PCGSelectionState.SelectedPointIndices.Clear();
            PCGSelectionState.SelectedPointIndices.Add(1);
            PCGSelectionState.SelectedPointIndices.Add(3);
            PCGSelectionState.SelectedPointIndices.Add(4);
            PCGSelectionState.SelectedPointIndices.Add(5);
            PCGSelectionState.SelectedPointIndices.Add(7);

            tool.ShrinkSelection();

            // Vertices 1,3,5,7 share primitives with corner vertices 0,2,6,8 which are NOT selected.
            // So 1,3,5,7 are boundary and should be removed. Vertex 4 shares prims with 0,1,3,5,6,7,8
            // - vertices 0,6,8 are not selected, so 4 is also boundary.
            // All 5 vertices are boundary => all removed.
            Assert.AreEqual(0, PCGSelectionState.SelectedPointIndices.Count);

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void ShrinkSelection_Edge_RemovesBoundaryEdges()
        {
            var geo = CreateQuadGeo();
            geo.BuildEdges();
            var tool = CreateToolWithGeo(geo);

            PCGSelectionState.SetMode(PCGSelectMode.Edge);
            int totalEdges = tool.Bridge.Geometry.Edges.Count;
            Assert.Greater(totalEdges, 0);

            // Select all edges
            for (int i = 0; i < totalEdges; i++)
                PCGSelectionState.SelectedEdgeIndices.Add(i);

            tool.ShrinkSelection();

            // All edges selected => no edge has an unselected neighbor => none removed
            Assert.AreEqual(totalEdges, PCGSelectionState.SelectedEdgeIndices.Count);

            // Now select only a subset: edges that connect to vertex 4 (center)
            PCGSelectionState.SelectedEdgeIndices.Clear();
            for (int i = 0; i < totalEdges; i++)
            {
                var edge = tool.Bridge.Geometry.Edges[i];
                if (edge[0] == 4 || edge[1] == 4)
                    PCGSelectionState.SelectedEdgeIndices.Add(i);
            }
            int centerEdgeCount = PCGSelectionState.SelectedEdgeIndices.Count;
            Assert.Greater(centerEdgeCount, 0);

            tool.ShrinkSelection();

            // Each center edge connects vertex 4 to a boundary vertex.
            // That boundary vertex also connects to other edges NOT in the selection.
            // So all center edges are boundary => all removed.
            Assert.AreEqual(0, PCGSelectionState.SelectedEdgeIndices.Count);

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void SelectByNormal_SelectsUpwardFaces()
        {
            var geo = new PCGGeometry();
            // Horizontal face (normal up)
            geo.Points.Add(new Vector3(0, 0, 0));
            geo.Points.Add(new Vector3(1, 0, 0));
            geo.Points.Add(new Vector3(0, 0, 1));
            geo.Primitives.Add(new int[] { 0, 1, 2 });

            // Vertical face (normal sideways)
            geo.Points.Add(new Vector3(0, 0, 0));
            geo.Points.Add(new Vector3(1, 0, 0));
            geo.Points.Add(new Vector3(0, 1, 0));
            geo.Primitives.Add(new int[] { 3, 4, 5 });

            var tool = CreateToolWithGeo(geo);
            PCGSelectionState.SetMode(PCGSelectMode.Face);

            tool.SelectByNormal(Vector3.up, 0.7f);

            // Only the horizontal face (prim 0) should be selected
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(0));
            Assert.IsFalse(PCGSelectionState.SelectedPrimIndices.Contains(1));

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void SelectByMaterialId_SelectsSameMaterial()
        {
            var geo = CreateQuadGeo();
            var matAttr = geo.PrimAttribs.CreateAttribute("material", AttribType.String);
            // Assign materials: prims 0,1,2,3 = "matA", prims 4,5,6,7 = "matB"
            for (int i = 0; i < 8; i++)
                matAttr.Values.Add(i < 4 ? "matA" : "matB");

            var tool = CreateToolWithGeo(geo);
            PCGSelectionState.SetMode(PCGSelectMode.Face);

            tool.SelectByMaterialId(0); // prim 0 has "matA"

            Assert.AreEqual(4, PCGSelectionState.SelectedPrimIndices.Count);
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(0));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(1));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(2));
            Assert.IsTrue(PCGSelectionState.SelectedPrimIndices.Contains(3));
            Assert.IsFalse(PCGSelectionState.SelectedPrimIndices.Contains(4));

            tool.Bridge.Dispose();
            Object.DestroyImmediate(tool);
        }

        [Test]
        public void SceneSelectionInputNode_ConnectBlastNode_DeletesSelectedFaces()
        {
            var geo = CreateQuadGeo();
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(0);
            PCGSelectionState.AddToSelection(1);

            // Execute SceneSelectionInputNode
            var selNode = new Nodes.SceneSelectionInputNode();
            var ctx = CreateContext();
            var selResult = selNode.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object> { { "groupName", "selected" } });
            var selGeo = selResult["geometry"];
            Assert.IsTrue(selGeo.PrimGroups.ContainsKey("selected"));
            Assert.AreEqual(2, selGeo.PrimGroups["selected"].Count);

            // Feed into BlastNode to delete the selected faces
            var blastNode = new Nodes.Geometry.BlastNode();
            var blastResult = blastNode.Execute(ctx,
                new Dictionary<string, PCGGeometry> { { "input", selGeo } },
                new Dictionary<string, object>
                {
                    { "group", "selected" },
                    { "groupType", "primitive" },
                    { "deleteNonSelected", false }
                });
            var blastGeo = blastResult["geometry"];

            // Original had 8 prims, 2 were selected and deleted
            Assert.AreEqual(6, blastGeo.Primitives.Count);
        }

        [Test]
        public void SceneSelectionInputNode_Persistence_RestoresAfterClear()
        {
            var geo = CreateQuadGeo();
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            PCGSelectionState.AddToSelection(2);
            PCGSelectionState.AddToSelection(5);

            // Execute to serialize selection
            var node = new Nodes.SceneSelectionInputNode();
            var ctx = CreateContext();
            ctx.CurrentNodeId = "testNode1";
            node.Execute(ctx,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object> { { "groupName", "selected" } });

            // Get serialized selection from context
            string serialized = ctx.GlobalVariables["testNode1.serializedSelection"] as string;
            Assert.IsFalse(string.IsNullOrEmpty(serialized));

            // Clear all selection
            PCGSelectionState.Clear();
            Assert.AreEqual(0, PCGSelectionState.SelectionCount);

            // Re-execute with serialized data, it should restore selection
            var ctx2 = CreateContext();
            var result = node.Execute(ctx2,
                new Dictionary<string, PCGGeometry>(),
                new Dictionary<string, object>
                {
                    { "groupName", "selected" },
                    { "serializedSelection", serialized }
                });

            var outGeo = result["geometry"];
            Assert.IsTrue(outGeo.PrimGroups.ContainsKey("selected"));
            Assert.AreEqual(2, outGeo.PrimGroups["selected"].Count);
            Assert.IsTrue(outGeo.PrimGroups["selected"].Contains(2));
            Assert.IsTrue(outGeo.PrimGroups["selected"].Contains(5));
        }
    }
}
