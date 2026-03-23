```json
{
  "iteration": 10,
  "title": "交互式选择 + Group 输入混合工作流",
  "description": "将 ProBuilder 的 Scene View 交互式选面/选边/选点能力移植到 PCG for Unity，将选择结果作为 PointGroups/PrimGroups 输入喂给节点图",
  "repositories": {
    "target": "No78Vino/pcg_for_unity",
    "reference": "No78Vino/com.unity.probuilder"
  },
  "phases": [
    {
      "id": "A",
      "name": "核心功能：桥接层 + 面选择",
      "tasks": [
        {
          "id": "A1",
          "title": "创建 PCGSelectMode 枚举",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Core/PCGSelectMode.cs",
          "description": "定义选择模式枚举：Face, Edge, Vertex",
          "details": {
            "type": "enum",
            "name": "PCGSelectMode",
            "values": ["Face", "Edge", "Vertex"]
          },
          "dependencies": []
        },
        {
          "id": "A2",
          "title": "创建 PCGSelectionState 选择状态管理器",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Core/PCGSelectionState.cs",
          "description": "存储当前交互选择的状态，使用 ScriptableSingleton<T> 或静态实例，使其在 EditorTool 和 Node 之间共享",
          "details": {
            "fields": [
              { "name": "SelectedPrimIndices", "type": "HashSet<int>", "description": "选中的面索引，对应 PCGGeometry.Primitives" },
              { "name": "SelectedPointIndices", "type": "HashSet<int>", "description": "选中的点索引，对应 PCGGeometry.Points" },
              { "name": "SelectedEdgeIndices", "type": "HashSet<int>", "description": "选中的边索引，对应 PCGGeometry.Edges" },
              { "name": "CurrentMode", "type": "PCGSelectMode", "description": "当前选择模式" },
              { "name": "SourceGeometry", "type": "PCGGeometry", "description": "当前操作的几何体引用" }
            ],
            "methods": [
              "AddToSelection(int index)",
              "RemoveFromSelection(int index)",
              "ToggleSelection(int index)",
              "Clear()"
            ],
            "events": ["event Action SelectionChanged"]
          },
          "dependencies": ["A1"]
        },
        {
          "id": "A3",
          "title": "创建 PCGSceneMeshBridge 桥接层",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Core/PCGSceneMeshBridge.cs",
          "description": "将 PCGGeometry 临时实例化为场景中的 Mesh + MeshCollider，维护双向索引映射。参考现有 PCGGeometryToMesh.ConvertWithSubmeshes() 的三角化逻辑，在三角化时记录映射关系",
          "details": {
            "key_methods": [
              {
                "name": "Instantiate(PCGGeometry geo)",
                "description": "创建临时 GameObject（HideFlags.DontSave），生成 Unity Mesh，添加 MeshCollider，构建索引映射"
              },
              {
                "name": "Dispose()",
                "description": "清理临时 GameObject 和所有资源"
              }
            ],
            "key_fields": [
              { "name": "unityTriToPcgPrim", "type": "Dictionary<int, int>", "description": "Unity triangleIndex → PCGGeometry Primitives 索引。因为 PCGGeometry 支持 N-gon，一个 Primitive 可能对应多个 Unity 三角形" },
              { "name": "unityVertToPcgPoint", "type": "Dictionary<int, int>", "description": "Unity vertex index → PCGGeometry Points 索引" },
              { "name": "TempGameObject", "type": "GameObject", "description": "临时场景对象引用" }
            ],
            "reference_files": [
              "Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs",
              "Assets/PCGToolkit/Editor/Graph/PCGScenePreview.cs"
            ],
            "notes": "三角化 N-gon 时，需要记录每个生成的 Unity 三角形对应的原始 Primitive 索引"
          },
          "dependencies": []
        },
        {
          "id": "A4",
          "title": "创建 PCGSelectionTool（面选择 + 点击）",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
          "description": "EditorTool 子类，拦截 SceneView 鼠标事件，执行面选择。阶段 A 先只实现面模式的点击选择",
          "details": {
            "base_class": "EditorTool",
            "key_methods": [
              {
                "name": "OnToolGUI(EditorWindow window)",
                "description": "处理鼠标事件入口"
              },
              {
                "name": "HandleFaceClick(Event evt)",
                "description": "Physics.Raycast 命中临时 Mesh 的 MeshCollider，获取 RaycastHit.triangleIndex，通过 PCGSceneMeshBridge.unityTriToPcgPrim 映射回 PCGGeometry Primitives 索引"
              }
            ],
            "modifier_keys": {
              "none": "替换选择",
              "shift": "追加选择",
              "ctrl": "减去选择"
            },
            "reference_files": [
              "No78Vino/com.unity.probuilder: Editor/EditorCore/EditorSceneViewPicker.cs (DoMouseClick, lines 56-252)"
            ]
          },
          "dependencies": ["A2", "A3"]
        },
        {
          "id": "A5",
          "title": "创建 PCGSelectionRenderer 选择高亮渲染",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionRenderer.cs",
          "description": "在 SceneView 中渲染选择高亮效果",
          "details": {
            "callback": "SceneView.duringSceneGui",
            "rendering": {
              "face_highlight": "GL 绘制半透明蓝色面片覆盖选中面",
              "edge_highlight": "Handles.DrawAAPolyLine 绘制亮蓝色线",
              "point_highlight": "Handles.DotHandleCap 或 Handles.SphereHandleCap 绘制蓝色圆点",
              "hover_preview": "鼠标悬停时用半透明黄色预览将要选中的元素"
            },
            "data_source": "从 PCGSelectionState 读取选中索引，从 PCGSceneMeshBridge 获取世界坐标"
          },
          "dependencies": ["A2", "A3"]
        },
        {
          "id": "A6",
          "title": "创建 SceneSelectionInputNode 节点",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Nodes/Input/SceneSelectionInputNode.cs",
          "description": "新的输入节点，读取 PCGSelectionState 并输出带 Group 的 PCGGeometry",
          "details": {
            "inputs": [
              { "name": "target", "type": "SceneObject", "description": "场景中的 GameObject" },
              { "name": "groupName", "type": "String", "default": "selected", "description": "输出的 Group 名称" },
              { "name": "applyTransform", "type": "Bool", "default": true, "description": "是否烘焙世界变换" },
              { "name": "readMaterials", "type": "Bool", "default": true, "description": "是否读取材质" }
            ],
            "outputs": [
              { "name": "geometry", "type": "Geometry", "description": "完整几何体，带 PrimGroups[groupName] 和/或 PointGroups[groupName]" }
            ],
            "execute_logic": [
              "1. 复用 SceneObjectInputNode 的 Mesh 读取逻辑",
              "2. 从 PCGSelectionState 读取当前选中的索引",
              "3. Face 模式：将 selectedPrimIndices 写入 geo.PrimGroups[groupName]",
              "4. Vertex 模式：将 selectedPointIndices 写入 geo.PointGroups[groupName]",
              "5. Edge 模式：将选中边的两端点写入 geo.PointGroups[groupName]"
            ],
            "reference_files": [
              "Assets/PCGToolkit/Editor/Nodes/Input/SceneObjectInputNode.cs"
            ]
          },
          "dependencies": ["A2"]
        },
        {
          "id": "A7",
          "title": "注册 SceneSelectionInputNode 到节点注册表",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeRegistry.cs",
          "description": "在 PCGNodeRegistry 中注册 SceneSelectionInputNode，分类为 Create/Input",
          "dependencies": ["A6"]
        },
        {
          "id": "A8",
          "title": "阶段 A 联调测试",
          "action": "test",
          "description": "验证面选择完整流程：激活 PCGSelectionTool → 点击选面 → SceneSelectionInputNode 输出带 PrimGroups['selected'] 的几何体 → 下游 BlastNode/ExtrudeNode 正确消费 Group",
          "test_cases": [
            "点击选中单个面，验证 PrimGroups 包含正确索引",
            "Shift 点击追加选择多个面",
            "Ctrl 点击减去已选面",
            "SceneSelectionInputNode 连接 BlastNode(group='selected')，验证删除选中面",
            "SceneSelectionInputNode 连接 ExtrudeNode(group='selected')，验证只挤出选中面",
            "N-gon 几何体的索引映射正确性"
          ],
          "dependencies": ["A4", "A5", "A6", "A7"]
        }
      ]
    },
    {
      "id": "B",
      "name": "完整选择模式：边/点选择 + 框选 + Overlay",
      "tasks": [
        {
          "id": "B1",
          "title": "PCGSelectionTool 添加点选择",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
          "description": "Raycast 命中三角形后，获取命中三角形的三个顶点，找距离鼠标最近的顶点，通过 unityVertToPcgPoint 映射回 PCGGeometry Points 索引",
          "dependencies": ["A8"]
        },
        {
          "id": "B2",
          "title": "PCGSelectionTool 添加边选择",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
          "description": "Raycast 命中三角形后，获取命中三角形的三条边，找距离鼠标最近的边，通过映射表反查 PCGGeometry Edges 索引。需先调用 geo.BuildEdges()",
          "dependencies": ["A8"]
        },
        {
          "id": "B3",
          "title": "PCGSelectionTool 添加矩形框选",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
          "description": "检测鼠标拖拽，计算屏幕空间矩形，遍历所有面/点/边，用 Camera.WorldToScreenPoint 判断是否在矩形内。参考 ProBuilder 的 DoMouseDrag (com.unity.probuilder Editor/EditorCore/EditorSceneViewPicker.cs:255-388)",
          "details": {
            "face_rect_select": "遍历所有 Primitive，计算其中心点或所有顶点的屏幕投影，判断是否在矩形内",
            "vertex_rect_select": "遍历所有 Points，投影到屏幕空间判断",
            "edge_rect_select": "遍历所有 Edges，投影两端点到屏幕空间判断"
          },
          "dependencies": ["A8"]
        },
        {
          "id": "B4",
          "title": "PCGSelectionRenderer 添加边和点高亮",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionRenderer.cs",
          "description": "扩展渲染器支持边高亮（DrawAAPolyLine）和点高亮（DotHandleCap），根据 PCGSelectionState.CurrentMode 切换渲染模式",
          "dependencies": ["B1", "B2"]
        },
        {
          "id": "B5",
          "title": "创建 PCGSelectionOverlay 工具栏",
          "action": "create",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
          "description": "SceneView Overlay 工具栏，提供 Face/Edge/Vertex 模式切换按钮",
          "details": {
            "base_class": "UnityEditor.Overlays.Overlay, ICreateToolbar",
            "ui_elements": [
              { "type": "toggle", "label": "Face", "action": "切换到面选择模式" },
              { "type": "toggle", "label": "Edge", "action": "切换到边选择模式" },
              { "type": "toggle", "label": "Vertex", "action": "切换到点选择模式" },
              { "type": "label", "content": "选中数量统计" },
              { "type": "button", "label": "Clear Selection", "action": "清空选择" },
              { "type": "button", "label": "Apply to Graph", "action": "将选择结果写入 PCGSelectionState 并通知节点图刷新" }
            ],
            "reference_files": [
              "No78Vino/com.unity.probuilder: Editor/Overlays/SelectionSettingsButtons.cs"
            ]
          },
          "dependencies": ["A8"]
        },
        {
          "id": "B6",
          "title": "修改 PCGNodeInspectorWindow 添加 Selection Tool 入口",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
          "description": "为 SceneSelectionInputNode 添加特殊的 Inspector UI：'Open Selection Tool' 按钮（激活 PCGSelectionTool）、显示当前选中元素数量",
          "dependencies": ["B5"]
        },
        {
          "id": "B7",
          "title": "阶段 B 联调测试",
          "action": "test",
          "description": "验证完整选择模式",
          "test_cases": [
            "Face/Edge/Vertex 三种模式切换正常",
            "点击选边，验证 PointGroups 包含边两端点索引",
            "点击选点，验证 PointGroups 包含正确点索引",
            "矩形框选多个面/边/点",
            "Overlay 工具栏按钮功能正常",
            "Inspector 中 Open Selection Tool 按钮正常激活工具",
            "SceneSelectionInputNode 连接 ScatterNode(group='selected')，验证只在选中面上散布"
          ],
          "dependencies": ["B3", "B4", "B5", "B6"]
        }
      ]
    },
    {
      "id": "C",
      "name": "高级功能：选择扩展/收缩 + 按属性选择 + 持久化",
      "tasks": [
        {
          "id": "C1",
          "title": "选择扩展/收缩功能",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
          "description": "实现 GrowSelection（扩展选择到相邻面/边/点）和 ShrinkSelection（收缩选择），参考 ProBuilder 的 ElementSelection.cs 中 GetNeighborFaces 等方法",
          "details": {
            "grow_selection": "从当前选中面出发，找到共享边的相邻面，加入选择",
            "shrink_selection": "移除选择集中边界面（只有部分邻居被选中的面）",
            "keyboard_shortcuts": {
              "grow": "Ctrl+Numpad+",
              "shrink": "Ctrl+Numpad-"
            }
          },
          "dependencies": ["B7"]
        },
        {
          "id": "C2",
          "title": "按属性选择",
          "action": "modify",
          "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
          "description": "支持按材质 ID 选择所有同材质面、按法线方向选择（如选择所有朝上的面）、按 Primitive Attribute 值选择",
          "details": {
            "select_by_material": "选中一个面后，可扩展选择所有相同材质 ID 的面",
            "select_by_normal": "提供法线方向阈值，选择法线在阈值范围内的面",
            "select_by_attribute": "根据 PCGGeometry 的 PrimAttributes 中某个属性值进行过滤选择"
          },
          "dependencies": ["B7"]
        },
        {
          "id": "C3",
          "title": "选择结果序列化持久化",
          "action": "modify",
          "files": [
            "Assets/PCGToolkit/Editor/Core/PCGSelectionState.cs",
            "Assets/PCGToolkit/Editor/Nodes/Input/SceneSelectionInputNode.cs"
          ],
          "description": "将选择结果序列化保存到 GraphData 中，使选择在关闭/重开编辑器后可恢复",
          "details": {
            "serialization_format": "将 selectedPrimIndices/selectedPointIndices 序列化为 JSON 或 int[] 存储在 SceneSelectionInputNode 的 SerializedProperty 中",
            "restore_logic": "节点加载时从序列化数据恢复 PCGSelectionState"
          },
          "dependencies": ["B7"]
        },
        {
          "id": "C4",
          "title": "阶段 C 联调测试",
          "action": "test",
          "description": "验证高级功能",
          "test_cases": [
            "选择一个面后 GrowSelection 正确扩展到相邻面",
            "ShrinkSelection 正确收缩边界面",
            "按材质选择所有同材质面",
            "按法线方向选择朝上的面",
            "关闭编辑器重开后选择结果正确恢复",
            "完整工作流：交互选面 → ExtrudeNode → UVProjectNode → 输出"
          ],
          "dependencies": ["C1", "C2", "C3"]
        }
      ]
    }
  ],
  "files_to_create": [
    "Assets/PCGToolkit/Editor/Core/PCGSelectMode.cs",
    "Assets/PCGToolkit/Editor/Core/PCGSelectionState.cs",
    "Assets/PCGToolkit/Editor/Core/PCGSceneMeshBridge.cs",
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionRenderer.cs",
    "Assets/PCGToolkit/Editor/Tools/PCGSelectionOverlay.cs",
    "Assets/PCGToolkit/Editor/Nodes/Input/SceneSelectionInputNode.cs"
  ],
  "files_to_modify": [
    "Assets/PCGToolkit/Editor/Graph/PCGNodeRegistry.cs",
    "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs"
  ],
  "key_reference_files": {
    "pcg_for_unity": [
      "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",
      "Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs",
      "Assets/PCGToolkit/Editor/Graph/PCGScenePreview.cs",
      "Assets/PCGToolkit/Editor/Nodes/Input/SceneObjectInputNode.cs",
      "Assets/PCGToolkit/Editor/Nodes/Distribute/ScatterNode.cs",
      "Assets/PCGToolkit/Editor/Nodes/Group/GroupCreateNode.cs"
    ],
    "com_unity_probuilder": [
      "Editor/EditorCore/EditorSceneViewPicker.cs",
      "Runtime/Core/SelectionPickerRenderer.cs",
      "Runtime/Core/ProBuilderEnum.cs",
      "Editor/Overlays/SelectionSettingsButtons.cs",
      "Editor/EditorCore/EditorHandleUtility.cs"
    ]
  },
  "data_flow": {
    "description": "PCGGeometry → PCGSceneMeshBridge.Instantiate() → 临时 Mesh + MeshCollider → 用户 Scene 交互选择 → PCGSelectionState → SceneSelectionInputNode → PrimGroups['selected'] / PointGroups['selected'] → 下游节点消费"
  },
  "downstream_node_compatibility": {
    "description": "SceneSelectionInputNode 输出的 Group 可被以下现有节点直接消费",
    "compatible_nodes": [
      { "node": "BlastNode", "usage": "删除选中面" },
      { "node": "ExtrudeNode", "usage": "挤出选中面" },
      { "node": "ScatterNode", "usage": "在选中面上散布" },
      { "node": "ForEachNode", "usage": "对选中元素逐个迭代" },
      { "node": "SplitNode", "usage": "按选中/未选中分流" }
    ]
  }
}
```

这份 JSON 包含了完整的三阶段任务分解、文件清单、依赖关系、测试用例和参考文件。每个 task 的 `dependencies` 字段标明了前置依赖，agent 可以据此确定执行顺序。