using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    public class PCGGeometrySpreadsheetWindow : EditorWindow
    {
        private enum SpreadsheetTab
        {
            Points,
            Vertices,
            Primitives,
            Detail
        }

        private PCGGeometry _geometry;
        private string _nodeDisplayName = "";
        private SpreadsheetTab _currentTab = SpreadsheetTab.Points;
        private Vector2 _scrollPos = Vector2.zero;
        private string _filterText = "";
        private int _filterFrom = -1;
        private int _filterTo = -1;
        private string _filterGroup = "";
        private int _sortColumn = -1;
        private bool _sortAscending = true;

        private const float ROW_HEIGHT = 18f;
        private const float HEADER_HEIGHT = 24f;

        private List<ColumnDef> _columns = new List<ColumnDef>();
        private int[] _filteredIndices;
        private List<int> _vertexMap;

        private class ColumnDef
        {
            public string Name;
            public float Width;
            public Func<int, string> GetValue;

            public ColumnDef(string name, float width, Func<int, string> getValueFn)
            {
                Name = name;
                Width = width;
                GetValue = getValueFn ?? (i => "-");
            }
        }

        [MenuItem("PCG Toolkit/Geometry Spreadsheet")]
        public static PCGGeometrySpreadsheetWindow Open()
        {
            var window = GetWindow<PCGGeometrySpreadsheetWindow>();
            window.titleContent = new GUIContent("Geometry Spreadsheet");
            window.minSize = new Vector2(600, 400);
            return window;
        }

        public void SetGeometry(PCGGeometry geometry, string nodeDisplayName)
        {
            _geometry = geometry;
            _nodeDisplayName = nodeDisplayName ?? "";
            _filterText = "";
            _filterFrom = -1;
            _filterTo = -1;
            _filterGroup = "";
            _sortColumn = -1;
            _vertexMap = BuildVertexMap();
            RebuildColumns();
            RebuildFilteredIndices();
            Repaint();
        }

        private List<int> BuildVertexMap()
        {
            var map = new List<int>();
            if (_geometry == null) return map;
            for (int pi = 0; pi < _geometry.Primitives.Count; pi++)
            {
                for (int vi = 0; vi < _geometry.Primitives[pi].Length; vi++)
                {
                    map.Add(pi * 10000 + vi);
                }
            }

            return map;
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (_geometry == null)
            {
                DrawEmptyState();
                return;
            }

            DrawTabs();
            RebuildColumns();
            RebuildFilteredIndices();
            DrawFilterBar();
            DrawTable();
            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Geometry Spreadsheet", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RebuildColumns();
                RebuildFilteredIndices();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No geometry loaded.",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    { alignment = TextAnchor.MiddleCenter, fontSize = 14 });
            EditorGUILayout.LabelField("Execute a node, then click 'Open Spreadsheet' in the Inspector.",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            GUILayout.FlexibleSpace();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var tab = GUILayout.Toggle(_currentTab == SpreadsheetTab.Points, "Points", EditorStyles.toolbarButton);
            if (tab) _currentTab = SpreadsheetTab.Points;
            tab = GUILayout.Toggle(_currentTab == SpreadsheetTab.Vertices, "Vertices", EditorStyles.toolbarButton);
            if (tab) _currentTab = SpreadsheetTab.Vertices;
            tab = GUILayout.Toggle(_currentTab == SpreadsheetTab.Primitives, "Primitives", EditorStyles.toolbarButton);
            if (tab) _currentTab = SpreadsheetTab.Primitives;
            tab = GUILayout.Toggle(_currentTab == SpreadsheetTab.Detail, "Detail", EditorStyles.toolbarButton);
            if (tab) _currentTab = SpreadsheetTab.Detail;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter:", GUILayout.Width(40));
            _filterText = GUILayout.TextField(_filterText, EditorStyles.toolbarSearchField, GUILayout.Width(120));

            GUILayout.Label("From:", GUILayout.Width(35));
            _filterFrom = EditorGUILayout.IntField(_filterFrom, EditorStyles.toolbarSearchField, GUILayout.Width(50));
            GUILayout.Label("To:", GUILayout.Width(20));
            _filterTo = EditorGUILayout.IntField(_filterTo, EditorStyles.toolbarSearchField, GUILayout.Width(50));

            string[] groups = null;
            if (_currentTab == SpreadsheetTab.Points)
                groups = _geometry.PointGroups.Keys.ToArray();
            else if (_currentTab == SpreadsheetTab.Primitives)
                groups = _geometry.PrimGroups.Keys.ToArray();

            if (groups != null && groups.Length > 0)
            {
                GUILayout.Label("Group:", GUILayout.Width(45));
                var list = new List<string> { "" };
                list.AddRange(groups);
                int idx = list.IndexOf(_filterGroup);
                if (idx < 0) idx = 0;
                idx = EditorGUILayout.Popup(idx, list.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(100));
                _filterGroup = idx > 0 ? list[idx] : "";
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _filterText = "";
                _filterFrom = -1;
                _filterTo = -1;
                _filterGroup = "";
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTable()
        {
            if (_filteredIndices == null || _filteredIndices.Length == 0)
            {
                EditorGUILayout.LabelField("No matching rows.");
                return;
            }

            float totalW = 0;
            foreach (var c in _columns) totalW += c.Width;

            EditorGUILayout.BeginHorizontal();
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            DrawHeader(totalW);

            float viewH = position.height - 120;
            int first = Mathf.Max(0, Mathf.FloorToInt(_scrollPos.y / ROW_HEIGHT));
            int last = Mathf.Min(_filteredIndices.Length, Mathf.CeilToInt((_scrollPos.y + viewH) / ROW_HEIGHT));

            for (int row = first; row < last; row++)
            {
                int dataIdx = _filteredIndices[row];
                bool even = (row % 2) == 0;
                Color txtCol = even ? new Color(0.85f, 0.85f, 0.85f) : Color.white;
                DrawRow(dataIdx, txtCol);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader(float totalWidth)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(totalWidth), GUILayout.Height(HEADER_HEIGHT));
            float xPos = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                Rect rect = new Rect(xPos, 0, col.Width, HEADER_HEIGHT);
                string lbl = col.Name + (_sortColumn == i ? (_sortAscending ? " ▲" : " ▼") : "");

                GUI.backgroundColor = _sortColumn == i
                    ? new Color(0.3f, 0.4f, 0.5f)
                    : new Color(0.25f, 0.25f, 0.25f);

                if (GUI.Button(rect, lbl, EditorStyles.toolbarButton))
                {
                    if (_sortColumn == i)
                        _sortAscending = !_sortAscending;
                    else
                    {
                        _sortColumn = i;
                        _sortAscending = true;
                    }

                    RebuildFilteredIndices();
                }

                GUI.backgroundColor = Color.white;
                xPos += col.Width;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawRow(int dataIdx, Color txtCol)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(ROW_HEIGHT));
            float xPos = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                Rect rect = new Rect(xPos, 0, col.Width, ROW_HEIGHT);
                string val = col.GetValue(dataIdx);
                GUI.Label(rect, val, new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    normal = { textColor = txtCol }
                });
                xPos += col.Width;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            int total = GetTotalRows();
            int filtered = _filteredIndices != null ? _filteredIndices.Length : 0;
            GUILayout.Label($"Showing {filtered} / {total} rows | Node: {_nodeDisplayName}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void RebuildColumns()
        {
            _columns.Clear();
            if (_geometry == null) return;

            switch (_currentTab)
            {
                case SpreadsheetTab.Points:
                    RebuildPointsColumns();
                    break;
                case SpreadsheetTab.Vertices:
                    RebuildVerticesColumns();
                    break;
                case SpreadsheetTab.Primitives:
                    RebuildPrimitivesColumns();
                    break;
                case SpreadsheetTab.Detail:
                    RebuildDetailColumns();
                    break;
            }
        }

        private void RebuildPointsColumns()
        {
            var geo = _geometry;
            _columns.Add(new ColumnDef("#", 50, i => i.ToString()));
            _columns.Add(new ColumnDef("P.x", 70, i => geo.Points[i].x.ToString("F4")));
            _columns.Add(new ColumnDef("P.y", 70, i => geo.Points[i].y.ToString("F4")));
            _columns.Add(new ColumnDef("P.z", 70, i => geo.Points[i].z.ToString("F4")));

            foreach (var attr in geo.PointAttribs.GetAllAttributes())
                AddAttribColumns(attr, i => i < attr.Values.Count ? attr.Values[i] : null);

            foreach (var kvp in geo.PointGroups)
            {
                var grp = kvp.Value;
                _columns.Add(new ColumnDef(kvp.Key, 30, i => grp.Contains(i) ? "1" : "0"));
            }
        }

        private void RebuildVerticesColumns()
        {
            var geo = _geometry;
            _columns.Add(new ColumnDef("#", 50, i => i.ToString()));

            if (_vertexMap != null && _filteredIndices != null)
            {
                int idx = _filteredIndices.Length > 0 ? _filteredIndices[0] : 0;
                // 使用全局索引计算
                int enc = 0;
                int pi = 0, vi = 0, ptIdx = -1;
                if (idx < _vertexMap.Count)
                {
                    enc = _vertexMap[idx];
                    pi = enc / 10000;
                    vi = enc % 10000;
                    if (pi < geo.Primitives.Count && vi < geo.Primitives[pi].Length)
                        ptIdx = geo.Primitives[pi][vi];
                }

                int primIdx = pi, vtxIn = vi;
                _columns.Add(new ColumnDef("Prim#", 60, i => primIdx.ToString()));
                _columns.Add(new ColumnDef("VtxInPrim", 70, i => vtxIn.ToString()));
                _columns.Add(new ColumnDef("PointIdx", 70, i => ptIdx >= 0 ? ptIdx.ToString() : "-"));
            }
            else
            {
                _columns.Add(new ColumnDef("Prim#", 60, i => "-"));
                _columns.Add(new ColumnDef("VtxInPrim", 70, i => "-"));
                _columns.Add(new ColumnDef("PointIdx", 70, i => "-"));
            }

            foreach (var attr in geo.VertexAttribs.GetAllAttributes())
                AddAttribColumns(attr, i => i < attr.Values.Count ? attr.Values[i] : null);
        }

        private void RebuildPrimitivesColumns()
        {
            var geo = _geometry;
            _columns.Add(new ColumnDef("#", 50, i => i.ToString()));
            _columns.Add(new ColumnDef("Verts", 50, i => geo.Primitives[i].Length.ToString()));
            _columns.Add(new ColumnDef("PointIndices", 180, i => IndexArrayToString(geo.Primitives[i])));

            foreach (var attr in geo.PrimAttribs.GetAllAttributes())
                AddAttribColumns(attr, i => i < attr.Values.Count ? attr.Values[i] : null);

            foreach (var kvp in geo.PrimGroups)
            {
                var grp = kvp.Value;
                _columns.Add(new ColumnDef(kvp.Key, 30, i => grp.Contains(i) ? "1" : "0"));
            }
        }

        private void RebuildDetailColumns()
        {
            var attrs = new List<PCGAttribute>(_geometry.DetailAttribs.GetAllAttributes());
            _columns.Add(new ColumnDef("Attribute", 120, i => i < attrs.Count ? attrs[i].Name : "-"));
            _columns.Add(new ColumnDef("Type", 80, i => i < attrs.Count ? attrs[i].Type.ToString() : "-"));
            _columns.Add(new ColumnDef("Value", 250, i =>
            {
                if (i >= attrs.Count) return "-";
                var attr = attrs[i];
                return attr.Values.Count > 0 ? FormatValue(attr.Values[0]) : "N/A";
            }));
        }

        private void AddAttribColumns(PCGAttribute attr, Func<int, object> getVal)
        {
            switch (attr.Type)
            {
                case AttribType.Float:
                case AttribType.Int:
                case AttribType.String:
                    _columns.Add(new ColumnDef(attr.Name, 100, i => FormatValue(getVal(i))));
                    break;
                case AttribType.Vector2:
                    _columns.Add(new ColumnDef(attr.Name + ".x", 60, i => FormatComponent(getVal(i), 0)));
                    _columns.Add(new ColumnDef(attr.Name + ".y", 60, i => FormatComponent(getVal(i), 1)));
                    break;
                case AttribType.Vector3:
                    _columns.Add(new ColumnDef(attr.Name + ".x", 60, i => FormatComponent(getVal(i), 0)));
                    _columns.Add(new ColumnDef(attr.Name + ".y", 60, i => FormatComponent(getVal(i), 1)));
                    _columns.Add(new ColumnDef(attr.Name + ".z", 60, i => FormatComponent(getVal(i), 2)));
                    break;
                case AttribType.Vector4:
                    for (int s = 0; s < 4; s++)
                    {
                        int sub = s;
                        _columns.Add(new ColumnDef(attr.Name + ".xyzw"[s].ToString(), 50,
                            i => FormatComponent(getVal(i), sub)));
                    }

                    break;
                case AttribType.Color:
                    for (int s = 0; s < 4; s++)
                    {
                        int sub = s;
                        _columns.Add(new ColumnDef(attr.Name + ".rgba"[s].ToString(), 50,
                            i => FormatComponent(getVal(i), sub)));
                    }

                    break;
            }
        }

        private void RebuildFilteredIndices()
        {
            int total = GetTotalRows();
            if (total == 0)
            {
                _filteredIndices = Array.Empty<int>();
                return;
            }

            var candidates = new List<int>();
            for (int i = 0; i < total; i++)
            {
                if (_filterFrom >= 0 && i < _filterFrom) continue;
                if (_filterTo >= 0 && i > _filterTo) continue;

                if (!string.IsNullOrEmpty(_filterGroup))
                {
                    bool inGroup = false;
                    if (_currentTab == SpreadsheetTab.Points)
                        inGroup = _geometry.PointGroups.TryGetValue(_filterGroup, out var pg) && pg.Contains(i);
                    else if (_currentTab == SpreadsheetTab.Primitives)
                        inGroup = _geometry.PrimGroups.TryGetValue(_filterGroup, out var rg) && rg.Contains(i);
                    if (!inGroup) continue;
                }

                if (!string.IsNullOrEmpty(_filterText) && !MatchesFilter(i))
                    continue;

                candidates.Add(i);
            }

            if (_sortColumn >= 0 && _sortColumn < _columns.Count)
            {
                var col = _columns[_sortColumn];
                candidates.Sort((a, b) =>
                {
                    string va = col.GetValue(a) ?? "";
                    string vb = col.GetValue(b) ?? "";
                    int cmp = string.Compare(va, vb, StringComparison.Ordinal);
                    return _sortAscending ? cmp : -cmp;
                });
            }

            _filteredIndices = candidates.ToArray();
        }

        private int GetTotalRows()
        {
            if (_geometry == null) return 0;
            switch (_currentTab)
            {
                case SpreadsheetTab.Points: return _geometry.Points.Count;
                case SpreadsheetTab.Vertices: return _vertexMap != null ? _vertexMap.Count : 0;
                case SpreadsheetTab.Primitives: return _geometry.Primitives.Count;
                case SpreadsheetTab.Detail: return _geometry.DetailAttribs.GetAllAttributes().Count();
                default: return 0;
            }
        }

        private bool MatchesFilter(int idx)
        {
            string filter = _filterText.ToLower();
            switch (_currentTab)
            {
                case SpreadsheetTab.Points:
                    if (idx < _geometry.Points.Count)
                    {
                        var p = _geometry.Points[idx];
                        return p.x.ToString().Contains(filter) ||
                               p.y.ToString().Contains(filter) ||
                               p.z.ToString().Contains(filter);
                    }

                    break;
                case SpreadsheetTab.Primitives:
                    if (idx < _geometry.Primitives.Count)
                    {
                        foreach (int i in _geometry.Primitives[idx])
                            if (i.ToString().Contains(filter))
                                return true;
                    }

                    break;
            }

            return false;
        }

        private string FormatValue(object v)
        {
            if (v == null) return "-";
            if (v is float f) return f.ToString("F4");
            if (v is double d) return d.ToString("F4");
            if (v is int i) return i.ToString();
            if (v is Vector2 v2) return $"({v2.x:F3}, {v2.y:F3})";
            if (v is Vector3 v3) return $"({v3.x:F3}, {v3.y:F3}, {v3.z:F3})";
            if (v is Vector4 v4) return $"({v4.x:F3}, {v4.y:F3}, {v4.z:F3}, {v4.w:F3})";
            if (v is Color c) return $"({c.r:F3}, {c.g:F3}, {c.b:F3}, {c.a:F3})";
            return v.ToString();
        }

        private string FormatComponent(object v, int component)
        {
            if (v == null) return "-";
            float[] comp = null;
            if (v is Vector2 v2) comp = new float[] { v2.x, v2.y };
            else if (v is Vector3 v3) comp = new float[] { v3.x, v3.y, v3.z };
            else if (v is Vector4 v4) comp = new float[] { v4.x, v4.y, v4.z, v4.w };
            else if (v is Color c) comp = new float[] { c.r, c.g, c.b, c.a };
            if (comp != null && component < comp.Length) return comp[component].ToString("F3");
            return "-";
        }

        private string IndexArrayToString(int[] prim)
        {
            if (prim == null) return "-";
            var sb = new StringBuilder();
            for (int i = 0; i < Mathf.Min(prim.Length, 8); i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(prim[i]);
            }

            if (prim.Length > 8) sb.Append("...");
            return sb.ToString();
        }
    }
}
