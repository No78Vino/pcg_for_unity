# 当前任务：迭代修复项目内的错误
以下是完整的修复方案，共涉及 **9 个文件**，包含 **2 个编译错误** 和 **9 个逻辑 Bug**。

---

## 修复方案

### 修复 1：`AttributeStore` 添加 `SetAttribute` 方法 [编译错误]

**文件**: `Assets/PCGToolkit/Editor/Core/AttributeStore.cs`

在第 73 行（`CreateAttribute` 方法之后）**新增**以下方法：

```csharp
/// <summary>
/// 便捷方法：创建/更新属性并设置单个值，返回 this 以支持链式调用。
/// </summary>
public AttributeStore SetAttribute(string name, object value)
{
    AttribType type = InferType(value);
    if (_attributes.TryGetValue(name, out var existing))
    {
        existing.DefaultValue = value;
        existing.Values.Clear();
        existing.Values.Add(value);
    }
    else
    {
        var attr = new PCGAttribute(name, type, value);
        attr.Values.Add(value);
        _attributes[name] = attr;
    }
    return this;
}

private static AttribType InferType(object value)
{
    if (value is float || value is double) return AttribType.Float;
    if (value is int) return AttribType.Int;
    if (value is Vector2) return AttribType.Vector2;
    if (value is Vector3) return AttribType.Vector3;
    if (value is Vector4) return AttribType.Vector4;
    if (value is Color) return AttribType.Color;
    return AttribType.String;
}
```

**影响范围**：修复以下 8 个文件共 13 处调用：
- `CurveCreateNode.cs` (L107, L113)
- `LSystemNode.cs` (L179, L180)
- `LODGenerateNode.cs` (L117, L120, L121, L122)
- `ExportFBXNode.cs` (L126)
- `SaveMaterialNode.cs` (L169)
- `SavePrefabNode.cs` (L114)
- `SaveSceneNode.cs` (L146)
- `WFCNode.cs` (L140 — 另有签名问题，见修复 6)

---

### 修复 2：`PCGNodeBase` 添加 `GetParamColor` 方法 [编译错误]

**文件**: `Assets/PCGToolkit/Editor/Core/PCGNodeBase.cs`

在第 98 行（`GetParamVector3` 方法之后）**新增**：

```csharp
/// <summary>
/// 从参数字典中获取 Color 值
/// </summary>
protected Color GetParamColor(Dictionary<string, object> parameters, string name, Color defaultValue)
{
    if (parameters != null && parameters.TryGetValue(name, out var val))
    {
        if (val is Color c) return c;
        if (val is Vector4 v4) return new Color(v4.x, v4.y, v4.z, v4.w);
        if (val is string s && ColorUtility.TryParseHtmlString(s, out var parsed))
            return parsed;
    }
    return defaultValue;
}
```

**影响范围**：修复 `SaveMaterialNode.cs` 第 52、56 行的 `GetParamColor` 调用。

---

### 修复 3：`ExportFBXNode` FBX 导出器类名错误

**文件**: `Assets/PCGToolkit/Editor/Nodes/Output/ExportFBXNode.cs`

**第 91 行**，将：
```csharp
var fbxExporterType = System.Type.GetType("UnityEditor.Formats.Fbx.Exporter.FBXExporter, UnityEditor.Formats.Fbx.Editor");
```
**改为**：
```csharp
var fbxExporterType = System.Type.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter, Unity.Formats.Fbx.Editor");
```

**第 95-101 行**，将：
```csharp
var exportMethod = fbxExporterType.GetMethod("ExportGameObjects",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

if (exportMethod != null)
{
    var gameObjects = new GameObject[] { go };
    exportMethod.Invoke(null, new object[] { gameObjects, fbxPath });
```
**改为**：
```csharp
var exportMethod = fbxExporterType.GetMethod("ExportObject",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
    null,
    new System.Type[] { typeof(string), typeof(UnityEngine.Object) },
    null);

if (exportMethod != null)
{
    exportMethod.Invoke(null, new object[] { fbxPath, go });
```

---

### 修复 4：`SaveMaterialNode` cutout 渲染模式关键字错误

**文件**: `Assets/PCGToolkit/Editor/Nodes/Output/SaveMaterialNode.cs`

**第 123-124 行**，将：
```csharp
material.DisableKeyword("_ALPHATEST_ON");
material.EnableKeyword("_ALPHABLEND_ON");
```
**改为**：
```csharp
material.EnableKeyword("_ALPHATEST_ON");
material.DisableKeyword("_ALPHABLEND_ON");
```

---

### 修复 5：`LSystemNode` 步长预缩放错误

