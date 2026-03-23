using UnityEditor;  
using UnityEngine;  
using PCGToolkit.Core;  
  
namespace PCGToolkit.Graph  
{  
    public class PCGNodePreviewWindow : EditorWindow  
    {  
        private PCGGeometry _geometry;  
        private Mesh _previewMesh;  
        private PreviewRenderUtility _previewRenderUtility;  
        private Material _previewMaterial;  
        private float _rotationX = -30f;  
        private float _rotationY = 45f;  
        private float _zoom = 3f;  
        private string _nodeDisplayName = "";  
        private string _nodeId = "";  
        private double _executionTimeMs;  
  
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
  
            _previewMaterial = new Material(Shader.Find("Standard"));  
            _previewMaterial.color = new Color(0.7f, 0.7f, 0.7f);  
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
  
            _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMaterial, 0);  
            _previewRenderUtility.camera.Render();  
  
            var resultTexture = _previewRenderUtility.EndPreview();  
            GUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);  
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