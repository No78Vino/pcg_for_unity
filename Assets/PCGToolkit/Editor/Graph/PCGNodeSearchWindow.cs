using System;  
using System.Collections.Generic;  
using UnityEditor;  
using UnityEditor.Experimental.GraphView;  
using UnityEngine;  
using PCGToolkit.Core;
using UnityEngine.UIElements;

namespace PCGToolkit.Graph  
{  
    /// <summary>  
    /// 节点搜索窗口（Tab 键或从端口拖拽时弹出）  
    /// 支持按名称/类别搜索，模糊匹配  
    /// 迭代二：支持端口类型过滤  
    /// </summary>  
    public class PCGNodeSearchWindow : ScriptableObject, ISearchWindowProvider  
    {  
        private PCGGraphView graphView;  
        private PCGGraphEditorWindow editorWindow;
        
        // 迭代二：端口过滤
        private PCGPortType? _filterPortType;
        private Direction? _filterDirection;
  
        public void Initialize(PCGGraphView view, PCGGraphEditorWindow window)  
        {  
            graphView = view;  
            editorWindow = window;  
        }
        
        /// <summary>
        /// 迭代二：设置端口过滤条件
        /// </summary>
        public void SetPortFilter(PCGPortType? portType, Direction? direction)
        {
            _filterPortType = portType;
            _filterDirection = direction;
        }
  
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)  
        {  
            var tree = new List<SearchTreeEntry>  
            {  
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),  
            };  
  
            // 按类别分组  
            var categories = new[]  
            {  
                PCGNodeCategory.Create,  
                PCGNodeCategory.Attribute,  
                PCGNodeCategory.Transform,  
                PCGNodeCategory.Utility,  
                PCGNodeCategory.Geometry,  
                PCGNodeCategory.UV,  
                PCGNodeCategory.Distribute,  
                PCGNodeCategory.Curve,  
                PCGNodeCategory.Deform,  
                PCGNodeCategory.Topology,  
                PCGNodeCategory.Procedural,  
                PCGNodeCategory.Output,  
            };  
  
            foreach (var category in categories)  
            {  
                var nodes = PCGNodeRegistry.GetNodesByCategory(category);  
                var nodeList = new List<IPCGNode>(nodes);  
                if (nodeList.Count == 0) continue;
                
                // 迭代二：过滤节点
                var filteredNodes = FilterNodes(nodeList);
                if (filteredNodes.Count == 0) continue;
  
                tree.Add(new SearchTreeGroupEntry(new GUIContent(category.ToString()), 1));  
  
                foreach (var node in filteredNodes)  
                {  
                    tree.Add(new SearchTreeEntry(new GUIContent(node.DisplayName))  
                    {  
                        userData = node,  
                        level = 2,  
                    });  
                }  
            }  
  
            return tree;  
        }
        
        /// <summary>
        /// 迭代二：根据端口过滤条件筛选节点
        /// </summary>
        private List<IPCGNode> FilterNodes(List<IPCGNode> nodes)
        {
            if (!_filterPortType.HasValue || !_filterDirection.HasValue)
                return nodes;
            
            var result = new List<IPCGNode>();
            
            foreach (var node in nodes)
            {
                // 如果是从输入端口拖拽，找有兼容输出端口的节点
                // 如果是从输出端口拖拽，找有兼容输入端口的节点
                var targetDirection = _filterDirection.Value == Direction.Input 
                    ? PCGPortDirection.Output 
                    : PCGPortDirection.Input;
                
                var portList = targetDirection == PCGPortDirection.Output 
                    ? node.Outputs 
                    : node.Inputs;
                
                if (portList == null) continue;
                
                foreach (var schema in portList)
                {
                    if (IsPortTypeCompatible(schema.PortType, _filterPortType.Value))
                    {
                        result.Add(node);
                        break;
                    }
                }
            }
            
            return result;
        }
        
        private bool IsPortTypeCompatible(PCGPortType a, PCGPortType b)
        {
            if (a == PCGPortType.Any || b == PCGPortType.Any)
                return true;
            return a == b;
        }
  
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)  
        {  
            if (entry.userData is IPCGNode selectedNode)  
            {  
                var newNode = (IPCGNode)Activator.CreateInstance(selectedNode.GetType());  
  
                var windowRoot = editorWindow.rootVisualElement;  
                var windowMousePosition = windowRoot.ChangeCoordinatesTo(  
                    windowRoot.parent,  
                    context.screenMousePosition - editorWindow.position.position);  
                var graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);  
  
                graphView.CreateNodeVisual(newNode, graphMousePosition);  
                return true;  
            }  
            return false;  
        }  
    }  
}