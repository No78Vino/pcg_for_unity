using System.Collections.Generic;  
using UnityEditor.Experimental.GraphView;  
using UnityEngine;  
using UnityEngine.UIElements;  
using PCGToolkit.Core;  
  
namespace PCGToolkit.Graph  
{  
    public class PCGNodeVisual : Node  
    {  
        public string NodeId { get; private set; }  
        public IPCGNode PCGNode { get; private set; }  
  
        private Dictionary<string, Port> inputPorts = new Dictionary<string, Port>();  
        private Dictionary<string, Port> outputPorts = new Dictionary<string, Port>();  
  
        // ---- 执行调试相关 ----  
        private Label _executionTimeLabel;  
        private VisualElement _highlightBorder;  
  
        public void Initialize(IPCGNode pcgNode, Vector2 position)  
        {  
            PCGNode = pcgNode;  
            NodeId = System.Guid.NewGuid().ToString();  
            title = pcgNode.DisplayName;  
            tooltip = pcgNode.Description;  
  
            SetPosition(new Rect(position, Vector2.zero));  
  
            SetCategoryColor(pcgNode.Category);  
  
            CreateInputPorts();  
            CreateOutputPorts();  
  
            // 创建底部执行时长 Label  
            CreateExecutionTimeLabel();  
  
            // 创建高亮边框覆盖层  
            CreateHighlightBorder();  
  
            RefreshExpandedState();  
            RefreshPorts();  
        }  
  
        public void SetNodeId(string id)  
        {  
            NodeId = id;  
        }  
  
        // ---- 执行调试方法 ----  
  
        /// <summary>  
        /// 显示执行高亮（黄色发光边框）  
        /// </summary>  
        public void SetHighlight(bool active)  
        {  
            if (_highlightBorder == null) return;  
            _highlightBorder.visible = active;  
        }  
  
        /// <summary>  
        /// 显示节点执行耗时  
        /// </summary>  
        public void ShowExecutionTime(double milliseconds)  
        {  
            if (_executionTimeLabel == null) return;  
            _executionTimeLabel.text = $"{milliseconds:F2}ms";  
            _executionTimeLabel.visible = true;  
        }  
  
        /// <summary>  
        /// 清除执行耗时显示  
        /// </summary>  
        public void ClearExecutionTime()  
        {  
            if (_executionTimeLabel == null) return;  
            _executionTimeLabel.text = "";  
            _executionTimeLabel.visible = false;  
        }  
  
        /// <summary>  
        /// 显示执行错误状态（红色边框）  
        /// </summary>  
        public void SetErrorState(bool hasError)  
        {  
            if (_highlightBorder == null) return;  
            if (hasError)  
            {  
                _highlightBorder.style.borderTopColor = new StyleColor(new Color(1f, 0.2f, 0.2f, 0.9f));  
                _highlightBorder.style.borderBottomColor = new StyleColor(new Color(1f, 0.2f, 0.2f, 0.9f));  
                _highlightBorder.style.borderLeftColor = new StyleColor(new Color(1f, 0.2f, 0.2f, 0.9f));  
                _highlightBorder.style.borderRightColor = new StyleColor(new Color(1f, 0.2f, 0.2f, 0.9f));  
                _highlightBorder.visible = true;  
            }  
            else  
            {  
                // 恢复为黄色高亮色  
                var highlightColor = new Color(1f, 0.85f, 0.1f, 0.9f);  
                _highlightBorder.style.borderTopColor = new StyleColor(highlightColor);  
                _highlightBorder.style.borderBottomColor = new StyleColor(highlightColor);  
                _highlightBorder.style.borderLeftColor = new StyleColor(highlightColor);  
                _highlightBorder.style.borderRightColor = new StyleColor(highlightColor);  
                _highlightBorder.visible = false;  
            }  
        }  
  
        // ---- 私有方法 ----  
  
