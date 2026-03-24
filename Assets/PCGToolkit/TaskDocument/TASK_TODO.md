Based on the existing task JSON format in `TASK_TODO.md`, here's the implementation plan converted to AI Agent-compatible JSON: [3-cite-0](#3-cite-0)

```json
{
  "plan_title": "Houdini 风格 Geometry SOP 窗口布局系统",
  "repository": "No78Vino/pcg_for_unity",
  "base_ref": "main",
  "summary": "实现一套模仿 Houdini Geometry SOP 操作时的经典 Layout 布局，在打开 PCG Node Editor 时自动触发。布局为：左上 SceneView（3D 视口）、左下 PCGGraphEditorWindow（节点编辑器）、右侧 PCGNodeInspectorWindow（参数面板）。",
  "layout_spec": {
    "description": "Houdini Geometry SOP 经典三栏布局",
    "diagram": "+---------------------------+------------------+\n|                           |                  |\n|     SceneView             | PCGInspector     |\n|     (3D Viewport)         | (Parameters)     |\n|                           |                  |\n+---------------------------+                  |\n|                           |                  |\n|  PCGGraphEditorWindow     |                  |\n|  (Network Editor)         |                  |\n|                           |                  |\n+---------------------------+------------------+",
    "panels": [
      {
        "name": "SceneView",
        "houdini_equivalent": "Scene Viewer",
        "unity_class": "UnityEditor.SceneView",
        "position": "top-left",
        "width_ratio": 0.75,
        "height_ratio": 0.5
      },
      {
        "name": "PCGGraphEditorWindow",
        "houdini_equivalent": "Network Editor",
        "unity_class": "PCGToolkit.Graph.PCGGraphEditorWindow",
        "position": "bottom-left",
        "width_ratio": 0.75,
        "height_ratio": 0.5
      },
      {
        "name": "PCGNodeInspectorWindow",
        "houdini_equivalent": "Parameter Editor",
        "unity_class": "PCGToolkit.Graph.PCGNodeInspectorWindow",
        "position": "right",
        "width_ratio": 0.25,
        "height_ratio": 1.0
      }
    ]
  },
  "tasks": [
    {
      "id": "LAYOUT-1",
      "title": "创建 PCGHoudiniLayout 布局管理器",
      "priority": "high",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGHoudiniLayout.cs",
      "current_state": "项目中不存在任何窗口布局管理逻辑。PCGGraphEditorWindow.OnEnable() 仅自动打开 PCGNodeInspectorWindow，但不控制窗口位置和大小。所有窗口由用户手动拖拽排列。",
      "target_state": "新建 PCGHoudiniLayout 静态工具类，提供 ApplyLayout() 方法，自动将 SceneView、PCGGraphEditorWindow、PCGNodeInspectorWindow 排列为 Houdini 风格布局。支持反射 Docking（优先）和浮动窗口位置排列（fallback）两种策略。",
      "changes": [
        {
          "action": "新建 PCGHoudiniLayout.cs",
          "details": [
            "命名空间 PCGToolkit.Graph",
            "静态类 PCGHoudiniLayout",
            "常量 RightPanelWidthRatio = 0.25f（Inspector 占屏幕宽度 25%）",
            "常量 TopHeightRatio = 0.5f（SceneView 占左栏高度 50%）",
            "常量 PrefKey = 'PCGToolkit_AutoLayout'（EditorPrefs key，控制是否自动触发）"
          ]
        },
        {
          "action": "实现 ApplyLayout() 公开方法",
          "details": [
            "添加 [MenuItem('PCG Toolkit/Apply Houdini Layout')] 特性，支持菜单手动触发",
            "内部调用 ApplyLayoutInternal()"
          ]
        },
        {
          "action": "实现 ApplyLayoutIfFirstTime() 公开方法",
          "details": [
            "检查 EditorPrefs.GetBool(PrefKey, false)，若已触发过则跳过",
            "首次触发时设置 EditorPrefs.SetBool(PrefKey, true)",
            "使用 EditorApplication.delayCall 延迟调用 ApplyLayoutInternal()，确保 Unity 窗口初始化完成"
          ]
        },
        {
          "action": "实现 ResetLayoutPreference() 公开方法",
          "details": [
            "添加 [MenuItem('PCG Toolkit/Reset Layout Preference')] 特性",
            "调用 EditorPrefs.DeleteKey(PrefKey) 重置标记",
            "下次打开 Node Editor 时会重新自动触发布局"
          ]
        },
        {
          "action": "实现 ApplyLayoutInternal() 私有方法",
          "details": [
            "步骤 1：通过 EditorWindow.GetWindow<SceneView>() 获取/打开 SceneView",
            "步骤 2：通过 EditorWindow.GetWindow<PCGGraphEditorWindow>() 获取/打开节点编辑器",
            "步骤 3：通过 PCGNodeInspectorWindow.Open() 获取/打开 Inspector",
            "步骤 4：调用 TryReflectionDocking() 尝试反射 Docking",
            "步骤 5：若反射失败，调用 ArrangeFloatingWindows() 作为 fallback",
            "步骤 6：调用 nodeEditor.Focus() 聚焦节点编辑器",
            "整体包裹在 try-catch 中，失败时 Debug.LogWarning"
          ]
        },
        {
          "action": "实现 TryReflectionDocking() 私有方法",
          "details": [
            "通过 typeof(EditorWindow).Assembly.GetType() 获取 Unity 内部类型：'UnityEditor.DockArea'、'UnityEditor.SplitView'、'UnityEditor.ContainerWindow'",
            "通过 typeof(EditorWindow).GetField('m_Parent', BindingFlags.NonPublic | BindingFlags.Instance) 获取窗口的父 DockArea",
            "尝试使用内部 API 将三个窗口 Dock 到同一个 ContainerWindow 中，按 Houdini 布局排列",
            "如果任何反射步骤失败（类型不存在、字段不存在、API 变更），返回 false 触发 fallback",
            "需要 using System.Reflection"
          ]
        },
        {
          "action": "实现 ArrangeFloatingWindows() 私有方法（fallback）",
          "details": [
            "获取屏幕分辨率 Screen.currentResolution.width / height",
            "预留 margin = 40f 给系统任务栏",
            "计算 rightPanelW = Max(350f, usableW * 0.25f)",
            "计算 leftW = usableW - rightPanelW",
            "计算 topH = usableH * 0.5f, bottomH = usableH - topH",
            "设置 sceneView.position = new Rect(0, margin, leftW, topH)",
            "设置 nodeEditor.position = new Rect(0, margin + topH, leftW, bottomH)",
            "设置 inspector.position = new Rect(leftW, margin, rightPanelW, usableH)"
          ]
        }
      ],
      "acceptance_criteria": [
        "菜单 'PCG Toolkit > Apply Houdini Layout' 可手动触发布局",
        "菜单 'PCG Toolkit > Reset Layout Preference' 可重置自动触发标记",
        "三个窗口按 Houdini 风格排列：SceneView 左上、NodeEditor 左下、Inspector 右侧全高",
        "反射 Docking 失败时自动 fallback 到浮动窗口排列，不报错",
        "不同屏幕分辨率下布局比例合理"
      ]
    },
    {
      "id": "LAYOUT-2",
      "title": "修改 PCGGraphEditorWindow 自动触发布局",
      "priority": "high",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs",
      "current_state": "OnEnable() 方法（第 53-76 行）在窗口打开时构建 GraphView、Toolbar、Executor，并通过 delayCall 自动打开 Inspector。OpenWindow() 方法（第 45-51 行）返回 void。",
      "target_state": "OnEnable() 末尾添加 PCGHoudiniLayout.ApplyLayoutIfFirstTime() 调用，首次打开时自动应用 Houdini 布局。OpenWindow() 返回类型改为 PCGGraphEditorWindow，供布局管理器使用。",
      "changes": [
        {
          "action": "修改 OpenWindow() 返回类型",
          "line_range": "45-51",
          "details": [
            "将 public static void OpenWindow() 改为 public static PCGGraphEditorWindow OpenWindow()",
            "在方法末尾添加 return window;",
            "MenuItem 特性保持不变"
          ]
        },
        {
          "action": "在 OnEnable() 末尾添加自动布局调用",
          "line_range": "53-76",
          "details": [
            "在现有 EditorApplication.delayCall（第 68-75 行）之后添加：",
            "PCGHoudiniLayout.ApplyLayoutIfFirstTime();",
            "注意：ApplyLayoutIfFirstTime() 内部也使用 delayCall，所以会在 Inspector 打开之后执行"
          ]
        }
      ],
      "acceptance_criteria": [
        "首次通过菜单 'PCG Toolkit > Node Editor' 打开时，自动应用 Houdini 布局",
        "第二次及之后打开不再自动触发（除非通过 Reset Layout Preference 重置）",
        "OpenWindow() 返回窗口实例，不影响 MenuItem 功能",
        "现有的 Inspector 自动打开逻辑不受影响"
      ]
    }
  ],
  "execution_order": [
    {
      "step": 1,
      "task_ids": ["LAYOUT-1"],
      "reason": "先创建布局管理器，可通过菜单手动测试布局效果"
    },
    {
      "step": 2,
      "task_ids": ["LAYOUT-2"],
      "reason": "布局管理器验证通过后，再接入自动触发逻辑"
    }
  ],
  "files_to_create": [
    "Assets/PCGToolkit/Editor/Graph/PCGHoudiniLayout.cs"
  ],
  "files_to_modify": [
    "Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs"
  ],
  "dependencies": {
    "unity_namespaces": [
      "UnityEditor",
      "UnityEngine",
      "System",
      "System.Reflection"
    ],
    "project_references": [
      "PCGToolkit.Graph.PCGGraphEditorWindow (第 45-51 行 OpenWindow, 第 53-76 行 OnEnable)",
      "PCGToolkit.Graph.PCGNodeInspectorWindow (第 37-43 行 Open)"
    ]
  },
  "notes": [
    "Unity 没有公开的窗口 Docking API，TryReflectionDocking() 依赖内部类型 DockArea/SplitView/ContainerWindow，可能在不同 Unity 版本间不兼容",
    "ArrangeFloatingWindows() 作为可靠的 fallback，使用 EditorWindow.position 设置浮动窗口位置，所有 Unity 版本均支持",
    "EditorPrefs 的 PrefKey 是全局的（非项目级），如需项目级隔离可改用 PlayerSettings.productName 作为 key 前缀",
    "如果用户已有自定义窗口布局，自动触发可能打乱。ApplyLayoutIfFirstTime() 的 EditorPrefs 机制确保只触发一次，降低干扰"
  ]
}
```

这个 JSON 遵循了项目 `TASK_TODO.md` 中已有的任务格式规范： [3-cite-0](#3-cite-0)

关键结构对齐点：
- `id` / `title` / `priority` / `file` / `current_state` / `target_state` / `changes` / `acceptance_criteria` 与 OPT-1~5 格式一致 [3-cite-1](#3-cite-1)
- `execution_order` 定义了任务执行顺序和依赖关系 [3-cite-2](#3-cite-2)
- `files_to_create` / `files_to_modify` 明确列出文件变更范围 [3-cite-3](#3-cite-3)
- `changes[].line_range` 引用了 `PCGGraphEditorWindow.cs` 中的具体行号 [3-cite-4](#3-cite-4) [3-cite-5](#3-cite-5)

额外增加了 `layout_spec`（布局规格）、`dependencies`（依赖项）和 `notes`（注意事项）字段，为 Agent 提供更完整的上下文。