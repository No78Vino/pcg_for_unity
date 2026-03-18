using System;  
using System.Collections.Generic;  
using System.Linq;  
using UnityEditor;  
using UnityEditor.Experimental.GraphView;  
using UnityEngine;  
using UnityEngine.UIElements;  
using PCGToolkit.Core;  
  
namespace PCGToolkit.Graph  
{  
    public class PCGGraphView : GraphView  
    {  
        private PCGGraphData graphData;  
        private PCGNodeSearchWindow _searchWindow;  
        private PCGGraphEditorWindow _editorWindow;  
  
        public PCGGraphView()  
        {  
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);  
  
            this.AddManipulator(new ContentDragger());  
            this.AddManipulator(new SelectionDragger());  
            this.AddManipulator(new RectangleSelector());  
  
            var grid = new GridBackground();  
            Insert(0, grid);  
            grid.StretchToParentSize();  
  
            graphViewChanged += OnGraphViewChanged;  
        }  
  
        public void Initialize(PCGGraphEditorWindow editorWindow)  
        {  
            _editorWindow = editorWindow;  
  
            _searchWindow = ScriptableObject.CreateInstance<PCGNodeSearchWindow>();  
            _searchWindow.Initialize(this, editorWindow);  
  
            nodeCreationRequest = ctx =>  
            {  
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);  
            };  
        }  
  
        // ---- 执行调试辅助方法 ----  
  
        /// <summary>  
        /// 根据 NodeId 查找对应的 PCGNodeVisual  
        /// </summary>  
        public PCGNodeVisual FindNodeVisual(string nodeId)  
        {  
            PCGNodeVisual found = null;  
            nodes.ForEach(node =>  
            {  
                if (found != null) return;  
                if (node is PCGNodeVisual visual && visual.NodeId == nodeId)  
                    found = visual;  
            });  
            return found;  
        }  
  
        /// <summary>  
        /// 清除所有节点的高亮状态  
        /// </summary>  
        public void ClearAllHighlights()  
        {  
            nodes.ForEach(node =>  
            {  
                if (node is PCGNodeVisual visual)  
                {  
                    visual.SetHighlight(false);  
                    visual.SetErrorState(false);  
                }  
            });  
        }  
  
        /// <summary>  
        /// 清除所有节点的执行时长显示  
        /// </summary>  
        public void ClearAllExecutionTimes()  
        {  
            nodes.ForEach(node =>  
            {  
                if (node is PCGNodeVisual visual)  
                    visual.ClearExecutionTime();  
            });  
        }  
  
        /// <summary>  
        /// 获取当前选中的第一个 PCGNodeVisual（用于 Run To Selected）  
        /// </summary>  
        public PCGNodeVisual GetSelectedNodeVisual()  
        {  
            foreach (var selectable in selection)  
            {  
                if (selectable is PCGNodeVisual visual)  
                    return visual;  
            }  
            return null;  
        }  
  
        // ---- 原有方法 ----  
  
        public void LoadGraph(PCGGraphData data)  
        {  
            graphData = data;  
            DeleteElements(graphElements.ToList());  
  
            if (data == null) return;  
  
            var nodeVisualMap = new Dictionary<string, PCGNodeVisual>();  
            foreach (var nodeData in data.Nodes)  
            {  
                var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);  
                if (nodeTemplate == null)  
                {  
                    Debug.LogWarning($"PCGGraphView: Node type not found: {nodeData.NodeType}");  
                    continue;  
                }  
  
                var newNode = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());  
                var visual = CreateNodeVisual(newNode, nodeData.Position);  
                visual.SetNodeId(nodeData.NodeId);  
                nodeVisualMap[nodeData.NodeId] = visual;  
            }  
  
            foreach (var edgeData in data.Edges)  
            {  
                if (!nodeVisualMap.TryGetValue(edgeData.OutputNodeId, out var outputVisual)) continue;  
                if (!nodeVisualMap.TryGetValue(edgeData.InputNodeId, out var inputVisual)) continue;  
  
                var outputPort = outputVisual.GetOutputPort(edgeData.OutputPortName);  
                var inputPort = inputVisual.GetInputPort(edgeData.InputPortName);  
                if (outputPort == null || inputPort == null) continue;  
  
                var edge = outputPort.ConnectTo(inputPort);  
                AddElement(edge);  
            }  
        }  
  
        public PCGGraphData SaveToGraphData()  
        {  
            var data = ScriptableObject.CreateInstance<PCGGraphData>();  
  
            nodes.ForEach(node =>  
            {  
                if (node is PCGNodeVisual visual)  
                {  
                    var nodeData = new PCGNodeData  
                    {  
                        NodeId = visual.NodeId,  
                        NodeType = visual.PCGNode.Name,  
                        Position = visual.GetPosition().position  
                    };  
                    data.Nodes.Add(nodeData);  
                }  
            });  
  
            edges.ForEach(edge =>  
            {  
                if (edge.output?.node is PCGNodeVisual outputVisual &&  
                    edge.input?.node is PCGNodeVisual inputVisual)  
                {  
                    var edgeData = new PCGEdgeData  
                    {  
                        OutputNodeId = outputVisual.NodeId,  
                        OutputPortName = edge.output.portName,  
                        InputNodeId = inputVisual.NodeId,  
                        InputPortName = edge.input.portName  
                    };  
                    data.Edges.Add(edgeData);  
                }  
            });  
  
            return data;  
        }  
  
        public PCGNodeVisual CreateNodeVisual(IPCGNode node, Vector2 position)  
        {  
            var visual = new PCGNodeVisual();  
            visual.Initialize(node, position);  
            AddElement(visual);  
            return visual;  
        }  
  
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)  
        {  
            var compatiblePorts = new List<Port>();  
            ports.ForEach(port =>  
            {  
                if (startPort != port &&  
                    startPort.node != port.node &&  
                    startPort.direction != port.direction &&  
                    (startPort.portType == port.portType ||  
                     startPort.portType == typeof(object) ||  
                     port.portType == typeof(object)))  
                {  
                    compatiblePorts.Add(port);  
                }  
            });  
            return compatiblePorts;  
        }  
  
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)  
        {  
            var categories = new[]  
            {  
                PCGNodeCategory.Create, PCGNodeCategory.Attribute,  
                PCGNodeCategory.Transform, PCGNodeCategory.Utility,  
                PCGNodeCategory.Geometry, PCGNodeCategory.UV,  
                PCGNodeCategory.Distribute, PCGNodeCategory.Curve,  
                PCGNodeCategory.Deform, PCGNodeCategory.Topology,  
                PCGNodeCategory.Procedural, PCGNodeCategory.Output,  
            };  
  
            foreach (var category in categories)  
            {  
                var nodesInCategory = PCGNodeRegistry.GetNodesByCategory(category).ToList();  
                foreach (var node in nodesInCategory)  
                {  
                    evt.menu.AppendAction(  
                        $"Create Node/{category}/{node.DisplayName}",  
                        action =>  
                        {  
                            var newNode = (IPCGNode)Activator.CreateInstance(node.GetType());  
                            var localMousePos = contentViewContainer.WorldToLocal(  
                                action.eventInfo.localMousePosition);  
                            CreateNodeVisual(newNode, localMousePos);  
                        });  
                }  
            }  
  
            base.BuildContextualMenu(evt);  
        }  
  
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)  
        {  
            if (change.edgesToCreate != null)  
            {  
                foreach (var edge in change.edgesToCreate) { }  
            }  
  
            if (change.elementsToRemove != null)  
            {  
                foreach (var element in change.elementsToRemove) { }  
            }  
  
            return change;  
        }  
    }  
}