# Curve Skill 文档

## 概述
- **类别**: Curve (Tier 2)
- **节点数量**: 6
- **适用场景**: 曲线创建、重采样、扫掠建模、线段转管道、裁切、倒角

## 节点列表

### Curve Create (`CurveCreate`)
- **Houdini 对标**: Curve SOP
- **功能**: 创建贝塞尔/多段线曲线
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | curveType | String | "polyline" | 否 | 曲线类型（bezier/polyline） |
  | closed | Bool | false | 否 | 是否闭合曲线 |
  | pointCount | Int | 4 | 否 | 控制点数量 |
  | radius | Float | 1.0 | 否 | 控制点分布半径 |
  | height | Float | 0.0 | 否 | Y轴高度 |
  | shape | String | "circle" | 否 | 形状（circle/line/spiral/random） |
  | seed | Int | 0 | 否 | 随机种子（shape=random时） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 曲线几何体 |

### Resample (`Resample`)
- **Houdini 对标**: Resample SOP
- **功能**: 按指定间距或数量重采样曲线/多段线
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入曲线/多段线 |
  | method | String | "length" | 否 | 采样方式（length/count） |
  | length | Float | 0.1 | 否 | 每段长度（method=length 时） |
  | segments | Int | 10 | 否 | 总段数（method=count 时） |
  | treatAsSubdivision | Bool | false | 否 | 是否在现有点之间细分 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 重采样后的几何体 |

### Sweep (`Sweep`)
- **Houdini 对标**: Sweep SOP
- **功能**: 沿路径曲线扫掠截面形状生成几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | backbone | Geometry | null | 是 | 路径曲线（骨架线） |
  | crossSection | Geometry | null | 否 | 截面形状（可选，默认使用圆形） |
  | scale | Float | 1.0 | 否 | 截面缩放 |
  | twist | Float | 0.0 | 否 | 沿路径的扭转角度 |
  | divisions | Int | 8 | 否 | 截面分段数（无截面输入时使用） |
  | capEnds | Bool | true | 否 | 封口两端 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 扫掠生成的几何体 |

### Poly Wire (`PolyWire`)
- **Houdini 对标**: PolyWire SOP
- **功能**: 将曲线/线段转换为管状网格（固定圆形截面）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入曲线/线段几何体（点序列） |
  | radius | Float | 0.1 | 否 | 管半径 |
  | sides | Int | 8 | 否 | 截面边数 |
  | capEnds | Bool | true | 否 | 是否封口两端 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 管状网格 |

### Carve (`Carve`)
- **Houdini 对标**: Carve SOP
- **功能**: 按参数范围裁切曲线（保留指定比例范围内的段）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入曲线 |
  | firstU | Float | 0.0 | 否 | 起始参数（0~1） |
  | secondU | Float | 1.0 | 否 | 结束参数（0~1） |
  | cutAtFirstU | Bool | true | 否 | 在起始参数处切断 |
  | cutAtSecondU | Bool | true | 否 | 在结束参数处切断 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 裁切后的曲线 |

### Fillet (`Fillet`)
- **Houdini 对标**: Fillet SOP
- **功能**: 对曲线或多段线的拐角进行倒角/圆角处理
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入曲线/多段线 |
  | radius | Float | 0.1 | 否 | 倒角半径 |
  | divisions | Int | 4 | 否 | 每个拐角的分段数 |
  | preserveEnds | Bool | true | 否 | 保持两端点不变 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 倒角后的曲线 |

## 常见组合模式 (Recipes)

### Recipe 1: 管道建模
```
CurveCreate → Resample → PolyWire → Normal
```
**说明**: 创建曲线后用 Resample 均匀化采样间距，PolyWire 将曲线转为管状网格，最后 Normal 重算法线。

### Recipe 2: 道路生成
```
CurveCreate → Resample → Sweep → UVProject
```
**说明**: 创建道路中心线，Resample 确保均匀采样，Sweep 使用截面形状扫掠生成路面几何体，UVProject 为路面添加 UV 以贴纹理。

### Recipe 3: 动画路径
```
CurveCreate → Carve → Resample
```
**说明**: 创建完整路径曲线，Carve 按参数裁切出需要的片段，Resample 调整采样密度，输出可用于动画或路径跟随。
