# 阶段 1 详细实施文档

**范围**: P0-1 ~ P0-7 (Critical Bugs) + P2-3 (版本信息)
**最新提交**: [`05f8f41`](https://github.com/No78Vino/pcg_for_unity/commit/05f8f419) (2026-03-20)

---

## P0-1: `PCGGraphView.OnKeyDown` 未检查文本输入焦点

**问题**: `PCGGraphEditorWindow.OnGlobalKeyDown` (line 305-309) 已正确检查焦点，但 `PCGGraphView.OnKeyDown` (line 368-382) 没有做同样的检查，导致在节点内联 TextField 中按 F 或 Delete 键时被拦截。

**修复代码** — 替换 `PCGGraphView.cs` line 368-382:

```csharp
private void OnKeyDown(KeyDownEvent evt)
{
    // 如果焦点在文本输入控件中，不拦截快捷键
    if (evt.target is TextField || evt.target is FloatField || 
        evt.target is IntegerField || evt.target is TextInput ||
        (evt.target is VisualElement ve && ve.GetFirstAncestorOfType<TextField>() != null) ||
        (evt.target is VisualElement ve2 && ve2.GetFirstAncestorOfType<FloatField>() != null) ||
        (evt.target is VisualElement ve3 && ve3.GetFirstAncestorOfType<IntegerField>() != null))
        return;

    // F: Frame All
    if (evt.keyCode == KeyCode.F)
    {
        FrameAll();
        evt.StopPropagation();
    }
    // Delete: 删除选中
    else if (evt.keyCode == KeyCode.Delete)
    {
        DeleteSelection();
        evt.StopPropagation();
    }
}
```

> 注意：UIElements 中 `evt.target` 可能是 TextField 内部的子元素（如 `TextInput`），所以需要用 `GetFirstAncestorOfType` 向上查找。如果 C# 不允许多次 `is VisualElement ve` 模式匹配，可以提取为辅助方法：

```csharp
private bool IsTargetInTextInput(EventBase evt)
{
    if (evt.target is not VisualElement target) return false;
    return target is TextField || target is FloatField || target is IntegerField ||
           target.GetFirstAncestorOfType<TextField>() != null ||
           target.GetFirstAncestorOfType<FloatField>() != null ||
           target.GetFirstAncestorOfType<IntegerField>() != null;
}
```

---

## P0-2: 右键菜单 "Create Node" 坐标错误

**问题**: `BuildContextualMenu` line 134 将 `evt.mousePosition`（GraphView 局部坐标）传给了 `screenMousePosition`，但 `SearchWindow.Open` 需要的是屏幕坐标。

**修复代码** — 替换 `PCGGraphView.cs` line 130-136:

```csharp
evt.menu.AppendAction("Create Node", _ => 
{
    // evt.mousePosition 是 GraphView 局部坐标，需要转换为屏幕坐标
    var screenPos = GUIUtility.GUIToScreenPoint(
        evt.originalMousePosition != Vector2.zero 
            ? evt.originalMousePosition 
            : Event.current?.mousePosition ?? evt.mousePosition);
    
    nodeCreationRequest?.Invoke(new NodeCreationContext()
    {
        screenMousePosition = screenPos
    });
});
```

> **注意**: 在 `ContextualMenuPopulateEvent` 回调中，`evt.mousePosition` 是 VisualElement 局部坐标。但 `AppendAction` 的 lambda 是延迟执行的（用户点击菜单项时才触发），此时 `Event.current` 可能已经不可用。更稳妥的做法是在 `BuildContextualMenu` 入口处就捕获屏幕坐标：

```csharp
public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
{
    base.BuildContextualMenu(evt);
    
    var localMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
    
    // 在事件处理时立即捕获屏幕坐标
    var screenMousePos = _editorWindow != null 
        ? _editorWindow.position.position + evt.mousePosition 
        : evt.mousePosition;
    
    evt.menu.AppendAction("Create Node", _ => 
    {
        nodeCreationRequest?.Invoke(new NodeCreationContext()
        {
            screenMousePosition = screenMousePos
        });
    });
    
    // ... 其余不变
}
```

---

## P0-3: `ExtrudeNode` 侧面索引计算错误

**问题**: line 122-127 先计算了一个复杂的 `prevIdx`，然后立即在 line 126-127 覆盖为 `(d-1)*prim.Length + i`。但这个简化公式也是错误的——它假设当前面的所有层级顶点从 `result.Points` 的索引 0 开始，但实际上每个面（`primIdx`）的顶点是追加到 `result.Points` 末尾的。

**分析**: 对于每个面 `primIdx`，循环 `d = 0..divisions` 创建 `(divisions+1)` 层顶点，每层 `prim.Length` 个。当前面的第一个顶点在 `result.Points` 中的起始偏移是 `baseOffset`（即进入该面循环前 `result.Points.Count` 的值）。

**修复代码** — 替换 `PCGGraphView.cs` 中 `ExtrudeNode.Execute` 的核心循环 (line 77-145):

```csharp
foreach (int primIdx in primsToExtrude)
{
    var prim = geo.Primitives[primIdx];
    if (prim.Length < 3) continue;

    Vector3 normal = CalculateFaceNormal(geo.Points, prim);

    Vector3 center = Vector3.zero;
    foreach (int idx in prim) center += geo.Points[idx];
    center /= prim.Length;

    // 记录当前面的顶点在 result.Points 中的起始偏移
    int baseOffset = result.Points.Count;

    for (int d = 0; d <= divisions; d++)
    {
        float t = (float)d / divisions;
        float offset = distance * t;
        float insetAmount = inset * t;

        int[] layerVertices = new int[prim.Length];
        for (int i = 0; i < prim.Length; i++)
        {
            Vector3 origPos = geo.Points[prim[i]];
            Vector3 toCenter = center - origPos;
            Vector3 newPos = origPos + normal * offset + toCenter.normalized * insetAmount;

            int newIdx = result.Points.Count;
            result.Points.Add(newPos);
            layerVertices[i] = newIdx;

            if (!extrudedVertices.ContainsKey(prim[i]))
                extrudedVertices[prim[i]] = new List<int>();
            extrudedVertices[prim[i]].Add(newIdx);
        }

        // 创建侧面（除了第一层）
        if (d > 0 && outputSide)
        {
            for (int i = 0; i < prim.Length; i++)
            {
                int next = (i + 1) % prim.Length;
                // 前一层的顶点索引 = baseOffset + (d-1)*prim.Length + i
                int prevIdx = baseOffset + (d - 1) * prim.Length + i;
                int prevNextIdx = baseOffset + (d - 1) * prim.Length + next;
                
                result.Primitives.Add(new int[] { prevIdx, prevNextIdx, layerVertices[next], layerVertices[i] });
            }
        }
    }

    // 输出顶面
    if (outputFront)
    {
        int lastLayerStart = baseOffset + divisions * prim.Length;
        int[] frontPrim = new int[prim.Length];
        for (int i = 0; i < prim.Length; i++)
        {
            frontPrim[i] = lastLayerStart + (prim.Length - 1 - i);
        }
        result.Primitives.Add(frontPrim);
    }
}
```

关键改动：
1. 在每个面循环开始时记录 `baseOffset = result.Points.Count`
2. 侧面索引使用 `baseOffset + (d-1)*prim.Length + i` 而非裸 `(d-1)*prim.Length + i`
3. 顶面索引也使用 `baseOffset + divisions * prim.Length` 而非裸 `divisions * prim.Length`

---

## P0-4: `MountainNode` 法线估算错误

**问题**: line 117-120 使用 `p.normalized`（点到原点方向）作为法线，对非中心对称几何体完全错误。

**修复思路**:
1. 优先从 `PointAttribs` 读取 "N" 属性
2. 如果没有，从相邻面计算顶点法线（面积加权平均）
3. 如果没有面，fallback 到 Y 轴

**修复代码** — 在 `MountainNode.cs` 中，在 `Execute` 方法的 for 循环之前，预计算所有顶点法线：

```csharp
// ---- 在 for 循环之前插入 ----

// 预计算顶点法线
Vector3[] vertexNormals = new Vector3[geo.Points.Count];

// 优先从 PointAttribs 读取 "N"
var normalAttr = geo.PointAttribs.GetAttribute("N");
if (normalAttr != null && normalAttr.Values.Count == geo.Points.Count)
{
    for (int i = 0; i < geo.Points.Count; i++)
    {
        vertexNormals[i] = (normalAttr.Values[i] is Vector3 n) ? n : Vector3.up;
    }
}
else if (geo.Primitives.Count > 0)
{
    // 从相邻面计算顶点法线（面积加权平均）
    for (int i = 0; i < geo.Points.Count; i++)
        vertexNormals[i] = Vector3.zero;

    foreach (var prim in geo.Primitives)
    {
        if (prim.Length < 3) continue;
        Vector3 v0 = geo.Points[prim[0]];
        Vector3 v1 = geo.Points[prim[1]];
        Vector3 v2 = geo.Points[prim[2]];
        Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0); // 未归一化 = 面积加权
        
        foreach (int idx in prim)
        {
            vertexNormals[idx] += faceNormal;
        }
    }

    for (int i = 0; i < geo.Points.Count; i++)
    {
        if (vertexNormals[i].sqrMagnitude > 0.0001f)
            vertexNormals[i] = vertexNormals[i].normalized;
        else
            vertexNormals[i] = Vector3.up;
    }
}
else
{
    // 无面数据，默认 Y 轴
    for (int i = 0; i < geo.Points.Count; i++)
        vertexNormals[i] = Vector3.up;
}
```

然后替换 line 113-124 为：

```csharp
// 沿法线方向偏移
geo.Points[i] = p + vertexNormals[i] * noiseValue * height;
```

---

## P0-5: `SaveToGraphData` 使用 `title` 作为 Group/StickyNote ID

**问题**: `GroupId = group.title` 和 `NoteId = note.title`，多个同名 Group/Note 会导致序列化冲突。 

**修复思路**: 需要为 Group 和 StickyNote 维护一个 GUID 映射。由于 Unity GraphView 的 `Group` 和 `StickyNote` 类没有自定义 ID 字段，需要用 `userData` 属性存储 GUID。

**修复代码**:

1. **创建 Group 时分配 GUID** — 修改 `GroupSelection()` (line 174):

```csharp
var group = new Group { title = "New Group" };
group.userData = System.Guid.NewGuid().ToString(); // 存储唯一 ID
```

2. **创建 StickyNote 时分配 GUID** — 修改 `AddStickyNote()` (line 188):

```csharp
var note = new StickyNote();
note.title = "Note";
note.contents = "Write your note here...";
note.userData = System.Guid.NewGuid().ToString(); // 存储唯一 ID
```

3. **保存时使用 GUID** — 修改 `SaveToGraphData()` line 573-603:

```csharp
if (element is Group group)
{
    var groupData = new PCGGroupData
    {
        GroupId = group.userData as string ?? System.Guid.NewGuid().ToString(),
        Title = group.title,
        Position = group.GetPosition().position,
        Size = group.GetPosition().size
    };
    
    foreach (var contained in group.containedElements)
    {
        if (contained is PCGNodeVisual visual)
            groupData.NodeIds.Add(visual.NodeId);
    }
    
    data.Groups.Add(groupData);
}

if (element is StickyNote note)
{
    var noteData = new PCGStickyNoteData
    {
        NoteId = note.userData as string ?? System.Guid.NewGuid().ToString(),
        Title = note.title,
        Content = note.contents,
        Position = note.GetPosition().position,
        Size = note.GetPosition().size
    };
    data.StickyNotes.Add(noteData);
}
```

4. **加载时恢复 GUID** — 修改 `LoadGraph()` line 496-520:

```csharp
foreach (var groupData in data.Groups)
{
    var group = new Group { title = groupData.Title };
    group.userData = groupData.GroupId; // 恢复 GUID
    group.SetPosition(new Rect(groupData.Position, groupData.Size));
    // ... 其余不变
}

foreach (var noteData in data.StickyNotes)
{
    var note = new StickyNote();
    note.title = noteData.Title;
    note.contents = noteData.Content;
    note.userData = noteData.NoteId; // 恢复 GUID
    note.SetPosition(new Rect(noteData.Position, noteData.Size));
    AddElement(note);
}
```

---

## P0-6: `PCGGeometryToMesh.Convert` 不提取 UV/Normal/Color 属性

**问题**: line 51 标记为 TODO，转换时丢失了 `PointAttribs` 中的 "N"、"uv"、"Cd" 等属性。 

**修复代码** — 替换 `PCGGeometryToMesh.cs` line 49-53:

```csharp
mesh.triangles = triangles.ToArray();

// 从 PointAttribs 提取属性映射到 Mesh
bool hasCustomNormals = false;

// Normal ("N")
var normalAttr = geometry.PointAttribs.GetAttribute("N");
if (normalAttr != null && normalAttr.Values.Count == geometry.Points.Count)
{
    var normals = new Vector3[geometry.Points.Count];
    for (int i = 0; i < geometry.Points.Count; i++)
    {
        normals[i] = normalAttr.Values[i] is Vector3 n ? n : Vector3.up;
    }
    mesh.normals = normals;
    hasCustomNormals = true;
}

// UV ("uv")
var uvAttr = geometry.PointAttribs.GetAttribute("uv");
if (uvAttr != null && uvAttr.Values.Count == geometry.Points.Count)
{
    var uvs = new Vector2[geometry.Points.Count];
    for (int i = 0; i < geometry.Points.Count; i++)
    {
        var val = uvAttr.Values[i];
        if (val is Vector2 uv2) uvs[i] = uv2;
        else if (val is Vector3 uv3) uvs[i] = new Vector2(uv3.x, uv3.y);
        else uvs[i] = Vector2.zero;
    }
    mesh.uv = uvs;
}

// Color ("Cd")
var colorAttr = geometry.PointAttribs.GetAttribute("Cd");
if (colorAttr != null && colorAttr.Values.Count == geometry.Points.Count)
{
    var colors = new Color[geometry.Points.Count];
    for (int i = 0; i < geometry.Points.Count; i++)
    {
        var val = colorAttr.Values[i];
        if (val is Color c) colors[i] = c;
        else if (val is Vector3 v) colors[i] = new Color(v.x, v.y, v.z, 1f);
        else colors[i] = Color.white;
    }
    mesh.colors = colors;
}

// Alpha ("Alpha") — 如果有单独的 Alpha 属性，合并到 Color
var alphaAttr = geometry.PointAttribs.GetAttribute("Alpha");
if (alphaAttr != null && alphaAttr.Values.Count == geometry.Points.Count && mesh.colors != null)
{
    var colors = mesh.colors;
    for (int i = 0; i < geometry.Points.Count; i++)
    {
        if (alphaAttr.Values[i] is float a)
            colors[i].a = a;
    }
    mesh.colors = colors;
}

// 如果没有自定义法线，自动计算
if (!hasCustomNormals)
    mesh.RecalculateNormals();

mesh.RecalculateBounds();
mesh.RecalculateTangents();
```

同时补全 `FromMesh` 方法 (line 80-88)，添加 UV 和 Color 映射：

```csharp
// 映射 UV
if (mesh.uv != null && mesh.uv.Length > 0)
{
    var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector2);
    foreach (var uv in mesh.uv)
        uvAttr.Values.Add(uv);
}

// 映射 Color
if (mesh.colors != null && mesh.colors.Length > 0)
{
    var colorAttr = geo.PointAttribs.CreateAttribute("Cd", AttribType.Color);
    foreach (var c in mesh.colors)
        colorAttr.Values.Add(c);
}
```

---

## P0-7: `PCGGeometry.BuildEdges()` 未实现

**问题**: 仍然是 TODO 状态，仅打印日志。

**修复代码** — 替换 `PCGGeometry.cs` line 59-63:

```csharp
public void BuildEdges()
{
    Edges.Clear();
    var edgeSet = new HashSet<long>(); // 用 long 编码无序边对

    foreach (var prim in Primitives)
    {
        for (int i = 0; i < prim.Length; i++)
        {
            int a = prim[i];
            int b = prim[(i + 1) % prim.Length];

            // 无序边：确保 min 在前
            int minIdx = Mathf.Min(a, b);
            int maxIdx = Mathf.Max(a, b);
            long edgeKey = ((long)minIdx << 32) | (long)(uint)maxIdx;

            if (edgeSet.Add(edgeKey))
            {
                Edges.Add(new int[] { minIdx, maxIdx });
            }
        }
    }
}
```

> 使用 `long` 编码无序边对 `(min, max)` 来去重，避免 `HashSet<(int,int)>` 的装箱开销。对于顶点数 < 2^31 的场景完全安全。

---

## P2-3: 版本信息与项目状态

**问题**: 项目没有集中的版本号管理，也没有在 UI 中显示版本信息。

### 新建文件: `Assets/PCGToolkit/Editor/Core/PCGToolkitVersion.cs`

```csharp
namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG Toolkit 版本信息（集中管理）
    /// </summary>
    public static class PCGToolkitVersion
    {
        public const int Major = 0;
        public const int Minor = 5;
        public const int Patch = 0;
        public const string Label = "alpha"; // alpha / beta / rc / ""
        
        /// <summary>对应的 Git commit SHA（构建时更新）</summary>
        public const string CommitSHA = "05f8f419";
        
        /// <summary>构建日期</summary>
        public const string BuildDate = "2026-03-20";

        public static string Version => Label == "" 
            ? $"{Major}.{Minor}.{Patch}" 
            : $"{Major}.{Minor}.{Patch}-{Label}";

        public static string FullVersion => $"{Version} ({CommitSHA})";
    }
}
```

### 修改 `PCGGraphEditorWindow.cs` — 在工具栏右侧添加版本标签

在 `GenerateToolbar()` 方法的 `toolbar.Add(_totalTimeLabel)` 之后（line 298 之后）添加：

```csharp
// 版本信息标签
var versionLabel = new Label($"v{PCGToolkit.Core.PCGToolkitVersion.Version}")
{
    style =
    {
        unityTextAlign = TextAnchor.MiddleRight,
        color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
        marginRight = 8,
        fontSize = 10,
    }
};
versionLabel.tooltip = $"PCG Toolkit {PCGToolkit.Core.PCGToolkitVersion.FullVersion}\nBuild: {PCGToolkit.Core.PCGToolkitVersion.BuildDate}";
toolbar.Add(versionLabel);
```

### 修改窗口标题以包含版本号

修改 `UpdateWindowTitle()` (line 345-352):

```csharp
private void UpdateWindowTitle()
{
    string graphName = string.IsNullOrEmpty(_currentAssetPath) 
        ? "New Graph" 
        : System.IO.Path.GetFileNameWithoutExtension(_currentAssetPath);
    string dirtyMark = _isDirty ? "*" : "";
    titleContent = new GUIContent(
        $"PCG Node Editor v{PCGToolkit.Core.PCGToolkitVersion.Version} - {graphName}{dirtyMark}");
}
```

---

## 附加修复: 消除 `DeserializeParamValue` 重复代码

虽然这属于 P3-2，但改动极小且与 P0 修复同步进行更合理。

**问题**: `PCGGraphView.cs` line 661-713 和 `PCGAsyncGraphExecutor.cs` line 367-421 各有一份 `DeserializeParamValue` 的独立实现，与 `PCGParamHelper.DeserializeParamValue` 功能完全相同。 [1-cite-9](#1-cite-9) [1-cite-10](#1-cite-10) [1-cite-11](#1-cite-11)

**修复**:

1. **`PCGGraphView.cs`** — 删除 line 661-713 的 `DeserializeParamValue` 方法，将所有调用处改为 `PCGParamHelper.DeserializeParamValue(param)`。涉及 line 282 和 line 471:

```csharp
// line 282 (UnserializeAndPaste)
defaults[param.Key] = PCGParamHelper.DeserializeParamValue(param);

// line 471 (LoadGraph)
defaults[param.Key] = PCGParamHelper.DeserializeParamValue(param);
```

2. **`PCGAsyncGraphExecutor.cs`** — 删除 line 367-421 的 `DeserializeParamValue` 方法，将调用处改为 `PCGParamHelper.DeserializeParamValue(param)`。需要在文件顶部添加 `using PCGToolkit.Core;`（已存在于 line 7）。

`PCGGraphExecutor.cs` line 230 已经在使用 `PCGParamHelper.DeserializeParamValue`，无需修改。 

---

## 文件变更清单

| 文件 | 操作 | 改动量 |
|------|------|--------|
| `Editor/Graph/PCGGraphView.cs` | 改 | P0-1 (~15行), P0-2 (~8行), P0-5 (~12行), 消除重复代码 (-52行) |
| `Editor/Nodes/Geometry/ExtrudeNode.cs` | 改 | P0-3 (~5行关键修改) |
| `Editor/Nodes/Deform/MountainNode.cs` | 改 | P0-4 (~30行新增法线预计算) |
| `Editor/Core/PCGGeometry.cs` | 改 | P0-7 (~20行替换) |
| `Editor/Core/PCGGeometryToMesh.cs` | 改 | P0-6 (~60行新增属性映射) |
| `Editor/Graph/PCGAsyncGraphExecutor.cs` | 改 | 消除重复代码 (-55行) |
| `Editor/Graph/PCGGraphEditorWindow.cs` | 改 | P2-3 (~15行新增) |
| `Editor/Core/PCGToolkitVersion.cs` | **新建** | P2-3 (~25行) |