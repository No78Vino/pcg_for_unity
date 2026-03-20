using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 断点工具栏控件
    /// </summary>
    public class PCGBreakpointToolbar : VisualElement
    {
        private Button _toggleBreakpointBtn;
        private Button _clearAllBtn;
        private Label _countLabel;
        private PCGGraphView _graphView;

        public PCGBreakpointToolbar(PCGGraphView graphView)
        {
            _graphView = graphView;

            style.flexDirection = FlexDirection.Row;
            style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 4;
            style.paddingBottom = 4;

            // 标题
            var title = new Label("Breakpoints")
            {
                style =
                {
                    color = new StyleColor(new Color(0.8f, 0.8f, 0.8f)),
                    marginRight = 8,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            Add(title);

            // 切换断点按钮
            _toggleBreakpointBtn = new Button(ToggleBreakpointOnSelection)
            {
                text = "Toggle",
                tooltip = "Toggle breakpoint on selected node"
            };
            Add(_toggleBreakpointBtn);

            // 清除所有断点按钮
            _clearAllBtn = new Button(ClearAllBreakpoints)
            {
                text = "Clear All",
                tooltip = "Clear all breakpoints"
            };
            Add(_clearAllBtn);

            // 断点计数
            _countLabel = new Label("0")
            {
                style =
                {
                    color = new StyleColor(new Color(0.9f, 0.7f, 0.3f)),
                    marginLeft = 8
                }
            };
            Add(_countLabel);

            // 监听断点变化
            PCGBreakpointManager.OnBreakpointToggled += OnBreakpointChanged;
        }

        private void ToggleBreakpointOnSelection()
        {
            var selected = _graphView.GetSelectedNodeVisual();
            if (selected != null)
            {
                PCGBreakpointManager.ToggleBreakpoint(selected.NodeId);
            }
        }

        private void ClearAllBreakpoints()
        {
            PCGBreakpointManager.ClearAll();
        }

        private void OnBreakpointChanged(string nodeId)
        {
            _countLabel.text = PCGBreakpointManager.GetAllBreakpoints().Count.ToString();
        }

        public void Destroy()
        {
            PCGBreakpointManager.OnBreakpointToggled -= OnBreakpointChanged;
        }
    }
}