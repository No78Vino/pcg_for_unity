using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PCGToolkit.Core
{
    public enum PCGErrorLevel { Warning, Error, Fatal }

    [System.Serializable]
    public class PCGError
    {
        public string NodeId;
        public string Message;
        public PCGErrorLevel Level;
    }

    public class PCGContext
    {
        public bool Debug { get; set; }
        public Dictionary<string, PCGGeometry> NodeOutputCache = new Dictionary<string, PCGGeometry>();
        public Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();
        public string CurrentNodeId;
        public List<string> Logs = new List<string>();

        public List<PCGError> Errors = new List<PCGError>();
        public bool ContinueOnError { get; set; } = false;

        public bool HasError => Errors.Any(e => e.Level >= PCGErrorLevel.Error);
        public bool HasFatal => Errors.Any(e => e.Level == PCGErrorLevel.Fatal);
        public string ErrorMessage => Errors.LastOrDefault(e => e.Level >= PCGErrorLevel.Error)?.Message;

        public Dictionary<string, UnityEngine.Object> SceneReferences = new Dictionary<string, UnityEngine.Object>();

        public PCGContext() { }
        public PCGContext(bool debug) { Debug = debug; }

        public GameObject GetSceneGameObject(string key)
        {
            SceneReferences.TryGetValue(key, out var obj);
            return obj as GameObject;
        }

        public IEnumerable<PCGError> GetNodeErrors(string nodeId)
            => Errors.Where(e => e.NodeId == nodeId);

        public void Log(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] {message}";
            Logs.Add(logEntry);
            UnityEngine.Debug.Log(logEntry);
        }

        public void LogWarning(string message)
        {
            Errors.Add(new PCGError
            {
                NodeId = CurrentNodeId,
                Message = message,
                Level = PCGErrorLevel.Warning
            });
            var logEntry = $"[Node:{CurrentNodeId}] WARNING: {message}";
            Logs.Add(logEntry);
            UnityEngine.Debug.LogWarning(logEntry);
        }

        public void LogError(string message)
        {
            Errors.Add(new PCGError
            {
                NodeId = CurrentNodeId,
                Message = message,
                Level = PCGErrorLevel.Error
            });
            var logEntry = $"[Node:{CurrentNodeId}] ERROR: {message}";
            Logs.Add(logEntry);
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
            Errors.Clear();
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
