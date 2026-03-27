# Topology Skill 文档

## 概述
- **类别**: Topology (Tier 3)
- **节点数量**: 8
- **适用场景**: 多边形倒角、桥接、填充孔洞、边分割、重新网格化、减面、凸分解

## 节点列表

### Poly Bevel (`PolyBevel`)
- **Houdini 对标**: PolyBevel SOP
- **功能**: 对多边形的边进行倒角，支持按 Group 选择
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | offset | Float | 0.1 | 否 | 倒角偏移距离 |
  | divisions | Int | 1 | 否 | 倒角分段数 |
  | group | String | "" | 否 | 仅对指定 PrimGroup 内的边倒角（留空=所有边） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Poly Bridge (`PolyBridge`)
- **Houdini 对标**: PolyBridge SOP
- **功能**: 在两个边界边环之间创建桥接面
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | divisions | Int | 1 | 否 | 桥接分段数 |
  | twist | Float | 0.0 | 否 | 扭转角度 |
  | taper | Float | 1.0 | 否 | 锥度（0~1） |
  | reverse | Bool | false | 否 | 反转连接方向 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Poly Fill (`PolyFill`)
- **Houdini 对标**: PolyFill SOP
- **功能**: 填充几何体中的孔洞
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | fillMode | String | "triangulate" | 否 | 填充模式（triangulate/fan/center） |
  | reverse | Bool | false | 否 | 反转填充面法线 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Poly Split (`PolySplit`)
- **Houdini 对标**: Clip / PolySplit SOP
- **功能**: 用平面切割面，拆分为子面
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | origin | Vector3 | (0,0,0) | 否 | 切割平面原点 |
  | normal | Vector3 | (0,1,0) | 否 | 切割平面法线 |
  | keepBoth | Bool | true | 否 | 保留两侧（false 则仅保留法线正侧） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Edge Divide (`EdgeDivide`)
- **Houdini 对标**: Subdivide（边分割模式）
- **功能**: 在边上等距插入新点
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | divisions | Int | 1 | 否 | 每条边插入的点数 |
  | group | String | "" | 否 | 仅对指定 PrimGroup 的边操作（留空=所有） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Remesh (`Remesh`)
- **Houdini 对标**: Remesh SOP
- **功能**: 重新生成均匀的三角形网格
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | targetEdgeLength | Float | 0.5 | 否 | 目标边长 |
  | iterations | Int | 3 | 否 | 迭代次数 |
  | smoothing | Float | 0.5 | 否 | 平滑系数 |
  | preserveBoundary | Bool | true | 否 | 保持边界不变 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Decimate (`Decimate`)
- **Houdini 对标**: PolyReduce SOP
- **功能**: 减少网格的面数
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | targetRatio | Float | 0.5 | 否 | 目标面数比例（0~1） |
  | targetCount | Int | 0 | 否 | 目标面数（优先于 ratio） |
  | preserveBoundary | Bool | true | 否 | 保持边界不变 |
  | preserveTopology | Bool | false | 否 | 保持拓扑结构 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Convex Decomposition (`ConvexDecomposition`)
- **Houdini 对标**: 碰撞体生成需求
- **功能**: 将网格分解为多个凸包
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | maxHulls | Int | 16 | 否 | 最大凸包数量 |
  | maxVerticesPerHull | Int | 0 | 否 | 每个凸包最大顶点数（0=无限制） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 分解后的几何体（每个凸包为独立面组） |

## 常见组合模式 (Recipes)

### Recipe 1: 硬表面倒角
```
Box → Extrude → PolyBevel → Subdivide → Normal
```
**说明**: 创建基础形体后挤出，PolyBevel 在边缘添加倒角细节，Subdivide 平滑细分使倒角更圆滑，Normal 重算法线。

### Recipe 2: LOD 简化
```
HighPoly → Decimate → Normal
```
**说明**: 对高面数模型使用 Decimate 按比例减少面数（如保留 50%），Decimate 使用 QEM 算法保留关键特征，Normal 重算法线。

### Recipe 3: 碰撞体生成
```
Mesh → ConvexDecomposition → ExportMesh
```
**说明**: 将复杂网格分解为多个凸包，每个凸包适合作为物理碰撞体使用，ExportMesh 导出为 Unity 资产。
