using System.Collections.Generic;
using System.Linq;
using UnityEditor;  
using UnityEngine;  
using UnityEngine.UIElements;
using PCGToolkit.Core;  
  
namespace PCGToolkit.Graph  
{  
    public class PCGNodePreviewWindow : EditorWindow  
    {  
        private enum PreviewRenderMode { Shaded, Wireframe, ShadedWireframe }

        private PCGGeometry _geometry;  
        private Mesh _previewMesh;  
        private PreviewRenderUtility _previewRenderUtility;  
        private Material _previewMaterial;  
        private Material _wireMaterial;
        private PreviewRenderMode _renderMode = PreviewRenderMode.Shaded;
        private float _rotationX = -30f;  
        private float _rotationY = 45f;  
        private float _zoom = 3f;  
        private string _nodeDisplayName = "";  
        private string _nodeId = "";  
        private double _executionTimeMs;

        // A4: Geometry Debug 可折叠面板
        private bool _showDataPanel = false;
        private Vector2 _dataPanelScrollPos = Vector2.zero;
  
        public static PCGNodePreviewWindow Open()  
        {  
            var window = GetWindow<PCGNodePreviewWindow>();  
            window.titleContent = new GUIContent("Node Preview");  
            window.minSize = new Vector2(300, 300);  
            return window;  
        }  
  
        public void SetPreviewData(string nodeId, string displayName, PCGGeometry geometry, double executionTimeMs)  
        {  
            _nodeId = nodeId;  
            _nodeDisplayName = displayName;  
            _geometry = geometry;  
            _executionTimeMs = executionTimeMs;  
  
            if (geometry != null && geometry.Points.Count > 0)  
            {  
                string cacheKey = "preview_" + nodeId + "_" + PCGGeometrySerializer.ComputeHash(geometry);  
                _previewMesh = PCGCacheManager.GetOrCreateMesh(cacheKey, geometry);  
            }  
            else  
            {  
                _previewMesh = null;  
            }  
  
            Repaint();  
        }  
  
        public void ClearPreview()  
        {  
            _geometry = null;  
            _nodeDisplayName = "";  
            _nodeId = "";  
            _executionTimeMs = 0;  
            if (_previewMesh != null)  
                DestroyImmediate(_previewMesh);  
            _previewMesh = null;  
            Repaint();  
        }  
  
        private void OnEnable()  
        {  
            _previewRenderUtility = new PreviewRenderUtility();  
            _previewRenderUtility.camera.fieldOfView = 30f;  
            _previewRenderUtility.camera.nearClipPlane = 0.01f;  
            _previewRenderUtility.camera.farClipPlane = 100f;  
  
            _previewMaterial = CreatePreviewMaterial();

            var wireShader = Shader.Find("Hidden/Internal-Colored");
            if (wireShader != null)
            {
                _wireMaterial = new Material(wireShader);
                _wireMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _wireMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _wireMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _wireMaterial.SetInt("_ZWrite", 0);
                _wireMaterial.SetColor("_Color", new Color(0f, 1f, 0f, 1f));
                _wireMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
        }

        private static Material CreatePreviewMaterial()
        {
            string[] shaderCandidates = new[]
            {
                "Universal Render Pipeline/Lit",
                "HDRP/Lit",
                "Standard",
            };

            Shader shader = null;
            foreach (var name in shaderCandidates)
            {
                shader = Shader.Find(name);
                if (shader != null && !shader.name.Contains("Error")) break;
                shader = null;
            }

            Material mat;
            if (shader != null)
            {
                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f));
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", new Color(0.7f, 0.7f, 0.7f));
            }
            else
            {
                mat = new Material(AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"));
            }

            return mat;
        }
  
        private void OnDisable()  
        {  
            if (_previewRenderUtility != null)  
            {  
                _previewRenderUtility.Cleanup();  
                _previewRenderUtility = null;  
            }  
            if (_previewMaterial != null)  
                DestroyImmediate(_previewMaterial);
            if (_wireMaterial != null)
                DestroyImmediate(_wireMaterial);
            if (_previewMesh != null)  
                DestroyImmediate(_previewMesh);  
        }  
  
