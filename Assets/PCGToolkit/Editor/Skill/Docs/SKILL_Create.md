# Create Skill 文档

## 概述
- **类别**: Create (Tier 1)
- **节点数量**: 16
- **适用场景**: 基础几何体生成、场景对象导入、几何体合并/删除/变换、分组创建与表达式筛选

## 节点列表

### Box (`Box`)
- **Houdini 对标**: Box SOP
- **功能**: 生成一个立方体几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | sizeX | Float | 1.0 | 否 | X 轴尺寸 |
  | sizeY | Float | 1.0 | 否 | Y 轴尺寸 |
  | sizeZ | Float | 1.0 | 否 | Z 轴尺寸 |
  | center | Vector3 | (0,0,0) | 否 | 中心位置 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Sphere (`Sphere`)
- **Houdini 对标**: Sphere SOP
- **功能**: 生成一个球体几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | radius | Float | 0.5 | 否 | 球体半径 |
  | rows | Int | 16 | 否 | 纬度方向的分段数 |
  | columns | Int | 32 | 否 | 经度方向的分段数 |
  | center | Vector3 | (0,0,0) | 否 | 中心位置 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Grid (`Grid`)
- **Houdini 对标**: Grid SOP
- **功能**: 生成一个平面网格几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | sizeX | Float | 10.0 | 否 | X 方向尺寸 |
  | sizeY | Float | 10.0 | 否 | Y 方向尺寸 |
  | rows | Int | 10 | 否 | 行数 |
  | columns | Int | 10 | 否 | 列数 |
  | center | Vector3 | (0,0,0) | 否 | 中心位置 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Circle (`Circle`)
- **Houdini 对标**: Circle SOP
- **功能**: 生成一个圆形几何体（多边形或线）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | radius | Float | 1.0 | 否 | 半径 |
  | divisions | Int | 16 | 否 | 分段数 |
  | arc | Float | 360.0 | 否 | 弧度角（360 = 完整圆） |
  | center | Vector3 | (0,0,0) | 否 | 中心位置 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Line (`Line`)
- **Houdini 对标**: Line SOP
- **功能**: 生成一条线段几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | origin | Vector3 | (0,0,0) | 否 | 起点 |
  | direction | Vector3 | (0,1,0) | 否 | 方向 |
  | length | Float | 1.0 | 否 | 长度 |
  | points | Int | 2 | 否 | 点数（包含起点和终点） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Tube (`Tube`)
- **Houdini 对标**: Tube SOP
- **功能**: 生成一个管状/圆柱体几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | radiusOuter | Float | 0.5 | 否 | 外半径 |
  | radiusInner | Float | 0.0 | 否 | 内半径（0 时为实心圆柱） |
  | height | Float | 1.0 | 否 | 高度 |
  | rows | Int | 1 | 否 | 高度方向的分段数 |
  | columns | Int | 16 | 否 | 圆周方向的分段数 |
  | endCaps | Bool | true | 否 | 是否封口 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Torus (`Torus`)
- **Houdini 对标**: Torus SOP
- **功能**: 生成一个环面（甜甜圈）几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | radiusMajor | Float | 1.0 | 否 | 主半径（环心到管心的距离） |
  | radiusMinor | Float | 0.25 | 否 | 次半径（管的截面半径） |
  | rows | Int | 16 | 否 | 管截面方向的分段数 |
  | columns | Int | 32 | 否 | 环周方向的分段数 |
  | center | Vector3 | (0,0,0) | 否 | 中心位置 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Platonic Solids (`PlatonicSolids`)
- **Houdini 对标**: Platonic Solids SOP
- **功能**: 生成正多面体（正四/八/十二/二十面体）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | type | String | "icosahedron" | 否 | 多面体类型（tetrahedron/octahedron/icosahedron/dodecahedron） |
  | radius | Float | 1.0 | 否 | 外接球半径 |
  | center | Vector3 | (0,0,0) | 否 | 中心位置 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Heightfield (`Heightfield`)
