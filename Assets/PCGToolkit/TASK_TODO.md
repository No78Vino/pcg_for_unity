# 当前任务： 修复 PR #11 迭代代码中的 16 个错误和疏漏

仓库：No78Vino/pcg_for_unity (branch: main)

本次修复涉及 PR #11 合入的四次迭代代码中的编译错误、运行时错误、功能缺失和逻辑问题。按优先级排列如下：

---

## 修复 1：PCGContext 缺少 Debug 属性和带参构造函数（编译错误）

文件：`Assets/PCGToolkit/Editor/Core/PCGContext.cs`

PCGContext 类（第 9 行）目前只有默认无参构造函数，没有 `Debug` 属性。但 SubGraphNode.cs 第 136 行调用了 `new PCGContext(ctx.Debug)`。

修改方案：
- 在 PCGContext 类中新增 `public bool Debug { get; set; }` 属性
- 新增带参构造函数 `public PCGContext(bool debug) { Debug = debug; }`
- 保留默认无参构造函数

---

## 修复 2：PCGGraphExecutor.Execute() 缺少接受 PCGContext 参数的重载（编译错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs`

当前 `Execute()` 方法（第 30 行）是无参的，但 SubGraphNode.cs 第 146 行调用 `executor.Execute(subContext)`。

修改方案：新增一个 `Execute(PCGContext externalContext)` 重载方法：
```csharp
public void Execute(PCGContext externalContext)
{
    context = externalContext;
    _nodeOutputs.Clear();
    context.ClearCache();
    
    var sortedNodes = TopologicalSort();
    if (sortedNodes == null)
    {
        Debug.LogError("PCGGraphExecutor: Topological sort failed (cycle detected).");
        return;
    }
    
    foreach (var nodeData in sortedNodes)
    {
        ExecuteNode(nodeData);
        if (context.HasError)
        {
            Debug.LogError($"PCGGraphExecutor: Execution stopped due to error at node {nodeData.NodeType} ({nodeData.NodeId})");
            return;
        }
    }
    
    Debug.Log($"PCGGraphExecutor: Execution completed. {sortedNodes.Count} nodes executed.");
}
```

---

## 修复 3：Group 构造函数签名错误（编译错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`，第 146 行

Unity 的 `Group` 类没有 `(string, Rect)` 构造函数。

将：
```csharp
var group = new Group("New Group", new Rect(minPos - new Vector2(20, 40), maxPos - minPos + new Vector2(40, 60)));
```
改为：
```csharp
var group = new Group();
group.title = "New Group";
group.SetPosition(new Rect(minPos - new Vector2(20, 40), maxPos - minPos + new Vector2(40, 60)));
```

---

## 修复 4：StickyNote 构造函数签名错误（编译错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`，第 159-163 行

Unity 的 `StickyNote` 没有 `(string)` 构造函数。

将：
```csharp
var note = new StickyNote("New Note")
{
    title = "Note",
    contents = "Write your note here..."
};
```
改为：
```csharp
var note = new StickyNote()
{
    title = "Note",
    contents = "Write your note here..."
};
```

---

## 修复 5：PCGNodeVisual.cs 缺少 `using System.Linq`（编译错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGNodeVisual.cs`

第 313 行 `schema.EnumOptions.ToList()` 使用了 LINQ 扩展方法，但文件头部（第 1-7 行）没有 `using System.Linq;`。

修改方案：在文件顶部 using 区域添加：
```csharp
using System.Linq;
```

---

## 修复 6：Update() 中 Event.current 无效（运行时不生效）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs`，第 298-326 行

`Event.current` 只在 `OnGUI()` 中有效，在 `Update()` 中为 null。Ctrl+S / Ctrl+Shift+S 快捷键永远不会触发。

修改方案：将 `Update()` 方法改为 `OnGUI()` 方法，或者改用 UIElements 的方式在 `ConstructGraphView()` 中注册：
```csharp
// 在 ConstructGraphView() 末尾添加
rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
{
    if (evt.keyCode == KeyCode.S && evt.ctrlKey && !evt.shiftKey)
    {
        SaveGraph();
        evt.StopPropagation();
    }
    else if (evt.keyCode == KeyCode.S && evt.ctrlKey && evt.shiftKey)
    {
        SaveAsGraph();
        evt.StopPropagation();
    }
});
```
然后删除 `Update()` 和 `HandleKeyboardShortcut()` 方法（第 298-326 行）。

---

## 修复 7：F 键和 Delete 键拦截文本输入（逻辑错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`，第 341-355 行