        private void OnGUI()  
        {  
            // 信息栏  
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);  
            GUILayout.Label(_nodeDisplayName, EditorStyles.boldLabel);  
            GUILayout.FlexibleSpace();  
            if (_executionTimeMs > 0)  
                GUILayout.Label($"{_executionTimeMs:F2}ms", EditorStyles.miniLabel);

            // A4: Data Panel 切换按钮
            _showDataPanel = GUILayout.Toggle(_showDataPanel, "Data", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();  
  
            // 几何体信息  
            if (_geometry != null)  
            {  
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);  
                GUILayout.Label(  
                    $"Points: {_geometry.Points.Count}  Prims: {_geometry.Primitives.Count}",  
                    EditorStyles.miniLabel);  
                EditorGUILayout.EndHorizontal();  
            }

            // 渲染模式工具栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Render:", EditorStyles.miniLabel, GUILayout.Width(45));
            if (GUILayout.Toggle(_renderMode == PreviewRenderMode.Shaded, "Shaded", EditorStyles.toolbarButton))
                _renderMode = PreviewRenderMode.Shaded;
            if (GUILayout.Toggle(_renderMode == PreviewRenderMode.Wireframe, "Wire", EditorStyles.toolbarButton))
                _renderMode = PreviewRenderMode.Wireframe;
            if (GUILayout.Toggle(_renderMode == PreviewRenderMode.ShadedWireframe, "Shaded+Wire", EditorStyles.toolbarButton))
                _renderMode = PreviewRenderMode.ShadedWireframe;
            EditorGUILayout.EndHorizontal();
  
            // A4: Geometry Debug 数据面板（可折叠）
            if (_showDataPanel)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                _dataPanelScrollPos = EditorGUILayout.BeginScrollView(_dataPanelScrollPos, GUILayout.Height(200));
                DrawGeometryDebugPanel();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
  
            // 预览区域  
            var previewRect = GUILayoutUtility.GetRect(  
                GUIContent.none, GUIStyle.none,  
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));  
  
