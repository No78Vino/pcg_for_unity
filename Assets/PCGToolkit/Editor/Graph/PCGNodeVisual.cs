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

        // 公开只读访问  
        public IReadOnlyDictionary<string, Port> InputPorts => inputPorts;  
        public IReadOnlyDictionary<string, Port> OutputPorts => outputPorts;
        
        // ---- 执行调试相关 ----
        private Label _executionTimeLabel;
        private VisualElement _highlightBorder;

        // ---- 内联默认值编辑相关 ----
        private Dictionary<string, object> _portDefaultValues = new Dictionary<string, object>();
        private Dictionary<string, PCGParamSchema> _inputSchemas = new Dictionary<string, PCGParamSchema>();

        // 迭代三：节点点击事件（用于预览）
        public event System.Action<string> OnNodeDoubleClicked;


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
            CreateExecutionTimeLabel();
            CreateHighlightBorder();

            RefreshExpandedState();
            RefreshPorts();

            // 迭代三：注册双击事件（用于预览）
            RegisterCallback<ClickEvent>(OnDoubleClick);
        }

        // 迭代三：双击处理
        private void OnDoubleClick(ClickEvent evt)
        {
            // 检测双击
            if (evt.clickCount >= 2)
            {
                OnNodeDoubleClicked?.Invoke(NodeId);
            }
        }

        public void SetNodeId(string id) { NodeId = id; }

        // ---- 内联默认值公共方法 ----

        /// <summary>
        /// 获取所有端口的当前默认值（用于保存/执行）
        /// </summary>
        public Dictionary<string, object> GetPortDefaultValues()
        {
            return new Dictionary<string, object>(_portDefaultValues);
        }

        /// <summary>
        /// 设置端口默认值（用于加载时恢复）
        /// </summary>
        public void SetPortDefaultValues(Dictionary<string, object> values)
        {
            if (values == null) return;
            foreach (var kvp in values)
                _portDefaultValues[kvp.Key] = kvp.Value;
        }

        public void OnPortConnectionChanged(string portName, bool isConnected) { }

        /// <summary>
        /// 检查指定端口是否已连接
        /// </summary>
        public bool IsPortConnected(string portName)
        {
            if (inputPorts.TryGetValue(portName, out var port))
                return port.connected;
            return false;
        }

        /// <summary>
        /// 获取所有输入端口的 Schema 定义（供 Inspector 使用）
        /// </summary>
        public IReadOnlyDictionary<string, PCGParamSchema> GetInputSchemas()
        {
            return _inputSchemas;
        }

        /// <summary>
        /// 获取所有输入端口的连接状态
        /// </summary>
        public Dictionary<string, bool> GetPortConnectionStates()
        {
            var states = new Dictionary<string, bool>();
            foreach (var kvp in inputPorts)
            {
                states[kvp.Key] = kvp.Value.connected;
            }
            return states;
        }

        // ---- 执行调试方法 ----

        public void SetHighlight(bool active)
        {
            if (_highlightBorder == null) return;
            _highlightBorder.visible = active;
        }

        public void ShowExecutionTime(double milliseconds)
        {
            if (_executionTimeLabel == null) return;
            _executionTimeLabel.text = $"{milliseconds:F2}ms";
            _executionTimeLabel.visible = true;
        }

        public void ClearExecutionTime()
        {
            if (_executionTimeLabel == null) return;
            _executionTimeLabel.text = "";
            _executionTimeLabel.visible = false;
        }

        public void SetErrorState(bool hasError)
        {
            if (_highlightBorder == null) return;
            if (hasError)
            {
                var errorColor = new StyleColor(new Color(1f, 0.2f, 0.2f, 0.9f));
                _highlightBorder.style.borderTopColor = errorColor;
                _highlightBorder.style.borderBottomColor = errorColor;
                _highlightBorder.style.borderLeftColor = errorColor;
                _highlightBorder.style.borderRightColor = errorColor;
                _highlightBorder.visible = true;
            }
            else
            {
                var highlightColor = new StyleColor(new Color(1f, 0.85f, 0.1f, 0.9f));
                _highlightBorder.style.borderTopColor = highlightColor;
                _highlightBorder.style.borderBottomColor = highlightColor;
                _highlightBorder.style.borderLeftColor = highlightColor;
                _highlightBorder.style.borderRightColor = highlightColor;
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
                    paddingLeft = 4, paddingRight = 4,
                    paddingTop = 1, paddingBottom = 1,
                    marginTop = 2,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
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

            var c = new Color(1f, 0.85f, 0.1f, 0.9f);
            _highlightBorder.style.borderTopWidth = 3;
            _highlightBorder.style.borderBottomWidth = 3;
            _highlightBorder.style.borderLeftWidth = 3;
            _highlightBorder.style.borderRightWidth = 3;
            _highlightBorder.style.borderTopColor = new StyleColor(c);
            _highlightBorder.style.borderBottomColor = new StyleColor(c);
            _highlightBorder.style.borderLeftColor = new StyleColor(c);
            _highlightBorder.style.borderRightColor = new StyleColor(c);
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

            // P1-4: 节点边框样式（选中时高亮边框）
            style.borderBottomWidth = 2;
            style.borderTopWidth = 2;
            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;
            style.borderBottomColor = new StyleColor(new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f));
            style.borderTopColor = new StyleColor(new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f));
            style.borderLeftColor = new StyleColor(new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f));
            style.borderRightColor = new StyleColor(new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f));

            style.minWidth = 120;
        }

        // P1-4: 更新节点边框（选中/错误状态）
        private bool _isSelected = false;
        private bool _isError = false;

        public void UpdateBorder()
        {
            Color borderColor;

            if (_isError)
            {
                borderColor = new Color(1f, 0.3f, 0.3f);
            }
            else if (_isSelected || selected)
            {
                borderColor = new Color(0.9f, 0.7f, 0.2f);
            }
            else
            {
                // 使用分类颜色的暗色
                var baseColor = titleContainer.style.backgroundColor.value;
                borderColor = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f);
            }

            style.borderBottomColor = new StyleColor(borderColor);
            style.borderTopColor = new StyleColor(borderColor);
            style.borderLeftColor = new StyleColor(borderColor);
            style.borderRightColor = new StyleColor(borderColor);
        }

        public void SetSelectedBorder(bool selected)
        {
            _isSelected = selected;
            UpdateBorder();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            SetSelectedBorder(true);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            SetSelectedBorder(false);
        }

        private void CreateInputPorts()
        {
            if (PCGNode.Inputs == null) return;

            foreach (var schema in PCGNode.Inputs)
            {
                var portCapacity = GetPortCapacity(schema);
                var port = InstantiatePort(
                    Orientation.Horizontal, Direction.Input,
                    portCapacity, GetSystemType(schema.PortType));

                // Geometry 端口保留名称；参数端口只显示短类型标记
                if (schema.PortType == PCGPortType.Geometry || schema.PortType == PCGPortType.Any)
                    port.portName = schema.DisplayName;
                else
                    port.portName = GetPortTypeShortLabel(schema.PortType);

                port.portColor = GetPortColor(schema.PortType);
                port.tooltip = $"{schema.DisplayName}: {schema.Description}";

                inputPorts[schema.Name] = port;
                _inputSchemas[schema.Name] = schema;

                // 初始化默认值
                if (schema.EnumOptions != null && schema.EnumOptions.Length > 0)
                    _portDefaultValues[schema.Name] = schema.DefaultValue as string ?? schema.EnumOptions[0];
                else if (schema.DefaultValue != null)
                    _portDefaultValues[schema.Name] = schema.DefaultValue;

                inputContainer.Add(port);
            }
        }

        private static string GetPortTypeShortLabel(PCGPortType t)
        {
            switch (t)
            {
                case PCGPortType.Float:       return "F";
                case PCGPortType.Int:         return "I";
                case PCGPortType.Bool:        return "B";
                case PCGPortType.String:      return "S";
                case PCGPortType.Vector3:     return "V3";
                case PCGPortType.Color:       return "C";
                case PCGPortType.SceneObject: return "GO";
                default:                      return "";
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

                // 迭代二：添加端口 Tooltip
                port.tooltip = schema.Description;

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
                case PCGPortType.Geometry:    return typeof(PCGGeometry);
                case PCGPortType.Float:       return typeof(float);
                case PCGPortType.Int:         return typeof(int);
                case PCGPortType.Vector3:     return typeof(Vector3);
                case PCGPortType.String:      return typeof(string);
                case PCGPortType.Bool:        return typeof(bool);
                case PCGPortType.Color:       return typeof(Color);
                case PCGPortType.SceneObject: return typeof(GameObject);
                default:                      return typeof(object);
            }
        }

        private Color GetPortColor(PCGPortType portType)
        {
            switch (portType)
            {
                case PCGPortType.Geometry:    return new Color(0.2f, 0.8f, 0.4f);
                case PCGPortType.Float:       return new Color(0.4f, 0.6f, 1.0f);
                case PCGPortType.Int:         return new Color(0.3f, 0.9f, 0.9f);
                case PCGPortType.Vector3:     return new Color(1.0f, 0.8f, 0.2f);
                case PCGPortType.String:      return new Color(1.0f, 0.4f, 0.6f);
                case PCGPortType.Bool:        return new Color(0.9f, 0.3f, 0.3f);
                case PCGPortType.Color:       return Color.white;
                case PCGPortType.SceneObject: return new Color(1.0f, 0.6f, 0.1f);
                default:                      return Color.gray;
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

        /// <summary>
        /// 通过 Port 实例反查 schema.Name（因为 port.portName 是 DisplayName，不能直接用于执行器）
        /// </summary>
        public string GetSchemaNameForPort(Port port)
        {
            foreach (var kvp in inputPorts)
            {
                if (kvp.Value == port) return kvp.Key;
            }
            foreach (var kvp in outputPorts)
            {
                if (kvp.Value == port) return kvp.Key;
            }
            return port.portName; // fallback，不应该走到这里
        }

        // PCGNodeVisual.cs 中添加
        public string FindPortSchemaName(Port port)
        {
            foreach (var kvp in inputPorts)
                if (kvp.Value == port) return kvp.Key;
            foreach (var kvp in outputPorts)
                if (kvp.Value == port) return kvp.Key;
            return null;
        }
    }
}