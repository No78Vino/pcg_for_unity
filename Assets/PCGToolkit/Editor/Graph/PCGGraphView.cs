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

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                // 不能连接自身节点
                if (port.node == startPort.node) return;
                // 方向必须相反
                if (port.direction == startPort.direction) return;
                // 类型必须兼容：相同类型，或其中一方是 Any (object)
                if (port.portType != startPort.portType &&
                    port.portType != typeof(object) &&
                    startPort.portType != typeof(object))
                    return;
                compatiblePorts.Add(port);
            });
            return compatiblePorts;
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

        public void ClearAllExecutionTimes()
        {
            nodes.ForEach(node =>
            {
                if (node is PCGNodeVisual visual)
                    visual.ClearExecutionTime();
            });
        }

        public PCGNodeVisual GetSelectedNodeVisual()
        {
            foreach (var selectable in selection)
            {
                if (selectable is PCGNodeVisual visual)
                    return visual;
            }

            return null;
        }

        public PCGNodeVisual CreateNodeVisual(IPCGNode node, Vector2 position)  
        {  
            var visual = new PCGNodeVisual();  
            visual.Initialize(node, position);  
            AddElement(visual);  
            return visual;  
        }
        
        // ---- 数据操作 ----  

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
  
                // 恢复端口默认值  
                if (nodeData.Parameters != null && nodeData.Parameters.Count > 0)  
                {  
                    var defaults = new Dictionary<string, object>();  
                    foreach (var param in nodeData.Parameters)  
                    {  
                        defaults[param.Key] = DeserializeParamValue(param);  
                    }  
                    visual.SetPortDefaultValues(defaults);  
                }  
  
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
  
                // 隐藏已连接端口的内联编辑器  
                inputVisual.OnPortConnectionChanged(edgeData.InputPortName, true);  
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
  
                    // 序列化端口默认值（只保存未连接端口的值）  
                    var defaults = visual.GetPortDefaultValues();  
                    foreach (var kvp in defaults)  
                    {  
                        if (!visual.IsPortConnected(kvp.Key))  
                        {  
                            nodeData.Parameters.Add(SerializeParamValue(kvp.Key, kvp.Value));  
                        }  
                    }  
  
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

        // ---- 序列化辅助 ----  
// ---- 以下方法添加到 PCGGraphView 类中 ----  

// ---- 序列化辅助方法 ----  

        private PCGSerializedParameter SerializeParamValue(string key, object value)
        {
            var param = new PCGSerializedParameter { Key = key };

            if (value == null)
            {
                param.ValueType = "null";
                param.ValueJson = "";
            }
            else if (value is float f)
            {
                param.ValueType = "float";
                param.ValueJson = f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (value is int i)
            {
                param.ValueType = "int";
                param.ValueJson = i.ToString();
            }
            else if (value is bool b)
            {
                param.ValueType = "bool";
                param.ValueJson = b.ToString();
            }
            else if (value is string s)
            {
                param.ValueType = "string";
                param.ValueJson = s;
            }
            else if (value is Vector3 v)
            {
                param.ValueType = "Vector3";
                param.ValueJson =
                    $"{v.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
            else if (value is Color c)
            {
                param.ValueType = "Color";
                param.ValueJson =
                    $"{c.r.ToString(System.Globalization.CultureInfo.InvariantCulture)},{c.g.ToString(System.Globalization.CultureInfo.InvariantCulture)},{c.b.ToString(System.Globalization.CultureInfo.InvariantCulture)},{c.a.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
            else
            {
                param.ValueType = value.GetType().FullName;
                param.ValueJson = value.ToString();
            }

            return param;
        }

        private object DeserializeParamValue(PCGSerializedParameter param)
        {
            try
            {
                switch (param.ValueType)
                {
                    case "float":
                        return float.Parse(param.ValueJson, System.Globalization.CultureInfo.InvariantCulture);
                    case "int":
                        return int.Parse(param.ValueJson);
                    case "bool":
                        return bool.Parse(param.ValueJson);
                    case "string":
                        return param.ValueJson;
                    case "Vector3":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 3)
                        {
                            return new Vector3(
                                float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
                        }

                        return Vector3.zero;
                    }
                    case "Color":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 4)
                        {
                            return new Color(
                                float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
                        }

                        return Color.white;
                    }
                    case "null":
                        return null;
                    default:
                        return param.ValueJson;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"PCGGraphView: Failed to deserialize param '{param.Key}': {e.Message}");
                return param.ValueJson;
            }
        }
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)  
        {  
            // 处理新建连线 → 隐藏内联编辑器  
            if (change.edgesToCreate != null)  
            {  
                foreach (var edge in change.edgesToCreate)  
                {  
                    if (edge.input?.node is PCGNodeVisual inputVisual)  
                    {  
                        // 通过 portName 反查 schema name  
                        var portName = FindSchemaName(inputVisual, edge.input);  
                        if (portName != null)  
                            inputVisual.OnPortConnectionChanged(portName, true);  
                    }  
                }  
            }  
  
            // 处理删除元素 → 如果删除的是边，恢复内联编辑器  
            if (change.elementsToRemove != null)  
            {  
                foreach (var element in change.elementsToRemove)  
                {  
                    if (element is Edge removedEdge)  
                    {  
                        if (removedEdge.input?.node is PCGNodeVisual inputVisual)  
                        {  
                            var portName = FindSchemaName(inputVisual, removedEdge.input);  
                            if (portName != null)  
                            {  
                                // 检查该端口是否还有其他连线  
                                bool stillConnected = false;  
                                edges.ForEach(e =>  
                                {  
                                    if (e != removedEdge && e.input == removedEdge.input)  
                                        stillConnected = true;  
                                });  
                                if (!stillConnected)  
                                    inputVisual.OnPortConnectionChanged(portName, false);  
                            }  
                        }  
                    }  
                }  
            }  
  
            return change;  
        }  
        
        /// <summary>  
        /// 通过 Port 实例反查 PCGParamSchema 的 Name（因为 port.portName 是 DisplayName）  
        /// </summary>  
        private string FindSchemaName(PCGNodeVisual visual, Port port)  
        {  
            if (visual.PCGNode.Inputs == null) return null;  
            foreach (var schema in visual.PCGNode.Inputs)  
            {  
                var inputPort = visual.GetInputPort(schema.Name);  
                if (inputPort == port)  
                    return schema.Name;  
            }  
            return null;  
        }
    }
}