            if (_previewMesh == null || _previewRenderUtility == null)  
            {  
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));  
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)  
                {  
                    alignment = TextAnchor.MiddleCenter,  
                    fontSize = 14,  
                };  
                GUI.Label(previewRect, "No geometry to preview", style);  
                return;  
            }  
  
            // 处理鼠标输入  
            HandleInput(previewRect);  
  
            // 渲染预览  
            _previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);  
  
            var cameraPos = Quaternion.Euler(_rotationX, _rotationY, 0) * new Vector3(0, 0, -_zoom);  
            _previewRenderUtility.camera.transform.position = cameraPos;  
            _previewRenderUtility.camera.transform.LookAt(Vector3.zero);  
  
            _previewRenderUtility.lights[0].intensity = 1.0f;  
            _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);  
            _previewRenderUtility.lights[1].intensity = 0.5f;

            if (_renderMode == PreviewRenderMode.Shaded || _renderMode == PreviewRenderMode.ShadedWireframe)
            {
                _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMaterial, 0);
            }

            if (_renderMode == PreviewRenderMode.Wireframe || _renderMode == PreviewRenderMode.ShadedWireframe)
            {
                _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity,
                    _wireMaterial != null ? _wireMaterial : _previewMaterial, 0);
                GL.wireframe = true;
                _previewRenderUtility.camera.Render();
                GL.wireframe = false;
            }
            else
            {
                _previewRenderUtility.camera.Render();
            }
  
            var resultTexture = _previewRenderUtility.EndPreview();  
            GUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);  
        }

        /// <summary>
        /// A4: 绘制Geometry Debug面板（复用Inspector中的逻辑）
        /// </summary>
        private void DrawGeometryDebugPanel()
        {
            if (_geometry == null)
            {
                EditorGUILayout.LabelField("Geometry: null");
                return;
            }

            // Topology
            EditorGUILayout.LabelField("Topology", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Points: {_geometry.Points.Count}");
            EditorGUILayout.LabelField($"Primitives: {_geometry.Primitives.Count}");
            EditorGUILayout.LabelField($"Edges: {_geometry.Edges.Count}");
            int totalVerts = 0;
            foreach (var prim in _geometry.Primitives) totalVerts += prim.Length;
            EditorGUILayout.LabelField($"Total Vertices: {totalVerts}");
            EditorGUI.indentLevel--;

            // 3: Open Spreadsheet 按钮
            if (GUILayout.Button("Open Spreadsheet"))
            {
                var window = PCGGeometrySpreadsheetWindow.Open();
                window.SetGeometry(_geometry, _nodeDisplayName);
            }

            // Point Attributes
            DrawAttribSection("Point Attributes", _geometry.PointAttribs, _geometry.Points.Count);

            // Vertex Attributes
            DrawAttribSection("Vertex Attributes", _geometry.VertexAttribs, totalVerts);

            // Primitive Attributes
            DrawAttribSection("Primitive Attributes", _geometry.PrimAttribs, _geometry.Primitives.Count);

            // Detail Attributes
            DrawAttribSection("Detail Attributes", _geometry.DetailAttribs, 1);

            // Point Groups
            DrawGroupsSection("Point Groups", _geometry.PointGroups);

            // Prim Groups
            DrawGroupsSection("Prim Groups", _geometry.PrimGroups);

            // Validation
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var messages = GeometryValidator.Validate(_geometry);
            if (messages.Count == 0)
            {
                EditorGUILayout.LabelField("No issues found", new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.3f, 0.9f, 0.3f) } });
            }
            else
            {
                foreach (var msg in messages)
                {
                    var color = msg.Severity == GeometryValidator.Severity.Error
                        ? new Color(1f, 0.3f, 0.3f)
                        : new Color(1f, 0.9f, 0.3f);
                    var prefix = msg.Severity == GeometryValidator.Severity.Error ? "[Error]" : "[Warning]";
                    EditorGUILayout.LabelField($"{prefix} {msg.Message}",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = color }, fontSize = 9 });
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DrawAttribSection(string title, AttributeStore attribStore, int expectedCount)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var attrs = attribStore.GetAllAttributes().ToList();
            if (attrs.Count == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                foreach (var attr in attrs)
                {
                    bool hasError = attr.Values.Count != expectedCount;
                    var color = hasError
                        ? new Color(1f, 0.3f, 0.3f)
                        : new Color(0.7f, 0.7f, 0.7f);
                    EditorGUILayout.LabelField($"{attr.Name}: {attr.Type} [{attr.Values.Count}/{expectedCount}]",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = color }, fontSize = 9 });
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DrawGroupsSection(string title, Dictionary<string, HashSet<int>> groups)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (groups.Count == 0)
            {
                EditorGUILayout.LabelField("(none)");
            }
            else
            {
                foreach (var kvp in groups)
                {
                    EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value.Count} elements",
                        new GUIStyle(EditorStyles.label) { fontSize = 9 });
                }
            }
            EditorGUI.indentLevel--;
        }
  
        private void HandleInput(Rect previewRect)  
        {  
            var evt = Event.current;  
            if (!previewRect.Contains(evt.mousePosition)) return;  
  
            switch (evt.type)  
            {  
                case EventType.MouseDrag:  
                    if (evt.button == 0) // 左键拖拽旋转  
                    {  
                        _rotationY += evt.delta.x * 0.5f;  
                        _rotationX += evt.delta.y * 0.5f;  
                        _rotationX = Mathf.Clamp(_rotationX, -89f, 89f);  
                        evt.Use();  
                        Repaint();  
                    }  
                    break;  
  
                case EventType.ScrollWheel: // 滚轮缩放  
                    _zoom += evt.delta.y * 0.1f;  
                    _zoom = Mathf.Clamp(_zoom, 0.5f, 20f);  
                    evt.Use();  
                    Repaint();  
                    break;  
            }  
        }  
    }  
}