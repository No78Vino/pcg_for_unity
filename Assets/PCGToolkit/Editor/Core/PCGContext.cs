using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    public class PCGContext
    {
        public bool Debug { get; set; }
        public Dictionary<string, PCGGeometry> NodeOutputCache = new Dictionary<string, PCGGeometry>();
        public Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();
        public string CurrentNodeId;
        public List<string> Logs = new List<string>();
        public bool HasError { get; private set; }
        public string ErrorMessage { get; private set; }

        // 迭代六：场景对象引用字典（执行期间持有强引用）
        public Dictionary<string, UnityEngine.Object> SceneReferences = new Dictionary<string, UnityEngine.Object>();

        public PCGContext() { }
        public PCGContext(bool debug) { Debug = debug; }

        /// <summary>从 SceneReferences 获取 GameObject</summary>
        public GameObject GetSceneGameObject(string key)
        {
            SceneReferences.TryGetValue(key, out var obj);
            return obj as GameObject;
        }

        public void Log(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] {message}";
            Logs.Add(logEntry);
            UnityEngine.Debug.Log(logEntry);
        }

        public void LogWarning(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] WARNING: {message}";
            Logs.Add(logEntry);
            UnityEngine.Debug.LogWarning(logEntry);
        }

        public void LogError(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] ERROR: {message}";
            Logs.Add(logEntry);
            HasError = true;
            ErrorMessage = message;
            UnityEngine.Debug.LogError(logEntry);
        }

        public void CacheOutput(string nodeId, PCGGeometry geometry)
        {
            NodeOutputCache[nodeId] = geometry;
        }

        public PCGGeometry GetCachedOutput(string nodeId)
        {
            NodeOutputCache.TryGetValue(nodeId, out var geo);
            return geo;
        }

        public void ClearCache()
        {
            NodeOutputCache.Clear();
            Logs.Clear();
            HasError = false;
            ErrorMessage = null;
        }

        public void SetExternalInput(string key, PCGGeometry geometry)
        {
            NodeOutputCache[$"__external_input__.{key}"] = geometry;
        }

        public bool TryGetExternalOutput(string key, out PCGGeometry geometry)
        {
            return NodeOutputCache.TryGetValue($"__external_output__.{key}", out geometry);
        }

        public void SetExternalOutput(string key, PCGGeometry geometry)
        {
            NodeOutputCache[$"__external_output__.{key}"] = geometry;
        }

        public bool TryGetExternalInput(string key, out PCGGeometry geometry)
        {
            return NodeOutputCache.TryGetValue($"__external_input__.{key}", out geometry);
        }
    }
}
