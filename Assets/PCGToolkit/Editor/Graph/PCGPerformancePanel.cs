using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    public class PCGPerformancePanel : VisualElement
    {
        private struct NodePerfEntry
        {
            public string NodeId;
            public string NodeType;
            public double ElapsedMs;
            public int OutputPoints;
            public int OutputPrims;
            public long EstimatedBytes;
        }

        // P2-T3: FlameChart 支持
        private enum ViewMode { Table, FlameChart }
        private ViewMode _viewMode = ViewMode.Table;
        private VisualElement _flameChartContainer;
        private Button _tableButton;
        private Button _flameButton;

        private List<NodePerfEntry> _entries = new List<NodePerfEntry>();
        private Label _summaryLabel;
        private VisualElement _listContainer;

        public PCGPerformancePanel()
        {
            style.borderTopWidth = 1;
            style.borderTopColor = new StyleColor(new Color(0.35f, 0.35f, 0.35f));
            style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f));
            style.maxHeight = 200;

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                }
            };

            var titleLabel = new Label("Performance")
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(Color.white),
                    flexGrow = 1,
                }
            };
            header.Add(titleLabel);

            // P2-T3: Table/FlameChart 切换按钮
            _tableButton = new Button(() => SwitchView(ViewMode.Table)) { text = "Table" };
            _tableButton.style.width = 45;
            _tableButton.style.height = 18;
            _tableButton.style.fontSize = 10;
            header.Add(_tableButton);

            _flameButton = new Button(() => SwitchView(ViewMode.FlameChart)) { text = "Flame" };
            _flameButton.style.width = 45;
            _flameButton.style.height = 18;
            _flameButton.style.fontSize = 10;
            header.Add(_flameButton);

            _summaryLabel = new Label("No data")
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(0.9f, 0.9f, 0.3f)),
                    unityTextAlign = TextAnchor.MiddleRight,
                }
            };
            header.Add(_summaryLabel);
            Add(header);

            // Column headers
            var colHeader = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 8,
                    paddingRight = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f)),
                }
            };
            colHeader.Add(MakeLabel("Node", 120, FontStyle.Bold));
            colHeader.Add(MakeLabel("Time", 70, FontStyle.Bold));
            colHeader.Add(MakeLabel("Points", 60, FontStyle.Bold));
            colHeader.Add(MakeLabel("Prims", 60, FontStyle.Bold));
            colHeader.Add(MakeLabel("Memory", 70, FontStyle.Bold));
            Add(colHeader);

            var scrollView = new ScrollView { style = { flexGrow = 1 } };
            _listContainer = new VisualElement();
            scrollView.Add(_listContainer);
            Add(scrollView);

            // P2-T3: 火焰图容器
            _flameChartContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.NoWrap,
                    height = 28,
                    paddingLeft = 8,
                    paddingRight = 8,
                    display = DisplayStyle.None,
                }
            };
            Add(_flameChartContainer);
        }

        public void CollectFromExecutor(PCGAsyncGraphExecutor asyncExecutor, PCGGraphData graphData)
        {
            _entries.Clear();
            _listContainer.Clear();

            if (graphData == null) return;

            double totalMs = 0;
            int totalPoints = 0;
            long totalBytes = 0;

            foreach (var nodeData in graphData.Nodes)
            {
                var entry = new NodePerfEntry
                {
                    NodeId = nodeData.NodeId,
                    NodeType = nodeData.NodeType,
                };

                var result = asyncExecutor.GetNodeResult(nodeData.NodeId);
                if (result != null)
                {
                    entry.ElapsedMs = result.ElapsedMs;
                    if (result.OutputGeometry != null)
                    {
                        entry.OutputPoints = result.OutputGeometry.Points.Count;
                        entry.OutputPrims = result.OutputGeometry.Primitives.Count;
                        entry.EstimatedBytes = EstimateMemory(result.OutputGeometry);
                    }
                }

                _entries.Add(entry);
                totalMs += entry.ElapsedMs;
                totalPoints += entry.OutputPoints;
                totalBytes += entry.EstimatedBytes;
            }

            _entries.Sort((a, b) => b.ElapsedMs.CompareTo(a.ElapsedMs));

            _summaryLabel.text = $"Total: {totalMs:F1} ms | {totalPoints:N0} pts | {FormatBytes(totalBytes)}";

            foreach (var entry in _entries)
            {
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        paddingLeft = 8,
                        paddingRight = 8,
                        paddingTop = 1,
                        paddingBottom = 1,
                    }
                };

                row.Add(MakeLabel(entry.NodeType, 120));

                var timeLabel = MakeLabel($"{entry.ElapsedMs:F2} ms", 70);
                if (entry.ElapsedMs > 50)
                    timeLabel.style.color = new StyleColor(new Color(1f, 0.3f, 0.3f));
                else if (entry.ElapsedMs > 10)
                    timeLabel.style.color = new StyleColor(new Color(1f, 0.8f, 0.2f));
                else
                    timeLabel.style.color = new StyleColor(new Color(0.6f, 0.9f, 0.6f));
                row.Add(timeLabel);

                row.Add(MakeLabel(entry.OutputPoints.ToString("N0"), 60));
                row.Add(MakeLabel(entry.OutputPrims.ToString("N0"), 60));
                row.Add(MakeLabel(FormatBytes(entry.EstimatedBytes), 70));

                _listContainer.Add(row);
            }

            // P2-T3: 如果当前是火焰图模式则自动刷新
            if (_viewMode == ViewMode.FlameChart) RenderFlameChart();
        }

        // P2-T3: 视图切换
        private void SwitchView(ViewMode mode)
        {
            _viewMode = mode;
            _listContainer.parent.style.display = mode == ViewMode.Table ? DisplayStyle.Flex : DisplayStyle.None;
            _flameChartContainer.style.display = mode == ViewMode.FlameChart ? DisplayStyle.Flex : DisplayStyle.None;
            if (mode == ViewMode.FlameChart) RenderFlameChart();
        }

        // P2-T3: 渲染火焰图
        private void RenderFlameChart()
        {
            _flameChartContainer.Clear();
            if (_entries.Count == 0) return;

            double totalMs = _entries.Sum(e => e.ElapsedMs);
            if (totalMs <= 0) totalMs = 1;

            foreach (var entry in _entries)
            {
                float widthPct = (float)(entry.ElapsedMs / totalMs) * 100f;
                if (widthPct < 0.5f) widthPct = 0.5f;

                var block = new VisualElement
                {
                    style =
                    {
                        width = new Length(widthPct, LengthUnit.Percent),
                        height = 24,
                        backgroundColor = GetHeatColor(entry.ElapsedMs),
                        borderRightWidth = 1,
                        borderRightColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)),
                    }
                };

                var label = new Label(entry.NodeType)
                {
                    style =
                    {
                        fontSize = 9,
                        overflow = Overflow.Hidden,
                        color = new StyleColor(Color.white),
                    }
                };
                block.Add(label);

                block.tooltip = $"{entry.NodeType}\n{entry.ElapsedMs:F2}ms\nPts: {entry.OutputPoints}\nPrims: {entry.OutputPrims}";
                _flameChartContainer.Add(block);
            }
        }

        private static StyleColor GetHeatColor(double elapsedMs)
        {
            if (elapsedMs > 50) return new StyleColor(new Color(0.8f, 0.2f, 0.2f, 0.9f));
            if (elapsedMs > 10) return new StyleColor(new Color(0.8f, 0.6f, 0.1f, 0.9f));
            return new StyleColor(new Color(0.2f, 0.6f, 0.3f, 0.9f));
        }

        private static long EstimateMemory(PCGGeometry geo)
        {
            long bytes = 0;
            bytes += geo.Points.Count * 12L + 64;

            foreach (var prim in geo.Primitives)
                bytes += 16 + prim.Length * 4L + 8;

            foreach (var edge in geo.Edges)
                bytes += 16 + edge.Length * 4L + 8;

            bytes += EstimateAttribStore(geo.PointAttribs, geo.Points.Count);
            bytes += EstimateAttribStore(geo.PrimAttribs, geo.Primitives.Count);
            bytes += EstimateAttribStore(geo.VertexAttribs, geo.Points.Count);

            return bytes;
        }

        private static long EstimateAttribStore(AttributeStore store, int elementCount)
        {
            long bytes = 0;
            int attribCount = store.GetAttributeNames()?.Count() ?? 0;
            bytes += attribCount * (40 + elementCount * 16L);
            return bytes;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private static Label MakeLabel(string text, int width, FontStyle fontStyle = FontStyle.Normal)
        {
            return new Label(text)
            {
                style =
                {
                    width = width,
                    fontSize = 10,
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)),
                    unityFontStyleAndWeight = fontStyle,
                }
            };
        }
    }
}
