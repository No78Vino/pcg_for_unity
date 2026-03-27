using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// SceneView 左上角叠加显示当前 PCG 执行状态和几何体统计
    /// </summary>
    [InitializeOnLoad]
    public static class PCGDebugOverlay
    {
        private static bool _active;
        private static string _currentNodeName = "";
        private static float _progress;
        private static int _totalNodes;
        private static int _completedNodes;
        private static string _lastGeoStats = "";
        private static bool _isLiveMode;

        static PCGDebugOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void Show(string nodeName, int completed, int total, PCGGeometry currentGeo)
        {
            _active = true;
            _currentNodeName = nodeName;
            _completedNodes = completed;
            _totalNodes = total;
            _progress = total > 0 ? (float)completed / total : 0;

            if (currentGeo != null)
                _lastGeoStats = $"Pts: {currentGeo.Points.Count:N0} | Prims: {currentGeo.Primitives.Count:N0}";
            else
                _lastGeoStats = "";

            SceneView.RepaintAll();
        }

        public static void Hide()
        {
            _active = false;
            SceneView.RepaintAll();
        }

        public static void SetLiveMode(bool isLive)
        {
            _isLiveMode = isLive;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!_active) return;

            Handles.BeginGUI();

            var rect = new Rect(10, 10, 280, 80);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            string modeLabel = _isLiveMode ? "[LIVE]" : "[EXEC]";
            GUI.Label(new Rect(14, 12, 270, 18), $"{modeLabel} {_currentNodeName}", EditorStyles.boldLabel);
            GUI.Label(new Rect(14, 30, 270, 18), $"Progress: {_completedNodes}/{_totalNodes} ({_progress:P0})");
            GUI.Label(new Rect(14, 48, 270, 18), _lastGeoStats);

            EditorGUI.ProgressBar(new Rect(14, 66, 262, 12), _progress, "");

            Handles.EndGUI();
        }
    }
}
