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