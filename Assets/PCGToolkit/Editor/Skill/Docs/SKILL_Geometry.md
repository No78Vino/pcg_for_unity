# Geometry Skill 文档

## 概述
- **类别**: Geometry (Tier 2)
- **节点数量**: 20
- **适用场景**: 几何体编辑与处理，包括挤出、布尔运算、细分、镜像、合并顶点、法线计算、面操作、测量等

## 节点列表

### Extrude (`Extrude`)
- **Houdini 对标**: PolyExtrude SOP
- **功能**: 沿法线方向挤出几何体的面
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 要挤出的面分组（留空=全部面） |
  | distance | Float | 0.5 | 否 | 挤出距离 |
  | inset | Float | 0.0 | 否 | 内缩距离 |
  | divisions | Int | 1 | 否 | 挤出方向的分段数 |
  | outputFront | Bool | true | 否 | 是否输出顶面 |
  | outputSide | Bool | true | 否 | 是否输出侧面 |
  | individual | Bool | false | 否 | 是否独立挤出每个面（避免共享顶点拉扯） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Boolean (`Boolean`)
- **Houdini 对标**: Boolean SOP
- **功能**: 对两个几何体执行布尔运算（并集/交集/差集）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | inputA | Geometry | null | 是 | 第一个几何体 |
  | inputB | Geometry | null | 是 | 第二个几何体 |
  | operation | String | "union" | 否 | 布尔运算类型（union/intersect/subtract） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Subdivide (`Subdivide`)
- **Houdini 对标**: Subdivide SOP
- **功能**: 对几何体进行细分（Catmull-Clark 或 Linear）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | iterations | Int | 1 | 否 | 细分迭代次数 |
  | algorithm | String | "linear" | 否 | 细分算法（catmull-clark / linear） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Mirror (`Mirror`)
- **Houdini 对标**: Mirror SOP
- **功能**: 沿平面镜像几何体，可选保留原始几何体并翻转面绕序
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | origin | Vector3 | (0,0,0) | 否 | 镜像平面原点 |
  | normal | Vector3 | (1,0,0) | 否 | 镜像平面法线（x/y/z 轴或自定义） |
  | keepOriginal | Bool | true | 否 | 是否保留原始几何体 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Fuse (`Fuse`)
- **Houdini 对标**: Fuse SOP
- **功能**: 合并距离阈值内的重叠顶点
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | distance | Float | 0.001 | 否 | 合并距离阈值 |
  | group | String | "" | 否 | 仅处理指定分组（留空=全部） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Triangulate (`Triangulate`)
- **Houdini 对标**: Divide SOP (triangulate 模式)
- **功能**: 将四边形和N边形统一转换为三角形
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（全三角形） |

### Normal (`Normal`)
- **Houdini 对标**: Normal SOP
- **功能**: 重新计算几何体的法线
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | type | String | "point" | 否 | 法线计算类型（point/vertex/primitive） |
  | cuspAngle | Float | 60.0 | 否 | 锐角阈值（超过此角度的边将产生硬边法线） |
  | weightByArea | Bool | true | 否 | 是否按面积加权 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Facet (`Facet`)
- **Houdini 对标**: Facet SOP
- **功能**: Unique Points / Consolidate / Compute Normals 三模式
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | mode | String | "unique" | 否 | 操作模式（unique/consolidate/computeNormals） |
  | normalMode | String | "flat" | 否 | 法线模式（flat/smooth），仅 computeNormals 模式使用 |
  | tolerance | Float | 0.0001 | 否 | consolidate 模式的合并距离阈值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Blast (`Blast`)
- **Houdini 对标**: Blast SOP
- **功能**: 按分组或编号删除点/面
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 要删除的分组名或编号列表 |
  | groupType | String | "primitive" | 否 | 分组类型（point/primitive） |
  | deleteNonSelected | Bool | false | 否 | 反转选择 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Clip (`Clip`)
- **Houdini 对标**: Clip SOP
- **功能**: 用一个平面裁剪几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | origin | Vector3 | (0,0,0) | 否 | 裁剪平面原点 |
  | normal | Vector3 | (0,1,0) | 否 | 裁剪平面法线 |
  | keepAbove | Bool | true | 否 | 保留法线方向侧 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Inset (`Inset`)
