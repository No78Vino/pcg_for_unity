以下是补全实施计划的 JSON 格式输出，包含 4 个待完成任务，每个任务有明确的文件路径、修改内容和验收标准：

```json
{
  "plan_title": "Scene 交互式选择工具补全计划",
  "repository": "No78Vino/pcg_for_unity",
  "base_ref": "main",
  "gap_summary": "基于 TASK_TODO.md 三阶段评估，当前完成度约 88%，剩余 4 项缺口需补全",
  "tasks": [
    {
      "id": "FIX-1",
      "title": "补全 PCGSelectionOverlay.CreateHorizontalToolbarContent",
      "priority": "medium",
      "phase": "B5",
      "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
      "current_state": "CreateHorizontalToolbarContent() 在第 79-83 行返回空的 OverlayToolbar，无任何工具栏元素",
      "target_state": "水平工具栏应包含 Face/Edge/Vertex 模式切换按钮，与 CreatePanelContent() 中的面板内容保持功能一致",
      "changes": [
        {
          "method": "CreateHorizontalToolbarContent()",
          "line_range": "79-83",
          "action": "在 OverlayToolbar 中添加 3 个 EditorToolbarToggle 或 EditorToolbarButton 元素，分别对应 Face/Edge/Vertex 模式切换",
          "implementation_notes": [
            "使用 UnityEditor.Toolbars.EditorToolbarButton 或 EditorToolbarToggle 创建按钮",
            "每个按钮点击时调用 PCGSelectionState.SetMode(PCGSelectMode.XXX)",
            "可选：添加一个 Clear 按钮调用 PCGSelectionState.Clear()",
            "参考 CreatePanelContent() 中第 25-36 行的模式按钮逻辑"
          ]
        }
      ],
      "acceptance_criteria": [
        "Overlay 在水平模式下显示 Face/Edge/Vertex 按钮",
        "点击按钮可正确切换选择模式",
        "当前激活模式的按钮有视觉区分"
      ]
    },
    {
      "id": "FIX-2",
      "title": "GrowSelection/ShrinkSelection 扩展支持 Edge 和 Vertex 模式",
      "priority": "medium",
      "phase": "C1",
      "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
      "current_state": "GrowSelection() 第 360-389 行和 ShrinkSelection() 第 391-423 行仅处理 PCGSelectMode.Face，Edge 和 Vertex 模式下调用无效果",
      "target_state": "三种模式均支持 Grow/Shrink 操作",
      "changes": [
        {
          "method": "GrowSelection()",
          "line_range": "360-389",
          "action": "在现有 Face 分支后添加 Edge 和 Vertex 分支",
          "implementation_notes": [
            "Vertex 模式 Grow：遍历所有 Primitives，如果某个 Primitive 包含已选中的顶点，则将该 Primitive 的所有顶点加入选择",
            "Edge 模式 Grow：遍历所有 Edges，如果某条边的一个端点属于已选中边的端点集合，则将该边加入选择",
            "需要从 _bridge.Geometry.Edges 和 _bridge.Geometry.Primitives 获取拓扑关系"
          ]
        },
        {
          "method": "ShrinkSelection()",
          "line_range": "391-423",
          "action": "在现有 Face 分支后添加 Edge 和 Vertex 分支",
          "implementation_notes": [
            "Vertex 模式 Shrink：移除那些存在未选中邻居顶点的边界顶点（通过共享 Primitive 判断邻接关系）",
            "Edge 模式 Shrink：移除那些端点连接了未选中边的边界边"
          ]
        }
      ],
      "acceptance_criteria": [
        "Vertex 模式下 Ctrl+Numpad+ 可扩展选择到相邻顶点",
        "Vertex 模式下 Ctrl+Numpad- 可收缩边界顶点",
        "Edge 模式下 Ctrl+Numpad+ 可扩展选择到相邻边",
        "Edge 模式下 Ctrl+Numpad- 可收缩边界边",
        "Face 模式行为不变"
      ]
    },
    {
      "id": "FIX-3",
      "title": "为 SelectByNormal 和 SelectByMaterialId 添加 UI 入口",
      "priority": "medium",
      "phase": "C2",
      "files": [
        "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
        "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs"
      ],
      "current_state": "SelectByNormal() 第 435-455 行和 SelectByMaterialId() 第 457-487 行在 PCGSelectionTool.cs 中已实现，但无任何 UI 按钮可触发这些方法",
      "target_state": "用户可通过 Overlay 面板或 Inspector 面板中的按钮触发按属性选择功能",
      "changes": [
        {
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
          "method": "CreatePanelContent()",
          "action": "在 actionRow 之后添加一个 'Advanced Selection' 折叠区域",
          "implementation_notes": [
            "添加 'Select Up Faces' 按钮：获取当前激活的 PCGSelectionTool 实例，调用 SelectByNormal(Vector3.up, 0.7f)",
            "添加 'Select by Material' 按钮：获取当前 hover 或最后选中的 prim index，调用 SelectByMaterialId(primIndex)",
            "添加 'Grow Selection' 和 'Shrink Selection' 按钮作为快捷键的 UI 替代",
            "获取 PCGSelectionTool 实例：UnityEditor.EditorTools.ToolManager.activeTool as PCGSelectionTool",
            "按钮仅在 PCGSelectionTool 激活时可用（否则 grayed out）"
          ]
        },
        {
          "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
          "method": "BuildSelectionToolSection()",
          "line_range": "389-413",
          "action": "在现有 foldout 中追加 SelectByNormal 和 SelectByMaterialId 的按钮",
          "implementation_notes": [
            "在 statsLabel 之后添加 'Select Up Faces' 按钮",
            "在其后添加 'Select Same Material' 按钮",
            "按钮逻辑与 Overlay 中相同"
          ]
        }
      ],
      "acceptance_criteria": [
        "Overlay 面板中出现 'Select Up Faces' 按钮，点击后选中所有法线朝上的面",
        "Overlay 面板中出现 'Select by Material' 按钮，点击后选中与当前选中面相同材质的所有面",
        "Inspector 面板中 SceneSelectionInputNode 的 Selection Tool 区域也有相同按钮",
        "PCGSelectionTool 未激活时按钮禁用"
      ]
    },
    {
      "id": "FIX-4",
      "title": "补全缺失的单元测试和集成测试",
      "priority": "high",
      "phase": "B7 + C4",
      "file": "Assets/PCGToolkit/Editor/Tests/SelectionTests.cs",
      "current_state": "现有 16 个测试覆盖 Phase A 基础功能和部分 Phase C（序列化、模式切换），缺少框选、Grow/Shrink、属性选择、下游节点集成测试",
      "target_state": "测试覆盖所有 TASK_TODO.md 中定义的测试用例",
      "changes": [
        {
          "action": "在 SelectionTests 类中追加以下测试方法",
          "new_tests": [
            {
              "name": "GrowSelection_Face_ExpandsToAdjacentFaces",
              "description": "使用 CreateQuadGeo() 创建 2x2 网格，选中 prim 0，调用 GrowSelection()，验证共享顶点的相邻面（prim 1, 2, 3）被加入选择",
              "notes": "需要创建 PCGSelectionTool 实例并调用 SetGeometry()，然后调用 GrowSelection()"
            },
            {
              "name": "ShrinkSelection_Face_RemovesBoundaryFaces",
              "description": "选中所有 8 个 prim，调用 ShrinkSelection()，验证边界面被移除，只保留内部面",
              "notes": "2x2 网格中所有面都是边界面，所以全部应被移除；可构造更大的 3x3 网格来测试"
            },
            {
              "name": "GrowSelection_Vertex_ExpandsToAdjacentVertices",
              "description": "切换到 Vertex 模式，选中中心顶点 4，调用 GrowSelection()，验证相邻顶点被加入选择",
              "notes": "依赖 FIX-2 完成后才能通过"
            },
            {
              "name": "GrowSelection_Edge_ExpandsToAdjacentEdges",
              "description": "切换到 Edge 模式，选中一条边，调用 GrowSelection()，验证共享端点的相邻边被加入选择",
              "notes": "依赖 FIX-2 完成后才能通过，需先调用 geo.BuildEdges()"
            },
            {
              "name": "SelectByNormal_SelectsUpwardFaces",
              "description": "创建包含朝上和朝侧面的几何体，调用 SelectByNormal(Vector3.up, 0.7f)，验证只有朝上的面被选中",
              "notes": "CreateQuadGeo() 所有面都朝上（Y 平面），可额外添加一个竖直面来验证过滤"
            },
            {
              "name": "SelectByMaterialId_SelectsSameMaterial",
              "description": "创建带 material PrimAttrib 的几何体，部分面标记为 'matA'，部分为 'matB'，调用 SelectByMaterialId(0)，验证所有 'matA' 面被选中",
              "notes": "需要手动设置 geo.PrimAttribs.CreateAttribute('material', AttribType.String) 并填充值"
            },
            {
              "name": "SceneSelectionInputNode_ConnectBlastNode_DeletesSelectedFaces",
              "description": "SceneSelectionInputNode 输出带 PrimGroups['selected'] 的几何体，连接 BlastNode(group='selected')，验证选中面被删除",
              "notes": "需要实例化 BlastNode 并传入 SceneSelectionInputNode 的输出"
            },
            {
              "name": "SceneSelectionInputNode_Persistence_RestoresAfterClear",
              "description": "选中若干面，执行 SceneSelectionInputNode 使其序列化选择数据，清空 SelectionState，再次执行节点，验证选择从序列化数据恢复",
              "notes": "验证 serializedSelection 参数的持久化逻辑"
            }
          ]
        }
      ],
      "acceptance_criteria": [
        "所有新增测试在 Unity Test Runner 中通过",
        "GrowSelection Face 模式测试验证相邻面扩展",
        "ShrinkSelection Face 模式测试验证边界面收缩",
        "SelectByNormal 测试验证法线过滤",
        "SelectByMaterialId 测试验证材质过滤",
        "BlastNode 集成测试验证下游节点正确消费 Group",
        "持久化测试验证序列化/反序列化往返"
      ]
    }
  ],
  "execution_order": [
    {
      "step": 1,
      "task_ids": ["FIX-1", "FIX-2"],
      "reason": "FIX-1 和 FIX-2 互不依赖，可并行执行"
    },
    {
      "step": 2,
      "task_ids": ["FIX-3"],
      "reason": "FIX-3 的 Overlay UI 需要调用 GrowSelection/ShrinkSelection，依赖 FIX-2 中 Edge/Vertex 模式的实现"
    },
    {
      "step": 3,
      "task_ids": ["FIX-4"],
      "reason": "测试需要在所有功能实现后编写，以确保测试覆盖完整功能"
    }
  ],
  "files_to_modify": [
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
    "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
    "Assets/PCGToolkit/Editor/Tests/SelectionTests.cs"
  ],
  "estimated_completion_after": "100% of TASK_TODO.md phases A/B/C"
}
```

---

### 关键说明

| 任务 | 涉及文件 | 核心改动 |
|------|----------|----------|
| FIX-1 | `PCGSelectionOverlay.cs` L79-83 | 填充空的 `CreateHorizontalToolbarContent()` |
| FIX-2 | `PCGSelectionTool.cs` L360-423 | `GrowSelection`/`ShrinkSelection` 增加 Edge/Vertex 分支 |
| FIX-3 | `PCGSelectionOverlay.cs` + `PCGNodeInspectorWindow.cs` L389-413 | 为 `SelectByNormal`/`SelectByMaterialId` 添加 UI 按钮 |
| FIX-4 | `SelectionTests.cs` | 新增 8 个测试方法覆盖框选、Grow/Shrink、属性选择、集成 | [1-cite-0](#1-cite-0) [1-cite-1](#1-cite-1) [1-cite-2](#1-cite-2) [1-cite-3](#1-cite-3) [1-cite-4](#1-cite-4) [1-cite-5](#1-cite-5)