# Distribute Skill 文档

## 概述
- **类别**: Distribute (Tier 2)
- **节点数量**: 6
- **适用场景**: 点散布、几何体复制到点、阵列、实例化、体积采样、射线投影

## 节点列表

### Scatter (`Scatter`)
- **Houdini 对标**: Scatter SOP
- **功能**: 在几何体表面随机散布点
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体（散布表面） |
  | count | Int | 100 | 否 | 散布点数量 |
  | seed | Int | 0 | 否 | 随机种子 |
  | densityAttrib | String | "" | 否 | 密度属性名（控制分布密度） |
  | relaxIterations | Int | 0 | 否 | 松弛迭代次数（使分布更均匀） |
  | group | String | "" | 否 | 仅在指定面分组上散布 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 散布点几何体 |

### Copy To Points (`CopyToPoints`)
- **Houdini 对标**: CopyToPoints SOP
- **功能**: 将源几何体复制到目标点的每个位置上
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | source | Geometry | null | 是 | 要复制的源几何体 |
  | target | Geometry | null | 是 | 目标点集 |
  | usePointOrient | Bool | true | 否 | 使用点的 orient 属性控制旋转 |
  | usePointScale | Bool | true | 否 | 使用点的 pscale 属性控制缩放 |
  | pack | Bool | false | 否 | 是否将副本打包为实例 |
  | transferAttributes | String | "" | 否 | 要传递的属性名（逗号分隔，如 variant,height） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Array (`Array`)
- **Houdini 对标**: Copy + Transform / Radial Copy
- **功能**: 线性阵列或径向阵列复制几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | mode | String | "linear" | 否 | 阵列模式（linear/radial） |
  | count | Int | 5 | 否 | 复制数量（含原始） |
  | offset | Vector3 | (1,0,0) | 否 | linear 模式：每次复制的偏移量 |
  | axis | Vector3 | (0,1,0) | 否 | radial 模式：旋转轴 |
  | center | Vector3 | (0,0,0) | 否 | radial 模式：旋转中心 |
  | fullAngle | Float | 360.0 | 否 | radial 模式：总旋转角度（度） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Instance (`Instance`)
- **Houdini 对标**: Instance SOP
- **功能**: 按属性选择不同几何体实例化到点上
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | target | Geometry | null | 是 | 目标点集 |
  | instance0 | Geometry | null | 否 | 实例几何体 0（index=0） |
  | instance1 | Geometry | null | 否 | 实例几何体 1（index=1） |
  | instance2 | Geometry | null | 否 | 实例几何体 2（index=2） |
  | instance3 | Geometry | null | 否 | 实例几何体 3（index=3） |
  | instance4 | Geometry | null | 否 | 实例几何体 4（index=4） |
  | instance5 | Geometry | null | 否 | 实例几何体 5（index=5） |
  | instance6 | Geometry | null | 否 | 实例几何体 6（index=6） |
  | instance7 | Geometry | null | 否 | 实例几何体 7（index=7） |
  | instanceAttrib | String | "instance" | 否 | 选择实例的属性名 |
  | usePointOrient | Bool | true | 否 | 使用点的 orient 属性控制旋转 |
  | usePointScale | Bool | true | 否 | 使用点的 pscale 属性控制缩放 |
  | pack | Bool | false | 否 | 是否输出打包的实例（而非展开的几何体） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Points From Volume (`PointsFromVolume`)
- **Houdini 对标**: Points from Volume SOP
- **功能**: 在包围盒内按体素网格生成点
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体（用于确定包围盒） |
  | spacing | Float | 0.5 | 否 | 体素间距 |
  | padding | Float | 0.0 | 否 | 包围盒外扩距离 |
  | jitter | Float | 0.0 | 否 | 随机抖动量（0=无抖动） |
  | seed | Int | 0 | 否 | 随机种子 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 生成的点几何体 |

### Ray (`Ray`)
- **Houdini 对标**: Ray SOP
- **功能**: 将几何体的点沿射线方向投影到目标表面
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 要投影的几何体 |
  | target | Geometry | null | 是 | 目标表面几何体 |
  | direction | Vector3 | (0,-1,0) | 否 | 射线方向（留空则使用点法线） |
  | maxDistance | Float | 100.0 | 否 | 最大投射距离 |
  | group | String | "" | 否 | 仅投影指定分组的点 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 投影后的几何体 |

## 常见组合模式 (Recipes)

### Recipe 1: 植被散布
```
Grid → Noise → Scatter → CopyToPoints → Instance
```
**说明**: 创建平面网格并用噪声变形模拟地形，在地形表面散布点，将植被模型复制到每个点位置，通过 Instance 节点实现多种植被实例化。

### Recipe 2: 环形阵列
```
Box → Array(radial) → Merge
```
**说明**: 使用径向阵列模式创建环形排列的副本，适合创建柱子、栏杆、环形建筑等重复结构。

### Recipe 3: 表面贴合
```
Scatter → CopyToPoints → Ray
```
**说明**: 在表面散布点后复制几何体，用 Ray 投射确保所有实例正确贴合目标表面，常用于将装饰物贴合到地形或建筑表面。
