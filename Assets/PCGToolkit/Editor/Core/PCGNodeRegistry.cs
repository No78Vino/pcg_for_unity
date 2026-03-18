using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 节点注册中心，管理所有可用的 PCG 节点类型。
    /// 提供节点查询、分类检索等功能，同时服务于 GraphView 搜索菜单和 AI Agent Skill 发现。
    /// </summary>
    public static class PCGNodeRegistry
    {
        private static readonly Dictionary<string, IPCGNode> _registeredNodes = new Dictionary<string, IPCGNode>();
        private static bool _initialized = false;

        /// <summary>
        /// 注册一个节点
        /// </summary>
        public static void Register(IPCGNode node)
        {
            if (node == null) return;
            _registeredNodes[node.Name] = node;
        }

        /// <summary>
        /// 根据名称获取节点
        /// </summary>
        public static IPCGNode GetNode(string name)
        {
            EnsureInitialized();
            _registeredNodes.TryGetValue(name, out var node);
            return node;
        }

        /// <summary>
        /// 获取所有已注册的节点
        /// </summary>
        public static IEnumerable<IPCGNode> GetAllNodes()
        {
            EnsureInitialized();
            return _registeredNodes.Values;
        }

        /// <summary>
        /// 按类别获取节点
        /// </summary>
        public static IEnumerable<IPCGNode> GetNodesByCategory(PCGNodeCategory category)
        {
            EnsureInitialized();
            return _registeredNodes.Values.Where(n => n.Category == category);
        }

        /// <summary>
        /// 获取所有节点名称
        /// </summary>
        public static IEnumerable<string> GetNodeNames()
        {
            EnsureInitialized();
            return _registeredNodes.Keys;
        }

        /// <summary>
        /// 自动扫描并注册所有 PCGNodeBase 子类
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            var baseType = typeof(PCGNodeBase);
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t));

            foreach (var type in nodeTypes)
            {
                try
                {
                    var instance = (IPCGNode)Activator.CreateInstance(type);
                    Register(instance);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PCGNodeRegistry] Failed to register node type {type.Name}: {e.Message}");
                }
            }

            Debug.Log($"[PCGNodeRegistry] Initialized with {_registeredNodes.Count} nodes.");
        }

        /// <summary>
        /// 强制重新扫描
        /// </summary>
        public static void Refresh()
        {
            _registeredNodes.Clear();
            _initialized = false;
            EnsureInitialized();
        }
    }
}