        private void CreateExecutionTimeLabel()  
        {  
            _executionTimeLabel = new Label("")  
            {  
                style =  
                {  
                    fontSize = 10,  
                    unityTextAlign = TextAnchor.MiddleCenter,  
                    color = new StyleColor(new Color(0.9f, 0.9f, 0.3f)),  
                    backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.7f)),  
                    paddingLeft = 4,  
                    paddingRight = 4,  
                    paddingTop = 1,  
                    paddingBottom = 1,  
                    marginTop = 2,  
                    borderTopLeftRadius = 3,  
                    borderTopRightRadius = 3,  
                    borderBottomLeftRadius = 3,  
                    borderBottomRightRadius = 3,  
                }  
            };  
            _executionTimeLabel.visible = false;  
            mainContainer.Add(_executionTimeLabel);  
        }  
  
        private void CreateHighlightBorder()  
        {  
            _highlightBorder = new VisualElement();  
            _highlightBorder.pickingMode = PickingMode.Ignore;  
            _highlightBorder.style.position = Position.Absolute;  
            _highlightBorder.style.top = -2;  
            _highlightBorder.style.bottom = -2;  
            _highlightBorder.style.left = -2;  
            _highlightBorder.style.right = -2;  
  
            var highlightColor = new Color(1f, 0.85f, 0.1f, 0.9f);  
            _highlightBorder.style.borderTopWidth = 3;  
            _highlightBorder.style.borderBottomWidth = 3;  
            _highlightBorder.style.borderLeftWidth = 3;  
            _highlightBorder.style.borderRightWidth = 3;  
            _highlightBorder.style.borderTopColor = new StyleColor(highlightColor);  
            _highlightBorder.style.borderBottomColor = new StyleColor(highlightColor);  
            _highlightBorder.style.borderLeftColor = new StyleColor(highlightColor);  
            _highlightBorder.style.borderRightColor = new StyleColor(highlightColor);  
            _highlightBorder.style.borderTopLeftRadius = 6;  
            _highlightBorder.style.borderTopRightRadius = 6;  
            _highlightBorder.style.borderBottomLeftRadius = 6;  
            _highlightBorder.style.borderBottomRightRadius = 6;  
  
            _highlightBorder.visible = false;  
            Add(_highlightBorder);  
        }  
  
        private void SetCategoryColor(PCGNodeCategory category)  
        {  
            Color color;  
            switch (category)  
            {  
                case PCGNodeCategory.Create: color = new Color(0.15f, 0.45f, 0.2f); break;  
                case PCGNodeCategory.Attribute: color = new Color(0.15f, 0.45f, 0.45f); break;  
                case PCGNodeCategory.Transform: color = new Color(0.55f, 0.5f, 0.1f); break;  
                case PCGNodeCategory.Utility: color = new Color(0.35f, 0.35f, 0.35f); break;  
                case PCGNodeCategory.Geometry: color = new Color(0.2f, 0.35f, 0.6f); break;  
                case PCGNodeCategory.UV: color = new Color(0.4f, 0.2f, 0.55f); break;  
                case PCGNodeCategory.Distribute: color = new Color(0.6f, 0.35f, 0.1f); break;  
                case PCGNodeCategory.Curve: color = new Color(0.6f, 0.25f, 0.4f); break;  
                case PCGNodeCategory.Deform: color = new Color(0.6f, 0.15f, 0.15f); break;  
                case PCGNodeCategory.Topology: color = new Color(0.25f, 0.35f, 0.5f); break;  
                case PCGNodeCategory.Procedural: color = new Color(0.5f, 0.45f, 0.1f); break;  
                case PCGNodeCategory.Output: color = new Color(0.4f, 0.4f, 0.4f); break;  
                default: color = new Color(0.3f, 0.3f, 0.3f); break;  
            }  
  
            titleContainer.style.backgroundColor = new StyleColor(color);  
  
            float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;  
            var textColor = luminance > 0.5f ? Color.black : Color.white;  
  
            var titleLabel = titleContainer.Q<Label>("title-label");  
            if (titleLabel != null)  
                titleLabel.style.color = new StyleColor(textColor);  
        }  
  
        private void CreateInputPorts()  
        {  
            if (PCGNode.Inputs == null) return;  
  
            foreach (var schema in PCGNode.Inputs)  
            {  
                var portType = GetPortCapacity(schema);  
                var port = InstantiatePort(  
                    Orientation.Horizontal, Direction.Input,  
                    portType, GetSystemType(schema.PortType));  
  
                port.portName = schema.DisplayName;  
                port.portColor = GetPortColor(schema.PortType);  
  
                inputPorts[schema.Name] = port;  
                inputContainer.Add(port);  
            }  
        }  
  
        private void CreateOutputPorts()  
        {  
            if (PCGNode.Outputs == null) return;  
  
            foreach (var schema in PCGNode.Outputs)  
            {  
                var port = InstantiatePort(  
                    Orientation.Horizontal, Direction.Output,  
                    Port.Capacity.Multi, GetSystemType(schema.PortType));  
  
                port.portName = schema.DisplayName;  
                port.portColor = GetPortColor(schema.PortType);  
  
                outputPorts[schema.Name] = port;  
                outputContainer.Add(port);  
            }  
        }  
  
        private Port.Capacity GetPortCapacity(PCGParamSchema schema)  
        {  
            return schema.AllowMultiple ? Port.Capacity.Multi : Port.Capacity.Single;  
        }  
  
        private System.Type GetSystemType(PCGPortType portType)  
        {  
            switch (portType)  
            {  
                case PCGPortType.Geometry: return typeof(PCGGeometry);  
                case PCGPortType.Float: return typeof(float);  
                case PCGPortType.Int: return typeof(int);  
                case PCGPortType.Vector3: return typeof(Vector3);  
                case PCGPortType.String: return typeof(string);  
                case PCGPortType.Bool: return typeof(bool);  
                case PCGPortType.Color: return typeof(Color);  
                default: return typeof(object);  
            }  
        }  
  
        private Color GetPortColor(PCGPortType portType)  
        {  
            switch (portType)  
            {  
                case PCGPortType.Geometry: return new Color(0.2f, 0.8f, 0.4f);  
                case PCGPortType.Float: return new Color(0.4f, 0.6f, 1.0f);  
                case PCGPortType.Int: return new Color(0.3f, 0.9f, 0.9f);  
                case PCGPortType.Vector3: return new Color(1.0f, 0.8f, 0.2f);  
                case PCGPortType.String: return new Color(1.0f, 0.4f, 0.6f);  
                case PCGPortType.Bool: return new Color(0.9f, 0.3f, 0.3f);  
                case PCGPortType.Color: return Color.white;  
                default: return Color.gray;  
            }  
        }  
  
        public Port GetInputPort(string portName)  
        {  
            inputPorts.TryGetValue(portName, out var port);  
            return port;  
        }  
  
        public Port GetOutputPort(string portName)  
        {  
            outputPorts.TryGetValue(portName, out var port);  
            return port;  
        }  
    }  
}