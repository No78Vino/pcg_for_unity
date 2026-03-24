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

        public PCGSceneMeshBridge Bridge => _bridge;

        public override GUIContent toolbarIcon =>
            new GUIContent(EditorGUIUtility.IconContent("EditCollider").image, "PCG Selection Tool");

        public void SetGeometry(PCGGeometry geo)
        {
            _bridge.Instantiate(geo);
            PCGSelectionState.SourceGeometry = geo;
            PCGSelectionState.Clear();
        }

        public override void OnActivated()
        {
            SceneView.RepaintAll();
        }

        public override void OnWillBeDeactivated()
        {
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
                Vector3 screenPos = cam.WorldToScreenPoint(center);
                if (screenPos.z < 0) continue;

                // Convert to GUI coordinates (flip Y)
                Vector2 guiPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
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
                Vector3 screenPos = cam.WorldToScreenPoint(_bridge.Geometry.Points[i]);
                if (screenPos.z < 0) continue;

                Vector2 guiPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
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
                Vector3 screenPos = cam.WorldToScreenPoint(midpoint);
                if (screenPos.z < 0) continue;

                Vector2 guiPos = new Vector2(screenPos.x, cam.pixelHeight - screenPos.y);
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
                        var selectedVerts = new HashSet<int>(_bridge.Geometry.Primitives[selectedPrim]);
                        for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
                        {
                            if (PCGSelectionState.SelectedPrimIndices.Contains(i)) continue;
                            var prim = _bridge.Geometry.Primitives[i];
                            foreach (int v in prim)
                            {
                                if (selectedVerts.Contains(v))
                                {
                                    toAdd.Add(i);
                                    break;
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
                        for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
                        {
                            var prim = _bridge.Geometry.Primitives[i];
                            bool containsSelected = false;
                            foreach (int v in prim)
                            {
                                if (v == selectedVert) { containsSelected = true; break; }
                            }
                            if (containsSelected)
                            {
                                foreach (int v in prim)
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
                    var selectedEndpoints = new HashSet<int>();
                    foreach (int edgeIdx in PCGSelectionState.SelectedEdgeIndices)
                    {
                        if (edgeIdx >= 0 && edgeIdx < _bridge.Geometry.Edges.Count)
                        {
                            selectedEndpoints.Add(_bridge.Geometry.Edges[edgeIdx][0]);
                            selectedEndpoints.Add(_bridge.Geometry.Edges[edgeIdx][1]);
                        }
                    }
                    var toAdd = new HashSet<int>();
                    for (int i = 0; i < _bridge.Geometry.Edges.Count; i++)
                    {
                        if (PCGSelectionState.SelectedEdgeIndices.Contains(i)) continue;
                        var edge = _bridge.Geometry.Edges[i];
                        if (selectedEndpoints.Contains(edge[0]) || selectedEndpoints.Contains(edge[1]))
                            toAdd.Add(i);
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
                        var primVerts = _bridge.Geometry.Primitives[selectedPrim];
                        bool isBoundary = false;
                        foreach (int v in primVerts)
                        {
                            for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
                            {
                                if (i == selectedPrim || !SharesVertex(i, v)) continue;
                                if (!PCGSelectionState.SelectedPrimIndices.Contains(i))
                                {
                                    isBoundary = true;
                                    break;
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
                        for (int i = 0; i < _bridge.Geometry.Primitives.Count; i++)
                        {
                            var prim = _bridge.Geometry.Primitives[i];
                            bool containsSelected = false;
                            foreach (int v in prim)
                            {
                                if (v == selectedVert) { containsSelected = true; break; }
                            }
                            if (!containsSelected) continue;
                            foreach (int v in prim)
                            {
                                if (v != selectedVert && !PCGSelectionState.SelectedPointIndices.Contains(v))
                                {
                                    isBoundary = true;
                                    break;
                                }
                            }
                            if (isBoundary) break;
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
                        for (int i = 0; i < _bridge.Geometry.Edges.Count; i++)
                        {
                            if (i == selectedEdge || PCGSelectionState.SelectedEdgeIndices.Contains(i)) continue;
                            var otherEdge = _bridge.Geometry.Edges[i];
                            if (otherEdge[0] == edge[0] || otherEdge[0] == edge[1] ||
                                otherEdge[1] == edge[0] || otherEdge[1] == edge[1])
                            {
                                isBoundary = true;
                                break;
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

        private bool SharesVertex(int primIdx, int vertIdx)
        {
            var prim = _bridge.Geometry.Primitives[primIdx];
            foreach (int v in prim)
                if (v == vertIdx) return true;
            return false;
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
