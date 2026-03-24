# 任务1
```json
{
  "task_id": "pcg_quick_select_tool",
  "title": "实现 PCG Selection Tool 三层快捷启动系统",
  "description": "当前从场景 GameObject 启动 Selection Tool 需要 6 步操作（打开Graph → 创建节点 → 拖GO → 选中节点 → 点Inspector按钮 → 选面）。本任务实现三层快捷入口，将操作简化为 1-2 步，并支持在没有 SceneSelectionInputNode 时自动创建。",
  "repository": "No78Vino/pcg_for_unity",

  "files_to_create": [
    {
      "path": "Assets/PCGToolkit/Editor/Tools/PCGQuickSelect.cs",
      "namespace": "PCGToolkit.Tools",
      "description": "核心调度器静态类，统一处理所有快捷入口的逻辑",
      "using_directives": [
        "System",
        "System.Collections.Generic",
        "UnityEditor",
        "UnityEditor.EditorTools",
        "UnityEngine",
        "PCGToolkit.Core",
        "PCGToolkit.Graph"
      ],
      "class_definition": {
        "name": "PCGQuickSelect",
        "modifiers": "public static"
      },
      "menu_items": [
        {
          "attribute": "[MenuItem(\"GameObject/PCG Toolkit/Select Faces\", false, 49)]",
          "method_name": "SelectFacesFromHierarchy",
          "body_logic": "设置 PCGSelectionState.SetMode(PCGSelectMode.Face)，然后调用 Launch(Selection.activeGameObject)"
        },
        {
          "attribute": "[MenuItem(\"GameObject/PCG Toolkit/Select Faces\", true)]",
          "method_name": "ValidateSelectFaces",
          "body_logic": "return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<MeshFilter>() != null"
        },
        {
          "attribute": "[MenuItem(\"GameObject/PCG Toolkit/Select Edges\", false, 50)]",
          "method_name": "SelectEdgesFromHierarchy",
          "body_logic": "设置 PCGSelectionState.SetMode(PCGSelectMode.Edge)，然后调用 Launch(Selection.activeGameObject)"
        },
        {
          "attribute": "[MenuItem(\"GameObject/PCG Toolkit/Select Edges\", true)]",
          "method_name": "ValidateSelectEdges",
          "body_logic": "同 ValidateSelectFaces"
        },
        {
          "attribute": "[MenuItem(\"GameObject/PCG Toolkit/Select Vertices\", false, 51)]",
          "method_name": "SelectVerticesFromHierarchy",
          "body_logic": "设置 PCGSelectionState.SetMode(PCGSelectMode.Vertex)，然后调用 Launch(Selection.activeGameObject)"
        },
        {
          "attribute": "[MenuItem(\"GameObject/PCG Toolkit/Select Vertices\", true)]",
          "method_name": "ValidateSelectVertices",
          "body_logic": "同 ValidateSelectFaces"
        }
      ],
      "methods": [
        {
          "name": "Launch",
          "signature": "public static void Launch(GameObject go)",
          "description": "主入口方法，完成以下步骤：",
          "steps": [
            "1. 验证 go 不为 null",
            "2. 获取 MeshFilter mf = go.GetComponent<MeshFilter>()，验证 mf != null && mf.sharedMesh != null，否则 Debug.LogWarning 并 return",
            "3. 调用 var geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh) 转换为 PCGGeometry",
            "4. 调用 ApplyWorldTransform(geo, go.transform) 烘焙世界变换",
            "5. 调用 EnsureGraphNode(go) 智能关联 Graph（如果 Graph Editor 打开）",
            "6. 调用 ToolManager.SetActiveTool<PCGSelectionTool>()",
            "7. 使用 EditorApplication.delayCall 延迟一帧（等待 OnActivated 设置 ActiveInstance），然后调用 PCGSelectionTool.ActiveInstance?.SetGeometry(geo)",
            "8. Debug.Log 输出成功信息，包含 go.name、Points.Count、Primitives.Count"
          ]
        },
        {
          "name": "ApplyWorldTransform",
          "signature": "public static void ApplyWorldTransform(PCGGeometry geo, Transform transform)",
          "description": "烘焙世界变换到几何体，复用 SceneSelectionInputNode.Execute 中 lines 84-99 的逻辑",
          "steps": [
            "1. var matrix = transform.localToWorldMatrix",
            "2. 遍历 geo.Points，用 matrix.MultiplyPoint3x4 变换每个点",
            "3. 获取法线属性 geo.PointAttribs.GetAttribute('N')",
            "4. 如果法线存在，用 matrix.inverse.transpose 的 MultiplyVector 变换每个法线并 normalized"
          ],
          "reference_code": {
            "file": "Assets/PCGToolkit/Editor/Nodes/Input/SceneSelectionInputNode.cs",
            "lines": "84-99"
          }
        },
        {
          "name": "EnsureGraphNode",
          "signature": "private static void EnsureGraphNode(GameObject go)",
          "description": "智能 Graph 关联：查找或创建 SceneSelectionInputNode，设置 target 参数",
          "steps": [
            "1. 通过 EditorWindow.GetWindow<PCGGraphEditorWindow>(false, null, false) 获取已打开的 Graph Editor（第三个参数 false 表示不强制聚焦，如果没打开则返回 null）",
            "2. 如果 editorWindow == null，直接 return（工具仍然可以独立工作，不需要 Graph）",
            "3. 通过新增的 editorWindow.GetGraphView() 方法获取 graphView",
            "4. 如果 graphView == null，return",
            "5. 调用 graphView.FindNodeVisualByType('SceneSelectionInput') 查找已有的 SceneSelectionInputNode",
            "6. 如果找到已有节点 existingNode：",
            "   a. 创建 var sceneRef = new PCGSceneObjectRef(go)",
            "   b. 调用 existingNode.SetPortDefaultValues(new Dictionary<string, object> { { 'target', sceneRef } })",
            "   c. 调用 graphView.ClearSelection()",
            "   d. 调用 graphView.AddToSelection(existingNode)",
            "7. 如果没找到已有节点：",
            "   a. 调用 PCGNodeRegistry.EnsureInitialized()",
            "   b. 调用 var template = PCGNodeRegistry.GetNode('SceneSelectionInput')",
            "   c. 如果 template == null，return",
            "   d. 创建 var newNode = (IPCGNode)Activator.CreateInstance(template.GetType())",
            "   e. 调用 var visual = graphView.CreateNodeVisual(newNode, new Vector2(100, 100))",
            "   f. 创建 var sceneRef = new PCGSceneObjectRef(go)",
            "   g. 调用 visual.SetPortDefaultValues(new Dictionary<string, object> { { 'target', sceneRef } })",
            "   h. 调用 graphView.ClearSelection()",
            "   i. 调用 graphView.AddToSelection(visual)",
            "   j. 调用 graphView.FrameSelection()",
            "   k. 调用 graphView.NotifyGraphChanged() 标记脏状态"
          ]
        }
      ]
    }
  ],

  "files_to_modify": [
    {
      "path": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
      "changes": [
        {
          "target": "OnActivated() 方法",
          "current_line_range": [70, 74],
          "current_code": "public override void OnActivated()\n{\n    ActiveInstance = this;\n    SceneView.RepaintAll();\n}",
          "description": "在 ActiveInstance = this 之后、SceneView.RepaintAll() 之前，添加自动检测逻辑",
          "new_code": "public override void OnActivated()\n{\n    ActiveInstance = this;\n\n    // 自动检测：如果没有几何数据，尝试从场景选中物体读取\n    if (!_bridge.IsValid)\n    {\n        var go = Selection.activeGameObject;\n        if (go != null)\n        {\n            var mf = go.GetComponent<MeshFilter>();\n            if (mf != null && mf.sharedMesh != null)\n            {\n                var geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh);\n                PCGQuickSelect.ApplyWorldTransform(geo, go.transform);\n                SetGeometry(geo);\n                Debug.Log($\"[PCGSelectionTool] Auto-loaded geometry from '{go.name}': {geo.Points.Count} points, {geo.Primitives.Count} prims\");\n            }\n        }\n    }\n\n    SceneView.RepaintAll();\n}",
          "required_using": "using PCGToolkit.Tools; // 如果不在同一命名空间，需要引用 PCGQuickSelect"
        }
      ]
    },
    {
      "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
      "changes": [
        {
          "target": "添加公开方法 GetGraphView()",
          "description": "在类中添加一个公开方法，供 PCGQuickSelect.EnsureGraphNode() 访问 graphView 实例",
          "insert_after_line": 487,
          "new_code": "/// <summary>\n/// 供外部（如 PCGQuickSelect）访问当前 GraphView 实例\n/// </summary>\npublic PCGGraphView GetGraphView() => graphView;"
        },
        {
          "target": "HandleKeyboardShortcut() 方法",
          "current_line_range": [489, 503],
          "description": "在方法末尾（line 503 的 } 之前）添加 Ctrl+Shift+F 快捷键",
          "insert_before_closing_brace": true,
          "new_code": "// Ctrl+Shift+F: Quick Select Faces on active scene object\nelse if (evt.keyCode == KeyCode.F && evt.ctrlKey && evt.shiftKey)\n{\n    var go = Selection.activeGameObject;\n    if (go != null)\n        PCGToolkit.Tools.PCGQuickSelect.Launch(go);\n    evt.StopPropagation();\n}"
        }
      ]
    },
    {
      "path": "Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs",
      "changes": [
        {
          "target": "添加 FindNodeVisualByType() 辅助方法",
          "description": "在 FindNodeVisual(string nodeId) 方法（line 609-618）之后添加按节点类型查找的方法",
          "insert_after_line": 618,
          "new_code": "/// <summary>\n/// 按节点类型名查找第一个匹配的 PCGNodeVisual\n/// </summary>\npublic PCGNodeVisual FindNodeVisualByType(string nodeTypeName)\n{\n    PCGNodeVisual found = null;\n    nodes.ForEach(node =>\n    {\n        if (found != null) return;\n        if (node is PCGNodeVisual visual && visual.PCGNode.Name == nodeTypeName)\n            found = visual;\n    });\n    return found;\n}"
        }
      ]
    }
  ],

  "key_dependencies": {
    "PCGSelectionTool": {
      "file": "Assets/PCGToolkit/Editor/Tools/PCGSelectionTool.cs",
      "relevant_members": {
        "ActiveInstance": { "line": 22, "type": "static property", "note": "在 OnActivated() 中设置，需要 delayCall 等待" },
        "SetGeometry(PCGGeometry)": { "line": 27, "note": "初始化 bridge，创建临时 MeshCollider，使 IsValid 为 true" },
        "_bridge.IsValid": { "line": 26, "note": "TempGameObject != null && TempMesh != null && Geometry != null" }
      }
    },
    "PCGGeometryToMesh": {
      "file": "Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs",
      "relevant_members": {
        "FromMesh(Mesh)": { "line": 280, "note": "将 Unity Mesh 转为 PCGGeometry，包含顶点、法线、UV、颜色、Submesh" }
      }
    },
    "PCGSceneObjectRef": {
      "file": "Assets/PCGToolkit/Editor/Core/PCGParamHelper.cs",
      "relevant_members": {
        "constructor(GameObject)": { "line": 18, "note": "记录 instanceID 和 HierarchyPath" },
        "Resolve()": { "line": 25, "note": "通过 instanceID 或路径解析为 GameObject" }
      }
    },
    "PCGNodeRegistry": {
      "file": "Assets/PCGToolkit/Editor/Core/PCGNodeRegistry.cs",
      "relevant_members": {
        "EnsureInitialized()": { "note": "确保节点注册表已初始化" },
        "GetNode(string)": { "note": "按节点类型名获取模板实例" }
      }
    },
    "PCGGraphView": {
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs",
      "relevant_members": {
        "CreateNodeVisual(IPCGNode, Vector2)": { "line": 653, "note": "程序化创建节点并添加到画布" },
        "FindNodeVisual(string)": { "line": 609, "note": "按 nodeId 查找节点" },
        "NotifyGraphChanged()": { "line": 28, "note": "触发脏状态事件" },
        "ClearSelection()": { "note": "GraphView 基类方法，清除选中" },
        "AddToSelection(ISelectable)": { "note": "GraphView 基类方法，添加选中" },
        "FrameSelection()": { "note": "GraphView 基类方法，聚焦到选中元素" }
      }
    },
    "PCGNodeVisual": {
      "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeVisual.cs",
      "relevant_members": {
        "PCGNode.Name": { "line": 12, "note": "节点类型名，如 'SceneSelectionInput'" },
        "SetPortDefaultValues(Dictionary)": { "line": 80, "note": "设置端口默认值" },
        "GetPortDefaultValues()": { "line": 72, "note": "获取端口默认值" }
      }
    },
    "PCGGraphEditorWindow": {
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
      "relevant_members": {
        "graphView": { "line": 11, "type": "private field", "note": "需要通过新增的 GetGraphView() 公开" },
        "HandleKeyboardShortcut()": { "line": 489, "note": "键盘快捷键处理" }
      }
    },
    "PCGSelectionState": {
      "note": "静态类，管理选择模式和选中索引",
      "relevant_members": {
        "SetMode(PCGSelectMode)": { "note": "设置 Face/Edge/Vertex 模式" },
        "Clear()": { "note": "清除所有选择" }
      }
    }
  },

  "testing": {
    "manual_test_cases": [
      {
        "id": "T1",
        "name": "Hierarchy 右键菜单 - Select Faces",
        "steps": [
          "1. 场景中放一个带 MeshFilter 的 Cube",
          "2. 在 Hierarchy 中右键 Cube",
          "3. 点击 'PCG Toolkit > Select Faces'",
          "4. 验证：Selection Tool 自动激活",
          "5. 验证：Scene View 中左键点击 Cube 的面可以选中（高亮显示）",
          "6. 验证：Shift+点击追加选择，Ctrl+点击取消选择"
        ]
      },
      {
        "id": "T2",
        "name": "自动创建 SceneSelectionInputNode",
        "steps": [
          "1. 打开 PCG Graph Editor (PCG Toolkit > Node Editor)",
          "2. 新建空图",
          "3. 在 Hierarchy 中右键带 MeshFilter 的 GO → PCG Toolkit > Select Faces",
          "4. 验证：Graph 中自动创建了 SceneSelectionInputNode",
          "5. 验证：该节点的 target 参数已设置为右键的 GO",
          "6. 验证：该节点被选中（Inspector 显示其参数）"
        ]
      },
      {
        "id": "T3",
        "name": "复用已有 SceneSelectionInputNode",
        "steps": [
          "1. 在 Graph 中已有一个 SceneSelectionInputNode（target 为 CubeA）",
          "2. 在 Hierarchy 中右键 CubeB → PCG Toolkit > Select Faces",
          "3. 验证：没有创建新节点，已有节点的 target 更新为 CubeB",
          "4. 验证：Selection Tool 加载了 CubeB 的几何"
        ]
      },
      {
        "id": "T4",
        "name": "无 Graph Editor 时独立工作",
        "steps": [
          "1. 关闭 PCG Graph Editor 窗口",
          "2. 在 Hierarchy 中右键带 MeshFilter 的 GO → PCG Toolkit > Select Faces",
          "3. 验证：Selection Tool 仍然正常激活并可以选面",
          "4. 验证：Console 无报错"
        ]
      },
      {
        "id": "T5",
        "name": "快捷键 Ctrl+Shift+F",
        "steps": [
          "1. 打开 PCG Graph Editor",
          "2. 在 Hierarchy 中选中一个带 MeshFilter 的 GO",
          "3. 在 Graph Editor 窗口中按 Ctrl+Shift+F",
          "4. 验证：Selection Tool 激活，加载了选中 GO 的几何"
        ]
      },
      {
        "id": "T6",
        "name": "OnActivated 自动检测",
        "steps": [
          "1. 在 Hierarchy 中选中一个带 MeshFilter 的 GO",
          "2. 通过 Unity 工具栏直接点击 PCG Selection Tool 图标（不通过右键菜单）",
          "3. 验证：工具自动从 Selection.activeGameObject 读取几何",
          "4. 验证：Scene View 中可以正常选面"
        ]
      },
      {
        "id": "T7",
        "name": "Validate 方法 - 无 MeshFilter 时菜单不可用",
        "steps": [
          "1. 在 Hierarchy 中选中一个空 GameObject（无 MeshFilter）",
          "2. 右键查看菜单",
          "3. 验证：'PCG Toolkit > Select Faces/Edges/Vertices' 菜单项为灰色不可点击"
        ]
      },
      {
        "id": "T8",
        "name": "Select Edges 和 Select Vertices 模式",
        "steps": [
          "1. 右键 GO → PCG Toolkit > Select Edges",
          "2. 验证：工具激活且模式为 Edge",
          "3. 右键 GO → PCG Toolkit > Select Vertices",
          "4. 验证：工具激活且模式为 Vertex"
        ]
      }
    ],
    "existing_test_file": "Assets/PCGToolkit/Editor/Tests/SelectionTests.cs",
    "note": "修改完成后运行现有测试确保不破坏已有功能"
  },

  "implementation_notes": [
    "PCGQuickSelect.Launch() 中的 EditorApplication.delayCall 是必须的，因为 ToolManager.SetActiveTool 是异步的，ActiveInstance 在 OnActivated() 回调中才被设置",
    "EnsureGraphNode 中使用 EditorWindow.GetWindow<PCGGraphEditorWindow>(false, null, false) 的第三个参数 false 很重要，它表示不强制聚焦窗口，如果窗口未打开则返回 null 而不是创建新窗口",
    "PCGSceneObjectRef 的构造函数会记录 instanceID 和 HierarchyPath，确保序列化后可以恢复",
    "ApplyWorldTransform 需要作为 public static 方法，因为 PCGSelectionTool.OnActivated() 中也需要调用它",
    "PCGSelectionTool.OnActivated() 中的自动检测不需要 delayCall，因为 SetGeometry 是同步调用",
    "FindNodeVisualByType 只返回第一个匹配的节点，如果图中有多个 SceneSelectionInputNode，只更新第一个"
  ]
}
```

