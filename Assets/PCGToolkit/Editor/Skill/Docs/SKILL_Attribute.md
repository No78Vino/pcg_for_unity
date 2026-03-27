# Attribute Skill 文档

## 概述
- **类别**: Attribute (Tier 2)
- **节点数量**: 8
- **适用场景**: 创建/修改/删除/传递几何体属性，驱动颜色、材质、分布密度等属性化操作

## 节点列表

### Attribute Create (`AttributeCreate`)
- **Houdini 对标**: AttribCreate SOP
- **功能**: 在几何体上创建新属性
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | name | String | "Cd" | 否 | 属性名称 |
  | class | String | "point" | 否 | 属性层级（point/vertex/primitive/detail） |
  | type | String | "float" | 否 | 数据类型（float/int/vector3/vector4/color/string） |
  | defaultFloat | Float | 0.0 | 否 | 默认值（Float 类型） |
  | defaultVector3 | Vector3 | (0,0,0) | 否 | 默认值（Vector3 类型） |
  | defaultString | String | "" | 否 | 默认值（String 类型） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Attribute Set (`AttributeSet`)
- **Houdini 对标**: AttribSet SOP
- **功能**: 设置或修改几何体上的属性值
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | name | String | "Cd" | 否 | 属性名称 |
  | class | String | "point" | 否 | 属性层级（point/vertex/primitive/detail） |
  | expression | String | "" | 否 | 值表达式（如 @P.y, rand(@ptnum) 等） |
  | group | String | "" | 否 | 仅对指定分组的元素进行设置 |
  | valueFloat | Float | 0.0 | 否 | 常量值（Float 类型，expression 为空时使用） |
  | valueVector3 | Vector3 | (0,0,0) | 否 | 常量值（Vector3 类型） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Attribute Delete (`AttributeDelete`)
- **Houdini 对标**: AttribDelete SOP
- **功能**: 删除几何体上的指定属性
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | name | String | "" | 否 | 要删除的属性名称（多个用逗号分隔） |
  | class | String | "point" | 否 | 属性层级（point/vertex/primitive/detail） |
  | deleteAll | Bool | false | 否 | 删除该层级的所有属性 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Attribute Copy (`AttributeCopy`)
- **Houdini 对标**: AttribCopy SOP
- **功能**: 将属性从一个几何体复制到另一个几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | dest | Geometry | null | 是 | 目标几何体（属性将被写入） |
  | src | Geometry | null | 是 | 源几何体（属性将被读取） |
  | name | String | "" | 否 | 要复制的属性名称 |
  | class | String | "point" | 否 | 属性层级（point/vertex/primitive/detail） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 目标几何体（包含复制的属性） |

### Attribute Promote (`AttributePromote`)
- **Houdini 对标**: AttribPromote SOP
- **功能**: 在属性层级之间提升或降级属性
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | name | String | "" | 否 | 要提升/降级的属性名称 |
  | fromClass | String | "point" | 否 | 源属性层级（point/vertex/primitive/detail） |
  | toClass | String | "detail" | 否 | 目标属性层级（point/vertex/primitive/detail） |
  | method | String | "min" | 否 | 聚合方法（用于降维：min/max/average/sum/first/last） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Attribute Randomize (`AttributeRandomize`)
- **Houdini 对标**: Attribute Randomize SOP
- **功能**: 为属性赋随机值（Uniform/Gaussian）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | name | String | "Cd" | 否 | 属性名称 |
  | class | String | "point" | 否 | 属性层级（point/primitive） |
  | type | String | "float" | 否 | 值类型（float/vector3/color） |
  | distribution | String | "uniform" | 否 | 分布（uniform/gaussian） |
  | min | Float | 0.0 | 否 | 最小值 |
  | max | Float | 1.0 | 否 | 最大值 |
  | seed | Int | 0 | 否 | 随机种子 |
  | group | String | "" | 否 | 仅对指定分组赋值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Attribute Transfer (`AttributeTransfer`)
- **Houdini 对标**: AttribTransfer SOP
- **功能**: 基于空间距离从源几何体传递属性到目标
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | target | Geometry | null | 是 | 目标几何体（接收属性） |
  | source | Geometry | null | 是 | 源几何体（提供属性） |
  | attributes | String | "*" | 否 | 要传递的属性名（逗号分隔，* 表示全部） |
  | maxDistance | Float | 0.0 | 否 | 最大搜索距离（0=无限） |
  | blendWidth | Float | 0.0 | 否 | 混合宽度（距离衰减） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 带传递属性的目标几何体 |

### Attribute Wrangle (`AttribWrangle`)
- **Houdini 对标**: AttribWrangle SOP
- **功能**: 对每个点/面执行表达式，修改属性值
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | code | String | "" | 否 | 表达式代码（分号分隔多条语句） |
  | runOver | String | "points" | 否 | 遍历模式（points/primitives/detail） |
  | group | String | "" | 否 | 仅对指定分组执行（留空=全部） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

## 常见组合模式 (Recipes)

### Recipe 1: 颜色驱动分布
```
Grid → AttributeCreate(density) → Scatter → CopyToPoints
```
**说明**: 在网格上创建 density 属性控制散布密度，用 Scatter 按密度属性分布点，再用 CopyToPoints 在点位置放置几何体。

### Recipe 2: 属性传递上色
```
Sphere → AttributeRandomize(Cd) → AttributeTransfer → Target
```
**说明**: 在球体上随机化颜色属性，然后通过 AttributeTransfer 将颜色传递到目标几何体上，实现基于距离的颜色映射。
