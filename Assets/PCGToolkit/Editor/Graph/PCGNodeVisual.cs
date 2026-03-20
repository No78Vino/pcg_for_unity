using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
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

        // ---- 内联默认值编辑相关 ----
        private Dictionary<string, VisualElement> _portWidgets = new Dictionary<string, VisualElement>();
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
            {
                _portDefaultValues[kvp.Key] = kvp.Value;
                // 更新 UI 控件的显示值
                UpdateWidgetValue(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 当端口连接状态变化时调用，显示/隐藏内联编辑器
        /// </summary>
        public void OnPortConnectionChanged(string portName, bool isConnected)
        {
            if (_portWidgets.TryGetValue(portName, out var widget))
            {
                widget.style.display = isConnected
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }

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

            // P2-2: 添加参数折叠按钮
            AddCollapseButton();
        }

        // P2-2: 参数折叠功能
        private bool _paramsExpanded = true;
        private Button _collapseButton;

        private void AddCollapseButton()
        {
            if (_collapseButton != null) return;

            _collapseButton = new Button(ToggleParams)
            {
                text = "▼",
                style =
                {
                    width = 16, height = 16,
                    fontSize = 10,
                    marginLeft = 4, marginRight = 0,
                    paddingLeft = 0, paddingRight = 0,
                    backgroundColor = new StyleColor(Color.clear),
                    color = new StyleColor(Color.white),
                }
            };
            _collapseButton.tooltip = "Collapse/Expand parameters";
            titleContainer.Insert(0, _collapseButton);
        }

        private void ToggleParams()
        {
            _paramsExpanded = !_paramsExpanded;
            _collapseButton.text = _paramsExpanded ? "▼" : "▶";

            // 切换非 Geometry 端口的可见性
            foreach (var kvp in _portWidgets)
            {
                kvp.Value.style.display = _paramsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
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

                port.portName = schema.DisplayName;
                port.portColor = GetPortColor(schema.PortType);

                // 迭代二：添加端口 Tooltip
                port.tooltip = schema.Description;

                inputPorts[schema.Name] = port;
                _inputSchemas[schema.Name] = schema;

                // 为非 Geometry 类型的端口创建内联编辑器
                if (schema.PortType != PCGPortType.Geometry && schema.PortType != PCGPortType.Any)
                {
                    var widget = CreateInlineWidget(schema);
                    if (widget != null)
                    {
                        port.Add(widget);
                        _portWidgets[schema.Name] = widget;
                    }
                }

                // 初始化默认值
                if (schema.DefaultValue != null)
                {
                    _portDefaultValues[schema.Name] = schema.DefaultValue;
                }

                inputContainer.Add(port);
            }
        }

        /// <summary>
        /// 根据端口类型创建对应的内联编辑控件
        /// </summary>
        private VisualElement CreateInlineWidget(PCGParamSchema schema)
        {
            VisualElement widget = null;

            // 迭代四：Enum/Dropdown 支持
            if (schema.EnumOptions != null && schema.EnumOptions.Length > 0)
            {
                var defaultIndex = 0;
                var defaultVal = schema.DefaultValue;

                if (defaultVal is int idx && idx >= 0 && idx < schema.EnumOptions.Length)
                {
                    defaultIndex = idx;
                }
                else if (defaultVal is string str)
                {
                    for (int i = 0; i < schema.EnumOptions.Length; i++)
                    {
                        if (schema.EnumOptions[i] == str)
                        {
                            defaultIndex = i;
                            break;
                        }
                    }
                }

                _portDefaultValues[schema.Name] = schema.EnumOptions[defaultIndex];

                var popup = new PopupField<string>(schema.EnumOptions.ToList(), defaultIndex)
                {
                    style =
                    {
                        width = 80,
                        marginLeft = 4,
                        fontSize = 10,
                    }
                };

                popup.RegisterValueChangedCallback(evt =>
                {
                    _portDefaultValues[schema.Name] = evt.newValue;
                });

                return popup;
            }

            switch (schema.PortType)
            {
                case PCGPortType.Float:
                {
                    var defaultVal = schema.DefaultValue is float f ? f : 0f;
                    _portDefaultValues[schema.Name] = defaultVal;

                    // 迭代二：当 Min/Max 有定义时使用 Slider
                    if (schema.Min != float.MinValue && schema.Max != float.MaxValue)
                    {
                        var slider = new Slider(schema.Min, schema.Max)
                        {
                            value = defaultVal,
                            style =
                            {
                                width = 80,
                                marginLeft = 4,
                            }
                        };

                        var label = new Label(defaultVal.ToString("F2"))
                        {
                            style =
                            {
                                fontSize = 9,
                                width = 40,
                                unityTextAlign = TextAnchor.MiddleRight,
                            }
                        };

                        slider.RegisterValueChangedCallback(evt =>
                        {
                            var val = evt.newValue;
                            _portDefaultValues[schema.Name] = val;
                            label.text = val.ToString("F2");
                        });

                        var container = new VisualElement
                        {
                            style =
                            {
                                flexDirection = FlexDirection.Row,
                                marginLeft = 4,
                            }
                        };
                        container.Add(slider);
                        container.Add(label);
                        widget = container;
                    }
                    else
                    {
                        var field = new FloatField()
                        {
                            value = defaultVal,
                            style =
                            {
                                width = 60,
                                marginLeft = 4,
                                fontSize = 10,
                            }
                        };
                        field.RegisterValueChangedCallback(evt =>
                        {
                            var val = evt.newValue;
                            // 应用 Min/Max 约束
                            if (schema.Min != float.MinValue && val < schema.Min) val = schema.Min;
                            if (schema.Max != float.MaxValue && val > schema.Max) val = schema.Max;
                            if (val != evt.newValue) field.SetValueWithoutNotify(val);
                            _portDefaultValues[schema.Name] = val;
                        });
                        widget = field;
                    }
                    break;
                }

                case PCGPortType.Int:
                {
                    var defaultVal = schema.DefaultValue is int i ? i : 0;
                    _portDefaultValues[schema.Name] = defaultVal;

                    // 迭代二：当 Min/Max 有定义时使用 Slider（整数）
                    if (schema.Min != float.MinValue && schema.Max != float.MaxValue)
                    {
                        var slider = new Slider(schema.Min, schema.Max)
                        {
                            value = defaultVal,
                            style =
                            {
                                width = 80,
                                marginLeft = 4,
                            }
                        };

                        var label = new Label(defaultVal.ToString())
                        {
                            style =
                            {
                                fontSize = 9,
                                width = 30,
                                unityTextAlign = TextAnchor.MiddleRight,
                            }
                        };

                        slider.RegisterValueChangedCallback(evt =>
                        {
                            var val = Mathf.RoundToInt(evt.newValue);
                            slider.SetValueWithoutNotify(val);
                            _portDefaultValues[schema.Name] = val;
                            label.text = val.ToString();
                        });

                        var container = new VisualElement
                        {
                            style =
                            {
                                flexDirection = FlexDirection.Row,
                                marginLeft = 4,
                            }
                        };
                        container.Add(slider);
                        container.Add(label);
                        widget = container;
                    }
                    else
                    {
                        var field = new IntegerField()
                        {
                            value = defaultVal,
                            style =
                            {
                                width = 60,
                                marginLeft = 4,
                                fontSize = 10,
                            }
                        };
                        field.RegisterValueChangedCallback(evt =>
                        {
                            var val = evt.newValue;
                            if (schema.Min != float.MinValue && val < (int)schema.Min) val = (int)schema.Min;
                            if (schema.Max != float.MaxValue && val > (int)schema.Max) val = (int)schema.Max;
                            if (val != evt.newValue) field.SetValueWithoutNotify(val);
                            _portDefaultValues[schema.Name] = val;
                        });
                        widget = field;
                    }
                    break;
                }

                case PCGPortType.Bool:
                {
                    var defaultVal = schema.DefaultValue is bool b && b;
                    _portDefaultValues[schema.Name] = defaultVal;

                    var field = new Toggle()
                    {
                        value = defaultVal,
                        style =
                        {
                            marginLeft = 4,
                        }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        _portDefaultValues[schema.Name] = evt.newValue;
                    });
                    widget = field;
                    break;
                }

                case PCGPortType.String:
                {
                    var defaultVal = schema.DefaultValue as string ?? "";
                    _portDefaultValues[schema.Name] = defaultVal;

                    var field = new TextField()
                    {
                        value = defaultVal,
                        style =
                        {
                            width = 80,
                            marginLeft = 4,
                            fontSize = 10,
                        }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        _portDefaultValues[schema.Name] = evt.newValue;
                    });
                    widget = field;
                    break;
                }

                case PCGPortType.Vector3:
                {
                    var defaultVal = schema.DefaultValue is Vector3 v ? v : Vector3.zero;
                    _portDefaultValues[schema.Name] = defaultVal;

                    // 迭代四：改为垂直排列，增大宽度
                    var container = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column,
                            marginLeft = 4,
                            marginTop = 2,
                            marginBottom = 2,
                        }
                    };

                    var fieldX = new FloatField("X") { value = defaultVal.x, style = { width = 70, fontSize = 9 } };
                    var fieldY = new FloatField("Y") { value = defaultVal.y, style = { width = 70, fontSize = 9 } };
                    var fieldZ = new FloatField("Z") { value = defaultVal.z, style = { width = 70, fontSize = 9 } };

                    // 设置标签样式
                    fieldX.labelElement.style.minWidth = 12;
                    fieldY.labelElement.style.minWidth = 12;
                    fieldZ.labelElement.style.minWidth = 12;

                    System.Action updateVector = () =>
                    {
                        _portDefaultValues[schema.Name] = new Vector3(fieldX.value, fieldY.value, fieldZ.value);
                    };

                    fieldX.RegisterValueChangedCallback(_ => updateVector());
                    fieldY.RegisterValueChangedCallback(_ => updateVector());
                    fieldZ.RegisterValueChangedCallback(_ => updateVector());

                    container.Add(fieldX);
                    container.Add(fieldY);
                    container.Add(fieldZ);
                    widget = container;
                    break;
                }

                case PCGPortType.Color:
                {
                    var defaultVal = schema.DefaultValue is Color c ? c : Color.white;
                    _portDefaultValues[schema.Name] = defaultVal;

                    var field = new ColorField()
                    {
                        value = defaultVal,
                        style =
                        {
                            width = 60,
                            marginLeft = 4,
                        }
                    };
                    field.RegisterValueChangedCallback(evt =>
                    {
                        _portDefaultValues[schema.Name] = evt.newValue;
                    });
                    widget = field;
                    break;
                }
            }

            return widget;
        }

        /// <summary>
        /// 更新指定端口的控件显示值（用于加载时恢复）
        /// </summary>
        private void UpdateWidgetValue(string portName, object value)
        {
            if (!_portWidgets.TryGetValue(portName, out var widget)) return;
            if (!_inputSchemas.TryGetValue(portName, out var schema)) return;

            switch (schema.PortType)
            {
                case PCGPortType.Float when widget is FloatField ff && value is float fv:
                    ff.SetValueWithoutNotify(fv);
                    break;
                // 迭代四修复：支持 Slider
                case PCGPortType.Float when schema.Min != float.MinValue && schema.Max != float.MaxValue && value is float fval:
                {
                    var slider = widget.Q<Slider>();
                    if (slider != null)
                    {
                        slider.SetValueWithoutNotify(fval);
                        var label = widget.Q<Label>();
                        if (label != null) label.text = fval.ToString("F2");
                    }
                    break;
                }
                case PCGPortType.Int when widget is IntegerField intF && value is int iv:
                    intF.SetValueWithoutNotify(iv);
                    break;
                // 迭代四修复：支持 Int Slider
                case PCGPortType.Int when schema.Min != float.MinValue && schema.Max != float.MaxValue && value is int ival:
                {
                    var slider = widget.Q<Slider>();
                    if (slider != null)
                    {
                        slider.SetValueWithoutNotify(ival);
                        var label = widget.Q<Label>();
                        if (label != null) label.text = ival.ToString();
                    }
                    break;
                }
                case PCGPortType.Bool when widget is Toggle toggle && value is bool bv:
                    toggle.SetValueWithoutNotify(bv);
                    break;
                case PCGPortType.String when widget is TextField tf && value is string sv:
                    tf.SetValueWithoutNotify(sv);
                    break;
                case PCGPortType.Color when widget is ColorField cf && value is Color cv:
                    cf.SetValueWithoutNotify(cv);
                    break;
                case PCGPortType.Vector3 when value is Vector3 vec:
                {
                    var fields = widget.Query<FloatField>().ToList();
                    if (fields.Count >= 3)
                    {
                        fields[0].SetValueWithoutNotify(vec.x);
                        fields[1].SetValueWithoutNotify(vec.y);
                        fields[2].SetValueWithoutNotify(vec.z);
                    }
                    break;
                }
                // 迭代四修复：Enum/Dropdown 支持
                case PCGPortType.String when schema.EnumOptions != null && widget is PopupField<string> popup && value is string enumVal:
                {
                    var index = System.Array.IndexOf(schema.EnumOptions, enumVal);
                    if (index >= 0)
                        popup.SetValueWithoutNotify(enumVal);
                    break;
                }
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