**文件**: `Assets/PCGToolkit/Editor/Nodes/Procedural/LSystemNode.cs`

**删除第 87-88 行**：
```csharp
// 删除以下两行：
for (int gen = 0; gen < iterations; gen++)
    currentStep *= stepScale;
```

这两行在龟壳解释器运行前就把 `currentStep` 缩小了 `stepScale^iterations` 倍，导致初始步长远小于预期。步长缩放应该只在 `[` / `]` / `!` / `'` 字符处理时发生（第 150、159、165、170 行已正确处理）。

---

### 修复 6：`WFCNode` `PrimAttribs.SetAttribute` 参数签名错误

**文件**: `Assets/PCGToolkit/Editor/Nodes/Procedural/WFCNode.cs`

**第 140 行**，将：
```csharp
geo.PrimAttribs.SetAttribute("tileType", primitives.Count - 12, tileType);
```
**改为**（使用 PrimGroup 存储瓦片类型）：
```csharp
// 为每种瓦片类型创建 Prim Group
string tileGroupName = $"tile_{tileType}";
if (!geo.PrimGroups.ContainsKey(tileGroupName))
    geo.PrimGroups[tileGroupName] = new HashSet<int>();
for (int pi = primitives.Count - 12; pi < primitives.Count; pi++)
    geo.PrimGroups[tileGroupName].Add(pi);
```

---

### 修复 7：`BendNode` 未使用变量 `newRadius`

**文件**: `Assets/PCGToolkit/Editor/Nodes/Deform/BendNode.cs`

**删除第 74-75 行**：
```csharp
// 删除以下两行：
// 计算弯曲后的新位置
float newRadius = radius + (axisIndex == 0 ? p.z : (axisIndex == 2 ? p.x : p.z));
```

---

### 修复 8：`SaveSceneNode` Mesh 资产路径错误

**文件**: `Assets/PCGToolkit/Editor/Nodes/Output/SaveSceneNode.cs`

**第 135 行**，将：
```csharp
string meshAssetPath = Path.ChangeExtension(scenePath, "_Mesh.asset");
```
**改为**：
```csharp
string meshAssetPath = Path.Combine(
    Path.GetDirectoryName(scenePath),
    Path.GetFileNameWithoutExtension(scenePath) + "_Mesh.asset");
```

`Path.ChangeExtension("PCGScene.unity", "_Mesh.asset")` 会生成 `PCGScene._Mesh.asset`（下划线成为扩展名的一部分），而非预期的 `PCGScene_Mesh.asset`。

同样的问题也存在于 `SavePrefabNode.cs` **第 102 行**，将：
```csharp
string meshAssetPath = Path.ChangeExtension(savePath, ".asset");
```
**改为**：
```csharp
string meshAssetPath = Path.Combine(
    Path.GetDirectoryName(savePath),
    Path.GetFileNameWithoutExtension(savePath) + "_Mesh.asset");
```

---

### 修复 9：`FilletNode` 圆弧生成数学错误

**文件**: `Assets/PCGToolkit/Editor/Nodes/Curve/FilletNode.cs`

**将第 115-136 行**（从 `// 生成圆弧段` 到 `}` 结束）**替换为**：

```csharp
// 生成圆弧段（使用球面线性插值）
Vector3 fromCenter = filletStart - center;
Vector3 toCenter = filletEnd - center;

float arcAngle = Vector3.Angle(fromCenter, toCenter) * Mathf.Deg2Rad;
float sinArc = Mathf.Sin(arcAngle);

for (int j = 1; j < divisions; j++)
{
    float t = (float)j / divisions;

    Vector3 arcPoint;
    if (sinArc < 0.001f)
    {
        // 角度太小，退化为线性插值
        arcPoint = Vector3.Lerp(filletStart, filletEnd, t);
    }
    else
    {
        float a = Mathf.Sin((1f - t) * arcAngle) / sinArc;
        float b = Mathf.Sin(t * arcAngle) / sinArc;
        arcPoint = center + a * fromCenter + b * toCenter;
    }
    newPoints.Add(arcPoint);
}
```

原代码使用固定的 `dirIn` 作为局部坐标系的 `arcRight`，并用 `Mathf.Cos(a)` / `Mathf.Sin(a)` 在 XZ 平面生成点，但 `startAngle` / `endAngle` 的计算方式与这个坐标系不匹配，导致圆弧点不在正确的位置上。

---

### 修复 10：`VoronoiFractureNode` 3D 裁剪使用了 2D 算法

**文件**: `Assets/PCGToolkit/Editor/Nodes/Procedural/VoronoiFractureNode.cs`

**将第 160-213 行的 `ComputeVoronoiCell` 方法整体替换为**：

