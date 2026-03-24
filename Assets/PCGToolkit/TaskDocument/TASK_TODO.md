基于上一轮评估中识别的 5 项潜在改进点，以下是 JSON 格式的改进计划：

```json
{
  "plan_title": "Scene 交互选择工具 - 后续改进计划",
  "repository": "No78Vino/pcg_for_unity",
  "base_ref": "main",
  "summary": "FIX-1~4 已完成，完成度 100%。以下 5 项为非阻塞性改进，提升性能、可用性和测试覆盖率。",
  "tasks": [
    {
      "id": "OPT-1",
      "title": "构建邻接表缓存，优化 Grow/Shrink 性能",
      "priority": "high",
      "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
      "current_state": "GrowSelection() 和 ShrinkSelection() 使用 O(n×m) 嵌套循环遍历所有 Primitives，每次调用 SharesVertex() 都做线性扫描。大网格（>10k 面）下会明显卡顿。",
      "target_state": "在 Geometry 加载时预构建邻接表（vertex→primitives, vertex→edges），Grow/Shrink 操作降为 O(n×k)，k 为平均邻接数。",
      "changes": [
        {
          "action": "新增邻接表数据结构",
          "details": [
            "在 PCGSelectionTool 或 PCGSceneMeshBridge 中新增 Dictionary<int, HashSet<int>> _vertexToPrims 字段，key 为顶点索引，value 为包含该顶点的 Primitive 索引集合",
            "新增 Dictionary<int, HashSet<int>> _vertexToEdges 字段，key 为顶点索引，value 为包含该顶点的 Edge 索引集合",
            "在 Geometry 加载/更新时（如 SetGeometry() 或 Instantiate()）构建这两个字典"
          ]
        },
        {
          "action": "重构 GrowSelection()",
          "line_range": "360-389",
          "details": [
            "Face 模式：遍历已选 Primitive 的顶点，通过 _vertexToPrims 直接获取相邻 Primitive，无需遍历全部 Primitives",
            "Vertex 模式：遍历已选顶点，通过 _vertexToPrims 获取包含该顶点的 Primitive，再收集这些 Primitive 的所有顶点",
            "Edge 模式：遍历已选边的端点，通过 _vertexToEdges 获取共享端点的相邻边"
          ]
        },
        {
          "action": "重构 ShrinkSelection()",
          "line_range": "391-423",
          "details": [
            "使用邻接表替代 SharesVertex() 的线性扫描",
            "删除或标记废弃 SharesVertex() 方法（第 425-431 行）"
          ]
        }
      ],
      "acceptance_criteria": [
        "功能行为与重构前完全一致（现有测试全部通过）",
        "10k 面网格上 Grow/Shrink 操作无明显卡顿（<50ms）",
        "邻接表在 Geometry 变更时自动重建"
      ]
    },
    {
      "id": "OPT-2",
      "title": "SelectByNormal 阈值参数化，添加 UI 滑块",
      "priority": "medium",
      "files": [
        "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
        "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs"
      ],
      "current_state": "SelectByNormal(Vector3 direction, float threshold = 0.7f) 的 threshold 参数在 UI 按钮中硬编码为 0.7f，用户无法调整角度阈值。",
      "target_state": "Overlay 和 Inspector 面板中提供 Slider 控件，允许用户在 0.0~1.0 范围内调整法线阈值。",
      "changes": [
        {
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
          "action": "在 'Select Up Faces' 按钮旁添加 Slider",
          "details": [
            "添加 Slider normalThresholdSlider = new Slider(0f, 1f) { value = 0.7f, label = 'Threshold' }",
            "修改 'Select Up Faces' 按钮回调，使用 normalThresholdSlider.value 替代硬编码 0.7f",
            "可选：添加 FloatField 与 Slider 双向绑定，方便精确输入"
          ]
        },
        {
          "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
          "action": "在 BuildSelectionToolSection() 中同样添加 Slider",
          "details": [
            "逻辑与 Overlay 中相同"
          ]
        }
      ],
      "acceptance_criteria": [
        "Slider 默认值为 0.7",
        "拖动 Slider 后点击 'Select Up Faces' 使用新阈值",
        "阈值 = 1.0 时只选中完全朝上的面，阈值 = 0.0 时选中所有面"
      ]
    },
    {
      "id": "OPT-3",
      "title": "补全 ShrinkSelection Vertex/Edge 模式的单元测试",
      "priority": "high",
      "file": "Assets/PCGToolkit/Editor/Tests/SelectionTests.cs",
      "current_state": "ShrinkSelection 实现了 Face/Vertex/Edge 三种模式，但测试只覆盖了 Face 模式（ShrinkSelection_Face_RemovesBoundaryFaces）。Vertex 和 Edge 模式的 Shrink 无测试覆盖。",
      "target_state": "新增 2 个测试方法覆盖 Vertex 和 Edge 模式的 Shrink 操作。",
      "changes": [
        {
          "action": "新增测试方法",
          "new_tests": [
            {
              "name": "ShrinkSelection_Vertex_RemovesBoundaryVertices",
              "description": "使用 CreateQuadGeo() 创建 3x3 网格（9 个顶点），选中所有 9 个顶点，切换到 Vertex 模式，调用 ShrinkSelection()，验证边界顶点（0,1,2,3,5,6,7,8）被移除，只保留中心顶点（4）",
              "assert": "Assert.AreEqual(1, PCGSelectionState.SelectedVertIndices.Count); Assert.IsTrue(PCGSelectionState.SelectedVertIndices.Contains(4))"
            },
            {
              "name": "ShrinkSelection_Edge_RemovesBoundaryEdges",
              "description": "使用 CreateQuadGeo() 并调用 BuildEdges()，选中所有边，切换到 Edge 模式，调用 ShrinkSelection()，验证与未选中元素相邻的边界边被移除",
              "assert": "验证剩余边数量小于初始选中数量，且剩余边均为内部边"
            }
          ]
        }
      ],
      "acceptance_criteria": [
        "两个新测试在 Unity Test Runner 中通过",
        "Vertex Shrink 测试精确验证剩余顶点集合",
        "Edge Shrink 测试验证边界边被正确移除"
      ]
    },
    {
      "id": "OPT-4",
      "title": "抽取 Overlay 和 Inspector 共享的 UI 按钮逻辑",
      "priority": "low",
      "files": [
        "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
        "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs"
      ],
      "current_state": "PCGSelectionOverlay.CreatePanelContent() 和 PCGNodeInspectorWindow.BuildSelectionToolSection() 中的 Grow/Shrink/SelectUpFaces/SelectByMaterial 按钮创建逻辑几乎完全相同，存在代码重复。",
      "target_state": "抽取共享逻辑到一个静态工具类，两处 UI 调用同一方法生成按钮。",
      "changes": [
        {
          "action": "新建共享 UI 工具类",
          "details": [
            "新建文件 Assets/PCGToolkit/Editor/Tools/PCGSelectionUIHelper.cs",
            "创建静态类 PCGSelectionUIHelper",
            "提取方法 static VisualElement CreateAdvancedSelectionButtons()，返回包含 Grow/Shrink/SelectUpFaces/SelectByMaterial 按钮的 VisualElement",
            "提取方法 static VisualElement CreateModeToggleRow()，返回 Face/Edge/Vertex 模式切换行",
            "提取方法 static Label CreateStatsLabel()，返回带定时更新的状态标签"
          ]
        },
        {
          "action": "重构 PCGSelectionOverlay.CreatePanelContent()",
          "details": [
            "替换内联按钮创建代码为 PCGSelectionUIHelper.CreateAdvancedSelectionButtons() 调用"
          ]
        },
        {
          "action": "重构 PCGNodeInspectorWindow.BuildSelectionToolSection()",
          "details": [
            "替换内联按钮创建代码为 PCGSelectionUIHelper.CreateAdvancedSelectionButtons() 调用"
          ]
        }
      ],
      "acceptance_criteria": [
        "Overlay 和 Inspector 面板功能行为不变",
        "两处 UI 不再有重复的按钮创建代码",
        "新增按钮时只需修改 PCGSelectionUIHelper 一处"
      ]
    },
    {
      "id": "OPT-5",
      "title": "强化 ShrinkSelection_Face 测试断言",
      "priority": "low",
      "file": "Assets/PCGToolkit/Editor/Tests/SelectionTests.cs",
      "current_state": "ShrinkSelection_Face_RemovesBoundaryFaces 测试仅使用 Assert.Less(count, 18) 做模糊断言，无法精确验证哪些面被保留。",
      "target_state": "使用精确断言验证 Shrink 后的剩余面集合。",
      "changes": [
        {
          "action": "重写测试断言",
          "details": [
            "构造一个 3x3 网格（18 个三角形 = 9 个 quad × 2），选中全部 18 个 prim",
            "调用 ShrinkSelection() 后，精确计算预期保留的内部面索引",
            "使用 CollectionAssert.AreEquivalent() 或逐个 Assert.IsTrue() 验证剩余面集合",
            "如果 2x2 网格（8 个 prim）全部为边界面，则 Shrink 后应为空集，断言 Assert.AreEqual(0, count)"
          ]
        }
      ],
      "acceptance_criteria": [
        "测试使用精确断言而非模糊的 Assert.Less",
        "断言内容与几何拓扑一致（可手动推导验证）",
        "测试在 Unity Test Runner 中通过"
      ]
    }
  ],
  "execution_order": [
    {
      "step": 1,
      "task_ids": ["OPT-1"],
      "reason": "性能优化是最高优先级，邻接表是后续优化的基础数据结构"
    },
    {
      "step": 2,
      "task_ids": ["OPT-2", "OPT-4"],
      "reason": "UI 改进互不依赖，可并行执行。OPT-4 的 UIHelper 抽取可为 OPT-2 的 Slider 提供更好的代码组织"
    },
    {
      "step": 3,
      "task_ids": ["OPT-3", "OPT-5"],
      "reason": "测试补全和强化应在功能改动完成后进行，确保测试覆盖最终实现"
    }
  ],
  "files_to_modify": [
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
    "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
    "Assets/PCGToolkit/Editor/Tests/SelectionTests.cs"
  ],
  "files_to_create": [
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionUIHelper.cs"
  ]
}
```

各任务对应的代码位置：

| 任务 | 关键文件 & 行号 | 改动性质 |
|------|----------------|----------|
| **OPT-1** | `PCGSelectionTool.cs` L360-431 | 重构 Grow/Shrink + 删除 `SharesVertex()` |
| **OPT-2** | `PCGSelectionOverlay.cs` + `PCGNodeInspectorWindow.cs` | 新增 Slider 控件 |
| **OPT-3** | `SelectionTests.cs` | 新增 2 个测试方法 |
| **OPT-4** | 新建 `PCGSelectionUIHelper.cs` | 抽取共享 UI 逻辑 |
| **OPT-5** | `SelectionTests.cs` | 重写断言 | [1-cite-0](#1-cite-0) [1-cite-1](#1-cite-1) [1-cite-2](#1-cite-2) [1-cite-3](#1-cite-3) [1-cite-4](#1-cite-4) [1-cite-5](#1-cite-5) [1-cite-6](#1-cite-6)