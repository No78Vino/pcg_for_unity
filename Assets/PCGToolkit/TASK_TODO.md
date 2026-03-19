# 当前任务清单


## 背景

目标：让 `ConstVector3 → Box → ExportMesh(SavePrefab)` 这条 3 节点流程在 PCG Node Editor 中完整跑通。

当前状态：
- ConstVector3Node 已实现，通过 ctx.GlobalVariables 传递 Vector3 值
- BoxNode 已实现，能生成 8 顶点 6 面的立方体
- ExportMeshNode 是 TODO 空壳
- PCGGeometryToMesh.Convert() 基础逻辑可用但有误导性日志
- 同步执行器 PCGGraphExecutor 有两个 bug（不反序列化参数、不读 GlobalVariables）
- PCGGraphView 没有 GetCompatiblePorts 覆写，允许任意类型端口互连

仓库：No78Vino/pcg_for_unity (branch: main)

---

## 任务 1：提取 DeserializeParamValue 为共享静态工具方法

### 问题
`DeserializeParamValue` 方法在 `PCGAsyncGraphExecutor`（第 354-408 行）和 `PCGGraphView`（第 257-309 行）中各有一份几乎相同的拷贝。同步执行器 `PCGGraphExecutor` 也需要用到它，但目前没有。

### 改动
1. 在 `Assets/PCGToolkit/Editor/Core/` 下新建文件 `PCGParamHelper.cs`
2. 创建静态类 `PCGParamHelper`，包含一个公共静态方法：
   ```csharp
   public static object DeserializeParamValue(PCGSerializedParameter param)
   ```
   逻辑直接从 `PCGAsyncGraphExecutor.DeserializeParamValue()`（第 354-408 行）复制过来。
   注意：需要 `using PCGToolkit.Graph;`（因为 `PCGSerializedParameter` 定义在 `PCGGraphData.cs` 中）和 `using UnityEngine;`。
3. 将 `PCGAsyncGraphExecutor`（第 354-408 行）和 `PCGGraphView`（第 257-309 行）中的 `DeserializeParamValue` 方法体改为调用 `PCGParamHelper.DeserializeParamValue(param)`，或直接删除私有方法改为调用静态方法。

---

## 任务 2：修复 PCGGraphExecutor.ExecuteNode() 的参数传递

### 问题
文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs`，第 197-202 行。

两个 bug：
1. `parameters[param.Key] = param.ValueJson` 传入的是原始 JSON 字符串，而非反序列化后的 float/int/Vector3 等类型。下游节点的 `GetParamFloat()` 等方法做类型检查（`val is float f`），字符串会匹配失败，导致 fallback 到默认值。
2. 缺少从 `context.GlobalVariables` 读取上游 Const 节点值的逻辑。ConstVector3Node 将值写入 `ctx.GlobalVariables["{nodeId}.value"]`，但同步执行器从不读取它。

### 改动
在 `ExecuteNode()` 方法中（第 197-202 行附近）：

**修复 1**：将第 201 行改为使用任务 1 中提取的共享方法：
```csharp
// 原来：
parameters[param.Key] = param.ValueJson;
// 改为：
parameters[param.Key] = PCGParamHelper.DeserializeParamValue(param);
```
需要在文件顶部添加 `using PCGToolkit.Core;`（如果还没有的话，用于访问 PCGParamHelper）。

**修复 2**：在第 202 行之后、第 204 行（`context.CurrentNodeId = ...`）之前，添加 GlobalVariables 读取逻辑，与 `PCGAsyncGraphExecutor`（第 303-316 行）完全一致：
```csharp
// 从上游 Const 节点的 GlobalVariables 中获取值
foreach (var edge in graphData.Edges)
{
    if (edge.InputNodeId == nodeData.NodeId)
    {
        var upstreamKey = $"{edge.OutputNodeId}.{edge.OutputPortName}";
        if (context.GlobalVariables.TryGetValue(upstreamKey, out var val))
        {
            parameters[edge.InputPortName] = val;
        }
    }
}
```

---

## 任务 3：实现 ExportMeshNode.Execute() 并支持 Prefab 保存

### 问题
文件：`Assets/PCGToolkit/Editor/Nodes/Create/ExportMeshNode.cs`，第 33-49 行。
整个 Execute 方法是 TODO 空壳，不做任何实际操作。

### 改动

**3a. 添加 using 指令**（文件顶部）：
```csharp
using UnityEditor;
using System.IO;
```

**3b. 修改 Inputs 定义**（第 17-25 行）：
将 `assetPath` 的默认值从 `"Assets/PCGOutput/mesh.asset"` 改为 `"Assets/PCGOutput/output.prefab"`，并将 DisplayName 改为 `"Save Path"`。

**3c. 实现 Execute 方法体**（替换第 37-48 行的 TODO 内容）：

实现逻辑如下（伪代码，请转为完整 C#）：
```
1. var geo = GetInputGeometry(inputGeometries, "input");
2. if (geo == null || geo.Points.Count == 0) {
     ctx.LogWarning("ExportMesh: 输入几何体为空，跳过导出");
     return SingleOutput("geometry", geo);
   }
3. string savePath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/output.prefab");
4. bool createRenderer = GetParamBool(parameters, "createRenderer", true);
5. // 确保目录存在
   string directory = Path.GetDirectoryName(savePath);
   if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
6. // 转换为 Mesh
   var mesh = PCGGeometryToMesh.Convert(geo);
   mesh.name = Path.GetFileNameWithoutExtension(savePath) + "_Mesh";