OnKeyDown 中按 F 或 Delete 时没有检查事件目标是否为文本输入控件，会导致在 TextField/FloatField 等控件中无法正常输入。

修改方案：在处理快捷键前检查事件目标：
```csharp
private void OnKeyDown(KeyDownEvent evt)
{
    // 如果焦点在文本输入控件上，不拦截
    if (evt.target is TextField || evt.target is FloatField || 
        evt.target is IntegerField || evt.target is TextInput ||
        (evt.target is VisualElement ve && ve.GetFirstAncestorOfType<TextField>() != null))
        return;
    
    if (evt.keyCode == KeyCode.F)
    {
        FrameAll();
        evt.StopPropagation();
    }
    else if (evt.keyCode == KeyCode.Delete)
    {
        DeleteSelection();
        evt.StopPropagation();
    }
}
```

---

## 修复 8：Group 和 StickyNote 未序列化/反序列化（功能缺失）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`

`SaveToGraphData()` 方法（第 469-515 行）和 `LoadGraph()` 方法（第 417-467 行）都没有处理 Groups 和 StickyNotes。

修改方案：

在 `SaveToGraphData()` 的 `return data;` 之前（第 514 行前）添加：
```csharp
// 序列化 Groups
graphElements.ForEach(element =>
{
    if (element is Group group)
    {
        var groupData = new PCGGroupData
        {
            GroupId = System.Guid.NewGuid().ToString(),
            Title = group.title,
            Position = group.GetPosition().position,
            Size = group.GetPosition().size,
        };
        foreach (var child in group.containedElements)
        {
            if (child is PCGNodeVisual nodeVisual)
                groupData.NodeIds.Add(nodeVisual.NodeId);
        }
        data.Groups.Add(groupData);
    }
    else if (element is StickyNote note)
    {
        var noteData = new PCGStickyNoteData
        {
            NoteId = System.Guid.NewGuid().ToString(),
            Title = note.title,
            Content = note.contents,
            Position = note.GetPosition().position,
            Size = note.GetPosition().size,
        };
        data.StickyNotes.Add(noteData);
    }
});
```

在 `LoadGraph()` 的末尾（第 466 行后，方法结束前）添加：
```csharp
// 恢复 Groups
if (data.Groups != null)
{
    foreach (var groupData in data.Groups)
    {
        var group = new Group();
        group.title = groupData.Title;
        group.SetPosition(new Rect(groupData.Position, groupData.Size));
        
        foreach (var nodeId in groupData.NodeIds)
        {
            if (nodeVisualMap.TryGetValue(nodeId, out var nodeVisual))
                group.AddElement(nodeVisual);
        }
        AddElement(group);
    }
}

// 恢复 StickyNotes
if (data.StickyNotes != null)
{
    foreach (var noteData in data.StickyNotes)
    {
        var note = new StickyNote()
        {
            title = noteData.Title,
            contents = noteData.Content,
        };
        note.SetPosition(new Rect(noteData.Position, noteData.Size));
        AddElement(note);
    }
}
```

---

## 修复 9：_nodeResults 字典在新执行时未清空（运行时错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs`，第 153-165 行

在 `StartExecution()` 方法中，`_nodeOutputs` 被清空了（第 162 行），但 `_nodeResults` 没有。

修改方案：在 `StartExecution()` 方法的第 162 行后添加：
```csharp
_nodeResults.Clear();
```

---

## 修复 10：新执行开始时错误面板未清空（运行时错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs`

在 `OnExecuteClicked()` 方法（第 385 行附近）和 `OnRunToSelectedClicked()` 方法（第 411 行附近）中，清除执行状态的代码块里，添加错误面板清空：
```csharp
_errorPanel.ClearErrors();
_errorPanel.style.display = DisplayStyle.None;
```

---

## 修复 11：ClearErrors() 不隐藏错误面板（逻辑错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGErrorPanel.cs`，第 92-96 行

`ClearErrors()` 清空了列表但没有隐藏面板。

修改方案：在 `ClearErrors()` 方法末尾添加：
```csharp
style.display = DisplayStyle.None;
```

---

## 修复 12：StretchToParentSize() 与 Flex 布局冲突（运行时错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs`，第 76 行

`graphView.StretchToParentSize()` 设置了 `position: absolute`，会导致 graphView 覆盖错误面板。

修改方案：将第 76 行：
```csharp
graphView.StretchToParentSize();
```
改为：
```csharp
graphView.style.flexGrow = 1;
```

---

## 修复 13：UpdateWidgetValue 不支持 Slider 和 PopupField（功能缺失）

