```json
{
  "task_id": "MERGE_NODE_OVERHAUL",
  "title": "MergeNode 改造：单 allowMultiple 端口 → 5 个显式输入端口 + 修复合并逻辑",
  "priority": "critical",
  "created": "2026-03-25",
  "root_cause_analysis": {
    "bug_1_executor_overwrite": {
      "severity": "critical",
      "description": "PCGAsyncGraphExecutor.ExecuteNodeInternal() 收集输入时，多条边连到同一个 'input' 端口会在 Dictionary<string, PCGGeometry> 中互相覆盖，只保留最后一条边的 geometry。PCGGraphExecutor 虽有 inputPortCounts workaround（重命名为 input_1, input_2），但命名规则不稳定且两个执行器行为不一致。",
      "affected_files": [
        "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs (line 316-328)",
        "Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs (line 168-188)"
      ]
    },
    "bug_2_agent_port_validation": {
      "severity": "high",
      "description": "AgentServer.HandleConnectNodes() 校验 inputPort 是否存在于节点模板的 Inputs 声明中。单端口 allowMultiple 模式下，Agent 无法用 'input_1' 等动态名称连接第二个输入。",
      "affected_file": "Assets/PCGToolkit/Editor/Communication/AgentServer.cs (line 485-494)"
    },
    "solution": "参照 InstanceNode 的 instance0~instance7 模式，将 MergeNode 改为 5 个显式 Geometry 输入端口 input0~input4，每个 required:false、allowMultiple:false。Execute 方法按 input0→input4 顺序收集非空 geometry 进行合并。"
  },
  "reference_pattern": {
    "node": "InstanceNode",
    "file": "Assets/PCGToolkit/Editor/Nodes/Distribute/InstanceNode.cs",
    "description": "InstanceNode 使用 instance0~instance7 共 8 个显式 Geometry 输入端口，每个 required:false。MergeNode 应采用相同模式。"
  },
  "files_to_modify": [
    {
      "file": "Assets/PCGToolkit/Editor/Nodes/Create/MergeNode.cs",
      "changes": [
        {
          "change_id": "M1",
          "title": "替换 Inputs 定义：1 个 allowMultiple 端口 → 5 个显式端口",
          "location": "Inputs 属性 (line 18-22)",
          "current_code": "public override PCGParamSchema[] Inputs => new[]\n{\n    new PCGParamSchema(\"input\", PCGPortDirection.Input, PCGPortType.Geometry,\n        \"Input\", \"输入几何体（支持多输入）\", null, required: true, allowMultiple: true),\n};",
          "new_code": "public override PCGParamSchema[] Inputs => new[]\n{\n    new PCGParamSchema(\"input0\", PCGPortDirection.Input, PCGPortType.Geometry,\n        \"Input 0\", \"输入几何体 0\", null, required: false),\n    new PCGParamSchema(\"input1\", PCGPortDirection.Input, PCGPortType.Geometry,\n        \"Input 1\", \"输入几何体 1\", null, required: false),\n    new PCGParamSchema(\"input2\", PCGPortDirection.Input, PCGPortType.Geometry,\n        \"Input 2\", \"输入几何体 2\", null, required: false),\n    new PCGParamSchema(\"input3\", PCGPortDirection.Input, PCGPortType.Geometry,\n        \"Input 3\", \"输入几何体 3\", null, required: false),\n    new PCGParamSchema(\"input4\", PCGPortDirection.Input, PCGPortType.Geometry,\n        \"Input 4\", \"输入几何体 4\", null, required: false),\n};"
        },
        {
          "change_id": "M2",
          "title": "重写 Execute 方法：按 input0→input4 顺序收集并合并",
          "location": "Execute 方法 (line 30-101)",
          "description": "不再遍历 inputGeometries 的全部 kvp（顺序不确定），改为按固定顺序 input0→input4 逐个尝试获取 geometry。对每个非空 geometry 执行合并逻辑。合并逻辑本身保持不变（Points、Primitives、Edges、PointAttribs、PrimAttribs、VertexAttribs、DetailAttribs、PointGroups、PrimGroups）。",
          "new_execute_pseudocode": [
            "private static readonly string[] InputPortNames = { \"input0\", \"input1\", \"input2\", \"input3\", \"input4\" };",
            "",
            "// 按固定顺序收集非空 geometry",
            "var geoList = new List<PCGGeometry>();",
            "foreach (var portName in InputPortNames)",
            "{",
            "    if (inputGeometries.TryGetValue(portName, out var geo) && geo != null && geo.Points.Count > 0)",
            "        geoList.Add(geo);",
            "}",
            "",
            "if (geoList.Count == 0)",
            "{",
            "    ctx.LogWarning(\"Merge: 没有有效的输入几何体\");",
            "    return SingleOutput(\"geometry\", new PCGGeometry());",
            "}",
            "",
            "// 对 geoList 执行原有的合并逻辑（pointOffset/primOffset/vertexOffset 累加）",
            "// ... 保持现有 MergeOneGeometry 逻辑不变 ..."
          ]
        },
        {
          "change_id": "M3",
          "title": "提取 MergeOneGeometry 辅助方法（可选但推荐）",
          "description": "将单个 geometry 的合并逻辑提取为 private void MergeOneGeometry(PCGGeometry result, PCGGeometry geo, ref int pointOffset, ref int primOffset, ref int vertexOffset)，使 Execute 方法更清晰。合并逻辑包括：Points、Primitives（索引+pointOffset）、Edges（索引+pointOffset）、PointAttribs（MergeAttributes）、PrimAttribs（MergeAttributes）、VertexAttribs（MergeAttributes）、DetailAttribs（MergeDetailAttribs）、PointGroups（索引+pointOffset）、PrimGroups（索引+primOffset）。"
        },
        {
          "change_id": "M4",
          "title": "修复 PrimGroups 合并的索引计算 bug",
          "location": "line 87-93",
          "current_code": "result.PrimGroups[group.Key].Add(idx + result.Primitives.Count - geo.Primitives.Count);",
          "description": "当前使用 result.Primitives.Count - geo.Primitives.Count 计算 primOffset，但此时 geo.Primitives 已经被添加到 result 中了，所以这个计算是正确的。但如果改为 geoList 遍历方式，应直接使用 primOffset 变量（在添加 Primitives 之前记录的值），更清晰且不易出错。",
          "new_code": "result.PrimGroups[group.Key].Add(idx + primOffset);"
        }
      ]
    },
    {
      "file": "Assets/PCGToolkit/Editor/Tests/CreateNodeTests.cs",
      "changes": [
        {
          "change_id": "T1",
          "title": "更新 MergeNode_TwoBoxes_CombinesGeometry 测试",
          "location": "line 62-73",
          "description": "测试已经使用 input0/input1 作为 key，与新的端口名一致，无需修改。但应增加以下测试用例：",
          "new_tests": [
            {
              "name": "MergeNode_FiveInputs_CombinesAll",
              "description": "创建 5 个 Box，分别连接到 input0~input4，验证合并后 Points.Count == 5 * singleBox.Points.Count"
            },
            {
              "name": "MergeNode_SparseInputs_SkipsEmpty",
              "description": "只连接 input0 和 input3（跳过 1、2、4），验证合并后 Points.Count == 2 * singleBox.Points.Count"
            },
            {
              "name": "MergeNode_SingleInput_PassThrough",
              "description": "只连接 input0，验证输出与输入完全一致"
            },
            {
              "name": "MergeNode_AttributePreservation",
              "description": "两个 Box 分别设置不同的 PointAttribs Cd，Merge 后验证 Cd.Values.Count == Points.Count"
            },
            {
              "name": "MergeNode_AsymmetricAttributes",
              "description": "Box A 有 Cd 属性，Box B 没有。Merge 后 Cd.Values.Count == 总点数，Box B 的点使用 DefaultValue"
            }
          ]
        }
      ]
    }
  ],
  "files_no_change_needed": [
    {
      "file": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",
      "reason": "5 个显式端口名各不相同，不会在 Dictionary 中发生 key 冲突，无需修改执行器的输入收集逻辑。"
    },
    {
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs",
      "reason": "同上。inputPortCounts workaround 不再被触发，但保留不影响功能。"
    },
    {
      "file": "Assets/PCGToolkit/Editor/Communication/AgentServer.cs",
      "reason": "Agent 连接时指定 input_port='input0'~'input4'，端口验证自然通过。"
    },
    {
      "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeVisual.cs",
      "reason": "CreateInputPorts() 遍历 Inputs 数组为每个 schema 创建端口，5 个显式端口会自动生成 5 个 UI 端口。"
    }
  ],
  "agent_api_usage_change": {
    "description": "AI Agent 通过 connect_nodes 连接 MergeNode 时，input_port 参数从 'input' 改为 'input0'~'input4'",
    "before": {
      "action": "connect_nodes",
      "output_node_id": "box_1",
      "output_port": "geometry",
      "input_node_id": "merge_1",
      "input_port": "input"
    },
    "after_examples": [
      {
        "action": "connect_nodes",
        "output_node_id": "box_1",
        "output_port": "geometry",
        "input_node_id": "merge_1",
        "input_port": "input0"
      },
      {
        "action": "connect_nodes",
        "output_node_id": "sphere_1",
        "output_port": "geometry",
        "input_node_id": "merge_1",
        "input_port": "input1"
      }
    ]
  },
  "validation_checklist": [
    "Box + Sphere → Merge → 输出 Points.Count == Box点数 + Sphere点数",
    "Box + Sphere → Merge → 输出 Primitives.Count == Box面数 + Sphere面数",
    "5 个不同几何体 → Merge → 全部合并，无丢失",
    "只连 input0 和 input3 → Merge → 正确合并两个，跳过空端口",
    "带 PointAttribs 的几何体 → Merge → 属性 Values.Count == Points.Count",
    "带 PrimGroups 的几何体 → Merge → 分组索引正确偏移",
    "PCGAsyncGraphExecutor 执行 Merge 图 → 结果与 PCGGraphExecutor 一致",
    "AgentServer connect_nodes input_port='input0' → 成功",
    "AgentServer connect_nodes input_port='input' → 返回端口不存在错误（预期行为）"
  ],
  "documentation_updates": [
    {
      "file": "Assets/PCGToolkit/HandBook.md",
      "change": "更新 MergeNode 说明：'合并多个几何体（最多5个输入）'"
    },
    {
      "file": "README.md",
      "change": "Merge 节点说明更新为 '合并最多 5 个 PCGGeometry'"
    }
  ]
}
```