7. // 保存 Mesh 资产
   string meshAssetPath = Path.ChangeExtension(savePath, ".asset");
   AssetDatabase.CreateAsset(mesh, meshAssetPath);
8. if (createRenderer) {
     // 创建临时 GameObject
     var go = new GameObject(Path.GetFileNameWithoutExtension(savePath));
     go.AddComponent<MeshFilter>().sharedMesh = mesh;
     var renderer = go.AddComponent<MeshRenderer>();
     renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
     // 保存为 Prefab
     string prefabPath = Path.ChangeExtension(savePath, ".prefab");
     PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
     Object.DestroyImmediate(go);
     ctx.Log($"ExportMesh: Prefab 已保存到 {prefabPath}");
   }
9. AssetDatabase.SaveAssets();
   AssetDatabase.Refresh();
   ctx.Log($"ExportMesh: Mesh 已保存到 {meshAssetPath}");
10. return SingleOutput("geometry", geo);
```

**3d. 移动文件位置**：
将 `Assets/PCGToolkit/Editor/Nodes/Create/ExportMeshNode.cs` 移动到 `Assets/PCGToolkit/Editor/Nodes/Output/ExportMeshNode.cs`。
注意：`Nodes/Output/` 目录当前为空，需要确认目录存在。namespace 保持不变（`PCGToolkit.Nodes.Create`），或者改为 `PCGToolkit.Nodes.Output` 以与目录一致——如果改 namespace，需要检查是否有其他文件引用了旧 namespace。

---

## 任务 4：清理 PCGGeometryToMesh.Convert() 的误导性日志

### 问题
文件：`Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs`，第 17-18 行。
日志写着 "TODO - 将 PCGGeometry 转换为 Unity Mesh"，但实际上基础转换逻辑（顶点 + 三角形/四边形 → Mesh + RecalculateNormals）已经可以工作。这会让开发者误以为功能未实现。

### 改动
将第 17-18 行：
```csharp
// TODO: 实现完整的 PCGGeometry → Mesh 转换
Debug.Log("[PCGGeometryToMesh] Convert: TODO - 将 PCGGeometry 转换为 Unity Mesh");
```
改为：
```csharp
Debug.Log($"[PCGGeometryToMesh] Converting PCGGeometry to Mesh (Points: {geometry?.Points.Count ?? 0}, Prims: {geometry?.Primitives.Count ?? 0})");
```

---

## 任务 5：为 PCGGraphView 添加 GetCompatiblePorts 端口类型检查

### 问题
文件：`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`。
当前没有覆写 `GetCompatiblePorts` 方法。Unity GraphView 的默认实现允许任意方向相反的端口互连，不检查数据类型。这意味着用户可以把 Geometry 输出连到 Float 输入，执行时会静默失败。

### 改动
在 `PCGGraphView` 类中添加 `GetCompatiblePorts` 覆写方法（建议放在第 31 行 `graphViewChanged += OnGraphViewChanged;` 之后）：

```csharp
public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
{
    var compatiblePorts = new List<Port>();
    ports.ForEach(port =>
    {
        // 不能连接自身节点
        if (port.node == startPort.node) return;
        // 方向必须相反
        if (port.direction == startPort.direction) return;
        // 类型必须兼容：相同类型，或其中一方是 Any
        if (port.portType != startPort.portType &&
            port.portType != typeof(object) &&
            startPort.portType != typeof(object))
            return;
        compatiblePorts.Add(port);
    });
    return compatiblePorts;
}
```

注意：`PCGPortType.Any` 在 `GetSystemType()` 中映射为 `typeof(object)`（见 PCGNodeVisual.cs 第 501 行），所以用 `typeof(object)` 判断 Any 类型。

---

## 验证步骤

完成以上 5 个任务后，按以下步骤验证：

1. 打开 Unity，菜单 `PCG Toolkit > Node Editor`
2. 按 Tab 或右键创建 `Const Vector3` 节点，设置 x=0, y=2, z=0
3. 创建 `Box` 节点
4. 创建 `Export Mesh` 节点，在 Save Path 字段输入 `Assets/PCGOutput/box.prefab`
5. 连线：ConstVector3 的 `Value` 输出 → Box 的 `Center` 输入
6. 连线：Box 的 `Geometry` 输出 → ExportMesh 的 `Input` 输入
7. 点击工具栏 `Execute` 按钮
8. 验证：
    - Console 无报错
    - `Assets/PCGOutput/` 目录下生成了 `box.asset`（Mesh 资产）和 `box.prefab`（Prefab）
    - 双击 Prefab 可以看到一个中心在 (0,2,0) 的立方体
    - 三个节点都显示了执行耗时标签
---
以上就是完整的 5 个任务。核心改动集中在 4 个文件 + 1 个新建文件：

| # | 任务 | 文件 | 性质 |
|---|------|------|------|
| 1 | 提取 `DeserializeParamValue` 为共享方法 | `Core/PCGParamHelper.cs` (新建) | 消除重复代码 |
| 2 | 修复同步执行器参数传递 | `Graph/PCGGraphExecutor.cs` 第 197-202 行 | Bug 修复 |
| 3 | 实现 ExportMeshNode + Prefab 保存 | `Nodes/Create/ExportMeshNode.cs` → 移到 `Nodes/Output/` | 功能实现 |
| 4 | 清理误导性 TODO 日志 | `Core/PCGGeometryToMesh.cs` 第 17-18 行 | 清理 |
| 5 | 添加端口类型兼容性检查 | `Graph/PCGGraphView.cs` | 防御性改进 |