文件：`Assets/PCGToolkit/Editor/Graph/PCGNodeVisual.cs`，第 594-628 行

当 Float/Int 参数有 Min/Max 时使用 Slider 容器，`widget is FloatField` 匹配失败。Enum 的 `PopupField<string>` 也未处理。

修改方案：在 `UpdateWidgetValue` 方法的 switch 中添加对 Slider 容器和 PopupField 的处理：

在 `case PCGPortType.Float` 分支中，当 `widget is FloatField` 不匹配时，添加 Slider 容器的处理：
```csharp
case PCGPortType.Float when value is float fv:
{
    if (widget is FloatField ff)
    {
        ff.SetValueWithoutNotify(fv);
    }
    else
    {
        // Slider 容器情况
        var slider = widget.Q<Slider>();
        if (slider != null) slider.SetValueWithoutNotify(fv);
        var label = widget.Q<Label>();
        if (label != null) label.text = fv.ToString("F2");
    }
    break;
}
```

类似地处理 `PCGPortType.Int` 的 Slider 情况。

在 switch 之前（或末尾）添加 PopupField 处理：
```csharp
// 处理 Enum PopupField
if (widget is PopupField<string> popup && value is string sv)
{
    popup.SetValueWithoutNotify(sv);
    return;
}
```

---

## 修复 14：端口拖拽过滤功能未接入（功能缺失）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`

`SetPortFilterForCreation()` 方法存在（第 333 行），但没有任何代码在端口拖拽时调用它。

修改方案：需要在 GraphView 中监听端口拖拽事件。在 `PCGGraphView` 构造函数中或 `Initialize()` 方法中，重写 `GetCompatiblePorts` 时记录拖拽端口信息，或者利用 `nodeCreationRequest` 的上下文。

一种实现方式是重写 `GetCompatiblePorts` 来记录拖拽起始端口：
```csharp
public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
{
    // 记录拖拽起始端口信息，用于搜索窗口过滤
    _dragStartPort = startPort;
    if (startPort.node is PCGNodeVisual visual)
    {
        var schemaName = visual.GetSchemaNameForPort(startPort);
        var schema = startPort.direction == Direction.Input 
            ? System.Array.Find(visual.PCGNode.Inputs, s => s.Name == schemaName)
            : System.Array.Find(visual.PCGNode.Outputs, s => s.Name == schemaName);
        if (schema != null)
        {
            _filterPortType = schema.PortType;
            _filterDirection = startPort.direction;
        }
    }
    
    // 原有兼容端口逻辑保持不变
    var compatiblePorts = new List<Port>();
    ports.ForEach(port => { ... }); // 保持原有逻辑
    return compatiblePorts;
}
```

---

## 修复 15：右键菜单 "Create Node" 坐标错误（逻辑错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`，第 106 行

`evt.mousePosition` 是局部坐标，不是屏幕坐标。`NodeCreationContext.screenMousePosition` 需要屏幕坐标。

修改方案：将第 106 行：
```csharp
screenMousePosition = evt.mousePosition
```
改为：
```csharp
screenMousePosition = GUIUtility.GUIToScreenPoint(evt.mousePosition)
```

---

## 修复 16：PCGGraphData.Clear() 未清空 Groups 和 StickyNotes（逻辑错误）

文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs`，第 165-169 行

修改方案：在 `Clear()` 方法中添加：
```csharp
public void Clear()
{
    Nodes.Clear();
    Edges.Clear();
    Groups.Clear();
    StickyNotes.Clear();
}
```

---

## 修复 17：删除死代码 _clickCount 和 _lastClickTime（代码清理）

文件：`Assets/PCGToolkit/Editor/Graph/PCGNodeVisual.cs`，第 55-56 行

删除未使用的字段：
```csharp
private int _clickCount = 0;
private float _lastClickTime = 0f;
```
以及注释 `// 迭代三：双击处理`（第 54 行）。

---

注意事项：
- 修复 1-5 是编译错误，必须全部修复后项目才能编译通过
- 修复顺序建议：先修复编译错误（1-5），再修复运行时问题（6-12），最后处理功能缺失和代码清理（13-17）
- 修复完成后请在 Unity Editor 中验证编译通过，并测试以下场景：
    1. 创建/保存/加载图（含 Group 和 StickyNote）
    2. Ctrl+S / Ctrl+Shift+S 快捷键
    3. 在 TextField 中输入字母 F 和按 Delete 键
    4. 执行图后查看错误面板行为
    5. 多次执行后检查预览数据是否正确
