# UV Skill 文档

## 概述
- **类别**: UV (Tier 2)
- **节点数量**: 5
- **适用场景**: UV 投影、展开、布局排列、变换和 Trim Sheet 映射

## 节点列表

### UV Project (`UVProject`)
- **Houdini 对标**: UVProject SOP
- **功能**: 对几何体进行 UV 投影（平面/柱面/球面/立方体）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | projectionType | String | "planar" | 否 | 投影类型（planar/cylindrical/spherical/cubic） |
  | group | String | "" | 否 | 仅对指定分组投影（留空=全部） |
  | scale | Vector3 | (1,1,1) | 否 | UV 缩放 |
  | offset | Vector3 | (0,0,0) | 否 | UV 偏移 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带 UV 属性） |

### UV Unwrap (`UVUnwrap`)
- **Houdini 对标**: UVUnwrap / UVFlatten SOP
- **功能**: 自动展开几何体的 UV（使用 xatlas）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 仅对指定分组展开（留空=全部） |
  | maxStretch | Float | 0.5 | 否 | 最大拉伸阈值 |
  | resolution | Int | 1024 | 否 | 图集分辨率 |
  | padding | Int | 2 | 否 | UV 岛间距（像素） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带展开的 UV） |

### UV Layout (`UVLayout`)
- **Houdini 对标**: UVLayout SOP
- **功能**: 重新排列 UV 岛以优化空间利用率
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | padding | Float | 0.01 | 否 | UV 岛之间的间距 |
  | resolution | Int | 1024 | 否 | 布局分辨率 |
  | rotateIslands | Bool | true | 否 | 是否允许旋转 UV 岛以优化排列 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### UV Transform (`UVTransform`)
- **Houdini 对标**: UVTransform SOP
- **功能**: 对 UV 坐标进行平移、旋转、缩放
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | translate | Vector3 | (0,0,0) | 否 | UV 平移 (仅 xy 有效) |
  | rotate | Float | 0.0 | 否 | UV 旋转角度 |
  | scale | Vector3 | (1,1,1) | 否 | UV 缩放 (仅 xy 有效) |
  | pivot | Vector3 | (0.5,0.5,0) | 否 | 变换枢轴 (仅 xy 有效) |
  | group | String | "" | 否 | 仅变换指定分组的 UV（留空=全部） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### UV Trim Sheet (`UVTrimSheet`)
- **Houdini 对标**: UV Edit + Group Filter
- **功能**: 将指定面组的 UV 映射到 Trim Sheet 贴图的指定矩形区域
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体（需已有 UV 属性） |
  | group | String | "" | 否 | 要重映射的面组名（留空=全部面） |
  | uMin | Float | 0.0 | 否 | 目标矩形左边界 (0~1) |
  | uMax | Float | 1.0 | 否 | 目标矩形右边界 (0~1) |
  | vMin | Float | 0.0 | 否 | 目标矩形下边界 (0~1) |
  | vMax | Float | 1.0 | 否 | 目标矩形上边界 (0~1) |
  | projectionAxis | String | "Y" | 否 | 若几何体没有 UV 则自动投影的方向 |
  | rotate90 | Bool | false | 否 | 将 UV 旋转 90°（横竖条切换） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带重映射 UV） |

## 常见组合模式 (Recipes)

### Recipe 1: 自动 UV 流程
```
Geometry → UVProject → UVLayout → MaterialAssign
```
**说明**: 先用 UVProject 做基础投影，再用 UVLayout 优化排列 UV 岛，最后分配材质。

### Recipe 2: Trim Sheet
```
Geometry → UVProject → UVTrimSheet → MaterialAssign
```
**说明**: 对几何体做 UV 投影后，使用 UVTrimSheet 将不同面组映射到 Trim Sheet 贴图的不同区域，适合建筑和环境资产的纹理复用。