```csharp
private List<Vector3> ComputeVoronoiCell(Vector3 seed, List<Vector3> allSeeds, Vector3 min, Vector3 max, int seedIndex)
{
    // 使用半空间裁剪法计算 3D Voronoi 单元
    // 从包围盒的 6 个面（每面 2 个三角形）开始，逐步用中垂面裁剪

    // 包围盒的 8 个角点
    Vector3[] boxVerts = new Vector3[]
    {
        new Vector3(min.x, min.y, min.z), // 0
        new Vector3(max.x, min.y, min.z), // 1
        new Vector3(max.x, max.y, min.z), // 2
        new Vector3(min.x, max.y, min.z), // 3
        new Vector3(min.x, min.y, max.z), // 4
        new Vector3(max.x, min.y, max.z), // 5
        new Vector3(max.x, max.y, max.z), // 6
        new Vector3(min.x, max.y, max.z), // 7
    };

    // 包围盒的 12 个三角形（6 个面，每面 2 个三角形）
    var faces = new List<int[]>
    {
        new[]{0,2,1}, new[]{0,3,2}, // 前面 (-Z)
        new[]{4,5,6}, new[]{4,6,7}, // 后面 (+Z)
        new[]{0,1,5}, new[]{0,5,4}, // 底面 (-Y)
        new[]{2,3,7}, new[]{2,7,6}, // 顶面 (+Y)
        new[]{0,4,7}, new[]{0,7,3}, // 左面 (-X)
        new[]{1,2,6}, new[]{1,6,5}, // 右面 (+X)
    };

    var vertices = new List<Vector3>(boxVerts);

    // 用每个相邻种子点的中垂面裁剪凸多面体
    foreach (var other in allSeeds)
    {
        if (other == seed) continue;

        Vector3 midpoint = (seed + other) * 0.5f;
        Vector3 normal = (other - seed).normalized;

        // 对每个三角面进行半空间裁剪
        var newFaces = new List<int[]>();

        foreach (var face in faces)
        {
            // 计算每个顶点到裁剪面的距离
            float d0 = Vector3.Dot(vertices[face[0]] - midpoint, normal);
            float d1 = Vector3.Dot(vertices[face[1]] - midpoint, normal);
            float d2 = Vector3.Dot(vertices[face[2]] - midpoint, normal);

            // 所有顶点都在保留侧
            if (d0 <= 0 && d1 <= 0 && d2 <= 0)
            {
                newFaces.Add(face);
                continue;
            }

            // 所有顶点都在裁剪侧
            if (d0 > 0 && d1 > 0 && d2 > 0)
                continue;

            // 部分裁剪：计算交点并生成新三角形
            var insideVerts = new List<int>();
            var outsideVerts = new List<int>();
            float[] dists = { d0, d1, d2 };

            for (int i = 0; i < 3; i++)
            {
                if (dists[i] <= 0)
                    insideVerts.Add(i);
                else
                    outsideVerts.Add(i);
            }

            if (insideVerts.Count == 2)
            {
                // 两个顶点在内侧，一个在外侧
                int outIdx = outsideVerts[0];
                int in0 = insideVerts[0];
                int in1 = insideVerts[1];

                // 计算两个交点
                float t0 = dists[in0] / (dists[in0] - dists[outIdx]);
                Vector3 inter0 = vertices[face[in0]] + t0 * (vertices[face[outIdx]] - vertices[face[in0]]);
                int inter0Idx = vertices.Count;
                vertices.Add(inter0);

                float t1 = dists[in1] / (dists[in1] - dists[outIdx]);
                Vector3 inter1 = vertices[face[in1]] + t1 * (vertices[face[outIdx]] - vertices[face[in1]]);
                int inter1Idx = vertices.Count;
                vertices.Add(inter1);

                // 生成两个新三角形
                newFaces.Add(new int[] { face[in0], face[in1], inter0Idx });
                newFaces.Add(new int[] { face[in1], inter1Idx, inter0Idx });
            }
            else if (insideVerts.Count == 1)
            {
                // 一个顶点在内侧，两个在外侧
                int inIdx = insideVerts[0];
                int out0 = outsideVerts[0];
                int out1 = outsideVerts[1];

                float t0 = dists[inIdx] / (dists[inIdx] - dists[out0]);
                Vector3 inter0 = vertices[face[inIdx]] + t0 * (vertices[face[out0]] - vertices[face[inIdx]]);
                int inter0Idx = vertices.Count;
                vertices.Add(inter0);

                float t1 = dists[inIdx] / (dists[inIdx] - dists[out1]);
                Vector3 inter1 = vertices[face[inIdx]] + t1 * (vertices[face[out1]] - vertices[face[inIdx]]);
                int inter1Idx = vertices.Count;
                vertices.Add(inter1);

                newFaces.Add(new int[] { face[inIdx], inter0Idx, inter1Idx });
            }
        }

        faces = newFaces;
        if (faces.Count == 0) break;
    }

    // 收集所有使用的顶点
    var usedVertices = new HashSet<int>();
    foreach (var face in faces)
    {
        usedVertices.Add(face[0]);
        usedVertices.Add(face[1]);
        usedVertices.Add(face[2]);
    }

    var result = new List<Vector3>();
    foreach (int idx in usedVertices)
        result.Add(vertices[idx]);

    return result;
}
```

