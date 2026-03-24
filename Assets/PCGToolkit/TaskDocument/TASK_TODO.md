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


Now I have all the verified node parameter names from the source code. Let me produce the complete JSON, continuing from where the previous message was cut off.

接上一条被截断的 JSON，以下是完整版本：

```json
{
  "plan_title": "程序化匕首建模 Example — PCG Graph 构建脚本",
  "repository": "No78Vino/pcg_for_unity",
  "base_ref": "main",
  "summary": "创建一个 C# Editor 脚本 DaggerExampleBuilder.cs，通过 PCGGraphData API 程序化构建匕首建模节点图（19 个节点），保存为 .asset 文件。匕首由刀身（Box+Subdivide+Taper+Bend）、护手（Box+Transform）、手柄（Box+CatmullClark+Mountain+Transform）三部分组成，合并后生成 UV 和材质，输出为 Prefab。暴露 10 个参数供 PCGGraphRunner 使用。同时创建示例场景脚本 DaggerExampleScene.cs。",

  "dagger_anatomy": {
    "description": "匕首由三个部分组成，各自独立建模后通过 MergeNode 合并",
    "diagram": "刀身(Blade): Box → Subdivide(linear,3) → GroupCreate('blade') → Taper → Bend\n护手(Guard): Box → Transform\n手柄(Handle): Box → Subdivide(catmull-clark,2) → Mountain → Transform → GroupCreate('handle')\n合并: Merge(3 inputs) → Fuse → Normal → UVProject → MaterialAssign('blade') → MaterialAssign('handle') → SavePrefab",
    "parts": [
      {
        "name": "刀身 (Blade)",
        "description": "扁平长方体，线性细分增加几何密度，锥化形成刀尖，弯曲形成弧度",
        "nodes": ["Box", "Subdivide", "GroupCreate", "Taper", "Bend"]
      },
      {
        "name": "护手 (Guard)",
        "description": "宽扁长方体，位于刀身与手柄交界处",
        "nodes": ["Box", "Transform"]
      },
      {
        "name": "手柄 (Handle)",
        "description": "长方体经 Catmull-Clark 细分圆化，Mountain 噪声生成花纹，随机种子控制花纹形状",
        "nodes": ["Box", "Subdivide", "Mountain", "Transform", "GroupCreate"]
      },
      {
        "name": "组装 (Assembly)",
        "description": "合并三部分，融合重叠顶点，重算法线，立方体投影 UV，按分组分配材质，输出 Prefab",
        "nodes": ["Merge", "Fuse", "Normal", "UVProject", "MaterialAssign", "MaterialAssign", "SavePrefab"]
      }
    ]
  },

  "node_graph": {
    "total_nodes": 19,
    "nodes": [
      {
        "id": "blade_box",
        "node_type": "Box",
        "display_label": "Blade Base",
        "position": [0, 0],
        "parameters": {
          "sizeX": { "value": 0.04, "type": "float", "description": "刀身宽度" },
          "sizeY": { "value": 0.25, "type": "float", "description": "刀身高度（长度）" },
          "sizeZ": { "value": 0.005, "type": "float", "description": "刀身厚度" }
        },
        "exposed_params": ["sizeX", "sizeY", "sizeZ"]
      },
      {
        "id": "blade_subdiv",
        "node_type": "Subdivide",
        "display_label": "Blade Subdivide",
        "position": [250, 0],
        "parameters": {
          "iterations": { "value": 3, "type": "int", "description": "细分次数，增加几何体密度以支持平滑变形" },
          "algorithm": { "value": "linear", "type": "string", "description": "线性细分保持锐利边缘" }
        }
      },
      {
        "id": "blade_group",
        "node_type": "GroupCreate",
        "display_label": "Blade Group",
        "position": [500, 0],
        "parameters": {
          "groupName": { "value": "blade", "type": "string" },
          "groupType": { "value": "primitive", "type": "string" },
          "filter": { "value": "", "type": "string", "description": "留空=全选，将所有面加入 blade 组" }
        }
      },
      {
        "id": "blade_taper",
        "node_type": "Taper",
        "display_label": "Blade Taper (Edge)",
        "position": [750, 0],
        "parameters": {
          "scaleStart": { "value": 1.0, "type": "float", "description": "刀身底部保持原始宽度" },
          "scaleEnd": { "value": 0.05, "type": "float", "description": "刀尖收窄程度，越小越尖锐。0.05≈近乎尖锐，1.0=无锥化" },
          "axis": { "value": "y", "type": "string", "description": "沿 Y 轴（刀身长度方向）锥化" }
        },
        "exposed_params": ["scaleEnd"],
        "notes": "scaleEnd 作为'开刃长度'的近似参数：值越小，刀刃越尖锐（开刃越长）。0.0=完全尖锐，1.0=无开刃。TaperNode 对整个几何体沿 Y 轴从 scaleStart 线性插值到 scaleEnd。"
      },
      {
        "id": "blade_bend",
        "node_type": "Bend",
        "display_label": "Blade Curvature",
        "position": [1000, 0],
        "parameters": {
          "angle": { "value": 5.0, "type": "float", "description": "弯曲角度（度），正值向前弯，负值向后弯" },
          "upAxis": { "value": "y", "type": "string", "description": "沿 Y 轴弯曲" },
          "captureLength": { "value": 0.25, "type": "float", "description": "受弯曲影响的长度范围，应≈刀身高度" }
        },
        "exposed_params": ["angle"]
      },
      {
        "id": "guard_box",
        "node_type": "Box",
        "display_label": "Guard",
        "position": [0, 250],
        "parameters": {
          "sizeX": { "value": 0.08, "type": "float", "description": "护手宽度（比刀身宽）" },
          "sizeY": { "value": 0.015, "type": "float", "description": "护手高度（薄片）" },
          "sizeZ": { "value": 0.02, "type": "float", "description": "护手厚度" }
        }
      },
      {
        "id": "guard_transform",
        "node_type": "Transform",
        "display_label": "Guard Position",
        "position": [250, 250],
        "parameters": {
          "translate": { "value": [0, -0.125, 0], "type": "Vector3", "description": "将护手移到刀身底部（Y = -bladeHeight/2 = -0.125）" }
        }
      },
      {
        "id": "handle_box",
        "node_type": "Box",
        "display_label": "Handle Base",
        "position": [0, 500],
        "parameters": {
          "sizeX": { "value": 0.03, "type": "float", "description": "手柄宽度" },
          "sizeY": { "value": 0.12, "type": "float", "description": "手柄高度（长度）" },
          "sizeZ": { "value": 0.03, "type": "float", "description": "手柄深度" }
        },
        "exposed_params": ["sizeX", "sizeY", "sizeZ"],
        "notes": "使用 Box 而非 Tube，配合 Catmull-Clark 细分可圆化为类圆柱形，同时保留三维独立控制"
      },
      {
        "id": "handle_subdiv",
        "node_type": "Subdivide",
        "display_label": "Handle Subdivide",
        "position": [250, 500],
        "parameters": {
          "iterations": { "value": 2, "type": "int", "description": "2 次 Catmull-Clark 将方形截面圆化" },
          "algorithm": { "value": "catmull-clark", "type": "string", "description": "Catmull-Clark 细分使手柄圆润" }
        }
      },
      {
        "id": "handle_pattern",
        "node_type": "Mountain",
        "display_label": "Handle Pattern",
        "position": [500, 500],
        "parameters": {
          "height": { "value": 0.002, "type": "float", "description": "花纹凹凸深度" },
          "frequency": { "value": 8.0, "type": "float", "description": "花纹频率，越高越密" },
          "octaves": { "value": 3, "type": "int", "description": "分形叠加层数" },
          "lacunarity": { "value": 2.0, "type": "float" },
          "persistence": { "value": 0.5, "type": "float" },
          "seed": { "value": 42, "type": "int", "description": "随机种子，改变种子产生不同花纹" },
          "noiseType": { "value": "perlin", "type": "string" }
        },
        "exposed_params": ["seed", "height"]
      },
      {
        "id": "handle_transform",
        "node_type": "Transform",
        "display_label": "Handle Position",
        "position": [750, 500],
        "parameters": {
          "translate": { "value": [0, -0.19, 0], "type": "Vector3", "description": "将手柄移到护手下方（Y = -(bladeHeight/2 + guardHeight + handleHeight/2) = -(0.125+0.015+0.06) = -0.19）" }
        }
      },
      {
        "id": "handle_group",
        "node_type": "GroupCreate",
        "display_label": "Handle Group",
        "position": [1000, 500],
        "parameters": {
          "groupName": { "value": "handle", "type": "string" },
          "groupType": { "value": "primitive", "type": "string" },
          "filter": { "value": "", "type": "string" }
        }
      },
      {
        "id": "merge_all",
        "node_type": "Merge",
        "display_label": "Merge All Parts",
        "position": [1250, 250],
        "parameters": {},
        "notes": "MergeNode 的 input 端口支持 allowMultiple=true，三条分支的 geometry 输出都连接到此节点的 input 端口。MergeNode 会保留各分支的 PrimGroups（blade/handle）。"
      },
      {
        "id": "fuse",
        "node_type": "Fuse",
        "display_label": "Fuse Vertices",
        "position": [1500, 250],
        "parameters": {
          "distance": { "value": 0.001, "type": "float", "description": "合并距离阈值" }
        }
      },
      {
        "id": "normals",
        "node_type": "Normal",
        "display_label": "Recalculate Normals",
        "position": [1750, 250],
        "parameters": {
          "type": { "value": "point", "type": "string" },
          "cuspAngle": { "value": 60.0, "type": "float", "description": "锐角阈值，60° 保留刀刃硬边" },
          "weightByArea": { "value": true, "type": "bool" }
        }
      },
      {
        "id": "uv_project",
        "node_type": "UVProject",
        "display_label": "UV Projection",
        "position": [2000, 250],
        "parameters": {
          "projectionType": { "value": "cubic", "type": "string", "description": "立方体投影适合匕首的多面体形状" }
        }
      },
      {
        "id": "mat_blade",
        "node_type": "MaterialAssign",
        "display_label": "Blade Material",
        "position": [2250, 250],
        "parameters": {
          "group": { "value": "blade", "type": "string", "description": "仅对 blade 分组的面分配材质" },
          "materialPath": { "value": "", "type": "string", "description": "留空使用默认 PBR 材质（SavePrefabNode 会 fallback 到 Default-Diffuse.mat）" },
          "materialId": { "value": 0, "type": "int", "description": "材质 ID 0 = 刀身材质" }
        }
      },
      {
        "id": "mat_handle",
        "node_type": "MaterialAssign",
        "display_label": "Handle Material",
        "position": [2500, 250],
        "parameters": {
          "group": { "value": "handle", "type": "string", "description": "仅对 handle 分组的面分配材质" },
          "materialPath": { "value": "", "type": "string", "description": "留空使用默认 PBR 材质" },
          "materialId": { "value": 1, "type": "int", "description": "材质 ID 1 = 手柄材质，与刀身区分" }
        }
      },
      {
        "id": "save_prefab",
        "node_type": "SavePrefab",
        "display_label": "Output Dagger Prefab",
        "position": [2750, 250],
        "parameters": {
          "assetPath": { "value": "Assets/PCGOutput/Dagger.prefab", "type": "string" },
          "prefabName": { "value": "Dagger", "type": "string" },
          "addCollider": { "value": true, "type": "bool" },
          "convexCollider": { "value": true, "type": "bool" }
        }
      }
    ],

    "edges": [
      { "from": "blade_box",       "output_port": "geometry", "to": "blade_subdiv",    "input_port": "input" },
      { "from": "blade_subdiv",    "output_port": "geometry", "to": "blade_group",     "input_port": "input" },
      { "from": "blade_group",     "output_port": "geometry", "to": "blade_taper",     "input_port": "input" },
      { "from": "blade_taper",     "output_port": "geometry", "to": "blade_bend",      "input_port": "input" },
      { "from": "guard_box",       "output_port": "geometry", "to": "guard_transform", "input_port": "input" },
      { "from": "handle_box",      "output_port": "geometry", "to": "handle_subdiv",   "input_port": "input" },
      { "from": "handle_subdiv",   "output_port": "geometry", "to": "handle_pattern",  "input_port": "input" },
      { "from": "handle_pattern",  "output_port": "geometry", "to": "handle_transform","input_port": "input" },
      { "from": "handle_transform","output_port": "geometry", "to": "handle_group",    "input_port": "input" },
      { "from": "blade_bend",      "output_port": "geometry", "to": "merge_all",       "input_port": "input" },
      { "from": "guard_transform", "output_port": "geometry", "to": "merge_all",       "input_port": "input" },
      { "from": "handle_group",    "output_port": "geometry", "to": "merge_all",       "input_port": "input" },
      { "from": "merge_all",       "output_port": "geometry", "to": "fuse",            "input_port": "input" },
      { "from": "fuse",            "output_port": "geometry", "to": "normals",         "input_port": "input" },
      { "from": "normals",         "output_port": "geometry", "to": "uv_project",      "input_port": "input" },
      { "from": "uv_project",      "output_port": "geometry", "to": "mat_blade",       "input_port": "input" },
      { "from": "mat_blade",       "output_port": "geometry", "to": "mat_handle",      "input_port": "input" },
      { "from": "mat_handle",      "output_port": "geometry", "to": "save_prefab",     "input_port": "input" }
    ],

    "exposed_parameters": [
      { "node_id": "blade_box",      "param_name": "sizeX",    "display_name": "Blade Width",     "type": "float",  "default": 0.04 },
      { "node_id": "blade_box",      "param_name": "sizeY",    "display_name": "Blade Height",    "type": "float",  "default": 0.25 },
      { "node_id": "blade_box",      "param_name": "sizeZ",    "display_name": "Blade Depth",     "type": "float",  "default": 0.005 },
      { "node_id": "blade_taper",    "param_name": "scaleEnd", "display_name": "Edge Sharpness",  "type": "float",  "default": 0.05 },
      { "node_id": "handle_box",     "param_name": "sizeX",    "display_name": "Handle Width",    "type": "float",  "default": 0.03 },
      { "node_id": "handle_box",     "param_name": "sizeY",    "display_name": "Handle Height",   "type": "float",  "default": 0.12 },
      { "node_id": "handle_box",     "param_name": "sizeZ",    "display_name": "Handle Depth",    "type": "float",  "default": 0.03 },
      { "node_id": "blade_bend",     "param_name": "angle",    "display_name": "Blade Curvature", "type": "float",  "default": 5.0 },
      { "node_id": "handle_pattern", "param_name": "seed",     "display_name": "Pattern Seed",    "type": "int",    "default": 42 },
      { "node_id": "handle_pattern", "param_name": "height",   "display_name": "Pattern Depth",   "type": "float",  "default": 0.002 }
    ],

    "groups": [
      {
        "title": "Blade (刀身)",
        "node_ids": ["blade_box", "blade_subdiv", "blade_group", "blade_taper", "blade_bend"],
        "position": [-20, -40],
        "size": [1100, 200]
      },
      {
        "title": "Guard (护手)",
        "node_ids": ["guard_box", "guard_transform"],
        "position": [-20, 210],
        "size": [350, 200]
      },
      {
        "title": "Handle (手柄)",
        "node_ids": ["handle_box", "handle_subdiv", "handle_pattern", "handle_transform", "handle_group"],
        "position": [-20, 460],
        "size": [1100, 200]
      },
      {
        "title": "Assembly (组装)",
        "node_ids": ["merge_all", "fuse", "normals", "uv_project", "mat_blade", "mat_handle", "save_prefab"],
        "position": [1230, 210],
        "size": [1600, 200]
      }
    ],

    "sticky_notes": [
      {
        "title": "Procedural Dagger",
        "content": "程序化匕首生成器\n\n暴露参数：\n- 刀身尺寸 (3D)\n- 开刃长度 (scaleEnd)\n- 手柄尺寸 (3D)\n- 弯曲弧度\n- 花纹种子\n- 花纹深度\n\n使用方法：\n1. 保存此图为 .asset\n2. 场景中添加 PCGGraphRunner\n3. 拖入 Graph Asset\n4. Sync Exposed Params\n5. 调整参数 → Run Graph",
        "position": [-300, -100],
        "size": [260, 400]
      }
    ]
  },

  "tasks": [
    {
      "id": "DAGGER-1",
      "title": "创建 DaggerExampleBuilder.cs 图构建脚本",
      "priority": "high",
      "file": "Assets/PCGToolkit/Editor/Examples/DaggerExampleBuilder.cs",
      "current_state": "项目中不存在 Examples 目录和示例脚本。",
      "target_state": "新建 DaggerExampleBuilder.cs，提供 MenuItem 菜单项，一键生成匕首程序化建模的 PCGGraphData .asset 文件。",
      "changes": [
        {
          "action": "新建 DaggerExampleBuilder.cs",
          "details": [
            "命名空间 PCGToolkit.Examples",
            "静态类 DaggerExampleBuilder",
            "添加 [MenuItem('PCG Toolkit/Examples/Create Dagger Graph')] 特性"
          ]
        },
        {
          "action": "实现 CreateDaggerGraph() 方法",
          "details": [
            "步骤 1：创建 PCGGraphData 实例 — var data = ScriptableObject.CreateInstance<PCGGraphData>(); data.GraphName = 'ProceduralDagger';",
            "步骤 2：添加 19 个节点 — 使用 data.AddNode(nodeType, position) 方法，nodeType 使用节点的 Name 属性（如 'Box', 'Subdivide', 'Taper' 等），position 使用 node_graph.nodes[].position 中定义的坐标",
            "步骤 3：设置节点参数 — 对每个 PCGNodeData，通过 nodeData.Parameters.Add(new PCGSerializedParameter { Key=paramName, ValueJson=value, ValueType=type }) 设置参数。注意 ValueJson 的格式：float 用 '0.04'，int 用 '3'，bool 用 'true'/'false'，string 用原始字符串，Vector3 用 '0,0.125,0' 格式",
            "步骤 4：连接 18 条边 — 使用 data.AddEdge(outputNodeId, 'geometry', inputNodeId, 'input') 方法，按 node_graph.edges[] 中定义的连接关系",
            "步骤 5：标记暴露参数 — 向 data.ExposedParameters 添加 10 个 PCGExposedParamInfo { NodeId=nodeId, ParamName=paramName }，按 node_graph.exposed_parameters[] 中定义的列表",
            "步骤 6：添加 4 个 Groups — 向 data.Groups 添加 PCGGroupData，包含 Title、NodeIds、Position、Size",
            "步骤 7：添加 1 个 StickyNote — 向 data.StickyNotes 添加 PCGStickyNoteData",
            "步骤 8：保存资产 — AssetDatabase.CreateAsset(data, 'Assets/PCGToolkit/Examples/ProceduralDagger.asset'); AssetDatabase.SaveAssets();"
          ]
        },
        {
          "action": "参数序列化格式说明",
          "details": [
            "PCGSerializedParameter.ValueType 使用以下值：'float', 'int', 'bool', 'string', 'Vector3'",
            "PCGSerializedParameter.ValueJson 格式：",
            "  float: '0.04'（使用 InvariantCulture 格式化）",
            "  int: '3'",
            "  bool: 'true' 或 'false'",
            "  string: 直接使用字符串值，如 'blade'",
            "  Vector3: '0,-0.125,0'（逗号分隔，无空格）",
            "参考 PCGGraphRunnerBridge.SerializeValue() 的格式（Assets/PCGToolkit/Editor/Graph/PCGGraphRunnerBridge.cs 第 81-99 行）"
          ]
        },
        {
          "action": "节点 ID 管理",
          "details": [
            "data.AddNode() 会自动生成 GUID 作为 NodeId",
            "需要在代码中保存每个 AddNode() 返回的 PCGNodeData 引用，以便后续 AddEdge() 和 ExposedParameters 使用 NodeId",
            "建议使用 Dictionary<string, PCGNodeData> 按逻辑名称（如 'blade_box', 'blade_subdiv'）索引"
          ]
        }
      ],
      "acceptance_criteria": [
        "菜单 'PCG Toolkit > Examples > Create Dagger Graph' 可执行",
        "生成的 .asset 文件可在 PCG Node Editor 中打开，显示 19 个节点和 18 条连线",
        "节点按 4 个 Group 分组（Blade/Guard/Handle/Assembly）",
        "ExposedParameters 包含 10 个暴露参数",
        "在 PCG Node Editor 中执行图，能生成匕首几何体并输出 Prefab 到 Assets/PCGOutput/Dagger.prefab",
        "在 PCGGraphRunner 中 Sync Exposed Params 后显示 10 个可调参数"
      ]
    },
    {
      "id": "DAGGER-2",
      "title": "创建 DaggerExampleScene.cs 示例场景脚本",
      "priority": "medium",
      "file": "Assets/PCGToolkit/Editor/Examples/DaggerExampleScene.cs",
      "current_state": "项目中不存在示例场景设置脚本。",
      "target_state": "新建 DaggerExampleScene.cs，提供 MenuItem 菜单项，一键创建包含 PCGGraphRunner 的示例场景。",
      "changes": [
        {
          "action": "新建 DaggerExampleScene.cs",
          "details": [
            "命名空间 PCGToolkit.Examples",
            "静态类 DaggerExampleScene",
            "添加 [MenuItem('PCG Toolkit/Examples/Setup Dagger Scene')] 特性"
          ]
        },
        {
          "action": "实现 SetupScene() 方法",
          "details": [
            "步骤 1：查找 ProceduralDagger.asset — var graphAsset = AssetDatabase.LoadAssetAtPath<PCGGraphData>('Assets/PCGToolkit/Examples/ProceduralDagger.asset'); 如果不存在，提示用户先执行 'Create Dagger Graph'",
            "步骤 2：创建 GameObject — var go = new GameObject('Dagger Generator');",
            "步骤 3：添加 PCGGraphRunner 组件 — var runner = go.AddComponent<PCGGraphRunner>();",
            "步骤 4：设置 GraphAsset — runner.GraphAsset = graphAsset;",
            "步骤 5：设置 InstantiateOutput = true",
            "步骤 6：调用 Undo.RegisterCreatedObjectUndo(go, 'Create Dagger Generator') 支持撤销",
            "步骤 7：选中新创建的 GameObject — Selection.activeGameObject = go;",
            "步骤 8：提示用户 — Debug.Log('Dagger Generator created. Click Sync Exposed Params then Run Graph in the Inspector.');"
          ]
        }
      ],
      "acceptance_criteria": [
        "菜单 'PCG Toolkit > Examples > Setup Dagger Scene' 可执行",
        "场景中创建 'Dagger Generator' GameObject，挂载 PCGGraphRunner 组件",
        "PCGGraphRunner 的 GraphAsset 已指向 ProceduralDagger.asset",
        "在 Inspector 中点击 'Sync Exposed Params' 后显示 10 个参数",
        "点击 'Run Graph' 后在场景中生成匕首 Mesh"
      ]
    }
  ],
  
  "execution_order": [
    {
      "step": 1,
      "task_ids": ["DAGGER-1"],
      "reason": "先创建图构建脚本，生成 .asset 文件，可在 Node Editor 中验证节点图正确性"
    },
    {
      "step": 2,
      "task_ids": ["DAGGER-2"],
      "reason": "图资产验证通过后，再创建示例场景脚本，配置 PCGGraphRunner 和暴露参数"
    }
  ],

  "files_to_create": [
    "Assets/PCGToolkit/Editor/Examples/DaggerExampleBuilder.cs",
    "Assets/PCGToolkit/Editor/Examples/DaggerExampleScene.cs"
  ],

  "files_to_modify": [],

  "dependencies": {
    "unity_namespaces": [
      "UnityEditor",
      "UnityEngine",
      "System.Collections.Generic"
    ],
    "project_references": [
      "PCGToolkit.Graph.PCGGraphData (Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs) — AddNode(), AddEdge(), ExposedParameters, Groups, StickyNotes",
      "PCGToolkit.Graph.PCGNodeData (Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs) — NodeId, NodeType, Parameters, Position",
      "PCGToolkit.Graph.PCGSerializedParameter (Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs) — Key, ValueJson, ValueType",
      "PCGToolkit.Graph.PCGExposedParamInfo (Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs) — NodeId, ParamName",
      "PCGToolkit.Runtime.PCGGraphRunner (Assets/PCGToolkit/Runtime/PCGGraphRunner.cs) — GraphAsset, InstantiateOutput"
    ],
    "node_types_used": [
      "Box (Assets/PCGToolkit/Editor/Nodes/Create/BoxNode.cs)",
      "Subdivide (Assets/PCGToolkit/Editor/Nodes/Geometry/SubdivideNode.cs)",
      "GroupCreate (Assets/PCGToolkit/Editor/Nodes/Geometry/GroupCreateNode.cs)",
      "Taper (Assets/PCGToolkit/Editor/Nodes/Deform/TaperNode.cs)",
      "Bend (Assets/PCGToolkit/Editor/Nodes/Deform/BendNode.cs)",
      "Transform (Assets/PCGToolkit/Editor/Nodes/Geometry/TransformNode.cs)",
      "Mountain (Assets/PCGToolkit/Editor/Nodes/Deform/MountainNode.cs)",
      "Merge (Assets/PCGToolkit/Editor/Nodes/Geometry/MergeNode.cs)",
      "Fuse (Assets/PCGToolkit/Editor/Nodes/Geometry/FuseNode.cs)",
      "Normal (Assets/PCGToolkit/Editor/Nodes/Geometry/NormalNode.cs)",
      "UVProject (Assets/PCGToolkit/Editor/Nodes/UV/UVProjectNode.cs)",
      "MaterialAssign (Assets/PCGToolkit/Editor/Nodes/Geometry/MaterialAssignNode.cs)",
      "SavePrefab (Assets/PCGToolkit/Editor/Nodes/Output/SavePrefabNode.cs)"
    ]
  },

  "parameter_serialization_reference": {
    "description": "PCGSerializedParameter 的 ValueJson 格式参考，Agent 构建节点参数时必须遵循",
    "examples": {
      "float": { "Key": "sizeX", "ValueJson": "0.04", "ValueType": "float" },
      "int": { "Key": "iterations", "ValueJson": "3", "ValueType": "int" },
      "bool": { "Key": "addCollider", "ValueJson": "true", "ValueType": "bool" },
      "string": { "Key": "groupName", "ValueJson": "blade", "ValueType": "string" },
      "Vector3": { "Key": "translate", "ValueJson": "0,-0.125,0", "ValueType": "Vector3" }
    },
    "notes": "float 值使用 InvariantCulture 格式化（小数点用 '.' 不用 ','）。Vector3 用逗号分隔三个分量，无空格。"
  },

  "validation_checklist": [
    "菜单 'PCG Toolkit > Examples > Create Dagger Graph' 生成 Assets/PCGToolkit/Examples/ProceduralDagger.asset",
    "在 PCG Node Editor 中打开 ProceduralDagger.asset，显示 19 个节点、18 条连线、4 个 Group",
    "执行图后在 Assets/PCGOutput/ 下生成 Dagger.prefab",
    "Prefab 包含 MeshFilter + MeshRenderer + MeshCollider(convex)",
    "Mesh 顶点数 > 100（细分后应有足够几何密度）",
    "菜单 'PCG Toolkit > Examples > Setup Dagger Scene' 在场景中创建 Dagger Generator 对象",
    "PCGGraphRunner Inspector 中 Sync Exposed Params 后显示 10 个参数",
    "修改 Pattern Seed 后 Run Graph，手柄花纹发生变化",
    "修改 Blade Curvature 后 Run Graph，刀身弯曲弧度发生变化",
    "修改 Handle Width/Height/Depth 后 Run Graph，手柄尺寸相应变化"
  ],

  "notes": [
    "所有节点的 node_type 字符串必须与对应 Node 类的 Name 属性完全一致（如 BoxNode.Name = 'Box'，SubdivideNode.Name = 'Subdivide'）",
    "MergeNode 的 input 端口 allowMultiple=true，三条分支可以同时连接到同一个 'input' 端口",
    "GroupCreate 必须在 Merge 之前执行，MergeNode 会保留并偏移 PrimGroup 索引",
    "guard_transform 的 translate.y = -bladeHeight/2 = -0.125，handle_transform 的 translate.y = -(bladeHeight/2 + guardHeight + handleHeight/2) = -0.19，这些值基于默认参数计算，用户修改尺寸后位置不会自动联动（这是当前节点系统的限制，可在后续迭代中通过表达式节点解决）",
    "MaterialAssign 的 materialPath 留空时，SavePrefabNode 会 fallback 到 Shader.Find('Standard') 创建默认 PBR 材质",
    "Catmull-Clark 细分会将 Box 的方形截面圆化为近似圆柱形，2 次迭代足够平滑",
    "Mountain 节点的 seed 参数直接控制 Perlin 噪声的随机偏移，不同 seed 产生完全不同的花纹图案"
  ]
}
```