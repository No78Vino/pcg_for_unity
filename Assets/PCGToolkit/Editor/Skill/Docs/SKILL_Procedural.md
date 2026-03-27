# Procedural Skill 文档

## 概述
- **类别**: Procedural (Tier 3)
- **节点数量**: 3
- **适用场景**: L-System 植物生成、Voronoi 碎裂、波函数坍缩关卡布局

## 节点列表

### L-System (`LSystem`)
- **Houdini 对标**: L-System SOP
- **功能**: 使用 Lindenmayer 系统生成分形/植物几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | axiom | String | "F" | 否 | 初始公理字符串 |
  | rules | String | "F=FF+[+F-F-F]-[-F+F+F]" | 否 | 产生式规则（格式: F=FF+[+F-F-F]-[-F+F+F]，多条规则用分号分隔） |
  | iterations | Int | 3 | 否 | 迭代次数 |
  | angle | Float | 25.7 | 否 | 转向角度 |
  | stepLength | Float | 1.0 | 否 | 每步前进长度 |
  | stepLengthScale | Float | 0.5 | 否 | 每次迭代的长度缩放 |
  | thickness | Float | 0.1 | 否 | 分支粗细 |
  | seed | Int | 0 | 否 | 随机种子（用于随机规则） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | L-System 生成的曲线几何体 |

### Voronoi Fracture (`VoronoiFracture`)
- **Houdini 对标**: Voronoi Fracture SOP
- **功能**: 使用 Voronoi 图对几何体进行碎裂分割
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 要碎裂的几何体 |
  | points | Geometry | null | 否 | Voronoi 种子点（可选，留空则自动散布） |
  | numPoints | Int | 20 | 否 | 自动散布的种子点数（无 points 输入时使用） |
  | seed | Int | 0 | 否 | 随机种子 |
  | createInterior | Bool | true | 否 | 是否生成内部截面 |
  | interiorGroup | String | "inside" | 否 | 内部截面的分组名 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 碎裂后的几何体 |

### WFC (`WFC`)
- **Houdini 对标**: N/A（Wave Function Collapse）
- **功能**: 使用波函数坍缩算法进行程序化内容生成
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | gridSizeX | Int | 10 | 否 | 网格 X 方向大小 |
  | gridSizeY | Int | 10 | 否 | 网格 Y 方向大小 |
  | gridSizeZ | Int | 1 | 否 | 网格 Z 方向大小（2D 时为 1） |
  | tileCount | Int | 4 | 否 | 瓦片种类数量 |
  | seed | Int | 0 | 否 | 随机种子 |
  | tileSize | Float | 1.0 | 否 | 瓦片尺寸 |
  | maxAttempts | Int | 10 | 否 | 最大尝试次数 |
  | adjacencyRules | String | "" | 否 | 自定义邻接规则 JSON（格式: {"0":[0,1],"1":[0,1,2],...}），留空则使用默认规则 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 生成的几何体 |

## 常见组合模式 (Recipes)

### Recipe 1: 程序化树木
```
LSystem → PolyWire → Normal → Scatter
```
**说明**: 使用 L-System 生成分形树枝曲线，PolyWire 将曲线转为管状网格作为树干和树枝，Normal 重算法线，Scatter 在树冠位置散布叶子点。

### Recipe 2: 破碎效果
```
Box → VoronoiFracture → Pack → Instance
```
**说明**: 创建立方体，VoronoiFracture 将其碎裂为多个碎片，Pack 打包碎片，Instance 用于实例化到场景中进行物理模拟。

### Recipe 3: 关卡布局
```
WFC → CopyToPoints → Instance
```
**说明**: WFC 生成关卡瓦片布局（输出包含位置和瓦片类型属性的点），CopyToPoints 或 Instance 将对应瓦片模型放置到每个位置。
