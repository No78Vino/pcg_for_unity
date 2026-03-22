# 第6轮迭代各 Batch 的详细实现思路指导方案

---

## Batch 1 — 场景引用基础设施

### A1: `PCGPortType` 新增 `SceneObject`

在 `PCGPortType` 枚举末尾追加 `SceneObject` 值。这是一个纯枚举扩展，不会破坏现有序列化（Unity 枚举按 int 序列化）。 [1-cite-0](#1-cite-0)

```
enum PCGPortType {
    ...,
    Any,
    SceneObject   // 新增
}
```

### A2: `PCGParamSchema` 新增 `ObjectType` 约束字段

在 `PCGParamSchema` 中新增一个 `System.Type ObjectType` 字段，用于约束 Inspector 中 `ObjectField` 的选择范围。同时新增一个 `bool AllowSceneObjects` 字段（默认 true），控制是否允许选择场景对象（vs 仅 Project 资产）。 [1-cite-1](#1-cite-1)

```
// PCGParamSchema 中新增：
Type ObjectType;           // typeof(GameObject), typeof(MeshFilter) 等
bool AllowSceneObjects = true;  // true = 场景对象, false = 仅 Project 资产
```

构造函数不需要改动（这两个字段用属性初始化器即可），节点定义时通过对象初始化器设置：

```
new PCGParamSchema("target", Input, SceneObject, "Target Object") {
    ObjectType = typeof(GameObject),
    AllowSceneObjects = true
}
```

### A3: Inspector 支持 `ObjectField` 控件

在 `PCGNodeInspectorWindow.CreateInspectorWidget` 的 `switch (schema.PortType)` 中新增 `case PCGPortType.SceneObject` 分支。 [1-cite-2](#1-cite-2)

实现思路：

```
case PCGPortType.SceneObject:
{
    // 1. 从 currentValue 恢复引用
    //    currentValue 存储的是 instanceID (int) 或 GlobalObjectId (string)
    var obj = ResolveSceneReference(currentValue);
    
    // 2. 创建 ObjectField
    var field = new ObjectField(schema.DisplayName);
    field.objectType = schema.ObjectType ?? typeof(GameObject);
    field.allowSceneObjects = schema.AllowSceneObjects;
    field.value = obj;
    
    // 3. 值变更回调
    field.RegisterValueChangedCallback(evt => {
        // 存储为 instanceID（运行时引用）
        // 同时存储 GlobalObjectId 字符串（持久化引用）
        var refData = new SceneObjectRef {
            instanceID = evt.newValue?.GetInstanceID() ?? 0,
            globalId = GlobalObjectId.GetGlobalObjectIdSlow(evt.newValue).ToString()
        };
        SyncValueToNode(nodeVisual, schema.Name, refData);
    });
    
    return field;
}
```

关键点：`ObjectField` 是 `UnityEditor.UIElements.ObjectField`，它原生支持 `allowSceneObjects` 属性，可以直接选择 Hierarchy 中的 GameObject。

### A4: `PCGContext` 新增场景引用字典

在 `PCGContext` 中新增一个字典，用于在执行期间持有场景对象的强引用。 [1-cite-3](#1-cite-3)

```
// PCGContext 新增：
Dictionary<string, UnityEngine.Object> SceneReferences = new();

// 辅助方法
public GameObject GetSceneGameObject(string key) {
    SceneReferences.TryGetValue(key, out var obj);
    return obj as GameObject;
}
```

### A5: `PCGNodeVisual` 端口着色与类型映射

在 `GetSystemType` 和 `GetPortColor` 中为 `SceneObject` 添加映射。 [1-cite-4](#1-cite-4)

```
// GetSystemType 新增：
case PCGPortType.SceneObject: return typeof(GameObject);

// GetPortColor 新增（建议橙色，与 Distribute 类似但更亮）：
case PCGPortType.SceneObject: return new Color(1.0f, 0.6f, 0.1f);

// GetPortTypeShortLabel 新增：
case PCGPortType.SceneObject: return "GO";
```

同时在 `PCGNodeInspectorWindow.GetPortLabelColor` 中也要加对应颜色。 [1-cite-5](#1-cite-5)

### A6: 序列化支持 — `SceneObjectRef` 数据结构

这是最关键的技术难点。场景对象的 `instanceID` 在 Editor 重启后会变，需要用 `GlobalObjectId` 做持久化。

**新建数据结构**（建议放在 `PCGParamHelper.cs` 同文件或新建 `PCGSceneObjectRef.cs`）：

```
[Serializable]
class SceneObjectRef {
    int instanceID;       // 运行时快速查找
    string globalObjectId; // 持久化标识（GlobalObjectId.ToString()）
    string hierarchyPath;  // 备用：Transform 层级路径（如 "Canvas/Panel/Button"）
}
```

**序列化**（`PCGParamHelper.SerializeParamValue` 和 `PCGGraphView.SerializeParamValue` 中新增分支）： [1-cite-6](#1-cite-6)

```
case SceneObjectRef ref:
    param.ValueType = "SceneObjectRef";
    param.ValueJson = JsonUtility.ToJson(ref);
```

**反序列化**（`PCGParamHelper.DeserializeParamValue` 中新增分支）： [1-cite-7](#1-cite-7)

```
case "SceneObjectRef":
    var ref = JsonUtility.FromJson<SceneObjectRef>(param.ValueJson);
    // 优先用 instanceID 查找（同一 session 内有效）
    // 失败则用 GlobalObjectId 恢复
    // 再失败则用 hierarchyPath 查找
    return ref;
```

**恢复引用的优先级链**：

```
Object ResolveSceneReference(SceneObjectRef ref):
    1. EditorUtility.InstanceIDToObject(ref.instanceID)  // 最快
    2. GlobalObjectId.TryParse → GlobalObjectId → Object  // 跨 session 可靠
    3. GameObject.Find(ref.hierarchyPath)                  // 最后兜底
```

### A7: 执行器桥接 — 注入场景引用

在 `PCGAsyncGraphExecutor.ExecuteNodeInternal` 中，收集参数阶段需要识别 `SceneObjectRef` 类型的参数，将其解析为实际的 `GameObject` 引用并注入 `PCGContext.SceneReferences`。 [1-cite-8](#1-cite-8)

```
// 在 "收集参数" 之后、"执行并计时" 之前，新增一个阶段：
foreach (var kvp in parameters) {
    if (kvp.Value is SceneObjectRef ref) {
        var go = ResolveSceneReference(ref);
        if (go != null) {
            _context.SceneReferences[$"{nodeData.NodeId}.{kvp.Key}"] = go;
            parameters[kvp.Key] = go;  // 直接替换为 GameObject 引用
        }
    }
}
```

这样节点的 `Execute` 方法中就可以直接从 `parameters` 拿到 `GameObject` 对象。

---

## Batch 2 — 场景输入节点

### B1: 完善 `FromMesh` 方法

当前 `PCGGeometryToMesh.FromMesh` 已有基础实现（顶点、三角形、法线、UV、颜色），但缺少 **多 Submesh → PrimGroup 映射**。 [1-cite-9](#1-cite-9)

需要补充的逻辑：

```
// 在 FromMesh 中，替换当前的单一三角形遍历：
for (int sub = 0; sub < mesh.subMeshCount; sub++) {
    var tris = mesh.GetTriangles(sub);
    string groupName = $"submesh_{sub}";
    var groupSet = new HashSet<int>();
    
    for (int i = 0; i < tris.Length; i += 3) {
        int primIdx = geo.Primitives.Count;
        geo.Primitives.Add(new int[] { tris[i], tris[i+1], tris[i+2] });
        groupSet.Add(primIdx);
    }
    
    if (groupSet.Count > 0)
        geo.PrimGroups[groupName] = groupSet;
}

// 同时将 Renderer 的材质路径写入 PrimAttrib "material"
// 这样 ConvertWithSubmeshes 反向转换时能恢复材质
```

### B2: `SceneObjectInputNode`

新建 `Nodes/Create/SceneObjectInputNode.cs`，这是核心的场景交互节点。

```
class SceneObjectInputNode : PCGNodeBase {
    Name => "SceneObjectInput"
    Category => Create
    
    Inputs => [
        // SceneObject 类型参数，Inspector 中渲染为 ObjectField
        new PCGParamSchema("target", Input, SceneObject, "Target Object") {
            ObjectType = typeof(GameObject),
            AllowSceneObjects = true
        },
        // 控制选项
        new PCGParamSchema("readUV", Input, Bool, "Read UV", default: true),
        new PCGParamSchema("readNormals", Input, Bool, "Read Normals", default: true),
        new PCGParamSchema("readColors", Input, Bool, "Read Colors", default: false),
        new PCGParamSchema("worldSpace", Input, Bool, "World Space", default: true),
    ]
    
    Outputs => [
        new PCGParamSchema("geometry", Output, Geometry, "Geometry"),
    ]
    
    Execute(ctx, inputs, params):
        // 1. 从 parameters 获取 GameObject（已被执行器解析）
        var go = params["target"] as GameObject;
        if (go == null) return empty;
        
        // 2. 获取 MeshFilter
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return empty;
        
        // 3. 调用 FromMesh 转换
        var geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh);
        
        // 4. 如果 worldSpace=true，将所有顶点从本地空间变换到世界空间
        if (worldSpace) {
            var matrix = go.transform.localToWorldMatrix;
            for (int i = 0; i < geo.Points.Count; i++)
                geo.Points[i] = matrix.MultiplyPoint3x4(geo.Points[i]);
            // 法线也要变换（用 inverse transpose）
        }
        
        // 5. 将 Transform 信息写入 Detail 属性
        geo.DetailAttribs.SetAttribute("source_position", go.transform.position);
        geo.DetailAttribs.SetAttribute("source_rotation", go.transform.rotation.eulerAngles);
        geo.DetailAttribs.SetAttribute("source_scale", go.transform.lossyScale);
        geo.DetailAttribs.SetAttribute("source_name", go.name);
        
        return SingleOutput("geometry", geo);
```

参考现有的 `ImportMeshNode`，它通过 `assetPath` 字符串加载 Project 中的 Mesh 资产。`SceneObjectInputNode` 的区别是直接引用场景中的活 GameObject。 [1-cite-10](#1-cite-10)

### B3: `ScenePointsInputNode`

用于读取场景中一组 GameObject 的位置/旋转/缩放作为点云。典型用途：选中场景中几个空物体作为分布锚点。

```
class ScenePointsInputNode : PCGNodeBase {
    Name => "ScenePointsInput"
    
    Inputs => [
        // 多个 SceneObject 输入（或一个父物体，读取所有子物体）
        new PCGParamSchema("parent", Input, SceneObject, "Parent Object") {
            ObjectType = typeof(GameObject)
        },
        new PCGParamSchema("includeChildren", Input, Bool, "Include Children", default: true),
        new PCGParamSchema("depth", Input, Int, "Depth", default: -1),  // -1 = 全部
    ]
    
    Execute(ctx, inputs, params):
        var parent = params["parent"] as GameObject;
        
        // 收集所有目标 Transform
        var transforms = new List<Transform>();
        if (includeChildren)
            CollectChildrenRecursive(parent.transform, transforms, depth);
        else
            transforms.Add(parent.transform);
        
        // 每个 Transform 生成一个 Point
        var geo = new PCGGeometry();
        foreach (var t in transforms) {
            geo.Points.Add(t.position);
        }
        
        // 将旋转和缩放写入 Point 属性
        var rotAttr = geo.PointAttribs.CreateAttribute("orient", Vector3);
        var scaleAttr = geo.PointAttribs.CreateAttribute("pscale", Vector3);
        var nameAttr = geo.PointAttribs.CreateAttribute("name", String);
        
        foreach (var t in transforms) {
            rotAttr.Values.Add(t.rotation.eulerAngles);
            scaleAttr.Values.Add(t.lossyScale);
            nameAttr.Values.Add(t.name);
        }
        
        return SingleOutput("geometry", geo);
```

---

## Batch 3 — 输出节点控制

### C1: Output 节点新增 `enabled` 参数

所有 7 个 Output 节点都需要修改。以 `ExportFBXNode` 为例： [1-cite-11](#1-cite-11)

思路：在 `Inputs` 数组的**最前面**插入一个 `enabled` Bool 参数，然后在 `Execute` 开头检查：

```
// Inputs 新增（放在第一个）：
new PCGParamSchema("enabled", Input, Bool, "Enabled", "是否启用输出", true)

// Execute 开头新增：
bool enabled = GetParamBool(parameters, "enabled", true);
if (!enabled) {
    ctx.Log("ExportFBX: 输出已禁用，跳过");
    // 仍然透传 Geometry，只是不执行输出动作
    return new Dictionary<string, PCGGeometry> {
        { "geometry", geo },
        { "fbxPath", null }
    };
}
```

需要修改的 7 个文件：
- `ExportFBXNode.cs`
- `ExportMeshNode.cs`
- `SavePrefabNode.cs`
- `AssemblePrefabNode.cs`
- `SaveMaterialNode.cs`
- `SaveSceneNode.cs`
- `LODGenerateNode.cs`


### C2: Inspector 增加 Export 按钮

在 `PCGNodeInspectorWindow.RebuildForNode` 中，检测当前节点的 `Category == Output`，如果是则在参数区域底部追加一个 "Export" 按钮。 [1-cite-12](#1-cite-12)

```
// 在 RebuildForNode 末尾、BuildPresetSection 之前：
if (pcgNode.Category == PCGNodeCategory.Output) {
    var exportBtn = new Button(() => {
        // 使用上次执行的缓存结果，仅重新执行该节点的输出逻辑
        ExecuteSingleOutputNode(nodeVisual);
    }) {
        text = "Export",
        style = { 
            backgroundColor = green,
            height = 30,
            fontSize = 14
        }
    };
    _paramContainer.Add(exportBtn);
}
```

`ExecuteSingleOutputNode` 的实现思路：

```
void ExecuteSingleOutputNode(PCGNodeVisual nodeVisual):
    // 1. 从 _asyncExecutor 获取该节点的上次执行结果
    var cachedResult = _asyncExecutor.GetNodeResult(nodeVisual.NodeId);
    if (cachedResult == null) {
        // 提示用户先执行一次图
        EditorUtility.DisplayDialog("需要先执行图");
        return;
    }
    
    // 2. 收集该节点的输入 Geometry（从缓存中）
    var inputGeos = cachedResult.Outputs;  // 上游传入的
    
    // 3. 收集参数
    var params = nodeVisual.GetPortDefaultValues();
    
    // 4. 强制 enabled=true，创建节点实例并执行
    params["enabled"] = true;
    var nodeInstance = Activator.CreateInstance(nodeTemplate.GetType());
    nodeInstance.Execute(ctx, inputGeos, params);
```

这需要 `PCGNodeInspectorWindow` 持有对 `PCGAsyncGraphExecutor` 的引用。可以通过 `BindGraphView` 时一并传入，或者通过 `PCGGraphEditorWindow` 中转。

### C3: Toolbar 增加 "Export All"

在 `PCGGraphEditorWindow.GenerateToolbar` 中，在 Execute 按钮旁新增 "Export All" 按钮。 [1-cite-13](#1-cite-13)

```
var exportAllBtn = new Button(() => OnExportAllClicked()) {
    text = L("btn.exportAll"),
    style = { backgroundColor = new Color(0.2f, 0.4f, 0.6f) }
};
toolbar.Add(exportAllBtn);
```

`OnExportAllClicked` 的实现思路：

```
void OnExportAllClicked():
    // 1. 检查是否有上次执行的缓存
    if (_asyncExecutor.State != Idle) return;
    
    // 2. 遍历图中所有节点
    var data = GetCurrentGraphData();
    foreach (var nodeData in data.Nodes) {
        var template = PCGNodeRegistry.GetNode(nodeData.NodeType);
        if (template.Category != Output) continue;
        
        // 3. 检查 enabled 参数
        var enabledParam = nodeData.Parameters.Find(p => p.Key == "enabled");
        if (enabledParam != null && !bool.Parse(enabledParam.ValueJson)) continue;
        
        // 4. 从缓存中获取该节点的输入，重新执行输出逻辑
        ExecuteSingleOutputNode(nodeData.NodeId);
    }
```

---

## Batch 4 — 场景实时预览

### D1: SceneView Gizmo 预览

新建 `Graph/PCGScenePreview.cs`，使用 `SceneView.duringSceneGui` 在 SceneView 中绘制 PCG 结果。

```
[InitializeOnLoad]
static class PCGScenePreview {
    static PCGGeometry _previewGeo;
    static Mesh _previewMesh;
    static Material _previewMat;  // 半透明线框材质
    static bool _showWireframe = true;
    
    static PCGScenePreview() {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    // 由 PCGGraphEditorWindow 在节点执行完成后调用
    public static void SetPreviewGeometry(PCGGeometry geo) {
        _previewGeo = geo;
        // 转换为 Mesh 用于绘制
        _previewMesh = PCGGeometryToMesh.Convert(geo);
    }
    
    static void OnSceneGUI(SceneView sceneView) {
        if (_previewMesh == null) return;
        
        if (_showWireframe) {
            // 用 GL 或 Handles 绘制线框
            Handles.color = new Color(0, 1, 0.5f, 0.5f);
            // 遍历三角形画线
            var verts = _previewMesh.vertices;
            var tris = _previewMesh.triangles;
            for (int i = 0; i < tris.Length; i += 3) {
                Handles.DrawLine(verts[tris[i]], verts[tris[i+1]]);
                Handles.DrawLine(verts[tris[i+1]], verts[tris[i+2]]);
                Handles.DrawLine(verts[tris[i+2]], verts[tris[i]]);
            }
        } else {
            // 实体预览
            Graphics.DrawMeshNow(_previewMesh, Matrix4x4.identity);
        }
        
        // 在 SceneView 左上角画一个小 GUI 控制面板
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 200, 60));
        _showWireframe = GUILayout.Toggle(_showWireframe, "Wireframe");
        if (GUILayout.Button("Clear Preview"))
            ClearPreview();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
```

在 `PCGGraphEditorWindow` 的 `OnNodeCompleted` 回调中，当选中节点执行完成时调用 `PCGScenePreview.SetPreviewGeometry`。 [1-cite-14](#1-cite-14)

### D2: "Inject to Scene" 功能

在 Output 节点的 Inspector 中（C2 的 Export 按钮旁边），新增一个 "Inject to Scene" 按钮：

```
var injectBtn = new Button(() => {
    var geo = GetCachedGeometryForNode(nodeVisual.NodeId);
    if (geo == null) return;
    
    // 创建临时 GameObject（标记为 HideFlags.DontSave）
    var mesh = PCGGeometryToMesh.Convert(geo);
    var go = new GameObject("[PCG Preview] " + nodeVisual.PCGNode.DisplayName);
    go.hideFlags = HideFlags.DontSave;  // 不保存到场景
    go.AddComponent<MeshFilter>().sharedMesh = mesh;
    go.AddComponent<MeshRenderer>().sharedMaterial = defaultMat;
    
    // 添加一个标记组件，方便后续清理
    go.AddComponent<PCGPreviewMarker>();
    
    Selection.activeGameObject = go;  // 选中它方便查看
}) { text = "Inject to Scene" };
```

`PCGPreviewMarker` 是一个空的 MonoBehaviour，仅用于标记和批量清理：

```
// 清理所有预览物体
foreach (var marker in FindObjectsOfType<PCGPreviewMarker>())
    DestroyImmediate(marker.gameObject);
```

---

## Batch 5 — HDA 化封装

### E1: 参数暴露机制

在 `PCGParamSchema` 中新增 `bool Exposed` 字段（默认 false）。 [1-cite-15](#1-cite-15)

```
// PCGParamSchema 新增：
bool Exposed;  // 标记为 true 的参数会出现在 PCGGraphRunner 和 Graph Asset Inspector 中
```

在 `PCGNodeInspectorWindow` 中，每个参数行旁边增加一个小的 "Expose" 切换按钮（类似 Houdini 的锁头图标）。点击后将该参数标记为 Exposed，并将信息序列化到 `PCGGraphData` 中。

`PCGGraphData` 需要新增一个列表来存储暴露参数的元信息：

```
// PCGGraphData 新增：
[Serializable]
class PCGExposedParam {
    string nodeId;
    string paramName;
    string displayName;
    PCGPortType portType;
    string defaultValueJson;
}

List<PCGExposedParam> ExposedParameters = new();
```

### E2: `PCGGraphRunner` MonoBehaviour

新建 `Graph/PCGGraphRunner.cs`，这是 HDA 的 Unity 等价物。

```
[ExecuteInEditMode]
class PCGGraphRunner : MonoBehaviour {
[SerializeField] PCGGraphData graphAsset;

    // 暴露参数的运行时值（序列化存储）
    [SerializeField] List<PCGSerializedParameter> parameterOverrides;
    
    // 生成的子物体引用
    [SerializeField] GameObject generatedRoot;
    
    // Inspector 中的 "Cook" 按钮触发
    public void Cook() {
        // 1. 克隆 graphAsset 的数据（不修改原始资产）
        var data = Instantiate(graphAsset);
        
        // 2. 将 parameterOverrides 注入到对应节点的参数中
        foreach (var override in parameterOverrides) {
            // 找到对应的 ExposedParam → nodeId + paramName
            // 修改 data.Nodes 中对应节点的 Parameters
        }
        
        // 3. 创建执行上下文
        var ctx = new PCGContext();
        // 将当前 GameObject 注入 SceneReferences（作为默认的场景锚点）
        ctx.SceneReferences["self"] = this.gameObject;
        
        // 4. 执行图（同步执行，因为是在 Inspector 按钮触发）
        var executor = new PCGGraphExecutor(data);
        executor.Execute(ctx);
        
        // 5. 清理旧的生成物
        if (generatedRoot != null)
            DestroyImmediate(generatedRoot);
        
        // 6. 将结果实例化为子物体
        generatedRoot = new GameObject("Generated");
        generatedRoot.transform.SetParent(this.transform);
        // 遍历所有 Output 节点的结果，转换为 Mesh 并创建子物体
    }
}
```

### E3: `PCGGraphRunner` 的自定义 Inspector

新建 `Graph/PCGGraphRunnerEditor.cs`：

```
[CustomEditor(typeof(PCGGraphRunner))]
class PCGGraphRunnerEditor : Editor {
    
    public override void OnInspectorGUI() {
        var runner = (PCGGraphRunner)target;
        
        // 1. Graph Asset 字段
        EditorGUI.BeginChangeCheck();
        runner.graphAsset = (PCGGraphData)EditorGUILayout.ObjectField(
            "PCG Graph", runner.graphAsset, typeof(PCGGraphData), false);
        if (EditorGUI.EndChangeCheck()) {
            // 图资产变更时，重新生成暴露参数列表
            RebuildExposedParameters(runner);
        }
        
        if (runner.graphAsset == null) {
            EditorGUILayout.HelpBox("请拖入一个 PCG Graph 资产", MessageType.Info);
            return;
        }
        
        // 2. 绘制暴露参数
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Exposed Parameters", EditorStyles.boldLabel);
        DrawExposedParameters(runner);
        
        // 3. 操作按钮
        EditorGUILayout.Space();
        
        // Cook 按钮 — 执行图并将结果注入场景
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("Cook", GUILayout.Height(30))) {
            runner.Cook();
        }
        
        // Export 按钮 — 仅执行 Output 节点的输出动作
        GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
        if (GUILayout.Button("Export All", GUILayout.Height(25))) {
            runner.ExportAll();
        }
        
        // Clear 按钮 — 清理生成的子物体
        GUI.backgroundColor = new Color(0.7f, 0.3f, 0.3f);
        if (GUILayout.Button("Clear Generated", GUILayout.Height(25))) {
            runner.ClearGenerated();
        }
        
        GUI.backgroundColor = Color.white;
        
        // 4. 打开 Graph Editor 的快捷按钮
        if (GUILayout.Button("Open in Graph Editor")) {
            PCGGraphEditorWindow.OpenWindow();
            // 加载当前图
        }
    }
}
```

`DrawExposedParameters` 的实现思路 — 根据 `PCGGraphData.ExposedParameters` 列表动态绘制参数控件：

```
void DrawExposedParameters(PCGGraphRunner runner):
    foreach (var exposed in runner.graphAsset.ExposedParameters) {
        // 从 runner.parameterOverrides 中查找当前值
        var override = runner.parameterOverrides.Find(p => 
            p.Key == $"{exposed.nodeId}.{exposed.paramName}");
        
        // 根据 portType 绘制对应的 EditorGUI 控件
        switch (exposed.portType) {
            case Float:
                float fVal = override != null ? float.Parse(override.ValueJson) : defaultVal;
                fVal = EditorGUILayout.FloatField(exposed.displayName, fVal);
                SaveOverride(runner, exposed, fVal);
                break;
            case Int:
                // EditorGUILayout.IntField(...)
                break;
            case Bool:
                // EditorGUILayout.Toggle(...)
                break;
            case String:
                // EditorGUILayout.TextField(...)
                break;
            case Vector3:
                // EditorGUILayout.Vector3Field(...)
                break;
            case Color:
                // EditorGUILayout.ColorField(...)
                break;
            case SceneObject:
                // EditorGUILayout.ObjectField(..., allowSceneObjects: true)
                // 这里是关键：HDA 化的核心体验
                // 用户可以直接在 Inspector 中拖入场景物体
                break;
        }
    }
```

`RebuildExposedParameters` 的实现思路 — 当图资产变更时，扫描图中所有标记为 `Exposed` 的参数，生成 `parameterOverrides` 的初始列表：

```
void RebuildExposedParameters(PCGGraphRunner runner):
    runner.parameterOverrides.Clear();
    
    if (runner.graphAsset == null) return;
    
    foreach (var exposed in runner.graphAsset.ExposedParameters) {
        // 用默认值初始化
        runner.parameterOverrides.Add(new PCGSerializedParameter {
            Key = $"{exposed.nodeId}.{exposed.paramName}",
            ValueType = exposed.portType.ToString(),
            ValueJson = exposed.defaultValueJson
        });
    }
    
    EditorUtility.SetDirty(runner);
```

---

### E4: `PCGGraphRunner.Cook()` 的完整执行流程

这是 HDA 化的核心方法，需要将图的执行与场景物体的生成串联起来。 [2-cite-1](#2-cite-1)

```
public void Cook():
    // 1. 克隆图数据（不修改原始资产）
    var data = Instantiate(graphAsset);
    
    // 2. 将 parameterOverrides 注入到对应节点的参数中
    foreach (var override in parameterOverrides) {
        // key 格式: "{nodeId}.{paramName}"
        var parts = override.Key.Split('.');
        var nodeId = parts[0];
        var paramName = parts[1];
        
        var nodeData = data.Nodes.Find(n => n.NodeId == nodeId);
        if (nodeData == null) continue;
        
        var param = nodeData.Parameters.Find(p => p.Key == paramName);
        if (param != null) {
            param.ValueJson = override.ValueJson;
            param.ValueType = override.ValueType;
        } else {
            nodeData.Parameters.Add(new PCGSerializedParameter {
                Key = paramName,
                ValueJson = override.ValueJson,
                ValueType = override.ValueType
            });
        }
    }
    
    // 3. 创建执行上下文
    var ctx = new PCGContext();
    
    // 注入 self 引用（当前 GameObject）
    ctx.SceneReferences["self"] = this.gameObject;
    
    // 解析所有 SceneObject 类型的参数覆盖
    foreach (var override in parameterOverrides) {
        if (override.ValueType == "SceneObjectRef") {
            var ref = JsonUtility.FromJson<SceneObjectRef>(override.ValueJson);
            var go = ResolveSceneReference(ref);
            if (go != null) {
                ctx.SceneReferences[override.Key] = go;
            }
        }
    }
    
    // 4. 使用同步执行器执行图
    //    （PCGGraphRunner 在 Inspector 按钮触发，不需要分帧异步）
    var executor = new PCGGraphExecutor(data);
    executor.Execute(ctx);
    
    // 5. 清理旧的生成物
    ClearGenerated();
    
    // 6. 收集所有 Output 节点的结果，转换为场景物体
    generatedRoot = new GameObject("[PCG Generated]");
    generatedRoot.transform.SetParent(this.transform);
    generatedRoot.transform.localPosition = Vector3.zero;
    
    // 遍历图中所有节点，找到 Output 类型的节点
    foreach (var nodeData in data.Nodes) {
        var template = PCGNodeRegistry.GetNode(nodeData.NodeType);
        if (template == null || template.Category != PCGNodeCategory.Output) continue;
        
        // 检查 enabled 参数
        var enabledParam = nodeData.Parameters.Find(p => p.Key == "enabled");
        if (enabledParam != null && enabledParam.ValueJson.ToLower() == "false") continue;
        
        // 从执行器缓存中获取该节点的输入 Geometry
        // 注意：需要 PCGGraphExecutor 暴露 _nodeOutputs 或提供 GetNodeOutput 方法
        var outputs = executor.GetNodeOutput(nodeData.NodeId);
        if (outputs == null) continue;
        
        foreach (var kvp in outputs) {
            if (kvp.Value == null || kvp.Value.Points.Count == 0) continue;
            
            // 转换为 Mesh 并创建子物体
            var meshResult = PCGGeometryToMesh.ConvertWithSubmeshes(kvp.Value);
            var child = new GameObject(nodeData.NodeType + "_" + kvp.Key);
            child.transform.SetParent(generatedRoot.transform);
            child.AddComponent<MeshFilter>().sharedMesh = meshResult.Mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = meshResult.Materials;
        }
    }
    
    Selection.activeGameObject = generatedRoot;
    SceneView.RepaintAll();
```

这里有一个关键依赖：`PCGGraphExecutor` 目前的 `_nodeOutputs` 是 `private` 的。需要为它添加一个公共访问方法（类似 `PCGAsyncGraphExecutor` 已有的 `GetNodeOutput`）： [2-cite-2](#2-cite-2)

```
// PCGGraphExecutor 新增公共方法：
public Dictionary<string, PCGGeometry> GetNodeOutput(string nodeId) {
    _nodeOutputs.TryGetValue(nodeId, out var outputs);
    return outputs;
}
```

`PCGAsyncGraphExecutor` 已经有这个方法了，保持一致即可。 [2-cite-3](#2-cite-3)

---

### E5: Inspector 中的 "Expose" 切换按钮

在 `PCGNodeInspectorWindow.CreateInspectorParam` 中，每个参数行旁边增加一个小的切换按钮。 [2-cite-4](#2-cite-4)

```
// 在 CreateInspectorParam 中，每个 paramRow 的右侧追加：
var exposeToggle = new Toggle("") {
    value = IsParamExposed(nodeVisual.NodeId, schema.Name),
    tooltip = "Expose this parameter to PCGGraphRunner"
};
exposeToggle.style.width = 20;
exposeToggle.RegisterValueChangedCallback(evt => {
    SetParamExposed(nodeVisual.NodeId, schema.Name, evt.newValue, schema);
});
paramRow.Add(exposeToggle);
```

`SetParamExposed` 的实现思路：

```
void SetParamExposed(string nodeId, string paramName, bool exposed, PCGParamSchema schema):
    // 获取当前图数据
    var graphData = _graphView.GetCurrentGraphData();
    
    if (exposed) {
        // 添加到 ExposedParameters 列表
        graphData.ExposedParameters.Add(new PCGExposedParam {
            nodeId = nodeId,
            paramName = paramName,
            displayName = schema.DisplayName,
            portType = schema.PortType,
            defaultValueJson = PCGParamHelper.SerializeParamValue(schema.DefaultValue)
        });
    } else {
        // 从列表中移除
        graphData.ExposedParameters.RemoveAll(p => 
            p.nodeId == nodeId && p.paramName == paramName);
    }
    
    // 标记脏状态
    _graphView.OnGraphChanged?.Invoke();
```

---

## 补充：关键串联点总结

以下是各 Batch 之间需要互相配合的关键串联点：

### 1. `PCGParamHelper` 的序列化/反序列化扩展

需要在两个地方同步修改： [2-cite-5](#2-cite-5)

以及 `PCGGraphView.SerializeParamValue`： [2-cite-6](#2-cite-6)

两处都需要新增 `SceneObjectRef` 类型的序列化/反序列化分支。

### 2. 执行器的 SceneObject 参数解析

`PCGAsyncGraphExecutor.ExecuteNodeInternal` 和 `PCGGraphExecutor.ExecuteNode` 都需要在"收集参数"之后、"执行节点"之前，增加 SceneObjectRef → GameObject 的解析阶段： [2-cite-7](#2-cite-7) [2-cite-8](#2-cite-8)

```
// 在两个执行器的参数收集之后、Execute 调用之前插入：
foreach (var kvp in parameters.ToList()) {  // ToList 避免遍历时修改
    if (kvp.Value is SceneObjectRef sceneRef) {
        var go = ResolveSceneReference(sceneRef);
        if (go != null) {
            parameters[kvp.Key] = go;
            _context.SceneReferences[$"{nodeData.NodeId}.{kvp.Key}"] = go;
        }
    }
}
```

### 3. `PCGNodeBase` 新增 `GetParamGameObject` 辅助方法

在 `PCGNodeBase` 中新增一个辅助方法，方便节点从 parameters 中获取 GameObject： [2-cite-9](#2-cite-9)

```
// PCGNodeBase 新增：
protected GameObject GetParamGameObject(
    Dictionary<string, object> parameters, string name) {
    if (parameters != null && parameters.TryGetValue(name, out var val)) {
        if (val is GameObject go) return go;
        if (val is Component comp) return comp.gameObject;
    }
    return null;
}
```

### 4. 搜索窗口的端口类型过滤

`PCGNodeSearchWindow` 中的端口类型过滤逻辑需要识别新的 `SceneObject` 类型： [2-cite-10](#2-cite-10)

在 `OnGraphViewChanged` 中的端口类型推断也需要新增 `SceneObject` 的映射： [2-cite-11](#2-cite-11)

```
// 新增映射：
else if (edge.output.portType == typeof(GameObject)) portType = PCGPortType.SceneObject;
```

### 5. 所有 7 个 Output 节点的 `enabled` 参数

需要修改的文件清单：


每个文件的修改模式完全一致：在 `Inputs` 数组最前面插入 `enabled` Bool 参数，在 `Execute` 开头检查。

---

## 藤曼生成器的端到端工作流示例

最后，用你提到的"藤曼生成器"场景来验证整个方案的完整性：

```
用户操作流程：

1. 创建 PCG Graph，设计藤曼生成逻辑
   - SceneObjectInput 节点 → 选择场景中的墙壁 Mesh
   - ScenePointsInput 节点 → 选择场景中的几个空物体作为藤曼生长起点
   - 后续节点：Scatter on Surface → Curve → Copy to Points → ...
   - Output: SavePrefab (enabled=true) + ExportFBX (enabled=false)

2. 标记暴露参数
   - 将 SceneObjectInput 的 "target" 参数标记为 Exposed
   - 将 ScenePointsInput 的 "parent" 参数标记为 Exposed
   - 将 Scatter 的 "density" 参数标记为 Exposed

3. 保存图为 VineGenerator.asset

4. 在场景中使用
   - 创建空 GameObject "VineGen"
   - 添加 PCGGraphRunner 组件
   - 拖入 VineGenerator.asset
   - Inspector 中出现 3 个暴露参数：
     - Target Object → 拖入场景中的墙壁
     - Parent Object → 拖入场景中的锚点父物体
     - Density → 滑条调节
   - 点击 Cook → 藤曼生成为子物体，直接在 SceneView 中可见
   - 调参 → 再次 Cook → 旧结果清除，新结果生成
   - 满意后点击 Export All → SavePrefab 输出 .prefab 文件
```

这就是完整的 HDA 化工作流，与 Houdini 的体验对齐。