using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG 节点执行上下文，用于在节点之间传递中间结果和全局状态。
    /// </summary>
    public class PCGContext
    {
        /// <summary>是否启用调试模式</summary>
        public bool Debug { get; set; }

        /// <summary>节点执行过程中的中间结果缓存，key 为节点 ID</summary>
        public Dictionary<string, PCGGeometry> NodeOutputCache = new Dictionary<string, PCGGeometry>();

        /// <summary>全局变量，可供所有节点访问</summary>
        public Dictionary<string, object> GlobalVariables = new Dictionary<string, object>();

        /// <summary>当前正在执行的节点 ID</summary>
        public string CurrentNodeId;

        /// <summary>日志记录</summary>
        public List<string> Logs = new List<string>();

        /// <summary>是否有错误发生</summary>
        public bool HasError { get; private set; }

        /// <summary>错误信息</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PCGContext() { }

        /// <summary>
        /// 带调试模式参数的构造函数
        /// </summary>
        public PCGContext(bool debug)
        {
            Debug = debug;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public void Log(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] {message}";
            Logs.Add(logEntry);
            UnityEngine.Debug.Log(logEntry);
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        public void LogWarning(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] WARNING: {message}";
            Logs.Add(logEntry);
            UnityEngine.Debug.LogWarning(logEntry);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        public void LogError(string message)
        {
            var logEntry = $"[Node:{CurrentNodeId}] ERROR: {message}";
            Logs.Add(logEntry);
            HasError = true;
            ErrorMessage = message;
            UnityEngine.Debug.LogError(logEntry);
        }

        /// <summary>
        /// 缓存节点输出结果
        /// </summary>
        public void CacheOutput(string nodeId, PCGGeometry geometry)
        {
            NodeOutputCache[nodeId] = geometry;
        }

        /// <summary>
        /// 获取缓存的节点输出结果
        /// </summary>
        public PCGGeometry GetCachedOutput(string nodeId)
        {
            NodeOutputCache.TryGetValue(nodeId, out var geo);
            return geo;
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            NodeOutputCache.Clear();
            Logs.Clear();
            HasError = false;
            ErrorMessage = null;
        }
    }
}