- **Houdini 对标**: HeightField SOP
- **功能**: 生成带 height 属性的噪声网格
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | sizeX | Float | 10.0 | 否 | 网格 X 方向大小 |
  | sizeZ | Float | 10.0 | 否 | 网格 Z 方向大小 |
  | resX | Int | 32 | 否 | X 方向分段数 |
  | resZ | Int | 32 | 否 | Z 方向分段数 |
  | amplitude | Float | 1.0 | 否 | 噪声振幅 |
  | frequency | Float | 0.5 | 否 | 噪声频率 |
  | octaves | Int | 4 | 否 | 噪声叠加层数 |
  | seed | Int | 0 | 否 | 随机种子偏移 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 高度场网格 |

### Font (`Font`)
- **Houdini 对标**: Font SOP
- **功能**: 文本转 2D 轮廓几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | text | String | "Hello" | 否 | 要生成的文本 |
  | fontSize | Float | 1.0 | 否 | 字体大小 |
  | letterSpacing | Float | 0.6 | 否 | 字间距 |
  | segments | Int | 8 | 否 | 曲线分段数 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 文本轮廓几何体 |

### Import Mesh (`ImportMesh`)
- **Houdini 对标**: File SOP（导入功能）
- **功能**: 从 Unity Mesh 资产导入几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | assetPath | String | "" | 否 | Mesh 资产路径（Assets/ 开头） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Merge (`Merge`)
- **Houdini 对标**: Merge SOP
- **功能**: 合并多个几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input0 | Geometry | null | 否 | 输入几何体 0 |
  | input1 | Geometry | null | 否 | 输入几何体 1 |
  | input2 | Geometry | null | 否 | 输入几何体 2 |
  | input3 | Geometry | null | 否 | 输入几何体 3 |
  | input4 | Geometry | null | 否 | 输入几何体 4 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 合并后的几何体 |

### Delete (`Delete`)
- **Houdini 对标**: Delete SOP
- **功能**: 删除几何体中的点、面或边
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 要删除的分组名（留空则用 filter） |
  | filter | String | "" | 否 | 过滤表达式（如 @P.y > 0） |
  | deleteNonSelected | Bool | false | 否 | 反转选择（删除未选中的元素） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Transform (`Transform`)
- **Houdini 对标**: Transform SOP
- **功能**: 对几何体进行平移、旋转、缩放变换
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | translate | Vector3 | (0,0,0) | 否 | 平移量 |
  | rotate | Vector3 | (0,0,0) | 否 | 旋转角度（欧拉角） |
  | scale | Vector3 | (1,1,1) | 否 | 缩放比例 |
  | uniformScale | Float | 1.0 | 否 | 统一缩放 |
  | pivot | Vector3 | (0,0,0) | 否 | 变换枢轴点 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Group Create (`GroupCreate`)
- **Houdini 对标**: Group SOP
- **功能**: 创建或修改点/面分组
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | groupName | String | "group1" | 否 | 分组名称 |
  | groupType | String | "point" | 否 | 分组类型（point/primitive） |
  | filter | String | "" | 否 | 过滤表达式（如 @P.y > 0） |
  | baseGroup | String | "" | 否 | 基于哪个已有分组进行过滤 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带新分组） |

### Group Expression (`GroupExpression`)
- **Houdini 对标**: Group Expression SOP
- **功能**: 使用表达式创建点或面分组
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | expression | String | "" | 否 | 分组表达式（如 @P.y > 5） |
  | groupName | String | "newGroup" | 否 | 分组名称 |
  | class | String | "point" | 否 | 分组类型（point/primitive） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带新分组） |

## 常见组合模式 (Recipes)

### Recipe 1: 基础建模起手式
```
Box → Transform → Extrude → Normal
```
**说明**: 从基础立方体出发，通过变换调整位置/大小，挤出增加厚度，最后重算法线。这是最常用的基础建模流程。

### Recipe 2: 地形生成
```
Grid → Noise → Normal → MaterialAssign
```
**说明**: 创建平面网格，用噪声变形生成地形起伏，重算法线使光照正确，最后分配材质。

### Recipe 3: 多体合并
```
[Box, Sphere, Tube] → Merge → Boolean
```
**说明**: 将多个基本体通过 Merge 合并后，使用 Boolean 进行 CSG 布尔运算，快速创建复杂形状。
