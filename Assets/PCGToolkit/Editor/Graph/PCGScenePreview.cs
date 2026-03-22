using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// SceneView 中的实时 PCG 几何体预览（Gizmo 线框）。
    /// 通过 PCGScenePreview.Show(geo) 触发，OnSceneGUI 回调渲染。
    /// </summary>
    [InitializeOnLoad]
    public static class PCGScenePreview
    {
        private static PCGGeometry _previewGeo;
        private static string _previewNodeId;
        private static bool _active;
        private static Color _wireColor = new Color(0.2f, 0.9f, 0.4f, 0.8f);
        private static Color _pointColor = new Color(1.0f, 0.6f, 0.1f, 0.9f);
        private static float _pointSize = 0.05f;

        // 注入场景的临时对象（Inject to Scene）
        private static List<GameObject> _injectedObjects = new List<GameObject>();

        static PCGScenePreview()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>显示预览几何体</summary>
        public static void Show(PCGGeometry geo, string nodeId = null)
        {
            _previewGeo = geo;
            _previewNodeId = nodeId;
            _active = geo != null && geo.Points.Count > 0;
            SceneView.RepaintAll();
        }

        /// <summary>清除预览</summary>
        public static void Hide()
        {
            _previewGeo = null;
            _active = false;
            SceneView.RepaintAll();
        }

        /// <summary>
        /// D2: 将预览几何体实例化为场景 GameObject（临时，重新执行时删除旧的）
        /// </summary>
        public static void InjectToScene(PCGGeometry geo, string label = "PCG_Preview")
        {
            // 删除之前注入的对象
            foreach (var old in _injectedObjects)
                if (old != null) Object.DestroyImmediate(old);
            _injectedObjects.Clear();

            if (geo == null || geo.Points.Count == 0) return;

            var mesh = PCGGeometryToMesh.Convert(geo);
            var go = new GameObject(label);
            go.hideFlags = HideFlags.DontSave; // 不随场景保存
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            _injectedObjects.Add(go);
            Selection.activeGameObject = go;
            SceneView.FrameLastActiveSceneView();

            Debug.Log($"[PCGScenePreview] Injected '{label}' to scene (verts:{mesh.vertexCount} tris:{mesh.triangles.Length / 3})");
        }

        private static void OnSceneGUI(SceneView sv)
        {
            if (!_active || _previewGeo == null) return;

            Handles.color = _wireColor;

            var pts = _previewGeo.Points;

            // 绘制线框（每个 Prim 的边）
            foreach (var prim in _previewGeo.Primitives)
            {
                if (prim == null || prim.Length < 2) continue;
                for (int i = 0; i < prim.Length; i++)
                {
                    int a = prim[i];
                    int b = prim[(i + 1) % prim.Length];
                    if (a < pts.Count && b < pts.Count)
                        Handles.DrawLine(pts[a], pts[b]);
                }
            }

            // 如果无面，绘制点云
            if (_previewGeo.Primitives.Count == 0)
            {
                Handles.color = _pointColor;
                foreach (var pt in pts)
                    Handles.DotHandleCap(0, pt, Quaternion.identity, _pointSize, EventType.Repaint);
            }

            // 节点 ID 标签
            if (!string.IsNullOrEmpty(_previewNodeId) && pts.Count > 0)
            {
                Handles.Label(pts[0] + Vector3.up * 0.3f, $"[{_previewNodeId}]");
            }
        }
    }
}