```json
{
  "task": {
    "id": "pcg-geometry-spreadsheet-window",
    "title": "实现 Houdini 风格 Geometry Spreadsheet 独立窗口",
    "description": "在 PCG Toolkit 中创建一个独立的 EditorWindow，以表格形式展示 PCGGeometry 的所有数据（Points、Vertices、Primitives、Detail），支持虚拟滚动、筛选过滤、列排序，对标 Houdini Geometry Spreadsheet。",
    "repository": "No78Vino/pcg_for_unity",
    "branch": "main",
    "priority": "high",
    "language": "C#",
    "framework": "Unity Editor (IMGUI + UIElements)"
  },

  "context": {
    "data_model": {
      "PCGGeometry": {
        "file": "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",
        "description": "核心几何数据结构，所有 PCG 节点之间传递的核心数据类型",
        "fields": {
          "Points": "List<Vector3> — 顶点位置列表",
          "Primitives": "List<int[]> — 面（三角形/四边形/多边形），每个元素是该面的顶点索引数组",
          "Edges": "List<int[]> — 边，每个元素是 [startIndex, endIndex]",
          "PointAttribs": "AttributeStore — Point 层级属性",
          "VertexAttribs": "AttributeStore — Vertex 层级属性",
          "PrimAttribs": "AttributeStore — Primitive 层级属性",
          "DetailAttribs": "AttributeStore — Detail 层级属性（全局，只有1行）",
          "PointGroups": "Dictionary<string, HashSet<int>> — Point 分组",
          "PrimGroups": "Dictionary<string, HashSet<int>> — Primitive 分组"
        }
      },
      "AttributeStore": {
        "file": "Assets/PCGToolkit/Editor/Core/AttributeStore.cs",
        "description": "属性存储器，管理某一层级的所有属性",
        "key_methods": [
          "GetAllAttributes() -> IEnumerable<PCGAttribute>",
          "GetAttribute(string name) -> PCGAttribute",
          "GetAttributeNames() -> IEnumerable<string>"
        ]
      },
      "PCGAttribute": {
        "file": "Assets/PCGToolkit/Editor/Core/AttributeStore.cs",
        "lines": "35-58",
        "fields": {
          "Name": "string — 属性名称（如 'N', 'Cd', 'uv'）",
          "Type": "AttribType — 枚举: Float, Int, Vector2, Vector3, Vector4, Color, String",
          "DefaultValue": "object — 默认值",
          "Values": "List<object> — 每个元素对应一行的属性值"
        }
      },
      "AttribType": {
        "file": "Assets/PCGToolkit/Editor/Core/AttributeStore.cs",
        "lines": "10-19",
        "values": ["Float", "Int", "Vector2", "Vector3", "Vector4", "Color", "String"]
      }
    },
    "existing_panels": {
      "inspector_panel": {
        "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
        "method": "BuildGeometryDebugPanel (line 113-168)",
        "description": "当前的 Geometry Debug 面板，基于 UIElements Foldout，只显示拓扑计数和属性名/类型/值数量，不显示逐行数据"
      },
      "preview_panel": {
        "file": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
        "method": "DrawGeometryDebugPanel (line 237-295)",
        "description": "Preview 窗口中的 IMGUI 版 Geometry Debug 面板，同样只是总览"
      }
    },
    "editor_window_pattern": {
      "namespace": "PCGToolkit.Graph",
      "menu_path_convention": "PCG Toolkit/...",
      "existing_windows": [
        "PCGGraphEditorWindow — [MenuItem('PCG Toolkit/Node Editor')]",
        "PCGNodeInspectorWindow — [MenuItem('PCG Toolkit/Node Inspector')]",
        "PCGNodePreviewWindow — 无 MenuItem，通过代码 Open()"
      ]
    },
    "geometry_data_flow": {
      "description": "执行完成后，PCGAsyncGraphExecutor 通过 OnNodeCompleted 回调传递 NodeExecutionResult，其中包含 OutputGeometry。Inspector 通过 UpdateExecutionInfo(elapsedMs, geometry) 接收。Preview 通过 SetPreviewData(nodeId, displayName, geometry, executionTimeMs) 接收。",
      "inspector_receives_geometry": "PCGNodeInspectorWindow.UpdateExecutionInfo() at line 97-108",
      "preview_receives_geometry": "PCGNodePreviewWindow.SetPreviewData() at line 39-57",
      "graph_editor_dispatches": "PCGGraphEditorWindow.cs line 216-227 (OnNodeCompleted callback)"
    }
  },

  "subtasks": [
    {
      "id": "1-create-spreadsheet-window",
      "title": "创建 PCGGeometrySpreadsheetWindow.cs",
      "type": "create_file",
      "file_path": "Assets/PCGToolkit/Editor/Graph/PCGGeometrySpreadsheetWindow.cs",
      "description": "创建独立的 EditorWindow 类，使用 IMGUI (OnGUI) 渲染表格",
      "requirements": {
        "namespace": "PCGToolkit.Graph",
        "class_name": "PCGGeometrySpreadsheetWindow",
        "base_class": "EditorWindow",
        "menu_item": "[MenuItem(\"PCG Toolkit/Geometry Spreadsheet\")]",
        "window_title": "Geometry Spreadsheet",
        "min_size": { "width": 600, "height": 400 }
      },
      "implementation_details": {
        "public_api": {
          "Open": {
            "signature": "public static PCGGeometrySpreadsheetWindow Open()",
            "description": "打开或获取窗口实例"
          },
          "SetGeometry": {
            "signature": "public void SetGeometry(PCGGeometry geometry, string nodeDisplayName)",
            "description": "设置要展示的 Geometry 数据，触发列/行缓存重建"
          }
        },
        "tab_system": {
          "enum_name": "SpreadsheetTab",
          "values": ["Points", "Vertices", "Primitives", "Detail"],
          "rendering": "使用 GUILayout.Toolbar 在窗口顶部渲染 Tab 切换栏",
          "state_field": "private SpreadsheetTab _currentTab = SpreadsheetTab.Points"
        },
        "column_definitions": {
          "Points_tab": {
            "fixed_columns": [
              { "name": "#", "width": 50, "description": "行索引 (0-based)" },
              { "name": "P.x", "width": 70, "source": "geo.Points[i].x" },
              { "name": "P.y", "width": 70, "source": "geo.Points[i].y" },
              { "name": "P.z", "width": 70, "source": "geo.Points[i].z" }
            ],
            "dynamic_columns": "遍历 geo.PointAttribs.GetAllAttributes()，根据 AttribType 展开子列",
            "group_columns": "遍历 geo.PointGroups，每个 group 一个 bool 列（该 point index 是否在 group 中）"
          },
          "Vertices_tab": {
            "fixed_columns": [
              { "name": "#", "width": 50, "description": "全局 vertex 索引 (跨所有 prim 顺序编号)" },
              { "name": "Prim#", "width": 60, "description": "所属 primitive 索引" },
              { "name": "VtxInPrim", "width": 70, "description": "在 primitive 内的 vertex 索引" },
              { "name": "PointIdx", "width": 70, "description": "引用的 point 索引 = geo.Primitives[primIdx][vtxInPrim]" }
            ],
            "dynamic_columns": "遍历 geo.VertexAttribs.GetAllAttributes()"
          },
          "Primitives_tab": {
            "fixed_columns": [
              { "name": "#", "width": 50, "description": "prim 索引" },
              { "name": "Vertices", "width": 60, "description": "该 prim 的顶点数 = prim.Length" },
              { "name": "PointIndices", "width": 120, "description": "顶点索引数组的截断字符串，如 '0,1,2,3'" }
            ],
            "dynamic_columns": "遍历 geo.PrimAttribs.GetAllAttributes()",
            "group_columns": "遍历 geo.PrimGroups"
          },
          "Detail_tab": {
            "description": "只有1行数据，每个 DetailAttrib 作为一列",
            "columns": [
              { "name": "Attribute", "width": 120, "description": "属性名" },
              { "name": "Type", "width": 80, "description": "属性类型" },
              { "name": "Value", "width": 200, "description": "属性值" }
            ]
          },
          "type_expansion_rules": {
            "Float": "1 列",
            "Int": "1 列",
            "String": "1 列 (width=120)",
            "Vector2": "2 子列 (.x, .y)",
            "Vector3": "3 子列 (.x, .y, .z)",
            "Vector4": "4 子列 (.x, .y, .z, .w)",
            "Color": "4 子列 (.r, .g, .b, .a)"
          }
        },
        "virtual_scrolling": {
          "description": "核心性能优化：只渲染可见行，支持万级数据量",
          "constants": {
            "ROW_HEIGHT": 18,
            "HEADER_HEIGHT": 22
          },
          "algorithm": [
            "1. 计算 totalContentHeight = filteredRowCount * ROW_HEIGHT",
            "2. 使用 EditorGUILayout.BeginScrollView 创建滚动区域",
            "3. 从 _scrollPos.y 计算 firstVisible = Floor(scrollY / ROW_HEIGHT)",
            "4. 从 viewHeight 计算 lastVisible = Ceil((scrollY + viewHeight) / ROW_HEIGHT)",
            "5. 在可见行之前放置 GUILayout.Space(firstVisible * ROW_HEIGHT) 占位",
            "6. 只绘制 [firstVisible, lastVisible] 范围内的行",
            "7. 在可见行之后放置剩余高度的 GUILayout.Space 占位",
            "8. EndScrollView"
          ],
          "cell_rendering": "使用 GUI.Label(rect, text) 配合固定 Rect 计算，不使用 GUILayout 系列（性能更好）"
        },
        "filtering": {
          "text_filter": {
            "description": "文本搜索框，过滤任意列值包含搜索字符串的行",
            "debounce_ms": 200,
            "field": "private string _filterText = \"\""
          },
          "row_range_filter": {
            "description": "行范围过滤，From/To IntField",
            "fields": ["private int _filterFrom = -1", "private int _filterTo = -1"]
          },
          "group_filter": {
            "description": "下拉选择 Group，只显示属于该 Group 的行",
            "applies_to": "Points tab -> PointGroups, Primitives tab -> PrimGroups",
            "field": "private string _filterGroup = \"\" (空字符串表示不过滤)"
          },
          "clear_button": "一键清除所有过滤条件",
          "implementation": {
            "data_structure": "private List<int> _filteredIndices — 通过所有过滤条件的行索引列表",
            "rebuild_trigger": "过滤条件变化时重建，虚拟滚动基于 _filteredIndices.Count 作为行数"
          }
        },
        "sorting": {
          "description": "点击列头排序（升序/降序切换）",
          "fields": [
            "private int _sortColumn = -1",
            "private bool _sortAscending = true"
          ],
          "implementation": "排序时构建 int[] _sortedIndices 映射数组，行查找通过该数组间接索引"
        },
        "row_styling": {
          "alternating_colors": {
            "even_row": "new Color(0.22f, 0.22f, 0.22f)",
            "odd_row": "new Color(0.25f, 0.25f, 0.25f)"
          },
          "filter_highlight": "匹配过滤文本的单元格高亮显示"
        },
        "value_formatting": {
          "Float": "{value:F4}",
          "Int": "{value}",
          "String": "{value}",
          "Vector2": "各子列分别显示 {v.x:F3}, {v.y:F3}",
          "Vector3": "各子列分别显示 {v.x:F3}, {v.y:F3}, {v.z:F3}",
          "Vector4": "各子列分别显示 {v.x:F3}, {v.y:F3}, {v.z:F3}, {v.w:F3}",
          "Color": "各子列分别显示 {c.r:F3}, {c.g:F3}, {c.b:F3}, {c.a:F3}"
        },
        "status_bar": {
          "position": "窗口底部",
          "content": "\"Showing {filteredCount} / {totalCount} rows | Node: {nodeDisplayName}\""
        },
        "null_handling": "geometry 为 null 时显示居中文本 'No geometry loaded. Select a node and execute the graph.'"
      }
    },
    {
      "id": "2-integrate-inspector-button",
      "title": "在 Inspector 面板添加 Open Spreadsheet 按钮",
      "type": "modify_file",
      "file_path": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
      "description": "在 BuildGeometryDebugPanel 方法中，Topology foldout 之后添加一个按钮，点击打开 Spreadsheet 窗口并传入当前 geometry",
      "modification": {
        "location": "BuildGeometryDebugPanel 方法内，line 140 (_geometryDebugContainer.Add(topologyFoldout)) 之后",
        "code_to_add": "添加一个 UIElements Button，text='Open Spreadsheet'，点击时调用 PCGGeometrySpreadsheetWindow.Open() 并调用 SetGeometry(_lastGeometry, _currentNode?.PCGNode?.DisplayName ?? '')",
        "button_style": {
          "marginBottom": 4,
          "backgroundColor": "new Color(0.15f, 0.35f, 0.5f)"
        }
      }
    },
    {
      "id": "3-integrate-preview-button",
      "title": "在 Preview 窗口添加 Open Spreadsheet 按钮",
      "type": "modify_file",
      "file_path": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
      "description": "在 DrawGeometryDebugPanel 方法中，Topology 部分之后添加 IMGUI 按钮",
      "modification": {
        "location": "DrawGeometryDebugPanel 方法内，line 254 (EditorGUI.indentLevel--) 之后",
        "code_to_add": "添加 GUILayout.Button('Open Spreadsheet')，点击时调用 PCGGeometrySpreadsheetWindow.Open().SetGeometry(_geometry, _nodeDisplayName)"
      }
    },
    {
      "id": "4-integrate-toolbar-button",
      "title": "在 Graph Editor 工具栏添加 Spreadsheet 按钮",
      "type": "modify_file",
      "file_path": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
      "description": "在 GenerateToolbar 方法中添加一个 Spreadsheet 按钮，点击时打开 Spreadsheet 窗口并传入当前选中节点的 geometry",
      "modification": {
        "location": "GenerateToolbar 方法内，Inspector 按钮之后（约 line 367）",
        "code_to_add": "添加一个 Button，text='Spreadsheet'，点击时获取当前选中节点的 geometry（通过 _asyncExecutor.GetNodeResult），然后调用 PCGGeometrySpreadsheetWindow.Open().SetGeometry(geo, displayName)"
      }
    }
  ],

  "acceptance_criteria": [
    {
      "id": "AC-1",
      "description": "通过菜单 PCG Toolkit > Geometry Spreadsheet 可以打开独立窗口"
    },
    {
      "id": "AC-2",
      "description": "窗口有 4 个 Tab：Points, Vertices, Primitives, Detail，切换后表格内容正确更新"
    },
    {
      "id": "AC-3",
      "description": "Points Tab 正确显示所有 point 的索引、P.x/y/z、所有 PointAttrib 展开列、PointGroup 成员列"
    },
    {
      "id": "AC-4",
      "description": "Vertices Tab 正确显示全局 vertex 索引、所属 Prim#、VtxInPrim、PointIdx、所有 VertexAttrib 列"
    },
    {
      "id": "AC-5",
      "description": "Primitives Tab 正确显示 prim 索引、顶点数、顶点索引数组、所有 PrimAttrib 列、PrimGroup 列"
    },
    {
      "id": "AC-6",
      "description": "Detail Tab 正确显示所有 DetailAttrib 的 Name/Type/Value"
    },
    {
      "id": "AC-7",
      "description": "10000 行数据时窗口滚动流畅（虚拟滚动生效，只渲染可见行）"
    },
    {
      "id": "AC-8",
      "description": "文本过滤功能正常：输入关键字后只显示匹配行"
    },
    {
      "id": "AC-9",
      "description": "Group 过滤功能正常：选择 group 后只显示属于该 group 的行"
    },
    {
      "id": "AC-10",
      "description": "列头点击排序功能正常：升序/降序切换"
    },
    {
      "id": "AC-11",
      "description": "Inspector 面板和 Preview 窗口中的 Open Spreadsheet 按钮可以正确打开窗口并传入 geometry"
    },
    {
      "id": "AC-12",
      "description": "geometry 为 null 时窗口显示友好提示，不崩溃"
    },
    {
      "id": "AC-13",
      "description": "状态栏正确显示 'Showing X / Y rows' 信息"
    }
  ],

  "technical_constraints": [
    "使用 IMGUI (OnGUI) 而非 UIElements 渲染表格主体，因为 IMGUI 对大量动态行的性能更好",
    "使用 GUI.Label(Rect, string) 配合手动 Rect 计算绘制单元格，避免 GUILayout 的布局开销",
    "虚拟滚动必须实现：只绘制可见行，不能为每行创建 UI 元素",
    "文本过滤使用 debounce（约 200ms），避免每次按键都重建过滤索引",
    "窗口必须是独立的 EditorWindow，不嵌入其他面板",
    "所有新文件放在 Assets/PCGToolkit/Editor/Graph/ 目录下",
    "命名空间使用 PCGToolkit.Graph",
    "引用 PCGToolkit.Core 中的 PCGGeometry, AttributeStore, PCGAttribute, AttribType 等类型"
  ],

  "reference_files": [
    {
      "path": "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",
      "reason": "核心数据结构，表格数据源"
    },
    {
      "path": "Assets/PCGToolkit/Editor/Core/AttributeStore.cs",
      "reason": "属性系统，列定义来源"
    },
    {
      "path": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
      "reason": "需要修改以添加 Open Spreadsheet 按钮；参考现有 geometry 数据面板实现"
    },
    {
      "path": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
      "reason": "需要修改以添加 Open Spreadsheet 按钮；参考 IMGUI 面板模式"
    },
    {
      "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
      "reason": "需要修改以添加工具栏按钮；参考 geometry 数据流和窗口管理模式"
    }
  ]
}
```