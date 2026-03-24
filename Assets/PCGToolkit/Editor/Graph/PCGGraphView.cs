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

        // E5: 暴露参数追踪列表（与图数据同步）
        private List<PCGExposedParamInfo> _exposedParams = new List<PCGExposedParamInfo>();
        
        // 迭代一：脏状态事件
        public event Action OnGraphChanged;

        // 新增：选中节点变化事件
        public event Action<PCGNodeVisual> OnSelectionNodeChanged;

        // 新增：供 Inspector 调用的脏状态通知
        public void NotifyGraphChanged()
        {
            OnGraphChanged?.Invoke();
        }

        // E5: 暴露参数查询/修改
        public bool IsParamExposed(string nodeId, string paramName)
        {
            return _exposedParams.Exists(e => e.NodeId == nodeId && e.ParamName == paramName);
        }

        public void SetParamExposed(string nodeId, string paramName, bool exposed)
        {
            if (exposed)
            {
                if (!IsParamExposed(nodeId, paramName))
                    _exposedParams.Add(new PCGExposedParamInfo { NodeId = nodeId, ParamName = paramName });
            }
            else
            {
                _exposedParams.RemoveAll(e => e.NodeId == nodeId && e.ParamName == paramName);
            }
            OnGraphChanged?.Invoke();
        }

        // C2: 执行单个 Output 节点（忽略 enabled 参数）
        public void ExecuteSingleOutputNode(PCGNodeVisual nodeVisual)
        {
            if (nodeVisual == null) return;
            var data = SaveToGraphData();
            var nodeData = data.Nodes.Find(n => n.NodeId == nodeVisual.NodeId);
            if (nodeData != null)
            {
                var existing = nodeData.Parameters.Find(p => p.Key == "enabled");
                if (existing != null) existing.ValueJson = "true";
                else nodeData.Parameters.Add(new PCGSerializedParameter
                    { Key = "enabled", ValueType = "bool", ValueJson = "true" });
            }
            _lastExecutor = new PCGGraphExecutor(data);
            _lastExecutor.Execute();
            UnityEngine.Debug.Log($"[PCGGraphView] Single export executed for {nodeVisual.PCGNode.DisplayName}");
        }

        // D2: 获取上次执行的节点输出（用于 Inject to Scene）
        private PCGGraphExecutor _lastExecutor;
        public PCGGeometry GetLastOutputGeometry(string nodeId)
        {
            return _lastExecutor?.GetNodeOutput(nodeId);
        }
        
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

            // P3-3: 端口对齐 - 按端口位置排序，最近的排在前面
            var startWorldPos = startPort.GetGlobalCenter();
            compatiblePorts.Sort((a, b) =>
            {
                float distA = Vector2.Distance(a.GetGlobalCenter(), startWorldPos);
                float distB = Vector2.Distance(b.GetGlobalCenter(), startWorldPos);
                return distA.CompareTo(distB);
            });

            return compatiblePorts;
        }

        // P3-3: 端口自动对齐（当两个节点靠近时）
        public void SnapNodesForConnection(PCGNodeVisual nodeA, PCGNodeVisual nodeB)
        {
            if (nodeA == null || nodeB == null) return;

            Port nearestOutPort = null;
            Port nearestInPort = null;
            PCGNodeVisual targetNode = null; // 需要移动的节点  
            float minDist = float.MaxValue;

            // 正向：nodeA.output → nodeB.input  
            foreach (var portA in nodeA.OutputPorts.Values)
            {
                foreach (var portB in nodeB.InputPorts.Values)
                {
                    if (portA.portType != portB.portType &&
                        portA.portType != typeof(object) &&
                        portB.portType != typeof(object))
                        continue;

                    float dist = Vector2.Distance(portA.GetGlobalCenter(), portB.GetGlobalCenter());
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestOutPort = portA;
                        nearestInPort = portB;
                        targetNode = nodeB;
                    }
                }
            }

            // 反向：nodeB.output → nodeA.input  
            foreach (var portB in nodeB.OutputPorts.Values)
            {
                foreach (var portA in nodeA.InputPorts.Values)
                {
                    if (portB.portType != portA.portType &&
                        portB.portType != typeof(object) &&
                        portA.portType != typeof(object))
                        continue;

                    float dist = Vector2.Distance(portB.GetGlobalCenter(), portA.GetGlobalCenter());
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestOutPort = portB;
                        nearestInPort = portA;
                        targetNode = nodeA;
                    }
                }
            }

            // 如果距离足够近，自动对齐  
            if (nearestOutPort != null && nearestInPort != null && minDist < 50f)  
            {  
                var outPos = nearestOutPort.GetGlobalCenter();  
                var inPos = nearestInPort.GetGlobalCenter();  
  
                // 将全局坐标转换到 GraphView 内容容器的本地坐标  
                var outLocal = contentViewContainer.WorldToLocal(outPos);  
                var inLocal = contentViewContainer.WorldToLocal(inPos);  
                var offset = outLocal - inLocal;  
  
                // 移动节点B使端口对齐  
                var newPos = nodeB.GetPosition();  
                newPos.position += offset;  
                nodeB.SetPosition(newPos);  
            }
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
        }
        
        // ---- 迭代二：右键上下文菜单 ----
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            // 迭代四修复：转换屏幕坐标为GraphView本地坐标
            var localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            
            // 在事件处理时立即捕获屏幕坐标
            var screenMousePos = _editorWindow != null 
                ? _editorWindow.position.position + evt.mousePosition 
                : evt.mousePosition;
            
            // 画布右键
            evt.menu.AppendAction("Create Node", _ => 
            {
                nodeCreationRequest?.Invoke(new NodeCreationContext()
                {
                    screenMousePosition = screenMousePos
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
                evt.menu.AppendAction("Cut", _ => CutSelection());
                evt.menu.AppendAction("Copy", _ => CopySelection());
                evt.menu.AppendAction("Paste", _ => PasteSelection(localMousePosition));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate", _ => DuplicateSelection());
                evt.menu.AppendAction("Group Selection", _ => GroupSelection());
                evt.menu.AppendAction("Disconnect All", _ => DisconnectSelection());
                evt.menu.AppendAction("Delete", _ => DeleteSelection());
            }
        }

        // P2-5: Cut/Copy/Paste 实现
        private void CutSelection()
        {
            CopySelection();
            DeleteSelection();
        }

        private void CopySelection()
        {
            var selectedNodes = selection.OfType<PCGNodeVisual>().ToList();
            if (selectedNodes.Count == 0) return;

            var data = SerializeSelection(selectedNodes);
            GUIUtility.systemCopyBuffer = data;
        }

        private void PasteSelection(Vector2 position)
        {
            var data = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(data)) return;

            UnserializeAndPaste("Paste", data);
        }

        private string SerializeSelection(List<PCGNodeVisual> nodes)
        {
            var nodeDataList = new List<PCGNodeData>();
            var edgeDataList = new List<PCGEdgeData>();

            foreach (var nodeVisual in nodes)
            {
                var nodeData = new PCGNodeData
                {
                    NodeId = nodeVisual.NodeId,
                    NodeType = nodeVisual.PCGNode.Name,
                    Position = nodeVisual.GetPosition().position
                };

                var defaults = nodeVisual.GetPortDefaultValues();
                if (defaults != null)
                {
                    nodeData.Parameters = new List<PCGSerializedParameter>();
                    foreach (var kvp in defaults)
                    {
                        nodeData.Parameters.Add(new PCGSerializedParameter
                        {
                            Key = kvp.Key,
                            ValueType = kvp.Value?.GetType().Name ?? "null",
                            ValueJson = PCGParamHelper.SerializeParamValue(kvp.Value)
                        });
                    }
                }

                nodeDataList.Add(nodeData);
            }

            // 序列化选中节点之间的连接
            foreach (var edge in edges)
            {
                var outputNode = edge.output.node as PCGNodeVisual;
                var inputNode = edge.input.node as PCGNodeVisual;
                if (outputNode != null && inputNode != null &&
                    nodes.Contains(outputNode) && nodes.Contains(inputNode))
                {
                    edgeDataList.Add(new PCGEdgeData
                    {
                        OutputNodeId = outputNode.NodeId,
                        OutputPort = edge.output.portName,
                        InputNodeId = inputNode.NodeId,
                        InputPort = edge.input.portName
                    });
                }
            }

            var tempData = new PCGGraphData
            {
                Nodes = nodeDataList,
                Edges = edgeDataList
            };

            return JsonUtility.ToJson(tempData);
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
            
            // 创建 Group（分配唯一 GUID）
            var group = new Group { title = "New Group" };
            group.userData = System.Guid.NewGuid().ToString();
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
            note.userData = System.Guid.NewGuid().ToString();
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
                        nodeData.Parameters.Add(new PCGSerializedParameter
                        {
                            Key = kvp.Key,
                            ValueType = kvp.Value?.GetType().Name ?? "null",
                            ValueJson = PCGParamHelper.SerializeParamValue(kvp.Value)
                        });
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
                            OutputPort = outputVisual.FindPortSchemaName(edge.output),
                            InputNodeId = inputVisual.NodeId,
                            InputPort = inputVisual.FindPortSchemaName(edge.input)
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
                            defaults[param.Key] = PCGParamHelper.DeserializeParamValue(param);
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
                    
                    var outputPort = outputVisual.GetOutputPort(edgeData.OutputPort);
                    var inputPort = inputVisual.GetInputPort(edgeData.InputPort);
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
            // 如果焦点在文本输入控件中，不拦截快捷键
            if (IsTargetInTextInput(evt))
                return;

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

        private bool IsTargetInTextInput(EventBase evt)
        {
            if (evt.target is not VisualElement target) return false;
            return target is TextField || target is FloatField || target is IntegerField ||
                   target.GetFirstAncestorOfType<TextField>() != null ||
                   target.GetFirstAncestorOfType<FloatField>() != null ||
                   target.GetFirstAncestorOfType<IntegerField>() != null;
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

        public PCGNodeVisual FindNodeVisualByType(string nodeTypeName)
        {
            PCGNodeVisual found = null;
            nodes.ForEach(node =>
            {
                if (found != null) return;
                if (node is PCGNodeVisual visual && visual.PCGNode.Name == nodeTypeName)
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
                        defaults[param.Key] = PCGParamHelper.DeserializeParamValue(param);  
                    }  
                    visual.SetPortDefaultValues(defaults);  
                }  
  
                nodeVisualMap[nodeData.NodeId] = visual;  
            }  
  
            foreach (var edgeData in data.Edges)  
            {  
                if (!nodeVisualMap.TryGetValue(edgeData.OutputNodeId, out var outputVisual)) continue;  
                if (!nodeVisualMap.TryGetValue(edgeData.InputNodeId, out var inputVisual)) continue;  
  
                var outputPort = outputVisual.GetOutputPort(edgeData.OutputPort);
                var inputPort = inputVisual.GetInputPort(edgeData.InputPort);  
                if (outputPort == null || inputPort == null) continue;  
  
                var edge = outputPort.ConnectTo(inputPort);  
                AddElement(edge);  
            }
            
            // 迭代四修复：加载 Groups
            foreach (var groupData in data.Groups)
            {
                var group = new Group { title = groupData.Title };
                group.userData = groupData.GroupId; // 恢复 GUID
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
                note.userData = noteData.NoteId; // 恢复 GUID
                note.SetPosition(new Rect(noteData.Position, noteData.Size));
                AddElement(note);
            }

            // E5: 恢复暴露参数
            _exposedParams = data.ExposedParameters != null
                ? new List<PCGExposedParamInfo>(data.ExposedParameters)
                : new List<PCGExposedParamInfo>();
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
                        OutputPort = outputVisual.FindPortSchemaName(edge.output),
                        InputNodeId = inputVisual.NodeId,
                        InputPort = inputVisual.FindPortSchemaName(edge.input)
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
                        GroupId = group.userData as string ?? System.Guid.NewGuid().ToString(),
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
                        NoteId = note.userData as string ?? System.Guid.NewGuid().ToString(),
                        Title = note.title,
                        Content = note.contents,
                        Position = note.GetPosition().position,
                        Size = note.GetPosition().size
                    };
                    data.StickyNotes.Add(noteData);
                }
            });
  
            // E5: 序列化暴露参数
            data.ExposedParameters = new List<PCGExposedParamInfo>(_exposedParams);

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
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // Undo support: record before changes
            if (graphData != null)
                Undo.RecordObject(graphData, "Graph View Change");

            // 迭代一：通知脏状态变更
            OnGraphChanged?.Invoke();

            // 迭代四修复：在拖拽连线时检测端口类型（用于搜索窗口过滤）
            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
            {
                var edge = change.edgesToCreate[0];
                if (edge.output != null && edge.output.portType != typeof(object))
                {
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

            // 处理新建连线 — Inspector 通过 Update() 轮询自动刷新连接状态

            // 处理删除元素 — Inspector 通过 Update() 轮询自动刷新

            return change;  
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