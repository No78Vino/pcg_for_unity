using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using PCGToolkit.Core;
using PCGToolkit.Graph;

namespace PCGToolkit.Tools
{
    public static class PCGQuickSelect
    {
        [MenuItem("GameObject/PCG Toolkit/Select Faces", false, 49)]
        private static void SelectFacesFromHierarchy()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Face);
            Launch(Selection.activeGameObject);
        }

        [MenuItem("GameObject/PCG Toolkit/Select Faces", true)]
        private static bool ValidateSelectFaces()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<MeshFilter>() != null;
        }

        [MenuItem("GameObject/PCG Toolkit/Select Edges", false, 50)]
        private static void SelectEdgesFromHierarchy()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Edge);
            Launch(Selection.activeGameObject);
        }

        [MenuItem("GameObject/PCG Toolkit/Select Edges", true)]
        private static bool ValidateSelectEdges()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<MeshFilter>() != null;
        }

        [MenuItem("GameObject/PCG Toolkit/Select Vertices", false, 51)]
        private static void SelectVerticesFromHierarchy()
        {
            PCGSelectionState.SetMode(PCGSelectMode.Vertex);
            Launch(Selection.activeGameObject);
        }

        [MenuItem("GameObject/PCG Toolkit/Select Vertices", true)]
        private static bool ValidateSelectVertices()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<MeshFilter>() != null;
        }

        public static void Launch(GameObject go)
        {
            if (go == null) return;

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"[PCGQuickSelect] GameObject '{go.name}' has no MeshFilter or mesh is null.");
                return;
            }

            var geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh);
            ApplyWorldTransform(geo, go.transform);

            EnsureGraphNode(go);

            ToolManager.SetActiveTool<PCGSelectionTool>();
            EditorApplication.delayCall += () =>
            {
                PCGSelectionTool.ActiveInstance?.SetGeometry(geo);
                Debug.Log($"[PCGQuickSelect] Geometry loaded from '{go.name}': {geo.Points.Count} points, {geo.Primitives.Count} prims");
            };
        }

        public static void ApplyWorldTransform(PCGGeometry geo, Transform transform)
        {
            var matrix = transform.localToWorldMatrix;
            for (int i = 0; i < geo.Points.Count; i++)
                geo.Points[i] = matrix.MultiplyPoint3x4(geo.Points[i]);

            var normalAttr = geo.PointAttribs.GetAttribute("N");
            if (normalAttr != null)
            {
                var normalMat = matrix.inverse.transpose;
                for (int i = 0; i < normalAttr.Values.Count; i++)
                {
                    if (normalAttr.Values[i] is Vector3 n)
                        normalAttr.Values[i] = normalMat.MultiplyVector(n).normalized;
                }
            }
        }

        private static void EnsureGraphNode(GameObject go)
        {
            var editorWindow = EditorWindow.GetWindow<PCGGraphEditorWindow>(false, null, false);
            if (editorWindow == null) return;

            var graphView = editorWindow.GetGraphView();
            if (graphView == null) return;

            var existingNode = graphView.FindNodeVisualByType("SceneSelectionInput");
            var sceneRef = new PCGSceneObjectRef(go);

            if (existingNode != null)
            {
                existingNode.SetPortDefaultValues(new Dictionary<string, object> { { "target", sceneRef } });
                graphView.ClearSelection();
                graphView.AddToSelection(existingNode);
            }
            else
            {
                var template = PCGNodeRegistry.GetNode("SceneSelectionInput");
                if (template == null) return;

                var newNode = (IPCGNode)Activator.CreateInstance(template.GetType());
                var visual = graphView.CreateNodeVisual(newNode, new Vector2(100, 100));
                visual.SetPortDefaultValues(new Dictionary<string, object> { { "target", sceneRef } });
                graphView.ClearSelection();
                graphView.AddToSelection(visual);
                graphView.FrameSelection();
                graphView.NotifyGraphChanged();
            }
        }
    }
}
