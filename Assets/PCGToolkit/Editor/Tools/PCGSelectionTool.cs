using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Tools
{
    [EditorTool("PCG Selection Tool")]
    public class PCGSelectionTool : EditorTool
    {
        private PCGSceneMeshBridge _bridge = new PCGSceneMeshBridge();
        private bool _isDragging;
        private Vector2 _dragStart;
        private static readonly float DragThreshold = 5f;

        private Dictionary<int, HashSet<int>> _vertexToPrims = new Dictionary<int, HashSet<int>>();
        private Dictionary<int, HashSet<int>> _vertexToEdges = new Dictionary<int, HashSet<int>>();

        public PCGSceneMeshBridge Bridge => _bridge;

        public static PCGSelectionTool ActiveInstance { get; private set; }

        public override GUIContent toolbarIcon =>
            new GUIContent(EditorGUIUtility.IconContent("EditCollider").image, "PCG Selection Tool");

        public void SetGeometry(PCGGeometry geo)
        {
            _bridge.Instantiate(geo);
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.Clear();
            RebuildAdjacency();
        }

        private void RebuildAdjacency()
        {
            _vertexToPrims.Clear();
            _vertexToEdges.Clear();
            if (_bridge.Geometry == null) return;

            for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
            {
                foreach (int v in _bridge.Geometry.Primitives[i])
                {
                    if (!_vertexToPrims.TryGetValue(v, out var set))
                    {
                        set = new HashSet<int>();
                        _vertexToPrims[v] = set;
                    }
                    set.Add(i);
                }
            }

            for (int i = 0; i < _bridge.Geometry.Edges.Count; i++)
            {
                var edge = _bridge.Geometry.Edges[i];
                for (int j = 0; j < 2; j++)
                {
                    int v = edge[j];
                    if (!_vertexToEdges.TryGetValue(v, out var set))
                    {
                        set = new HashSet<int>();
                        _vertexToEdges[v] = set;
                    }
                    set.Add(i);
                }
            }
        }

        public override void OnActivated()
        {
            ActiveInstance = this;

            if (!_bridge.IsValid)
            {
                var go = Selection.activeGameObject;
                if (go != null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh);
                        PCGQuickSelect.ApplyWorldTransform(geo, go.transform);
                        SetGeometry(geo);
                        Debug.Log($"[PCGSelectionTool] Auto-loaded geometry from '{go.name}': {geo.Points.Count} points, {geo.Primitives.Count} prims");
                    }
                }
            }

            SceneView.RepaintAll();
        }

        public override void OnWillBeDeactivated()
        {
            if (ActiveInstance == this) ActiveInstance = null;
            _bridge.Dispose();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView sceneView)) return;
            if (!_bridge.IsValid) return;

            var evt = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0 && !evt.alt)
                    {
                        _isDragging = false;
                        _dragStart = evt.mousePosition;
                        GUIUtility.hotControl = controlId;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && evt.button == 0)
                    {
                        if (!_isDragging && Vector2.Distance(_dragStart, evt.mousePosition) > DragThreshold)
                            _isDragging = true;

                        if (_isDragging)
                        {
                            // Rect selection handled in Phase B
                            HandleRectSelect(evt, sceneView);
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;

                        if (!_isDragging)
                        {
                            HandleClick(evt, sceneView);
                        }
                        else
                        {
                            FinishRectSelect(evt, sceneView);
                            _isDragging = false;
                        }

                        evt.Use();
                    }
                    break;

                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;

                case EventType.KeyDown:
                    if (evt.control && evt.keyCode == KeyCode.KeypadPlus)
                    {
                        GrowSelection();
                        evt.Use();
                    }
                    else if (evt.control && evt.keyCode == KeyCode.KeypadMinus)
                    {
                        ShrinkSelection();
                        evt.Use();
                    }
                    break;

                case EventType.MouseMove:
                    UpdateHover(evt);
                    break;
            }

            // Draw rect selection outline
            if (_isDragging)
            {
                Handles.BeginGUI();
                var rect = GetDragRect(evt.mousePosition);
                GUI.Box(rect, GUIContent.none, "SelectionRect");
                Handles.EndGUI();
            }
        }

        private void HandleClick(Event evt, SceneView sceneView)
        {
            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                    HandleFaceClick(evt);
                    break;
                case PCGSelectMode.Vertex:
                    HandleVertexClick(evt);
                    break;
                case PCGSelectMode.Edge:
                    HandleEdgeClick(evt);
                    break;
            }
        }

        private void UpdateHover(Event evt)
        {
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (!Physics.Raycast(ray, out var hit) ||
                hit.collider == null || hit.collider.gameObject != _bridge.TempGameObject)
            {
                if (PCGSelectionState.HoveredIndex != -1)
                {
                    PCGSelectionState.HoveredIndex = -1;
                    SceneView.RepaintAll();
                }
                return;
            }

            int newHover = -1;
            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                    _bridge.UnityTriToPcgPrim.TryGetValue(hit.triangleIndex, out newHover);
                    break;
                case PCGSelectMode.Vertex:
                    newHover = _bridge.FindClosestVertex(hit.triangleIndex, hit.point);
                    break;
                case PCGSelectMode.Edge:
                    newHover = _bridge.FindClosestEdge(hit.triangleIndex, hit.point);
                    break;
            }

            if (newHover != PCGSelectionState.HoveredIndex)
            {
                PCGSelectionState.HoveredIndex = newHover;
                SceneView.RepaintAll();
            }
        }

        private void HandleFaceClick(Event evt)
        {
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) return;

            if (hit.collider == null || hit.collider.gameObject != _bridge.TempGameObject) return;

            if (!_bridge.UnityTriToPcgPrim.TryGetValue(hit.triangleIndex, out int primIdx)) return;

            if (evt.shift)
                PCGSelectionState.AddToSelection(primIdx);
            else if (evt.control)
                PCGSelectionState.RemoveFromSelection(primIdx);
            else
            {
                PCGSelectionState.SelectedPrimIndices.Clear();
                PCGSelectionState.AddToSelection(primIdx);
            }

            SceneView.RepaintAll();
        }

        private void HandleVertexClick(Event evt)
        {
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) return;
            if (hit.collider == null || hit.collider.gameObject != _bridge.TempGameObject) return;

            int pcgPointIdx = _bridge.FindClosestVertex(hit.triangleIndex, hit.point);
            if (pcgPointIdx < 0) return;

            if (evt.shift)
                PCGSelectionState.AddToSelection(pcgPointIdx);
            else if (evt.control)
                PCGSelectionState.RemoveFromSelection(pcgPointIdx);
            else
            {
                PCGSelectionState.SelectedPointIndices.Clear();
                PCGSelectionState.AddToSelection(pcgPointIdx);
            }

            SceneView.RepaintAll();
        }

        private void HandleEdgeClick(Event evt)
        {
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) return;
            if (hit.collider == null || hit.collider.gameObject != _bridge.TempGameObject) return;

            int edgeIdx = _bridge.FindClosestEdge(hit.triangleIndex, hit.point);
            if (edgeIdx < 0) return;

            if (evt.shift)
                PCGSelectionState.AddToSelection(edgeIdx);
            else if (evt.control)
                PCGSelectionState.RemoveFromSelection(edgeIdx);
            else
            {
                PCGSelectionState.SelectedEdgeIndices.Clear();
                PCGSelectionState.AddToSelection(edgeIdx);
            }

            SceneView.RepaintAll();
        }

        private void HandleRectSelect(Event evt, SceneView sceneView)
        {
            SceneView.RepaintAll();
        }

        private void FinishRectSelect(Event evt, SceneView sceneView)
        {
            var rect = GetDragRect(evt.mousePosition);
            if (rect.width < DragThreshold || rect.height < DragThreshold) return;

            var cam = sceneView.camera;
            if (cam == null) return;

            bool additive = evt.shift;
            bool subtractive = evt.control;

            if (!additive && !subtractive)
            {
                switch (PCGSelectionState.CurrentMode)
                {
                    case PCGSelectMode.Face: PCGSelectionState.SelectedPrimIndices.Clear(); break;
                    case PCGSelectMode.Vertex: PCGSelectionState.SelectedPointIndices.Clear(); break;
                    case PCGSelectMode.Edge: PCGSelectionState.SelectedEdgeIndices.Clear(); break;
                }
            }

            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                    RectSelectFaces(rect, cam, subtractive);
                    break;
                case PCGSelectMode.Vertex:
                    RectSelectVertices(rect, cam, subtractive);
                    break;
                case PCGSelectMode.Edge:
                    RectSelectEdges(rect, cam, subtractive);
                    break;
            }

            PCGSelectionState.NotifyChanged();
            SceneView.RepaintAll();
        }

        private void RectSelectFaces(Rect rect, Camera cam, bool subtractive)
        {
            if (_bridge.Geometry == null) return;

            for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
            {
                Vector3 center = _bridge.GetPrimCenter(i);
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(center);
                if (cam.WorldToScreenPoint(center).z < 0) continue;

                if (rect.Contains(guiPos))
                {
                    if (subtractive)
                        PCGSelectionState.SelectedPrimIndices.Remove(i);
                    else
                        PCGSelectionState.SelectedPrimIndices.Add(i);
                }
            }
        }

        private void RectSelectVertices(Rect rect, Camera cam, bool subtractive)
        {
            if (_bridge.Geometry == null) return;

            for (int i = 0; i < _bridge.Geometry.Points.Count; i++)
            {
                Vector3 worldPos = _bridge.Geometry.Points[i];
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);
                if (cam.WorldToScreenPoint(worldPos).z < 0) continue;

                if (rect.Contains(guiPos))
                {
                    if (subtractive)
                        PCGSelectionState.SelectedPointIndices.Remove(i);
                    else
                        PCGSelectionState.SelectedPointIndices.Add(i);
                }
            }
        }

        private void RectSelectEdges(Rect rect, Camera cam, bool subtractive)
        {
            if (_bridge.Geometry == null) return;

            for (int i = 0; i < _bridge.Geometry.Edges.Count; i++)
            {
                var edge = _bridge.Geometry.Edges[i];
                Vector3 midpoint = (_bridge.Geometry.Points[edge[0]] + _bridge.Geometry.Points[edge[1]]) * 0.5f;
                Vector2 guiPos = HandleUtility.WorldToGUIPoint(midpoint);
                if (cam.WorldToScreenPoint(midpoint).z < 0) continue;

                if (rect.Contains(guiPos))
                {
                    if (subtractive)
                        PCGSelectionState.SelectedEdgeIndices.Remove(i);
                    else
                        PCGSelectionState.SelectedEdgeIndices.Add(i);
                }
            }
        }

        private Rect GetDragRect(Vector2 currentPos)
        {
            float x = Mathf.Min(_dragStart.x, currentPos.x);
            float y = Mathf.Min(_dragStart.y, currentPos.y);
            float w = Mathf.Abs(currentPos.x - _dragStart.x);
            float h = Mathf.Abs(currentPos.y - _dragStart.y);
            return new Rect(x, y, w, h);
        }

        // ---- Grow / Shrink selection (Phase C) ----

        public void GrowSelection()
        {
            if (_bridge.Geometry == null) return;

            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                {
                    var toAdd = new HashSet<int>();
                    foreach (int selectedPrim in PCGSelectionState.SelectedPrimIndices)
                    {
                        foreach (int v in _bridge.Geometry.Primitives[selectedPrim])
                        {
                            if (_vertexToPrims.TryGetValue(v, out var adjPrims))
                            {
                                foreach (int adj in adjPrims)
                                {
                                    if (!PCGSelectionState.SelectedPrimIndices.Contains(adj))
                                        toAdd.Add(adj);
                                }
                            }
                        }
                    }
                    foreach (int idx in toAdd)
                        PCGSelectionState.SelectedPrimIndices.Add(idx);
                    break;
                }

                case PCGSelectMode.Vertex:
                {
                    var toAdd = new HashSet<int>();
                    foreach (int selectedVert in PCGSelectionState.SelectedPointIndices)
                    {
                        if (_vertexToPrims.TryGetValue(selectedVert, out var adjPrims))
                        {
                            foreach (int primIdx in adjPrims)
                            {
                                foreach (int v in _bridge.Geometry.Primitives[primIdx])
                                    toAdd.Add(v);
                            }
                        }
                    }
                    foreach (int idx in toAdd)
                        PCGSelectionState.SelectedPointIndices.Add(idx);
                    break;
                }

                case PCGSelectMode.Edge:
                {
                    var toAdd = new HashSet<int>();
                    foreach (int edgeIdx in PCGSelectionState.SelectedEdgeIndices)
                    {
                        if (edgeIdx < 0 || edgeIdx >= _bridge.Geometry.Edges.Count) continue;
                        var edge = _bridge.Geometry.Edges[edgeIdx];
                        for (int j = 0; j < 2; j++)
                        {
                            if (_vertexToEdges.TryGetValue(edge[j], out var adjEdges))
                            {
                                foreach (int adj in adjEdges)
                                {
                                    if (!PCGSelectionState.SelectedEdgeIndices.Contains(adj))
                                        toAdd.Add(adj);
                                }
                            }
                        }
                    }
                    foreach (int idx in toAdd)
                        PCGSelectionState.SelectedEdgeIndices.Add(idx);
                    break;
                }
            }

            SceneView.RepaintAll();
        }

        public void ShrinkSelection()
        {
            if (_bridge.Geometry == null) return;

            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                {
                    var toRemove = new HashSet<int>();
                    foreach (int selectedPrim in PCGSelectionState.SelectedPrimIndices)
                    {
                        bool isBoundary = false;
                        foreach (int v in _bridge.Geometry.Primitives[selectedPrim])
                        {
                            if (_vertexToPrims.TryGetValue(v, out var adjPrims))
                            {
                                foreach (int adj in adjPrims)
                                {
                                    if (adj != selectedPrim && !PCGSelectionState.SelectedPrimIndices.Contains(adj))
                                    {
                                        isBoundary = true;
                                        break;
                                    }
                                }
                            }
                            if (isBoundary) break;
                        }
                        if (isBoundary) toRemove.Add(selectedPrim);
                    }
                    foreach (int idx in toRemove)
                        PCGSelectionState.SelectedPrimIndices.Remove(idx);
                    break;
                }

                case PCGSelectMode.Vertex:
                {
                    var toRemove = new HashSet<int>();
                    foreach (int selectedVert in PCGSelectionState.SelectedPointIndices)
                    {
                        bool isBoundary = false;
                        if (_vertexToPrims.TryGetValue(selectedVert, out var adjPrims))
                        {
                            foreach (int primIdx in adjPrims)
                            {
                                foreach (int v in _bridge.Geometry.Primitives[primIdx])
                                {
                                    if (v != selectedVert && !PCGSelectionState.SelectedPointIndices.Contains(v))
                                    {
                                        isBoundary = true;
                                        break;
                                    }
                                }
                                if (isBoundary) break;
                            }
                        }
                        if (isBoundary) toRemove.Add(selectedVert);
                    }
                    foreach (int idx in toRemove)
                        PCGSelectionState.SelectedPointIndices.Remove(idx);
                    break;
                }

                case PCGSelectMode.Edge:
                {
                    var toRemove = new HashSet<int>();
                    foreach (int selectedEdge in PCGSelectionState.SelectedEdgeIndices)
                    {
                        if (selectedEdge < 0 || selectedEdge >= _bridge.Geometry.Edges.Count) continue;
                        var edge = _bridge.Geometry.Edges[selectedEdge];
                        bool isBoundary = false;
                        for (int j = 0; j < 2 && !isBoundary; j++)
                        {
                            if (_vertexToEdges.TryGetValue(edge[j], out var adjEdges))
                            {
                                foreach (int adj in adjEdges)
                                {
                                    if (adj != selectedEdge && !PCGSelectionState.SelectedEdgeIndices.Contains(adj))
                                    {
                                        isBoundary = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isBoundary) toRemove.Add(selectedEdge);
                    }
                    foreach (int idx in toRemove)
                        PCGSelectionState.SelectedEdgeIndices.Remove(idx);
                    break;
                }
            }

            SceneView.RepaintAll();
        }

        // ---- Select by attribute (Phase C) ----

        public void SelectByNormal(Vector3 direction, float threshold = 0.7f)
        {
            if (_bridge.Geometry == null) return;

            direction.Normalize();
            for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
            {
                var prim = _bridge.Geometry.Primitives[i];
                if (prim.Length < 3) continue;

                Vector3 a = _bridge.Geometry.Points[prim[0]];
                Vector3 b = _bridge.Geometry.Points[prim[1]];
                Vector3 c = _bridge.Geometry.Points[prim[2]];
                Vector3 faceNormal = Vector3.Cross(b - a, c - a).normalized;

                if (Vector3.Dot(faceNormal, direction) >= threshold)
                    PCGSelectionState.SelectedPrimIndices.Add(i);
            }

            SceneView.RepaintAll();
        }

        public void SelectByMaterialId(int primIndex)
        {
            if (_bridge.Geometry == null) return;

            var matAttr = _bridge.Geometry.PrimAttribs.GetAttribute("material");
            var matIdAttr = _bridge.Geometry.PrimAttribs.GetAttribute("materialId");

            object targetValue = null;
            PCGAttribute attr = null;

            if (matAttr != null && primIndex < matAttr.Values.Count)
            {
                attr = matAttr;
                targetValue = matAttr.Values[primIndex];
            }
            else if (matIdAttr != null && primIndex < matIdAttr.Values.Count)
            {
                attr = matIdAttr;
                targetValue = matIdAttr.Values[primIndex];
            }

            if (attr == null || targetValue == null) return;

            for (int i = 0; i < attr.Values.Count; i++)
            {
                if (Equals(attr.Values[i], targetValue))
                    PCGSelectionState.SelectedPrimIndices.Add(i);
            }

            SceneView.RepaintAll();
        }
    }
}
