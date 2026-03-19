using System.Collections.Generic;
using UnityEditor;
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
            
            var element = CreateErrorElement(entry);
            _scrollView.Add(element);
            
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
        public string NodeId;
        public string NodeName;
        public string Message;
        public bool IsWarning;
        
        public PCGErrorEntry(string nodeId, string nodeName, string message, bool isWarning)
        {
            NodeId = nodeId;
            NodeName = nodeName;
            Message = message;
            IsWarning = isWarning;
        }
    }
}