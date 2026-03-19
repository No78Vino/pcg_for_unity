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
        
        // 迭代一：脏状态事件
        public event Action OnGraphChanged;
        
        // 迭代三：节点点击事件（用于预览）
        public event Action<string> OnNodeClicked;
        
        // 迭代二：端口拖拽过滤
        private PCGPortType? _filterPortType;
        private Direction? _filterDirection;

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
            
            // 迭代一：注册键盘事件
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            // 迭代一：添加 MiniMap
            var miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            Add(miniMap);
            
            // 迭代二：设置复制/粘贴回调
            serializeGraphElements = SerializeGraphElements;
            unserializeAndPaste = UnserializeAndPaste;
            canPasteSerializedData = CanPasteSerializedData;
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
                // 迭代四修复：传递端口过滤信息
                _searchWindow.SetPortFilter(_filterPortType, _filterDirection);
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);
                
                // 清除过滤
                _filterPortType = null;
                _filterDirection = null;
            };
            
            // 迭代四修复：注册端口拖拽事件
            graphViewChanged += change =>
            {
                // 在拖拽连线时检测端口类型
                if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
                {
                    var edge = change.edgesToCreate[0];
                    if (edge.output != null && edge.output.portType != typeof(object))
                    {
                        // 记录输出端口类型用于下次创建节点时的过滤
                        var portType = PCGPortType.Any;
                        if (edge.output.portType == typeof(PCGToolkit.Core.PCGGeometry)) portType = PCGPortType.Geometry;
                        else if (edge.output.portType == typeof(float)) portType = PCGPortType.Float;
                        else if (edge.output.portType == typeof(int)) portType = PCGPortType.Int;
                        else if (edge.output.portType == typeof(bool)) portType = PCGPortType.Bool;
                        else if (edge.output.portType == typeof(string)) portType = PCGPortType.String;
                        else if (edge.output.portType == typeof(Vector3)) portType = PCGPortType.Vector3;
                        else if (edge.output.portType == typeof(Color)) portType = PCGPortType.Color;
                        
                        _filterPortType = portType;
                        _filterDirection = Direction.Input;
                    }
                }
                return change;
            };
        }
        
        // ---- 迭代二：右键上下文菜单 ----
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            // 迭代四修复：转换屏幕坐标为GraphView本地坐标
            var localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            
            // 画布右键
            evt.menu.AppendAction("Create Node", _ => 
            {
                nodeCreationRequest?.Invoke(new NodeCreationContext()
                {
                    screenMousePosition = evt.mousePosition
                });
            });
            
            // 迭代四：添加 Sticky Note
            evt.menu.AppendAction("Add Sticky Note", _ => AddStickyNote(localMousePosition));
            
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Frame All", _ => FrameAll());
            
            // 节点右键（当选中节点时）
            if (selection.OfType<PCGNodeVisual>().Any())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Group Selection", _ => GroupSelection());
                evt.menu.AppendAction("Duplicate", _ => DuplicateSelection());
                evt.menu.AppendAction("Disconnect All", _ => DisconnectSelection());
                evt.menu.AppendAction("Delete", _ => DeleteSelection());
            }
        }
        
        // ---- 迭代四：节点分组与注释 ----
        
        private void GroupSelection()
        {
            var selectedNodes = selection.OfType<PCGNodeVisual>().ToList();
            if (selectedNodes.Count == 0) return;
            
            // 计算包围盒
            var minPos = new Vector2(float.MaxValue, float.MaxValue);
            var maxPos = new Vector2(float.MinValue, float.MinValue);
            
            foreach (var node in selectedNodes)
            {
                var pos = node.GetPosition();
                minPos = Vector2.Min(minPos, pos.position);
                maxPos = Vector2.Max(maxPos, pos.position + pos.size);
            }
            
            // 创建 Group
            var group = new Group { title = "New Group" };
            group.SetPosition(new Rect(minPos - new Vector2(20, 40), maxPos - minPos + new Vector2(40, 60)));
            
            foreach (var node in selectedNodes)
            {
                group.AddElement(node);
            }
            
            AddElement(group);
            OnGraphChanged?.Invoke();
        }
        
        private void AddStickyNote(Vector2 position)
        {
            var note = new StickyNote();
            note.title = "Note";
            note.contents = "Write your note here...";
            note.SetPosition(new Rect(position, new Vector2(200, 100)));
            AddElement(note);
            OnGraphChanged?.Invoke();
        }
        
        // ---- 迭代二：节点复制/粘贴 ----
        
        private string SerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var nodeList = new List<PCGNodeData>();
            var edgeList = new List<PCGEdgeData>();
            
            var nodeVisuals = elements.OfType<PCGNodeVisual>().ToList();
            foreach (var visual in nodeVisuals)
            {
                var nodeData = new PCGNodeData
                {
                    NodeId = visual.NodeId,
                    NodeType = visual.PCGNode.Name,
                    Position = visual.GetPosition().position
                };
                
                var defaults = visual.GetPortDefaultValues();
                foreach (var kvp in defaults)
                {
                    if (!visual.IsPortConnected(kvp.Key))
                    {
                        nodeData.Parameters.Add(SerializeParamValue(kvp.Key, kvp.Value));
                    }
                }
                
                nodeList.Add(nodeData);
            }
            
            // 序列化内部连线
            edges.ForEach(edge =>
            {
                if (edge.output?.node is PCGNodeVisual outputVisual &&
                    edge.input?.node is PCGNodeVisual inputVisual)
                {
                    // 只序列化选中的节点之间的连线
                    if (nodeVisuals.Contains(outputVisual) && nodeVisuals.Contains(inputVisual))
                    {
                        edgeList.Add(new PCGEdgeData
                        {
                            OutputNodeId = outputVisual.NodeId,
                            OutputPortName = outputVisual.FindPortSchemaName(edge.output),
                            InputNodeId = inputVisual.NodeId,
                            InputPortName = inputVisual.FindPortSchemaName(edge.input)
                        });
                    }
                }
            });
            
            var copyData = new PCGCopyData { Nodes = nodeList, Edges = edgeList };
            return JsonUtility.ToJson(copyData);
        }
        
        private void UnserializeAndPaste(string operationName, string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            
            try
            {
                var copyData = JsonUtility.FromJson<PCGCopyData>(data);
                if (copyData == null || copyData.Nodes.Count == 0) return;
                
                // 记录旧ID到新ID的映射
                var idMap = new Dictionary<string, string>();
                var nodeVisualMap = new Dictionary<string, PCGNodeVisual>();
                
                // 创建新节点（偏移位置）
                foreach (var nodeData in copyData.Nodes)
                {
                    var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);
                    if (nodeTemplate == null) continue;
                    
                    var newNode = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());
                    var offsetPosition = nodeData.Position + new Vector2(30, 30);
                    var visual = CreateNodeVisual(newNode, offsetPosition);
                    
                    string newId = System.Guid.NewGuid().ToString();
                    idMap[nodeData.NodeId] = newId;
                    visual.SetNodeId(newId);
                    
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
                    
                    nodeVisualMap[newId] = visual;
                }
                
                // 重建内部连线
                foreach (var edgeData in copyData.Edges)
                {
                    if (!idMap.TryGetValue(edgeData.OutputNodeId, out var newOutputId)) continue;
                    if (!idMap.TryGetValue(edgeData.InputNodeId, out var newInputId)) continue;
                    
                    if (!nodeVisualMap.TryGetValue(newOutputId, out var outputVisual)) continue;
                    if (!nodeVisualMap.TryGetValue(newInputId, out var inputVisual)) continue;
                    
                    var outputPort = outputVisual.GetOutputPort(edgeData.OutputPortName);
                    var inputPort = inputVisual.GetInputPort(edgeData.InputPortName);
                    if (outputPort == null || inputPort == null) continue;
                    
                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PCGGraphView: Failed to paste nodes: {e.Message}");
            }
        }
        
        private bool CanPasteSerializedData(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;
            try
            {
                var copyData = JsonUtility.FromJson<PCGCopyData>(data);
                return copyData != null && copyData.Nodes.Count > 0;
            }
            catch
            {
                return false;
            }
        }
        
        // ---- 迭代二：辅助方法 ----
        
        private void DuplicateSelection()
        {
            var selected = selection.OfType<PCGNodeVisual>().ToList();
            if (selected.Count == 0) return;
            
            var data = SerializeGraphElements(selected);
            UnserializeAndPaste("Duplicate", data);
        }
        
        private void DisconnectSelection()
        {
            var selected = selection.OfType<PCGNodeVisual>().ToList();
            if (selected.Count == 0) return;
            
            var edgesToRemove = new List<Edge>();
            edges.ForEach(edge =>
            {
                if (edge.output?.node is PCGNodeVisual outputVisual &&
                    edge.input?.node is PCGNodeVisual inputVisual)
                {
                    if (selected.Contains(outputVisual) || selected.Contains(inputVisual))
                    {
                        edgesToRemove.Add(edge);
                    }
                }
            });
            
            DeleteElements(edgesToRemove);
        }
        
        // 迭代二：设置端口过滤（由外部调用）
        public void SetPortFilterForCreation(PCGPortType portType, Direction direction)
        {
            _filterPortType = portType;
            _filterDirection = direction;
        }
        
        // ---- 迭代一：键盘快捷键 ----
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            // F: Frame All
            if (evt.keyCode == KeyCode.F)
            {
                FrameAll();
                evt.StopPropagation();
            }
            // Delete: 删除选中
            else if (evt.keyCode == KeyCode.Delete)
            {
                DeleteSelection();
                evt.StopPropagation();
            }
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
            
            // 迭代三：注册节点双击事件
            visual.OnNodeDoubleClicked += nodeId => OnNodeClicked?.Invoke(nodeId);
            
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
            
            // 迭代四修复：加载 Groups
            foreach (var groupData in data.Groups)
            {
                var group = new Group { title = groupData.Title };  
                group.SetPosition(new Rect(groupData.Position, groupData.Size));
                
                foreach (var nodeId in groupData.NodeIds)
                {
                    if (nodeVisualMap.TryGetValue(nodeId, out var visual))
                    {
                        group.AddElement(visual);
                    }
                }
                
                AddElement(group);
            }
            
            // 迭代四修复：加载 StickyNotes
            foreach (var noteData in data.StickyNotes)
            {
                var note = new StickyNote();  
                note.title = noteData.Title;  
                note.contents = noteData.Content;
                note.SetPosition(new Rect(noteData.Position, noteData.Size));
                AddElement(note);
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
                        OutputPortName = outputVisual.FindPortSchemaName(edge.output),  
                        InputNodeId = inputVisual.NodeId,  
                        InputPortName = inputVisual.FindPortSchemaName(edge.input)  
                    };  
                    data.Edges.Add(edgeData);  
                }  
            });
            
            // 迭代四修复：序列化 Groups
            graphElements.ForEach(element =>
            {
                if (element is Group group)
                {
                    var groupData = new PCGGroupData
                    {
                        GroupId = group.title,
                        Title = group.title,
                        Position = group.GetPosition().position,
                        Size = group.GetPosition().size
                    };
                    
                    foreach (var contained in group.containedElements)
                    {
                        if (contained is PCGNodeVisual visual)
                        {
                            groupData.NodeIds.Add(visual.NodeId);
                        }
                    }
                    
                    data.Groups.Add(groupData);
                }
                
                if (element is StickyNote note)
                {
                    var noteData = new PCGStickyNoteData
                    {
                        NoteId = note.title,
                        Title = note.title,
                        Content = note.contents,
                        Position = note.GetPosition().position,
                        Size = note.GetPosition().size
                    };
                    data.StickyNotes.Add(noteData);
                }
            });
  
            return data;  
        }  

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
            // 迭代一：通知脏状态变更
            OnGraphChanged?.Invoke();
            
            // 处理新建连线 → 隐藏内联编辑器  
            if (change.edgesToCreate != null)  
            {  
                foreach (var edge in change.edgesToCreate)  
                {  
                    if (edge.input?.node is PCGNodeVisual inputVisual)  
                    {  
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
    
    // 迭代二：复制粘贴数据结构
    [Serializable]
    public class PCGCopyData
    {
        public List<PCGNodeData> Nodes = new List<PCGNodeData>();
        public List<PCGEdgeData> Edges = new List<PCGEdgeData>();
    }
}