using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PCGToolkit.Graph
{
    public static class PCGHoudiniLayout
    {
        private const float RightPanelWidthRatio = 0.25f;
        private const float TopHeightRatio = 0.5f;
        private const string PrefKey = "PCGToolkit_AutoLayout";

        [MenuItem("PCG Toolkit/Apply Houdini Layout")]
        public static void ApplyLayout()
        {
            ApplyLayoutInternal();
        }

        public static void ApplyLayoutIfFirstTime()
        {
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            EditorPrefs.SetBool(PrefKey, true);
            EditorApplication.delayCall += ApplyLayoutInternal;
        }

        [MenuItem("PCG Toolkit/Reset Layout Preference")]
        public static void ResetLayoutPreference()
        {
            EditorPrefs.DeleteKey(PrefKey);
            Debug.Log("[PCGHoudiniLayout] Layout preference reset. Next time Node Editor opens, layout will auto-apply.");
        }

        private static void ApplyLayoutInternal()
        {
            try
            {
                var sceneView = EditorWindow.GetWindow<SceneView>();
                var nodeEditor = EditorWindow.GetWindow<PCGGraphEditorWindow>();
                var inspector = PCGNodeInspectorWindow.Open();

                if (!TryReflectionDocking(sceneView, nodeEditor, inspector))
                    ArrangeFloatingWindows(sceneView, nodeEditor, inspector);

                nodeEditor.Focus();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PCGHoudiniLayout] Failed to apply layout: {e.Message}");
            }
        }

        private static bool TryReflectionDocking(SceneView sceneView, PCGGraphEditorWindow nodeEditor, PCGNodeInspectorWindow inspector)
        {
            try
            {
                var editorAssembly = typeof(EditorWindow).Assembly;
                var dockAreaType = editorAssembly.GetType("UnityEditor.DockArea");
                var splitViewType = editorAssembly.GetType("UnityEditor.SplitView");
                var containerWindowType = editorAssembly.GetType("UnityEditor.ContainerWindow");
                if (dockAreaType == null || splitViewType == null || containerWindowType == null)
                    return false;

                var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
                if (parentField == null) return false;

                var sceneParent = parentField.GetValue(sceneView);
                if (sceneParent == null) return false;

                // Try to use internal SplitView API
                var viewType = editorAssembly.GetType("UnityEditor.View");
                if (viewType == null) return false;

                var parentProp = viewType.GetProperty("parent", BindingFlags.Public | BindingFlags.Instance);
                if (parentProp == null) return false;

                var sceneViewParent = parentProp.GetValue(sceneParent);
                if (sceneViewParent == null || sceneViewParent.GetType() != splitViewType)
                    return false;

                // Attempt to add views to the split
                var addChildMethod = splitViewType.GetMethod("AddChild", BindingFlags.Public | BindingFlags.Instance,
                    null, new Type[] { viewType }, null);
                if (addChildMethod == null) return false;

                var nodeParent = parentField.GetValue(nodeEditor);
                var inspectorParent = parentField.GetValue(inspector);

                if (nodeParent == null || inspectorParent == null)
                    return false;

                // If we got this far, the internal API structure is available but the
                // actual docking logic is too fragile across Unity versions.
                // Return false to use the reliable floating window fallback.
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static void ArrangeFloatingWindows(SceneView sceneView, PCGGraphEditorWindow nodeEditor, PCGNodeInspectorWindow inspector)
        {
            float screenW = Screen.currentResolution.width;
            float screenH = Screen.currentResolution.height;
            float margin = 40f;
            float usableW = screenW - margin;
            float usableH = screenH - margin * 2;

            float rightPanelW = Mathf.Max(350f, usableW * RightPanelWidthRatio);
            float leftW = usableW - rightPanelW;
            float topH = usableH * TopHeightRatio;
            float bottomH = usableH - topH;

            sceneView.position = new Rect(0, margin, leftW, topH);
            nodeEditor.position = new Rect(0, margin + topH, leftW, bottomH);
            inspector.position = new Rect(leftW, margin, rightPanelW, usableH);
        }
    }
}