- **Houdini 对标**: PolyExtrude Inset 模式
- **功能**: 对面进行内缩，生成环形侧面带
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 要内缩的面分组（留空=全部） |
  | distance | Float | 0.1 | 否 | 内缩距离 |
  | outputInner | Bool | true | 否 | 是否输出内缩后的中心面 |
  | outputSide | Bool | true | 否 | 是否输出侧面带 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Peak (`Peak`)
- **Houdini 对标**: Peak SOP
- **功能**: 沿法线方向均匀偏移顶点位置（不改变拓扑）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | distance | Float | 0.1 | 否 | 沿法线偏移距离 |
  | group | String | "" | 否 | 仅对指定点分组偏移（留空=全部） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### PolyExpand2D (`PolyExpand2D`)
- **Houdini 对标**: PolyExpand2D SOP
- **功能**: 2D 多边形偏移/内缩（基于 Clipper2）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | offset | Float | 0.1 | 否 | 偏移量（正=膨胀，负=收缩） |
  | joinType | String | "round" | 否 | 拐角类型（round/miter/square） |
  | miterLimit | Float | 2.0 | 否 | Miter 模式的尖角限制 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 偏移后的几何体 |

### Reverse (`Reverse`)
- **Houdini 对标**: Reverse SOP
- **功能**: 翻转几何体的面法线方向（反转顶点顺序）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 仅翻转指定分组的面（留空=全部） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Sort (`Sort`)
- **Houdini 对标**: Sort SOP
- **功能**: 按指定规则排序点或面的顺序
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | key | String | "y" | 否 | 排序依据（x/y/z/random/attribute） |
  | reverse | Bool | false | 否 | 降序排列 |
  | pointSort | Bool | true | 否 | 是否排序点 |
  | primSort | Bool | false | 否 | 是否排序面 |
  | seed | Int | 0 | 否 | 随机种子（当 key=random 时使用） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Measure (`Measure`)
- **Houdini 对标**: Measure SOP
- **功能**: 测量面积、周长、曲率等几何属性并写入属性
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | type | String | "area" | 否 | 测量类型（area/perimeter/curvature/volume） |
  | attribName | String | "area" | 否 | 存储结果的属性名 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带测量属性） |

### Connectivity (`Connectivity`)
- **Houdini 对标**: Connectivity SOP
- **功能**: 为每个连通分量分配唯一的 class 属性值
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | attribName | String | "class" | 否 | 输出属性名 |
  | connectType | String | "point" | 否 | 连通类型（point/prim） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带 class 属性） |

### Material Assign (`MaterialAssign`)
- **Houdini 对标**: Material SOP
- **功能**: 为指定的面分组分配材质
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 面分组名称（留空=所有面） |
  | materialPath | String | "" | 否 | 材质路径（如 Assets/Materials/MyMat.mat） |
  | materialId | Int | 0 | 否 | 材质 ID（可选，用于整数索引） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带材质属性） |

### Pack (`Pack`)
- **Houdini 对标**: Pack SOP
- **功能**: 将几何体打包为 Point Group 或 Named Primitive
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | groupName | String | "packed" | 否 | 输出分组名称 |
  | createPrimitive | Bool | true | 否 | 创建一个代表打包几何体的 Primitive |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 打包后的几何体 |

### Unpack (`Unpack`)
- **Houdini 对标**: Unpack SOP
- **功能**: 从 Point Group 解包几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | groupName | String | "packed" | 否 | 要解包的分组名称 |
  | keepGroup | Bool | false | 否 | 解包后保留分组 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 解包后的几何体 |

## 常见组合模式 (Recipes)

### Recipe 1: 建筑外墙
```
Box → Extrude → Inset → Extrude → Normal
```
**说明**: 创建基础立方体，挤出增加深度，Inset 内缩形成窗框/墙壁结构，再次挤出增加内层厚度，最后重算法线。

### Recipe 2: CSG 布尔
```
[Box, Sphere] → Boolean → Fuse → Normal
```
**说明**: 将两个基本体通过布尔运算合并（如球体减去立方体），Fuse 修复可能存在的重复顶点，Normal 重算法线。

### Recipe 3: 对称建模
```
Box → Extrude → Mirror → Fuse → Subdivide
```
**说明**: 先在一侧建模（Box+Extrude），然后 Mirror 镜像到另一侧，Fuse 合并接缝处顶点，最后 Subdivide 平滑细分。