---

### 修复 11：`DecimateNode` 边坍缩后 `edgeTris` 字典过期

**文件**: `Assets/PCGToolkit/Editor/Nodes/Topology/DecimateNode.cs`

**将第 133-184 行**（从 `// 迭代坍缩边` 到 `currentCount = geo.Primitives.Count;` 之后的 `}`）**替换为**：

```csharp
// 迭代坍缩边
int currentCount = originalCount;

while (currentCount > finalCount && edgeCosts.Count > 0)
{
    var edge = edgeCosts.Dequeue();

    // 重新检查边是否仍然有效（edgeTris 可能已过期）
    // 验证边的两个顶点是否仍然存在于某个三角形中
    int v0 = edge.Item1;
    int v1 = edge.Item2;

    // 找到包含这条边的三角形
    var adjacentTris = new List<int>();
    for (int triIdx = 0; triIdx < geo.Primitives.Count; triIdx++)
    {
        var tri = geo.Primitives[triIdx];
        if (tri == null) continue;
        bool hasV0 = tri[0] == v0 || tri[1] == v0 || tri[2] == v0;
        bool hasV1 = tri[0] == v1 || tri[1] == v1 || tri[2] == v1;
        if (hasV0 && hasV1) adjacentTris.Add(triIdx);
    }

    // 边必须恰好被 2 个三角形共享才能安全坍缩
    if (adjacentTris.Count != 2) continue;

    // 合并顶点（将 v1 合并到 v0 的中点位置）
    Vector3 newPos = (geo.Points[v0] + geo.Points[v1]) * 0.5f;
    geo.Points[v0] = newPos;

    // 更新所有引用 v1 的面
    for (int triIdx = 0; triIdx < geo.Primitives.Count; triIdx++)
    {
        var tri = geo.Primitives[triIdx];
        if (tri == null) continue;

        for (int i = 0; i < 3; i++)
        {
            if (tri[i] == v1)
                tri[i] = v0;
        }

        // 检查是否产生退化三角形（重复顶点）
        if (tri[0] == tri[1] || tri[1] == tri[2] || tri[0] == tri[2])
        {
            geo.Primitives[triIdx] = null; // 标记删除
        }
    }

    // 移除标记删除的面
    geo.Primitives.RemoveAll(t => t == null);
    currentCount = geo.Primitives.Count;
}
```

---

## 修改文件汇总

| # | 文件 | 操作 | 严重度 |
|---|------|------|--------|
| 1 | `Editor/Core/AttributeStore.cs` | 新增 `SetAttribute` + `InferType` 方法 | 编译错误 |
| 2 | `Editor/Core/PCGNodeBase.cs` | 新增 `GetParamColor` 方法 | 编译错误 |
| 3 | `Editor/Nodes/Output/ExportFBXNode.cs` | 修改 L91, L95-101 | 运行时错误 |
| 4 | `Editor/Nodes/Output/SaveMaterialNode.cs` | 修改 L123-124 | 逻辑错误 |
| 5 | `Editor/Nodes/Procedural/LSystemNode.cs` | 删除 L87-88 | 逻辑错误 |
| 6 | `Editor/Nodes/Procedural/WFCNode.cs` | 替换 L140 | 编译错误 |
| 7 | `Editor/Nodes/Deform/BendNode.cs` | 删除 L74-75 | 无用代码 |
| 8 | `Editor/Nodes/Output/SaveSceneNode.cs` | 修改 L135 | 逻辑错误 |
| 8b | `Editor/Nodes/Output/SavePrefabNode.cs` | 修改 L102 | 逻辑错误 |
| 9 | `Editor/Nodes/Curve/FilletNode.cs` | 替换 L115-136 | 数学错误 |
| 10 | `Editor/Nodes/Procedural/VoronoiFractureNode.cs` | 替换 L160-213 | 算法错误 |
| 11 | `Editor/Nodes/Topology/DecimateNode.cs` | 替换 L133-184 | 逻辑错误 |