以上 JSON 包含了完整的实现规格。关键引用：

- `PCGSelectionTool.OnActivated()` 需要添加自动检测逻辑 [4-cite-0](#4-cite-0)

- `PCGGraphEditorWindow.HandleKeyboardShortcut()` 需要添加 Ctrl+Shift+F [4-cite-1](#4-cite-1)

- `PCGGraphEditorWindow.graphView` 是 private 字段，需要通过新增 `GetGraphView()` 公开 [4-cite-2](#4-cite-2)

- `PCGGraphView.FindNodeVisual()` 是按 nodeId 查找的模板，新增的 `FindNodeVisualByType()` 按类型名查找 [4-cite-3](#4-cite-3)

- `PCGGraphView.CreateNodeVisual()` 用于程序化创建节点 [4-cite-4](#4-cite-4)

- `PCGSceneObjectRef` 构造函数和 `Resolve()` 方法 [4-cite-5](#4-cite-5)

- `PCGGeometryToMesh.FromMesh()` 转换 Mesh 到 PCGGeometry [4-cite-6](#4-cite-6)

- `SceneSelectionInputNode` 的 transform 烘焙逻辑（需要复用） [4-cite-7](#4-cite-7)

- `PCGNodeVisual.SetPortDefaultValues()` 用于设置节点参数 [4-cite-8](#4-cite-8)


--- 


# 任务2
## 问题分析

`PCGNodePreviewWindow` 有两个问题：

**1. 材质丢失（紫红色）**：`OnEnable()` 中使用 `Shader.Find("Standard")` 创建预览材质。如果项目使用 URP 或 HDRP，"Standard" shader 不存在，Unity 回退到紫红色错误 shader。 [5-cite-0](#5-cite-0)

**2. 无渲染模式选项**：当前只有一种渲染方式——`_previewRenderUtility.DrawMesh()` 纯 Shaded 模式，没有 Wireframe 或 Shaded+Wireframe 选项。 [5-cite-1](#5-cite-1)

---

```json
{
  "task_id": "pcg_preview_window_material_and_render_modes",
  "title": "修复预览窗口材质丢失 + 添加渲染模式切换",
  "description": "PCGNodePreviewWindow 使用 Shader.Find('Standard') 创建预览材质，在 URP/HDRP 项目中 shader 不存在导致紫红色。同时缺少 Wireframe、Shaded+Wireframe 等渲染模式选项。",
  "repository": "No78Vino/pcg_for_unity",

  "files_to_modify": [
    {
      "path": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
      "description": "预览窗口主文件，需要修复材质创建逻辑并添加渲染模式切换",
      "changes": [
        {
          "id": "C1",
          "target": "新增渲染模式枚举",
          "description": "在类内部定义渲染模式枚举",
          "insert_before_line": 9,
          "new_code": "private enum PreviewRenderMode { Shaded, Wireframe, ShadedWireframe }"
        },
        {
          "id": "C2",
          "target": "新增字段",
          "description": "添加渲染模式状态字段和线框材质字段",
          "insert_after_line": 18,
          "new_fields": [
            "private PreviewRenderMode _renderMode = PreviewRenderMode.Shaded;",
            "private Material _wireMaterial;  // 用于绘制线框的纯色材质"
          ]
        },
        {
          "id": "C3",
          "target": "OnEnable() 方法 - 修复材质创建",
          "current_line_range": [60, 69],
          "current_code": "private void OnEnable()\n{\n    _previewRenderUtility = new PreviewRenderUtility();\n    _previewRenderUtility.camera.fieldOfView = 30f;\n    _previewRenderUtility.camera.nearClipPlane = 0.01f;\n    _previewRenderUtility.camera.farClipPlane = 100f;\n\n    _previewMaterial = new Material(Shader.Find(\"Standard\"));\n    _previewMaterial.color = new Color(0.7f, 0.7f, 0.7f);\n}",
          "description": "替换 Shader.Find('Standard') 为智能 fallback 链：先尝试 URP Lit，再尝试 HDRP Lit，再尝试 Standard，最后 fallback 到内置 Default-Diffuse.mat。同时创建线框材质。",
          "new_code": "private void OnEnable()\n{\n    _previewRenderUtility = new PreviewRenderUtility();\n    _previewRenderUtility.camera.fieldOfView = 30f;\n    _previewRenderUtility.camera.nearClipPlane = 0.01f;\n    _previewRenderUtility.camera.farClipPlane = 100f;\n\n    _previewMaterial = CreatePreviewMaterial();\n\n    // 线框材质：使用 Unity 内置的 \"Hidden/Internal-Colored\" shader\n    // 这个 shader 在所有渲染管线中都可用\n    _wireMaterial = new Material(Shader.Find(\"Hidden/Internal-Colored\"));\n    _wireMaterial.SetInt(\"_SrcBlend\", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);\n    _wireMaterial.SetInt(\"_DstBlend\", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);\n    _wireMaterial.SetInt(\"_Cull\", (int)UnityEngine.Rendering.CullMode.Off);\n    _wireMaterial.SetInt(\"_ZWrite\", 0);\n}",
          "note": "CreatePreviewMaterial 是新增的辅助方法，见下方"
        },
        {
          "id": "C4",
          "target": "新增 CreatePreviewMaterial() 辅助方法",
          "description": "智能 shader fallback 链，确保在任何渲染管线下都能正确显示灰色材质",
          "insert_after": "OnEnable 方法之后",
          "new_code": "private static Material CreatePreviewMaterial()\n{\n    // 按优先级尝试不同渲染管线的 shader\n    string[] shaderCandidates = new[]\n    {\n        \"Universal Render Pipeline/Lit\",  // URP\n        \"HDRP/Lit\",                        // HDRP\n        \"Standard\",                         // Built-in\n    };\n\n    Shader shader = null;\n    foreach (var name in shaderCandidates)\n    {\n        shader = Shader.Find(name);\n        if (shader != null && !shader.name.Contains(\"Error\")) break;\n        shader = null;\n    }\n\n    Material mat;\n    if (shader != null)\n    {\n        mat = new Material(shader);\n        // 设置基础颜色（兼容不同管线的属性名）\n        if (mat.HasProperty(\"_BaseColor\"))\n            mat.SetColor(\"_BaseColor\", new Color(0.7f, 0.7f, 0.7f));  // URP/HDRP\n        if (mat.HasProperty(\"_Color\"))\n            mat.SetColor(\"_Color\", new Color(0.7f, 0.7f, 0.7f));      // Standard\n    }\n    else\n    {\n        // 终极 fallback：使用 Unity 内置默认材质\n        mat = new Material(AssetDatabase.GetBuiltinExtraResource<Material>(\"Default-Diffuse.mat\"));\n    }\n\n    return mat;\n}"
        },
        {
          "id": "C5",
          "target": "OnDisable() 方法 - 清理线框材质",
          "current_line_range": [71, 82],
          "description": "在 OnDisable 中添加 _wireMaterial 的清理",
          "add_to_method": "if (_wireMaterial != null)\n    DestroyImmediate(_wireMaterial);"
        },
        {
          "id": "C6",
          "target": "OnGUI() 方法 - 添加渲染模式工具栏",
          "current_line_range": [84, 140],
          "description": "在信息栏和几何体信息之间（或几何体信息之后），添加渲染模式切换工具栏。然后修改渲染逻辑支持三种模式。",
          "changes_detail": [
            {
              "location": "在 line 102（几何体信息 EndHorizontal）之后插入渲染模式工具栏",
              "new_code": "// 渲染模式工具栏\nEditorGUILayout.BeginHorizontal(EditorStyles.toolbar);\nGUILayout.Label(\"Render:\", EditorStyles.miniLabel, GUILayout.Width(45));\nif (GUILayout.Toggle(_renderMode == PreviewRenderMode.Shaded, \"Shaded\", EditorStyles.toolbarButton))\n    _renderMode = PreviewRenderMode.Shaded;\nif (GUILayout.Toggle(_renderMode == PreviewRenderMode.Wireframe, \"Wire\", EditorStyles.toolbarButton))\n    _renderMode = PreviewRenderMode.Wireframe;\nif (GUILayout.Toggle(_renderMode == PreviewRenderMode.ShadedWireframe, \"Shaded+Wire\", EditorStyles.toolbarButton))\n    _renderMode = PreviewRenderMode.ShadedWireframe;\nEditorGUILayout.EndHorizontal();"
            },
            {
              "location": "替换 line 125-139 的渲染逻辑",
              "current_code": "_previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);\n\nvar cameraPos = Quaternion.Euler(_rotationX, _rotationY, 0) * new Vector3(0, 0, -_zoom);\n_previewRenderUtility.camera.transform.position = cameraPos;\n_previewRenderUtility.camera.transform.LookAt(Vector3.zero);\n\n_previewRenderUtility.lights[0].intensity = 1.0f;\n_previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);\n_previewRenderUtility.lights[1].intensity = 0.5f;\n\n_previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMaterial, 0);\n_previewRenderUtility.camera.Render();\n\nvar resultTexture = _previewRenderUtility.EndPreview();\nGUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);",
              "new_code": "_previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);\n\nvar cameraPos = Quaternion.Euler(_rotationX, _rotationY, 0) * new Vector3(0, 0, -_zoom);\n_previewRenderUtility.camera.transform.position = cameraPos;\n_previewRenderUtility.camera.transform.LookAt(Vector3.zero);\n\n_previewRenderUtility.lights[0].intensity = 1.0f;\n_previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);\n_previewRenderUtility.lights[1].intensity = 0.5f;\n\n// Shaded 渲染（Shaded 和 ShadedWireframe 模式都需要）\nif (_renderMode == PreviewRenderMode.Shaded || _renderMode == PreviewRenderMode.ShadedWireframe)\n{\n    _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _previewMaterial, 0);\n}\n\n// Wireframe 渲染\nif (_renderMode == PreviewRenderMode.Wireframe || _renderMode == PreviewRenderMode.ShadedWireframe)\n{\n    GL.wireframe = true;\n    _previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _wireMaterial, 0);\n    // 注意：GL.wireframe 需要在 camera.Render() 之后重置\n}\n\n_previewRenderUtility.camera.Render();\nGL.wireframe = false;  // 确保重置，避免影响其他渲染\n\nvar resultTexture = _previewRenderUtility.EndPreview();\nGUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);",
              "note": "GL.wireframe 是 Unity 内置的全局开关，设为 true 后所有后续 DrawMesh 调用都以线框模式渲染。必须在 camera.Render() 之后重置为 false。对于 ShadedWireframe 模式，需要先画一遍 Shaded，再画一遍 Wireframe（两次 DrawMesh），这样线框会叠加在实体上。但由于 PreviewRenderUtility 的限制，更可靠的方式是：Shaded 模式正常渲染，然后设置 GL.wireframe=true 再渲染一次线框 pass。如果 GL.wireframe 在 PreviewRenderUtility 中不生效（某些 Unity 版本有此问题），则改用手动绘制线框的方式（见备选方案）。"
            }
          ]
        },
        {
          "id": "C7",
          "target": "新增 DrawWireframeManual() 备选方法",
          "description": "如果 GL.wireframe 在 PreviewRenderUtility 中不生效，使用 GL.Begin/GL.End 手动绘制线框作为备选",
          "new_code": "/// <summary>\n/// 备选方案：手动绘制线框（当 GL.wireframe 不生效时使用）\n/// 在 _previewRenderUtility.BeginPreview 和 camera.Render 之间调用\n/// </summary>\nprivate void DrawWireframeManual()\n{\n    if (_previewMesh == null || _wireMaterial == null) return;\n\n    _wireMaterial.SetPass(0);\n    GL.PushMatrix();\n    GL.MultMatrix(Matrix4x4.identity);\n    GL.Begin(GL.LINES);\n    GL.Color(new Color(0.0f, 0.0f, 0.0f, 0.6f));  // 半透明黑色线框\n\n    var verts = _previewMesh.vertices;\n    var tris = _previewMesh.triangles;\n    for (int i = 0; i < tris.Length; i += 3)\n    {\n        var v0 = verts[tris[i]];\n        var v1 = verts[tris[i + 1]];\n        var v2 = verts[tris[i + 2]];\n        GL.Vertex(v0); GL.Vertex(v1);\n        GL.Vertex(v1); GL.Vertex(v2);\n        GL.Vertex(v2); GL.Vertex(v0);\n    }\n\n    GL.End();\n    GL.PopMatrix();\n}",
          "note": "如果 GL.wireframe 方案在测试中正常工作，此方法可以保留但不调用。如果 GL.wireframe 不生效，则在 ShadedWireframe 和 Wireframe 模式中替换为调用此方法。Wireframe-only 模式下，先用深灰色背景清屏，再调用此方法。"
        }
      ]
    }
  ],

  "key_dependencies": {
    "PreviewRenderUtility": {
      "note": "Unity Editor 内置类，用于在 EditorWindow 中渲染 3D 预览。支持 DrawMesh、camera.Render 等。",
      "current_usage_line": 62
    },
    "GL.wireframe": {
      "note": "Unity 全局渲染开关。设为 true 后，后续所有 mesh 渲染以线框模式显示。必须在使用后重置为 false。在某些 Unity 版本的 PreviewRenderUtility 中可能不生效，需要测试。",
      "fallback": "如果不生效，使用 GL.Begin(GL.LINES) 手动绘制三角形边"
    },
    "Shader.Find": {
      "note": "在 URP 项目中 Shader.Find('Standard') 返回 null 或返回一个 error shader，导致紫红色。需要按渲染管线选择正确的 shader。",
      "current_usage_line": 67
    },
    "Hidden/Internal-Colored": {
      "note": "Unity 内置 shader，在所有渲染管线中都可用，适合用于纯色/线框渲染"
    },
    "AssetDatabase.GetBuiltinExtraResource<Material>": {
      "note": "终极 fallback，加载 Unity 内置的 Default-Diffuse.mat，保证不会出现紫红色"
    },
    "PCGCacheManager.GetOrCreateMesh": {
      "file": "Assets/PCGToolkit/Editor/Core/PCGCacheManager.cs",
      "line": 170,
      "note": "预览窗口通过此方法获取/缓存 Mesh，内部调用 PCGGeometryToMesh.Convert()"
    }
  },

  "implementation_notes": [
    "Shader.Find('Standard') 在 URP 项目中返回 null 是紫红色的根本原因。URP 的等效 shader 是 'Universal Render Pipeline/Lit'，HDRP 是 'HDRP/Lit'。",
    "CreatePreviewMaterial() 中检查 shader.name.Contains('Error') 是为了排除 Unity 返回的 error shader（某些版本 Shader.Find 不返回 null 而是返回 error shader）。",
    "URP Lit shader 的颜色属性名是 '_BaseColor'，Standard shader 是 '_Color'，所以两个都要设置。",
    "GL.wireframe 是最简单的线框渲染方式，但在 PreviewRenderUtility 中可能有兼容性问题。如果测试发现不生效，改用 DrawWireframeManual() 方法。",
    "ShadedWireframe 模式需要两次 DrawMesh：第一次正常 Shaded，第二次 GL.wireframe=true 叠加线框。线框颜色建议用半透明黑色，在浅色模型上清晰可见。",
    "渲染模式工具栏使用 EditorStyles.toolbarButton 风格的 Toggle，与 Unity 原生工具栏风格一致。",
    "_wireMaterial 使用 'Hidden/Internal-Colored' shader，这是 Unity 内置的，在所有管线中都可用，不需要额外依赖。"
  ],

  "testing": {
    "manual_test_cases": [
      {
        "id": "T1",
        "name": "材质不再紫红色",
        "steps": [
          "1. 在 URP 项目中打开 PCG Graph Editor",
          "2. 创建一个 BoxNode，执行图",
          "3. 双击 BoxNode 打开预览窗口",
          "4. 验证：模型显示为灰色，不是紫红色"
        ]
      },
      {
        "id": "T2",
        "name": "Built-in 管线兼容",
        "steps": [
          "1. 在 Built-in 渲染管线项目中重复 T1",
          "2. 验证：模型显示为灰色"
        ]
      },
      {
        "id": "T3",
        "name": "Shaded 模式（默认）",
        "steps": [
          "1. 打开预览窗口",
          "2. 验证：默认选中 'Shaded' 按钮",
          "3. 验证：模型以实体灰色渲染，有光照效果"
        ]
      },
      {
        "id": "T4",
        "name": "Wireframe 模式",
        "steps": [
          "1. 在预览窗口工具栏点击 'Wire' 按钮",
          "2. 验证：模型以线框模式渲染，只显示三角形边",
          "3. 验证：可以旋转和缩放查看线框"
        ]
      },
      {
        "id": "T5",
        "name": "Shaded+Wireframe 模式",
        "steps": [
          "1. 点击 'Shaded+Wire' 按钮",
          "2. 验证：模型同时显示实体和线框叠加",
          "3. 验证：线框在实体表面上清晰可见"
        ]
      },
      {
        "id": "T6",
        "name": "模式切换不影响旋转/缩放状态",
        "steps": [
          "1. 在 Shaded 模式下旋转模型到某个角度",
          "2. 切换到 Wire 模式",
          "3. 验证：视角保持不变",
          "4. 切换到 Shaded+Wire 模式",
          "5. 验证：视角仍然保持不变"
        ]
      },
      {
        "id": "T7",
        "name": "GL.wireframe 兼容性测试",
        "steps": [
          "1. 切换到 Wire 模式",
          "2. 如果线框不显示（GL.wireframe 不生效），需要改用 DrawWireframeManual() 备选方案",
          "3. 验证修改后线框正常显示"
        ]
      }
    ]
  }
}
```

关键代码引用：

当前的材质创建逻辑（问题根源）： [5-cite-2](#5-cite-2)

当前的渲染逻辑（只有 Shaded 模式）： [5-cite-3](#5-cite-3)

信息栏和几何体信息区域（渲染模式工具栏插入位置）： [5-cite-4](#5-cite-4)

OnDisable 清理逻辑（需要添加 _wireMaterial 清理）： [5-cite-5](#5-cite-5)

项目中其他地方使用 `Default-Diffuse.mat` 作为 fallback 的先例： [5-cite-6](#5-cite-6)