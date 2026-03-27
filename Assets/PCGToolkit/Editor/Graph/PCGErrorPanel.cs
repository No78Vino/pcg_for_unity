using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 迭代三：编辑器内错误面板
    /// 显示节点执行错误和警告
    /// </summary>
    public class PCGErrorPanel : VisualElement
    {
        private ScrollView _scrollView;
        private List<PCGErrorEntry> _errors = new List<PCGErrorEntry>();

        // P2-T3: 过滤/搜索
        private bool _showWarnings = true;
        private bool _showErrors = true;
        private string _searchText = "";
        private ToolbarToggle _warningToggle;
        private ToolbarToggle _errorToggle;
        private TextField _searchField;
        
        public PCGErrorPanel()
        {
            style.height = 150;
            style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f));
            style.borderTopWidth = 1;
            style.borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
            
            // 标题栏
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 24,
                    backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f)),
                    paddingBottom = 2,
                    paddingTop = 2,
                    paddingLeft = 8,
                    paddingRight = 8,
                }
            };
            
            var titleLabel = new Label("Errors & Warnings")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.9f, 0.9f, 0.9f)),
                    flexGrow = 1,
                }
            };
            header.Add(titleLabel);

            // P2-T3: Warning 过滤 Toggle
            _warningToggle = new ToolbarToggle { text = "Warn", value = true };
            _warningToggle.style.width = 55;
            _warningToggle.RegisterValueChangedCallback(evt => { _showWarnings = evt.newValue; RefreshFilteredView(); });
            header.Add(_warningToggle);

            // P2-T3: Error 过滤 Toggle
            _errorToggle = new ToolbarToggle { text = "Error", value = true };
            _errorToggle.style.width = 55;
            _errorToggle.RegisterValueChangedCallback(evt => { _showErrors = evt.newValue; RefreshFilteredView(); });
            header.Add(_errorToggle);

            // P2-T3: 搜索框
            _searchField = new TextField { value = "" };
            _searchField.style.width = 120;
            _searchField.style.height = 18;
            _searchField.style.fontSize = 10;
            _searchField.RegisterValueChangedCallback(evt => { _searchText = evt.newValue; RefreshFilteredView(); });
            header.Add(_searchField);

            // 清除按钮
            var clearButton = new Button(() => ClearErrors())
            {
                text = "Clear",
                style =
                {
                    width = 60,
                    height = 18,
                    fontSize = 10,
                }
            };
            header.Add(clearButton);
            
            Add(header);
            
            // 滚动视图
            _scrollView = new ScrollView
            {
                style =
                {
                    flexGrow = 1,
                }
            };
            Add(_scrollView);
        }
        
        public void AddError(string nodeId, string nodeName, string message, bool isWarning = false)
        {
            var entry = new PCGErrorEntry(nodeId, nodeName, message, isWarning);
            _errors.Add(entry);
            RefreshFilteredView();
            style.display = DisplayStyle.Flex;
        }
        
        public void AddWarning(string nodeId, string nodeName, string message)
        {
            AddError(nodeId, nodeName, message, isWarning: true);
        }
        
        public void ClearErrors()
        {
            _errors.Clear();
            _scrollView.Clear();
            style.display = DisplayStyle.None;
        }

        // P2-T3: 刷新过滤后的视图
        private void RefreshFilteredView()
        {
            _scrollView.Clear();
            foreach (var entry in _errors)
            {
                // 过滤: Warning/Error Toggle
                if (entry.IsWarning && !_showWarnings) continue;
                if (!entry.IsWarning && !_showErrors) continue;

                // 过滤: 搜索文本
                if (!string.IsNullOrEmpty(_searchText))
                {
                    if (!entry.NodeName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) &&
                        !entry.Message.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                _scrollView.Add(CreateErrorElement(entry));
            }
        }
        
        public bool HasErrors => _errors.Exists(e => !e.IsWarning);
        public bool HasWarnings => _errors.Exists(e => e.IsWarning);
        public int ErrorCount => _errors.Count;
        
        private VisualElement CreateErrorElement(PCGErrorEntry entry)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 22,
                    paddingLeft = 8,
                    paddingRight = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f)),
                }
            };
            
            // 图标
            var iconColor = entry.IsWarning 
                ? new Color(0.9f, 0.7f, 0.2f) 
                : new Color(0.9f, 0.3f, 0.3f);
            var icon = new Label(entry.IsWarning ? "⚠" : "✖")
            {
                style =
                {
                    color = new StyleColor(iconColor),
                    width = 20,
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            container.Add(icon);
            
            // 节点名称
            var nodeLabel = new Label(entry.NodeName)
            {
                style =
                {
                    color = new StyleColor(new Color(0.6f, 0.8f, 0.9f)),
                    width = 120,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    fontSize = 11,
                }
            };
            container.Add(nodeLabel);
            
            // 消息
            var messageLabel = new Label(entry.Message)
            {
                style =
                {
                    color = new StyleColor(new Color(0.85f, 0.85f, 0.85f)),
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    fontSize = 11,
                }
            };
            container.Add(messageLabel);
            
            // 点击高亮节点
            container.RegisterCallback<ClickEvent>(evt =>
            {
                // 触发事件让 GraphView 高亮对应节点
                OnErrorClicked?.Invoke(entry.NodeId);
            });
            
            return container;
        }
        
        public event System.Action<string> OnErrorClicked;
    }
    
    public class PCGErrorEntry
    {
        public enum ErrorLevel { Warning, Error, Fatal }

        public string NodeId;
        public string NodeName;
        public string Message;
        public bool IsWarning;
        public ErrorLevel Level;

        public PCGErrorEntry(string nodeId, string nodeName, string message, bool isWarning)
        {
            NodeId = nodeId;
            NodeName = nodeName;
            Message = message;
            IsWarning = isWarning;
            Level = isWarning ? ErrorLevel.Warning : ErrorLevel.Error;
        }
    }
}