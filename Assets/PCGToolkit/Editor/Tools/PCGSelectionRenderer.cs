using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Tools
{
    [InitializeOnLoad]
    public static class PCGSelectionRenderer
    {
        private static Material _faceMaterial;
        private static readonly Color FaceHighlightColor = new Color(0.2f, 0.5f, 1.0f, 0.3f);
        private static readonly Color EdgeHighlightColor = new Color(0.2f, 0.7f, 1.0f, 0.9f);
        private static readonly Color PointHighlightColor = new Color(0.2f, 0.7f, 1.0f, 0.9f);
        private static readonly Color HoverFaceColor = new Color(1.0f, 0.9f, 0.2f, 0.2f);
        private static readonly Color HoverEdgeColor = new Color(1.0f, 0.9f, 0.2f, 0.9f);
        private static readonly Color HoverPointColor = new Color(1.0f, 0.9f, 0.2f, 0.9f);
        private static readonly float PointSize = 0.04f;
        private static readonly float EdgeWidth = 3f;

        static PCGSelectionRenderer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (PCGSelectionState.SourceGeometry == null) return;
            var geo = PCGSelectionState.SourceGeometry;

            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                    DrawSelectedFaces(geo);
                    DrawHoverFace(geo);
                    break;
                case PCGSelectMode.Edge:
                    DrawSelectedEdges(geo);
                    DrawHoverEdge(geo);
                    break;
                case PCGSelectMode.Vertex:
                    DrawSelectedVertices(geo);
                    DrawHoverVertex(geo);
                    break;
            }
        }

        private static void DrawSelectedFaces(PCGGeometry geo)
        {
            if (PCGSelectionState.SelectedPrimIndices.Count == 0) return;

            EnsureFaceMaterial();
            _faceMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.TRIANGLES);
            GL.Color(FaceHighlightColor);

            foreach (int primIdx in PCGSelectionState.SelectedPrimIndices)
            {
                if (primIdx < 0 || primIdx >= geo.Primitives.Count) continue;
                var prim = geo.Primitives[primIdx];

                if (prim.Length == 3)
                {
                    GL.Vertex(geo.Points[prim[0]]);
                    GL.Vertex(geo.Points[prim[1]]);
                    GL.Vertex(geo.Points[prim[2]]);
                }
                else if (prim.Length == 4)
                {
                    GL.Vertex(geo.Points[prim[0]]);
                    GL.Vertex(geo.Points[prim[1]]);
                    GL.Vertex(geo.Points[prim[2]]);
                    GL.Vertex(geo.Points[prim[0]]);
                    GL.Vertex(geo.Points[prim[2]]);
                    GL.Vertex(geo.Points[prim[3]]);
                }
                else if (prim.Length > 4)
                {
                    for (int i = 1; i < prim.Length - 1; i++)
                    {
                        GL.Vertex(geo.Points[prim[0]]);
                        GL.Vertex(geo.Points[prim[i]]);
                        GL.Vertex(geo.Points[prim[i + 1]]);
                    }
                }
            }

            GL.End();
            GL.PopMatrix();

            // Draw wireframe outline of selected faces
            Handles.color = EdgeHighlightColor;
            foreach (int primIdx in PCGSelectionState.SelectedPrimIndices)
            {
                if (primIdx < 0 || primIdx >= geo.Primitives.Count) continue;
                var prim = geo.Primitives[primIdx];

                for (int i = 0; i < prim.Length; i++)
                {
                    int a = prim[i];
                    int b = prim[(i + 1) % prim.Length];
                    if (a < geo.Points.Count && b < geo.Points.Count)
                        Handles.DrawAAPolyLine(EdgeWidth, geo.Points[a], geo.Points[b]);
                }
            }
        }

        private static void DrawSelectedEdges(PCGGeometry geo)
        {
            if (PCGSelectionState.SelectedEdgeIndices.Count == 0 || geo.Edges.Count == 0) return;

            Handles.color = EdgeHighlightColor;
            foreach (int edgeIdx in PCGSelectionState.SelectedEdgeIndices)
            {
                if (edgeIdx < 0 || edgeIdx >= geo.Edges.Count) continue;
                var edge = geo.Edges[edgeIdx];
                if (edge[0] < geo.Points.Count && edge[1] < geo.Points.Count)
                    Handles.DrawAAPolyLine(EdgeWidth, geo.Points[edge[0]], geo.Points[edge[1]]);
            }
        }

        private static void DrawSelectedVertices(PCGGeometry geo)
        {
            if (PCGSelectionState.SelectedPointIndices.Count == 0) return;

            Handles.color = PointHighlightColor;
            foreach (int ptIdx in PCGSelectionState.SelectedPointIndices)
            {
                if (ptIdx < 0 || ptIdx >= geo.Points.Count) continue;
                Handles.DotHandleCap(0, geo.Points[ptIdx], Quaternion.identity, PointSize, EventType.Repaint);
            }
        }

        private static void DrawHoverFace(PCGGeometry geo)
        {
            int idx = PCGSelectionState.HoveredIndex;
            if (idx < 0 || idx >= geo.Primitives.Count) return;
            if (PCGSelectionState.SelectedPrimIndices.Contains(idx)) return;

            EnsureFaceMaterial();
            _faceMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.TRIANGLES);
            GL.Color(HoverFaceColor);

            var prim = geo.Primitives[idx];
            for (int i = 1; i < prim.Length - 1; i++)
            {
                GL.Vertex(geo.Points[prim[0]]);
                GL.Vertex(geo.Points[prim[i]]);
                GL.Vertex(geo.Points[prim[i + 1]]);
            }

            GL.End();
            GL.PopMatrix();
        }

        private static void DrawHoverEdge(PCGGeometry geo)
        {
            int idx = PCGSelectionState.HoveredIndex;
            if (idx < 0 || idx >= geo.Edges.Count) return;
            if (PCGSelectionState.SelectedEdgeIndices.Contains(idx)) return;

            var edge = geo.Edges[idx];
            Handles.color = HoverEdgeColor;
            Handles.DrawAAPolyLine(EdgeWidth, geo.Points[edge[0]], geo.Points[edge[1]]);
        }

        private static void DrawHoverVertex(PCGGeometry geo)
        {
            int idx = PCGSelectionState.HoveredIndex;
            if (idx < 0 || idx >= geo.Points.Count) return;
            if (PCGSelectionState.SelectedPointIndices.Contains(idx)) return;

            Handles.color = HoverPointColor;
            Handles.DotHandleCap(0, geo.Points[idx], Quaternion.identity, PointSize * 1.5f, EventType.Repaint);
        }

        private static void EnsureFaceMaterial()
        {
            if (_faceMaterial != null) return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _faceMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _faceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _faceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _faceMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _faceMaterial.SetInt("_ZWrite", 0);
            _faceMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        }
    }
}
